using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Tracks when the player enters and exits the NavMesh surface.
    /// This allows guards to know where the player landed after climbing off the NavMesh.
    /// </summary>
    public class PlayerNavMeshTracker : MonoBehaviour
    {
        [Header("NavMesh Detection")]
        [Tooltip("Maximum distance to check for NavMesh below the player")]
        [SerializeField] private float navMeshCheckDistance = 2.0f;
        
        [Tooltip("How often to check NavMesh status (seconds)")]
        [SerializeField] private float checkInterval = 0.2f;
        
        [Tooltip("Minimum time off NavMesh before tracking re-entry (prevents flickering)")]
        [SerializeField] private float minOffNavMeshTime = 0.5f;

        // Current state
        public bool IsOnNavMesh { get; private set; }
        
        // Re-entry tracking
        public bool HasReEnteredNavMesh { get; private set; }
        public Vector3 LastNavMeshReEntryPosition { get; private set; }
        public float TimeOfLastReEntry { get; private set; }
        
        private bool wasOnNavMesh;
        private float timeOffNavMesh;
        private float nextCheckTime;

        private void Start()
        {
            IsOnNavMesh = CheckIfOnNavMesh();
            wasOnNavMesh = IsOnNavMesh;
            HasReEnteredNavMesh = false;
        }

        private void Update()
        {
            // Throttle checks for performance
            if (Time.time < nextCheckTime)
                return;
                
            nextCheckTime = Time.time + checkInterval;
            
            bool currentlyOnNavMesh = CheckIfOnNavMesh();
            
            // Track time off NavMesh
            if (!currentlyOnNavMesh)
            {
                timeOffNavMesh += checkInterval;
            }
            else
            {
                timeOffNavMesh = 0f;
            }
            
            // Detect re-entry: was off NavMesh long enough, now back on
            if (!wasOnNavMesh && currentlyOnNavMesh && timeOffNavMesh >= minOffNavMeshTime)
            {
                OnReEnteredNavMesh();
            }
            
            wasOnNavMesh = currentlyOnNavMesh;
            IsOnNavMesh = currentlyOnNavMesh;
        }

        private bool CheckIfOnNavMesh()
        {
            NavMeshHit hit;
            Vector3 checkPosition = transform.position;
            
            // Check if there's a NavMesh surface within range
            if (NavMesh.SamplePosition(checkPosition, out hit, navMeshCheckDistance, NavMesh.AllAreas))
            {
                // Additional check: make sure we're actually close to the surface
                float distanceToSurface = Vector3.Distance(checkPosition, hit.position);
                return distanceToSurface <= navMeshCheckDistance;
            }
            
            return false;
        }

        private void OnReEnteredNavMesh()
        {
            HasReEnteredNavMesh = true;
            
            // Store the clamped position on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, navMeshCheckDistance, NavMesh.AllAreas))
            {
                LastNavMeshReEntryPosition = hit.position;
            }
            else
            {
                LastNavMeshReEntryPosition = transform.position;
            }
            
            TimeOfLastReEntry = Time.time;
            
            Debug.Log($"[PlayerNavMeshTracker] Player re-entered NavMesh at {LastNavMeshReEntryPosition}");
        }

        /// <summary>
        /// Clear the re-entry flag (called by guards when they acknowledge the re-entry)
        /// </summary>
        public void ClearReEntryFlag()
        {
            HasReEnteredNavMesh = false;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
                
            // Draw current NavMesh status
            Gizmos.color = IsOnNavMesh ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // Draw last re-entry position
            if (HasReEnteredNavMesh)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(LastNavMeshReEntryPosition, 0.3f);
                Gizmos.DrawLine(transform.position, LastNavMeshReEntryPosition);
            }
        }
    }
}

