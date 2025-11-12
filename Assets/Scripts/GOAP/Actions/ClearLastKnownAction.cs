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
        public override void Created() { }

        public override void Start(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var sight = mono.Transform.GetComponent<GuardSight>();
            var brain = mono.Transform.GetComponent<BrainBehaviour>();

            // How long to "check" the last known position once arrived
            // Defaults; will be overridden by BrainBehaviour values if present
            float ClearDuration = 1.5f;
            float ScanAngle = 75f;
            float ScanSweepTime = 1.5f;
            float FallbackArriveDistance = 1.6f;

            // Read per-agent scan settings if available
            if (brain != null)
            {
                if (brain.scanDuration > 0f) ClearDuration = brain.scanDuration;
                if (brain.scanAngle > 0f) ScanAngle = brain.scanAngle;
                if (brain.scanSweepTime > 0f) ScanSweepTime = brain.scanSweepTime;
                if (brain.arriveDistance > 0f) FallbackArriveDistance = brain.arriveDistance;
            }

            // Store settings in data so Perform can access them
            data.ClearDuration = ClearDuration;
            data.ScanAngle = ScanAngle;
            data.ScanSweepTime = ScanSweepTime;
            data.FallbackArriveDistance = FallbackArriveDistance;
            
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
            
            if (sight != null && sight.CanSeePlayer())
            {
                Debug.Log($"[ClearLastKnownAction] {mono.Transform.name} player seen again, aborting clear.");
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

                data.ScanningInitialized = false;
                data.ArrivalTimer = 0f;
                return ActionRunState.Continue;
            }

            if (!arrived)
            {
                agent.updateRotation = true;
                agent.isStopped = false;
                agent.SetDestination(currentTargetPos);
                return ActionRunState.Continue;
            }

            // Arrived: stop and run scan pattern
            agent.isStopped = true;

            if (!data.ScanningInitialized)
            {
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

        public override void End(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.updateRotation = true;
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
        }
    }
}
