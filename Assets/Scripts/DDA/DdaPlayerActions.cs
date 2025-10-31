using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.DDA
{
    public class DdaPlayerActions : MonoBehaviour
    {
        // Paintings
        private Dictionary<string, float> paintingStealingLength = new Dictionary<string, float>();
        private List<float> paintingStealingLengths = new List<float>();
        private float startMoment = 0f;

        // Item usage / Throwables
        private int currentAmmo = 0;
        private int itemSuccesses = 0;
        private int totalItemsUsed = 0;

        public static DdaPlayerActions Instance;

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
            float averageStealingTime = paintingStealingLengths.Sum() / paintingStealingLengths.Count;

            // Tell the difficulty tracker the average time it takes to steal a painting
            DifficultyTracker.AlterDifficulty(PlayerDAAs.TimeBetweenPaintings, averageStealingTime);
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
            }

            // Update the tracked ammo
            currentAmmo = newAmmo;
        }
    }
}
