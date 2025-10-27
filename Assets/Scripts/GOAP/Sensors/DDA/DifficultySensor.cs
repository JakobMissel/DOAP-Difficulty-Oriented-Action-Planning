using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using Assets.Scripts.DDA;

namespace Assets.Scripts.GOAP.Sensors.DDA
{
    [GoapId("DifficultySensor-260fd14a-d8e1-49eb-93c7-4281156582c2")]
    public class DifficultySensor : LocalWorldSensorBase
    {
        public override void Created() { }

        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            return new SenseValue(DifficultyTracker.GetDifficultyI());
        }
    }
}