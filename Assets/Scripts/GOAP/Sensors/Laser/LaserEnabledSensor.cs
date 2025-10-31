using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Sensors.Laser
{
    [GoapId("LaserEnabledSensor-0adf54c7-6a47-4f6d-9d7e-8e0a1f1f2f31")]
    public class LaserEnabledSensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            var laser = refs.GetCachedComponent<LaserBehaviour>();
            int value = (laser != null && laser.IsLaserEnabled) ? 1 : 0;
            return new SenseValue(value);
        }
    }
}

