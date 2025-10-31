using System;
using UnityEngine;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;

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
                // We could also listen to onDeactivated if you want to react
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
                beam.onActivated.RemoveListener(OnBeamActivated);
        }

        private void OnBeamActivated()
        {
            pendingTriggers = Mathf.Clamp(pendingTriggers + 1, 0, 999);
        }

        public void ConsumeOneTrigger()
        {
            pendingTriggers = Mathf.Max(0, pendingTriggers - 1);
        }
    }
}

