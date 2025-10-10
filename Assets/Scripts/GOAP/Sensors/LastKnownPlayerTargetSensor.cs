using UnityEngine;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("LastKnownPlayerTargetSensor-ca4d172e-345b-4a5d-905b-fb7f2a44c144")]
    public class LastKnownPlayerTargetSensor : LocalTargetSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference refs, ITarget existingTarget)
        {
            var brain = refs.GetCachedComponent<BrainBehaviour>();
            
            if (brain != null && brain.HasLastKnownPosition)
            {
                Debug.Log($"[LastKnownPlayerTargetSensor] Returning last known position: {brain.LastKnownPlayerPosition}");
                
                if (existingTarget is PositionTarget pt)
                    return pt.SetPosition(brain.LastKnownPlayerPosition);
                
                return new PositionTarget(brain.LastKnownPlayerPosition);
            }
            
            Debug.Log("[LastKnownPlayerTargetSensor] No last known position available");
            return null;
        }
    }
}