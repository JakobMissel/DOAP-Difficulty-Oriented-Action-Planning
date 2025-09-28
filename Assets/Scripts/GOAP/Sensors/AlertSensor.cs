using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("AlertSensor-21c25467-410e-4d08-a28e-8df3f7202e15")]
    public class AlertSensor : LocalWorldSensorBase
    {
        public float visionRange = 10f;
        private Transform player;

        public override void Created()
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        public override void Update() {}

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            if (player == null)
                return new SenseValue(false);

            bool inRange = Vector3.Distance(agent.Transform.position, player.position) <= visionRange;
            Debug.Log($"[AlertSensor] {agent.Transform.name} sees player? {inRange}");
            return new SenseValue(inRange);
        }
    }
}