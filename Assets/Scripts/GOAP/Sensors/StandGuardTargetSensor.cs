using UnityEngine;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("StandGuardTargetSensor-b8c9d0e1-f2a3-4b4c-5d6e-7f8a9b0c1d2e")]
    public class StandGuardTargetSensor : LocalTargetSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            // Find all objects tagged with "StandGuardPoint"
            GameObject[] guardPoints = GameObject.FindGameObjectsWithTag("StandGuardPoint");
            
            if (guardPoints == null || guardPoints.Length == 0)
                return null;

            // Find the nearest guard point
            GameObject nearestPoint = null;
            float nearestDistance = float.MaxValue;

            foreach (var point in guardPoints)
            {
                if (point != null)
                {
                    float distance = Vector3.Distance(agent.Transform.position, point.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestPoint = point;
                    }
                }
            }

            if (nearestPoint == null)
                return null;

            if (existingTarget is PositionTarget pt)
                return pt.SetPosition(nearestPoint.transform.position);

            return new PositionTarget(nearestPoint.transform.position);
        }
    }
}

