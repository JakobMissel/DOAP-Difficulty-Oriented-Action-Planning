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
        
        // Search behavior constants
        private const float SEARCH_RADIUS = 2.0f; // How close to last known position to trigger search
        private const float LOOK_DURATION = 1.5f; // How long to look in each direction
        private const float LOOK_ANGLE = 90f; // How far to turn when looking (90 = left/right)

        // Search state enum
        public enum SearchState
        {
            MovingToLastKnown,
            LookingLeft,
            LookingRight,
            SearchComplete
        }
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
            
            // Reset timers and state
            data.CloseRangeTimer = 0f;
            data.SearchState = SearchState.MovingToLastKnown;
            data.SearchTimer = 0f;
            data.InitialRotation = Quaternion.identity;

            Debug.Log($"[PursuitAction] {mono.Transform.name} chasing {data.Target?.Position}");
        }
        
        // This method is called every frame while the action is running
        // This method is required
        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            if (agent == null)
                return ActionRunState.Stop;

            // Check if we can see the player - if so, switch back to normal pursuit
            if (data.Target != null && data.Target.IsValid() && sight.CanSeePlayer())
            {
                return PerformActivePursuit(mono, data);
            }
            else
            {
                return PerformSearch(mono, data);
            }
        }

        private IActionRunState PerformActivePursuit(IMonoAgent mono, Data data)
        {
            // Reset search state if we see the player again
            data.SearchState = SearchState.MovingToLastKnown;
            data.SearchTimer = 0f;
            
            // Latest position of the target
            agent.SetDestination(data.Target.Position);
            agent.isStopped = false;
            agent.updateRotation = true;

            float dist = Vector3.Distance(mono.Transform.position, data.Target.Position);

            // Use a generous distance check that considers the agent's stopping distance
            float catchDistance = Mathf.Max(1.5f, agent.stoppingDistance + 0.5f);
    
            if (dist <= catchDistance)
            {
                Debug.Log($"[PursuitAction] {mono.Transform.name} Player caught! Distance: {dist:F2}");
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
                Debug.Log($"[PursuitAction] Player in close range ({dist:F1}m) - Timer: {data.CloseRangeTimer:F1}s/{CLOSE_RANGE_CATCH_TIME}s");
                
                if (data.CloseRangeTimer >= CLOSE_RANGE_CATCH_TIME)
                {
                    Debug.Log($"[PursuitAction] {mono.Transform.name} Player caught by close range timer!");
                    
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

        private IActionRunState PerformSearch(IMonoAgent mono, Data data)
        {
            if (data.Target == null || !data.Target.IsValid())
            {
                Debug.Log("[PursuitAction] No target to search - stopping");
                
                // Clear last known position
                var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
                if (brain != null)
                {
                    brain.ClearLastKnownPlayerPosition();
                }
                
                return ActionRunState.Stop;
            }

            switch (data.SearchState)
            {
                case SearchState.MovingToLastKnown:
                    return HandleMovingToLastKnown(mono, data);
                
                case SearchState.LookingLeft:
                    return HandleLookingLeft(mono, data);
                
                case SearchState.LookingRight:
                    return HandleLookingRight(mono, data);
                
                case SearchState.SearchComplete:
                    Debug.Log("[PursuitAction] Search complete - player not found");
                    
                    // Clear last known position
                    var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
                    if (brain != null)
                    {
                        brain.ClearLastKnownPlayerPosition();
                    }
                    
                    return ActionRunState.Stop;
            }

            return ActionRunState.Continue;
        }

        private IActionRunState HandleMovingToLastKnown(IMonoAgent mono, Data data)
        {
            agent.SetDestination(data.Target.Position);
            agent.isStopped = false;
            agent.updateRotation = true;

            float dist = Vector3.Distance(mono.Transform.position, data.Target.Position);
            Debug.Log($"[PursuitAction] Moving to last known position - Distance: {dist:F2}m");

            // Check if we can see the player while moving
            if (sight.CanSeePlayer())
            {
                Debug.Log("[PursuitAction] Found player while moving to last known position!");
                return ActionRunState.Continue; // Will be handled by PerformActivePursuit next frame
            }

            // Have we reached the last known position?
            if (dist <= SEARCH_RADIUS)
            {
                Debug.Log("[PursuitAction] Reached last known position - starting search");
                agent.isStopped = true;
                agent.updateRotation = false;
                
                // Store initial rotation for the search
                data.InitialRotation = mono.Transform.rotation;
                data.SearchState = SearchState.LookingLeft;
                data.SearchTimer = 0f;
            }

            return ActionRunState.Continue;
        }

        private IActionRunState HandleLookingLeft(IMonoAgent mono, Data data)
        {
            data.SearchTimer += Time.deltaTime;

            // Smoothly rotate left
            Quaternion targetRotation = data.InitialRotation * Quaternion.Euler(0, -LOOK_ANGLE, 0);
            mono.Transform.rotation = Quaternion.Slerp(mono.Transform.rotation, targetRotation, Time.deltaTime * 2f);

            Debug.Log($"[PursuitAction] Looking left - Timer: {data.SearchTimer:F1}s/{LOOK_DURATION}s");

            // Check if we can see the player
            if (sight.CanSeePlayer())
            {
                Debug.Log("[PursuitAction] Found player while looking left!");
                agent.isStopped = false;
                agent.updateRotation = true;
                data.SearchState = SearchState.MovingToLastKnown; // Reset to pursuit mode
                return ActionRunState.Continue;
            }

            // Finished looking left?
            if (data.SearchTimer >= LOOK_DURATION)
            {
                Debug.Log("[PursuitAction] Finished looking left - now looking right");
                data.SearchState = SearchState.LookingRight;
                data.SearchTimer = 0f;
            }

            return ActionRunState.Continue;
        }

        private IActionRunState HandleLookingRight(IMonoAgent mono, Data data)
        {
            data.SearchTimer += Time.deltaTime;

            // Smoothly rotate right (from initial, not from left position)
            Quaternion targetRotation = data.InitialRotation * Quaternion.Euler(0, LOOK_ANGLE, 0);
            mono.Transform.rotation = Quaternion.Slerp(mono.Transform.rotation, targetRotation, Time.deltaTime * 2f);

            Debug.Log($"[PursuitAction] Looking right - Timer: {data.SearchTimer:F1}s/{LOOK_DURATION}s");

            // Check if we can see the player
            if (sight.CanSeePlayer())
            {
                Debug.Log("[PursuitAction] Found player while looking right!");
                agent.isStopped = false;
                agent.updateRotation = true;
                data.SearchState = SearchState.MovingToLastKnown; // Reset to pursuit mode
                return ActionRunState.Continue;
            }

            // Finished looking right?
            if (data.SearchTimer >= LOOK_DURATION)
            {
                Debug.Log("[PursuitAction] Finished looking right - player not found");
                data.SearchState = SearchState.SearchComplete;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            if (agent != null)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
            }
            
            Debug.Log("[PursuitAction] Ended");
        }

        // The action class itself must be stateless
        // All data should be stored in the data class
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float CloseRangeTimer { get; set; } = 0f;
            public SearchState SearchState { get; set; } = SearchState.MovingToLastKnown;
            public float SearchTimer { get; set; } = 0f;
            public Quaternion InitialRotation { get; set; }
        }
    }
}