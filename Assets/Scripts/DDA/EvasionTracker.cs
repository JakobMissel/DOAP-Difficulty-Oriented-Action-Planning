using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.DDA
{
    /// <summary>
    /// Tracks player evasion times from guards.
    /// Monitors pursuit start (PursuitAction) to successful evasion (ClearLastKnownAction).
    /// Each guard is tracked independently - faster evasions increase difficulty more.
    /// </summary>
    public class EvasionTracker : MonoBehaviour
    {
        public static EvasionTracker Instance { get; private set; }

        // Track pursuit start time per guard (key = guard GameObject instance ID)
        private Dictionary<int, float> pursuitStartTimes = new Dictionary<int, float>();

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

#if UNITY_EDITOR
        [Header("Testing Visualization")]
        [SerializeField] private bool showTestingUI = false;
        [Tooltip("Assign a TextMeshProUGUI component to show live evasion stats during play")]
        [SerializeField] private TMPro.TextMeshProUGUI debugTextField;
        private string baseTestMessage = "Evasion Speed is at difficulty: <u><b>{0}</b></u>\nLast evasion time: <u><b>{1}s</b></u>\nActive pursuits: <u><b>{2}</b></u>";
        private float lastEvasionTime = 0f;
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
                    lastEvasionTime.ToString("N2"),
                    pursuitStartTimes.Count
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

            // Record the time this guard started pursuit
            Instance.pursuitStartTimes[guardId] = Time.time;

            if (Instance.showDebugLogs)
                Debug.Log($"[EvasionTracker] Guard {guardId} started pursuit at {Time.time:F2}s");
        }

        /// <summary>
        /// Call when a guard successfully completes clearing last known position (from ClearLastKnownAction.End())
        /// Calculates evasion time and adjusts difficulty accordingly
        /// </summary>
        /// <param name="guardId">Guard's GameObject instance ID</param>
        public static void EvasionSuccessful(int guardId)
        {
            if (Instance == null)
            {
                Debug.LogError("[EvasionTracker] Instance is null! Make sure EvasionTracker is in the scene.");
                return;
            }

            // Check if this guard was tracking a pursuit
            if (Instance.pursuitStartTimes.TryGetValue(guardId, out float startTime))
            {
                // Calculate how long the player took to evade this guard
                float evasionTime = Time.time - startTime;

                // Tell the difficulty tracker about this evasion
                // Faster evasion (lower time) should increase difficulty more
                DifficultyTracker.AlterDifficulty(PlayerDAAs.PlayerEvasionSpeed, evasionTime);

                // Remove this guard from tracking
                Instance.pursuitStartTimes.Remove(guardId);

#if UNITY_EDITOR
                Instance.lastEvasionTime = evasionTime;
#endif

                if (Instance.showDebugLogs)
                    Debug.Log($"[EvasionTracker] Guard {guardId} evasion successful! Time: {evasionTime:F2}s → Difficulty adjusted");
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

            if (Instance.pursuitStartTimes.Remove(guardId))
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

            Instance.pursuitStartTimes.Clear();

            if (Instance.showDebugLogs)
                Debug.Log("[EvasionTracker] All pursuits reset");
        }

        /// <summary>
        /// Check if a specific guard is currently tracking pursuit
        /// </summary>
        public static bool IsTrackingPursuit(int guardId)
        {
            if (Instance == null)
                return false;

            return Instance.pursuitStartTimes.ContainsKey(guardId);
        }

        /// <summary>
        /// Get the current evasion time for a guard (if being tracked)
        /// </summary>
        public static float GetCurrentEvasionTime(int guardId)
        {
            if (Instance == null || !Instance.pursuitStartTimes.TryGetValue(guardId, out float startTime))
                return -1f;

            return Time.time - startTime;
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
