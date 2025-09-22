using CrashKonijn.Goap.Interfaces;

namespace GOAP.WorldKeys
{
    // This key represents whether the agent is at a guard post
    // World Keys are like variables that describe the world state
    public class IsAtGuardPostKey : IWorldKey
    {
        // Every WorldKey needs a Name property
        // This is used internally by the GOAP system to identify the key
        public string Name { get; } = "IsAtGuardPost";
    }
}