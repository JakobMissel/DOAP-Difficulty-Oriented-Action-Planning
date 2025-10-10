using UnityEngine;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("PatrolTargetSensor-19d1c6d2-77c7-4e3f-b8cf-123456789abc")]
    public class PatrolTargetSensor : LocalTargetSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            var route = references.GetCachedComponent<Assets.Scripts.GOAP.Behaviours.PatrolRouteBehaviour>();
            if (route == null) 
            {
                Debug.LogWarning("[PatrolTargetSensor] No PatrolRouteBehaviour found!");
                return null;
            }

            var wp = route.GetCurrent();
            if (wp == null) 
            {
                Debug.LogWarning("[PatrolTargetSensor] No current waypoint!");
                return null;
            }

            Debug.Log($"[PatrolTargetSensor] Target waypoint: {wp.name} at {wp.position}");

            if (existingTarget is PositionTarget pt)
                return pt.SetPosition(wp.position);

            return new PositionTarget(wp.position);
        }
    }
}