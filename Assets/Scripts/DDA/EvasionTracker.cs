using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.DDA
{
    /// <summary>
    /// Tracks player evasion count from guards.
    /// Monitors pursuit start (PursuitAction) to successful evasion (ClearLastKnownAction).
    /// Each guard is tracked independently - each successful evasion increases difficulty.
    /// </summary>
    public class EvasionTracker : MonoBehaviour
    {
        public static EvasionTracker Instance { get; private set; }

        // Track which guards are currently pursuing (key = guard GameObject instance ID)
        private HashSet<int> activePursuits = new HashSet<int>();

        // Total evasion count (across all guards)
        private int totalEvasions = 0;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

#if UNITY_EDITOR
        [Header("Testing Visualization")]
        [SerializeField] private bool showTestingUI = false;
        [Tooltip("Assign a TextMeshProUGUI component to show live evasion stats during play")]
        [SerializeField] private TMPro.TextMeshProUGUI debugTextField;
        private string baseTestMessage = "Evasion Count is at difficulty: <u><b>{0}</b></u>\nTotal evasions: <u><b>{1}</b></u>\nActive pursuits: <u><b>{2}</b></u>";
#endif

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[EvasionTracker] Multiple instances detected, destroying duplicate.");
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (showDebugLogs)
                Debug.Log("[EvasionTracker] Initialized successfully.");
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (showTestingUI && debugTextField != null)
            {
                debugTextField.text = string.Format(
                    baseTestMessage,
                    DifficultyTracker.GetDifficultyF(PlayerDAAs.PlayerEvasionSpeed).ToString("N2"),
                    totalEvasions,
                    activePursuits.Count
                );
            }
        }
#endif

        /// <summary>
        /// Call when a guard starts pursuing the player (from PursuitAction.Start())
        /// </summary>
        /// <param name="guardId">Guard's GameObject instance ID</param>
        public static void StartPursuit(int guardId)
        {
            if (Instance == null)
            {
                Debug.LogError("[EvasionTracker] Instance is null! Make sure EvasionTracker is in the scene.");
                return;
            }

            // Mark this guard as actively pursuing
            Instance.activePursuits.Add(guardId);

            if (Instance.showDebugLogs)
                Debug.Log($"[EvasionTracker] Guard {guardId} started pursuit (total active pursuits: {Instance.activePursuits.Count})");
        }

        /// <summary>
        /// Call when a guard successfully completes clearing last known position (from ClearLastKnownAction.End())
        /// Increments evasion count and adjusts difficulty accordingly
        /// </summary>
        /// <param name="guardId">Guard's GameObject instance ID</param>
        public static void EvasionSuccessful(int guardId)
        {
            if (Instance == null)
            {
                Debug.LogError("[EvasionTracker] Instance is null! Make sure EvasionTracker is in the scene.");
                return;
            }

            // Check if this guard was actively pursuing
            if (Instance.activePursuits.Remove(guardId))
            {
                // Increment total evasion count
                Instance.totalEvasions++;

                // Tell the difficulty tracker about this evasion
                // Pass the total evasion COUNT (not time)
                DifficultyTracker.AlterDifficulty(PlayerDAAs.PlayerEvasionSpeed, Instance.totalEvasions);

                if (Instance.showDebugLogs)
                    Debug.Log($"[EvasionTracker] Guard {guardId} evasion successful! Total evasions: {Instance.totalEvasions} → Difficulty adjusted");
            }
            else
            {
                if (Instance.showDebugLogs)
                    Debug.LogWarning($"[EvasionTracker] Guard {guardId} called EvasionSuccessful but wasn't tracking pursuit (may have been reset by capture)");
            }
        }

        /// <summary>
        /// Call when pursuit should be reset without counting as an evasion
        /// (e.g., player was captured, guard lost player for other reasons)
        /// </summary>
        /// <param name="guardId">Guard's GameObject instance ID</param>
        public static void ResetPursuit(int guardId)
        {
            if (Instance == null)
            {
                Debug.LogError("[EvasionTracker] Instance is null! Make sure EvasionTracker is in the scene.");
                return;
            }

            if (Instance.activePursuits.Remove(guardId))
            {
                if (Instance.showDebugLogs)
                    Debug.Log($"[EvasionTracker] Guard {guardId} pursuit reset (likely player captured)");
            }
        }

        /// <summary>
        /// Clear all pursuit tracking (useful for level resets, etc.)
        /// </summary>
        public static void ResetAllPursuits()
        {
            if (Instance == null)
                return;

            Instance.activePursuits.Clear();

            if (Instance.showDebugLogs)
                Debug.Log("[EvasionTracker] All pursuits reset");
        }

        /// <summary>
        /// Reset the total evasion count (useful for level resets)
        /// </summary>
        public static void ResetEvasionCount()
        {
            if (Instance == null)
                return;

            Instance.totalEvasions = 0;

            if (Instance.showDebugLogs)
                Debug.Log("[EvasionTracker] Evasion count reset to 0");
        }

        /// <summary>
        /// Check if a specific guard is currently tracking pursuit
        /// </summary>
        public static bool IsTrackingPursuit(int guardId)
        {
            if (Instance == null)
                return false;

            return Instance.activePursuits.Contains(guardId);
        }

        /// <summary>
        /// Get the current total evasion count
        /// </summary>
        public static int GetTotalEvasions()
        {
            if (Instance == null)
                return 0;

            return Instance.totalEvasions;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) return;

            // Auto-find debug text field if needed
            if (showTestingUI && debugTextField == null)
            {
                debugTextField = GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (debugTextField != null)
                    Debug.Log("[EvasionTracker] Auto-found debug text field");
            }
        }
#endif
    }
}
