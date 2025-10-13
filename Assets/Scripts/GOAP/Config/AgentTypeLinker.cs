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
        }

        private void Start()
        {
            if (provider == null || goap == null)
                return;

            var agentType = goap.GetAgentType(agentTypeName);
            if (agentType == null)
            {
                Debug.LogError($"[AgentTypeLinker] AgentType '{agentTypeName}' not found. Ensure GuardAgentTypeFactory is added to GoapBehaviour > Agent Type Config Factories and uses the same name.");
                return;
            }

            provider.AgentType = agentType;
            Debug.Log($"[AgentTypeLinker] Linked provider to AgentType '{agentTypeName}'.");
        }
    }
}

