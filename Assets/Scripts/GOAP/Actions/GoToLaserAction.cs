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
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var sight = mono.Transform.GetComponent<GuardSight>();
            var animation = mono.Transform.GetComponent<GuardAnimation>();

            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;

            // If we already fully spotted the player, abort and clear the alert so pursuit can take over immediately
            if (sight != null && sight.PlayerSpotted())
            {
                LaserAlertSystem.ClearWorldKey();
                return;
            }

            // Trigger Walking animation when going to investigate laser
            if (animation != null)
            {
                animation.Walk();
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
                Debug.Log($"[GoToLaserAction] {mono.Transform.name} heading to laser at {data.Target.Position}");
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
            var agent = mono.Transform.GetComponent<NavMeshAgent>();
            var sight = mono.Transform.GetComponent<GuardSight>();
            
            // Abort instantly if we fully spotted the player; clear alert so pursuit wins
            if (sight != null && sight.PlayerSpotted())
            {
                LaserAlertSystem.ClearWorldKey();
                Debug.Log($"[GoToLaserAction] {mono.Transform.name} spotted player, aborting laser investigation");
                return ActionRunState.Stop;
            }

            if (agent == null || !agent.enabled || !agent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
                return ActionRunState.Stop;

            // Keep updating destination in case the laser alert target changes
            agent.SetDestination(data.Target.Position);

            // Check if we've arrived
            float dist = Vector3.Distance(mono.Transform.position, data.Target.Position);
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 1.0f)
            {
                Debug.Log($"[GoToLaserAction] {mono.Transform.name} arrived at laser position");
                LaserAlertSystem.ClearWorldKey();
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
            // Ensure alert is cleared when action ends
            LaserAlertSystem.ClearWorldKey();
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