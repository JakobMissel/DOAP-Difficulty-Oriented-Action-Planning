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

        public override void Created()
        {
        }
        

        // This method is called when the action is started
        // This method is optional and can be removed
        public override void Start(IMonoAgent mono, Data data)
        {
            if (agent == null)
                agent = mono.Transform.GetComponent<NavMeshAgent>();
            if (sight == null)
                sight = mono.Transform.GetComponent<SimpleGuardSightNiko>();

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.updatePosition = true;
            
            // Reset the close range timer when starting pursuit
            data.CloseRangeTimer = 0f;

            Debug.Log($"[PursuitAction] {mono.Transform.name} chasing {data.Target?.Position}");
        }
        
        // This method is called every frame while the action is running
        // This method is required
        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            if (agent == null || data.Target == null || !data.Target.IsValid() || !sight.CanSeePlayer())
                return ActionRunState.Stop;

            // Latest position of the target
            agent.SetDestination(data.Target.Position);

            float dist = Vector3.Distance(mono.Transform.position, data.Target.Position);

            // Use a generous distance check that considers the agent's stopping distance
            float catchDistance = Mathf.Max(1.5f, agent.stoppingDistance + 0.5f);
    
            if (dist <= catchDistance)
            {
                Debug.Log($"[PursuitAction] {mono.Transform.name} Player caught! Distance: {dist:F2}, Catch Distance: {catchDistance:F2}");
                // Set world state that player is caught
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
                // Player is within close range - increment timer
                data.CloseRangeTimer += Time.deltaTime;
                Debug.Log($"[PursuitAction] Player in close range ({dist:F1}m) - Timer: {data.CloseRangeTimer:F1}s/{CLOSE_RANGE_CATCH_TIME}s");
                
                if (data.CloseRangeTimer >= CLOSE_RANGE_CATCH_TIME)
                {
                    Debug.Log($"[PursuitAction] {mono.Transform.name} Player caught by close range timer!");
                    
                    // Set world state that player is caught
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
                // Player is now out of range - decrease timer in incremental steps
                if (data.CloseRangeTimer > 0f)
                {
                    data.CloseRangeTimer -= Time.deltaTime * TIMER_DECAY_SPEED;
                    data.CloseRangeTimer = Mathf.Max(0f, data.CloseRangeTimer); // Don't go below 0
                    Debug.Log($"[PursuitAction] Player out of close range ({dist:F1}m) - Timer decaying: {data.CloseRangeTimer:F1}s");
                }
            }

            return ActionRunState.Continue;
        }

        // The action class itself must be stateless
        // All data should be stored in the data class
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float CloseRangeTimer { get; set; } = 0f;
        }
    }
}