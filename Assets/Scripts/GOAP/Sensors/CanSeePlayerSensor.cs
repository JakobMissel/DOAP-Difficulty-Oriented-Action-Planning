using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("CanSeePlayerSensor-859b0f9f-9d6a-41ba-adf2-a7a7526be1e9")]
    public class CanSeePlayerSensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            if (agent.Transform.TryGetComponent<SimpleGuardSightNiko>(out var sight))
            {
                bool canSee = sight.CanSeePlayer();
                Debug.Log($"[CanSeePlayerSensor] {agent.Transform.name} - CanSee: {canSee}");
                return new SenseValue(canSee ? 1 : 0);
            }

            return new SenseValue(false);
        }
    }
}