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
    public enum DifficultyAdjustingActions
    {
        TimesCaptured,
        TimeBetweenPaintings,
        SuccesfulItemUsage,

    }

    public enum EnemyActions
    {
        StandGuard,
        Patrol,
        InspectArea,
        BatterypowerUsage,
        ContactBackup,
        ChasePlayer,
        EnergyUsage,
        ActivateLasers,
        ActivateCameras,

    }

    /// <summary>
    /// Singleton to keep track of what difficulty the game is at
    /// </summary>
    public class DifficultyTracker : MonoBehaviour
    {
        public static DifficultyTracker Instance { get; private set; }

        [SerializeField, Tooltip("How a player action affects difficulty. 0 as value means no effect, 1 means max difficulty. Time to be based on player action.")] private DaaDictionary playerDifficultyEffect;
        [SerializeField, Tooltip("How much an enemy action will cost based on difficulty. Value 0 means no cost, 5 means 5 times the cost. Time to be between 0 and 1.")] private EaDictionary enemyDifficultyEffect;
        
        [SerializeField, Tooltip("Effective difficulties of the differens difficultyAdjustingActions")] private Dictionary<DifficultyAdjustingActions, float> effectiveDifficulties = new Dictionary<DifficultyAdjustingActions, float>();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            // If not all difficultyAdjustingActions have a base effective difficulty, create one at the medium difficulty
            foreach (DifficultyAdjustingActions difficultyAction in playerDifficultyEffect.Keys)
            {
                if (effectiveDifficulties.ContainsKey(difficultyAction)) continue;
                effectiveDifficulties.Add(difficultyAction, 0.5f);
            }
        }

        /// <summary>
        /// Gets the difficulty translation of a player action
        /// </summary>
        /// <param name="actionToTranslate"></param>
        /// <returns>A number to be summed with the other DifficultyTranslationPlayer actions</returns>
        public float DifficultyTranslationPlayer(DifficultyAdjustingActions actionToTranslate)
        {
            float difficultyTranslation = playerDifficultyEffect[actionToTranslate].Evaluate(GetDifficultyF());
            return difficultyTranslation;
        }

        /// <summary>
        /// Gets the difficulty translation of an enemy action
        /// </summary>
        /// <param name="actionToTranslate">Which enemy action do you want to evaluate</param>
        /// <returns>A number to be multiplied by the WorldKey of that enemy action</returns>
        public float DifficultyTranslationEnemy(EnemyActions actionToTranslate)
        {
            float difficultyTranslation = enemyDifficultyEffect[actionToTranslate].Evaluate(GetDifficultyF());
            return difficultyTranslation;
        }

        /// <summary>
        /// To be called whenever the player does an action that affects the difficulty. Updates the effective difficulty of that action.
        /// </summary>
        /// <param name="actionToAdjust"></param>
        /// <param name="actionInputValue">Eg average seconds since last painting was picked up</param>
        public void AlterDifficulty(DifficultyAdjustingActions actionToAdjust, float actionInputValue)
        {
            effectiveDifficulties[actionToAdjust] = playerDifficultyEffect[actionToAdjust].Evaluate(actionInputValue);
        }

        /// <summary>
        /// Gets the current difficulty as a float between 0 and 1
        /// </summary>
        /// <returns></returns>
        public float GetDifficultyF()
        {
            float effectiveDifficulty = 0f;
            
            foreach (float difficulty in effectiveDifficulties.Values)
            {
                effectiveDifficulty += difficulty;
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