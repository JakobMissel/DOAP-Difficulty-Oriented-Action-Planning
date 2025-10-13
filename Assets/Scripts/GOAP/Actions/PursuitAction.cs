using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Core;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("Pursuit-67f97c9c-9d66-42ce-853c-133448c53402")]
    public class PursuitAction : GoapActionBase<PursuitAction.Data>
    {
        private NavMeshAgent agent;
        private SimpleGuardSightNiko sight;

        // Timer constants
        private const float CLOSE_RANGE_DISTANCE = 5.0f;
        private const float CLOSE_RANGE_CATCH_TIME = 5.0f;
        private const float TIMER_DECAY_SPEED = 1.0f;

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
            agent.updatePosition = true;
            
            data.CloseRangeTimer = 0f;

            Debug.Log($"[PursuitAction] {mono.Transform.name} chasing player");
        }
        
        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
                return ActionRunState.Stop;

            // If we can't see the player anymore, stop (InvestigateLastKnownGoal will take over)
            if (!sight.CanSeePlayer())
            {
                Debug.Log("[PursuitAction] Lost sight of player - stopping pursuit");
                return ActionRunState.Stop;
            }

            // Chase the player
            agent.SetDestination(data.Target.Position);
            agent.isStopped = false;
            agent.updateRotation = true;

            float dist = Vector3.Distance(mono.Transform.position, data.Target.Position);

            // Catch distance check
            float catchDistance = Mathf.Max(1.5f, agent.stoppingDistance + 0.5f);
    
            if (dist <= catchDistance)
            {
                Debug.Log($"[PursuitAction] Player caught! Distance: {dist:F2}");
                var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
                if (brain != null)
                {
                    brain.SetPlayerCaught(true);
                }
                
                return ActionRunState.Completed;
            }
            
            // Close range timer logic
            if (dist <= CLOSE_RANGE_DISTANCE)
            {
                data.CloseRangeTimer += Time.deltaTime;
                
                if (data.CloseRangeTimer >= CLOSE_RANGE_CATCH_TIME)
                {
                    Debug.Log($"[PursuitAction] Player caught by close range timer!");
                    
                    var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
                    if (brain != null)
                    {
                        brain.SetPlayerCaught(true);
                    }
                    
                    return ActionRunState.Completed;
                }
            }
            else
            {
                if (data.CloseRangeTimer > 0f)
                {
                    data.CloseRangeTimer -= Time.deltaTime * TIMER_DECAY_SPEED;
                    data.CloseRangeTimer = Mathf.Max(0f, data.CloseRangeTimer);
                }
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
            
            Debug.Log("[PursuitAction] Ended");
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float CloseRangeTimer { get; set; } = 0f;
        }
    }
}