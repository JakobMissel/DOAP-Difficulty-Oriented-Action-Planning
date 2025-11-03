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
        private NavMeshAgent navAgent;
        private EnergyBehaviour energy;
        private bool hasReachedStation = false;

        public class Data : IActionData
        {
            public ITarget Target { get; set; }
        }

        public override void Created()
        {
        }

        public override void Start(IMonoAgent mono, Data data)
        {
            if (navAgent == null)
                navAgent = mono.Transform.GetComponent<NavMeshAgent>();

            if (energy == null)
                energy = mono.Transform.GetComponent<EnergyBehaviour>();

            hasReachedStation = false;

            if (navAgent == null)
            {
                Debug.LogError("[RechargeAction] NavMeshAgent is NULL!");
                return;
            }

            if (!navAgent.enabled)
            {
                Debug.LogWarning("[RechargeAction] NavMeshAgent is DISABLED!");
                return;
            }

            if (!navAgent.isOnNavMesh)
            {
                Debug.LogWarning($"[RechargeAction] Agent is NOT on NavMesh! Position: {mono.Transform.position}");
                // Try to warp onto NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(mono.Transform.position, out hit, 5f, NavMesh.AllAreas))
                {
                    navAgent.Warp(hit.position);
                    Debug.Log($"[RechargeAction] Warped agent to NavMesh at {hit.position}");
                }
                else
                {
                    Debug.LogError("[RechargeAction] Could not find nearby NavMesh position!");
                    return;
                }
            }

            if (data.Target == null || !data.Target.IsValid())
            {
                Debug.LogError("[RechargeAction] Target is NULL or invalid!");
                return;
            }

            navAgent.isStopped = false;
            bool pathSet = navAgent.SetDestination(data.Target.Position);
            Debug.Log($"[RechargeAction] Started - moving to station at {data.Target.Position}, pathSet={pathSet}, hasPath={navAgent.hasPath}");
        }

        public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
        {
            if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh || data.Target == null || !data.Target.IsValid())
                return ActionRunState.Stop;

            // First check if we've reached the station
            if (!hasReachedStation)
            {
                navAgent.SetDestination(data.Target.Position);

                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance + 0.5f)
                {
                    hasReachedStation = true;
                    navAgent.isStopped = true;
                    
                    if (energy != null)
                        energy.SetRecharging(true);
                    
                    Debug.Log("[RechargeAction] Reached station! Starting recharge.");
                }
                
                return ActionRunState.Continue;
            }

            // We're at the station - check if recharged
            if (energy != null)
            {
                float currentEnergy = energy.CurrentEnergy;
                float maxEnergy = energy.MaxEnergy;
                Debug.Log($"[RechargeAction] Recharging... Energy: {currentEnergy:0.00}/{energy.MaxEnergy}");
                
                // Only complete when at maximum capacity (with small tolerance for floating point)
                if (currentEnergy >= maxEnergy - 0.1f)
                {
                    Debug.Log("[RechargeAction] Fully recharged! Completing action.");
                    return ActionRunState.Completed;
                }
            }

            return ActionRunState.Continue;
        }

        public override void End(IMonoAgent mono, Data data)
        {
            if (energy != null)
                energy.SetRecharging(false);

            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
                navAgent.isStopped = false;

            hasReachedStation = false;
            Debug.Log("[RechargeAction] Ended");
        }
    }
}