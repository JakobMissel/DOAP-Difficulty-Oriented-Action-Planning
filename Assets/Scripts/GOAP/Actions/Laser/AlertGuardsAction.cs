using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using Assets.Scripts.GOAP.Behaviours;
using Assets.Scripts.GOAP.Systems;

namespace Assets.Scripts.GOAP.Actions.Laser
{
    [GoapId("AlertGuardsAction-8c6e2f63-9e52-4d1e-898e-1a27a08b2f4d")]
    public class AlertGuardsAction : GoapActionBase<AlertGuardsAction.Data>
    {
        public override void Start(IMonoAgent agent, Data data)
        {
            var laser = agent.Transform.GetComponent<LaserBehaviour>();
            var beam = agent.Transform.GetComponent<LaserBeam>();
            if (laser == null || beam == null)
                return;

            var pos = beam.transform.position;

            // Raise a global alert so all guards will plan to go to this position
            LaserAlertSystem.RaiseAlert(beam.transform);

            // Consume one pending trigger so the laser process goal can re-satisfy
            laser.ConsumeOneTrigger();
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            return ActionRunState.Completed;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}
