using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Actions.Laser
{
    [GoapId("EnableLaserAction-a29a1d9e-02f8-4cb8-b32b-3ad58a8a701d")]
    public class EnableLaserAction : GoapActionBase<EnableLaserAction.Data>
    {
        public override void Start(IMonoAgent agent, Data data)
        {
            var laser = agent.Transform.GetComponent<LaserBehaviour>();
            if (laser != null)
            {
                var beam = agent.Transform.GetComponent<LaserBeam>();
                if (beam != null)
                {
                    beam.SetEnabled(true);
                }
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
