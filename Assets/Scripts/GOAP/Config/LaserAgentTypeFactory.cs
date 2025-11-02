using System.Collections.Generic;
using System.Linq;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace GOAP.Config
{
    public class LaserAgentTypeFactory : AgentTypeFactoryBase
    {
        [Header("Source Capabilities (ScriptableObject)")]
        [SerializeField] private List<CapabilityConfigScriptable> capabilities = new();

        [Header("Agent Type")]
        [SerializeField] private string agentTypeName = "Laser";

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

            // Remove duplicate sensors that target the same key (capabilities may overlap)
            DeduplicateSensors(cfg);
            
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
        }
    }
}
