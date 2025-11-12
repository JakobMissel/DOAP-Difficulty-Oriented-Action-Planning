using UnityEngine;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("PlayerTarget-2264f0c6-cd4e-43a3-844a-d8e505c7c1c4")]
    public class PlayerTargetSensor : LocalTargetSensorBase
    {
        private Transform player;

        public override void Created()
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference refs, ITarget existingTarget)
        {
            if (player == null)
                return null;

            bool hasVisual = false;
            if (agent.Transform.TryGetComponent<GuardSight>(out var sight))
            {
                hasVisual = sight.CanSeePlayer();
            }

            if (hasVisual)
            {
                if (existingTarget is TransformTarget t)
                    return t.SetTransform(player);

                return new TransformTarget(player);
            }

            return null;
        }
    }
}