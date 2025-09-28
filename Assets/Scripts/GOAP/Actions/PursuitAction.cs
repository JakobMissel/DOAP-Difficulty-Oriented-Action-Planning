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
        // This method is called when the action is created
        // This method is optional and can be removed
        public override void Created()
        {
        }
        

        // This method is called when the action is started
        // This method is optional and can be removed
        public override void Start(IMonoAgent mono, Data data)
        {
            if (agent == null)
                agent = mono.Transform.GetComponent<NavMeshAgent>();

            Debug.Log($"[PursuitAction] {mono.Transform.name} chasing {data.Target?.Position}");
        }
        
        // This method is called every frame while the action is running
        // This method is required
        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            if (agent == null || data.Target == null || !data.Target.IsValid())
                return ActionRunState.Stop;

            agent.SetDestination(data.Target.Position);

            float dist = Vector3.Distance(mono.Transform.position, data.Target.Position);
            if (dist < 1.5f) // "caught" player
            {
                Debug.Log($"[PursuitAction] {mono.Transform.name} Player caught!");
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        // The action class itself must be stateless!
        // All data should be stored in the data class
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}