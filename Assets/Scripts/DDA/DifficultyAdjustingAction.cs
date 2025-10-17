using System;
using UnityEngine;

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

    [Serializable]
    public class DifficultyAdjustingAction
    {
        [SerializeField, Tooltip("Action to assign difficulty adjustements to")] public DifficultyAdjustingActions action;
        [SerializeField, Tooltip("How this action affects difficulty. 0 as value means no effect, 1 means max difficulty. Negative values would lower total difficulty.\nTime to be based on the action (Eg 10 captures for times captured)")] public AnimationCurve curve;
        [SerializeField, Tooltip("The current difficulty given by this action.")] public float effectiveDifficulty = 0.16f;
    }

    [Serializable]
    public class EnemyAction
    {
        [SerializeField, Tooltip("Which enemy action to adjust costs of based on difficulty")] public EnemyActions action;
        [SerializeField, Tooltip("How much this action will cost based on difficulty. Value 0 means no cost, 5 means 5 times the base cost.\nTime is difficulty between 0 and 1.")] public AnimationCurve curve;
    }
}
