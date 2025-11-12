using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("RechargeTargetSensor-d75bc583-1396-4c22-a928-5c77e9a1bdaa")]
    public class RechargeTargetSensor : LocalTargetSensorBase
    {
        public override void Created()
        {
            // No longer need to find a breakpoint - guards recharge on the spot
        }
        
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference refs, ITarget existingTarget)
        {
            // Return the guard's current position as the recharge target (recharge on the spot)
            var mono = refs.GetCachedComponent<MonoBehaviour>();
            if (mono == null || mono.transform == null)
            {
                Debug.LogWarning("[RechargeTargetSensor] Could not get agent transform");
                return null;
            }

            Vector3 pos = mono.transform.position;
            
            // Optionally sample NavMesh to ensure we're on a valid position
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            {
                pos = hit.position;
            }

            if (existingTarget is PositionTarget t)
                return t.SetPosition(pos);

            return new PositionTarget(pos);
        }
    }
}