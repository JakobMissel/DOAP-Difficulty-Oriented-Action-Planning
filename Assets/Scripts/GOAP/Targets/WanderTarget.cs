using CrashKonijn.Goap.Behaviours;

namespace GOAP.Targets
{
	public class WanderTarget : TargetKeyBase
	{
		// TargetKeyBase already provides the necessary implementation
		// The cost and conditions are configured via the config system
		// You can override GetCost if you need dynamic cost calculation

		// Example of custom cost calculation (optional):
		// public override float GetCost(IActionReceiver agent, IComponentReference references)
		// {
		//     return this.Config.BaseCost; // or your custom calculation
		// }
	}
}