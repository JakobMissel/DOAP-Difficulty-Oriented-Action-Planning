using Assets.Scripts.DDA;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.DDA
{
    public class DdaPlayerActions : MonoBehaviour
    {
        // Paintings
        private Dictionary<string, float> paintingStealingLength = new Dictionary<string, float>();
        private float startMoment = 0f;

        // Item usage / Throwables
        private int currentAmmo = 0;
        [HideInInspector, Tooltip("To be updated whenever an enemy gets distracted by a used item")] public int itemSuccesses = 0;
        private int totalItemsUsed = 0;

        private void OnEnable()
        {
            PlayerActions.addPickup += PaintingPickedUp;
            PlayerActions.ammoUpdate += UsedItem;
        }

        private void OnDisable()
        {
            PlayerActions.addPickup -= PaintingPickedUp;
            PlayerActions.ammoUpdate -= UsedItem;
        }

        private void PaintingPickedUp(Pickup pickup)
        {
            // TEMP If this is the first painting stolen, assume that this is the end of the tutorial and start tracking time
            if (paintingStealingLength.Count == 0)
            {
                startMoment = Time.time;
            }

            // If this is a painting that was already picked up, don't do anything
            if (paintingStealingLength.ContainsKey(pickup.name))
            {
                return;
            }

            // Remember the painting and pickup-time
            paintingStealingLength.Add(pickup.name, Time.time - startMoment);

            // Remember this time so that next painting stealing length can be the length of time it between this one and that one
            startMoment = Time.time;

            float totalStealingTime = 0f;

            foreach (float stealingMoment in paintingStealingLength.Values)
            {
                totalStealingTime += stealingMoment;
            }

            // Tell the average painting stealing time to the difficulty tracker
            DifficultyTracker.AlterDifficulty(PlayerDAAs.TimeBetweenPaintings, totalStealingTime / paintingStealingLength.Count);
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
