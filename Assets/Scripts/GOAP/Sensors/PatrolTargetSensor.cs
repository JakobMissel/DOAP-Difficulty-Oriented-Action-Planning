using UnityEngine;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Assets.Scripts.GOAP.Sensors
{
    // Defining a GoapId is required for ScriptableObject configuration
    [GoapId("PatrolTargetSensor-19d1c6d2-77c7-4e3f-b8cf-123456789abc")]
    public class PatrolTargetSensor : LocalTargetSensorBase
    {
        public override void Created() { } // Is called when this script is initialzed
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            var route = references.GetCachedComponent<Assets.Scripts.GOAP.Behaviours.PatrolRouteBehaviour>();
            if (route == null) return null;

            var wp = route.GetCurrent();
            if (wp == null) return null;

            if (existingTarget is TransformTarget t)
                return t.SetTransform(wp);

            return new TransformTarget(wp);
        }
    }
}