using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.DDA;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using System;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    
    [Header("Main Panel Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Toggle ddaToggle;
    
    [Header("Difficulty Panel Buttons")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;
    [SerializeField] private Button difficultyBackButton;
    
    [Header("Credits Panel Buttons")]
    [SerializeField] private Button creditsBackButton;
    
    [Header("Pause Panel Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseExitButton;
    
    
    [Header("Game Over Panel Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button gameOverExitButton;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject gameplayCanvas;

    [Header("Settings")]
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameplaySceneName = "Jakob";
    
    [Header("Input")]
    [SerializeField] private InputActionAsset uiInputActions;
    
    [Header("Decor Overlays")]
    [SerializeField] private Graphic[] nonBlockingGraphics;
    [SerializeField] private CanvasGroup[] nonBlockingCanvasGroups;
    
    [Header("Audio")]
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] private AudioSource gameplayMusicSource;
    [SerializeField] private AudioSource pauseMusicSource;
    [SerializeField] private AudioSource gameOverMusicSource;
    [SerializeField] private float retryAudioDelay = 1.5f;
    [SerializeField] private float muteAudioDelay = 4f;
    [SerializeField] private float musicFadeInDuration = 4f;
    [SerializeField] private float musicFadeOutDuration = 1f;
    [SerializeField] private float gameOverMusicDelay = 2f;
    [Tooltip("Tag to find all gameplay audio sources")]
    [SerializeField] private string gameplayAudioTag = "GameplayAudio";
    
    private bool gameplayAudioMuted = false;
    private Coroutine pauseMusicFadeCoroutine = null;
    private Coroutine gameOverMusicFadeCoroutine = null;
    private Coroutine gameplayAudioFadeCoroutine = null;

    // Cached text elements by tag
    private GameObject[] mainPanelTextElements;
    private GameObject[] difficultyPanelTextElements;
    private GameObject[] creditsPanelTextElements;

    private GameObject player;
    private bool isGamePaused;
    private bool isGameOver;
    private bool isRetrying; // Track if we're currently in a retry flow
    private bool isTransitioningToGameplay;
    private EventSystem menuEventSystem;
    private PlayerInput playerInput;
    private InputSystemUIInputModule menuInputModule;

    public static MainMenu Instance { get; private set; }

    private bool IsGameplaySceneLoaded => SceneManager.GetSceneByName(gameplaySceneName).isLoaded;

    bool gameStarted = false;

    public static Action fadeToGameplayScene;
    public static void OnFadeToGameplayScene() => fadeToGameplayScene?.Invoke();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // Make the entire canvas hierarchy persistent (not just this script object)
            // Find the root canvas object
            Transform canvasRoot = transform;
            while (canvasRoot.parent != null)
            {
                canvasRoot = canvasRoot.parent;
            }
            
            // Ensure it's at root and make it persistent
            canvasRoot.SetParent(null);
            DontDestroyOnLoad(canvasRoot.gameObject);
            
            Debug.Log($"[MainMenu] Made {canvasRoot.name} persistent across scenes");
            
            EnsureEventSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Subscribe to checkpoint load completion
        CheckpointManager.loadCheckpoint += OnCheckpointLoaded;
        ObjectivesManager.objectiveStarted += BeginGameMusic;
        PlayerActions.playerEscaped += () => gameplayMusicSource.enabled = false;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ObjectivesManager.objectiveStarted -= BeginGameMusic;
        PlayerActions.playerEscaped -= () => gameplayMusicSource.enabled = false;
    }

    private void Start()
    {
        // Find player reference
        player = GameObject.FindGameObjectWithTag(playerTag);

        // Auto-find gameplay canvas if not assigned
        if (!gameplayCanvas)
        {
            // Try to find a canvas named "Canvas" or "GameplayCanvas" or "HUD"
            gameplayCanvas = GameObject.Find("Canvas") ?? GameObject.Find("GameplayCanvas") ?? GameObject.Find("HUD");
        }

        // Cache text elements by tags
        CacheTextElements();

        // Setup button listeners
        SetupButtonListeners();
        
        // Ensure canvas is properly configured for input
        EnsureCanvasConfiguration();
        ConfigureDecorativeOverlays();

        // Check if we're auto-starting from a retry
        bool autoStart = PlayerPrefs.GetInt("RetryAutoStart", 0) == 1;
        
        if (autoStart)
        {
            Debug.Log("[MainMenu] Auto-starting from retry");
            
            // Restore DDA settings
            bool wasDDAEnabled = PlayerPrefs.GetInt("RetryDDAEnabled", 1) == 1;
            int previousDifficulty = PlayerPrefs.GetInt("RetryDifficulty", 0);
            
            if (wasDDAEnabled)
            {
                // Restore DDA mode
                DifficultyTracker.EnableTestingMode(false);
                Debug.Log("[MainMenu] Restored DDA mode from retry");
            }
            else
            {
                // Restore static difficulty mode
                DifficultyTracker.EnableTestingMode(true);
                DifficultyTracker.SetTestingDifficultyPercent(previousDifficulty);
                Debug.Log($"[MainMenu] Restored static difficulty {previousDifficulty}% from retry");
            }
            
            // Clear the retry flag
            PlayerPrefs.DeleteKey("RetryAutoStart");
            PlayerPrefs.DeleteKey("RetryDDAEnabled");
            PlayerPrefs.DeleteKey("RetryDifficulty");
            PlayerPrefs.Save();

            // Start game immediately without showing menu
            // StartGameplayScene();
            // Fade to gameplay scene + audio fade
            OnFadeToGameplayScene();
            StartCoroutine(FadeOutMusic(menuMusicSource, 1f, false)); // 1f matches default fade out duration
        }
        else
        {
            // Normal start - setup initial state
            if (showOnStart)
            {
                ShowMenu();
            }
            else
            {
                HideMenu();
            }
        }

        // Initialize DDA toggle state (only if not auto-starting)
        if (!autoStart && ddaToggle)
        {
            ddaToggle.isOn = true; // Default to DDA enabled
            OnDDAToggleChanged(ddaToggle.isOn);
        }

        // Hide all secondary panels initially
        if (difficultyPanel) difficultyPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    private void Update()
    {
        // Don't allow ESC key to trigger pause menu if any main menu panels are showing
        if ((mainPanel != null && mainPanel.activeSelf) ||
            (creditsPanel != null && creditsPanel.activeSelf) ||
            (difficultyPanel != null && difficultyPanel.activeSelf))
        {
            return; // Ignore ESC key when on main menu screens
        }

        // Allow ESC key to show pause menu during gameplay (not game over, not already paused)
        if (Input.GetKeyDown(KeyCode.Escape) && !isGamePaused && !isGameOver)
        {
            Debug.Log("[MainMenu] ESC pressed - showing pause menu");
            ShowPauseMenu();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && isGameOver)
        {
            Debug.Log("[MainMenu] ESC pressed but game is over - ignoring");
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && isGamePaused)
        {
            Debug.Log("[MainMenu] ESC pressed while paused - resuming game");
            OnResumeClicked();
        }
    }

    public void ShowMenu()
    {
        isGamePaused = true;
        
        SetGameplayInputActive(false);
        // Show the main menu
        if (mainPanel) mainPanel.SetActive(true);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (difficultyPanel) difficultyPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        // Show MainPanel text elements
        ShowMainPanelText();

        // Hide gameplay UI elements
        HideGameplayUI();

        // Pause the game
        // Time.timeScale = 0f;
        
        // Mute gameplay audio and play menu music
        MuteGameplayAudio();
        PlayMenuMusic();

        // Disable player controls
        if (player)
        {
            DisablePlayerControls();
        }

        // Unlock and show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowPauseMenu()
    {
        isGamePaused = true;
        
        SetGameplayInputActive(false);
        
        // Ensure EventSystem is active and ready
        EnsureEventSystemActive();
        
        // Show the pause menu
        if (mainPanel) mainPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (difficultyPanel) difficultyPanel.SetActive(false);
        if (pausePanel) 
        {
            pausePanel.SetActive(true);
            Debug.Log($"[MainMenu] PausePanel.SetActive(true) called - ActiveSelf={pausePanel.activeSelf}, ActiveInHierarchy={pausePanel.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[MainMenu] PausePanel reference is NULL!");
        }
        if (gameOverPanel) gameOverPanel.SetActive(false);

        // Hide gameplay UI elements
        HideGameplayUI();

        // Pause the game
        Time.timeScale = 0f;

        // Disable player controls
        if (player)
        {
            DisablePlayerControls();
        }

        // Unlock and show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Mute gameplay audio and play pause music with fade-in
        MuteGameplayAudio();
        ToggleGameMusicWithFade(false);
        PlayPauseMusicWithFadeIn();
        
        // Force enable button GameObjects if they're disabled
        ForceEnableButtons();
        
        // Debug button states AFTER showing panels
        DebugButtonStates();
        
        // Check for Graphic Raycaster
        CheckGraphicRaycaster();
        
        Debug.Log("[MainMenu] Game Paused - Pause Panel Active");
    }

    public void ShowGameOverMenu()
    {
        Debug.Log("[MainMenu] ========== SHOWING GAME OVER MENU ==========");
        isGamePaused = true;
        isGameOver = true;
        
        SetGameplayInputActive(false);
        
        // Ensure EventSystem is active and ready
        EnsureEventSystemActive();
        
        // Show the game over menu
        if (mainPanel) mainPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (difficultyPanel) difficultyPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(true);

        // Hide gameplay UI elements
        HideGameplayUI();

        // Pause the game
        Time.timeScale = 0f;

        // Disable player controls
        if (player)
        {
            DisablePlayerControls();
        }

        // Unlock and show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Mute gameplay audio with fade and play game over music with fade-in
        StartCoroutine(MuteGameplayAudioDelayed(muteAudioDelay));
        PlayGameOverMusicWithFadeIn();
        
        // Force enable button GameObjects if they're disabled
        ForceEnableButtons();
        
        // Debug button states
        DebugButtonStates();
        
        // Check for Graphic Raycaster
        CheckGraphicRaycaster();
        
        Debug.Log("[MainMenu] Game Over - Game Over Panel Active");
    }

    public void HideMenu()
    {
        Debug.Log("[MainMenu] ========== HIDING ALL MENUS ==========");
        Debug.Log($"[MainMenu] Previous state - isGamePaused: {isGamePaused}, isGameOver: {isGameOver}");
        
        isGamePaused = false;
        isGameOver = false;

        // Hide ALL panels
        if (mainPanel) mainPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (difficultyPanel) difficultyPanel.SetActive(false);
        if (pausePanel) 
        {
            pausePanel.SetActive(false);
            Debug.Log("[MainMenu] Deactivated Pause panel");
        }
        if (gameOverPanel) 
        {
            gameOverPanel.SetActive(false);
            Debug.Log("[MainMenu] Deactivated GameOver panel");
        }

        // Hide all text elements
        HideAllPanelText();

        // Re-enable gameplay EventSystems
        RestoreGameplayEventSystems();

        // Show gameplay UI elements
        ShowGameplayUI();

        // Unpause the game
        Time.timeScale = 1f;

        // Enable player controls
        if (player)
        {
            EnablePlayerControls();
        }

        // Lock and hide cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Stop all menu music with fade out and unmute gameplay audio (unless retrying, which has its own delay)
        StopMenuMusic();
        StopPauseMusicWithFadeOut();
        StopGameOverMusicWithFadeOut();
        if (!isRetrying && gameStarted)
        {
            UnmuteGameplayAudio();
        }
        
        SetGameplayInputActive(true);
    }

    private void HideGameplayUI()
    {
        if (gameplayCanvas) gameplayCanvas.SetActive(false);
    }

    private void ShowGameplayUI()
    {
        if (gameplayCanvas) gameplayCanvas.SetActive(true);
    }

    private void OnPlayClicked()
    {
        // Check if DDA is enabled
        bool isDDAEnabled = ddaToggle && ddaToggle.isOn;

        if (isDDAEnabled)
        {
            // DDA is enabled - start game immediately with dynamic difficulty
            DifficultyTracker.EnableTestingMode(false);
            Debug.Log("Dynamic Difficulty Adjustment: ENABLED - Starting game");
            //StartGameplayScene();
            // Fade to gameplay scene + audio fade
            OnFadeToGameplayScene();
            StartCoroutine(FadeOutMusic(menuMusicSource, 1f, false)); // 1f matches default fade out duration
        }
        else
        {
            // DDA is disabled - show difficulty selection panel
            Debug.Log("Static Difficulty Mode - Select difficulty");
            if (mainPanel) mainPanel.SetActive(false);
            if (difficultyPanel) difficultyPanel.SetActive(true);
            
            // Show difficulty panel text elements
            ShowDifficultyPanelText();
        }
    }

    private void OnDifficultySelected(int difficultyPercent)
    {
        // Set static difficulty
        DifficultyTracker.EnableTestingMode(true);
        DifficultyTracker.SetTestingDifficultyPercent(difficultyPercent);
        
        string difficultyName = difficultyPercent switch
        {
            0 => "Easy",
            50 => "Medium",
            100 => "Hard",
            _ => "Unknown"
        };
        
        Debug.Log($"Static Difficulty Set: {difficultyName} ({difficultyPercent}%) - Starting game");

        // Start the game
        //StartGameplayScene();
        // Fade to gameplay scene + audio fade
        OnFadeToGameplayScene();
        StartCoroutine(FadeOutMusic(menuMusicSource, 1f, false)); // 1f matches default fade out duration
    }

    private void OnCreditsClicked()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(true);
        ShowCreditsPanelText();
    }

    public void OnBackFromCredits()
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (creditsPanel) creditsPanel.SetActive(false);
        ShowMainPanelText();
    }

    public void OnBackFromDifficulty()
    {
        if (difficultyPanel) difficultyPanel.SetActive(false);
        if (mainPanel) mainPanel.SetActive(true);
        ShowMainPanelText();
    }

    private void OnExitClicked()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void OnDDAToggleChanged(bool isEnabled)
    {
        // Just log the change - actual behavior happens on Play button
        if (isEnabled)
        {
            Debug.Log("Dynamic Difficulty Mode Selected");
        }
        else
        {
            Debug.Log("Static Difficulty Mode Selected");
        }
    }

    private void DisablePlayerControls()
    {
        if (!player) return;

        // Disable player input components
        var playerActions = player.GetComponent<PlayerActions>();
        if (playerActions) playerActions.enabled = false;

        var playerMovement = player.GetComponent<MonoBehaviour>();
        if (playerMovement && playerMovement.GetType().Name.Contains("Movement"))
        {
            playerMovement.enabled = false;
        }

        // Disable any other player control scripts as needed
        var allPlayerScripts = player.GetComponents<MonoBehaviour>();
        foreach (var script in allPlayerScripts)
        {
            string scriptName = script.GetType().Name;
            if (scriptName.Contains("Player") && 
                (scriptName.Contains("Movement") || scriptName.Contains("Interact") || scriptName.Contains("Action")))
            {
                script.enabled = false;
            }
        }
    }

    private void EnablePlayerControls()
    {
        if (!player) return;

        // Re-enable player input components
        var playerActions = player.GetComponent<PlayerActions>();
        if (playerActions) playerActions.enabled = true;

        // Re-enable movement and interaction
        var allPlayerScripts = player.GetComponents<MonoBehaviour>();
        foreach (var script in allPlayerScripts)
        {
            string scriptName = script.GetType().Name;
            if (scriptName.Contains("Player") && 
                (scriptName.Contains("Movement") || scriptName.Contains("Interact") || scriptName.Contains("Action")))
            {
                script.enabled = true;
            }
        }
        
        // Restore PlayerInteract state - trigger the canInteract event to ensure proper state
        var playerInteract = player.GetComponent<PlayerInteract>();
        if (playerInteract)
        {
            // Re-trigger the interact permission based on current objective state
            // This ensures the component gets the correct canInteract state after being re-enabled
            if(ObjectivesManager.Instance != null && ObjectivesManager.Instance.completedTutorial)
            {
                PlayerActions.OnCanInteract(true);
            }
        }
    }

    private void RefreshObjectiveDisplay()
    {
        // Trigger the objectives manager to re-display the current objective
        if (ObjectivesManager.Instance != null && ObjectivesManager.Instance.CurrentObjective != null)
        {
            ObjectivesManager.OnDisplayObjective(ObjectivesManager.Instance.CurrentObjective, 0, 0f);
        }
    }

    public void StartGameplayScene()
    {
        if (IsGameplaySceneLoaded)
        {
            Debug.Log("[MainMenu] Gameplay scene already loaded, resuming");
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(gameplaySceneName));
            HideMenu();
            return;
        }

        isTransitioningToGameplay = true;
        HideMenu();
        Debug.Log($"[MainMenu] Loading gameplay scene '{gameplaySceneName}' (additive)");
        SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Additive);
    }

    private void LoadMainMenuScene()
    {
        isTransitioningToGameplay = false;
        Debug.Log($"[MainMenu] Returning to main menu scene '{mainMenuSceneName}'");
        var menuScene = SceneManager.GetSceneByName(mainMenuSceneName);
        if (menuScene.isLoaded)
        {
            SceneManager.SetActiveScene(menuScene);
            ShowMenu();
            UnloadGameplayScene();
        }
        else
        {
            Debug.LogWarning($"[MainMenu] Main menu scene '{mainMenuSceneName}' is not loaded yet.");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameplaySceneName)
        {
            CacheGameplayReferences(scene);
            SceneManager.SetActiveScene(scene);
            if (isTransitioningToGameplay)
            {
                HideMenu();
                isTransitioningToGameplay = false;
                
                // Unload the MainMenu scene after gameplay scene is loaded
                // DontDestroyOnLoad objects (like the menu canvas) will persist
                UnloadMainMenuScene();
            }
        }
        else if (scene.name == mainMenuSceneName)
        {
            SceneManager.SetActiveScene(scene);
            ShowMenu();
        }
    }

    private void CacheGameplayReferences(Scene scene)
    {
        player = GameObject.FindGameObjectWithTag(playerTag);
        playerInput = player?.GetComponent<PlayerInput>();
        if (gameplayCanvas == null || gameplayCanvas.scene != scene)
        {
            gameplayCanvas = FindGameplayCanvas(scene);
        }
    }

    private GameObject FindGameplayCanvas(Scene scene)
    {
        string[] candidateNames = { "GameplayCanvas", "HUD", "Canvas" };
        foreach (var candidateName in candidateNames)
        {
            var candidate = GameObject.Find(candidateName);
            if (candidate && candidate.scene == scene)
            {
                return candidate;
            }
        }

        foreach (var root in scene.GetRootGameObjects())
        {
            var canvas = root.GetComponentInChildren<Canvas>(true);
            if (canvas)
            {
                return canvas.gameObject;
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Unsubscribe from checkpoint events
        CheckpointManager.loadCheckpoint -= OnCheckpointLoaded;
        
        // Clean up listeners
        if (playButton) playButton.onClick.RemoveListener(OnPlayClicked);
        if (creditsButton) creditsButton.onClick.RemoveListener(OnCreditsClicked);
        if (exitButton) exitButton.onClick.RemoveListener(OnExitClicked);
        if (creditsBackButton) creditsBackButton.onClick.RemoveListener(OnBackFromCredits);
        if (difficultyBackButton) difficultyBackButton.onClick.RemoveListener(OnBackFromDifficulty);
        if (ddaToggle) ddaToggle.onValueChanged.RemoveListener(OnDDAToggleChanged);
        if (easyButton) easyButton.onClick.RemoveListener(() => OnDifficultySelected(0));
        if (mediumButton) mediumButton.onClick.RemoveListener(() => OnDifficultySelected(50));
        if (hardButton) hardButton.onClick.RemoveListener(() => OnDifficultySelected(100));
        
        // Pause panel
        if (resumeButton) resumeButton.onClick.RemoveListener(OnResumeClicked);
        if (pauseExitButton) pauseExitButton.onClick.RemoveListener(OnExitClicked);
        
        // Game over panel
        if (retryButton) retryButton.onClick.RemoveListener(OnRetryClicked);
        if (gameOverExitButton) gameOverExitButton.onClick.RemoveListener(OnExitClicked);
        
        // Clear singleton instance
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnResumeClicked()
    {
        Debug.Log("[MainMenu] Resuming game");
        HideMenu();
    }

    private void OnRetryClicked()
    {
        Debug.Log("[MainMenu] Retrying level - Using checkpoint system");
        isRetrying = true;
        isGameOver = false;
        HideMenu();
        
        // Start delayed audio unmute (gameplay audio stays muted for 2 seconds)
        StartCoroutine(UnmuteGameplayAudioDelayed(retryAudioDelay));
        
        CheckpointManager.Instance?.BeginLoading();
    }

    private void OnCheckpointLoaded()
    {
        if (!isRetrying) return;

        isRetrying = false;
        Debug.Log("[MainMenu] Checkpoint loaded, retry complete");
        GameOverManager.Instance?.ResetGameOver();
    }

    private void UnloadGameplayScene()
    {
        var gameplayScene = SceneManager.GetSceneByName(gameplaySceneName);
        if (gameplayScene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(gameplayScene);
        }
    }

    private void UnloadMainMenuScene()
    {
        var mainMenuScene = SceneManager.GetSceneByName(mainMenuSceneName);
        if (mainMenuScene.isLoaded)
        {
            Debug.Log($"[MainMenu] Unloading MainMenu scene - DontDestroyOnLoad objects will persist");
            SceneManager.UnloadSceneAsync(mainMenuScene);
        }
    }

    private void EnsureEventSystem()
    {
        if (menuEventSystem && menuEventSystem.gameObject)
        {
            SetupInputModule();
            return;
        }

        // Search for existing EventSystem in the entire hierarchy
        menuEventSystem = FindFirstObjectByType<EventSystem>();
        
        if (menuEventSystem == null)
        {
            // Create EventSystem as child of the canvas root, not this script object
            Transform canvasRoot = transform;
            while (canvasRoot.parent != null)
            {
                canvasRoot = canvasRoot.parent;
            }
            
            var eventSystemContainer = new GameObject("MainMenu EventSystem");
            eventSystemContainer.transform.SetParent(canvasRoot);
            menuEventSystem = eventSystemContainer.AddComponent<EventSystem>();
            Debug.Log("[MainMenu] Created persistent EventSystem for menu interaction");
        }
        else
        {
            Debug.Log($"[MainMenu] Found existing EventSystem: {menuEventSystem.name}");
        }

        SetupInputModule();
    }

    private void EnsureEventSystemActive()
    {
        // CRITICAL: Get canvas root and ensure it's on top
        Transform canvasRoot = transform;
        while (canvasRoot.parent != null)
        {
            canvasRoot = canvasRoot.parent;
        }
        
        var canvas = canvasRoot.GetComponent<Canvas>();
        if (canvas)
        {
            // FORCE canvas to render on top of everything else
            canvas.sortingOrder = 9999;
            canvas.overrideSorting = true;
            Debug.Log($"[MainMenu] Set Canvas sortingOrder to 9999 to render on top");
            
            // Ensure canvas render mode allows interaction
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            {
                Debug.LogWarning("[MainMenu] Canvas is in Camera mode but has no camera assigned!");
            }
            
            // Ensure Graphic Raycaster is enabled
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster)
            {
                if (!raycaster.enabled)
                {
                    raycaster.enabled = true;
                    Debug.Log("[MainMenu] Enabled Graphic Raycaster on Canvas");
                }
            }
            else
            {
                Debug.LogError("[MainMenu] GraphicRaycaster MISSING! Adding it...");
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("[MainMenu] GraphicRaycaster added to Canvas!");
            }
        }
        
        // Find all EventSystems in the scene
        EventSystem[] allEventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        
        Debug.Log($"[MainMenu] Found {allEventSystems.Length} EventSystem(s) in scene");
        
        // Disable all EventSystems except our menu one
        foreach (var es in allEventSystems)
        {
            if (es == menuEventSystem)
            {
                // Ensure our menu EventSystem is enabled
                if (!es.enabled)
                {
                    es.enabled = true;
                    Debug.Log("[MainMenu] Enabled menu EventSystem");
                }
                if (!es.gameObject.activeInHierarchy)
                {
                    es.gameObject.SetActive(true);
                    Debug.Log("[MainMenu] Activated menu EventSystem GameObject");
                }
            }
            else
            {
                // Temporarily disable other EventSystems to avoid conflicts
                if (es.enabled)
                {
                    es.enabled = false;
                    Debug.Log($"[MainMenu] Disabled gameplay EventSystem: {es.name}");
                }
            }
        }
        
        // Ensure our EventSystem exists
        if (menuEventSystem == null)
        {
            Debug.LogWarning("[MainMenu] Menu EventSystem is null! Recreating...");
            EnsureEventSystem();
        }
        
        // Make sure the current selected object is cleared to allow button interactions
        if (menuEventSystem != null)
        {
            menuEventSystem.SetSelectedGameObject(null);
        }
        
        // CRITICAL: Disable gameplay camera raycasting
        DisableGameplayCameraRaycasting();
        
        // Ensure input module is properly configured
        SetupInputModule();
        
        // Force enable UI input actions
        if (menuInputModule != null && uiInputActions != null)
        {
            var uiActionMap = uiInputActions.FindActionMap("UI");
            if (uiActionMap != null && !uiActionMap.enabled)
            {
                uiActionMap.Enable();
                Debug.Log("[MainMenu] Re-enabled UI input actions");
            }
        }
    }
    
    private void DisableGameplayCameraRaycasting()
    {
        // Find all cameras and disable their PhysicsRaycaster during menu
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        
        foreach (var cam in allCameras)
        {
            // Skip UI/Menu cameras
            if (cam.gameObject.name.Contains("UI") || cam.gameObject.name.Contains("Menu"))
                continue;
            
            // Disable PhysicsRaycaster on gameplay cameras
            var physicsRaycaster = cam.GetComponent<PhysicsRaycaster>();
            if (physicsRaycaster && physicsRaycaster.enabled)
            {
                physicsRaycaster.enabled = false;
                Debug.Log($"[MainMenu] Disabled PhysicsRaycaster on camera: {cam.name}");
            }
        }
    }

    private void RestoreGameplayEventSystems()
    {
        // Find all EventSystems and re-enable the gameplay ones
        EventSystem[] allEventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        
        foreach (var es in allEventSystems)
        {
            if (es != menuEventSystem)
            {
                es.enabled = true;
                Debug.Log($"[MainMenu] Re-enabled gameplay EventSystem: {es.name}");
            }
            else
            {
                // Keep menu EventSystem active but it won't interfere
                Debug.Log("[MainMenu] Menu EventSystem remains active for future use");
            }
        }
        
        // Re-enable gameplay camera raycasting
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCameras)
        {
            if (cam.gameObject.name.Contains("UI") || cam.gameObject.name.Contains("Menu"))
                continue;
            
            var physicsRaycaster = cam.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            if (physicsRaycaster && !physicsRaycaster.enabled)
            {
                physicsRaycaster.enabled = true;
                Debug.Log($"[MainMenu] Re-enabled PhysicsRaycaster on camera: {cam.name}");
            }
        }
    }

    private void SetupInputModule()
    {
        if (menuEventSystem == null)
            return;

        menuInputModule = menuEventSystem.GetComponent<InputSystemUIInputModule>();
        if (menuInputModule == null)
        {
            menuInputModule = menuEventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        if (menuInputModule != null && uiInputActions != null)
        {
            menuInputModule.actionsAsset = uiInputActions;
            
            // Enable the UI input actions so they can receive input
            var uiActionMap = uiInputActions.FindActionMap("UI");
            if (uiActionMap != null)
            {
                uiActionMap.Enable();
                Debug.Log("[MainMenu] UI Input actions enabled for menu interaction");
            }
        }
        else if (uiInputActions == null)
        {
            Debug.LogWarning("[MainMenu] UI Input Actions asset is not assigned! Menu buttons will not be clickable.");
        }
    }

    private void SetGameplayInputActive(bool isActive)
    {
        if (playerInput == null && player)
        {
            playerInput = player.GetComponent<PlayerInput>();
        }

        if (isActive)
        {
            if (playerInput)
            {
                playerInput.enabled = true;
                playerInput.actions?.Enable();
            }
        }
        else
        {
            // Disable all PlayerInput components to prevent gameplay input during menus
            var allPlayerInputs = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
            foreach (var pi in allPlayerInputs)
            {
                if (pi.enabled)
                {
                    pi.actions?.Disable();
                    pi.enabled = false;
                }
            }
        }
    }

    private void SetupButtonListeners()
    {
        Debug.Log("[MainMenu] Setting up button listeners...");
        
        // Main menu buttons
        if (playButton) 
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
            Debug.Log("[MainMenu] Play button listener added");
        }
        else Debug.LogWarning("[MainMenu] Play button is NULL!");
        
        if (creditsButton)
        {
            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(OnCreditsClicked);
        }
        
        if (exitButton)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitClicked);
        }
        
        if (creditsBackButton)
        {
            creditsBackButton.onClick.RemoveAllListeners();
            creditsBackButton.onClick.AddListener(OnBackFromCredits);
        }
        else
        {
            Debug.LogWarning("[MainMenu] Credits back button is NULL!");
        }
        
        if (difficultyBackButton)
        {
            difficultyBackButton.onClick.RemoveAllListeners();
            difficultyBackButton.onClick.AddListener(OnBackFromDifficulty);
        }
        else
        {
            Debug.LogWarning("[MainMenu] Difficulty back button is NULL!");
        }
        
        if (ddaToggle)
        {
            ddaToggle.onValueChanged.RemoveAllListeners();
            ddaToggle.onValueChanged.AddListener(OnDDAToggleChanged);
        }
        
        // Setup difficulty button listeners
        if (easyButton)
        {
            easyButton.onClick.RemoveAllListeners();
            easyButton.onClick.AddListener(() => OnDifficultySelected(0));
        }
        
        if (mediumButton)
        {
            mediumButton.onClick.RemoveAllListeners();
            mediumButton.onClick.AddListener(() => OnDifficultySelected(50));
        }
        
        if (hardButton)
        {
            hardButton.onClick.RemoveAllListeners();
            hardButton.onClick.AddListener(() => OnDifficultySelected(100));
        }
        
        // Setup pause panel button listeners
        if (resumeButton)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeClicked);
            Debug.Log("[MainMenu] Resume button listener added");
        }
        else Debug.LogWarning("[MainMenu] Resume button is NULL!");
        
        if (pauseExitButton)
        {
            pauseExitButton.onClick.RemoveAllListeners();
            pauseExitButton.onClick.AddListener(OnExitClicked);
        }
        
        // Setup game over panel button listeners
        if (retryButton)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
            Debug.Log("[MainMenu] Retry button listener added");
        }
        else Debug.LogWarning("[MainMenu] Retry button is NULL!");
        
        if (gameOverExitButton)
        {
            gameOverExitButton.onClick.RemoveAllListeners();
            gameOverExitButton.onClick.AddListener(OnExitClicked);
        }
        
        Debug.Log("[MainMenu] Button listeners setup complete!");
    }

    private void DebugButtonStates()
    {
        Debug.Log("[MainMenu] === BUTTON DEBUG INFO ===");
        
        // Check pause panel state
        if (pausePanel)
        {
            Debug.Log($"[MainMenu] Pause Panel: ActiveSelf={pausePanel.activeSelf}, ActiveInHierarchy={pausePanel.activeInHierarchy}");
        }
        
        if (resumeButton)
        {
            string hierarchy = GetGameObjectPath(resumeButton.gameObject);
            Debug.Log($"[MainMenu] Resume Button: Path='{hierarchy}'");
            Debug.Log($"[MainMenu] Resume Button: ActiveSelf={resumeButton.gameObject.activeSelf}, " +
                     $"ActiveInHierarchy={resumeButton.gameObject.activeInHierarchy}, " +
                     $"Enabled={resumeButton.enabled}, Interactable={resumeButton.interactable}, " +
                     $"Listeners={resumeButton.onClick.GetPersistentEventCount()}");
            
            // Check if button's GameObject itself is disabled
            if (!resumeButton.gameObject.activeSelf)
            {
                Debug.LogError($"[MainMenu] >>> RESUME BUTTON GAMEOBJECT IS DISABLED! <<<");
            }
        }
        else
        {
            Debug.LogError("[MainMenu] Resume Button reference is NULL!");
        }
        
        // Check game over panel state
        if (gameOverPanel)
        {
            Debug.Log($"[MainMenu] GameOver Panel: ActiveSelf={gameOverPanel.activeSelf}, ActiveInHierarchy={gameOverPanel.activeInHierarchy}");
        }
        
        if (retryButton)
        {
            string hierarchy = GetGameObjectPath(retryButton.gameObject);
            Debug.Log($"[MainMenu] Retry Button: Path='{hierarchy}'");
            Debug.Log($"[MainMenu] Retry Button: ActiveSelf={retryButton.gameObject.activeSelf}, " +
                     $"ActiveInHierarchy={retryButton.gameObject.activeInHierarchy}, " +
                     $"Enabled={retryButton.enabled}, Interactable={retryButton.interactable}, " +
                     $"Listeners={retryButton.onClick.GetPersistentEventCount()}");
            
            // Check if button's GameObject itself is disabled
            if (!retryButton.gameObject.activeSelf)
            {
                Debug.LogError($"[MainMenu] >>> RETRY BUTTON GAMEOBJECT IS DISABLED! <<<");
            }
        }
        else
        {
            Debug.LogError("[MainMenu] Retry Button reference is NULL!");
        }
        
        if (pauseExitButton)
        {
            Debug.Log($"[MainMenu] Pause Exit Button: ActiveInHierarchy={pauseExitButton.gameObject.activeInHierarchy}, " +
                     $"Enabled={pauseExitButton.enabled}, Interactable={pauseExitButton.interactable}");
        }
        
        if (gameOverExitButton)
        {
            Debug.Log($"[MainMenu] GameOver Exit Button: ActiveInHierarchy={gameOverExitButton.gameObject.activeInHierarchy}, " +
                     $"Enabled={gameOverExitButton.enabled}, Interactable={gameOverExitButton.interactable}");
        }
    }
    
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    private void ForceEnableButtons()
    {
        Debug.Log("[MainMenu] === FORCE ENABLING BUTTONS ===");
        
        // ONLY process the panel that is CURRENTLY ACTIVE - don't activate inactive ones!
        
        // If pause panel is active, fix its buttons and CanvasGroup
        if (pausePanel && pausePanel.activeSelf)
        {
            Debug.Log("[MainMenu] Processing Pause Panel buttons (panel is active)");
            
            // Check for CanvasGroup that might be blocking interaction
            var canvasGroup = pausePanel.GetComponent<CanvasGroup>();
            if (canvasGroup)
            {
                if (!canvasGroup.interactable)
                {
                    Debug.LogWarning($"[MainMenu] Pause Panel has CanvasGroup with interactable=false! Enabling it...");
                    canvasGroup.interactable = true;
                }
                if (canvasGroup.blocksRaycasts == false)
                {
                    Debug.LogWarning($"[MainMenu] Pause Panel CanvasGroup blocksRaycasts=false! Enabling it...");
                    canvasGroup.blocksRaycasts = true;
                }
            }
            
            // Force enable pause panel buttons ONLY
            if (resumeButton && !resumeButton.gameObject.activeSelf)
            {
                Debug.LogWarning($"[MainMenu] Resume button GameObject was disabled! Enabling it now...");
                resumeButton.gameObject.SetActive(true);
            }
            
            if (pauseExitButton && !pauseExitButton.gameObject.activeSelf)
            {
                Debug.LogWarning($"[MainMenu] Pause Exit button GameObject was disabled! Enabling it now...");
                pauseExitButton.gameObject.SetActive(true);
            }
            
        }
        else if (pausePanel)
        {
            Debug.Log("[MainMenu] Pause Panel exists but is INACTIVE - skipping button processing");
        }
        
        // If game over panel is active, fix its buttons and CanvasGroup
        if (gameOverPanel && gameOverPanel.activeSelf)
        {
            Debug.Log("[MainMenu] Processing GameOver Panel buttons (panel is active)");
            
            // Check for CanvasGroup that might be blocking interaction
            var canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (canvasGroup)
            {
                if (!canvasGroup.interactable)
                {
                    Debug.LogWarning($"[MainMenu] GameOver Panel has CanvasGroup with interactable=false! Enabling it...");
                    canvasGroup.interactable = true;
                }
                if (canvasGroup.blocksRaycasts == false)
                {
                    Debug.LogWarning($"[MainMenu] GameOver Panel CanvasGroup blocksRaycasts=false! Enabling it...");
                    canvasGroup.blocksRaycasts = true;
                }
            }
            
            // Force enable game over panel buttons ONLY
            if (retryButton && !retryButton.gameObject.activeSelf)
            {
                Debug.LogWarning($"[MainMenu] Retry button GameObject was disabled! Enabling it now...");
                retryButton.gameObject.SetActive(true);
            }
            
            if (gameOverExitButton && !gameOverExitButton.gameObject.activeSelf)
            {
                Debug.LogWarning($"[MainMenu] GameOver Exit button GameObject was disabled! Enabling it now...");
                gameOverExitButton.gameObject.SetActive(true);
            }
        }
        else if (gameOverPanel)
        {
            Debug.Log("[MainMenu] GameOver Panel exists but is INACTIVE - skipping button processing");
        }
    }
    


    private void CheckGraphicRaycaster()
    {
        Debug.Log("[MainMenu] === CANVAS RAYCASTER CHECK ===");
        
        // Get the canvas root
        Transform canvasRoot = transform;
        while (canvasRoot.parent != null)
        {
            canvasRoot = canvasRoot.parent;
        }
        
        var canvas = canvasRoot.GetComponent<Canvas>();
        if (canvas)
        {
            Debug.Log($"[MainMenu] Canvas found: {canvas.name}");
            
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster)
            {
                Debug.Log($"[MainMenu] GraphicRaycaster FOUND: Enabled={raycaster.enabled}, " +
                         $"IgnoreReversedGraphics={raycaster.ignoreReversedGraphics}");
            }
            else
            {
                Debug.LogError("[MainMenu] GraphicRaycaster MISSING! Adding it now...");
                raycaster = canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Debug.Log("[MainMenu] GraphicRaycaster added to Canvas!");
            }
        }
        else
        {
            Debug.LogError("[MainMenu] Canvas component not found!");
        }
        
        // Check EventSystem
        if (menuEventSystem)
        {
            Debug.Log($"[MainMenu] EventSystem: Enabled={menuEventSystem.enabled}, " +
                     $"CurrentSelected={menuEventSystem.currentSelectedGameObject}");
            
            if (menuInputModule)
            {
                Debug.Log($"[MainMenu] InputModule: Enabled={menuInputModule.enabled}, " +
                         $"ActionsAsset={(menuInputModule.actionsAsset != null ? "Assigned" : "NULL")}");
            }
            else
            {
                Debug.LogError("[MainMenu] InputModule is NULL!");
            }
        }
        else
        {
            Debug.LogError("[MainMenu] EventSystem is NULL!");
        }
    }

    private void CacheTextElements()
    {
        // Find all GameObjects with MainPanelElement tag
        try
        {
            mainPanelTextElements = GameObject.FindGameObjectsWithTag("MainPanelElement");
            Debug.Log($"[MainMenu] Found {mainPanelTextElements.Length} text elements with MainPanelElement tag");
        }
        catch (UnityException)
        {
            Debug.LogWarning("[MainMenu] Tag 'MainPanelElement' not defined in Tags and Layers. Please add it in Project Settings > Tags and Layers");
            mainPanelTextElements = new GameObject[0];
        }

        // Find all GameObjects with DifficultyElement tag
        try
        {
            difficultyPanelTextElements = GameObject.FindGameObjectsWithTag("DifficultyElement");
            Debug.Log($"[MainMenu] Found {difficultyPanelTextElements.Length} text elements with DifficultyElement tag");
        }
        catch (UnityException)
        {
            Debug.LogWarning("[MainMenu] Tag 'DifficultyElement' not defined in Tags and Layers. Please add it in Project Settings > Tags and Layers");
            difficultyPanelTextElements = new GameObject[0];
        }

        // Find all GameObjects with CreditsPanelElement tag
        try
        {
            creditsPanelTextElements = GameObject.FindGameObjectsWithTag("CreditsPanelElement");
            Debug.Log($"[MainMenu] Found {creditsPanelTextElements.Length} text elements with CreditsPanelElement tag");
        }
        catch (UnityException)
        {
            Debug.LogWarning("[MainMenu] Tag 'CreditsPanelElement' not defined in Tags and Layers. Please add it in Project Settings > Tags and Layers");
            creditsPanelTextElements = new GameObject[0];
        }
    }

    private void ShowMainPanelText()
    {
        // Show MainPanel text elements
        if (mainPanelTextElements != null)
        {
            foreach (var textElement in mainPanelTextElements)
            {
                if (textElement != null)
                {
                    textElement.SetActive(true);
                }
            }
            Debug.Log($"[MainMenu] Showed {mainPanelTextElements.Length} MainPanel text elements");
        }

        // Hide DifficultyPanel text elements
        if (difficultyPanelTextElements != null)
        {
            foreach (var textElement in difficultyPanelTextElements)
            {
                if (textElement != null)
                {
                    textElement.SetActive(false);
                }
            }
        }
        
        if (creditsPanelTextElements != null)
        {
            foreach (var textElement in creditsPanelTextElements)
            {
                if (textElement != null)
                {
                    textElement.SetActive(false);
                }
            }
        }
    }

    private void ShowDifficultyPanelText()
    {
        // Hide MainPanel text elements
        if (mainPanelTextElements != null)
        {
            foreach (var textElement in mainPanelTextElements)
            {
                if (textElement != null)
                {
                    textElement.SetActive(false);
                }
            }
        }

        // Show DifficultyPanel text elements
        if (difficultyPanelTextElements != null)
        {
            foreach (var textElement in difficultyPanelTextElements)
            {
                if (textElement != null)
                {
                    textElement.SetActive(true);
                }
            }
            Debug.Log($"[MainMenu] Showed {difficultyPanelTextElements.Length} DifficultyPanel text elements");
        }
        
        if (creditsPanelTextElements != null)
        {
            foreach (var textElement in creditsPanelTextElements)
            {
                if (textElement != null)
                {
                    textElement.SetActive(false);
                }
            }
        }
    }
    
    private void ShowCreditsPanelText()
    {
        if (mainPanelTextElements != null)
        {
            foreach (var textElement in mainPanelTextElements)
            {
                if (textElement != null)
                {
                    textElement.SetActive(false);
                }
            }
        }
        
        if (difficultyPanelTextElements != null)
        {
            foreach (var textElement in difficultyPanelTextElements)
            {
                if (textElement != null)
                {
                    textElement.SetActive(false);
                }
            }
        }
        
        if (creditsPanelTextElements != null)
        {
            foreach (var textElement in creditsPanelTextElements)
            {
                if (textElement != null)
                {
                    textElement.SetActive(true);
                }
            }
            Debug.Log($"[MainMenu] Showed {creditsPanelTextElements.Length} CreditsPanel text elements");
        }
    }

    private void HideAllPanelText()
    {
        // Hide all text elements when no panel is showing
        if (mainPanelTextElements != null)
        {
            foreach (var textElement in mainPanelTextElements)
            {
                if (textElement != null)
                {
                    textElement.SetActive(false);
                }
            }
        }

        if (difficultyPanelTextElements != null)
        {
            foreach (var textElement in difficultyPanelTextElements)
            {
                if (textElement != null)
                {
                    textElement.SetActive(false);
                }
            }
        }
        
        if (creditsPanelTextElements != null)
        {
            foreach (var textElement in creditsPanelTextElements)
            {
                if (textElement != null)
                {
                    textElement.SetActive(false);
                }
            }
        }
    }

    private void EnsureCanvasConfiguration()
    {
        // Get the canvas root
        Transform canvasRoot = transform;
        while (canvasRoot.parent != null)
        {
            canvasRoot = canvasRoot.parent;
        }

        var canvas = canvasRoot.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[MainMenu] Canvas component not found on root!");
            return;
        }

        Debug.Log($"[MainMenu] Ensuring Canvas configuration - RenderMode: {canvas.renderMode}");

        // Ensure Canvas has GraphicRaycaster
        var raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log("[MainMenu] Added GraphicRaycaster to Canvas");
        }
        raycaster.enabled = true;

        // If using Screen Space - Camera, ensure camera is assigned
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
            Debug.Log("[MainMenu] Assigned Main Camera to Canvas");
        }

        // Set canvas to render on top
        canvas.sortingOrder = 1000;
        Debug.Log($"[MainMenu] Canvas configured - Raycaster enabled, SortingOrder: {canvas.sortingOrder}");
    }

    private void ConfigureDecorativeOverlays()
    {
        if (nonBlockingGraphics != null)
        {
            foreach (var graphic in nonBlockingGraphics)
            {
                if (!graphic)
                {
                    continue;
                }
                if (graphic.raycastTarget)
                {
                    graphic.raycastTarget = false;
                    Debug.Log($"[MainMenu] Disabled raycast target on decor graphic '{graphic.name}'");
                }
            }
        }
        
        if (nonBlockingCanvasGroups != null)
        {
            foreach (var canvasGroup in nonBlockingCanvasGroups)
            {
                if (!canvasGroup)
                {
                    continue;
                }
                if (canvasGroup.blocksRaycasts)
                {
                    canvasGroup.blocksRaycasts = false;
                    Debug.Log($"[MainMenu] Disabled raycast blocking on decor CanvasGroup '{canvasGroup.name}'");
                }
            }
        }
    }
    
    #region Audio Management
    
    /// <summary>
    /// Mutes all gameplay audio sources (lasers, footsteps, etc.)
    /// </summary>
    private void MuteGameplayAudio()
    {
        if (gameplayAudioMuted) return;
        
        Debug.LogWarning("[MainMenu] Muting gameplay audio");
        gameplayAudioMuted = true;
        
        // Find and mute all AudioSources in the gameplay scene
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AudioSource audioSource in allAudioSources)
        {
            // Skip the menu music source
            if (audioSource == menuMusicSource) continue;
            
            // Mute gameplay audio sources
            if (audioSource.gameObject.scene.name == gameplaySceneName || 
                audioSource.CompareTag(gameplayAudioTag))
            {
                audioSource.mute = true;
            }
        }
        ToggleGameMusicWithFade(false);
    }

    /// <summary>
    /// Unmutes all gameplay audio sources and restores volumes to 1.0
    /// </summary>
    private void UnmuteGameplayAudio()
    {
        if (!gameplayAudioMuted) return;
        
        Debug.LogWarning("[MainMenu] Unmuting gameplay audio");
        gameplayAudioMuted = false;
        
        // Find and unmute all AudioSources in the gameplay scene
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AudioSource audioSource in allAudioSources)
        {
            // Skip the menu music source
            if (audioSource == menuMusicSource) continue;
            
            // Unmute gameplay audio sources and restore volume
            if (audioSource.gameObject.scene.name == gameplaySceneName || 
                audioSource.CompareTag(gameplayAudioTag))
            {
                audioSource.mute = false;
                // Restore volume to full (in case it was faded out)
                if (audioSource.volume < 0.1f)
                {
                    audioSource.volume = 1f;
                }
            }
        }
        ToggleGameMusicWithFade(true);
    }
    
    /// <summary>
    /// Toggles the gameplay music on or off with a fade effect.
    /// </summary>
    void ToggleGameMusicWithFade(bool state)
    {
        if (gameplayMusicSource == null) 
        { 
            Debug.LogWarning("[MainMenu] Gameplay music source is NULL, cannot toggle music");
            return;
        }

        if (gameplayAudioFadeCoroutine != null)
        {
            StopCoroutine(gameplayAudioFadeCoroutine);
            gameplayAudioFadeCoroutine = null;
        }
        
        Debug.LogWarning("[MainMenu] Toggling gameplay music with fade-in");
        if (state)
        {
            gameplayAudioFadeCoroutine = StartCoroutine(FadeInMusic(gameplayMusicSource, musicFadeInDuration, isGameOverMusic: false));
        }
        else
        {
            gameplayAudioFadeCoroutine = StartCoroutine(FadeOutMusic(gameplayMusicSource, musicFadeOutDuration, isGameOverMusic: false, false));
        }
    }

    void BeginGameMusic()
    {
        gameStarted = true;
        ToggleGameMusicWithFade(true);
    }

    /// <summary>
    /// Starts playing menu music if available
    /// </summary>
    private void PlayMenuMusic()
    {
        if (menuMusicSource != null && !menuMusicSource.isPlaying)
        {
            menuMusicSource.Play();
            Debug.Log("[MainMenu] Started menu music");
        }
    }
    
    /// <summary>
    /// Stops playing menu music
    /// </summary>
    private void StopMenuMusic()
    {
        if (menuMusicSource != null && menuMusicSource.isPlaying)
        {
            menuMusicSource.Stop();
            Debug.Log("[MainMenu] Stopped menu music");
        }
    }
    
    /// <summary>
    /// Fades in pause music over the specified duration
    /// </summary>
    private void PlayPauseMusicWithFadeIn()
    {
        if (pauseMusicSource == null) return;

        // Stop any current pause music fade
        if (pauseMusicFadeCoroutine != null)
        {
            StopCoroutine(pauseMusicFadeCoroutine);
            pauseMusicFadeCoroutine = null;
        }
        
        pauseMusicFadeCoroutine = StartCoroutine(FadeInMusic(pauseMusicSource, musicFadeInDuration, isGameOverMusic: false));
    }
    
    /// <summary>
    /// Fades out pause music over the specified duration
    /// </summary>
    private void StopPauseMusicWithFadeOut()
    {
        if (pauseMusicSource == null) return;

        // Stop any current pause music fade
        if (pauseMusicFadeCoroutine != null)
        {
            StopCoroutine(pauseMusicFadeCoroutine);
            pauseMusicFadeCoroutine = null;
        }
        
        pauseMusicFadeCoroutine = StartCoroutine(FadeOutMusic(pauseMusicSource, musicFadeOutDuration, isGameOverMusic: false));
    }
    
    /// <summary>
    /// Fades in game over music after a delay
    /// </summary>
    private void PlayGameOverMusicWithFadeIn()
    {
        if (gameOverMusicSource == null) return;

        // Stop any current game over music fade
        if (gameOverMusicFadeCoroutine != null)
        {
            StopCoroutine(gameOverMusicFadeCoroutine);
            gameOverMusicFadeCoroutine = null;
        }
        
        gameOverMusicFadeCoroutine = StartCoroutine(PlayGameOverMusicWithDelayCoroutine());
    }
    
    /// <summary>
    /// Coroutine to delay then fade in game over music
    /// </summary>
    private System.Collections.IEnumerator PlayGameOverMusicWithDelayCoroutine()
    {
        Debug.Log($"[MainMenu] Waiting {gameOverMusicDelay} seconds before fading in game over music");
        yield return new WaitForSecondsRealtime(gameOverMusicDelay);
        if (gameOverMusicSource != null)
        {
            Debug.Log("[MainMenu] Starting game over music fade-in");
            yield return FadeInMusic(gameOverMusicSource, musicFadeInDuration, isGameOverMusic: true);
        }
        
        gameOverMusicFadeCoroutine = null;
    }
    
    /// <summary>
    /// Fades out game over music over the specified duration
    /// </summary>
    private void StopGameOverMusicWithFadeOut()
    {
        if (gameOverMusicSource == null) return;

        // Stop any current game over music fade
        if (gameOverMusicFadeCoroutine != null)
        {
            StopCoroutine(gameOverMusicFadeCoroutine);
            gameOverMusicFadeCoroutine = null;
        }
        
        gameOverMusicFadeCoroutine = StartCoroutine(FadeOutMusic(gameOverMusicSource, musicFadeOutDuration, isGameOverMusic: true));
    }
    
    /// <summary>
    /// Coroutine to fade in an audio source
    /// </summary>
    private System.Collections.IEnumerator FadeInMusic(AudioSource audioSource, float duration, bool isGameOverMusic)
    {
        if (audioSource == null) yield break;
        
        Debug.Log($"[MainMenu] Fading in {audioSource.name} over {duration} seconds");
        
        // Start playing if not already
        if (!audioSource.isPlaying)
        {
            audioSource.volume = 0f;
            audioSource.Play();
        }
        
        float startVolume = audioSource.volume;
        float targetVolume = 0.4f; // was 1f, but it was too loud
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float fadeProgress = elapsed / duration;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, fadeProgress);
            yield return null;
        }
        
        audioSource.volume = targetVolume;
        Debug.Log($"[MainMenu] Fade in complete for {audioSource.name}");
        
        // Clear the tracking reference
        if (isGameOverMusic)
            gameOverMusicFadeCoroutine = null;
        else
            pauseMusicFadeCoroutine = null;
    }
    
    /// <summary>
    /// Coroutine to fade out an audio source
    /// </summary>
    private System.Collections.IEnumerator FadeOutMusic(AudioSource audioSource, float duration, bool isGameOverMusic, bool stopMusic = true)
    {
        if (audioSource == null) yield break;
        
        Debug.Log($"[MainMenu] Fading out {audioSource.name} over {duration} seconds - start volume: {audioSource.volume}");
        
        float startVolume = audioSource.volume;
        float targetVolume = 0f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float fadeProgress = elapsed / duration;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, fadeProgress);
            Debug.Log($"[MainMenu] Fade out progress: {fadeProgress:F2}, volume: {audioSource.volume:F2}");
            yield return null;
        }
        
        audioSource.volume = targetVolume;
        if(stopMusic)
        {
            audioSource.Stop();
        }

        Debug.Log($"[MainMenu] Fade out complete for {audioSource.name}");
        
        // Clear the tracking reference
        if (isGameOverMusic)
            gameOverMusicFadeCoroutine = null;
        else
            pauseMusicFadeCoroutine = null;
    }
    
    /// <summary>
    /// Unmute gameplay audio after a delay (for retry functionality)
    /// </summary>
    private System.Collections.IEnumerator UnmuteGameplayAudioDelayed(float delay)
    {
        Debug.Log($"[MainMenu] Delaying gameplay audio unmute by {delay} seconds");
        yield return new WaitForSecondsRealtime(delay);
        UnmuteGameplayAudio();
    }
    
    /// <summary>
    /// Mute gameplay audio after a delay with fade-out effect (for when player is caught)
    /// </summary>
    private System.Collections.IEnumerator MuteGameplayAudioDelayed(float delay)
    {
        Debug.Log($"[MainMenu] Fading out gameplay audio over {delay} seconds");
        
        // Find all gameplay audio sources
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        // Store original volumes and filter to gameplay sources only
        System.Collections.Generic.List<AudioSource> gameplayAudioSources = new System.Collections.Generic.List<AudioSource>();
        System.Collections.Generic.Dictionary<AudioSource, float> originalVolumes = new System.Collections.Generic.Dictionary<AudioSource, float>();
        
        foreach (AudioSource audioSource in allAudioSources)
        {
            // Skip the menu music source
            if (audioSource == menuMusicSource) continue;
            
            // Only include gameplay audio sources
            if (audioSource.gameObject.scene.name == gameplaySceneName || 
                audioSource.CompareTag(gameplayAudioTag))
            {
                gameplayAudioSources.Add(audioSource);
                originalVolumes[audioSource] = audioSource.volume;
            }
        }
        
        // Fade out over the delay duration
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaledDeltaTime in case game is paused
            float fadeProgress = elapsed / delay; // 0 to 1
            float volumeMultiplier = 1f - fadeProgress; // 1 to 0
            
            // Apply fade to all gameplay audio sources
            foreach (AudioSource audioSource in gameplayAudioSources)
            {
                if (audioSource != null && originalVolumes.ContainsKey(audioSource))
                {
                    audioSource.volume = originalVolumes[audioSource] * volumeMultiplier;
                }
            }
            if (!isRetrying)
            {
                yield break; // Exit early if player is retrying
            }
            yield return null; // Wait one frame
        }
        
        // Ensure all volumes are at 0 and mute them
        foreach (AudioSource audioSource in gameplayAudioSources)
        {
            if (audioSource != null)
            {
                audioSource.volume = 0f;
            }
        }
        
        // Finally, mute all gameplay audio
        MuteGameplayAudio();
        
        Debug.Log("[MainMenu] Gameplay audio fade-out complete");
    }
    
    #endregion
}
