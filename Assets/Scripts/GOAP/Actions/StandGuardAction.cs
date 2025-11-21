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
        private const float GUARD_DURATION = 10f; // Stand guard for 10 seconds
        private const float ARRIVAL_THRESHOLD = 1.5f; // How close to be considered "at" the guard point
        private const float ROTATION_SPEED = 2f; // Speed of rotation between angle points

        public override void Created()
        {
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

            if (agent == null || !agent.enabled || !agent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
            {
                Debug.LogWarning($"[StandGuardAction] {mono.Transform.name} no valid target or agent!");
                return ActionRunState.Stop;
            }

            // Phase 1: Moving to the guard point
            if (!data.HasArrived)
            {
                float distanceToTarget = Vector3.Distance(mono.Transform.position, data.Target.Position);
                
                if (distanceToTarget <= ARRIVAL_THRESHOLD)
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

                    Debug.Log($"[StandGuardAction] {mono.Transform.name} arrived at guard point, starting {GUARD_DURATION}s guard duty");
                }
                
                return ActionRunState.Continue;
            }

            // Phase 2: Standing guard and rotating between angle points
            data.GuardTime += Time.deltaTime;

            // Rotate between the two angle points
            if (data.LeftAnglePoint != null && data.RightAnglePoint != null)
            {
                RotateBetweenPoints(mono.Transform, data);
            }
            else
            {
                // No angle points found, just stand still
                if (animation != null)
                {
                    animation.Idle();
                }
            }

            // Check if guard duration is complete
            if (data.GuardTime >= GUARD_DURATION)
            {
                Debug.Log($"[StandGuardAction] {mono.Transform.name} completed {GUARD_DURATION}s guard duty");
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void Complete(IMonoAgent mono, Data data)
        {
            // Start the cooldown timer
            var timerBehaviour = mono.Transform.GetComponent<StandGuardTimerBehaviour>();
            if (timerBehaviour == null)
            {
                timerBehaviour = mono.Transform.gameObject.AddComponent<StandGuardTimerBehaviour>();
            }
            timerBehaviour.StartCooldown();

            Debug.Log($"[StandGuardAction] {mono.Transform.name} completed and started cooldown");
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
            GameObject[] anglePoints = GameObject.FindGameObjectsWithTag("StandGuardAnglePoint");
            
            if (anglePoints == null || anglePoints.Length < 2)
            {
                Debug.LogWarning("[StandGuardAction] Need at least 2 StandGuardAnglePoint objects in the scene!");
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

        private void RotateBetweenPoints(Transform guardTransform, Data data)
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
                Time.deltaTime * ROTATION_SPEED
            );

            // Check if we've reached the target rotation (within threshold)
            float angleDifference = Quaternion.Angle(guardTransform.rotation, targetRotation);
            if (angleDifference < 5f)
            {
                // Switch direction
                data.RotatingToRight = !data.RotatingToRight;
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