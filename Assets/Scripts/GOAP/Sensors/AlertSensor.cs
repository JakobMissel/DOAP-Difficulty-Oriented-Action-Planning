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
        private float alertCooldownTimer = 0f;
        private const float ALERT_COOLDOWN_DURATION = 1f; // Stay alert for `n` seconds after losing sight

        public override void Created()
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        public override void Update()
        {
            // Decay the cooldown timer
            if (alertCooldownTimer > 0f)
            {
                alertCooldownTimer -= Time.deltaTime;
            }
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
                
                if (canSee)
                {
                    // Player is visible - reset the cooldown timer and set alert
                    alertCooldownTimer = ALERT_COOLDOWN_DURATION;
                    Debug.Log($"[AlertSensor] {agent.Transform.name} can see player - IsAlert: true");
                    return new SenseValue(true);
                }
                else
                {
                    // Player not visible - check cooldown timer OR last known position
                    bool hasLastKnown = brain != null && brain.HasLastKnownPosition;
                    bool isAlert = alertCooldownTimer > 0f || hasLastKnown;
                    
                    Debug.Log($"[AlertSensor] {agent.Transform.name} - CanSee: false, Cooldown: {alertCooldownTimer:F1}s, HasLastKnown: {hasLastKnown}, IsAlert: {isAlert}");
                    return new SenseValue(isAlert);
                }
            }

            Debug.LogWarning($"[AlertSensor] {agent.Transform.name} has no SimpleGuardSightNiko component!");
            return new SenseValue(false);
        }
    }
}