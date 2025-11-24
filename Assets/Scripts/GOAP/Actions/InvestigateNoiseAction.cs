// Assets/Scripts/GOAP/Actions/InvestigateNoiseAction.cs
using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Core;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("InvestigateNoise-73e3c275-b47d-48a5-bcdf-9e503738a046")]
    public class InvestigateNoiseAction : GoapActionBase<InvestigateNoiseAction.Data>
    {
        private const float INVESTIGATION_DURATION = 2.0f; // How long to stay at noise location
        private const float CONFUSION_PAUSE_DURATION = 1.5f; // How long to pause in confusion before moving
        private const float NAVMESH_SAMPLE_DISTANCE = 10f; // Max distance to search for valid NavMesh position

        public override void Created()
        {
        }
        
        /// <summary>
        /// Finds the closest valid position on the NavMesh to the target position.
        /// Returns true if a valid position was found, false otherwise.
        /// </summary>
        private bool TryGetClosestNavMeshPosition(Vector3 targetPosition, out Vector3 closestPosition)
        {
            // Try to find a valid NavMesh position near the target
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, NAVMESH_SAMPLE_DISTANCE, NavMesh.AllAreas))
            {
                closestPosition = hit.position;
                return true;
            }
            
            closestPosition = targetPosition;
            return false;
        }

        public override void Start(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var animation = mono.Transform.GetComponent<GuardAnimation>();
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();
            var speedController = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.GuardDetectionSpeedController>();

            // Notify speed controller that we're investigating noise
            if (speedController != null)
            {
                speedController.SetInvestigatingNoise(true);
            }

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            // Forcefully stop any current movement
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            agent.updateRotation = false; // We'll manually rotate towards the noise
            
            // Initialize state for the confused pause phase
            data.StartDelay = 0.1f;
            data.InvestigationTime = 0f;
            data.ConfusionPauseTime = 0f;
            data.IsInConfusionPhase = true;
            data.HasStartedMoving = false;
            
            // Find the closest valid NavMesh position to the noise
            if (data.Target != null && data.Target.IsValid())
            {
                if (TryGetClosestNavMeshPosition(data.Target.Position, out Vector3 validPosition))
                {
                    data.ValidNavMeshPosition = validPosition;
                    float distance = Vector3.Distance(data.Target.Position, validPosition);
                    if (distance > 0.5f)
                    {
                        Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} noise at {data.Target.Position} is off NavMesh. Using closest valid position: {validPosition} (distance: {distance:F2}m)");
                    }
                    else
                    {
                        Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} noise at {data.Target.Position} is on NavMesh");
                    }
                }
                else
                {
                    Debug.LogWarning($"[InvestigateNoiseAction] {mono.Transform.name} could not find valid NavMesh position near {data.Target.Position}!");
                    data.ValidNavMeshPosition = mono.Transform.position; // Fallback to guard's current position
                }
            }
            
            // Trigger Search animation for the "huh?" confusion phase
            if (animation != null)
            {
                animation.Search();
            }
            audio?.PlayGuardHuh();

            Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} STARTING - heard noise at {data.Target?.Position}");
        }

        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var sight = mono.Transform.GetComponent<GuardSight>();
            var animation = mono.Transform.GetComponent<GuardAnimation>();
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();
            
            // If guard fully spots the player during investigation, abort noise investigation and pursue
            if (sight != null && sight.PlayerSpotted())
            {
                Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} spotted player during investigation - aborting noise investigation!");
                var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
                if (brain != null)
                {
                    brain.ClearNoise(); // Clear whatever noise was being investigated
                }
                return ActionRunState.Stop;
            }
            
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
            {
                Debug.LogWarning($"[InvestigateNoiseAction] {mono.Transform.name} no valid target or agent!");
                return ActionRunState.Stop;
            }

            // Handle start delay
            if (data.StartDelay > 0f)
            {
                data.StartDelay -= Time.deltaTime;
                return ActionRunState.Continue;
            }

            // Phase 1: Confusion phase - rotate towards noise and pause with "huh?" animation
            if (data.IsInConfusionPhase)
            {
                // Calculate direction to original noise position (for visual accuracy)
                Vector3 directionToNoise = (data.Target.Position - mono.Transform.position).normalized;
                directionToNoise.y = 0; // Keep rotation horizontal
                
                if (directionToNoise.magnitude > 0.01f)
                {
                    // Smoothly rotate towards the noise
                    Quaternion targetRotation = Quaternion.LookRotation(directionToNoise);
                    mono.Transform.rotation = Quaternion.Slerp(
                        mono.Transform.rotation, 
                        targetRotation, 
                        Time.deltaTime * 5f // Rotation speed
                    );
                }
                
                // Animation: Searching during "huh?" pause
                if (animation != null)
                {
                    animation.Search();
                }
                
                // Wait in confusion
                data.ConfusionPauseTime += Time.deltaTime;
                
                if (data.ConfusionPauseTime >= CONFUSION_PAUSE_DURATION)
                {
                    Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} finished confusion pause, now moving to investigate at {data.ValidNavMeshPosition}");
                    data.IsInConfusionPhase = false;
                    
                    // Start movement towards VALID NAVMESH position
                    agent.updateRotation = true;
                    agent.updatePosition = true;
                    agent.isStopped = false;
                    agent.SetDestination(data.ValidNavMeshPosition);
                    data.HasStartedMoving = true;
                }
                
                return ActionRunState.Continue;
            }

            // Phase 2: Move to the noise location (using valid NavMesh position)
            if (!data.HasStartedMoving)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.updatePosition = true;
                agent.SetDestination(data.ValidNavMeshPosition);
                data.HasStartedMoving = true;
                
                Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} now moving to noise at {data.ValidNavMeshPosition}");
            }

            // Keep updating destination to valid NavMesh position
            agent.SetDestination(data.ValidNavMeshPosition);

            float dist = Vector3.Distance(mono.Transform.position, data.ValidNavMeshPosition);

            // Phase 3: Arrived at noise location - investigate
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                // Stop at the location and investigate
                agent.isStopped = true;
                data.InvestigationTime += Time.deltaTime;
                
                // Animation: Search while investigating
                if (animation != null)
                {
                    animation.Search();
                }
                audio?.StopWalkLoop();

                if (data.InvestigationTime < INVESTIGATION_DURATION)
                {
                    Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} investigating... {data.InvestigationTime:F1}s / {INVESTIGATION_DURATION}s");
                }

                // After investigating for the duration, complete
                if (data.InvestigationTime >= INVESTIGATION_DURATION)
                {
                    var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
                    if (brain != null)
                    {
                        // Clear noise based on what type it was
                        if (brain.IsPlayerNoise)
                        {
                            Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} finished investigating PLAYER noise. Nothing found!");
                            brain.ClearPlayerNoise();
                        }
                        else
                        {
                            Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} finished investigating DISTRACTION noise. Nothing found!");
                            brain.ClearDistractionNoise();
                        }
                    }
                    
                    return ActionRunState.Completed;
                }
            }
            else
            {
                // Still moving to noise location - only play walk animation if velocity > 0
                Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} moving to noise source... Distance: {dist:F1}m");
                
                if (animation != null)
                {
                    if (agent.velocity.magnitude > 0.1f)
                    {
                        animation.Walk();
                        audio?.PlayWalkLoop();
                    }
                    else
                    {
                        animation.Search();
                        audio?.StopWalkLoop();
                    }
                }
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
            var speedController = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.GuardDetectionSpeedController>();
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();
            var animation = mono.Transform.GetComponent<GuardAnimation>();
            
            // Reset speed controller state
            if (speedController != null)
            {
                speedController.SetInvestigatingNoise(false);
            }
            
            // Ensure noise is cleared (handles both player and distraction noise)
            if (brain != null)
            {
                brain.ClearNoise();
            }
            
            // Reset animation to idle
            if (animation != null)
            {
                animation.Idle();
            }
            
            // Resume normal movement state
            if (agent != null)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.updatePosition = true;
            }
            audio?.StopWalkLoop();
 
            Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} ending investigation - ready to resume patrol.");
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float InvestigationTime { get; set; }
            public float StartDelay { get; set; }
            public float ConfusionPauseTime { get; set; }
            public bool IsInConfusionPhase { get; set; }
            public bool HasStartedMoving { get; set; }
            public Vector3 ValidNavMeshPosition { get; set; }
         }
     }
 }
