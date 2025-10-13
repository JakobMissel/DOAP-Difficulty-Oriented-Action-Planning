using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Core;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("ClearLastKnown-11630d95-435f-4d5b-8ad2-dcf0edec684f")]
    public class ClearLastKnownAction : GoapActionBase<ClearLastKnownAction.Data>
    {
        private NavMeshAgent agent;
        private SimpleGuardSightNiko sight;

        private const float SEARCH_RADIUS = 1.5f;
        private const float LOOK_DURATION = 1.5f;
        private const float LOOK_ANGLE = 75f;

        public enum SearchState
        {
            MovingToLocation,
            LookingLeft,
            LookingRight,
            SearchComplete
        }

        public override void Created() { }

        public override void Start(IMonoAgent mono, Data data)
        {
            if (agent == null)
                agent = mono.Transform.GetComponent<NavMeshAgent>();
            if (sight == null)
                sight = mono.Transform.GetComponent<SimpleGuardSightNiko>();

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            agent.isStopped = false;
            agent.updateRotation = true;
            
            data.SearchState = SearchState.MovingToLocation;
            data.SearchTimer = 0f;
            data.InitialRotation = Quaternion.identity;

            Debug.Log($"[InvestigateLastKnownAction] {mono.Transform.name} investigating {data.Target?.Position}");
        }

        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
                return ActionRunState.Stop;

            // If we spot the player during investigation, stop and let PursuitGoal take over
            if (sight.CanSeePlayer())
            {
                Debug.Log("[ClearLastKnownAction] Spotted player during investigation!");
                return ActionRunState.Stop; // Will switch to PursuitGoal
            }

            switch (data.SearchState)
            {
                case SearchState.MovingToLocation:
                    return HandleMovingToLocation(mono, data);
        
                case SearchState.LookingLeft:
                    return HandleLookingLeft(mono, data);
        
                case SearchState.LookingRight:
                    return HandleLookingRight(mono, data);
        
                case SearchState.SearchComplete:
                    Debug.Log("[ClearLastKnownAction] Search complete - clearing last known position");
            
                    var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
                    if (brain != null)
                    {
                        brain.ClearLastKnownPlayerPosition();
                    }
            
                    // The effects (IsAlert-- and HasLastKnownPosition--) will be applied by GOAP
                    // This makes IsAlert = 0, which satisfies ClearLastKnownGoal
                    return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        private IActionRunState HandleMovingToLocation(IMonoAgent mono, Data data)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return ActionRunState.Stop;
            agent.SetDestination(data.Target.Position);
            agent.isStopped = false;
            agent.updateRotation = true;

            float dist = Vector3.Distance(mono.Transform.position, data.Target.Position);
            Debug.Log($"[InvestigateLastKnownAction] Moving to location - Distance: {dist:F2}m");

            if (dist <= SEARCH_RADIUS)
            {
                Debug.Log("[InvestigateLastKnownAction] Reached location - starting search");
                agent.isStopped = true;
                agent.updateRotation = false;
                
                data.InitialRotation = mono.Transform.rotation;
                data.SearchState = SearchState.LookingLeft;
                data.SearchTimer = 0f;
            }

            return ActionRunState.Continue;
        }

        private IActionRunState HandleLookingLeft(IMonoAgent mono, Data data)
        {
            data.SearchTimer += Time.deltaTime;

            Quaternion targetRotation = data.InitialRotation * Quaternion.Euler(0, -LOOK_ANGLE, 0);
            mono.Transform.rotation = Quaternion.Slerp(mono.Transform.rotation, targetRotation, Time.deltaTime * 2f);

            Debug.Log($"[InvestigateLastKnownAction] Looking left - {data.SearchTimer:F1}s/{LOOK_DURATION}s");

            if (sight.CanSeePlayer())
            {
                Debug.Log("[InvestigateLastKnownAction] Found player while looking left!");
                agent.isStopped = false;
                agent.updateRotation = true;
                return ActionRunState.Stop; // Switch to PursuitGoal
            }

            if (data.SearchTimer >= LOOK_DURATION)
            {
                data.SearchState = SearchState.LookingRight;
                data.SearchTimer = 0f;
            }

            return ActionRunState.Continue;
        }

        private IActionRunState HandleLookingRight(IMonoAgent mono, Data data)
        {
            data.SearchTimer += Time.deltaTime;

            Quaternion targetRotation = data.InitialRotation * Quaternion.Euler(0, LOOK_ANGLE, 0);
            mono.Transform.rotation = Quaternion.Slerp(mono.Transform.rotation, targetRotation, Time.deltaTime * 2f);

            Debug.Log($"[InvestigateLastKnownAction] Looking right - {data.SearchTimer:F1}s/{LOOK_DURATION}s");

            if (sight.CanSeePlayer())
            {
                Debug.Log("[InvestigateLastKnownAction] Found player while looking right!");
                agent.isStopped = false;
                agent.updateRotation = true;
                return ActionRunState.Stop; // Switch to PursuitGoal
            }

            if (data.SearchTimer >= LOOK_DURATION)
            {
                data.SearchState = SearchState.SearchComplete;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
            }
            
            Debug.Log("[InvestigateLastKnownAction] Ended");
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public SearchState SearchState { get; set; } = SearchState.MovingToLocation;
            public float SearchTimer { get; set; } = 0f;
            public Quaternion InitialRotation { get; set; }
        }
    }
}