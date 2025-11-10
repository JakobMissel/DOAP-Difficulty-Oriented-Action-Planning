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
        private Transform player;

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
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.updatePosition = true;
            
            data.CloseRangeTimer = 0f;
        }
        
        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
                return ActionRunState.Stop;

            // Stop if we no longer have visual contact
            bool hasVisual = sight != null && sight.CanSeePlayer();
            if (!hasVisual)
                return ActionRunState.Stop;

            // Chase the player
            agent.SetDestination(data.Target.Position);
            agent.isStopped = false;
            agent.updateRotation = true;

            float dist = Vector3.Distance(mono.Transform.position, data.Target.Position);

            // Catch distance check
            float catchDistance = Mathf.Max(1.5f, agent.stoppingDistance + 0.5f);
    
            if (dist <= catchDistance)
            {
                Debug.Log($"[PursuitAction] Player caught at distance {dist:F2}m!");
                
                var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
                if (brain != null)
                {
                    brain.SetPlayerCaught(true);
                }
                
                // Show Game Over UI
                if (MainMenu.Instance != null)
                {
                    MainMenu.Instance.ShowGameOverMenu();
                    Debug.Log("[PursuitAction] Game Over menu shown!");
                }
                else
                {
                    Debug.LogError("[PursuitAction] MainMenu instance not found! Cannot show game over screen.");
                }
                
                return ActionRunState.Completed;
            }
            
            // Close range timer logic
            if (dist <= CLOSE_RANGE_DISTANCE)
            {
                data.CloseRangeTimer += Time.deltaTime;
                
                if (data.CloseRangeTimer >= CLOSE_RANGE_CATCH_TIME)
                {
                    Debug.Log($"[PursuitAction] Player caught after {data.CloseRangeTimer:F1}s in close range!");
                    
                    var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
                    if (brain != null)
                    {
                        brain.SetPlayerCaught(true);
                    }
                    
                    // Show Game Over UI
                    if (MainMenu.Instance != null)
                    {
                        MainMenu.Instance.ShowGameOverMenu();
                        Debug.Log("[PursuitAction] Game Over menu shown!");
                    }
                    else
                    {
                        Debug.LogError("[PursuitAction] MainMenu instance not found! Cannot show game over screen.");
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
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float CloseRangeTimer { get; set; } = 0f;
        }
    }
}