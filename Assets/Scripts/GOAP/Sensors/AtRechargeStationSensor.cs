using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Core;
using UnityEngine;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("AtRechargeStationSensor-bc4891f5-9f4c-433e-8a18-995894a9a9d7")]
    public class AtRechargeStationSensor : LocalWorldSensorBase
    {
        private Vector3 stationNavmeshPos;
        private float threshold = 3f;

        public override void Created()
        {
            var obj = GameObject.FindWithTag("Breakpoint");
            if (obj != null)
            {
                // Snap once at start
                if (UnityEngine.AI.NavMesh.SamplePosition(obj.transform.position, out var hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                    stationNavmeshPos = hit.position;
                else
                    stationNavmeshPos = obj.transform.position;
            }
        }

        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            if (stationNavmeshPos == Vector3.zero)
                return new SenseValue(0);

            float dist = Vector3.Distance(agent.Transform.position, stationNavmeshPos);
            bool atStation = dist <= threshold;
            
            return new SenseValue(atStation ? 1 : 0);
        }
    }
}