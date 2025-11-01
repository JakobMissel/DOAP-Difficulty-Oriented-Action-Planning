using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Sensors.Laser
{
    [GoapId("LaserTriggeredSensor-0c0db3e3-3415-4e86-8a83-10c2f9b22fd1")]
    public class LaserTriggeredSensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            var laser = refs.GetCachedComponent<LaserBehaviour>();
            int value = (laser != null) ? laser.PendingTriggers : 0;
            return new SenseValue(value);
        }
    }
}

