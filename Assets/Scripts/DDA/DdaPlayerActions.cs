using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.DDA
{
    public class DdaPlayerActions : MonoBehaviour
    {
        // TODO: Make startmonent be updated to whatever time the tutorial ended
        private float startMoment = 0f;
        private bool tutorialEnded = false;

        // Item usage / Throwables - DEPRECATED: Now using EvasionTracker for difficulty tracking
        // private int currentAmmo = 0;
        // private List<int> successes = new List<int>();
        // private int maxRememberedThrows = 10;
        private int timesCaptured = 0;
        
        // Track latest painting stealing time for UI display
        private float lastPaintingStealingTime = -1f;

        public static DdaPlayerActions Instance;

#if UNITY_EDITOR
        [Header("Testing")]
        [SerializeField] private bool isTestingDdaPlayerActions = false;
        [SerializeField] private GameObject uiVisualisationPrefab;
        [Tooltip("0 = Painting stealing time\n1 = Times Evaded\n2 = Times captured")] private TMPro.TextMeshProUGUI[] testTextFields;
        private string baseTestMessage = "{0} is at difficulty: <u><b>{1}</b></u>, with {2} at <u><b>{3}</b></u>";
        private GameObject currentSpawnedPrefab = null;
#endif

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            // DEPRECATED: Now using EvasionTracker instead of SuccessfulItemUsage
            // maxRememberedThrows = DifficultyTracker.GetMaxRemembered(PlayerDAAs.SuccesfulItemUsage);
            // Debug.LogWarning($"I am remembering up to {maxRememberedThrows} throws");
        }

#if UNITY_EDITOR
        private void Update()
        {
            // Continuously update the DDA test UI if it's active
            if (isTestingDdaPlayerActions && testTextFields != null && testTextFields.Length >= 4)
            {
                UpdateDdaTestUI();
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) return;

            Debug.Log($"{(isTestingDdaPlayerActions ? "Creat" : "Destroy")}ing {(currentSpawnedPrefab == null ? "prefab instance" : currentSpawnedPrefab)}");

            if (isTestingDdaPlayerActions && currentSpawnedPrefab == null)
            {
                DoDdaTest();
            }
            else if (!isTestingDdaPlayerActions && currentSpawnedPrefab != null)
            {
                Destroy(currentSpawnedPrefab);
            }
        }

        private void UpdateDdaTestUI()
        {
            try
            {
                // Update painting stealing time
                float paintingDiff = DifficultyTracker.GetDifficultyF(PlayerDAAs.TimeBetweenPaintings);
                string paintingTimeDisplay = lastPaintingStealingTime < 0 ? "N/A" : lastPaintingStealingTime.ToString("N2");
                testTextFields[0].text = string.Format(baseTestMessage,
                                                       "Painting stealing time",
                                                       float.IsNaN(paintingDiff) ? "NaN" : paintingDiff.ToString("N2"),
                                                       "latest stealing time",
                                                       paintingTimeDisplay);
                
                // Update times evaded
                float evadedDiff = DifficultyTracker.GetDifficultyF(PlayerDAAs.TimesEvaded);
                int totalEvasions = EvasionTracker.GetTotalEvasions();
                
                // Debug log every 60 frames to track if difficulty is updating
                if (Time.frameCount % 60 == 0 && totalEvasions > 0)
                {
                    Debug.Log($"[DdaPlayerActions] Times Evaded: {totalEvasions} evasions, difficulty: {evadedDiff:F2}");
                }
                
                testTextFields[1].text = string.Format(baseTestMessage,
                                                       "Times Evaded",
                                                       float.IsNaN(evadedDiff) ? "NaN" : evadedDiff.ToString("N2"),
                                                       "total evasions",
                                                       totalEvasions.ToString());
                
                // Update captures
                float capturesDiff = DifficultyTracker.GetDifficultyF(PlayerDAAs.TimesCaptured);
                testTextFields[2].text = string.Format(baseTestMessage,
                                                       "Captures",
                                                       float.IsNaN(capturesDiff) ? "NaN" : capturesDiff.ToString("N2"),
                                                       "times captured",
                                                       timesCaptured.ToString());
                
                // Update full difficulty
                WriteFullDifficulty();
                
                // Debug log if any values are NaN
                if (float.IsNaN(paintingDiff))
                    Debug.LogError("[DdaPlayerActions] TimeBetweenPaintings difficulty is NaN!");
                if (float.IsNaN(evadedDiff))
                    Debug.LogError("[DdaPlayerActions] TimesEvaded difficulty is NaN!");
                if (float.IsNaN(capturesDiff))
                    Debug.LogError("[DdaPlayerActions] TimesCaptured difficulty is NaN!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DdaPlayerActions] Error updating DDA UI: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void DoDdaTest()
        {
            if (isTestingDdaPlayerActions)
            {
                currentSpawnedPrefab = Instantiate(uiVisualisationPrefab, transform);
                testTextFields = currentSpawnedPrefab.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                
                Debug.Log($"[DdaPlayerActions] Spawned UI with {testTextFields.Length} TextMeshProUGUI components");
                
                // Log each text field for debugging
                for (int i = 0; i < testTextFields.Length; i++)
                {
                    Debug.Log($"[DdaPlayerActions] TextField[{i}]: '{testTextFields[i].text}' on {testTextFields[i].gameObject.name}");
                }
                
                // Ensure we have at least 4 text fields
                if (testTextFields.Length < 4)
                {
                    Debug.LogError($"[DdaPlayerActions] UI prefab only has {testTextFields.Length} text fields, but we need 4! Add more TextMeshProUGUI components to the prefab.");
                    return;
                }
                
                // Clear any hardcoded text and set initial values
                UpdateDdaTestUI();
            }
        }
#endif

        private void OnEnable()
        {
            PlayerActions.tutorialCompletion += TutorialDone;
            PlayerActions.stealItem += PaintingPickedUp;
            PlayerActions.paintingDelivered += PaintingDelivered;
            PlayerActions.playerCaught += PlayerGotCaught;
            //PlayerActions.ammoUpdate += UsedItem;
        }

        private void OnDisable()
        {
            PlayerActions.tutorialCompletion -= TutorialDone;
            PlayerActions.stealItem -= PaintingPickedUp;
            PlayerActions.paintingDelivered -= PaintingDelivered;
            PlayerActions.playerCaught -= PlayerGotCaught;
            //PlayerActions.ammoUpdate -= UsedItem;
        }

        private void TutorialDone()
        {
            tutorialEnded = true;
            PaintingDelivered();
        }

        private void PlayerGotCaught()
        {
            timesCaptured++;
            DifficultyTracker.AlterDifficulty(PlayerDAAs.TimesCaptured, timesCaptured);
#if UNITY_EDITOR
            if (isTestingDdaPlayerActions)
            {
                testTextFields[2].text = string.Format(baseTestMessage,
                                                   "Captures",
                                                   DifficultyTracker.GetDifficultyF(PlayerDAAs.TimesCaptured).ToString("N2"),
                                                   "times captured",
                                                   timesCaptured.ToString());
                WriteFullDifficulty();
            }
#endif
        }

        /// <summary>
        /// To be called whenever the player picks up a painting.
        /// Calculates teh average time it takes to steal a painting and tells the Difficulty Tracker.
        /// </summary>
        private void PaintingPickedUp(StealablePickup painting, bool isNew)
        {
            if (!isNew)
            {
                Debug.Log("[DdaPlayerActions] PaintingPickedUp called but painting is not new, ignoring difficulty adjustment.");
                return;
            }
            // If tutorial isn't done, don't alter difficulty
            if (!tutorialEnded)
                return;

            // Remember the time it took to find this painting
            float paintingStealingLength = Time.time - startMoment;
            
            // Store for UI display
            lastPaintingStealingTime = paintingStealingLength;

            // Remember this time so that next painting stealing length can be the length of time it between this one and that one
            startMoment = Time.time;

            // Tell the difficulty tracker the average time it took to steal the current painting
            DifficultyTracker.AlterDifficulty(PlayerDAAs.TimeBetweenPaintings, paintingStealingLength);

#if UNITY_EDITOR
            if (isTestingDdaPlayerActions)
            {
                // Update top difficulty text
                testTextFields[0].text = string.Format(baseTestMessage,
                                                       "Painting stealing time",
                                                       DifficultyTracker.GetDifficultyF(PlayerDAAs.TimeBetweenPaintings).ToString("N2"),
                                                       "latest stealing time",
                                                       paintingStealingLength.ToString("N2"));
                WriteFullDifficulty();
            }
#endif
        }

        /// <summary>
        /// To be called whenever a painting has been delivered.
        /// Remembers what time the painting was delivered, to use for calculating the average painting stealing time.
        /// </summary>
        private void PaintingDelivered()
        {
            // If tutorial isn't done, don't alter difficulty
            if (!tutorialEnded)
                return;

            // Note the time as to keep track of the average painting stealing time
            startMoment = Time.time;
        }

        /// <summary>
        /// DEPRECATED: This method is no longer used for difficulty tracking.
        /// Difficulty is now tracked via EvasionTracker (TimesEvaded) instead of item usage.
        /// Kept for backward compatibility but does nothing.
        /// </summary>
        [System.Obsolete("Use EvasionTracker for difficulty tracking instead")]
        public void SuccesfulItemUsage()
        {
            // DEPRECATED: Now using EvasionTracker (TimesEvaded) instead
            // This method is kept for backward compatibility but no longer tracks difficulty
            Debug.LogWarning("[DdaPlayerActions] SuccessfulItemUsage is deprecated. Use EvasionTracker instead.");
        }

        /// <summary>
        /// DEPRECATED: This method is no longer used for difficulty tracking.
        /// Difficulty is now tracked via EvasionTracker (TimesEvaded) instead of item usage.
        /// </summary>
        [System.Obsolete("Use EvasionTracker for difficulty tracking instead")]
        private void UsedItem(int newAmmo)
        {
            // DEPRECATED: Now using EvasionTracker (TimesEvaded) instead
            // This method is kept for backward compatibility but no longer tracks difficulty
        }

#if UNITY_EDITOR
        private void WriteFullDifficulty()
        {
            float difficulty = DifficultyTracker.GetDifficultyF();
            
            // Handle NaN or invalid difficulty values
            if (float.IsNaN(difficulty) || float.IsInfinity(difficulty))
            {
                testTextFields[3].text = $"Full difficulty at <b><u>ERROR (NaN)</u></b>";
                Debug.LogError("[DdaPlayerActions] Full difficulty is NaN! Check PlayerDifficultyEffects configuration - you may need to remove SuccessfulItemUsage from the asset.");
            }
            else
            {
                testTextFields[3].text = $"Full difficulty at <b><u>{difficulty.ToString("N2")}</u></b>";
            }
        }
#endif

        public void ResetRunData()
        {
            startMoment = 0f;
            tutorialEnded = false;
            timesCaptured = 0;
            lastPaintingStealingTime = -1f;
#if UNITY_EDITOR
            if (isTestingDdaPlayerActions)
            {
                UpdateDdaTestUI();
            }
#endif
        }
    }
}
