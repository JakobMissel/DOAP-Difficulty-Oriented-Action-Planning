using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts.GOAP.Behaviours
{
    /// <summary>
    /// Velocity-based animation controller for guards.
    /// Updates Animator parameters based on NavMeshAgent velocity.
    /// Industry standard approach - actions don't control animations directly.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class GuardAnimationController : MonoBehaviour
    {
        [Header("References")]
        private Animator animator;
        private NavMeshAgent navAgent;
        private GuardAnimation guardAnimation; // Your existing animation wrapper

        [Header("Animation System")]
        [Tooltip("Use speed-based detection (recommended for synchronized guard speeds)")]
        [SerializeField] private bool useSpeedBasedDetection = true;

        [Header("Velocity Thresholds (m/s) - Legacy")]
        [Tooltip("Velocity below this = Idle animation")]
        [SerializeField] private float idleThreshold = 0.1f;
        [Tooltip("Velocity below this = Walk animation (above idle threshold)")]
        [SerializeField] private float walkThreshold = 2.5f;

        [Header("Speed-Based Animation (Recommended)")]
        [Tooltip("Match NavMeshAgent speed to animation state. More reliable than velocity.")]
        [SerializeField] private float normalPatrolSpeed = 2.6f;     // Should match GuardDetectionSpeedController.normalSpeed
        [SerializeField] private float detectionSpeed = 1.0f;         // Should match GuardDetectionSpeedController.detectingSpeed
        [SerializeField] private float spottedSpeed = 3.2f;           // Should match GuardDetectionSpeedController.spottedSpeed
        [SerializeField] private float investigationSpeed = 2.8f;     // Should match GuardDetectionSpeedController.noiseInvestigationSpeed
        
        [Tooltip("Speed tolerance for animation switching (prevents micro-adjustments from triggering changes)")]
        [SerializeField] private float speedTolerance = 0.3f;

        [Header("Hysteresis (Prevents Flickering)")]
        [Tooltip("Time in seconds an animation must be active before switching to another")]
        [SerializeField] private float animationSwitchCooldown = 0.15f;  // Reduced from 0.2 for snappier response

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Track current animation state to prevent rapid switching
        private enum AnimState { Idle, Walk, Run, Search, Recharge }
        private AnimState currentState = AnimState.Idle;
        private float lastStateChangeTime = 0f;

        // Track if we're in a forced state (Search/Recharge) that shouldn't be overridden by velocity
        private bool isInForcedState = false;

        private GuardSight sight;
        private GuardDetectionSpeedController speedController;

        private void Awake()
        {
            // Find Animator - check this GameObject first, then children
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    Debug.Log($"[GuardAnimationController] {name} found Animator on child: {animator.gameObject.name}");
                }
            }

            navAgent = GetComponent<NavMeshAgent>();
            sight = GetComponent<GuardSight>();
            speedController = GetComponent<GuardDetectionSpeedController>();

            // Find GuardAnimation - check this GameObject first, then children
            guardAnimation = GetComponent<GuardAnimation>();
            if (guardAnimation == null)
            {
                guardAnimation = GetComponentInChildren<GuardAnimation>();
                if (guardAnimation != null)
                {
                    Debug.Log($"[GuardAnimationController] {name} found GuardAnimation on child: {guardAnimation.gameObject.name}");
                }
            }

            if (animator == null)
            {
                Debug.LogError($"[GuardAnimationController] {name} missing Animator component (searched self and children)!");
            }
            else
            {
                Debug.Log($"[GuardAnimationController] {name} using Animator on: {animator.gameObject.name}");
            }

            if (navAgent == null)
            {
                Debug.LogError($"[GuardAnimationController] {name} missing NavMeshAgent component!");
            }

            if (guardAnimation == null)
            {
                Debug.LogError($"[GuardAnimationController] {name} missing GuardAnimation component!");
            }

            if (speedController == null)
            {
                Debug.LogWarning($"[GuardAnimationController] {name} missing GuardDetectionSpeedController - difficulty-aware animations disabled!");
            }

            Debug.Log($"[GuardAnimationController] {name} initialized - Speed-based mode: {useSpeedBasedDetection}, Animator: {(animator != null ? "✓" : "✗")}, NavAgent: {(navAgent != null ? "✓" : "✗")}, GuardAnimation: {(guardAnimation != null ? "✓" : "✗")}, SpeedController: {(speedController != null ? "✓" : "✗")}");
        }

        private void Update()
        {
            if (navAgent == null || guardAnimation == null)
                return;

            // Don't override forced states (Search/Recharge) with velocity-based animations
            if (isInForcedState)
                return;

            // Use speed-based detection (matches NavMeshAgent.speed to animation)
            if (useSpeedBasedDetection)
            {
                UpdateAnimationSpeedBased();
            }
            else
            {
                // Legacy velocity-based detection
                float currentVelocity = navAgent.velocity.magnitude;
                UpdateAnimationAbsolute(currentVelocity);
            }
        }

        /// <summary>
        /// Update animation using absolute velocity thresholds (m/s)
        /// Difficulty-aware: adjusts walkThreshold based on difficulty multiplier
        /// </summary>
        private void UpdateAnimationAbsolute(float velocity)
        {
            // Apply difficulty to walk threshold
            float difficultyMultiplier = speedController != null ? speedController.GetCurrentDifficultyMultiplier() : 1f;
            float adjustedWalkThreshold = walkThreshold * difficultyMultiplier;

            if (velocity < idleThreshold)
            {
                // Standing still
                if (showDebugLogs && Time.frameCount % 60 == 0)
                    Debug.Log($"[GuardAnimationController] {name} velocity {velocity:F2} m/s (threshold {idleThreshold:F2}) → IDLE");

                guardAnimation.Idle();
            }
            else if (velocity < adjustedWalkThreshold)
            {
                // Walking
                if (showDebugLogs && Time.frameCount % 60 == 0)
                    Debug.Log($"[GuardAnimationController] {name} velocity {velocity:F2} m/s (threshold {adjustedWalkThreshold:F2}, mult {difficultyMultiplier:F2}) → WALK");

                guardAnimation.Walk();
            }
            else
            {
                // Running
                if (showDebugLogs && Time.frameCount % 60 == 0)
                    Debug.Log($"[GuardAnimationController] {name} velocity {velocity:F2} m/s (threshold {adjustedWalkThreshold:F2}, mult {difficultyMultiplier:F2}) → RUN");

                guardAnimation.Run();
            }
        }

        /// <summary>
        /// Update animation based on NavMeshAgent.speed (more reliable than velocity)
        /// Since all guards use GuardDetectionSpeedController with synchronized speeds,
        /// we can map speed directly to animation state.
        /// This works perfectly with normalized animations because the speed values are consistent.
        /// </summary>
        private void UpdateAnimationSpeedBased()
        {
            float currentSpeed = navAgent.speed;
            float currentVelocity = navAgent.velocity.magnitude;
            
            // Get difficulty multiplier to adjust thresholds
            float difficultyMultiplier = speedController != null ? speedController.GetCurrentDifficultyMultiplier() : 1f;
            
            // Apply difficulty to speed thresholds
            float adjustedNormalSpeed = normalPatrolSpeed * difficultyMultiplier;
            float adjustedDetectionSpeed = detectionSpeed * difficultyMultiplier;
            float adjustedSpottedSpeed = spottedSpeed * difficultyMultiplier;
            float adjustedInvestigationSpeed = investigationSpeed * difficultyMultiplier;

            AnimState desiredState = currentState;

            // Determine animation based on current speed setting and actual velocity
            // If guard is barely moving (stopped/turning), always idle
            if (currentVelocity < idleThreshold)
            {
                desiredState = AnimState.Idle;
            }
            // If speed is set to spotted/pursuit speed AND moving, RUN
            else if (Mathf.Abs(currentSpeed - adjustedSpottedSpeed) < speedTolerance)
            {
                desiredState = AnimState.Run;
            }
            // If speed is set to investigation speed AND moving, could be Walk or Run depending on context
            else if (Mathf.Abs(currentSpeed - adjustedInvestigationSpeed) < speedTolerance)
            {
                // Investigation is typically walking pace, but check if we're pursuing
                bool isPursuing = sight != null && sight.PlayerSpotted();
                desiredState = isPursuing ? AnimState.Run : AnimState.Walk;
            }
            // If speed is set to detection speed (slowed down), WALK slowly
            else if (Mathf.Abs(currentSpeed - adjustedDetectionSpeed) < speedTolerance)
            {
                desiredState = AnimState.Walk;
            }
            // If speed is set to normal patrol speed AND moving, WALK
            else if (Mathf.Abs(currentSpeed - adjustedNormalSpeed) < speedTolerance)
            {
                desiredState = AnimState.Walk;
            }
            // Fallback: if moving at any speed, walk
            else if (currentVelocity >= idleThreshold)
            {
                // Use speed relative to normal patrol to decide Walk vs Run
                if (currentSpeed > adjustedNormalSpeed + speedTolerance)
                {
                    desiredState = AnimState.Run;
                }
                else
                {
                    desiredState = AnimState.Walk;
                }
            }

            // Apply animation with cooldown to prevent flickering
            float timeSinceLastSwitch = Time.time - lastStateChangeTime;
            if (desiredState != currentState && timeSinceLastSwitch >= animationSwitchCooldown)
            {
                ApplyAnimationState(desiredState, currentSpeed, currentVelocity, difficultyMultiplier);
            }
        }

        /// <summary>
        /// Apply the animation state and update tracking variables
        /// </summary>
        private void ApplyAnimationState(AnimState newState, float speed, float velocity, float difficultyMult)
        {
            currentState = newState;
            lastStateChangeTime = Time.time;

            switch (currentState)
            {
                case AnimState.Idle:
                    guardAnimation.Idle();
                    if (showDebugLogs)
                        Debug.Log($"[GuardAnimationController] {name} speed={speed:F2} (×{difficultyMult:F2}), velocity={velocity:F2} → IDLE");
                    break;

                case AnimState.Walk:
                    guardAnimation.Walk();
                    if (showDebugLogs)
                        Debug.Log($"[GuardAnimationController] {name} speed={speed:F2} (×{difficultyMult:F2}), velocity={velocity:F2} → WALK");
                    break;

                case AnimState.Run:
                    guardAnimation.Run();
                    if (showDebugLogs)
                        Debug.Log($"[GuardAnimationController] {name} speed={speed:F2} (×{difficultyMult:F2}), velocity={velocity:F2} → RUN");
                    break;
            }
        }

        /// <summary>
        /// Override animation state (for special cases like Search, Recharge)
        /// Call this from actions that need specific animations regardless of velocity
        /// </summary>
        public void OverrideAnimation(System.Action animationCall)
        {
            if (guardAnimation != null)
            {
                animationCall?.Invoke();
            }
        }

        /// <summary>
        /// Manually trigger specific animation (bypasses velocity logic)
        /// Useful for special states like searching, recharging, etc.
        /// These states are "sticky" - they won't be overridden by velocity until ClearForcedState() is called
        /// </summary>
        public void ForceSearch()
        {
            currentState = AnimState.Search;
            lastStateChangeTime = Time.time;
            isInForcedState = true;
            guardAnimation?.Search();

            if (showDebugLogs)
                Debug.Log($"[GuardAnimationController] {name} FORCE SEARCH (forced state locked)");
        }

        public void ForceRecharge()
        {
            currentState = AnimState.Recharge;
            lastStateChangeTime = Time.time;
            isInForcedState = true;
            guardAnimation?.Recharge();

            if (showDebugLogs)
                Debug.Log($"[GuardAnimationController] {name} FORCE RECHARGE (forced state locked)");
        }

        /// <summary>
        /// Clear the forced state and resume velocity-based animations
        /// Call this when special states (Search/Recharge) are finished
        /// </summary>
        public void ClearForcedState()
        {
            isInForcedState = false;

            if (showDebugLogs)
                Debug.Log($"[GuardAnimationController] {name} cleared forced state, resuming velocity-based animations");
        }
    }
}
