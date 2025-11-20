using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Actions.Laser
{
    [GoapId("DisableLaserAction-5a17cfd1-34b6-4c5d-b7f3-8c43a9a7c0ce")]
    public class DisableLaserAction : GoapActionBase<DisableLaserAction.Data>
    {
        public override void Start(IMonoAgent agent, Data data)
        {
            var beam = agent.Transform.GetComponent<LaserBeam>();
            var audio = agent.Transform.GetComponent<ActionAudioBehaviour>();
            if (beam != null)
            {
                beam.SetEnabled(false);
                audio?.PlayLaserDeactivate();
            }
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
