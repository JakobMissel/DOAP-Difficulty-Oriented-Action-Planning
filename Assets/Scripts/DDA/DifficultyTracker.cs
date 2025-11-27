using Assets.Scripts.Logging;
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

        [Tooltip("Unweighted difficulties of player actions and lists that carry last X player actions")] private static List<float>[] unweightedPlayerActionDifficulties;

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

            // Create an array sized to accommodate all enum values (not just configured ones)
            // Find the maximum enum value to determine array size
            int maxEnumValue = 0;
            foreach (var action in System.Enum.GetValues(typeof(PlayerDAAs)))
            {
                int enumValue = (int)action;
                Debug.Log($"[DifficultyTracker] Enum {action} = {enumValue}");
                if (enumValue > maxEnumValue)
                    maxEnumValue = enumValue;
            }
            
            Debug.Log($"[DifficultyTracker] Max enum value: {maxEnumValue}, creating array of size {maxEnumValue + 1}");
            unweightedPlayerActionDifficulties = new List<float>[maxEnumValue + 1];

            // Assign pde values to base values
            Debug.Log($"[DifficultyTracker] PlayerDifficultyEffects has {pde.playerActions.Count} actions configured");
            
            for (int i = 0; i < pde.playerActions.Count; i++)
            {
                PlayerDAAs action = pde.playerActions[i].action;
                int actionIndex = (int)action;
                float startDiff = pde.playerActions[i].startDifficulty;
                
                Debug.Log($"[DifficultyTracker] Configuring action {i}: {action} (enum value {actionIndex}), startDifficulty: {startDiff}");
                
                // Safety check
                if (actionIndex < 0 || actionIndex >= unweightedPlayerActionDifficulties.Length)
                {
                    Debug.LogError($"[DifficultyTracker] ERROR: Action {action} has enum value {actionIndex} which is outside array bounds [0-{unweightedPlayerActionDifficulties.Length - 1}]! Skipping this action.");
                    continue;
                }
                
                actualDifficulties.Add(startDiff);
                // Add this actions base difficulty
                unweightedPlayerActionDifficulties[actionIndex] = new List<float>() { startDiff };
            }

            // Effective difficulties should be equal to the starting difficulties
            PutDifficultyIntoEffect();
        }

        private static void RememberUnweightedAction(PlayerDAAs action, float unweightedDifficulty, int maxLength)
        {
            CalledNow();

            // Remember this action difficulty or whatever
            int specificAction = (int)action;

            // Safety check: ensure the action index is within array bounds
            if (specificAction < 0 || specificAction >= unweightedPlayerActionDifficulties.Length)
            {
                Debug.LogError($"[DifficultyTracker] PlayerDAAs.{action} (index {specificAction}) is not configured in PlayerDifficultyEffects.asset! Cannot track difficulty. Please add it to the asset.");
                return;
            }

            // Safety check: ensure the list was initialized for this action
            if (unweightedPlayerActionDifficulties[specificAction] == null)
            {
                Debug.LogWarning($"[DifficultyTracker] PlayerDAAs.{action} list was null, initializing with default value.");
                unweightedPlayerActionDifficulties[specificAction] = new List<float>() { 0.5f }; // Default to 0.5 difficulty
            }

            for (int i = 0; i < pde.playerActions.Count; i++)
            {
                if (pde.playerActions[i].action != action) continue;


                if (pde.playerActions[i].useOnlyMostRecentDifficulty)
                {
                    // Safety check: ensure the list has at least one element
                    if (unweightedPlayerActionDifficulties[specificAction].Count == 0)
                    {
                        unweightedPlayerActionDifficulties[specificAction].Add(unweightedDifficulty);
                    }
                    else
                    {
                        unweightedPlayerActionDifficulties[specificAction][0] = unweightedDifficulty;
                    }
                }
                else
                {
                    unweightedPlayerActionDifficulties[specificAction].Add(unweightedDifficulty);

                    // Forget the oldest if we are remembering too many
                    if (unweightedPlayerActionDifficulties[specificAction].Count > maxLength)
                        unweightedPlayerActionDifficulties[specificAction].RemoveAt(0);
                }

                break;
            }
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

            // Safety check: ensure the action index is valid
            int actionIndex = (int)actionToTranslate;
            if (actionIndex < 0 || actionIndex >= unweightedPlayerActionDifficulties.Length ||
                unweightedPlayerActionDifficulties[actionIndex] == null)
            {
                Debug.LogWarning($"[DifficultyTracker] Cannot translate difficulty for {actionToTranslate} - not properly initialized. Returning 0.");
                return 0f;
            }

            for (int i = 0; i < pde.playerActions.Count; i++)
            {
                if (pde.playerActions[i].action != actionToTranslate) continue;


                if (pde.playerActions[i].useOnlyMostRecentDifficulty)
                {
                    // Safety check: ensure list has at least one element
                    if (unweightedPlayerActionDifficulties[actionIndex].Count == 0)
                    {
                        Debug.LogWarning($"[DifficultyTracker] {actionToTranslate} list is empty. Returning 0.");
                        difficultyTranslation = 0f;
                    }
                    else
                    {
                        difficultyTranslation = unweightedPlayerActionDifficulties[actionIndex][0];
                    }
                }
                else
                {
                    float totalActionValue = 0f;

                    int maxActionsRemembered = pde.playerActions[i].actionsRemembered;
                    int actualActionsRemembered = unweightedPlayerActionDifficulties[actionIndex].Count;

                    Debug.Log($"Did {actionToTranslate} action. This actions is remembered {maxActionsRemembered} times and has been performed {actualActionsRemembered} times.");

                    for (int j = 1; j <= actualActionsRemembered; j++)
                    {
                        // Figure out what weight this action should have based on how many actions of this type are remembered
                        float thisWeight = Mathf.Log(j + 1, maxActionsRemembered + 1) - Mathf.Log(j, maxActionsRemembered + 1);

                        // Get this difficulty with applied weight. More recent actions weighted heavier
                        totalActionValue += unweightedPlayerActionDifficulties[actionIndex][actualActionsRemembered - j] * thisWeight;

                        Debug.Log($"Weight {j} is {thisWeight}, and the difficulty multiplied by that weight {unweightedPlayerActionDifficulties[actionIndex][actualActionsRemembered - j]}, resulting in {unweightedPlayerActionDifficulties[actionIndex][actualActionsRemembered - j] * thisWeight}, totalling {totalActionValue}");
                    }

                    difficultyTranslation = totalActionValue;
                }

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
                RememberUnweightedAction(actionToAdjust, pde.playerActions[i].curve.Evaluate(actionInputValue), pde.playerActions[i].actionsRemembered);

                // Assigns the actual difficulties as the weighted difficulties
                actualDifficulties[i] = DifficultyTranslation(actionToAdjust);

                // Evaluates the effective difficulty based on the updates info about the player action
                //actualDifficulties[i] = pde.playerActions[i].curve.Evaluate(actionInputValue);

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

#if UNITY_EDITOR
        /// <summary>
        /// Test function to help visualise how individual actions affect the overall difficulty
        /// </summary>
        /// <param name="playerDifficultyAction">An action to get the difficulty of</param>
        /// <returns>The difficulty provided by the player action</returns>
        public static float GetDifficultyF(PlayerDAAs playerDifficultyAction)
        {
            CalledNow();

            // Find the index in actualDifficulties by matching the action
            for (int i = 0; i < pde.playerActions.Count; i++)
            {
                if (pde.playerActions[i].action == playerDifficultyAction)
                {
                    return actualDifficulties[i];
                }
            }

            // If not found, return 0 and log warning
            Debug.LogWarning($"[DifficultyTracker] GetDifficultyF called for {playerDifficultyAction} but it's not configured in PlayerDifficultyEffects! Returning 0.");
            return 0f;
        }
#endif

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
#if UNITY_EDITOR
                    Debug.Log($"Applying cost change of {ede.enemyActions[i].action}, on capability {ede.enemyActions[i].capability}.\nThe cost is being set to {DifficultyTranslation(ede.enemyActions[i].action)}, given the difficulty {GetDifficultyF()}");
#endif
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
#if UNITY_EDITOR
                Debug.Log($"Applying cost change of {ede.enemyActions[i].action}, on capability {ede.enemyActions[i].capability}.\nThe cost is being set to {DifficultyTranslation(ede.enemyActions[i].action)}, given the difficulty {GetDifficultyF()}");
#endif
                ede.enemyActions[i].capability.actions[0].baseCost = DifficultyTranslation(ede.enemyActions[i].action);
            }
#if UNITY_EDITOR
            Debug.Log($"Difficulty set to {GetDifficultyF().ToString("n2")}");
#endif
            // Add difficulty log data to LogMaster
            LogMaster.Instance?.AddDdaLogData(Time.time, GetDifficultyF());
        }

        /// <summary>
        /// Get the max amount of actions/difficulties remembered for an action
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static int GetMaxRemembered(PlayerDAAs action)
        {
            CalledNow();

            int maxRemembered = 0;

            // Goes through all playerDAAs
            for (int i = 0; i < pde.playerActions.Count; i++)
            {
                // Skips non-selected ones
                if (pde.playerActions[i].action != action) continue;

                maxRemembered = pde.playerActions[i].actionsRemembered;

                break;
            }

            return maxRemembered;
        }
#region Testing Region
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
#endregion
    }
}