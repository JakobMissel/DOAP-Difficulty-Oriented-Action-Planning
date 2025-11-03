using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("RechargeTargetSensor-d75bc583-1396-4c22-a928-5c77e9a1bdaa")]
    public class RechargeTargetSensor : LocalTargetSensorBase
    {
        private Transform station;

        public override void Created()
        {
            var obj = GameObject.FindWithTag("Breakpoint");
            if (obj != null)
            {
                station = obj.transform;
                Debug.Log($"[RechargeTargetSensor] Found Breakpoint at {station.position}");
            }
            else
            {
                Debug.LogError("[RechargeTargetSensor] Could not find GameObject with tag 'Breakpoint'!");
            }
        }
        
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference refs, ITarget existingTarget)
        {
            if (station == null)
            {
                Debug.LogWarning("[RechargeTargetSensor] Station is NULL - cannot create target");
                return null;
            }

            Vector3 pos = station.position;
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                pos = hit.position;
                Debug.Log($"[RechargeTargetSensor] Sampled NavMesh position: {pos} (original: {station.position})");
            }
            else
            {
                Debug.LogWarning($"[RechargeTargetSensor] Could not sample NavMesh near Breakpoint at {station.position}! Using original position.");
            }

            if (existingTarget is PositionTarget t)
                return t.SetPosition(pos);

            return new PositionTarget(pos);
        }
    }
}