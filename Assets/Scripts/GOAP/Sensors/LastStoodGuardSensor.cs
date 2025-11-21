using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Core;
using UnityEngine;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("LastStoodGuardSensor-a7b8c9d0-e1f2-4a3b-4c5d-6e7f8a9b0c1d")]
    public class LastStoodGuardSensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            var timerBehaviour = refs.GetCachedComponent<Assets.Scripts.GOAP.Behaviours.StandGuardTimerBehaviour>();
            
            if (timerBehaviour == null)
            {
                // No timer behaviour means never guarded, return 0 (ready to guard)
                return new SenseValue(0);
            }

            // Return time since guard duty started (will be 0 when not guarding, counts up during cooldown)
            float timeSinceGuardStart = timerBehaviour.GetTimeSinceGuardStart();
            return new SenseValue(Mathf.RoundToInt(timeSinceGuardStart));
        }
    }
}

