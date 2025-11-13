using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;
using Assets.Scripts.GOAP.Behaviours;

namespace Assets.Scripts.GOAP.Actions
{
    [GoapId("Recharge-760e34a2-b02a-4d32-9256-4193d18e9447")]
    public class RechargeAction : GoapActionBase<RechargeAction.Data>
    {
        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }

        public override void Created()
        {
        }

        public override void Start(IMonoAgent mono, Data data)
        {
            var navAgent = mono.Transform.GetComponent<NavMeshAgent>();
            var energy = mono.Transform.GetComponent<EnergyBehaviour>();
            var animation = mono.Transform.GetComponent<GuardAnimation>();

            // Stop the guard where they are
            if (navAgent != null)
            {
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
            }
            
            // Trigger Idle animation when recharging
            if (animation != null)
            {
                animation.Idle();
            }
            
            // Start recharging immediately on the spot
            if (energy != null)
            {
                energy.SetRecharging(true);
                Debug.Log($"[RechargeAction] {mono.Transform.name} started recharging on the spot (Energy: {energy.CurrentEnergy:F1}/{energy.MaxEnergy:F1})");
            }
            else
            {
                Debug.LogError($"[RechargeAction] {mono.Transform.name} has no EnergyBehaviour!");
            }
        }

        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            var energy = mono.Transform.GetComponent<EnergyBehaviour>();
            
            if (energy == null)
            {
                Debug.LogWarning($"[RechargeAction] {mono.Transform.name} has no EnergyBehaviour!");
                return ActionRunState.Stop;
            }

            // Check if fully recharged
            if (energy.CurrentEnergy >= energy.MaxEnergy)
            {
                Debug.Log($"[RechargeAction] {mono.Transform.name} fully recharged!");
                energy.SetRecharging(false);
                return ActionRunState.Completed;
            }

            // Continue recharging
            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            var navAgent = mono.Transform.GetComponent<NavMeshAgent>();
            var energy = mono.Transform.GetComponent<EnergyBehaviour>();
            
            // Resume movement
            if (navAgent != null)
            {
                navAgent.isStopped = false;
            }
            
            // Make sure recharging is stopped when action ends
            if (energy != null)
            {
                energy.SetRecharging(false);
                Debug.Log($"[RechargeAction] {mono.Transform.name} ending recharge action");
            }
        }
    }
}