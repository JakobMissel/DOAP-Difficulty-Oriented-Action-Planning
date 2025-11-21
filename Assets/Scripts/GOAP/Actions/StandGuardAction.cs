using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP
{
    [GoapId("StandGuard-3abae1a9-8686-4be1-a79b-bfc0a95a47e3")]
    public class StandGuardAction : GoapActionBase<StandGuardAction.Data>
    {
        public override void Created()
        {
            Debug.Log("[StandGuardAction] Created() called");
        }
        public override bool IsValid(IActionReceiver agent, Data data)
        {
            // Validate that the target is still valid
            return data.Target != null && data.Target.IsValid();
        }

        public override void Start(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var animation = mono.Transform.GetComponent<GuardAnimation>();
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            // Initialize state
            data.HasArrived = false;
            data.GuardTime = 0f;
            data.LeftAnglePoint = null;
            data.RightAnglePoint = null;
            data.RotatingToRight = true;
            data.InitialRotationDone = false;

            // Start moving towards the guard point
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.updatePosition = true;

            if (data.Target != null && data.Target.IsValid())
            {
                agent.SetDestination(data.Target.Position);
            }

            // Trigger walking animation
            if (animation != null)
            {
                animation.Walk();
            }
            audio?.PlayWalkLoop();

            Debug.Log($"[StandGuardAction] {mono.Transform.name} starting - moving to guard point at {data.Target?.Position}");
        }

        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext context)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var animation = mono.Transform.GetComponent<GuardAnimation>();
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();
            var timerBehaviour = mono.Transform.GetComponent<StandGuardTimerBehaviour>();

            if (timerBehaviour == null)
            {
                Debug.LogError($"[StandGuardAction] {mono.Transform.name} has no StandGuardTimerBehaviour! Adding it now.");
                timerBehaviour = mono.Transform.gameObject.AddComponent<StandGuardTimerBehaviour>();
            }

            if (agent == null || !agent.enabled || !agent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
            {
                Debug.LogWarning($"[StandGuardAction] {mono.Transform.name} validation failed - Agent: {agent != null}, Enabled: {agent?.enabled}, OnNavMesh: {agent?.isOnNavMesh}, Target: {data.Target != null}, TargetValid: {data.Target?.IsValid()}");
                return ActionRunState.Stop;
            }

            // Phase 1: Moving to the guard point
            if (!data.HasArrived)
            {
                float distanceToTarget = Vector3.Distance(mono.Transform.position, data.Target.Position);
                float arrivalThreshold = timerBehaviour.ArrivalThreshold;
                
                // Log every 2 seconds while moving
                if (Time.frameCount % 120 == 0)
                {
                    Debug.Log($"[StandGuardAction] {mono.Transform.name} moving to guard point - Distance: {distanceToTarget:F2}m, Threshold: {arrivalThreshold:F2}m");
                }
                
                if (distanceToTarget <= arrivalThreshold)
                {
                    // Arrived at guard point
                    data.HasArrived = true;
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    agent.updateRotation = false; // We'll manually control rotation

                    // Switch to idle animation
                    if (animation != null)
                    {
                        animation.Idle();
                    }
                    audio?.StopWalkLoop();

                    // Find the two nearest angle points
                    FindNearestAnglePoints(mono.Transform.position, data);

                    Debug.Log($"[StandGuardAction] {mono.Transform.name} ARRIVED at guard point, starting {timerBehaviour.GuardDuration}s guard duty");
                }
                
                return ActionRunState.Continue;
            }

            // Phase 2: Standing guard and rotating between angle points
            data.GuardTime += Time.deltaTime;

            // Rotate between the two angle points
            if (data.LeftAnglePoint != null && data.RightAnglePoint != null)
            {
                RotateBetweenPoints(mono.Transform, data, timerBehaviour.RotationSpeed);
            }
            else
            {
                // No angle points found, just stand still
                if (animation != null)
                {
                    animation.Idle();
                }
            }
            
            // Log every 2 seconds during guard duty
            if (Mathf.RoundToInt(data.GuardTime) % 2 == 0 && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[StandGuardAction] {mono.Transform.name} standing guard - Time: {data.GuardTime:F1}/{timerBehaviour.GuardDuration}s");
            }

            // Check if guard duration is complete
            if (data.GuardTime >= timerBehaviour.GuardDuration)
            {
                Debug.Log($"[StandGuardAction] {mono.Transform.name} COMPLETED {timerBehaviour.GuardDuration}s guard duty");
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void Complete(IMonoAgent mono, Data data)
        {
            // Set IsGuarding to 1 (starts the 15-second timer and effect)
            var timerBehaviour = mono.Transform.GetComponent<StandGuardTimerBehaviour>();
            if (timerBehaviour == null)
            {
                timerBehaviour = mono.Transform.gameObject.AddComponent<StandGuardTimerBehaviour>();
            }
            timerBehaviour.StartGuarding();

            Debug.Log($"[StandGuardAction] {mono.Transform.name} completed guard duty - IsGuarding set to 1, 15s cooldown started");
        }

        public override void End(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            
            // Resume normal movement
            if (agent != null)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.updatePosition = true;
            }
        }

        private void FindNearestAnglePoints(Vector3 guardPosition, Data data)
        {
            GameObject[] anglePoints = GameObject.FindGameObjectsWithTag("StandGuardWatchAnglePoint");
            
            if (anglePoints == null || anglePoints.Length < 2)
            {
                Debug.LogWarning("[StandGuardAction] Need at least 2 StandGuardWatchAnglePoint objects in the scene!");
                return;
            }

            // Find the two closest angle points
            GameObject closest1 = null;
            GameObject closest2 = null;
            float dist1 = float.MaxValue;
            float dist2 = float.MaxValue;

            foreach (var point in anglePoints)
            {
                if (point == null) continue;
                
                float dist = Vector3.Distance(guardPosition, point.transform.position);
                
                if (dist < dist1)
                {
                    closest2 = closest1;
                    dist2 = dist1;
                    closest1 = point;
                    dist1 = dist;
                }
                else if (dist < dist2)
                {
                    closest2 = point;
                    dist2 = dist;
                }
            }

            data.LeftAnglePoint = closest1?.transform;
            data.RightAnglePoint = closest2?.transform;

            Debug.Log($"[StandGuardAction] Found angle points: {closest1?.name} and {closest2?.name}");
        }

        private void RotateBetweenPoints(Transform guardTransform, Data data, float rotationSpeed)
        {
            // Determine target point
            Transform targetPoint = data.RotatingToRight ? data.RightAnglePoint : data.LeftAnglePoint;
            
            if (targetPoint == null)
                return;

            // Calculate direction to target point (horizontal only)
            Vector3 directionToTarget = (targetPoint.position - guardTransform.position).normalized;
            directionToTarget.y = 0;

            if (directionToTarget.magnitude < 0.01f)
                return;

            // Calculate target rotation
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            
            // Smoothly rotate towards target
            guardTransform.rotation = Quaternion.Slerp(
                guardTransform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );

            // Check if we've reached the target rotation (within threshold)
            float angleDifference = Quaternion.Angle(guardTransform.rotation, targetRotation);
            if (angleDifference < 5f)
            {
                // Switch direction
                data.RotatingToRight = !data.RotatingToRight;
                string targetName = data.RotatingToRight ? data.RightAnglePoint?.name : data.LeftAnglePoint?.name;
                Debug.Log($"[StandGuardAction] {guardTransform.name} switching rotation direction to {targetName}");
            }
        }

        // The action class itself must be stateless!
        // All data should be stored in the data class
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            
            // Movement tracking
            public bool HasArrived { get; set; }
            
            // Guard timing
            public float GuardTime { get; set; }
            
            // Rotation tracking
            public Transform LeftAnglePoint { get; set; }
            public Transform RightAnglePoint { get; set; }
            public bool RotatingToRight { get; set; }
            public bool InitialRotationDone { get; set; }
        }
    }
}