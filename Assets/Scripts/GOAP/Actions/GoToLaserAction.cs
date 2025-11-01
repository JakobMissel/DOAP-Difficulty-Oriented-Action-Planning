using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;
using Assets.Scripts.GOAP.Systems;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("GoToLaser-a8fbbe83-4db9-486c-89f5-ad58636d61bb")]
    public class GoToLaserAction : GoapActionBase<GoToLaserAction.Data>
    {
        private NavMeshAgent agent;
        private SimpleGuardSightNiko sight;

        // This method is called when the action is created
        // This method is optional and can be removed
        public override void Created()
        {
        }

        // This method is called every frame before the action is performed
        // If this method returns false, the action will be stopped
        // This method is optional and can be removed
        public override bool IsValid(IActionReceiver agent, Data data)
        {
            return true;
        }

        // This method is called when the action is started
        // This method is optional and can be removed
        public override void Start(IMonoAgent mono, Data data)
        {
            if (agent == null)
                agent = mono.Transform.GetComponent<NavMeshAgent>();
            if (sight == null)
                sight = mono.Transform.GetComponent<SimpleGuardSightNiko>();

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            // If we already see the player, abort and clear the alert so pursuit can take over immediately
            if (sight != null && sight.CanSeePlayer())
            {
                LaserAlertSystem.ClearWorldKey();
                return;
            }

            agent.isStopped = false;
            agent.updateRotation = true;
            agent.updatePosition = true;

            // Snapshot which laser raised the alert when this action starts
            data.AnchorSnapshot = Assets.Scripts.GOAP.Systems.LaserAlertSystem.Anchor;

            // Optional immediate set; Perform keeps updating until arrival
            if (data.Target != null && data.Target.IsValid())
            {
                agent.SetDestination(data.Target.Position);
            }
        }

        // This method is called once before the action is performed
        // This method is optional and can be removed
        public override void BeforePerform(IMonoAgent agent, Data data)
        {
        }

        // This method is called every frame while the action is running
        // This method is required
        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            // Abort instantly if we can see the player; clear alert so pursuit wins
            if (sight != null && sight.CanSeePlayer())
            {
                LaserAlertSystem.ClearWorldKey();
                return ActionRunState.Stop;
            }

            if (agent == null || !agent.enabled || !agent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
                return ActionRunState.Stop;

            // Continuously head to the target position
            agent.SetDestination(data.Target.Position);

            // Arrive when close enough
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
            {
                agent.isStopped = true;

                // Arrival reached; do not auto-clear the global alert here.
                // Alert will clear when the laser deactivates (via LaserAlertSystem.OnLaserDeactivated()).
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        // This method is called when the action is completed
        // This method is optional and can be removed
        public override void Complete(IMonoAgent mono, Data data)
        {
            // Try to clear the world key if no newer alert replaced this one
            Assets.Scripts.GOAP.Systems.LaserAlertSystem.TryClearWorldKeyForAnchor(data.AnchorSnapshot);
        }

        // This method is called when the action is stopped
        // This method is optional and can be removed
        public override void Stop(IMonoAgent agent, Data data)
        {
        }

        // This method is called when the action is completed or stopped
        // This method is optional and can be removed
        public override void End(IMonoAgent mono, Data data)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }

        // The action class itself must be stateless!
        // All data should be stored in the data class
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
            public Transform AnchorSnapshot { get; set; }
        }
    }
}