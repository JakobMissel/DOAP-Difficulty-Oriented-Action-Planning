using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("HasLastKnownPositionSensor-cd897159-34ae-441e-904c-3fca6a73e074")]
    public class HasLastKnownPositionSensor : LocalWorldSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            var brain = refs.GetCachedComponent<BrainBehaviour>();
            bool hasLastKnown = brain != null && brain.HasLastKnownPosition;
            
            Debug.Log($"[HasLastKnownPositionSensor] {agent.Transform.name} - HasLastKnown: {hasLastKnown}");
            return new SenseValue(hasLastKnown);
        }
    }
}