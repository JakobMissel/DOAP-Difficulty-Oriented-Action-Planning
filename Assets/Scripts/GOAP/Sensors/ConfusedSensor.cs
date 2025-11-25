using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("ConfusedSensor-b9e4d8a3-5f2c-4b8e-ad3f-6e7b9c2d4a5e")]
    public class ConfusedSensor : LocalWorldSensorBase
    {
        public override void Created()
        {
        }

        public override void Update()
        {
        }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            // Returns true if guard has heard ANY noise (player or distraction)
            // This is specifically for noise detection, not sight-based detection
            if (agent.Transform.TryGetComponent<BrainBehaviour>(out var brain))
            {
                bool hasHeardNoise = brain.HasHeardNoise;
                return new SenseValue(hasHeardNoise ? 1 : 0);
            }

            return new SenseValue(false);
        }
    }
}
