using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("EnergySensor-a4da1183-e27a-4159-83bd-fb49f2ea3f2a")]
    public class EnergySensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            var energyBehaviour = refs.GetCachedComponent<EnergyBehaviour>();
            if (energyBehaviour == null)
            {
                // Removed per-frame warning
                return new SenseValue(0);
            }

            // Return the exact energy value (rounded to nearest int for world state)
            return new SenseValue(Mathf.RoundToInt(energyBehaviour.CurrentEnergy));
        }
    }
}