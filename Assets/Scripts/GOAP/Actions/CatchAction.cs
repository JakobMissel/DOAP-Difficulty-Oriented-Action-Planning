using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("Catch-5fe8c2cb-3a74-49ca-a008-19cf7bdba069")]
    public class CatchAction : GoapActionBase<CatchAction.Data>
    {
        private float catchAnimationTime = 0f;
        private const float CATCH_ANIMATION_DURATION = 2.0f; // 2 second catch sequence

        public override void Created()
        {
        }

        public override void Start(IMonoAgent agent, Data data)
        {
            catchAnimationTime = 0f;
            
            Debug.Log($"[CatchAction] Starting catch sequence!");
            
            // Stop the agent from moving
            var navAgent = agent.Transform.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.isStopped = true;
            }
            
            // TODO: Play catch animation/sound effects here
            // TODO: Disable player movement here
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            catchAnimationTime += Time.deltaTime;
            
            Debug.Log($"[CatchAction] Catch sequence progress: {catchAnimationTime:F1}s / {CATCH_ANIMATION_DURATION}s");
            
            // Wait for the catch animation/sequence to finish
            if (catchAnimationTime >= CATCH_ANIMATION_DURATION)
            {
                Debug.Log("[CatchAction] Catch complete! Ending game...");
                return ActionRunState.Completed;
            }
            
            return ActionRunState.Continue;
        }

        public override void Complete(IMonoAgent agent, Data data)
        {
            Debug.Log("[CatchAction] You have been caught! Game Over!");

            
            // TODO: Show Game Over UI here
            // TODO: Add restart/quit buttons
        }

        public override void End(IMonoAgent agent, Data data)
        {
            catchAnimationTime = 0f;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}