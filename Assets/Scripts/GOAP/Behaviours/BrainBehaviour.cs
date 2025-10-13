using Assets.Scripts.GOAP.Goals;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;
using Assets.Scripts.GOAP;

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
            // Add all goals to a provider (now includes InvestigateNoiseGoal)
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

                // Calculate velocity
                if (hasTrackedPosition)
                {
                    Vector3 positionDelta = currentPlayerPos - lastTrackedPlayerPosition;
                    Vector3 instantVelocity = positionDelta / Time.deltaTime;
                    
                    // Smooth velocity to avoid jitter
                    playerVelocity = Vector3.Lerp(playerVelocity, instantVelocity, 0.3f);
                    
                    // Only log occasionally to avoid spam
                    if (Time.frameCount % 30 == 0)
                    {
                        Debug.Log($"[BrainBehaviour] Tracking velocity: {playerVelocity.magnitude:F2} m/s, Direction: {playerVelocity.normalized}");
                    }
                }
                
                lastTrackedPlayerPosition = currentPlayerPos;
                hasTrackedPosition = true;
                wasSeenLastFrame = true;
                hasInvestigated = false; // Reset investigation flag when we see the player again
            }
            else
            {
                // Player left sight this frame - ONLY capture position on the transition frame AND if not already investigated
                if (wasSeenLastFrame && !HasLastKnownPosition && !hasInvestigated)
                {
                    Vector3 predictedPosition = CalculatePredictedPosition();
                    LastKnownPlayerPosition = predictedPosition;
                    HasLastKnownPosition = true;
                    
                    Debug.Log($"[BrainBehaviour] ★ Player left sight! Last: {lastTrackedPlayerPosition}, Velocity: {playerVelocity} ({playerVelocity.magnitude:F2} m/s), Predicted: {predictedPosition}");
                }
                
                // IMPORTANT: Only set wasSeenLastFrame to false AFTER the check above
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
        
        // Calculate predicted position based on player velocity
        private Vector3 CalculatePredictedPosition()
        {
            if (!usePrediction || !hasTrackedPosition)
            {
                Debug.Log("[BrainBehaviour] Using actual last seen position (no prediction)");
                return lastTrackedPlayerPosition;
            }
            
            // Check if player was moving fast enough to predict
            float speed = playerVelocity.magnitude;
            if (speed < minVelocityThreshold)
            {
                Debug.Log($"[BrainBehaviour] Player moving too slowly ({speed:F2} m/s) - using actual position");
                return lastTrackedPlayerPosition;
            }
            
            // Predict where the player might be based on their velocity
            Vector3 predictedPosition = lastTrackedPlayerPosition + (playerVelocity * predictionTime);
            
            Debug.Log($"[BrainBehaviour] Calculation - LastPos: {lastTrackedPlayerPosition}, Velocity: {playerVelocity}, Result: {predictedPosition}");
            
            // Clamp to NavMesh to ensure it's a valid position
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(predictedPosition, out hit, 5.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                Debug.Log($"[BrainBehaviour] ✓ Predicted position on NavMesh: {hit.position} (speed: {speed:F2} m/s, {predictionTime}s ahead)");
                return hit.position;
            }
            
            // If predicted position is off NavMesh, use last known position
            Debug.Log($"[BrainBehaviour] ✗ Predicted position OFF NavMesh - using actual: {lastTrackedPlayerPosition}");
            return lastTrackedPlayerPosition;
        }
        
        // Method to clear last known position (called after investigation is complete)
        public void ClearLastKnownPlayerPosition()
        {
            HasLastKnownPosition = false;
            hasInvestigated = true; // Mark that we've investigated - don't re-capture until we see player again
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