using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.DDA;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Toggle ddaToggle;
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;
    
    [Header("Pause Panel Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseExitButton;
    [SerializeField] private Button howToPlayButton;
    
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

    private GameObject player;
    private bool isGamePaused;
    private bool isGameOver;
    private bool isRetrying; // Track if we're currently in a retry flow
    private bool isTransitioningToGameplay;

    public static MainMenu Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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

        // Subscribe to checkpoint load completion
        CheckpointManager.loadCheckpoint += OnCheckpointLoaded;

        // Setup button listeners
        if (playButton) playButton.onClick.AddListener(OnPlayClicked);
        if (creditsButton) creditsButton.onClick.AddListener(OnCreditsClicked);
        if (exitButton) exitButton.onClick.AddListener(OnExitClicked);
        if (ddaToggle) ddaToggle.onValueChanged.AddListener(OnDDAToggleChanged);
        
        // Setup difficulty button listeners
        if (easyButton) easyButton.onClick.AddListener(() => OnDifficultySelected(0));
        if (mediumButton) mediumButton.onClick.AddListener(() => OnDifficultySelected(50));
        if (hardButton) hardButton.onClick.AddListener(() => OnDifficultySelected(100));
        
        // Setup pause panel button listeners
        if (resumeButton) resumeButton.onClick.AddListener(OnResumeClicked);
        if (pauseExitButton) pauseExitButton.onClick.AddListener(OnExitClicked);
        if (howToPlayButton) howToPlayButton.onClick.AddListener(OnHowToPlayClicked);
        
        // Setup game over panel button listeners
        if (retryButton) retryButton.onClick.AddListener(OnRetryClicked);
        if (gameOverExitButton) gameOverExitButton.onClick.AddListener(OnExitClicked);

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
            StartGameplayScene();
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
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
    }

    private void Update()
    {
        // Allow ESC key to show pause menu during gameplay (not game over)
        if (Input.GetKeyDown(KeyCode.Escape) && !isGamePaused && !isGameOver)
        {
            ShowPauseMenu();
        }
    }

    public void ShowMenu()
    {
        isGamePaused = true;
        
        // Show the main menu
        if (mainPanel) mainPanel.SetActive(true);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (difficultyPanel) difficultyPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (howToPlayPanel) howToPlayPanel.SetActive(false);

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
    }

    public void ShowPauseMenu()
    {
        isGamePaused = true;
        
        // Show the pause menu
        if (mainPanel) mainPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (difficultyPanel) difficultyPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(true);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (howToPlayPanel) howToPlayPanel.SetActive(false);

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
        
        Debug.Log("[MainMenu] Game Paused");
    }

    public void ShowGameOverMenu()
    {
        isGamePaused = true;
        isGameOver = true;
        
        // Show the game over menu
        if (mainPanel) mainPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (difficultyPanel) difficultyPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (howToPlayPanel) howToPlayPanel.SetActive(false);

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
        
        Debug.Log("[MainMenu] Game Over");
    }

    public void HideMenu()
    {
        isGamePaused = false;
        isGameOver = false;

        // Hide all panels
        if (mainPanel) mainPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (difficultyPanel) difficultyPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (howToPlayPanel) howToPlayPanel.SetActive(false);

        // Show gameplay UI elements
        ShowGameplayUI();
        
        // Only refresh objective display if not coming from game over (retry)
        // The checkpoint system will handle objectives properly during retry
        if (!isGameOver && !isRetrying)
        {
            RefreshObjectiveDisplay();
        }

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
            StartGameplayScene();
        }
        else
        {
            // DDA is disabled - show difficulty selection panel
            Debug.Log("Static Difficulty Mode - Select difficulty");
            if (mainPanel) mainPanel.SetActive(false);
            if (difficultyPanel) difficultyPanel.SetActive(true);
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
        StartGameplayScene();
    }

    private void OnCreditsClicked()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(true);
    }

    public void OnBackFromCredits()
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (creditsPanel) creditsPanel.SetActive(false);
    }

    public void OnBackFromDifficulty()
    {
        if (mainPanel) mainPanel.SetActive(true);
        if (difficultyPanel) difficultyPanel.SetActive(false);
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
            PlayerActions.OnCanInteract(true);
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

    private void StartGameplayScene()
    {
        isTransitioningToGameplay = true;
        HideMenu();
        Debug.Log($"[MainMenu] Loading gameplay scene '{gameplaySceneName}'");
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void LoadMainMenuScene()
    {
        isTransitioningToGameplay = false;
        Debug.Log($"[MainMenu] Loading main menu scene '{mainMenuSceneName}'");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameplaySceneName)
        {
            CacheGameplayReferences(scene);
            if (isTransitioningToGameplay)
            {
                HideMenu();
                isTransitioningToGameplay = false;
            }
        }
        else if (scene.name == mainMenuSceneName)
        {
            ShowMenu();
        }
    }

    private void CacheGameplayReferences(Scene scene)
    {
        player = GameObject.FindGameObjectWithTag(playerTag);
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
        if (ddaToggle) ddaToggle.onValueChanged.RemoveListener(OnDDAToggleChanged);
        if (easyButton) easyButton.onClick.RemoveListener(() => OnDifficultySelected(0));
        if (mediumButton) mediumButton.onClick.RemoveListener(() => OnDifficultySelected(50));
        if (hardButton) hardButton.onClick.RemoveListener(() => OnDifficultySelected(100));
        
        // Pause panel
        if (resumeButton) resumeButton.onClick.RemoveListener(OnResumeClicked);
        if (pauseExitButton) pauseExitButton.onClick.RemoveListener(OnExitClicked);
        if (howToPlayButton) howToPlayButton.onClick.RemoveListener(OnHowToPlayClicked);
        
        // Game over panel
        if (retryButton) retryButton.onClick.RemoveListener(OnRetryClicked);
        if (gameOverExitButton) gameOverExitButton.onClick.RemoveListener(OnExitClicked);
        
        // Clear singleton instance
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnHowToPlayClicked()
    {
        Debug.Log("[MainMenu] Opening How to Play");
        if (pausePanel) pausePanel.SetActive(false);
        if (howToPlayPanel) howToPlayPanel.SetActive(true);
    }

    public void OnBackFromHowToPlay()
    {
        Debug.Log("[MainMenu] Returning from How to Play");
        if (pausePanel) pausePanel.SetActive(true);
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
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
        CheckpointManager.Instance?.BeginLoading();
    }

    private void OnCheckpointLoaded()
    {
        if (!isRetrying) return;

        isRetrying = false;
        Debug.Log("[MainMenu] Checkpoint loaded, retry complete");
        GameOverManager.Instance?.ResetGameOver();
    }
}