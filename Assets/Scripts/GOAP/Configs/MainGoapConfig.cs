using CrashKonijn.Goap.Configs;
using CrashKonijn.Goap.Configs.Interfaces;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace GOAP.Configs
{
    [CreateAssetMenu(menuName = "GOAP/Configs/MainGoapConfig")]
    public class MainGoapConfig : GoapConfigBase
    {
        [SerializeField] private WanderAgentTypeConfig wanderAgentConfig;

        public override IGoapConfig Create()
        {
            var config = new GoapConfig();

            if (wanderAgentConfig != null)
            {
                var agentTypeConfig = wanderAgentConfig.Create();
                config.AddAgentType(agentTypeConfig);
            }

            return config;
        }
    }
}