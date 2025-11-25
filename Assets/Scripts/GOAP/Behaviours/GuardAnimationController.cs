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

        [Header("Velocity Thresholds (m/s)")]
        [Tooltip("Velocity below this = Idle animation")]
        [SerializeField] private float idleThreshold = 0.1f;

        [Tooltip("Velocity below this = Walk animation (above idle threshold) \n Velocity above walk threshold = Run animation")]
        [SerializeField] private float walkThreshold = 2.5f;
        // Run threshold is implicit (anything above walkThreshold)

        [Header("Alternative: Normalized Velocity (0-1)")]
        [Tooltip("Use normalized velocity (0-1) instead of raw m/s. Better for variable speeds.")]
        [SerializeField] private bool useNormalizedVelocity = true;

        [Tooltip("Normalized velocity thresholds when using normalized mode")]
        [SerializeField] private float normalizedIdleThreshold = 0.05f;
        [SerializeField] private float normalizedWalkThreshold = 0.5f;  // Lowered from 0.6 to reduce flickering
        [SerializeField] private float normalizedRunThreshold = 0.7f;   // Hysteresis: need higher velocity to switch TO run

        [Header("Hysteresis (Prevents Flickering)")]
        [Tooltip("Time in seconds an animation must be active before switching to another")]
        [SerializeField] private float animationSwitchCooldown = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Track current animation state to prevent rapid switching
        private enum AnimState { Idle, Walk, Run, Search, Recharge }
        private AnimState currentState = AnimState.Idle;
        private float lastStateChangeTime = 0f;

        // Track if we're in a forced state (Search/Recharge) that shouldn't be overridden by velocity
        private bool isInForcedState = false;

        private GuardSight sight;
        private BrainBehaviour brain;

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
            brain = GetComponent<BrainBehaviour>();

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

            Debug.Log($"[GuardAnimationController] {name} initialized - Normalized mode: {useNormalizedVelocity}, Animator: {(animator != null ? "✓" : "✗")}, NavAgent: {(navAgent != null ? "✓" : "✗")}, GuardAnimation: {(guardAnimation != null ? "✓" : "✗")}");
        }

        private void Update()
        {
            if (navAgent == null || guardAnimation == null)
                return;

            // Don't override forced states (Search/Recharge) with velocity-based animations
            if (isInForcedState)
                return;

            // Get current velocity magnitude
            float currentVelocity = navAgent.velocity.magnitude;

            // Determine animation state based on velocity
            if (useNormalizedVelocity)
            {
                UpdateAnimationNormalized(currentVelocity);
            }
            else
            {
                UpdateAnimationAbsolute(currentVelocity);
            }
        }

        /// <summary>
        /// Update animation using absolute velocity thresholds (m/s)
        /// </summary>
        private void UpdateAnimationAbsolute(float velocity)
        {
            if (velocity < idleThreshold)
            {
                // Standing still
                if (showDebugLogs && Time.frameCount % 60 == 0)
                    Debug.Log($"[GuardAnimationController] {name} velocity {velocity:F2} m/s → IDLE");

                guardAnimation.Idle();
            }
            else if (velocity < walkThreshold)
            {
                // Walking
                if (showDebugLogs && Time.frameCount % 60 == 0)
                    Debug.Log($"[GuardAnimationController] {name} velocity {velocity:F2} m/s → WALK");

                guardAnimation.Walk();
            }
            else
            {
                // Running
                if (showDebugLogs && Time.frameCount % 60 == 0)
                    Debug.Log($"[GuardAnimationController] {name} velocity {velocity:F2} m/s → RUN");

                guardAnimation.Run();
            }
        }

        /// <summary>
        /// Update animation using normalized velocity (0-1 scale)
        /// Better for guards with variable speeds due to difficulty
        /// Uses hysteresis to prevent rapid animation switching
        /// </summary>
        private void UpdateAnimationNormalized(float velocity)
        {
            // Normalize velocity: 0 = stopped, 1 = max speed
            float maxSpeed = navAgent.speed; // Current max speed (can change with difficulty)
            float normalizedVelocity = maxSpeed > 0 ? velocity / maxSpeed : 0f;

            AnimState desiredState = currentState;

            // Determine if guard should RUN based on pursuit-related states or high speed
            // Run if: actively pursuing, following last known position, or moving at high speed
            bool isPursuing = sight != null && (sight.PlayerSpotted() || sight.CanSeePlayer());
            bool isFollowingLastKnown = brain != null && brain.HasLastKnownPosition;
            bool isHighSpeed = maxSpeed >= 3.2f; // GoToLaser (3.5) and Pursuit (3.2)

            bool shouldRun = isPursuing || isFollowingLastKnown || isHighSpeed;

            // Determine desired state based on velocity with hysteresis
            if (normalizedVelocity < normalizedIdleThreshold)
            {
                desiredState = AnimState.Idle;
            }
            else if (shouldRun && normalizedVelocity >= normalizedWalkThreshold)
            {
                // High-speed action (pursuit/investigation) and moving - RUN
                desiredState = AnimState.Run;
            }
            else if (normalizedVelocity >= normalizedWalkThreshold)
            {
                // Normal speed action (patrol) and moving - WALK
                desiredState = AnimState.Walk;
            }
            else if (normalizedVelocity < normalizedWalkThreshold)
            {
                // Moving slowly - WALK
                desiredState = AnimState.Walk;
            }
            else
            {
                // Fallback to walk
                desiredState = AnimState.Walk;
            }

            // Only switch animation if enough time has passed (cooldown prevents flickering)
            float timeSinceLastSwitch = Time.time - lastStateChangeTime;
            if (desiredState != currentState && timeSinceLastSwitch >= animationSwitchCooldown)
            {
                currentState = desiredState;
                lastStateChangeTime = Time.time;

                // Apply the animation
                switch (currentState)
                {
                    case AnimState.Idle:
                        if (showDebugLogs)
                            Debug.Log($"[GuardAnimationController] {name} normalized velocity {normalizedVelocity:F2} ({velocity:F2} m/s) → IDLE");
                        guardAnimation.Idle();
                        break;

                    case AnimState.Walk:
                        if (showDebugLogs)
                            Debug.Log($"[GuardAnimationController] {name} normalized velocity {normalizedVelocity:F2} ({velocity:F2} m/s) → WALK");
                        guardAnimation.Walk();
                        break;

                    case AnimState.Run:
                        if (showDebugLogs)
                            Debug.Log($"[GuardAnimationController] {name} normalized velocity {normalizedVelocity:F2} ({velocity:F2} m/s) → RUN");
                        guardAnimation.Run();
                        break;
                }
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
