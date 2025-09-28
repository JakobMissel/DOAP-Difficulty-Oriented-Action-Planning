using UnityEngine;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("PlayerTarget-2264f0c6-cd4e-43a3-844a-d8e505c7c1c4")]
    public class PlayerTargetSensor : LocalTargetSensorBase
    {
        [SerializeField] private float detectionRange = 10f;
        private Transform player;

        public override void Created()
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            Debug.Log($"[PlayerTargetSensor-1] Player={player} sensed");
        }

        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference refs, ITarget existingTarget)
        {
            if (player == null)
            {
                Debug.Log("[PlayerTargetSensor-2] No player transform found!");
                return null;
            }

            float dist = Vector3.Distance(agent.Transform.position, player.position);
            bool inRange = dist <= detectionRange;

            Debug.Log($"[PlayerTargetSensor-3] In range={inRange}, Dist={dist}, Player={player}");

            if (!inRange)
                return null;

            if (existingTarget is TransformTarget t)
                return t.SetTransform(player);
            
            Debug.Log($"[PlayerTargetSensor-4] Returning TransformTarget for {player}, dist={dist}");
            return new TransformTarget(player);
        }
    }
}