// Assets/Scripts/GOAP/Sensors/HeardNoiseSensor.cs
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("HeardNoiseSensor-40920bcf-96e7-4bab-b863-1d2b153581a4")]
    public class HeardNoiseSensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            var brain = refs.GetCachedComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
            if (brain != null && brain.HasHeardDistractionNoise)
            {
                return new SenseValue(1); // true
            }

            return new SenseValue(0); // false
        }
    }
}