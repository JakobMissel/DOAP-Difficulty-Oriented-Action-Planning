using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Core;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("Pursuit-67f97c9c-9d66-42ce-853c-133448c53402")]
    public class PursuitAction : GoapActionBase<PursuitAction.Data>
    {
        // Timer constants
        private const float CLOSE_RANGE_DISTANCE = 5.0f;
        private const float CLOSE_RANGE_CATCH_TIME = 5.0f;
        private const float TIMER_DECAY_SPEED = 1.0f;
        
        // Catch distance - guard must be very close to catch player
        private const float IMMEDIATE_CATCH_DISTANCE = 0.7f; //lower is closer
        
        // Cache player reference statically since it's shared between all guards
        private static Transform cachedPlayer;

        public override void Created() { }

        public override void Start(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.updatePosition = true;
            
            data.CloseRangeTimer = 0f;
        }
        
        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var sight = mono.Transform.GetComponent<SimpleGuardSightNiko>();
            
            if (cachedPlayer == null)
                cachedPlayer = GameObject.FindGameObjectWithTag("Player")?.transform;
            
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

            // Immediate catch if guard is very close (touching the player)
            if (dist <= IMMEDIATE_CATCH_DISTANCE)
            {
                Debug.Log($"[PursuitAction] {mono.Transform.name} caught player at distance {dist:F2}m!");
                
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
            
            // Close range timer logic - if guard stays close for extended time, eventually catch
            if (dist <= CLOSE_RANGE_DISTANCE)
            {
                data.CloseRangeTimer += Time.deltaTime;
                
                if (data.CloseRangeTimer >= CLOSE_RANGE_CATCH_TIME)
                {
                    Debug.Log($"[PursuitAction] {mono.Transform.name} caught player after {data.CloseRangeTimer:F1}s in close range!");
                    
                    var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
                    if (brain != null)
                    {
                        brain.SetPlayerCaught(true);
                    }
                    
                    // Show Game Over UI
                    if (MainMenu.Instance != null)
                    {
                        MainMenu.Instance.ShowGameOverMenu();
                    }
                    
                    return ActionRunState.Completed;
                }
            }
            else
            {
                // Decay timer when not in close range
                data.CloseRangeTimer = Mathf.Max(0f, data.CloseRangeTimer - (Time.deltaTime * TIMER_DECAY_SPEED));
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            // Optional cleanup
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float CloseRangeTimer { get; set; }
        }
    }
}