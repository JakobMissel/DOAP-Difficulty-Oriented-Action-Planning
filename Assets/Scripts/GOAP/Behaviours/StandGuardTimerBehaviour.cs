using UnityEngine;

namespace Assets.Scripts.GOAP.Behaviours
{
    /// <summary>
    /// Behaviour to track when a guard last stood guard.
    /// This allows the sensor to determine if the guard can stand guard again.
    /// </summary>
    public class StandGuardTimerBehaviour : MonoBehaviour
    {
        [Header("Timing Configuration")]
        [SerializeField] [Tooltip("How long the guard stands at the guard point (in seconds)")]
        private float guardDuration = 10f;
        
        [SerializeField] [Tooltip("Cooldown duration after standing guard (in seconds)")]
        private float cooldownDuration = 15f;

        [SerializeField] [Tooltip("Cooldown duration after being interrupted while standing guard (in seconds)")]
        private float interruptionCooldownDuration = 8f;

        [Header("Movement Configuration")]
        [SerializeField] [Tooltip("How close the guard needs to be to the guard point to stop")]
        private float arrivalThreshold = 1.5f;

        [SerializeField] [Tooltip("Speed of rotation between angle points")]
        private float rotationSpeed = 2f;

        private float guardStartTime;
        private bool isGuarding;

        // Track if actively performing StandGuardAction
        private bool isCurrentlyStandingGuard;
        private float lastStandGuardEndTime;
        
        // Public properties for action to access
        public float GuardDuration => guardDuration;
        public float ArrivalThreshold => arrivalThreshold;
        public float RotationSpeed => rotationSpeed;
        
        /// <summary>
        /// Public property to check if currently guarding (0 or 1 for GOAP)
        /// </summary>
        public bool IsGuarding => isGuarding;

        /// <summary>
        /// Returns true if the guard is ACTIVELY performing StandGuardAction right now
        /// </summary>
        public bool IsCurrentlyStandingGuard => isCurrentlyStandingGuard;

        /// <summary>
        /// Returns time since the last time StandGuardAction ended (either completed or interrupted)
        /// </summary>
        public float TimeSinceLastStandGuard => lastStandGuardEndTime > 0 ? Time.time - lastStandGuardEndTime : 1000f;
        
        /// <summary>
        /// Call this when StandGuardAction STARTS
        /// </summary>
        public void BeginStandGuardAction()
        {
            isCurrentlyStandingGuard = true;
            Debug.Log($"[StandGuardTimer] {gameObject.name} BEGAN StandGuardAction (IsCurrentlyStandingGuard = 1)");
        }

        /// <summary>
        /// Call this when StandGuardAction ENDS (either completed or interrupted)
        /// </summary>
        public void EndStandGuardAction(bool wasInterrupted)
        {
            isCurrentlyStandingGuard = false;
            lastStandGuardEndTime = Time.time;

            if (wasInterrupted)
            {
                Debug.Log($"[StandGuardTimer] {gameObject.name} was INTERRUPTED during StandGuardAction - {interruptionCooldownDuration}s cooldown started");
            }
            else
            {
                Debug.Log($"[StandGuardTimer] {gameObject.name} COMPLETED StandGuardAction normally");
            }
        }

        /// <summary>
        /// Check if enough time has passed since last StandGuard to allow it again
        /// </summary>
        public bool CanStandGuardAgain()
        {
            // If currently standing guard, can't do it again
            if (isCurrentlyStandingGuard)
                return false;

            // If never stood guard before, can do it
            if (lastStandGuardEndTime <= 0)
                return true;

            // Check if cooldown has passed
            float requiredCooldown = interruptionCooldownDuration;
            return TimeSinceLastStandGuard >= requiredCooldown;
        }

        /// <summary>
        /// Call this when the guard COMPLETES the StandGuard action (sets IsGuarding to 1)
        /// This is the old method, kept for backwards compatibility
        /// </summary>
        public void StartGuarding()
        {
            isGuarding = true;
            guardStartTime = Time.time;
            Debug.Log($"[StandGuardTimer] {gameObject.name} started guarding cooldown (IsGuarding = 1)");
        }
        
        /// <summary>
        /// Returns the time in seconds since guard duty started (for sensor)
        /// If not guarding, returns time since cooldown started
        /// </summary>
        public float GetTimeSinceGuardStart()
        {
            if (guardStartTime <= 0)
                return 1000f; // Never guarded, return large value
                
            return Time.time - guardStartTime;
        }
        
        /// <summary>
        /// Update checks if cooldown is complete and resets state
        /// </summary>
        private void Update()
        {
            // If guarding and cooldown period has passed
            if (isGuarding && GetTimeSinceGuardStart() >= cooldownDuration)
            {
                // Reset to not guarding
                isGuarding = false;
                guardStartTime = 0f;
                Debug.Log($"[StandGuardTimer] {gameObject.name} cooldown complete - reset (IsGuarding = 0, timer = 0)");
            }
        }
        
        /// <summary>
        /// Check if the cooldown period has passed (for manual checks)
        /// </summary>
        public bool IsCooldownComplete()
        {
            return !isGuarding && guardStartTime <= 0;
        }
    }
}

