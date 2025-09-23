using CrashKonijn.Goap.Configs;
using CrashKonijn.Goap.Configs.Interfaces;
using GOAP.Actions;
using GOAP.Goals;
using GOAP.Sensors;
using GOAP.Targets;
using UnityEngine;

namespace GOAP.Configs
{
    [CreateAssetMenu(menuName = "GOAP/Configs/WanderAgentTypeConfig")]
    public class WanderAgentTypeConfig : AgentTypeConfigBase
    {
        public override IAgentTypeConfig Create()
        {
            var config = new AgentTypeConfig("WanderAgent");

            // Add the Wander Goal
            config.AddGoal<WanderGoal>()
                .SetBaseCost(1);

            // Add the Wander Action
            config.AddAction<WanderAction>()
                .SetTarget<WanderTarget>()
                .SetBaseCost(1);

            // Add the Wander Target Sensor
            config.AddTargetSensor<WanderTargetSensor>()
                .SetTarget<WanderTarget>();

            return config;
        }
    }
}
