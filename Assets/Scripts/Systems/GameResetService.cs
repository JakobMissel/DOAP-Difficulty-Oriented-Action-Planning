using Assets.Scripts.DDA;
using Assets.Scripts.GOAP;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    /// <summary>
    /// Central place to reset persistent singletons and static systems when returning to the main menu.
    /// Ensures a new playthrough starts from a clean slate without requiring a full application restart.
    /// </summary>
    public static class GameResetService
    {
        public static void ResetPersistentSystems()
        {
            DifficultyTracker.HardReset();
            EvasionTracker.ResetAllPursuits();
            EvasionTracker.ResetEvasionCount();
            DdaPlayerActions.Instance?.ResetRunData();
        }

        public static void ResetGoapSystems()
        {
            // GOAP currently persists via GoapPersistence; leave this in place until a safe reset hook is defined.
        }
    }
}
