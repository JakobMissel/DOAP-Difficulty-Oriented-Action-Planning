// filepath: Assets/Scripts/GOAP/Sensors/SelfTargetSensor.cs
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Assets.Scripts.GOAP.Sensors
{
    // Resolves the agent itself (its Transform) as a target for actions that operate on self
    [GoapId("SelfTargetSensor-8c8b1f7d-ea75-4c89-87d5-5c7c2c8d64c9")]
    public class SelfTargetSensor : LocalTargetSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            if (existingTarget is TransformTarget tt && tt.Transform != null)
                return tt;

            return new TransformTarget(agent.Transform);
        }
    }
}

