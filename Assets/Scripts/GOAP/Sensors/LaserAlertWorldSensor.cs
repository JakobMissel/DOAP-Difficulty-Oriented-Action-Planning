// filepath: Assets/Scripts/GOAP/Sensors/LaserAlertWorldSensor.cs
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using Assets.Scripts.GOAP.Systems;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("LaserAlertWorldSensor-1c9e351b-7b8d-4d5c-8b3b-8b3e51c6fd10")]
    public class LaserAlertWorldSensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            // Keep the timed clear running even if no guards arrive
            LaserAlertSystem.UpdateTimer();
            return new SenseValue(LaserAlertSystem.WorldKeyActive ? 1 : 0);
        }
    }
}
