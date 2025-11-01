using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.DDA
{
    public class DdaPlayerActions : MonoBehaviour
    {
        // Paintings
        private List<float> paintingStealingLengths = new List<float>();
        private float startMoment = 0f;

        // Item usage / Throwables
        private int currentAmmo = 0;
        private int itemSuccesses = 0;
        private int totalItemsUsed = 0;
        private int timesCaptured = 0;

        public static DdaPlayerActions Instance;

#if UNITY_EDITOR
        [Header("Testing")]
        [SerializeField] private bool isTestingDdaPlayerActions = false;
        [SerializeField] private GameObject uiVisualisationPrefab;
        [Tooltip("0 = Painting stealing time\n1 = Succesful item usage\n2 = Times captured")] private TMPro.TextMeshProUGUI[] testTextFields;
        private string baseTestMessage = "{0} is at difficulty: <u><b>{1}</b></u>, with {2} at <u><b>{3}</b></u>";
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
#if UNITY_EDITOR
            if (isTestingDdaPlayerActions)
            {
                GameObject spawnedPrefab = Instantiate(uiVisualisationPrefab, transform);
                Debug.Log($"Spawned with {spawnedPrefab.transform.childCount} children\nThey have {spawnedPrefab.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Length} TMProUGUI components");
                testTextFields = spawnedPrefab.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                testTextFields[0].text = string.Format(baseTestMessage,
                                                       "Painting stealing time",
                                                       DifficultyTracker.GetDifficultyF(PlayerDAAs.TimeBetweenPaintings).ToString("N2"),
                                                       "average stealing time",
                                                       paintingStealingLengths.Count > 0 ? paintingStealingLengths.Average().ToString("N2") : "N/A");
                testTextFields[1].text = string.Format(baseTestMessage,
                                                       "Succesful item usage",
                                                       DifficultyTracker.GetDifficultyF(PlayerDAAs.SuccesfulItemUsage).ToString("N2"),
                                                       "succesful item ratio",
                                                       totalItemsUsed > 0 ? ((float)itemSuccesses / (float)totalItemsUsed).ToString("N2"): "0");
                testTextFields[2].text = string.Format(baseTestMessage,
                                                       "Captures",
                                                       DifficultyTracker.GetDifficultyF(PlayerDAAs.TimesCaptured).ToString("N2"),
                                                       "times captured",
                                                       timesCaptured.ToString());
            }
#endif
        }

        private void OnEnable()
        {
            PlayerActions.stealItem += (pickup) => PaintingPickedUp();
            PlayerActions.paintingDelivered += PaintingStolen;
            PlayerActions.ammoUpdate += UsedItem;
        }

        private void OnDisable()
        {
            PlayerActions.stealItem -= (pickup) => PaintingPickedUp();
            PlayerActions.paintingDelivered -= PaintingStolen;
            PlayerActions.ammoUpdate -= UsedItem;
        }

        /// <summary>
        /// To be called whenever the player picks up a painting
        /// </summary>
        private void PaintingPickedUp()
        {
            // Remember the time it took to find this painting (In case we want only the last X paintings to affect difficulty)
            paintingStealingLengths.Add(Time.time - startMoment);

            // Remember this time so that next painting stealing length can be the length of time it between this one and that one
            startMoment = Time.time;

            // Get the average stealing time (sum of stealing times / amount of paintings stolen)
            float averageStealingTime = paintingStealingLengths.Average();

            // Tell the difficulty tracker the average time it takes to steal a painting
            DifficultyTracker.AlterDifficulty(PlayerDAAs.TimeBetweenPaintings, averageStealingTime);

#if UNITY_EDITOR
            // Update top difficulty text
            testTextFields[0].text = string.Format(baseTestMessage,
                                                   "Painting stealing time",
                                                   DifficultyTracker.GetDifficultyF(PlayerDAAs.TimeBetweenPaintings).ToString("N2"),
                                                   "average stealing time",
                                                   paintingStealingLengths.Average().ToString("N2"));
#endif
        }

        /// <summary>
        /// To be called whenever a painting has been delivered
        /// </summary>
        private void PaintingStolen()
        {
            // Note the time as to keep track of the average painting stealing time
            startMoment = Time.time;
        }

        /// <summary>
        /// To be called whenever an enemy gets succesfully distracted by an item
        /// </summary>
        public void SuccesfulItemUsage()
        {
            itemSuccesses++;
            // Tell the DifficultyTracker the current percentage of succesful item usages
            DifficultyTracker.AlterDifficulty(PlayerDAAs.SuccesfulItemUsage, (float)itemSuccesses / (float)totalItemsUsed);

#if UNITY_EDITOR
            // Update middle difficulty text
            testTextFields[1].text = string.Format(baseTestMessage,
                                                   "Succesful item usage",
                                                   DifficultyTracker.GetDifficultyF(PlayerDAAs.SuccesfulItemUsage).ToString("N2"),
                                                   "succesful item ratio",
                                                   ((float)itemSuccesses / (float)totalItemsUsed).ToString("N2"));
#endif
        }

        private void UsedItem(int newAmmo)
        {
            // If the new ammo amount is less than the old one, that means that an item was used
            if (currentAmmo > newAmmo)
            {
                // Count up the total items used
                totalItemsUsed++;
                // Tell the DifficultyTracker the current percentage of succesful item usages
                DifficultyTracker.AlterDifficulty(PlayerDAAs.SuccesfulItemUsage, (float)itemSuccesses/(float)totalItemsUsed);

#if UNITY_EDITOR
                // Update middle difficulty text
                testTextFields[1].text = string.Format(baseTestMessage,
                                                       "Succesful item usage",
                                                       DifficultyTracker.GetDifficultyF(PlayerDAAs.SuccesfulItemUsage).ToString("N2"),
                                                       "succesful item ratio",
                                                       ((float)itemSuccesses / (float)totalItemsUsed).ToString("N2"));
#endif
            }

            // Update the tracked ammo
            currentAmmo = newAmmo;
        }
    }
}
