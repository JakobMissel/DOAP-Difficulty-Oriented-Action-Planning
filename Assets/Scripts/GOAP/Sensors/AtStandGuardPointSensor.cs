using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Core;
using UnityEngine;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("AtStandGuardPointSensor-f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0c")]
    public class AtStandGuardPointSensor : LocalWorldSensorBase
    {
        private const float PROXIMITY_THRESHOLD = 5f;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            // Find all objects tagged with "StandGuardPoint"
            GameObject[] guardPoints = GameObject.FindGameObjectsWithTag("StandGuardPoint");
            
            if (guardPoints == null || guardPoints.Length == 0)
                return new SenseValue(0);

            // Check if any guard point is within 5 meters
            foreach (var point in guardPoints)
            {
                if (point != null)
                {
                    float distance = Vector3.Distance(agent.Transform.position, point.transform.position);
                    if (distance <= PROXIMITY_THRESHOLD)
                    {
                        return new SenseValue(1);
                    }
                }
            }

            return new SenseValue(0);
        }
    }
}

