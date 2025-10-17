using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Assets.Scripts.DDA
{
    // TODO: Få en sej GoapId
    // [GoapId("DifficultySensor-21c25467-410e-4d08-a28e-8df3f7202e15")]
    public class DifficultySensor : LocalWorldSensorBase
    {
        private DifficultyTracker difficultyTracker;

        public override void Created()
        {
            difficultyTracker = DifficultyTracker.Instance;
        }

        public override void Update()
        {

        }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            return new SenseValue(difficultyTracker.GetDifficultyI());
        }
    }
}