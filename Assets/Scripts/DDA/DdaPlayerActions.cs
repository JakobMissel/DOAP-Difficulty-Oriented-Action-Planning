using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.DDA
{
    public class DdaPlayerActions : MonoBehaviour
    {
        // TODO: Make startmonent be updated to whatever time the tutorial ended
        private float startMoment = 0f;

        // Item usage / Throwables
        private int currentAmmo = 0;
        private List<int> successes = new List<int>();
        private int maxRememberedThrows = 10;
        private int timesCaptured = 0;

        public static DdaPlayerActions Instance;

#if UNITY_EDITOR
        [Header("Testing")]
        [SerializeField] private bool isTestingDdaPlayerActions = false;
        [SerializeField] private GameObject uiVisualisationPrefab;
        [Tooltip("0 = Painting stealing time\n1 = Succesful item usage\n2 = Times captured")] private TMPro.TextMeshProUGUI[] testTextFields;
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
            maxRememberedThrows = DifficultyTracker.GetMaxRemembered(PlayerDAAs.SuccesfulItemUsage);
            Debug.LogWarning($"I am remembering up to {maxRememberedThrows} throws");
        }

#if UNITY_EDITOR
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

        private void DoDdaTest()
        {
            if (isTestingDdaPlayerActions)
            {
                currentSpawnedPrefab = Instantiate(uiVisualisationPrefab, transform);
                Debug.Log($"Spawned with {currentSpawnedPrefab.transform.childCount} children\nThey have {currentSpawnedPrefab.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Length} TMProUGUI components");
                testTextFields = currentSpawnedPrefab.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                testTextFields[0].text = string.Format(baseTestMessage,
                                                       "Painting stealing time",
                                                       DifficultyTracker.GetDifficultyF(PlayerDAAs.TimeBetweenPaintings).ToString("N2"),
                                                       "latest stealing time",
                                                       "N/A");
                testTextFields[1].text = string.Format(baseTestMessage,
                                                       "Succesful item usage",
                                                       DifficultyTracker.GetDifficultyF(PlayerDAAs.SuccesfulItemUsage).ToString("N2"),
                                                       "succesful item ratio",
                                                       successes.Count > 0 ? ((float)successes.Sum() / (float)successes.Count).ToString("N2"): "0");
                testTextFields[2].text = string.Format(baseTestMessage,
                                                       "Captures",
                                                       DifficultyTracker.GetDifficultyF(PlayerDAAs.TimesCaptured).ToString("N2"),
                                                       "times captured",
                                                       timesCaptured.ToString());
                WriteFullDifficulty();
            }
        }
#endif

        private void OnEnable()
        {
            PlayerActions.stealItem += (pickup) => PaintingPickedUp();
            PlayerActions.paintingDelivered += PaintingDelivered;
            PlayerActions.tutorialCompletion += PaintingDelivered;
            PlayerActions.ammoUpdate += UsedItem;
        }

        private void OnDisable()
        {
            PlayerActions.stealItem -= (pickup) => PaintingPickedUp();
            PlayerActions.paintingDelivered -= PaintingDelivered;
            PlayerActions.tutorialCompletion -= PaintingDelivered;
            PlayerActions.ammoUpdate -= UsedItem;
        }

        /// <summary>
        /// To be called whenever the player picks up a painting.
        /// Calculates teh average time it takes to steal a painting and tells the Difficulty Tracker.
        /// </summary>
        private void PaintingPickedUp()
        {
            // Remember the time it took to find this painting
            float paintingStealingLength = Time.time - startMoment;

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
            // Note the time as to keep track of the average painting stealing time
            startMoment = Time.time;
        }

        /// <summary>
        /// To be called whenever an enemy gets succesfully distracted by an item.
        /// Calculates the succesful item usage ratio and tells the Difficulty Tracker.
        /// </summary>
        public void SuccesfulItemUsage()
        {
            // Safety check: ensure there's at least one throw recorded before incrementing
            if (successes.Count == 0)
            {
                Debug.LogWarning("[DdaPlayerActions] SuccesfulItemUsage called but no throws recorded yet. Adding initial entry.");
                successes.Add(0);
            }

            successes[successes.Count - 1]++;
            // Tell the DifficultyTracker the current percentage of succesful item usages
            DifficultyTracker.AlterDifficulty(PlayerDAAs.SuccesfulItemUsage, (float)successes.Sum() / (float)successes.Count);

#if UNITY_EDITOR
            if (isTestingDdaPlayerActions)
            {
                // Update middle difficulty text
                testTextFields[1].text = string.Format(baseTestMessage,
                                                       "Succesful item usage",
                                                       DifficultyTracker.GetDifficultyF(PlayerDAAs.SuccesfulItemUsage).ToString("N2"),
                                                       "succesful item ratio",
                                                       ((float)successes.Sum() / (float)successes.Count).ToString("N2"));
                WriteFullDifficulty();
            }
#endif
        }

        /// <summary>
        /// To be called whenever an item is used.
        /// Calculates the succesful item usage ratio and tells the Difficulty Tracker.
        /// </summary>
        /// <param name="newAmmo">The current amount of ammo the player has. Used to figure out whether an item was used or picked up</param>
        private void UsedItem(int newAmmo)
        {
            // If the new ammo amount is less than the old one, that means that an item was used
            if (currentAmmo > newAmmo)
            {
                // Count up the total items used
                successes.Add(0);

                // Forget oldest if there are more than the max throws
                if (successes.Count > maxRememberedThrows)
                {
                    successes.RemoveAt(0);
                }

                // Tell the DifficultyTracker the current percentage of succesful item usages
                DifficultyTracker.AlterDifficulty(PlayerDAAs.SuccesfulItemUsage, (float)successes.Sum() / (float)successes.Count);

#if UNITY_EDITOR
                if (isTestingDdaPlayerActions)
                {
                    // Update middle difficulty text
                    testTextFields[1].text = string.Format(baseTestMessage,
                                                           "Succesful item usage",
                                                           DifficultyTracker.GetDifficultyF(PlayerDAAs.SuccesfulItemUsage).ToString("N2"),
                                                           "succesful item ratio",
                                                           ((float)successes.Sum() / (float)successes.Count).ToString("N2"));
                    WriteFullDifficulty();
                }
#endif
            }

            // Update the tracked ammo
            currentAmmo = newAmmo;
        }

#if UNITY_EDITOR
        private void WriteFullDifficulty()
        {
            testTextFields[3].text = $"Full difficulty at <b><u>{DifficultyTracker.GetDifficultyF().ToString("N2")}</u></b>";
        }
#endif
    }
}
