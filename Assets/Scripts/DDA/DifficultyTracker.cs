using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Assets.Scripts.Variables;
using Assets.Scripts.DDA;


[Serializable] public class DaaDictionary : SerializableDictionary<DifficultyAdjustingActions, AnimationCurve> { }
[Serializable] public class EaDictionary : SerializableDictionary<EnemyActions, AnimationCurve> { }

namespace Assets.Scripts.DDA
{
    /// <summary>
    /// Singleton to keep track of what difficulty the game is at
    /// </summary>
    public class DifficultyTracker : MonoBehaviour
    {
        public static DifficultyTracker Instance { get; private set; }

        [SerializeField, Tooltip("How a player action affects difficulty.")] private List<DifficultyAdjustingAction> playerDifficultyEffect;
        [SerializeField, Tooltip("How much an enemy action will cost based on difficulty.")] private List<EnemyAction> enemyDifficultyEffect;
        
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Gets the difficulty translation of a player action
        /// </summary>
        /// <param name="actionToTranslate"></param>
        /// <returns>A number to be summed with the other DifficultyTranslationPlayer actions</returns>
        public float DifficultyTranslationPlayer(DifficultyAdjustingActions actionToTranslate)
        {
            float difficultyTranslation = 0f;
            foreach (DifficultyAdjustingAction action in playerDifficultyEffect)
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
        /// <returns>A number to be multiplied by the WorldKey of that enemy action</returns>
        public float DifficultyTranslationEnemy(EnemyActions actionToTranslate)
        {
            float difficultyTranslation = 0f;
            foreach (EnemyAction action in enemyDifficultyEffect)
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
        public void AlterDifficulty(DifficultyAdjustingActions actionToAdjust, float actionInputValue)
        {
            // Goes through all playerDAAs
            for (int i = 0; i < playerDifficultyEffect.Count; i++)
            {
                // Skips non-selected ones
                if (playerDifficultyEffect[i].action != actionToAdjust) continue;

                // Evaluates the effective difficulty based on the updates info about the player action
                playerDifficultyEffect[i].effectiveDifficulty = playerDifficultyEffect[i].curve.Evaluate(actionInputValue);

                break;
            }
        }

        /// <summary>
        /// Gets the current difficulty as a float between 0 and 1
        /// </summary>
        /// <returns></returns>
        public float GetDifficultyF()
        {
            float effectiveDifficulty = 0f;
            
            // Sum and clamp01 all effective difficulties
            foreach (DifficultyAdjustingAction difficultyAction in playerDifficultyEffect)
            {
                effectiveDifficulty += difficultyAction.effectiveDifficulty;
            }

            effectiveDifficulty = Mathf.Clamp01(effectiveDifficulty);

            return effectiveDifficulty;
        }

        /// <summary>
        /// Gets the current difficulty as an int between 0 and 100
        /// </summary>
        /// <returns></returns>
        public int GetDifficultyI()
        {
            int difficulty = Mathf.RoundToInt(GetDifficultyF() * 100);
            return difficulty;
        }
    }
}