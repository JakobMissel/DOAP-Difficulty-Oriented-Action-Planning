using Assets.Scripts.GOAP.Goals;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Assets.Scripts.DDA;

namespace Assets.Scripts.GOAP.Behaviours
{
    public class BrainBehaviour : MonoBehaviour
    {
        private AgentBehaviour agent;
        private GoapActionProvider provider;
        private GoapBehaviour goap;
        
        // Global registry for efficient broadcasts (e.g., player noise)
        private static readonly HashSet<BrainBehaviour> ActiveBrains = new HashSet<BrainBehaviour>();
        public static IEnumerable<BrainBehaviour> GetActiveBrains() => ActiveBrains;
        private void OnEnable() => ActiveBrains.Add(this);
        private void OnDisable() => ActiveBrains.Remove(this);
        
        // Track if player has been caught
        public bool IsPlayerCaught { get; private set; } = false;

        // Track noise (both distraction and player-generated)
        public bool HasHeardNoise { get; private set; } = false;
        public Vector3 LastNoisePosition { get; private set; }
        public float LastNoiseRadius { get; private set; } = 0f;
        public bool IsPlayerNoise { get; private set; } = false; // Track if the noise was from player vs thrown object
        private float lastNoiseTime = -999f;

        [Tooltip("Seconds after a noise pulse during which audio can trigger proximity logic (if used)")] 
        public float audiblePursuitMemory = 0.75f;
        
        // Backwards compatibility properties
        public bool HasHeardDistractionNoise => HasHeardNoise;
        public Vector3 LastDistractionNoisePosition => LastNoisePosition;
        public bool HasHeardPlayerNoise => HasHeardNoise && IsPlayerNoise;
        public Vector3 LastPlayerNoisePosition => LastNoisePosition;
        public float LastHeardNoiseRadius => LastNoiseRadius;
        
        // Track last known player position
        public bool HasLastKnownPosition { get; private set; } = false;
        public Vector3 LastKnownPlayerPosition { get; private set; }
        
        // Track player re-entry to NavMesh after climbing
        public bool PlayerReEnteredNavMesh { get; private set; } = false;
        public Vector3 PlayerNavMeshReEntryPosition { get; private set; }

        // References
        private Transform playerTransform;
        private Assets.Scripts.Player.PlayerNavMeshTracker playerNavMeshTracker;
        private GuardSight sight;
        private bool wasSeenLastFrame = false;
        private bool hasInvestigated = false; // Prevent re-capturing after investigation
        private bool wasOffNavMeshDuringChase = false; // Track if player went off NavMesh during this chase

        [Header("NavMesh Settings")]
        [SerializeField] private float angularSpeed = 360f;
        [SerializeField] private float acceleration = 12f;
        
        [Header("Difficulty Scaling")]
        [Tooltip("Enable difficulty-based angular speed scaling")]
        [SerializeField] private bool scaleTurningSpeedWithDifficulty = true;
        [Tooltip("Angular speed multiplier at 0% difficulty")]
        [SerializeField] private float minAngularSpeedMultiplier = 0.9f;
        [Tooltip("Angular speed multiplier at 100% difficulty")]
        [SerializeField] private float maxAngularSpeedMultiplier = 1.1f;
        
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

        [Header("Hearing Settings")]
        [Tooltip("If true, player noise can be heard through walls. If false, a line-of-sight check is used and walls block player noise.")]
        public bool hearPlayerThroughWalls = false;
        [Tooltip("Layers that block hearing when 'hearPlayerThroughWalls' is false. Set this to the same layers as your sight obstacle mask.")]
        public LayerMask hearingObstructionMask;
        [Tooltip("Ray height offset (meters) used when checking hearing occlusion")] public float hearingRayHeight = 1.5f;

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
            this.sight = this.GetComponent<GuardSight>();
            this.playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            
            // Get player NavMesh tracker for re-entry detection
            if (this.playerTransform != null)
            {
                this.playerNavMeshTracker = this.playerTransform.GetComponent<Assets.Scripts.Player.PlayerNavMeshTracker>();
            }
            
            // Configure NavMeshAgent turning speed with difficulty scaling
            var navAgent = GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                float appliedAngularSpeed = angularSpeed;
                if (scaleTurningSpeedWithDifficulty)
                {
                    float difficultyMultiplier = GetAngularSpeedMultiplier();
                    appliedAngularSpeed = angularSpeed * difficultyMultiplier;
                    Debug.Log($"[BrainBehaviour] Set angular speed to {appliedAngularSpeed:F1} degrees/sec (base: {angularSpeed}, difficulty multiplier: {difficultyMultiplier:F2})");
                }
                else
                {
                    Debug.Log($"[BrainBehaviour] Set angular speed to {angularSpeed} degrees/sec");
                }
                
                navAgent.angularSpeed = appliedAngularSpeed;
                navAgent.acceleration = acceleration;
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
                typeof(InvestigateNoiseGoal),
                typeof(Assets.Scripts.GOAP.GoToLaserGoal),
                typeof(StandGuardGoal)
            });
        }

        private void Update()
        {
            if (playerTransform == null || sight == null)
                return;

            bool canSeePlayer = sight.CanSeePlayer();
            
            // Check if we should manually control rotation when detecting player (before fully spotted)
            bool isDetecting = canSeePlayer && !sight.PlayerSpotted();
            

            // Disable NavMeshAgent rotation when manually controlling it during detection
            var navAgent = GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                // Update angular speed with difficulty scaling periodically (once per second is enough)
                if (scaleTurningSpeedWithDifficulty && Time.frameCount % 60 == 0)
                {
                    float difficultyMultiplier = GetAngularSpeedMultiplier();
                    navAgent.angularSpeed = angularSpeed * difficultyMultiplier;
                }
                
                if (isDetecting)
                {
                    navAgent.updateRotation = false; // We'll handle rotation manually during detection
                }
                else
                {
                    navAgent.updateRotation = true; // Let NavMeshAgent handle rotation normally
                }
            }

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
                        Vector3 newPosition = ClampToNavMesh(playerTransform.position);
                        
                        // Check if player has moved significantly vertically (likely climbed a wall)
                        float verticalDifference = Mathf.Abs(newPosition.y - LastKnownPlayerPosition.y);
                        if (verticalDifference > 2.0f) // Player is now on a different level
                        {
                            // Stop following immediately - player is unreachable
                            isLastKnownFollowActive = false;
                            wasOffNavMeshDuringChase = true;
                            Debug.Log($"[BrainBehaviour] Player moved to different vertical level ({verticalDifference:F1}m difference). Ending follow window early. Will track re-entry.");
                        }
                        else
                        {
                            LastKnownPlayerPosition = newPosition;
                        }
                    }

                    if (lastKnownFollowTimer >= lastKnownChaseDuration)
                    {
                        isLastKnownFollowActive = false; // Freeze last-known here
                        Debug.Log($"[BrainBehaviour] Last-known follow window ended. Frozen at: {LastKnownPlayerPosition}");
                    }
                }
                // Check for player re-entry to NavMesh after they went off during chase
                else if (wasOffNavMeshDuringChase && HasLastKnownPosition && !hasInvestigated)
                {
                    if (playerNavMeshTracker != null && playerNavMeshTracker.HasReEnteredNavMesh)
                    {
                        // Update last known position to where player landed back on NavMesh
                        LastKnownPlayerPosition = playerNavMeshTracker.LastNavMeshReEntryPosition;
                        PlayerReEnteredNavMesh = true;
                        PlayerNavMeshReEntryPosition = playerNavMeshTracker.LastNavMeshReEntryPosition;
                        
                        Debug.Log($"[BrainBehaviour] {name} detected player re-entry to NavMesh at {LastKnownPlayerPosition}. Updating target.");
                        
                        // Don't clear the tracker flag yet - let ClearLastKnownAction use it
                    }
                }

                wasSeenLastFrame = false;
            }
            
            // Manually rotate towards player when detecting (before fully spotted)
            if (isDetecting && canSeePlayer)
            {
                Vector3 dirToPlayer = playerTransform.position - transform.position;
                dirToPlayer.y = 0f;
                if (dirToPlayer.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dirToPlayer.normalized, Vector3.up);
                    float rotationSpeed = 256f; // Degrees per second
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }
            }
        }
        
        public void SetPlayerCaught(bool caught)
        {
            IsPlayerCaught = caught;
            
            if (caught)
            {
                Debug.Log($"[BrainBehaviour] {name} has caught the player! GameOverManager will handle the rest.");
            }
        }
        
        // Clamp position to NavMesh (used in follow)
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
            wasOffNavMeshDuringChase = false;
            PlayerReEnteredNavMesh = false;
            
            // Clear the player's re-entry flag so it can detect new re-entries
            if (playerNavMeshTracker != null && playerNavMeshTracker.HasReEnteredNavMesh)
            {
                playerNavMeshTracker.ClearReEntryFlag();
            }
            
            Debug.Log("[BrainBehaviour] Cleared last known player position - marked as investigated");
        }
        
        // Backwards-compat: interpret as distraction noise
        public void OnNoiseHeard(Vector3 noisePosition)
        {
            OnDistractionNoiseHeard(noisePosition, 0f);
        }

        // Backwards-compat: interpret as distraction noise
        public void OnNoiseHeard(Vector3 noisePosition, float radius)
        {
            OnDistractionNoiseHeard(noisePosition, radius);
        }

        // Explicit API for thrown/distraction noise
        public void OnDistractionNoiseHeard(Vector3 noisePosition, float radius)
        {
            DdaPlayerActions.Instance.SuccesfulItemUsage();
            HasHeardNoise = true;
            LastNoisePosition = noisePosition;
            LastNoiseRadius = radius;
            IsPlayerNoise = false; // This is a distraction (thrown object)
            lastNoiseTime = Time.time;
            Debug.Log($"[BrainBehaviour] Distraction noise at {noisePosition} (r={radius:F1})");
        }

        // Explicit API for player-generated noise
        public void OnPlayerNoiseHeard(Vector3 noisePosition, float radius)
        {
            // If hearing through walls is disabled, check for occlusion
            if (!hearPlayerThroughWalls)
            {
                var from = transform.position + Vector3.up * Mathf.Max(0f, hearingRayHeight);
                var to = noisePosition + Vector3.up * Mathf.Max(0f, hearingRayHeight);
                if (IsHearingOccluded(from, to))
                {
                    // Ignore occluded player noise
                    Debug.Log($"[BrainBehaviour] Player noise at {noisePosition} was OCCLUDED by wall - not hearing it");
                    return;
                }
            }

            // Set unified noise properties - this will trigger GOAP investigation
            HasHeardNoise = true;
            LastNoisePosition = noisePosition;
            LastNoiseRadius = radius;
            IsPlayerNoise = true; // This is player-generated noise
            lastNoiseTime = Time.time;
            Debug.Log($"[BrainBehaviour] Player noise HEARD at {noisePosition} (r={radius:F1}) - will investigate via GOAP");
        }

        public void ClearDistractionNoise()
        {
            HasHeardNoise = false;
            IsPlayerNoise = false;
            Debug.Log("[BrainBehaviour] Noise cleared (was distraction)");
        }

        public void ClearPlayerNoise()
        {
            HasHeardNoise = false;
            IsPlayerNoise = false;
            Debug.Log("[BrainBehaviour] Noise cleared (was player)");
        }
        
        // Clear any noise (unified method)
        public void ClearNoise()
        {
            HasHeardNoise = false;
            IsPlayerNoise = false;
            Debug.Log("[BrainBehaviour] Noise cleared");
        }

        // Helper for sensors (if you ever want to use audio proximity again)
        public bool IsWithinAudiblePursuitWindow(Transform player)
        {
            if (player == null)
                return false;
            if (!IsPlayerNoise || Time.time - lastNoiseTime > audiblePursuitMemory)
                return false;

            float dist = Vector3.Distance(transform.position, player.position);
            return dist <= Mathf.Max(0f, LastNoiseRadius);
        }

        private bool IsHearingOccluded(Vector3 origin, Vector3 target)
        {
            Vector3 dir = target - origin;
            float dist = dir.magnitude;
            if (dist <= 0.01f)
                return false;
            dir /= dist;

            int mask = hearingObstructionMask.value == 0 ? Physics.DefaultRaycastLayers : hearingObstructionMask.value;
            // If a collider is hit in the obstruction mask, hearing is considered blocked
            return Physics.Raycast(origin, dir, dist, mask);
        }
        
        /// <summary>
        /// Calculate the current difficulty multiplier for angular speed based on game difficulty (0-1)
        /// Returns a value between minAngularSpeedMultiplier and maxAngularSpeedMultiplier
        /// </summary>
        private float GetAngularSpeedMultiplier()
        {
            float difficulty01 = DifficultyTracker.GetDifficultyF();
            return Mathf.Lerp(minAngularSpeedMultiplier, maxAngularSpeedMultiplier, difficulty01);
        }
    }
}
