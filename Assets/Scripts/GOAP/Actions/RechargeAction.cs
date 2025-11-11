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
            public bool HasReachedStation { get; set; }
        }

        public override void Created()
        {
        }

        public override void Start(IMonoAgent mono, Data data)
        {
            var navAgent = mono.Transform.GetComponent<NavMeshAgent>();
            var energy = mono.Transform.GetComponent<EnergyBehaviour>();
            
            data.HasReachedStation = false;

            if (navAgent == null)
            {
                Debug.LogError($"[RechargeAction] {mono.Transform.name} NavMeshAgent is NULL!");
                return;
            }

            if (!navAgent.enabled)
            {
                Debug.LogWarning($"[RechargeAction] {mono.Transform.name} NavMeshAgent is DISABLED!");
                return;
            }

            if (!navAgent.isOnNavMesh)
            {
                Debug.LogWarning($"[RechargeAction] {mono.Transform.name} is NOT on NavMesh! Position: {mono.Transform.position}");
                // Try to warp onto NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(mono.Transform.position, out hit, 5f, NavMesh.AllAreas))
                {
                    navAgent.Warp(hit.position);
                    Debug.Log($"[RechargeAction] {mono.Transform.name} warped to NavMesh at {hit.position}");
                }
                else
                {
                    Debug.LogError($"[RechargeAction] {mono.Transform.name} could not find nearby NavMesh position!");
                    return;
                }
            }

            if (data.Target == null || !data.Target.IsValid())
            {
                Debug.LogError($"[RechargeAction] {mono.Transform.name} target is NULL or invalid!");
                return;
            }

            navAgent.isStopped = false;
            bool pathSet = navAgent.SetDestination(data.Target.Position);
            Debug.Log($"[RechargeAction] {mono.Transform.name} started - moving to station at {data.Target.Position}, pathSet={pathSet}");
            
            // Make sure recharging is off while traveling to station
            if (energy != null)
            {
                energy.SetRecharging(false);
            }
        }

        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            var navAgent = mono.Transform.GetComponent<NavMeshAgent>();
            var energy = mono.Transform.GetComponent<EnergyBehaviour>();
            
            if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
                return ActionRunState.Stop;

            // First check if we've reached the station
            if (!data.HasReachedStation)
            {
                float dist = Vector3.Distance(mono.Transform.position, data.Target.Position);
                
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance + 0.5f)
                {
                    data.HasReachedStation = true;
                    navAgent.isStopped = true;
                    
                    // Start recharging now that we're at the station
                    if (energy != null)
                    {
                        energy.SetRecharging(true);
                        Debug.Log($"[RechargeAction] {mono.Transform.name} reached station, starting recharge");
                    }
                }
                else
                {
                    return ActionRunState.Continue;
                }
            }

            // Now at station, check if fully recharged
            if (energy != null)
            {
                if (energy.CurrentEnergy >= energy.MaxEnergy)
                {
                    Debug.Log($"[RechargeAction] {mono.Transform.name} fully recharged!");
                    energy.SetRecharging(false);
                    return ActionRunState.Completed;
                }
            }
            else
            {
                Debug.LogWarning($"[RechargeAction] {mono.Transform.name} has no EnergyBehaviour!");
                return ActionRunState.Stop;
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            var navAgent = mono.Transform.GetComponent<NavMeshAgent>();
            var energy = mono.Transform.GetComponent<EnergyBehaviour>();
            
            if (navAgent != null)
            {
                navAgent.isStopped = false;
            }
            
            // Make sure recharging is stopped when action ends
            if (energy != null)
            {
                energy.SetRecharging(false);
            }
        }
    }
}