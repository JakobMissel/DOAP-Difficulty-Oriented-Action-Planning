using System.Collections.Generic;
using System.Linq;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using Assets.Scripts.GOAP.Sensors;
using Assets.Scripts.GOAP.WorldKeys;

namespace Assets.Scripts.GOAP.Config
{
    // Attach this component to the same GameObject that has GoapBehaviour
    // Then, in the GoapBehaviour inspector, add this component to "Agent Type Config Factories".
    // It will build the AgentType from the selected capability assets, with optional code-time tweaks.
    public class GuardAgentTypeFactory : AgentTypeFactoryBase
    {
        [Header("Source Capabilities (ScriptableObject)")]
        [SerializeField] private List<CapabilityConfigScriptable> capabilities = new();

        [Header("Agent Type")]
        [SerializeField] private string agentTypeName = "Guard";

        [Header("Optional Code Overrides (Difficulty Tuning)")]
        [Tooltip("Multiply the cost of InvestigateNoiseAction to make it more/less attractive.")]
        [SerializeField] private float investigateNoiseCostMultiplier = 1f;

        [Tooltip("Multiply the cost of PursuitAction to make it more/less attractive.")]
        [SerializeField] private float pursuitCostMultiplier = 1f;

        public override IAgentTypeConfig Create()
        {
            // Build capability configs from SOs
            var builtCaps = new List<ICapabilityConfig>();
            foreach (var cap in capabilities)
            {
                if (cap == null)
                    continue;
                builtCaps.Add(cap.Create());
            }

            // Merge all capability parts into a single AgentTypeConfig
            var cfg = new AgentTypeConfig(string.IsNullOrWhiteSpace(agentTypeName) ? name : agentTypeName)
            {
                Goals = builtCaps.SelectMany(x => x.Goals).ToList(),
                Actions = builtCaps.SelectMany(x => x.Actions).ToList(),
                WorldSensors = builtCaps.SelectMany(x => x.WorldSensors).ToList(),
                TargetSensors = builtCaps.SelectMany(x => x.TargetSensors).ToList(),
                MultiSensors = builtCaps.SelectMany(x => x.MultiSensors).ToList(),
            };

            // Remove duplicate sensors that target the same key (common when multiple capabilities add the same sensor)
            DeduplicateSensors(cfg);

            // Apply small programmatic tweaks when desired
            ApplyDifficultyTweaks(cfg);
            return cfg;
        }

        private void DeduplicateSensors(AgentTypeConfig cfg)
        {
            if (cfg == null)
                return;

            if (cfg.WorldSensors != null && cfg.WorldSensors.Count > 0)
            {
                cfg.WorldSensors = cfg.WorldSensors
                    .GroupBy(ws => ws.Key?.Name)
                    .Select(g => g.First())
                    .ToList();
            }

            if (cfg.TargetSensors != null && cfg.TargetSensors.Count > 0)
            {
                cfg.TargetSensors = cfg.TargetSensors
                    .GroupBy(ts => ts.Key?.Name)
                    .Select(g => g.First())
                    .ToList();
            }

            // Ensure we have a sensor for AtPlayerTarget (needed by Pursuit/Catch graphs)
            bool hasAtPlayerTarget = cfg.WorldSensors.Any(ws => ws.Key?.Name == nameof(AtPlayerTarget));
            if (!hasAtPlayerTarget)
            {
                cfg.WorldSensors.Add(new WorldSensorConfig<AtPlayerTargetSensor>
                {
                    Key = new AtPlayerTarget(),
                });
            }
        }

        private void ApplyDifficultyTweaks(AgentTypeConfig cfg)
        {
            if (cfg?.Actions == null || cfg.Actions.Count == 0)
                return;

            // Example: Adjust costs by action class name.
            foreach (var action in cfg.Actions)
            {
                if (action is ActionConfig a)
                {
                    // InvestigateNoiseAction cost tuning
                    if (a.ClassType != null && a.ClassType.Contains("InvestigateNoiseAction"))
                    {
                        a.BaseCost *= Mathf.Max(0f, investigateNoiseCostMultiplier);
                    }

                    // PursuitAction cost tuning
                    if (a.ClassType != null && a.ClassType.Contains("PursuitAction"))
                    {
                        a.BaseCost *= Mathf.Max(0f, pursuitCostMultiplier);
                    }
                }
            }
        }
    }
}
