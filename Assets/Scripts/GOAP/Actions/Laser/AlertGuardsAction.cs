using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using Assets.Scripts.GOAP.Behaviours;
using Assets.Scripts.GOAP.Systems;
using CrashKonijn.Agent.Runtime;
using System.Linq;

namespace Assets.Scripts.GOAP.Actions.Laser
{
    [GoapId("AlertGuardsAction-8c6e2f63-9e52-4d1e-898e-1a27a08b2f4d")]
    public class AlertGuardsAction : GoapActionBase<AlertGuardsAction.Data>
    {
        public override void Start(IMonoAgent agent, Data data)
        {
            var laser = agent.Transform.GetComponent<LaserBehaviour>();
            var beam = agent.Transform.GetComponent<LaserBeam>();
            if (laser == null || beam == null)
                return;

            var pos = beam.transform.position;

            // Find all guards in the scene
            var allGuards = GameObject.FindObjectsByType<AgentBehaviour>(FindObjectsSortMode.None)
                .Where(a => a.GetComponent<GuardSight>() != null) // Only guards have GuardSight
                .ToList();

            Transform closestGuard = null;
            float closestDistance = float.MaxValue;

            // Find the closest guard to the laser
            foreach (var guard in allGuards)
            {
                float distance = Vector3.Distance(guard.transform.position, pos);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestGuard = guard.transform;
                }
            }

            if (closestGuard != null)
            {
                Debug.Log($"[AlertGuardsAction] Closest guard is {closestGuard.name} at distance {closestDistance:F2}");
            }

            // Raise alert with the closest guard assigned
            LaserAlertSystem.RaiseAlert(beam.transform, closestGuard);

            // Consume one pending trigger so the laser process goal can re-satisfy
            laser.ConsumeOneTrigger();
        }

        public override IActionRunState Perform(IMonoAgent agent, Data data, IActionContext context)
        {
            return ActionRunState.Completed;
        }

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }
    }
}
