// csharp
using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Core;
using Assets.Scripts.GOAP.Behaviours;
using Assets.Scripts.DDA;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("ClearLastKnown-11630d95-435f-4d5b-8ad2-dcf0edec684f")]
    public class ClearLastKnownAction : GoapActionBase<ClearLastKnownAction.Data>
    {
        public override void Created() { }

        public override void Start(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var sight = mono.Transform.GetComponent<GuardSight>();
            var brain = mono.Transform.GetComponent<BrainBehaviour>();
            var animation = mono.Transform.GetComponent<GuardAnimation>();
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();

            // How long to "check" the last known position once arrived
            // Defaults; will be overridden by BrainBehaviour values if present
            float ClearDuration = 1.5f;
            float ScanAngle = 75f;
            float ScanSweepTime = 1.5f;
            float FallbackArriveDistance = 1.6f;
            float MaxActionDuration = 15f; // Failsafe: auto-complete after this many seconds total

            // Read per-agent scan settings if available
            if (brain != null)
            {
                if (brain.scanDuration > 0f) ClearDuration = brain.scanDuration;
                if (brain.scanAngle > 0f) ScanAngle = brain.scanAngle;
                if (brain.scanSweepTime > 0f) ScanSweepTime = brain.scanSweepTime;
                if (brain.arriveDistance > 0f) FallbackArriveDistance = brain.arriveDistance;
                // Allow customization of max action duration per guard if needed
                // (BrainBehaviour would need a maxClearDuration field for this)
            }

            // Store settings in data so Perform can access them
            data.ClearDuration = ClearDuration;
            data.ScanAngle = ScanAngle;
            data.ScanSweepTime = ScanSweepTime;
            data.FallbackArriveDistance = FallbackArriveDistance;
            data.MaxActionDuration = MaxActionDuration;
            
            data.Timer = 0f;
            data.ArrivalTimer = 0f;
            data.ActionStartTime = Time.time;
            data.ScanningInitialized = false;
            data.BaseYaw = 0f;
            data.LookTransform = null;
            data.BaseLocalEuler = Vector3.zero;
            data.HasAcknowledgedReEntry = false;

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            if (data.Target != null && data.Target.IsValid())
            {
                agent.SetDestination(data.Target.Position);
                audio?.PlayWalkLoop();
                
                // Prefer rotating the eyes during scan; fallback to the root transform
                var eyes = (sight != null) ? sight.Eyes : null;
                data.LookTransform = eyes != null ? eyes : mono.Transform;
                data.BaseLocalEuler = data.LookTransform.localEulerAngles;
                
                Debug.Log($"[ClearLastKnownAction] {mono.Transform.name} heading to last known position {data.Target.Position}");
            }
            else
            {
                Debug.LogWarning($"[ClearLastKnownAction] {mono.Transform.name} no valid last-known target provided.");
            }
        }

        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var sight = mono.Transform.GetComponent<GuardSight>();
            var brain = mono.Transform.GetComponent<BrainBehaviour>();
            var animation = mono.Transform.GetComponent<GuardAnimation>();
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();

            // Failsafe: if action has been running too long, force completion
            float elapsedTime = Time.time - data.ActionStartTime;
            if (elapsedTime >= data.MaxActionDuration)
            {
                Debug.LogWarning($"[ClearLastKnownAction] {mono.Transform.name} exceeded max duration ({data.MaxActionDuration}s), force-completing (likely unreachable target).");
                if (brain != null)
                    brain.ClearLastKnownPlayerPosition();
                return ActionRunState.Completed;
            }
            
            if (sight != null && sight.PlayerSpotted())
            {
                Debug.Log($"[ClearLastKnownAction] {mono.Transform.name} player spotted again, aborting clear.");
                return ActionRunState.Stop;
            }

            data.Timer += Time.deltaTime;

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return ActionRunState.Stop;

            if (data.Target == null || !data.Target.IsValid())
            {
                if (brain != null && brain.HasLastKnownPosition)
                {
                    Debug.Log($"[ClearLastKnownAction] {mono.Transform.name} target invalid, clearing last-known state anyway.");
                    brain.ClearLastKnownPlayerPosition();
                    return ActionRunState.Completed;
                }

                return ActionRunState.Stop;
            }

            // Check if player re-entered NavMesh - if so, update target to their landing position
            if (brain != null && brain.PlayerReEnteredNavMesh && !data.HasAcknowledgedReEntry)
            {
                data.HasAcknowledgedReEntry = true;
                Debug.Log($"[ClearLastKnownAction] {mono.Transform.name} detected player re-entry at {brain.PlayerNavMeshReEntryPosition}. Updating destination.");
                
                // Reset scanning state since we have a new target
                data.ScanningInitialized = false;
                data.ArrivalTimer = 0f;
            }

            // Compute arrival using XZ distance and NavMeshAgent remainingDistance
            float arriveDist = Mathf.Max(agent.stoppingDistance, data.FallbackArriveDistance);
            Vector3 currentTargetPos = (brain != null && brain.HasLastKnownPosition) ? brain.LastKnownPlayerPosition : data.Target.Position;

            // Update GOAP target object too
            if (data.Target is PositionTarget posT)
                posT.SetPosition(currentTargetPos);

            Vector3 a = mono.Transform.position; a.y = 0f;
            Vector3 b = currentTargetPos; b.y = 0f;
            float dist = Vector3.Distance(a, b);
            bool arrivedByNavmesh = !agent.pathPending && agent.remainingDistance <= arriveDist;
            bool arrivedByDistance = dist <= arriveDist;
            bool arrived = arrivedByNavmesh || arrivedByDistance;

            // If the follow window is still active, keep updating destination
            if (brain != null && brain.IsLastKnownFollowActive)
            {
                agent.updateRotation = true;
                agent.isStopped = false;
                agent.SetDestination(currentTargetPos);
                audio?.PlayWalkLoop();

                data.ScanningInitialized = false;
                data.ArrivalTimer = 0f;
                return ActionRunState.Continue;
            }

            if (!arrived)
            {
                agent.updateRotation = true;
                agent.isStopped = false;
                agent.SetDestination(currentTargetPos);
                audio?.PlayWalkLoop();

                return ActionRunState.Continue;
            }

            // Arrived: stop and run scan pattern
            agent.isStopped = true;
            audio?.StopWalkLoop();

            if (!data.ScanningInitialized)
            {
                // Force Search animation ONCE when first arriving
                var animController = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.GuardAnimationController>();
                if (animController != null)
                {
                    animController.ForceSearch();
                    Debug.Log($"[ClearLastKnownAction] {mono.Transform.name} arrived, forcing Search animation for scanning");
                }
                else if (animation != null)
                {
                    // Fallback if controller not present
                    animation.Search();
                }

                data.BaseYaw = (data.LookTransform != null ? data.LookTransform.eulerAngles.y : mono.Transform.eulerAngles.y);
                data.ScanningInitialized = true;
                agent.updateRotation = false;
                data.Timer = 0f;
                Debug.Log($"[ClearLastKnownAction] {mono.Transform.name} arrived, starting scan.");
            }

            // Oscillate yaw around base yaw
            float t = data.ArrivalTimer;
            float phase = Mathf.Sin(t / data.ScanSweepTime * Mathf.PI * 2f);
            float targetYaw = data.BaseYaw + phase * data.ScanAngle;

            if (data.LookTransform != null)
            {
                Vector3 eulers = data.BaseLocalEuler;
                eulers.y = targetYaw;
                data.LookTransform.localEulerAngles = eulers;
            }
            else
            {
                Vector3 eulers = mono.Transform.eulerAngles;
                eulers.y = targetYaw;
                mono.Transform.eulerAngles = eulers;
            }

            data.ArrivalTimer += Time.deltaTime;

            if (data.ArrivalTimer >= data.ClearDuration)
            {
                Debug.Log($"[ClearLastKnownAction] {mono.Transform.name} scan complete, clearing last-known.");
                if (brain != null)
                    brain.ClearLastKnownPlayerPosition();
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        public override void Complete(IMonoAgent mono, Data data)
        {
            // Track successful evasion - player escaped from this guard
            // This is only called when the action completes successfully (not when interrupted)
            int guardId = mono.Transform.GetInstanceID();
            EvasionTracker.EvasionSuccessful(guardId);

            Debug.Log($"[ClearLastKnownAction] {mono.Transform.name} - Guard has given up pursuit! Player successfully evaded.");
        }

        public override void End(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var sight = mono.Transform.GetComponent<GuardSight>();
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();
            var animController = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.GuardAnimationController>();

            // Get guard ID for patrol reset (needed later in this method)
            int guardId = mono.Transform.GetInstanceID();

            // Re-enable NavMeshAgent rotation
            if (agent != null)
                agent.updateRotation = true;

            // Reset the eyes to forward-facing (local rotation zero)
            if (data.LookTransform != null && sight != null && sight.Eyes != null)
            {
                // Reset eyes to default forward position
                data.LookTransform.localEulerAngles = Vector3.zero;
                Debug.Log($"[ClearLastKnownAction] {mono.Transform.name} scan ended, resetting eyes to forward.");
            }
            audio?.StopWalkLoop();

            // Clear forced animation state to resume velocity-based animations
            if (animController != null)
            {
                animController.ClearForcedState();
            }
            
            // Mark that this guard should reset to closest waypoint when returning to patrol
            var patrolAction = typeof(PatrolAction);
            
            // Access the static dictionary via reflection to set the reset flag
            var fieldInfo = patrolAction.GetField("GuardNeedsReset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (fieldInfo != null)
            {
                var dict = fieldInfo.GetValue(null) as System.Collections.Generic.Dictionary<int, bool>;
                if (dict != null)
                {
                    dict[guardId] = true;
                    Debug.Log($"[ClearLastKnownAction] {mono.Transform.name} marked to reset patrol to closest waypoint");
                }
            }
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float Timer { get; set; }
            public float ArrivalTimer { get; set; }
            public bool ScanningInitialized { get; set; }
            public float BaseYaw { get; set; }
            public Transform LookTransform { get; set; }
            public Vector3 BaseLocalEuler { get; set; }
            
            // Store per-guard settings
            public float ClearDuration { get; set; }
            public float ScanAngle { get; set; }
            public float ScanSweepTime { get; set; }
            public float FallbackArriveDistance { get; set; }
            public float MaxActionDuration { get; set; }
            public float ActionStartTime { get; set; }
            
            // Track if guard has acknowledged player re-entry to NavMesh
            public bool HasAcknowledgedReEntry { get; set; }
        }
    }
}
