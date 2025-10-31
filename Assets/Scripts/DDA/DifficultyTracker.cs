using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.DDA
{
    /// <summary>
    /// Singleton to keep track of what difficulty the game is at
    /// </summary>
    public static class DifficultyTracker
    {
        [Tooltip("PlayerDifficultyEffects")] private static PlayerDifficultyEffects pde;
        [Tooltip("EnemyDifficultyEffects")]  private static EnemyDifficultyEffects ede;

        private static bool hasBeenCalled = false;

        private static List<float> actualDifficulties = new List<float>();
        private static List<float> effectiveDifficulties = new List<float>();

        // Testing-mode override
        private static bool testingMode = false;
        private static float testingDifficulty01 = 0f; // 0..1
        private static readonly int[] testingSteps = new[] { 0, 25, 50, 75, 100 };

        /// <summary>
        /// If this is the first time this is called, get the Player and Enemy Difficulty Effects
        /// </summary>
        private static void CalledNow()
        {
            if (hasBeenCalled) return;

            hasBeenCalled = true;

            pde = Resources.Load<PlayerDifficultyEffects>("DDA/PlayerDifficultyEffects");
            ede = Resources.Load<EnemyDifficultyEffects>("DDA/EnemyDifficultyEffects");

            // Assign pde values to base values
            for (int i = 0; i < pde.playerActions.Count; i++)
            {
                actualDifficulties.Add(pde.playerActions[i].startDifficulty);
            }

            // Effective difficulties should be equal to the starting difficulties
            PutDifficultyIntoEffect();
        }

        /// <summary>
        /// Gets the difficulty translation of a player action
        /// </summary>
        /// <param name="actionToTranslate"></param>
        /// <returns>A number to be summed with the other DifficultyTranslationPlayer actions</returns>
        public static float DifficultyTranslation(PlayerDAAs actionToTranslate)
        {
            CalledNow();

            float difficultyTranslation = 0f;
            for (int i = 0; i < pde.playerActions.Count; i++)
            {
                if (pde.playerActions[i].action != actionToTranslate) continue;

                difficultyTranslation = pde.playerActions[i].curve.Evaluate(GetDifficultyF());

                break;
            }
            return difficultyTranslation;
        }

        /// <summary>
        /// Gets the difficulty translation of an enemy action
        /// </summary>
        /// <param name="actionToTranslate">Which enemy action do you want to evaluate</param>
        /// <returns>A number to be multiplied with the relevant enemy action</returns>
        public static float DifficultyTranslation(EnemyActions actionToTranslate)
        {
            CalledNow();

            float difficultyTranslation = 0f;
            for (int i = 0; i < ede.enemyActions.Count; i++)
            {
                if (ede.enemyActions[i].action != actionToTranslate) continue;

                difficultyTranslation = ede.enemyActions[i].curve.Evaluate(GetDifficultyF());

                break;
            }
            return difficultyTranslation;
        }

        /// <summary>
        /// To be called whenever the player does an action that affects the difficulty. Updates the effective difficulty of that action.
        /// </summary>
        /// <param name="actionToAdjust"></param>
        /// <param name="actionInputValue">Eg average seconds since last painting was picked up</param>
        public static void AlterDifficulty(PlayerDAAs actionToAdjust, float actionInputValue)
        {
            CalledNow();

            // Goes through all playerDAAs
            for (int i = 0; i < pde.playerActions.Count; i++)
            {
                // Skips non-selected ones
                if (pde.playerActions[i].action != actionToAdjust) continue;

                // Evaluates the effective difficulty based on the updates info about the player action
                actualDifficulties[i] = pde.playerActions[i].curve.Evaluate(actionInputValue);

                break;
            }

            // Put the new difficulties into effect
            PutDifficultyIntoEffect();
        }

        /// <summary>
        /// Gets the current difficulty as a float between 0 and 1
        /// </summary>
        /// <returns></returns>
        public static float GetDifficultyF()
        {
            CalledNow();

            // In testing mode, return the override directly
            if (testingMode)
                return Mathf.Clamp01(testingDifficulty01);

            float summedEffectiveDifficulty = 0f;
            
            // Sum and clamp01 all effective difficulties
            for (int i = 0; i < effectiveDifficulties.Count; i++)
            {
                summedEffectiveDifficulty += effectiveDifficulties[i];
            }

            summedEffectiveDifficulty = Mathf.Clamp01(summedEffectiveDifficulty);

            return summedEffectiveDifficulty;
        }

        /// <summary>
        /// Gets the current difficulty as an int between 0 and 100
        /// </summary>
        /// <returns></returns>
        public static int GetDifficultyI()
        {
            CalledNow();

            int difficulty = Mathf.RoundToInt(GetDifficultyF() * 100);
            return difficulty;
        }

        /// <summary>
        /// Put the current difficulty into effect
        /// </summary>
        public static void PutDifficultyIntoEffect()
        {
            // If testing mode is active, just log and update costs using the override via DifficultyTranslation
            if (testingMode)
            {
#if UNITY_EDITOR
                Debug.Log($"[DDA] Testing mode difficulty set to {Mathf.RoundToInt(testingDifficulty01 * 100)}% (override)");
#endif
                // Apply costs against the testing value
                for (int i = 0; i < ede.enemyActions.Count; i++)
                {
                    if (ede.enemyActions[i].capability == null)
                        continue;
                    Debug.Log($"Applying cost change of {ede.enemyActions[i].action}, on capability {ede.enemyActions[i].capability}.\nThe cost is being set to {DifficultyTranslation(ede.enemyActions[i].action)}, given the difficulty {GetDifficultyF()}");
                    ede.enemyActions[i].capability.actions[0].baseCost = DifficultyTranslation(ede.enemyActions[i].action);
                }
#if UNITY_EDITOR
                Debug.Log($"Difficulty set to {GetDifficultyF().ToString("n2")}");
#endif
                return;
            }

            // Update effective difficulties to match the actual difficulties
            effectiveDifficulties = new List<float>(actualDifficulties);

            // Make costs of GOAP actions reflect the difficulties
            for (int i = 0; i < ede.enemyActions.Count; i++)
            {
                if (ede.enemyActions[i].capability == null)
                    continue;
                Debug.Log($"Applying cost change of {ede.enemyActions[i].action}, on capability {ede.enemyActions[i].capability}.\nThe cost is being set to {DifficultyTranslation(ede.enemyActions[i].action)}, given the difficulty {GetDifficultyF()}");
                ede.enemyActions[i].capability.actions[0].baseCost = DifficultyTranslation(ede.enemyActions[i].action);
            }
#if UNITY_EDITOR
            Debug.Log($"Difficulty set to {GetDifficultyF().ToString("n2")}");
#endif
        }

        // --- Testing mode---
        public static void EnableTestingMode(bool enabled)
        {
            CalledNow();
            testingMode = enabled;
            // Re-apply costs using current difficulty source
            PutDifficultyIntoEffect();
        }

        public static bool IsTestingMode() => testingMode;

        public static void SetTestingDifficulty01(float value01)
        {
            CalledNow();
            testingMode = true;
            testingDifficulty01 = Mathf.Clamp01(value01);
            PutDifficultyIntoEffect();
        }

        public static void SetTestingDifficultyPercent(int percent)
        {
            SetTestingDifficulty01(Mathf.Clamp01(percent / 100f));
        }

        public static int StepTestingDifficulty(int direction)
        {
            CalledNow();
            testingMode = true;

            int currentPercent = Mathf.RoundToInt(GetDifficultyF() * 100f);
            int closestIdx = 0;
            int best = int.MaxValue;
            for (int i = 0; i < testingSteps.Length; i++)
            {
                int d = Mathf.Abs(testingSteps[i] - currentPercent);
                if (d < best)
                {
                    best = d;
                    closestIdx = i;
                }
            }

            int newIdx = Mathf.Clamp(closestIdx + (direction >= 0 ? 1 : -1), 0, testingSteps.Length - 1);
            int newPercent = testingSteps[newIdx];
            SetTestingDifficultyPercent(newPercent);
            return newPercent;
        }
    }
}