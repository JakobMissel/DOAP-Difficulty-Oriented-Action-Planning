using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Assets.Scripts.GOAP.Config
{
    [DefaultExecutionOrder(-50)]
    public class AgentTypeLinker : MonoBehaviour
    {
        [SerializeField] private string agentTypeName = "Guard";

        private GoapActionProvider provider;
        private GoapBehaviour goap;

        private void Awake()
        {
            provider = GetComponent<GoapActionProvider>();
            goap = FindAnyObjectByType<GoapBehaviour>();

            // Link as early as possible so editor tools (Graph Viewer) can read live state immediately.
            LinkIfPossible();
        }

        private void Start()
        {
            // Fallback in case GoapBehaviour was not yet available in Awake
            LinkIfPossible();
        }

        private void LinkIfPossible()
        {
            if (provider == null || goap == null)
                return;

            var agentType = goap.GetAgentType(agentTypeName);
            if (agentType == null)
            {
                Debug.LogError($"[AgentTypeLinker] AgentType '{agentTypeName}' not found. Ensure a matching AgentTypeFactory (e.g., GuardAgentTypeFactory, LaserAgentTypeFactory) is added to GoapBehaviour > Agent Type Config Factories and uses the same Agent Type Name.");
                return;
            }

            // If already linked to the same AgentType instance or matching id, skip
            if (provider.AgentType == agentType || (provider.AgentType != null && provider.AgentType.Id == agentTypeName))
                return;

            provider.AgentType = agentType;
            Debug.Log($"[AgentTypeLinker] Linked provider to AgentType '{agentTypeName}'.");
        }
    }
}
