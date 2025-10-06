// Assets/Scripts/GOAP/Sensors/NoiseTargetSensor.cs
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("NoiseTargetSensor-ebd8de9d-f878-45c1-add0-edef3a65bc21")]
    public class NoiseTargetSensor : LocalTargetSensorBase
    {
        public override void Created() { }
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference refs, ITarget existingTarget)
        {
            var brain = refs.GetCachedComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
            if (brain == null || !brain.HasHeardNoise)
                return null;

            Vector3 noisePosition = brain.LastNoisePosition;

            // Ensure the position is on the NavMesh
            if (UnityEngine.AI.NavMesh.SamplePosition(noisePosition, out var hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                noisePosition = hit.position;

            if (existingTarget is PositionTarget t)
                return t.SetPosition(noisePosition);

            return new PositionTarget(noisePosition);
        }
    }
}