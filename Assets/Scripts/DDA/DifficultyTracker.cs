using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Assets.Scripts.Variables;
using Assets.Scripts.DDA;


[Serializable] public class DaaDictionary : SerializableDictionary<DifficultyAdjustingActions, AnimationCurve> { }

namespace Assets.Scripts.DDA
{
    public enum DifficultyAdjustingActions
    {
        PaintingsStolen,
        TimeUnseen
    }

    /// <summary>
    /// Singleton to keep track of what difficulty the game is at
    /// </summary>
    public class DifficultyTracker : MonoBehaviour
    {
        public static DifficultyTracker Instance { get; private set; }

        private float effectiveDifficulty;


        [SerializeField] private DaaDictionary actionDifficultyEffect;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public void AlterDifficulty()
        {

        }

        /// <summary>
        /// Gets the current difficulty as an int between 0 and 100
        /// </summary>
        /// <returns></returns>
        public int GetDifficulty()
        {
            int difficulty = Mathf.RoundToInt(effectiveDifficulty * 100);
            return difficulty;
        }
    }
}