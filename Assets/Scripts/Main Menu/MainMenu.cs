using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.DDA;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Toggle ddaToggle;
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;

    [Header("Gameplay UI")]
    [SerializeField] private GameObject gameplayCanvas;

    [Header("Settings")]
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private string playerTag = "Player";

    private GameObject player;
    private bool isGamePaused;

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

        // Setup button listeners
        if (playButton) playButton.onClick.AddListener(OnPlayClicked);
        if (creditsButton) creditsButton.onClick.AddListener(OnCreditsClicked);
        if (exitButton) exitButton.onClick.AddListener(OnExitClicked);
        if (ddaToggle) ddaToggle.onValueChanged.AddListener(OnDDAToggleChanged);
        
        // Setup difficulty button listeners
        if (easyButton) easyButton.onClick.AddListener(() => OnDifficultySelected(0));
        if (mediumButton) mediumButton.onClick.AddListener(() => OnDifficultySelected(50));
        if (hardButton) hardButton.onClick.AddListener(() => OnDifficultySelected(100));

        // Setup initial state
        if (showOnStart)
        {
            ShowMenu();
        }
        else
        {
            HideMenu();
        }

        // Initialize DDA toggle state
        if (ddaToggle)
        {
            ddaToggle.isOn = true; // Default to DDA enabled
            OnDDAToggleChanged(ddaToggle.isOn);
        }

        // Hide difficulty panel initially
        if (difficultyPanel)
        {
            difficultyPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Allow ESC key to toggle menu during gameplay
        if (Input.GetKeyDown(KeyCode.Escape) && !isGamePaused)
        {
            ShowMenu();
        }
    }

    public void ShowMenu()
    {
        isGamePaused = true;
        
        // Show the main menu
        if (mainPanel) mainPanel.SetActive(true);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (difficultyPanel) difficultyPanel.SetActive(false);

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

    public void HideMenu()
    {
        isGamePaused = false;

        // Hide all panels
        if (mainPanel) mainPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (difficultyPanel) difficultyPanel.SetActive(false);

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
            HideMenu();
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
        HideMenu();
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

        // Re-enable movement
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
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (playButton) playButton.onClick.RemoveListener(OnPlayClicked);
        if (creditsButton) creditsButton.onClick.RemoveListener(OnCreditsClicked);
        if (exitButton) exitButton.onClick.RemoveListener(OnExitClicked);
        if (ddaToggle) ddaToggle.onValueChanged.RemoveListener(OnDDAToggleChanged);
        if (easyButton) easyButton.onClick.RemoveListener(() => OnDifficultySelected(0));
        if (mediumButton) mediumButton.onClick.RemoveListener(() => OnDifficultySelected(50));
        if (hardButton) hardButton.onClick.RemoveListener(() => OnDifficultySelected(100));
    }
}
