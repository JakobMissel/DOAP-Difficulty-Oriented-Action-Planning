using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("RechargeTargetSensor-d75bc583-1396-4c22-a928-5c77e9a1bdaa")]
    public class RechargeTargetSensor : LocalTargetSensorBase
    {
        private Transform station;

        public override void Created()
        {
            var obj = GameObject.FindWithTag("Breakpoint");
            if (obj != null)
                station = obj.transform;
        }
        
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference refs, ITarget existingTarget)
        {
            if (station == null)
                return null;

            if (existingTarget is TransformTarget t)
                return t.SetTransform(station);

            return new TransformTarget(station);
        }
    }
}