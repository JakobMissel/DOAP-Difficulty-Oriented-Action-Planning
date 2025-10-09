using UnityEngine;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("PlayerTarget-2264f0c6-cd4e-43a3-844a-d8e505c7c1c4")]
    public class PlayerTargetSensor : LocalTargetSensorBase
    {
        private Transform player;

        public override void Created()
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            Debug.Log($"[PlayerTargetSensor] Player found: {player != null}");
        }

        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference refs, ITarget existingTarget)
        {
            if (player == null)
            {
                Debug.Log("[PlayerTargetSensor] No player transform found!");
                return null;
            }
            
            var brain = refs.GetCachedComponent<BrainBehaviour>();
            
            // Check if the agent can see the player using SimpleGuardSightNiko
            if (!agent.Transform.TryGetComponent<SimpleGuardSightNiko>(out var sight))
            {
                Debug.LogWarning("[PlayerTargetSensor] No SimpleGuardSightNiko component found!");
                return null;
            }

            bool canSee = sight.CanSeePlayer();
            
            // Update the brain with current visibility state
            // This will capture last known position when player leaves sight
            if (brain != null)
            {
                brain.UpdatePlayerVisibility(canSee, player.position);
            }

            if (canSee)
            {
                // Player is visible - return live tracking target
                float dist = Vector3.Distance(agent.Transform.position, player.position);
                Debug.Log($"[PlayerTargetSensor] Player VISIBLE - Distance: {dist:F2}");

                if (existingTarget is TransformTarget t)
                    return t.SetTransform(player);
                
                return new TransformTarget(player);
            }
            else
            {
                // Player not visible - check if we have a last known position to investigate
                if (brain != null && brain.HasLastKnownPosition)
                {
                    Debug.Log($"[PlayerTargetSensor] Player not visible - using FROZEN last known position: {brain.LastKnownPlayerPosition}");
                    
                    if (existingTarget is PositionTarget pt)
                        return pt.SetPosition(brain.LastKnownPlayerPosition);
                    
                    return new PositionTarget(brain.LastKnownPlayerPosition);
                }
                
                Debug.Log("[PlayerTargetSensor] Player not in sight and no last known position - returning null");
                return null;
            }
        }
    }
}