using CrashKonijn.Goap.Runtime;
using System;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.DDA
{
    /// <summary>
    /// Player Difficulty Adjusting Actions
    /// </summary>
    public enum PlayerDAAs
    {
        TimesCaptured,
        TimeBetweenPaintings,
        TimesEvaded,
        // SuccessfulItemUsage removed - now using EvasionTracker (TimesEvaded) instead
    }

    /// <summary>
    /// Enemy Actions adjusted by the Difficulty
    /// </summary>
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
        RechargeAction,

    }

    [Serializable]
    public class DifficultyAdjustingAction
    {
        [SerializeField, Tooltip("Action to assign difficulty adjustements to")] public PlayerDAAs action;
        [SerializeField, Tooltip("How this action affects difficulty. 0 as value means no effect, 1 means max difficulty. Negative values would lower total difficulty.\nTime to be based on the action (Eg 10 captures for times captured)")] public AnimationCurve curve;
        [SerializeField, Tooltip("The amount of actions this should remember to calculate the difficulty from")] private uint _actionsRemembered = 3;
        [HideInInspector, Tooltip("The amount of actions this should remember to calculate the difficulty from")] public int actionsRemembered => (int)_actionsRemembered;
        [SerializeField, Tooltip("Whether this uses the last actionsRemembered difficulties to get the current from, or uses only the recent-most")] private bool _useOnlyMostRecentDifficulty = false;
        [HideInInspector, Tooltip("Whether this uses the last actionsRemembered difficulties to get the current from, or uses only the recent-most")] public bool useOnlyMostRecentDifficulty => _useOnlyMostRecentDifficulty;
        [SerializeField, Tooltip("Start difficulty of this")] private float _startDifficulty = 0.16f;
        [HideInInspector, Tooltip("Start difficulty of this")] public float startDifficulty => _startDifficulty;
#if UNITY_EDITOR
        // Gonna ignore the warning that this isn't a used variable. Its use is providing clarity in the editor.
#pragma warning disable 0414
        [SerializeField, TextArea(1, 5), Tooltip("A note to help explain this Difficulty Adjusting Action.\nWhat affects the difficulty here, and the values assigned to it.")] private string developerNote = "";
#pragma warning restore 0414
#endif
    }

    [Serializable]
    public class EnemyAction
    {
        [SerializeField, Tooltip("Which enemy action to adjust costs of based on difficulty")] public EnemyActions action;
        [SerializeField, Tooltip("How much this action will cost based on difficulty. Value 0 means no cost, 5 means a cost of 5.\nTime is difficulty between 0 and 1.")] public AnimationCurve curve;
        [SerializeField, Tooltip("What capability's cost should be altered (If any)")] public CapabilityConfigScriptable capability;
#if UNITY_EDITOR
        // Gonna ignore the warning that this isn't a used variable. Its use is providing clarity in the editor.
#pragma warning disable 0414
        [SerializeField, TextArea(1, 5), Tooltip("A note to help explain this Enemy Action.\nWhat it affects, and the values assigned to it.")] private string developerNote = "";
#pragma warning restore 0414
#endif
    }
}
