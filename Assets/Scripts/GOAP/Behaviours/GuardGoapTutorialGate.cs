using System.Collections.Generic;
using Assets.Scripts.GOAP.Behaviours;
using CrashKonijn.Agent.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace GOAP.Behaviours
{
    /// <summary>
    /// Gates all GOAP guard agents (tagged "Agent") behind the tutorial.
    /// At game start, while the tutorial is active, all guard brains are disabled.
    /// When the tutorial completes, guards are re-enabled and begin patrolling.
    /// </summary>
    public class GuardGoapTutorialGate : MonoBehaviour
    {
        [Tooltip("Tag used to find GOAP agents that should be gated by the tutorial.")]
        [SerializeField] private string agentTag = "Agent";

        private readonly List<BrainRecord> brains = new List<BrainRecord>();

        private class BrainRecord
        {
            public GameObject Root;
            public BrainBehaviour Brain;
            public AgentBehaviour Agent;
            public NavMeshAgent NavAgent;
            public GuardResetBehaviour ResetBehaviour;
        }

        private void Awake()
        {
            CacheAgents();
        }

        private void OnEnable()
        {
            PlayerActions.tutorialCompletion += OnTutorialCompleted;

            // If the tutorial is already completed when we enable, just ensure guards are active.
            if (ObjectivesManager.Instance != null && ObjectivesManager.Instance.completedTutorial)
            {
                EnableAgents();
            }
            else
            {
                DisableAgents();
            }
        }

        private void OnDisable()
        {
            PlayerActions.tutorialCompletion -= OnTutorialCompleted;
        }

        private void CacheAgents()
        {
            brains.Clear();

            var tagged = GameObject.FindGameObjectsWithTag(agentTag);
            foreach (var go in tagged)
            {
                var brain = go.GetComponent<BrainBehaviour>();
                if (brain == null)
                    continue; // Only interested in GOAP guards

                var record = new BrainRecord
                {
                    Root = go,
                    Brain = brain,
                    Agent = go.GetComponent<AgentBehaviour>(),
                    NavAgent = go.GetComponent<NavMeshAgent>(),
                    ResetBehaviour = go.GetComponent<GuardResetBehaviour>()
                };

                brains.Add(record);
            }
        }

        private void DisableAgents()
        {
            foreach (var r in brains)
            {
                if (r.Root != null)
                    r.Root.SetActive(false);

                // Components are disabled implicitly when the root is inactive, but
                // we keep the explicit calls here in case root toggling is changed later.
                if (r.Brain != null)
                    r.Brain.enabled = false;

                if (r.Agent != null)
                    r.Agent.enabled = false;

                if (r.NavAgent != null)
                {
                    r.NavAgent.isStopped = true;
                    r.NavAgent.velocity = Vector3.zero;
                }
            }
        }

        private void EnableAgents()
        {
            foreach (var r in brains)
            {
                if (r.Root != null && !r.Root.activeSelf)
                    r.Root.SetActive(true);

                if (r.ResetBehaviour != null)
                {
                    // Ensure a clean starting state when waking up
                    r.ResetBehaviour.ResetGuard();
                }

                if (r.NavAgent != null)
                {
                    r.NavAgent.isStopped = false;
                }

                if (r.Agent != null)
                    r.Agent.enabled = true;

                if (r.Brain != null)
                    r.Brain.enabled = true;
            }
        }

        private void OnTutorialCompleted()
        {
            EnableAgents();
        }
    }
}
