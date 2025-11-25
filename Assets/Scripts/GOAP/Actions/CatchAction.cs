using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("Catch-5fe8c2cb-3a74-49ca-a008-19cf7bdba069")]
    public class CatchAction : GoapActionBase<CatchAction.Data>
    {
        private ActionAudioBehaviour audio;

        public override void Created()
        {
        }

        public override void Start(IMonoAgent agent, Data data)
        {
            audio = agent.Transform.GetComponent<ActionAudioBehaviour>();
            audio?.StopWalkLoop();

            // Animation handled by GuardAnimationController based on velocity
            // When agent stops (velocity = 0), controller will automatically transition to idle

            Debug.Log($"[CatchAction] {agent.Transform.name} starting catch sequence - waiting for physical contact!");

            // Stop the agent from moving during catch attempt
            var navAgent = agent.Transform.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
            }
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            // Check if the guard has made physical contact with the player
            var brain = agent.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
            
            if (brain != null && brain.IsPlayerCaught)
            {
                Debug.Log($"[CatchAction] {agent.Transform.name} has caught the player via physical contact!");
                return ActionRunState.Completed;
            }
            
            // Keep trying to catch (guard should be moving towards player via pursuit)
            return ActionRunState.Continue;
        }

        public override void Complete(IMonoAgent agent, Data data)
        {
            Debug.Log($"[CatchAction] {agent.Transform.name} - Catch action completed! Player has been caught.");
            audio?.PlayCaptured();
        }

        public override void End(IMonoAgent agent, Data data)
        {
            // Cleanup if needed
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}