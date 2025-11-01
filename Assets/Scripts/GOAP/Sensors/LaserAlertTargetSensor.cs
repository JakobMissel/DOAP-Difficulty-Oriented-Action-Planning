// filepath: Assets/Scripts/GOAP/Sensors/LaserAlertTargetSensor.cs
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using Assets.Scripts.GOAP.Systems;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("LaserAlertTargetSensor-76a9bf6a-5d9d-4a1f-8c3e-6df9c1b4f0f1")]
    public class LaserAlertTargetSensor : LocalTargetSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference refs, ITarget existingTarget)
        {
            if (!LaserAlertSystem.Active)
                return null;

            Vector3 pos = LaserAlertSystem.GetCurrentPosition();

            // Optional fallback: use a tagged object if no anchor was provided
            if (LaserAlertSystem.Anchor == null)
            {
                var tagged = GameObject.FindWithTag("LaserPosition");
                if (tagged != null)
                    pos = tagged.transform.position;
            }

            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                pos = hit.position;

            if (existingTarget is PositionTarget t)
                return t.SetPosition(pos);

            return new PositionTarget(pos);
        }
    }
}
