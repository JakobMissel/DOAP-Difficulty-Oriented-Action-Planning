using System;
using UnityEngine;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using Assets.Scripts.GOAP.Systems;

namespace Assets.Scripts.GOAP.Behaviours
{
    [DisallowMultipleComponent]
    public class LaserBehaviour : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private LaserBeam beam;
        [SerializeField] private GoapActionProvider provider;

        [Header("Alert Broadcast")]
        [Tooltip("Radius used when broadcasting a noise alert to guards when the beam is triggered.")]
        public float alertRadius = 10f;

        [Tooltip("If enabled, keep LaserTriggered at 1 as long as the beam is active; it resets to 0 only when the player leaves the beam. If disabled, the Alert action will clear it immediately.")]
        [SerializeField] private bool holdUntilExit = true;

        private int pendingTriggers = 0;

        public bool IsLaserEnabled => beam != null && beam.IsEnabled;
        public int PendingTriggers => pendingTriggers;

        private void Reset()
        {
            if (beam == null) beam = GetComponent<LaserBeam>();
            if (provider == null) provider = GetComponent<GoapActionProvider>();
        }

        private void Awake()
        {
            if (beam == null) beam = GetComponent<LaserBeam>();
            if (provider == null) provider = GetComponent<GoapActionProvider>();

            if (beam != null)
            {
                beam.onActivated ??= new UnityEngine.Events.UnityEvent();
                beam.onActivated.AddListener(OnBeamActivated);
                // Ensure we clear the trigger when the player leaves the beam (last collider exits)
                beam.onDeactivated ??= new UnityEngine.Events.UnityEvent();
                beam.onDeactivated.AddListener(OnBeamDeactivated);
            }
        }

        private void Start()
        {
            // Ensure our goals are requested so the planner can consider them
            if (provider != null)
            {
                provider.RequestGoal(new[]
                {
                    typeof(Assets.Scripts.GOAP.Goals.LaserMaintainOnGoal),
                    typeof(Assets.Scripts.GOAP.Goals.LaserMaintainOffGoal),
                    typeof(Assets.Scripts.GOAP.Goals.LaserProcessTriggerGoal),
                });
            }
        }

        private void OnDestroy()
        {
            if (beam != null)
            {
                beam.onActivated.RemoveListener(OnBeamActivated);
                beam.onDeactivated.RemoveListener(OnBeamDeactivated);
            }
        }

        private void OnBeamActivated()
        {
            // Latch as triggered while the beam is active
            pendingTriggers = 1;

            // Immediately raise a global alert anchored to this beam so guards react without delay
            if (beam != null)
            {
                Assets.Scripts.GOAP.Systems.LaserAlertSystem.RaiseAlert(beam.transform);
                Debug.Log($"Laser beam activated at {beam.transform.position}");
            }
        }

        private void OnBeamDeactivated()
        {
            // Clear when the laser deactivates (player left or beam turned off)
            pendingTriggers = 0;
            // Schedule global alert clearance after hold period
            LaserAlertSystem.OnLaserDeactivated();
        }

        public void ConsumeOneTrigger()
        {
            // If configured to hold until exit and the beam is currently active, keep it latched
            if (holdUntilExit && beam != null && beam.IsEnabled)
                return;

            // Otherwise clear the pending trigger after processing an alert
            pendingTriggers = 0;
        }
    }
}
