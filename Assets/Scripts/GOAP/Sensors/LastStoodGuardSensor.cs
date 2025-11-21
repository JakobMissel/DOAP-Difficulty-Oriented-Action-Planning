using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Core;
using UnityEngine;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("LastStoodGuardSensor-a7b8c9d0-e1f2-4a3b-4c5d-6e7f8a9b0c1d")]
    public class LastStoodGuardSensor : LocalWorldSensorBase
    {
        private const float COOLDOWN_DURATION = 15f;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            var timerBehaviour = refs.GetCachedComponent<Assets.Scripts.GOAP.Behaviours.StandGuardTimerBehaviour>();
            
            if (timerBehaviour == null)
            {
                // No timer behaviour means the guard has never stood guard, so it's available
                return new SenseValue(Mathf.RoundToInt(COOLDOWN_DURATION + 1)); // Return value > 15
            }

            float timeSinceLastGuard = timerBehaviour.GetTimeSinceLastGuard();
            return new SenseValue(Mathf.RoundToInt(timeSinceLastGuard));
        }
    }
}

