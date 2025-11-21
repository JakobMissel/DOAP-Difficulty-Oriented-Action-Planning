using UnityEngine;

namespace Assets.Scripts.GOAP.Behaviours
{
    /// <summary>
    /// Behaviour to track when a guard last stood guard.
    /// This allows the sensor to determine if the guard can stand guard again.
    /// </summary>
    public class StandGuardTimerBehaviour : MonoBehaviour
    {
        private float lastGuardTime = -1000f; // Start with a large negative value so guard can stand immediately
        
        /// <summary>
        /// Call this when the guard completes the StandGuard action
        /// </summary>
        public void StartCooldown()
        {
            lastGuardTime = Time.time;
            Debug.Log($"[StandGuardTimer] {gameObject.name} started cooldown at {lastGuardTime}");
        }
        
        /// <summary>
        /// Returns the time in seconds since the guard last stood guard
        /// </summary>
        public float GetTimeSinceLastGuard()
        {
            if (lastGuardTime < 0)
                return 1000f; // Return large value if never stood guard
                
            return Time.time - lastGuardTime;
        }
        
        /// <summary>
        /// Check if the cooldown period has passed
        /// </summary>
        public bool IsCooldownComplete(float cooldownDuration = 15f)
        {
            return GetTimeSinceLastGuard() >= cooldownDuration;
        }
    }
}

