using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts.GOAP.Behaviours
{
    /// <summary>
    /// Controls the guard's movement speed based on detection state.
    /// Slows down during detection charge-up, speeds up when fully spotted or detection resets.
    /// </summary>
    [RequireComponent(typeof(GuardSight))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class GuardDetectionSpeedController : MonoBehaviour
    {
        [Header("Speed Settings")]
        [SerializeField] private float normalSpeed = 2.6f;
        [SerializeField] private float detectingSpeed = 1.0f;
        [SerializeField] private float spottedSpeed = 3.2f;
        [SerializeField] private float laserInvestigationSpeed = 3.5f;
        [SerializeField] private float noiseInvestigationSpeed = 2.8f;
        
        [Header("Transition Settings")]
        [SerializeField] private float speedTransitionSpeed = 5f;
        [Tooltip("Detection charge percentage above which guard starts slowing down (0-1)")]
        [SerializeField] private float detectionSlowdownThreshold = 0.1f;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        private GuardSight sight;
        private NavMeshAgent navAgent;
        
        private float targetSpeed;
        private float currentSpeed;
        
        private bool wasDetecting = false;
        private bool wasSpotted = false;
        
        // Track current action state
        private bool isInvestigatingLaser = false;
        private bool isInvestigatingNoise = false;

        private void Awake()
        {
            sight = GetComponent<GuardSight>();
            navAgent = GetComponent<NavMeshAgent>();
            
            if (sight == null)
            {
                Debug.LogError($"[GuardDetectionSpeedController] No GuardSight found on {name}!");
            }
            
            if (navAgent == null)
            {
                Debug.LogError($"[GuardDetectionSpeedController] No NavMeshAgent found on {name}!");
            }
            
            // Initialize speeds
            currentSpeed = normalSpeed;
            targetSpeed = normalSpeed;
            
            if (navAgent != null)
            {
                navAgent.speed = normalSpeed;
            }
        }

        private void Update()
        {
            if (sight == null || navAgent == null)
                return;

            UpdateSpeedBasedOnDetectionState();
            
            // Smoothly transition to target speed
            if (Mathf.Abs(currentSpeed - targetSpeed) > 0.01f)
            {
                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedTransitionSpeed * Time.deltaTime);
                navAgent.speed = currentSpeed;
            }
        }

        private void UpdateSpeedBasedOnDetectionState()
        {
            // Check if currently performing special investigation actions first
            if (isInvestigatingLaser)
            {
                SetTargetSpeed(laserInvestigationSpeed, "Investigating Laser");
                return;
            }
            
            if (isInvestigatingNoise)
            {
                SetTargetSpeed(noiseInvestigationSpeed, "Investigating Noise");
                return;
            }
            
            bool canSee = sight.CanSeePlayer();
            bool isSpotted = sight.PlayerSpotted();
            
            // Get detection charge level (0 to 1)
            float detectionCharge = GetDetectionCharge();

            if (isSpotted)
            {
                // Fully spotted - go fast!
                SetTargetSpeed(spottedSpeed, "Spotted");
                wasSpotted = true;
                wasDetecting = false;
            }
            else if (canSee && detectionCharge > detectionSlowdownThreshold)
            {
                // Detecting player - slow down
                SetTargetSpeed(detectingSpeed, "Detecting");
                wasDetecting = true;
                wasSpotted = false;
            }
            else
            {
                // Normal patrol speed
                if (wasDetecting || wasSpotted)
                {
                    SetTargetSpeed(normalSpeed, "Back to Normal");
                }
                else
                {
                    SetTargetSpeed(normalSpeed, null);
                }
                
                wasDetecting = false;
                wasSpotted = false;
            }
        }

        private float GetDetectionCharge()
        {
            // Now we can accurately read the detection charge from GuardSight
            return sight.DetectionCharge;
        }

        private void SetTargetSpeed(float newSpeed, string reason)
        {
            if (Mathf.Abs(targetSpeed - newSpeed) > 0.01f)
            {
                targetSpeed = newSpeed;
                
                if (debugMode && !string.IsNullOrEmpty(reason))
                {
                    Debug.Log($"[GuardDetectionSpeedController] {name} speed changing to {newSpeed:F1} ({reason})");
                }
            }
        }

        /// <summary>
        /// Set when the guard starts investigating a laser alert
        /// </summary>
        public void SetInvestigatingLaser(bool investigating)
        {
            isInvestigatingLaser = investigating;
            if (debugMode)
            {
                Debug.Log($"[GuardDetectionSpeedController] {name} investigating laser: {investigating}");
            }
        }
        
        /// <summary>
        /// Set when the guard starts investigating a noise
        /// </summary>
        public void SetInvestigatingNoise(bool investigating)
        {
            isInvestigatingNoise = investigating;
            if (debugMode)
            {
                Debug.Log($"[GuardDetectionSpeedController] {name} investigating noise: {investigating}");
            }
        }

        /// <summary>
        /// Manually set the normal patrol speed
        /// </summary>
        public void SetNormalSpeed(float speed)
        {
            normalSpeed = speed;
        }

        /// <summary>
        /// Manually set the detecting speed
        /// </summary>
        public void SetDetectingSpeed(float speed)
        {
            detectingSpeed = speed;
        }

        /// <summary>
        /// Manually set the spotted speed
        /// </summary>
        public void SetSpottedSpeed(float speed)
        {
            spottedSpeed = speed;
        }
        
        /// <summary>
        /// Manually set the laser investigation speed
        /// </summary>
        public void SetLaserInvestigationSpeed(float speed)
        {
            laserInvestigationSpeed = speed;
        }
        
        /// <summary>
        /// Manually set the noise investigation speed
        /// </summary>
        public void SetNoiseInvestigationSpeed(float speed)
        {
            noiseInvestigationSpeed = speed;
        }

        private void OnValidate()
        {
            // Ensure speeds make sense
            if (detectingSpeed > normalSpeed)
            {
                Debug.LogWarning($"[GuardDetectionSpeedController] Detecting speed should be lower than normal speed!");
            }
            
            if (spottedSpeed < normalSpeed)
            {
                Debug.LogWarning($"[GuardDetectionSpeedController] Spotted speed should be higher than normal speed!");
            }
        }
    }
}
