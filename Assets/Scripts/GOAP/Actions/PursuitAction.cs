using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Core;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("Pursuit-67f97c9c-9d66-42ce-853c-133448c53402")]
    public class PursuitAction : GoapActionBase<PursuitAction.Data>
    {
        // Cache player reference statically since it's shared between all guards
        private static Transform cachedPlayer;

        public override void Created() { }

        public override void Start(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            audio?.PlayWalkLoop();

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.updatePosition = true;
        }
        
        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var sight = mono.Transform.GetComponent<GuardSight>();

            if (cachedPlayer == null)
                cachedPlayer = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (agent == null || !agent.enabled || !agent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
                return ActionRunState.Stop;

            // During pursuit, continue as long as we CAN see the player (visual contact)
            // PlayerSpotted() is used to START pursuit, CanSeePlayer() is used to MAINTAIN it
            bool canSeePlayer = sight != null && sight.CanSeePlayer();
            if (!canSeePlayer)
            {
                Debug.Log($"[PursuitAction] {mono.Transform.name} lost visual contact with player, stopping pursuit.");
                return ActionRunState.Stop;
            }

            // Chase the player - collision detection (GuardCatchTrigger) will handle the catch
            agent.SetDestination(data.Target.Position);
            agent.isStopped = false;
            agent.updateRotation = true;

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();
            audio?.StopWalkLoop();

            // Mark that this guard should reset to closest waypoint when returning to patrol
            var patrolAction = typeof(PatrolAction);
            var guardId = mono.Transform.GetInstanceID();
            
            // Access the static dictionary via reflection to set the reset flag
            var fieldInfo = patrolAction.GetField("GuardNeedsReset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (fieldInfo != null)
            {
                var dict = fieldInfo.GetValue(null) as System.Collections.Generic.Dictionary<int, bool>;
                if (dict != null)
                {
                    dict[guardId] = true;
                    Debug.Log($"[PursuitAction] {mono.Transform.name} marked to reset patrol to closest waypoint");
                }
            }
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}