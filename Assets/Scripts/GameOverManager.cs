using UnityEngine;
using Assets.Scripts.GOAP.Behaviours;
using System.Linq;

/// <summary>
/// Continuously monitors all guards to check if the player has been caught.
/// When any guard catches the player, triggers the game over sequence.
/// This decouples the game over logic from the GOAP action system.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool debugMode = true;
    
    private bool isGameOver = false;
    private bool hasTriggeredGameOver = false;

    private void Update()
    {
        // Skip if game is already over
        if (hasTriggeredGameOver)
            return;

        // Check all active guards to see if any have caught the player
        CheckForPlayerCaught();
    }

    private void CheckForPlayerCaught()
    {
        // Get all active BrainBehaviour instances (all guards)
        var allGuards = BrainBehaviour.GetActiveBrains();
        
        if (allGuards == null || !allGuards.Any())
            return;

        // Check if any guard has caught the player
        foreach (var guard in allGuards)
        {
            if (guard != null && guard.IsPlayerCaught)
            {
                if (debugMode)
                {
                    Debug.Log($"[GameOverManager] Guard '{guard.name}' has caught the player! Triggering game over...");
                }

                TriggerGameOver();
                return; // Exit once we've triggered game over
            }
        }
    }

    private void TriggerGameOver()
    {
        if (hasTriggeredGameOver)
            return;

        hasTriggeredGameOver = true;
        isGameOver = true;

        if (debugMode)
        {
            Debug.Log("[GameOverManager] Game Over sequence initiated!");
        }

        // Block player input globally
        PlayerActions.OnGameOverState(true);

        // Disable player movement via the event system
        PlayerActions.OnPlayerCaught();

        // Show the game over menu
        if (MainMenu.Instance != null)
        {
            MainMenu.Instance.ShowGameOverMenu();
            
            if (debugMode)
            {
                Debug.Log("[GameOverManager] Game Over menu shown successfully!");
            }
        }
        else
        {
            Debug.LogError("[GameOverManager] MainMenu instance not found! Cannot show game over screen.");
        }
    }

    /// <summary>
    /// Public method to manually trigger game over (can be called from other systems if needed)
    /// </summary>
    public void ForceGameOver()
    {
        if (debugMode)
        {
            Debug.Log("[GameOverManager] Game Over manually forced!");
        }
        
        TriggerGameOver();
    }

    /// <summary>
    /// Reset the game over state (useful for restarting the game)
    /// </summary>
    public void ResetGameOver()
    {
        hasTriggeredGameOver = false;
        isGameOver = false;
        
        if (debugMode)
        {
            Debug.Log("[GameOverManager] Game Over state reset.");
        }

        PlayerActions.OnGameOverState(false);
    }

    // Singleton pattern for easy access
    public static GameOverManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
