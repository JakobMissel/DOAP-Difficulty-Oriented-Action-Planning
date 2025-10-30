using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts.DDA
{
    [CreateAssetMenu(fileName = "EnemyDifficultyEffects", menuName = "DDA/EnemyDifficultyEffects")]
    public class EnemyDifficultyEffects : ScriptableObject
    {
#if UNITY_EDITOR
        // Gonna ignore the warning that this isn't a used variable. Its use is providing clarity in the editor.
#pragma warning disable 0414
        [SerializeField, TextArea(1, 5), Tooltip("A note to help explain this Scriptable Object and its pruposes.")] private string developerNote = "";
#pragma warning restore 0414
#endif
        [SerializeField, Tooltip("How the difficulty affects an enemy action.")] public List<EnemyAction> enemyActions;
    }
}
