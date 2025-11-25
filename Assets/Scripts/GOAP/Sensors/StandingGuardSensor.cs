using UnityEngine;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Core;
using StandGuardTimerBehaviour = Assets.Scripts.GOAP.Behaviours.StandGuardTimerBehaviour;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("StandingGuardSensor-d4e5f6a7-b8c9-0d1e-2f3a-4b5c6d7e8f9a")]
    public class StandingGuardSensor : LocalWorldSensorBase
    {
        public override void Created()
        {
            Debug.Log("[StandingGuardSensor] Created");
        }

        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            var timerBehaviour = refs.GetCachedComponent<StandGuardTimerBehaviour>();

            // If no timer exists, can stand guard (return 0 = not currently standing guard)
            if (timerBehaviour == null)
            {
                return new SenseValue(0);
            }

            // Check if guard can perform StandGuardAction again
            // Returns 0 if CAN stand guard (cooldown expired)
            // Returns 1 if CANNOT stand guard (currently standing or cooldown active)
            bool canStandGuard = timerBehaviour.CanStandGuardAgain();

            if (Time.frameCount % 300 == 0) // Log every 5 seconds
            {
                Debug.Log($"[StandingGuardSensor] {agent.Transform.name} CanStandGuardAgain = {canStandGuard}, returning {(canStandGuard ? 0 : 1)}");
            }

            return new SenseValue(canStandGuard ? 0 : 1);
        }
    }
}
