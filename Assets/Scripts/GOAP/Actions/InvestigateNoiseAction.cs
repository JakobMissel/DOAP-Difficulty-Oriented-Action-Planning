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
        private const float INVESTIGATION_DURATION = 3.0f; // How long to stay at noise location

        public override void Created()
        {
        }

        public override void Start(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            // Forcefully stop any current movement
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            
            // Small delay to ensure clean state
            data.StartDelay = 0.1f;
            data.InvestigationTime = 0f;

            Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} STARTING investigation of noise at {data.Target?.Position}");
        }

        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var sight = mono.Transform.GetComponent<GuardSight>();
            
            // If guard fully spots the player during investigation, abort noise investigation and pursue
            if (sight != null && sight.PlayerSpotted())
            {
                Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} spotted player during investigation - aborting noise investigation!");
                var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
                if (brain != null)
                {
                    brain.ClearDistractionNoise();
                }
                return ActionRunState.Stop;
            }
            
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
            {
                Debug.LogWarning($"[InvestigateNoiseAction] {mono.Transform.name} no valid target or agent!");
                return ActionRunState.Stop;
            }

            // Handle start delay
            if (data.StartDelay > 0f)
            {
                data.StartDelay -= Time.deltaTime;
                if (data.StartDelay <= 0f)
                {
                    // Now start moving
                    agent.isStopped = false;
                    agent.updateRotation = true;
                    agent.updatePosition = true;
                    agent.SetDestination(data.Target.Position);
                    Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} now moving to noise at {data.Target.Position}");
                }
                return ActionRunState.Continue;
            }

            // Move to the noise location
            agent.SetDestination(data.Target.Position);

            float dist = Vector3.Distance(mono.Transform.position, data.Target.Position);

            // Check if we've reached the noise location
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                // Stop at the location and investigate
                agent.isStopped = true;
                data.InvestigationTime += Time.deltaTime;

                if (data.InvestigationTime < INVESTIGATION_DURATION)
                {
                    Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} investigating... {data.InvestigationTime:F1}s / {INVESTIGATION_DURATION}s");
                }

                // After investigating for the duration, complete
                if (data.InvestigationTime >= INVESTIGATION_DURATION)
                {
                    Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} investigation complete! Clearing noise and returning to patrol.");
                    
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
                Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} moving to noise source... Distance: {dist:F1}m");
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var brain = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
            
            // Ensure noise is cleared
            if (brain != null)
            {
                brain.ClearDistractionNoise();
            }
            
            // Resume normal movement state
            if (agent != null)
            {
                agent.isStopped = false;
            }
            
            Debug.Log($"[InvestigateNoiseAction] {mono.Transform.name} ending investigation - ready to resume patrol.");
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public float InvestigationTime { get; set; }
            public float StartDelay { get; set; }
        }
    }
}