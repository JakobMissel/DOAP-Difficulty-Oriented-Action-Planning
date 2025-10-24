using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.DDA
{
    /// <summary>
    /// Singleton to keep track of what difficulty the game is at
    /// </summary>
    public static class DifficultyTracker
    {
        private static PlayerDifficultyEffects pde;
        private static EnemyDifficultyEffects ede;

        private static bool hasBeenCalled = false;

        private static List<float> effectiveDifficulties = new List<float>();

        /// <summary>
        /// If this is the first time this is called, get the Player and Enemy Difficulty Effects
        /// </summary>
        private static void CalledNow()
        {
            if (hasBeenCalled) return;

            hasBeenCalled = true;

            pde = Resources.Load<PlayerDifficultyEffects>("DDA/PlayerDifficultyEffects");
            ede = Resources.Load<EnemyDifficultyEffects>("DDA/EnemyDifficultyEffects");

            // Assign pde to base values
            for (int i = 0; i < pde.playerActions.Count; i++)
            {
                effectiveDifficulties.Add(pde.playerActions[i].startDifficulty);
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
            foreach (DifficultyAdjustingAction action in pde.playerActions)
            {
                if (action.action != actionToTranslate) continue;

                difficultyTranslation = action.curve.Evaluate(GetDifficultyF());

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
            foreach (EnemyAction action in ede.enemyActions)
            {
                if (action.action != actionToTranslate) continue;

                difficultyTranslation = action.curve.Evaluate(GetDifficultyF());

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
                effectiveDifficulties[i] = pde.playerActions[i].curve.Evaluate(actionInputValue);

                break;
            }
        }

        /// <summary>
        /// Gets the current difficulty as a float between 0 and 1
        /// </summary>
        /// <returns></returns>
        public static float GetDifficultyF()
        {
            CalledNow();

            float summedEffectiveDifficulty = 0f;
            
            // Sum and clamp01 all effective difficulties
            foreach (float effectiveDifficulty in effectiveDifficulties)
            {
                summedEffectiveDifficulty += effectiveDifficulty;
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
    }
}