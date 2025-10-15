// csharp
using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Core;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("ClearLastKnown-11630d95-435f-4d5b-8ad2-dcf0edec684f")]
    public class ClearLastKnownAction : GoapActionBase<ClearLastKnownAction.Data>
    {
        private NavMeshAgent agent;
        private SimpleGuardSightNiko sight;
        private BrainBehaviour brain;

        // How long to "check" the last known position once arrived
        // Defaults; will be overridden by BrainBehaviour values if present
        private float ClearDuration = 1.5f;
        private float ScanAngle = 75f;
        private float ScanSweepTime = 1.5f;
        private float FallbackArriveDistance = 1.6f;

        public override void Created() { }

        public override void Start(IMonoAgent mono, Data data)
        {
            if (agent == null)
                agent = mono.Transform.GetComponent<NavMeshAgent>();
            if (sight == null)
                sight = mono.Transform.GetComponent<SimpleGuardSightNiko>();
            if (brain == null)
                brain = mono.Transform.GetComponent<BrainBehaviour>();

            // Read per-agent scan settings if available
            if (brain != null)
            {
                if (brain.scanDuration > 0f) ClearDuration = brain.scanDuration;
                if (brain.scanAngle > 0f) ScanAngle = brain.scanAngle;
                if (brain.scanSweepTime > 0f) ScanSweepTime = brain.scanSweepTime;
                if (brain.arriveDistance > 0f) FallbackArriveDistance = brain.arriveDistance;
            }

            data.Timer = 0f;
            data.ArrivalTimer = 0f;
            data.ScanningInitialized = false;
            data.BaseYaw = 0f;
            data.LookTransform = null;
            data.BaseLocalEuler = Vector3.zero;

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.updatePosition = true;

            if (data.Target != null && data.Target.IsValid())
            {
                agent.SetDestination(data.Target.Position);
                
                // Prefer rotating the eyes during scan; fallback to the root transform
                var eyes = (sight != null) ? sight.Eyes : null;
                data.LookTransform = eyes != null ? eyes : mono.Transform;
                data.BaseLocalEuler = data.LookTransform.localEulerAngles;
                
                Debug.Log($"[ClearLastKnownAction] Heading to last known position {data.Target.Position}");
            }
            else
            {
                Debug.LogWarning("[ClearLastKnownAction] No valid last-known target provided.");
            }
        }

        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            if (sight != null && sight.CanSeePlayer())
            {
                Debug.Log("[ClearLastKnownAction] Player seen again, aborting clear.");
                return ActionRunState.Stop;
            }

            data.Timer += Time.deltaTime;

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return ActionRunState.Stop;

            if (data.Target == null || !data.Target.IsValid())
            {
                if (brain != null && brain.HasLastKnownPosition)
                {
                    Debug.Log("[ClearLastKnownAction] Target invalid, clearing last-known state anyway.");
                    brain.ClearLastKnownPlayerPosition();
                    return ActionRunState.Completed;
                }

                return ActionRunState.Stop;
            }

            // Compute arrival using XZ distance and NavMeshAgent remainingDistance
            float arriveDist = Mathf.Max(agent.stoppingDistance, FallbackArriveDistance);
            // Always use brain's current/frozen last-known for movement to ensure we follow updates reliably
            Vector3 currentTargetPos = (brain != null && brain.HasLastKnownPosition) ? brain.LastKnownPlayerPosition : data.Target.Position;

            // Update GOAP target object too, so Graph Viewer and other systems reflect the latest position
            if (data.Target is PositionTarget posT)
                posT.SetPosition(currentTargetPos);

            Vector3 a = mono.Transform.position; a.y = 0f;
            Vector3 b = currentTargetPos; b.y = 0f;
            float dist = Vector3.Distance(a, b);
            bool arrivedByNavmesh = !agent.pathPending && agent.remainingDistance <= arriveDist;
            bool arrivedByDistance = dist <= arriveDist;
            bool arrived = arrivedByNavmesh || arrivedByDistance;

            // If the follow window is still active, keep updating destination and NEVER start scanning yet
            if (brain != null && brain.IsLastKnownFollowActive)
            {
                agent.updateRotation = true;
                agent.isStopped = false;
                agent.SetDestination(currentTargetPos);

                // Ensure we don't accidentally carry over scan state
                data.ScanningInitialized = false;
                data.ArrivalTimer = 0f;
                return ActionRunState.Continue;
            }

            if (!arrived)
            {
                // Follow window ended, but we haven't reached the frozen spot yet — keep moving
                agent.updateRotation = true;
                agent.isStopped = false;
                agent.SetDestination(currentTargetPos);
                return ActionRunState.Continue;
            }

            // Arrived AND follow window ended: stop and run scan pattern
            agent.isStopped = true;

            if (!data.ScanningInitialized)
            {
                data.BaseYaw = (data.LookTransform != null ? data.LookTransform.eulerAngles.y : mono.Transform.eulerAngles.y);
                data.ScanningInitialized = true;
                agent.updateRotation = false; // manual rotation while scanning
                data.Timer = 0f; // reset since we just started scanning
                Debug.Log("[ClearLastKnownAction] Arrived at frozen last-known. Starting scan.");
            }

            // Oscillate yaw around base yaw
            float t = data.ArrivalTimer;
            float omega = Mathf.PI * 2f / Mathf.Max(0.01f, ScanSweepTime);
            float yawOffset = Mathf.Sin(t * omega) * ScanAngle;

            if (data.LookTransform != null)
            {
                var euler = data.BaseLocalEuler;
                euler.y = euler.y + yawOffset;
                data.LookTransform.localEulerAngles = euler;
            }
            else
            {
                var euler = mono.Transform.eulerAngles;
                euler.y = data.BaseYaw + yawOffset;
                mono.Transform.eulerAngles = euler;
            }

            data.ArrivalTimer += Time.deltaTime;

            if (data.ArrivalTimer >= ClearDuration)
            {
                if (brain != null)
                    brain.ClearLastKnownPlayerPosition();

                Debug.Log("[ClearLastKnownAction] Area cleared after scan. Resuming normal duties.");
                return ActionRunState.Completed;
            }

            // Continue scanning until duration met (no global timeout to allow long walks)
            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
            }

            // Restore look transform rotation if we modified it
            if (data.LookTransform != null)
            {
                data.LookTransform.localEulerAngles = data.BaseLocalEuler;
            }

            data.Timer = 0f;
            data.ArrivalTimer = 0f;
            data.ScanningInitialized = false;
            data.LookTransform = null;
            
            Debug.Log("[ClearLastKnownAction] Ended");
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
        }
    }
}
