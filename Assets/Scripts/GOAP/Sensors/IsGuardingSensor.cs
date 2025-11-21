using UnityEngine;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Core;
using StandGuardTimerBehaviour = Assets.Scripts.GOAP.Behaviours.StandGuardTimerBehaviour;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("IsGuardingSensor-a2b3c4d5-e6f7-8a9b-0c1d-2e3f4a5b6c7d")]
    public class IsGuardingSensor : LocalWorldSensorBase
    {
        public override void Created() 
        { 
            Debug.Log("[IsGuardingSensor] Created");
        }
        
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            var timerBehaviour = refs.GetCachedComponent<StandGuardTimerBehaviour>();
            
            // Return 0 (not guarding) if no timer exists - this is the initial/default state
            if (timerBehaviour == null)
            {
                if (Time.frameCount % 300 == 0) // Log every 5 seconds
                {
                    Debug.Log($"[IsGuardingSensor] No timer behaviour on {agent.Transform.name}, returning 0 (not guarding)");
                }
                return new SenseValue(0);
            }

            // Return 1 if guarding, 0 if not guarding
            int value = timerBehaviour.IsGuarding ? 1 : 0;
            
            if (Time.frameCount % 300 == 0) // Log every 5 seconds
            {
                Debug.Log($"[IsGuardingSensor] {agent.Transform.name} IsGuarding = {value}");
            }
            
            return new SenseValue(value);
        }
    }
}

