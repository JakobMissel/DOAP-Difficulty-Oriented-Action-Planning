using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Core;
using Assets.Scripts.GOAP.Systems;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("Patrol-7a350849-db29-4d18-9bb8-70adcb964707")]
    public class PatrolAction : GoapActionBase<PatrolAction.Data>
    {
        // Track if this is the first time patrol is starting (for closest waypoint initialization)
        private static readonly System.Collections.Generic.Dictionary<int, bool> GuardNeedsReset = new System.Collections.Generic.Dictionary<int, bool>();
        
        public override void Created()
        {
            Debug.Log("[PatrolAction] Created");
        }

        public override void Start(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                Debug.LogWarning($"[PatrolAction] {mono.Transform.name} has invalid NavMeshAgent!");
                return;
            }

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
            // Immediately interrupt patrol when a laser alert is active so we can replan to GoToLaser
            if (LaserAlertSystem.WorldKeyActive)
                return ActionRunState.Stop;

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

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}