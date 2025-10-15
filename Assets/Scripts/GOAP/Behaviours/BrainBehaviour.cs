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

        // NEW: Track player velocity for prediction
        private Transform playerTransform;
        private SimpleGuardSightNiko sight;
        private Vector3 lastTrackedPlayerPosition = Vector3.zero;
        private Vector3 playerVelocity = Vector3.zero;
        private bool wasSeenLastFrame = false;
        private bool hasTrackedPosition = false;
        private bool hasInvestigated = false; // NEW: Prevent re-capturing after investigation

        [Header("NavMesh Settings")]
        [SerializeField] private float angularSpeed = 360f;
        [SerializeField] private float acceleration = 12f;
        
        [Header("Prediction Settings")]
        [SerializeField] private float predictionTime = 1.5f;
        [SerializeField] private bool usePrediction = true;
        [SerializeField] private float minVelocityThreshold = 0.5f;

        [Header("Last-known Follow Settings")] 
        [Tooltip("How long after losing sight we keep updating the last-known position")] 
        [SerializeField] private float lastKnownChaseDuration = 5.0f; 
        [Tooltip("How often we refresh the last-known position during the follow window")] 
        [SerializeField] private float lastKnownUpdateInterval = 0.1f;

        // NEW: simple follow-window state (replaces velocity-based prediction)
        private bool isLastKnownFollowActive; 
        private float lastKnownFollowTimer; 
        private float lastKnownUpdateAccumulator;

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

        // NEW: Track player velocity every frame
        private void Update()
        {
            if (playerTransform == null || sight == null)
                return;

            bool canSeePlayer = sight.CanSeePlayer();

            if (canSeePlayer)
            {
                Vector3 currentPlayerPos = playerTransform.position;

                // Track last seen to support seamless hand-off
                if (hasTrackedPosition)
                {
                    Vector3 positionDelta = currentPlayerPos - lastTrackedPlayerPosition;
                    Vector3 instantVelocity = positionDelta / Time.deltaTime;
                    playerVelocity = Vector3.Lerp(playerVelocity, instantVelocity, 0.3f);
                }

                lastTrackedPlayerPosition = currentPlayerPos;
                hasTrackedPosition = true;
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
        
        // Method to set player caught state (called from PursuitAction or other sources)
        public void SetPlayerCaught(bool caught)
        {
            IsPlayerCaught = caught;
            
            if (caught)
            {
                Debug.Log("[BrainBehaviour] Player has been caught!");
            }
        }
        
        // SIMPLIFIED: Sensor just checks if we can see, doesn't calculate velocity
        public void UpdatePlayerVisibility(bool canSee, Vector3 currentPlayerPosition)
        {
            // This method is now mostly unused since we track in Update()
            // But we keep it for compatibility with the sensor
        }
        
        // NEW: Clamp position to NavMesh (used in prediction and follow)
        private Vector3 ClampToNavMesh(Vector3 pos)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(pos, out hit, 5.0f, NavMesh.AllAreas))
                return hit.position;
            return pos;
        }

        // Calculate predicted position based on player velocity
        private Vector3 CalculatePredictedPosition()
        {
            // Kept for reference, but no longer used by the follow-window approach.
            if (!usePrediction || !hasTrackedPosition)
                return lastTrackedPlayerPosition;
            float speed = playerVelocity.magnitude;
            if (speed < minVelocityThreshold)
                return lastTrackedPlayerPosition;
            Vector3 predictedPosition = lastTrackedPlayerPosition + (playerVelocity * predictionTime);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(predictedPosition, out hit, 5.0f, NavMesh.AllAreas))
                return hit.position;
            return lastTrackedPlayerPosition;
        }
        
        // Method to clear last known position (called after investigation is complete)
        public void ClearLastKnownPlayerPosition()
        {
            HasLastKnownPosition = false;
            hasInvestigated = true;
            isLastKnownFollowActive = false;
            lastKnownFollowTimer = 0f;
            lastKnownUpdateAccumulator = 0f;
            Debug.Log("[BrainBehaviour] Cleared last known player position - marked as investigated");
        }
        
        // Method called when a noise is heard (called from NoiseArea or thrown object)
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

        // Method to clear noise after investigation
        public void ClearNoise()
        {
            HasHeardNoise = false;
            Debug.Log("[BrainBehaviour] Noise cleared");
        }
    }
}

