using Assets.Scripts.GOAP.Sensors;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("Recharge-760e34a2-b02a-4d32-9256-4193d18e9447")]
    public class RechargeAction : GoapActionBase<RechargeAction.Data>
    {
        private UnityEngine.AI.NavMeshAgent agent;
        private EnergySensor energy;

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }

        public override void Created()
        {
        }

        public override void Start(IMonoAgent mono, Data data)
        {
            if (agent == null)
                agent = mono.Transform.GetComponent<UnityEngine.AI.NavMeshAgent>();

            if (energy == null)
                energy = mono.Transform.GetComponent<EnergySensor>();

            agent.isStopped = false;
            agent.SetDestination(data.Target.Position);

            // start charging when we arrive
        }

        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            bool atStation = Vector3.Distance(mono.Transform.position, data.Target.Position)
                             <= agent.stoppingDistance + 0.2f;

            if (energy == null)
                energy = mono.Transform.GetComponent<EnergySensor>();

            if (energy != null)
                energy.SetRecharging(atStation);

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            if (energy == null)
                energy = mono.Transform.GetComponent<EnergySensor>();
            if (energy != null)
                energy.SetRecharging(false);
        }
    }
}