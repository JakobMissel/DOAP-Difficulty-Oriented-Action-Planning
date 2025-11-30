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
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();
            var animController = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.GuardAnimationController>();
            var sight = mono.Transform.GetComponent<GuardSight>();

            // Stop the guard where they are
            if (navAgent != null)
            {
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
            }

            // Show the recharging icon
            ShowRechargingIcon(mono.Transform, true);

            // Turn off flashlight during recharge
            SetFlashlightActive(mono.Transform, false);

            // Start recharging
            if (energy != null)
            {
                energy.SetRecharging(true);
                audio?.PlayRechargeLoop();
                Debug.Log($"[RechargeAction] {mono.Transform.name} started recharging on the spot (Energy: {energy.CurrentEnergy:F1}/{energy.MaxEnergy:F1})");
            }
            else
            {
                Debug.LogError($"[RechargeAction] {mono.Transform.name} has no EnergyBehaviour!");
            }

            // Force Recharge animation (will be locked until action ends)
            if (animController != null)
            {
                animController.ForceRecharge();
                Debug.Log($"[RechargeAction] {mono.Transform.name} forcing Recharge animation");
            }
            if (sight != null)
            {
                sight.SetDetectionPaused(true);
                Debug.Log($"[RechargeAction] {mono.Transform.name} disabled sight while recharging");
            }
        }
 
         public override IActionRunState Perform(IMonoAgent mono, Data data, IActionContext ctx)
         {
             var energy = mono.Transform.GetComponent<EnergyBehaviour>();
             var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();
            var sight = mono.Transform.GetComponent<GuardSight>();
            
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
                 audio?.StopRechargeLoop();
                 sight?.SetDetectionPaused(false);
                 return ActionRunState.Completed;
             }
 
             // Continue recharging
             return ActionRunState.Continue;
         }
 
        public override void End(IMonoAgent mono, Data data)
        {
            var navAgent = mono.Transform.GetComponent<NavMeshAgent>();
            var energy = mono.Transform.GetComponent<EnergyBehaviour>();
            var audio = mono.Transform.GetComponent<ActionAudioBehaviour>();
            var animController = mono.Transform.GetComponent<Assets.Scripts.GOAP.Behaviours.GuardAnimationController>();
            var sight = mono.Transform.GetComponent<GuardSight>();

            // Hide the recharging icon
            ShowRechargingIcon(mono.Transform, false);

            // Turn flashlight back on after recharge
            SetFlashlightActive(mono.Transform, true);

            // Resume movement
            if (navAgent != null)
            {
                navAgent.isStopped = false;
            }

            // Stop recharging
            if (energy != null)
            {
                energy.SetRecharging(false);
                audio?.StopRechargeLoop();
                Debug.Log($"[RechargeAction] {mono.Transform.name} ending recharge action");
            }

            // Clear forced animation state to resume velocity-based animations
            if (animController != null)
            {
                animController.ClearForcedState();
                Debug.Log($"[RechargeAction] {mono.Transform.name} cleared forced animation state");
            }
            if (sight != null)
            {
                sight.SetDetectionPaused(false);
                Debug.Log($"[RechargeAction] {mono.Transform.name} re-enabled sight after recharging");
            }
        }
        
        /// <summary>
        /// Shows or hides the RechargingIcon UI element
        /// </summary>
        private void ShowRechargingIcon(Transform guardTransform, bool show)
        {
            // Find the RechargingIcon in the guard's hierarchy
            Transform iconTransform = guardTransform.Find("Canvas/RechargingIcon");
            if (iconTransform != null)
            {
                iconTransform.gameObject.SetActive(show);
                Debug.Log($"[RechargeAction] {guardTransform.name} RechargingIcon set to: {show}");
            }
            else
            {
                Debug.LogWarning($"[RechargeAction] {guardTransform.name} could not find Canvas/RechargingIcon");
            }
        }

        /// <summary>
        /// Enables or disables the guard's flashlight (spotlight)
        /// </summary>
        private void SetFlashlightActive(Transform guardTransform, bool active)
        {
            // Search for Light components in children (typically the flashlight/spotlight)
            Light[] lights = guardTransform.GetComponentsInChildren<Light>();

            if (lights != null && lights.Length > 0)
            {
                foreach (Light light in lights)
                {
                    light.enabled = active;
                }
                Debug.Log($"[RechargeAction] {guardTransform.name} flashlight(s) set to: {active} ({lights.Length} light(s) found)");
            }
            else
            {
                Debug.LogWarning($"[RechargeAction] {guardTransform.name} has no Light components in children!");
            }
        }
     }
 }
