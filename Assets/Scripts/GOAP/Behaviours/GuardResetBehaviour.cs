using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Agent.Runtime;

namespace Assets.Scripts.GOAP.Behaviours
{
    /// <summary>
    /// Tracks a guard's initial position and state for resetting on retry.
    /// </summary>
    public class GuardResetBehaviour : MonoBehaviour
    {
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool hasStoredInitialState = false;

        private NavMeshAgent navAgent;
        private BrainBehaviour brain;
        private GuardSight sight;
        private AgentBehaviour agent;
        private PatrolRouteBehaviour patrolRoute;
        private GuardCatchTrigger catchTrigger;

        private void Awake()
        {
            // Get references
            navAgent = GetComponent<NavMeshAgent>();
            brain = GetComponent<BrainBehaviour>();
            sight = GetComponent<GuardSight>();
            agent = GetComponent<AgentBehaviour>();
            patrolRoute = GetComponent<PatrolRouteBehaviour>();
            catchTrigger = GetComponent<GuardCatchTrigger>();
        }

        private void Start()
        {
            // Store initial position after the guard is fully initialized
            StoreInitialState();
        }

        private void OnEnable()
        {
            // Subscribe to checkpoint load event
            CheckpointManager.loadCheckpoint += ResetGuard;
        }

        private void OnDisable()
        {
            // Unsubscribe from checkpoint load event
            CheckpointManager.loadCheckpoint -= ResetGuard;
        }

        /// <summary>
        /// Stores the guard's initial position and rotation.
        /// </summary>
        public void StoreInitialState()
        {
            if (!hasStoredInitialState)
            {
                initialPosition = transform.position;
                initialRotation = transform.rotation;
                hasStoredInitialState = true;
                Debug.Log($"[GuardResetBehaviour] {name} stored initial position: {initialPosition}");
            }
        }

        /// <summary>
        /// Resets the guard to their initial position and clears their state.
        /// </summary>
        public void ResetGuard()
        {
            if (!hasStoredInitialState)
            {
                Debug.LogWarning($"[GuardResetBehaviour] {name} has no stored initial state to reset to!");
                return;
            }

            Debug.Log($"[GuardResetBehaviour] {name} resetting to initial position: {initialPosition}");

            // Stop and reset NavMeshAgent
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.ResetPath();
                navAgent.velocity = Vector3.zero;
                navAgent.isStopped = true;
            }

            // Reset position and rotation
            transform.position = initialPosition;
            transform.rotation = initialRotation;

            // Warp agent after position change
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.Warp(initialPosition);
                navAgent.isStopped = false;
            }

            // Clear brain state
            if (brain != null)
            {
                brain.ClearLastKnownPlayerPosition();
                brain.ClearDistractionNoise();
                brain.ClearPlayerNoise();
                brain.SetPlayerCaught(false); // Reset the caught state so player can be caught again
            }

            // Reset sight detection
            if (sight != null)
            {
                sight.ResetDetection();
            }

            // Reset patrol route to start from initial position
            if (patrolRoute != null)
            {
                patrolRoute.ResetToClosestWaypoint();
            }

            // Force agent to re-evaluate goals and return to patrol
            if (agent != null)
            {
                // Clear current action to force re-planning
                agent.StopAction(true);
            }

            // Reset catch trigger state
            if (catchTrigger != null)
            {
                catchTrigger.ResetCatch();
            }

            Debug.Log($"[GuardResetBehaviour] {name} reset complete");
        }
    }
}
