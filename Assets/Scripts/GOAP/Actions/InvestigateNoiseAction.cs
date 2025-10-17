// Assets/Scripts/GOAP/Actions/InvestigateNoiseAction.cs
using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Core;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("InvestigateNoise-73e3c275-b47d-48a5-bcdf-9e503738a046")]
    public class InvestigateNoiseAction : GoapActionBase<InvestigateNoiseAction.Data>
    {
        private NavMeshAgent agent;
        private float investigationTime = 0f;
        private const float INVESTIGATION_DURATION = 3.0f; // How long to stay at noise location

        public override void Created()
        {
        }

        public override void Start(IMonoAgent mono, Data data)
        {
            if (agent == null)
                agent = mono.Transform.GetComponent<NavMeshAgent>();

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.updatePosition = true;

            investigationTime = 0f;

            Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} investigating noise at {data.Target?.Position}");
        }

        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
            {
                Debug.LogWarning("[InvestigateNoiseAction] No valid target or agent!");
                return ActionRunState.Stop;
            }

            // Move to the noise location
            agent.SetDestination(data.Target.Position);

            float dist = Vector3.Distance(mono.Transform.position, data.Target.Position);

            // Check if we've reached the noise location
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                // Stop at the location and investigate
                agent.isStopped = true;
                investigationTime += Time.deltaTime;

                Debug.Log($"[InvestigateNoiseAction] Investigating... {investigationTime:F1}s / {INVESTIGATION_DURATION}s");

                // After investigating for the duration, complete
                if (investigationTime >= INVESTIGATION_DURATION)
                {
                    Debug.Log("[InvestigateNoiseAction] Investigation complete!");
                    
                    // Clear the distraction noise from the behaviour
                    var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
                    if (brain != null)
                    {
                        brain.ClearDistractionNoise();
                    }
                    
                    return ActionRunState.Completed;
                }
            }
            else
            {
                Debug.Log($"[InvestigateNoiseAction] Moving to noise source... Distance: {dist:F1}m");
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
            
            investigationTime = 0f;
            Debug.Log("[InvestigateNoiseAction] Ended");
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}