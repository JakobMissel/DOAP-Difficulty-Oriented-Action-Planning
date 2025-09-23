using UnityEngine;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Interfaces;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Sensors;

namespace GOAP.Sensors
{
	public class WanderTargetSensor : LocalTargetSensorBase
	{
		public override void Created()
		{
		}
		public override void Update()
		{
		}
		public override ITarget Sense(IMonoAgent agent, IComponentReference references)
		{
			Vector3 position = GetRandomPosition(agent); ;
			return new PositionTarget(position);
		}
		private Vector3 GetRandomPosition(IMonoAgent agent)
		{
			// Generate a random position around the agent
			var agentPosition = agent.transform.position;
			var randomOffset = Random.insideUnitSphere * 10f;
			randomOffset.y = 0; // Keep on ground level
			return agentPosition + randomOffset;
		}
	}
}