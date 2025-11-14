using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;
using Assets.Scripts.GOAP.Systems;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("GoToLaser-a8fbbe83-4db9-486c-89f5-ad58636d61bb")]
    public class GoToLaserAction : GoapActionBase<GoToLaserAction.Data>
    {
        private const float SEARCH_DURATION = 1.5f; // How long to search the area around the laser
        private const float SEARCH_RADIUS = 12f; // Radius to detect player around laser location

        // This method is called when the action is created
        // This method is optional and can be removed
        public override void Created()
        {
        }

        // This method is called every frame before the action is performed
        // If this method returns false, the action will be stopped
        // This method is optional and can be removed
        public override bool IsValid(IActionReceiver agent, Data data)
        {
            return true;
        }

        // This method is called when the action is started
        // This method is optional and can be removed
        public override void Start(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var sight = mono.Transform.GetComponent<GuardSight>();
            var animation = mono.Transform.GetComponent<GuardAnimation>();
            var speedController = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.GuardDetectionSpeedController>();

            // Notify speed controller that we're investigating a laser
            if (speedController != null)
            {
                speedController.SetInvestigatingLaser(true);
            }

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            // If we already fully spotted the player, abort and clear the alert so pursuit can take over immediately
            if (sight != null && sight.PlayerSpotted())
            {
                LaserAlertSystem.ClearWorldKey();
                if (speedController != null)
                {
                    speedController.SetInvestigatingLaser(false);
                }
                return;
            }

            // Trigger Running animation when going to investigate laser
            if (animation != null)
            {
                animation.Run();
            }

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.updatePosition = true;

            // Snapshot which laser raised the alert when this action starts
            data.AnchorSnapshot = Assets.Scripts.GOAP.Systems.LaserAlertSystem.Anchor;

            // Reset all phase flags
            data.HasArrived = false;
            data.SearchTime = 0f;
            data.PlayerLocationCaptured = false;
            data.SearchComplete = false;

            // Optional immediate set; Perform keeps updating until arrival
            if (data.Target != null && data.Target.IsValid())
            {
                agent.SetDestination(data.Target.Position);
                Debug.Log($"[GoToLaserAction] {mono.Transform.name} heading to laser at {data.Target.Position}");
            }
        }

        // This method is called once before the action is performed
        // This method is optional and can be removed
        public override void BeforePerform(IMonoAgent agent, Data data)
        {
        }

        // This method is called every frame while the action is running
        // This method is required
        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var sight = mono.Transform.GetComponent<GuardSight>();
            var animation = mono.Transform.GetComponent<GuardAnimation>();
            var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
            
            // Abort instantly if we fully spotted the player; clear alert so pursuit wins
            if (sight != null && sight.PlayerSpotted())
            {
                LaserAlertSystem.ClearWorldKey();
                Debug.Log($"[GoToLaserAction] {mono.Transform.name} spotted player, aborting laser investigation");
                return ActionRunState.Stop;
            }

            if (agent == null || !agent.enabled || !agent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
                return ActionRunState.Stop;

            // Phase 1: Moving to laser location
            if (!data.HasArrived)
            {
                // Keep updating destination in case the laser alert target changes
                agent.SetDestination(data.Target.Position);

                // Check if we've arrived
                float dist = Vector3.Distance(mono.Transform.position, data.Target.Position);
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 1.0f)
                {
                    Debug.Log($"[GoToLaserAction] {mono.Transform.name} arrived at laser position - starting search");
                    
                    // Stop movement and start search phase
                    agent.isStopped = true;
                    data.HasArrived = true;
                    data.SearchTime = 0f;
                    data.LaserSearchPosition = data.Target.Position; // Store the laser position for radius check
                    
                    // Trigger search animation
                    if (animation != null)
                    {
                        animation.Search();
                    }
                }
                
                return ActionRunState.Continue;
            }

            // Phase 2: Searching the area around the laser
            if (!data.SearchComplete)
            {
                data.SearchTime += Time.deltaTime;
                
                // Only scan during the search duration
                if (data.SearchTime < SEARCH_DURATION)
                {
                    // Check if player is within search radius of the LASER position
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        float distanceToLaser = Vector3.Distance(data.LaserSearchPosition, player.transform.position);
                        
                        if (distanceToLaser <= SEARCH_RADIUS)
                        {
                            // Player detected within search radius! Capture their location
                            data.CapturedPlayerLocation = player.transform.position;
                            data.PlayerLocationCaptured = true;
                            
                            Debug.Log($"[GoToLaserAction] {mono.Transform.name} detected player within {SEARCH_RADIUS}m radius at distance {distanceToLaser:F1}m! Captured location: {data.CapturedPlayerLocation}");
                        }
                    }
                    
                    return ActionRunState.Continue;
                }
                
                // Search duration complete
                data.SearchComplete = true;
                
                if (data.PlayerLocationCaptured)
                {
                    Debug.Log($"[GoToLaserAction] {mono.Transform.name} search complete - moving to captured player location: {data.CapturedPlayerLocation}");
                    
                    // DON'T clear the laser alert yet - keep it active so the goal remains valid
                    // We'll clear it when we arrive at the captured location
                    
                    // Start moving to captured location
                    agent.isStopped = false;
                    agent.SetDestination(data.CapturedPlayerLocation);
                    
                    // Switch to walking/running animation
                    if (animation != null)
                    {
                        animation.Run();
                    }
                }
                else
                {
                    Debug.Log($"[GoToLaserAction] {mono.Transform.name} search complete - no player detected in {SEARCH_RADIUS}m radius");
                    
                    // Only clear if no player was detected (action will complete)
                    LaserAlertSystem.ClearWorldKey();
                    
                    // Resume movement
                    agent.isStopped = false;
                    
                    return ActionRunState.Completed;
                }
                
                return ActionRunState.Continue;
            }

            // Phase 3: If player location was captured, move to it
            if (data.PlayerLocationCaptured)
            {
                // Keep moving to the captured location
                agent.SetDestination(data.CapturedPlayerLocation);
                
                // Check if we've arrived at the captured location
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
                {
                    Debug.Log($"[GoToLaserAction] {mono.Transform.name} arrived at captured player location - investigation complete");
                    
                    // NOW clear the alert since we've arrived
                    LaserAlertSystem.ClearWorldKey();
                    
                    // Stop and complete the action
                    agent.isStopped = true;
                    
                    return ActionRunState.Completed;
                }
                
                return ActionRunState.Continue;
            }

            // Fallback - shouldn't reach here
            return ActionRunState.Completed;
        }

        // This method is called when the action is completed
        // This method is optional and can be removed
        public override void Complete(IMonoAgent mono, Data data)
        {
            // Try to clear the world key if no newer alert replaced this one
            Assets.Scripts.GOAP.Systems.LaserAlertSystem.TryClearWorldKeyForAnchor(data.AnchorSnapshot);
        }

        // This method is called when the action is stopped
        // This method is optional and can be removed
        public override void Stop(IMonoAgent agent, Data data)
        {
        }

        // This method is called when the action is completed or stopped
        // This method is optional and can be removed
        public override void End(IMonoAgent mono, Data data)
        {
            var speedController = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.GuardDetectionSpeedController>();
            
            // Reset speed controller state
            if (speedController != null)
            {
                speedController.SetInvestigatingLaser(false);
            }
            
            // Ensure alert is cleared when action ends
            LaserAlertSystem.ClearWorldKey();
        }

        // The action class itself must be stateless!
        // All data should be stored in the data class
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public Transform AnchorSnapshot { get; set; }
            public bool HasArrived { get; set; }
            public float SearchTime { get; set; }
            public Vector3 LaserSearchPosition { get; set; }
            public bool PlayerLocationCaptured { get; set; }
            public Vector3 CapturedPlayerLocation { get; set; }
            public bool SearchComplete { get; set; }
        }
    }
}

