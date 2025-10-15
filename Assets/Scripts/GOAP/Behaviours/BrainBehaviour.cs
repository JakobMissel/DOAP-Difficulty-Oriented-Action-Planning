using Assets.Scripts.GOAP.Goals;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts.GOAP.Behaviours
{
    public class BrainBehaviour : MonoBehaviour
    {
        private AgentBehaviour agent;
        private GoapActionProvider provider;
        private GoapBehaviour goap;
        
        // Track if player has been caught
        public bool IsPlayerCaught { get; private set; } = false;

        // Track noise/distraction
        public bool HasHeardNoise { get; private set; } = false;
        public Vector3 LastNoisePosition { get; private set; }
        public float NoiseHearingRadius = 20f; // How far the agent can hear noises
        
        // Track last known player position
        public bool HasLastKnownPosition { get; private set; } = false;
        public Vector3 LastKnownPlayerPosition { get; private set; }

        // References
        private Transform playerTransform;
        private SimpleGuardSightNiko sight;
        private bool wasSeenLastFrame = false;
        private bool hasInvestigated = false; // Prevent re-capturing after investigation

        [Header("NavMesh Settings")]
        [SerializeField] private float angularSpeed = 360f;
        [SerializeField] private float acceleration = 12f;
        
        [Header("Last-known Follow Settings")] 
        [Tooltip("How long after losing sight we keep updating the last-known position")] 
        [SerializeField] private float lastKnownChaseDuration = 4.0f; 
        [Tooltip("How often we refresh the last-known position during the follow window")] 
        [SerializeField] private float lastKnownUpdateInterval = 0.1f;

        [Header("ClearLastKnown Scan Settings")]
        [Tooltip("Seconds to scan left/right at the last-known position")] 
        public float scanDuration = 1.5f;
        [Tooltip("Degrees to either side during scan")] 
        public float scanAngle = 75f;
        [Tooltip("Seconds per full left-right-left oscillation")] 
        public float scanSweepTime = 1.5f;
        [Tooltip("Fallback distance considered 'arrived' if NavMeshAgent remainingDistance is unreliable")] 
        public float arriveDistance = 1.5f;

        // Follow-window state
        private bool isLastKnownFollowActive; 
        private float lastKnownFollowTimer; 
        private float lastKnownUpdateAccumulator;
        public bool IsLastKnownFollowActive => isLastKnownFollowActive;

        private void Awake()
        {
            this.goap = FindAnyObjectByType<GoapBehaviour>();
            this.agent = this.GetComponent<AgentBehaviour>();
            this.provider = this.GetComponent<GoapActionProvider>();
            this.sight = this.GetComponent<SimpleGuardSightNiko>();
            this.playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            
            // Configure NavMeshAgent turning speed
            var navAgent = GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.angularSpeed = angularSpeed;
                navAgent.acceleration = acceleration;
                Debug.Log($"[BrainBehaviour] Set angular speed to {angularSpeed} degrees/sec");
            }
        }

        private void Start()
        {
            // Add all goals to the provider
            this.provider.RequestGoal(new[]
            {
                typeof(PatrolGoal),
                typeof(PursuitGoal),
                typeof(CatchGoal),
                typeof(ClearLastKnownGoal),
                typeof(RechargeGoal),
                typeof(InvestigateNoiseGoal)
            });
        }

        private void Update()
        {
            if (playerTransform == null || sight == null)
                return;

            bool canSeePlayer = sight.CanSeePlayer();

            if (canSeePlayer)
            {
                wasSeenLastFrame = true;
                hasInvestigated = false;

                // If we see the player, we don't need last-known state
                if (HasLastKnownPosition)
                    HasLastKnownPosition = false;

                // Cancel any follow window
                isLastKnownFollowActive = false;
                lastKnownFollowTimer = 0f;
                lastKnownUpdateAccumulator = 0f;
            }
            else
            {
                // Just lost sight this frame
                if (wasSeenLastFrame && !hasInvestigated)
                {
                    // Start follow window: keep updating last-known to player's transform for a short time
                    LastKnownPlayerPosition = ClampToNavMesh(playerTransform.position);
                    HasLastKnownPosition = true;

                    isLastKnownFollowActive = true;
                    lastKnownFollowTimer = 0f;
                    lastKnownUpdateAccumulator = 0f;

                    Debug.Log($"[BrainBehaviour] Lost sight. Starting last-known follow for {lastKnownChaseDuration:F1}s. Initial: {LastKnownPlayerPosition}");
                }
                // Continue follow updates while window is active
                else if (isLastKnownFollowActive)
                {
                    lastKnownFollowTimer += Time.deltaTime;
                    lastKnownUpdateAccumulator += Time.deltaTime;

                    if (lastKnownUpdateAccumulator >= Mathf.Max(0.01f, lastKnownUpdateInterval))
                    {
                        lastKnownUpdateAccumulator = 0f;
                        LastKnownPlayerPosition = ClampToNavMesh(playerTransform.position);
                    }

                    if (lastKnownFollowTimer >= lastKnownChaseDuration)
                    {
                        isLastKnownFollowActive = false; // Freeze last-known here
                        Debug.Log($"[BrainBehaviour] Last-known follow window ended. Frozen at: {LastKnownPlayerPosition}");
                    }
                }

                wasSeenLastFrame = false;
            }
        }
        
        public void SetPlayerCaught(bool caught)
        {
            IsPlayerCaught = caught;
            
            if (caught)
            {
                Debug.Log("[BrainBehaviour] Player has been caught!");
            }
        }
        
        // NEW: Clamp position to NavMesh (used in follow)
        private Vector3 ClampToNavMesh(Vector3 pos)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(pos, out hit, 5.0f, NavMesh.AllAreas))
                return hit.position;
            return pos;
        }
        
        public void ClearLastKnownPlayerPosition()
        {
            HasLastKnownPosition = false;
            hasInvestigated = true;
            isLastKnownFollowActive = false;
            lastKnownFollowTimer = 0f;
            lastKnownUpdateAccumulator = 0f;
            Debug.Log("[BrainBehaviour] Cleared last known player position - marked as investigated");
        }
        
        public void OnNoiseHeard(Vector3 noisePosition)
        {
            float distance = Vector3.Distance(transform.position, noisePosition);
            Debug.Log($"[BrainBehaviour] Noise made at {noisePosition}, distance: {distance:F1}m");
            if (distance <= NoiseHearingRadius)
            {
                HasHeardNoise = true;
                LastNoisePosition = noisePosition;
                Debug.Log($"[BrainBehaviour] Heard noise at {noisePosition}, distance: {distance:F1}m");
            }
        }

        public void ClearNoise()
        {
            HasHeardNoise = false;
            Debug.Log("[BrainBehaviour] Noise cleared");
        }
    }
}
