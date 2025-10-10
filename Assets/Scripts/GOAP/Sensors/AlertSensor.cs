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
                Debug.Log("[AlertSensor] No player found");
                return new SenseValue(false);
            }

            var brain = refs.GetCachedComponent<BrainBehaviour>();
            
            // Use the SimpleGuardSightNiko component to check if player is visible
            if (agent.Transform.TryGetComponent<SimpleGuardSightNiko>(out var sight))
            {
                bool canSee = sight.CanSeePlayer();
                
                // Alert is TRUE if:
                // 1. We can see the player, OR
                // 2. We have a last known position to investigate
                bool hasLastKnown = brain != null && brain.HasLastKnownPosition;
                bool isAlert = canSee || hasLastKnown;
                
                Debug.Log($"[AlertSensor] {agent.Transform.name} - CanSee: {canSee}, HasLastKnown: {hasLastKnown}, IsAlert: {isAlert}");
                return new SenseValue(isAlert ? 1 : 0);
            }

            Debug.LogWarning($"[AlertSensor] {agent.Transform.name} has no SimpleGuardSightNiko component!");
            return new SenseValue(false);
        }
    }
}