using UnityEngine;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("PlayerTarget-2264f0c6-cd4e-43a3-844a-d8e505c7c1c4")]
    public class PlayerTargetSensor : LocalTargetSensorBase
    {
        private Transform player;

        public override void Created()
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            Debug.Log($"[PlayerTargetSensor-1] Player={player} sensed");
        }

        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference refs, ITarget existingTarget)
        {
            if (player == null)
            {
                Debug.Log("[PlayerTargetSensor] No player transform found!");
                return null;
            }
            
            // Check if the agent can see the player using SimpleGuardSightNiko
            if (!agent.Transform.TryGetComponent<SimpleGuardSightNiko>(out var sight))
            {
                Debug.LogWarning("[PlayerTargetSensor] No SimpleGuardSightNiko component found!");
                return null;
            }

            if (!sight.CanSeePlayer())
            {
                Debug.Log("[PlayerTargetSensor] Player not in sight - returning null");
                return null;
            }

            // Player is visible - return target
            float dist = Vector3.Distance(agent.Transform.position, player.position);
            Debug.Log($"[PlayerTargetSensor] Player VISIBLE - Distance: {dist:F2}");

            if (existingTarget is TransformTarget t)
                return t.SetTransform(player);
            
            return new TransformTarget(player);
        }
    }
}