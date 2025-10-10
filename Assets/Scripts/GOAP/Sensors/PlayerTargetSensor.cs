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

            if (!agent.Transform.TryGetComponent<SimpleGuardSightNiko>(out var sight))
            {
                Debug.LogWarning("[PlayerTargetSensor] No SimpleGuardSightNiko component found!");
                return null;
            }

            bool canSee = sight.CanSeePlayer();
            
            if (canSee)
            {
                Debug.Log($"[PlayerTargetSensor] Can see player at {player.position}");
                
                if (existingTarget is TransformTarget t)
                    return t.SetTransform(player);

                return new TransformTarget(player);
            }

            Debug.Log("[PlayerTargetSensor] Cannot see player");
            return null;
        }
    }
}