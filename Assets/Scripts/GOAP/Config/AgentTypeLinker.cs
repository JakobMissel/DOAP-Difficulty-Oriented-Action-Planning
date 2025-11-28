using CrashKonijn.Goap.Runtime;
using System.Linq;
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

            Debug.Log($"[AgentTypeLinker] Awake on {gameObject.name} - Provider: {(provider != null ? "Found" : "NULL")}, GoapBehaviour: {(goap != null ? "Found" : "NULL")}");

            // Link as early as possible so editor tools (Graph Viewer) can read live state immediately.
            LinkIfPossible();
        }

        private void Start()
        {
            Debug.Log($"[AgentTypeLinker] Start on {gameObject.name} - Attempting fallback link");

            // Retry finding GoapBehaviour if it was null in Awake
            if (goap == null)
            {
                Debug.Log($"[AgentTypeLinker] GoapBehaviour was null in Awake, retrying search in Start() for {gameObject.name}");
                goap = FindAnyObjectByType<GoapBehaviour>();

                if (goap != null)
                {
                    Debug.Log($"[AgentTypeLinker] Successfully found GoapBehaviour in Start() on {goap.gameObject.name}");
                }
                else
                {
                    Debug.LogError($"[AgentTypeLinker] Still cannot find GoapBehaviour in Start() for {gameObject.name}!");
                }
            }

            // Fallback in case GoapBehaviour was not yet available in Awake
            LinkIfPossible();
        }

        private void LinkIfPossible()
        {
            if (provider == null)
            {
                Debug.LogError($"[AgentTypeLinker] Provider is NULL on {gameObject.name}!", this);
                return;
            }

            if (goap == null)
            {
                Debug.LogError($"[AgentTypeLinker] GoapBehaviour is NULL on {gameObject.name}! Make sure there's a GameObject with GoapBehaviour in the scene.", this);
                return;
            }

            Debug.Log($"[AgentTypeLinker] Attempting to get AgentType '{agentTypeName}' from GoapBehaviour on {gameObject.name}");

            var agentType = goap.GetAgentType(agentTypeName);
            if (agentType == null)
            {
                Debug.LogError($"[AgentTypeLinker] AgentType '{agentTypeName}' not found on {gameObject.name}. Ensure a matching AgentTypeFactory (e.g., GuardAgentTypeFactory, LaserAgentTypeFactory) is added to GoapBehaviour > Agent Type Config Factories and uses the same Agent Type Name.", this);
                Debug.LogError($"[AgentTypeLinker] GoapBehaviour GameObject: {goap.gameObject.name}", this);
                return;
            }

            // If already linked to the same AgentType instance or matching id, skip
            if (provider.AgentType == agentType || (provider.AgentType != null && provider.AgentType.Id == agentTypeName))
            {
                Debug.Log($"[AgentTypeLinker] Already linked to AgentType '{agentTypeName}' on {gameObject.name}");
                return;
            }

            provider.AgentType = agentType;
            Debug.Log($"[AgentTypeLinker] Successfully linked provider on {gameObject.name} to AgentType '{agentTypeName}'");
        }
    }
}
