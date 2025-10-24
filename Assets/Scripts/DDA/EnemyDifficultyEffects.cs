using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts.DDA
{
    [CreateAssetMenu(fileName = "EnemyDifficultyEffects", menuName = "DDA/EnemyDifficultyEffects")]
    public class EnemyDifficultyEffects : ScriptableObject
    {
        [SerializeField, Tooltip("How the difficulty affects an enemy action.")] public List<EnemyAction> enemyActions;
    }
}
