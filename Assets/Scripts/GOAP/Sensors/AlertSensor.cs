using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("AlertSensor-21c25467-410e-4d08-a28e-8df3f7202e15")]
    public class AlertSensor : LocalWorldSensorBase
    {
        private Transform player;

        public override void Created()
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        public override void Update()
        {
        }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            if (player == null)
            {
                return new SenseValue(false);
            }

            // IMPORTANT: Only return true if guard has VISUAL contact
            // Do NOT include last-known position here, as that should trigger ClearLastKnownGoal, not PursuitGoal
            if (agent.Transform.TryGetComponent<GuardSight>(out var sight))
            {
                bool canSee = sight.CanSeePlayer();
                return new SenseValue(canSee ? 1 : 0);
            }

            return new SenseValue(false);
        }
    }
}