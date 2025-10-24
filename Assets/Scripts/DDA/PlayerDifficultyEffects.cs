using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts.DDA
{
    [CreateAssetMenu(fileName = "PlayerDifficultyEffects", menuName = "DDA/PlayerDifficultyEffects")]
    public class PlayerDifficultyEffects : ScriptableObject
    {
        [SerializeField, Tooltip("How a player action affects difficulty.")] public List<DifficultyAdjustingAction> playerActions;
    }
}
