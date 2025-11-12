// Assets/Scripts/GOAP/Sensors/AtPlayerTargetSensor.cs
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("AtPlayerTargetSensor-5f28b6b3-6c7c-4c51-9d86-3a12a6fd2f2c")]
    public class AtPlayerTargetSensor : LocalWorldSensorBase
    {
        private static Transform cachedPlayer;

        public override void Created() { }
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            // Get agent position
            var mono = refs.GetCachedComponent<MonoBehaviour>();
            var transform = (mono != null) ? mono.transform : null;
            if (transform == null)
                return new SenseValue(0);

            // Find/cached player transform
            if (cachedPlayer == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null)
                    cachedPlayer = go.transform;
            }

            if (cachedPlayer == null)
                return new SenseValue(0);

            // Compute catch distance
            float catchDistance = 1.0f;
            var nav = refs.GetCachedComponent<NavMeshAgent>();
            if (nav != null)
                catchDistance = Mathf.Max(catchDistance, nav.stoppingDistance + 0.5f);

            float dist = Vector3.Distance(transform.position, cachedPlayer.position);
            return new SenseValue(dist <= catchDistance ? 1 : 0);
        }
    }
}

