using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Core;
using Assets.Scripts.GOAP.Systems;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("Patrol-7a350849-db29-4d18-9bb8-70adcb964707")]
    public class PatrolAction : GoapActionBase<PatrolAction.Data>
    {
        // Track if this is the first time patrol is starting (for closest waypoint initialization)
        private static readonly System.Collections.Generic.Dictionary<int, bool> GuardNeedsReset = new System.Collections.Generic.Dictionary<int, bool>();
        
        // Stuck detection tracking per guard
        private static readonly System.Collections.Generic.Dictionary<int, StuckDetectionData> StuckTracking = new System.Collections.Generic.Dictionary<int, StuckDetectionData>();
        
        private class StuckDetectionData
        {
            public Vector3 LastPosition;
            public float TimeAtPosition;
            public float StuckThreshold = 3.0f; // Seconds before considering guard stuck
            public float MinMovementDistance = 0.5f; // Minimum distance to consider guard has moved
        }
        
        public override void Created()
        {
            Debug.Log("[PatrolAction] Created");
        }

        public override void Start(IMonoAgent mono, Data data)
        {
            Debug.Log($"[PatrolAction] Start called for {mono.Transform.name}");

            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var animation = mono.Transform.GetComponent<GuardAnimation>();
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                Debug.LogWarning($"[PatrolAction] {mono.Transform.name} - NavMeshAgent invalid! agent={agent != null}, enabled={agent?.enabled}, onNavMesh={agent?.isOnNavMesh}");
                return;
            }

            // Animation handled by GuardAnimationController based on velocity
            audio?.PlayWalkLoop();

            // Ensure flashlight is on when patrolling (defensive - in case recharge didn't turn it back on)
            SetFlashlightActive(mono.Transform, true);

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.updatePosition = true;

            // If a laser alert is active, abort starting patrol and let the planner switch immediately
            if (LaserAlertSystem.WorldKeyActive)
                return;

            var route = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.PatrolRouteBehaviour>();
            if (route == null)
            {
                Debug.LogWarning($"[PatrolAction] {mono.Transform.name} has no PatrolRouteBehaviour!");
                return;
            }

            int guardId = mono.Transform.GetInstanceID();
            
            // Initialize stuck detection for this guard
            if (!StuckTracking.ContainsKey(guardId))
            {
                StuckTracking[guardId] = new StuckDetectionData
                {
                    LastPosition = mono.Transform.position,
                    TimeAtPosition = Time.time
                };
            }
            else
            {
                // Reset stuck detection when starting patrol
                StuckTracking[guardId].LastPosition = mono.Transform.position;
                StuckTracking[guardId].TimeAtPosition = Time.time;
            }
            
            // Check if this guard needs to reset to closest waypoint (first time or after pursuit)
            bool needsReset = !GuardNeedsReset.ContainsKey(guardId) || GuardNeedsReset[guardId];
            
            if (needsReset)
            {
                // Reset to closest waypoint when starting patrol for first time or resuming after pursuit
                route.ResetToClosestWaypoint();
                GuardNeedsReset[guardId] = false; // Mark as no longer needing reset
                Debug.Log($"[PatrolAction] {mono.Transform.name} reset to closest waypoint for patrol");
            }
            
            // Get the current waypoint (either the new closest one, or the next in sequence)
            var currentWaypoint = route.GetCurrent();
            if (currentWaypoint != null)
            {
                Vector3 targetPos = currentWaypoint.position;
                
                // Update the data target with the current position
                if (data.Target is PositionTarget posTarget)
                {
                    posTarget.SetPosition(targetPos);
                }
                else
                {
                    data.Target = new PositionTarget(targetPos);
                }
                
                // Set the NavMeshAgent destination
                agent.SetDestination(targetPos);
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.updatePosition = true;
                
                Debug.Log($"[PatrolAction] {mono.Transform.name} patrolling to waypoint {route.GetCurrentIndex()} at {targetPos}");
            }
            else
            {
                Debug.LogWarning($"[PatrolAction] {mono.Transform.name} could not get current waypoint!");
            }
        }

        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            // Debug log once per second to avoid spam
            int guardId = mono.Transform.GetInstanceID();
            if (!StuckTracking.ContainsKey(guardId) || Time.time - StuckTracking[guardId].TimeAtPosition > 1.0f)
            {
                if (StuckTracking.ContainsKey(guardId))
                    StuckTracking[guardId].TimeAtPosition = Time.time;
                Debug.Log($"[PatrolAction] Perform called for {mono.Transform.name}");
            }

            // Immediately interrupt patrol when a laser alert is active so we can replan to GoToLaser
            if (LaserAlertSystem.WorldKeyActive)
                return ActionRunState.Stop;
            
            // Interrupt patrol if distraction noise is heard
            var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
            if (brain != null && brain.HasHeardDistractionNoise)
            {
                Debug.Log($"[PatrolAction] {mono.Transform.name} heard distraction noise - stopping patrol to investigate!");
                return ActionRunState.Stop;
            }

            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                Debug.LogWarning($"[PatrolAction] {mono.Transform.name} NavMeshAgent became invalid!");
                return ActionRunState.Stop;
            }

            if (!TryGetValidTargetPosition(data, out var pos))
            {
                Debug.LogWarning($"[PatrolAction] {mono.Transform.name} lost valid target!");
                return ActionRunState.Stop;
            }
            
            
            // Check for stuck detection
            if (StuckTracking.ContainsKey(guardId))
            {
                var stuckData = StuckTracking[guardId];
                var energyBehaviour = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.EnergyBehaviour>();
                
                // Only check for stuck if guard has energy and is not recharging
                bool hasEnergy = energyBehaviour == null || (!energyBehaviour.IsRecharging && energyBehaviour.CurrentEnergy > 0f);
                
                if (hasEnergy)
                {
                    Vector3 currentPos = mono.Transform.position;
                    float distanceMoved = Vector3.Distance(currentPos, stuckData.LastPosition);
                    
                    if (distanceMoved < stuckData.MinMovementDistance)
                    {
                        // Guard hasn't moved significantly
                        float timeStuck = Time.time - stuckData.TimeAtPosition;
                        
                        if (timeStuck >= stuckData.StuckThreshold)
                        {
                            // Guard is stuck! Reset route to closest waypoint
                            Debug.LogWarning($"[PatrolAction] {mono.Transform.name} appears stuck (stationary for {timeStuck:F1}s). Resetting route!");
                            
                            var route = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.PatrolRouteBehaviour>();
                            if (route != null)
                            {
                                route.ResetToClosestWaypoint();
                                var newWaypoint = route.GetCurrent();
                                if (newWaypoint != null)
                                {
                                    Vector3 newTargetPos = newWaypoint.position;
                                    
                                    if (data.Target is PositionTarget posTarget)
                                    {
                                        posTarget.SetPosition(newTargetPos);
                                    }
                                    else
                                    {
                                        data.Target = new PositionTarget(newTargetPos);
                                    }
                                    
                                    agent.SetDestination(newTargetPos);
                                    Debug.Log($"[PatrolAction] {mono.Transform.name} reset to waypoint {route.GetCurrentIndex()} at {newTargetPos}");
                                }
                            }
                            
                            // Reset stuck tracking
                            stuckData.LastPosition = currentPos;
                            stuckData.TimeAtPosition = Time.time;
                        }
                    }
                    else
                    {
                        // Guard has moved, update position and reset timer
                        stuckData.LastPosition = currentPos;
                        stuckData.TimeAtPosition = Time.time;
                    }
                }
            }

            // Check horizontal distance only (ignore Y-level)
            Vector3 agentPosFlat = new Vector3(mono.Transform.position.x, 0, mono.Transform.position.z);
            Vector3 targetPosFlat = new Vector3(pos.x, 0, pos.z);
            float horizontalDistance = Vector3.Distance(agentPosFlat, targetPosFlat);

            // Consider arrived if within stopping distance horizontally
            if (horizontalDistance <= agent.stoppingDistance + 0.5f)
            {
                Debug.Log($"[PatrolAction] {mono.Transform.name} reached waypoint (horizontal dist: {horizontalDistance:F2}m)");
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();
            audio?.StopWalkLoop();
            // Safety check: if the agent is being destroyed (scene unloading), skip
            if (mono == null || mono.Transform == null)
                return;

            Debug.Log($"[PatrolAction] {mono.Transform.name} ending action, advancing route.");

            var route = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.PatrolRouteBehaviour>();
            if (route != null)
            {
                route.Advance();
            }
            else
            {
                Debug.LogWarning($"[PatrolAction] {mono.Transform.name} has no PatrolRouteBehaviour!");
            }
        }

        private bool TryGetValidTargetPosition(Data data, out Vector3 pos)
        {
            pos = default;

            if (data?.Target == null || !data.Target.IsValid())
                return false;

            pos = data.Target.Position;
            return true;
        }

        /// <summary>
        /// Enables or disables the guard's flashlight (spotlight)
        /// </summary>
        private void SetFlashlightActive(Transform guardTransform, bool active)
        {
            // Search for Light components in children (typically the flashlight/spotlight)
            Light[] lights = guardTransform.GetComponentsInChildren<Light>();

            if (lights != null && lights.Length > 0)
            {
                foreach (Light light in lights)
                {
                    light.enabled = active;
                }
            }
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}