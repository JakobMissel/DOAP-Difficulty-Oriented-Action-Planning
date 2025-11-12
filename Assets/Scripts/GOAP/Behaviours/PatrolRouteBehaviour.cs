using UnityEngine;
using System.Linq;

namespace Assets.Scripts.GOAP.Behaviours
{
    public class PatrolRouteBehaviour : MonoBehaviour
    {
        [Header("Waypoint Configuration")]
        [Tooltip("Parent GameObject containing waypoints as children (leave empty to use tag-based search)")]
        public Transform waypointParent;
        
        [Tooltip("If true, uses children of waypointParent in hierarchy order. If false, uses FindGameObjectsWithTag.")]
        public bool useHierarchyOrder = true;

        [Header("NavMesh Snapping")]
        [Tooltip("Additional Y-offset to add after snapping to NavMesh (e.g., agent's base offset from floor)")]
        public float agentHeightOffset = 0.5f;

        private Transform[] waypoints;
        private int currentIndex = 0;
        private bool hasInitializedToClosest = false;

        private void Awake()
        {
            if (useHierarchyOrder && waypointParent != null)
            {
                // Use hierarchy order from the parent container
                int childCount = waypointParent.childCount;
                waypoints = new Transform[childCount];
                
                for (int i = 0; i < childCount; i++)
                {
                    waypoints[i] = waypointParent.GetChild(i);
                }
                
                Debug.Log($"[PatrolRouteBehaviour] Loaded {waypoints.Length} waypoints from hierarchy in order");
            }
            else
            {
                // Fallback: Collect all waypoints by tag and sort by name
                var objs = GameObject.FindGameObjectsWithTag("Waypoint");
                
                // Sort by name to get consistent ordering (e.g., Waypoint_1, Waypoint_2, etc.)
                waypoints = objs
                    .OrderBy(obj => obj.name)
                    .Select(obj => obj.transform)
                    .ToArray();
                
                Debug.Log($"[PatrolRouteBehaviour] Loaded {waypoints.Length} waypoints by tag, sorted by name");
            }

            // Snap all waypoints to NavMesh level + agent height offset
            SnapWaypointsToNavMesh();
        }

        private void SnapWaypointsToNavMesh()
        {
            if (waypoints == null)
                return;

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                    continue;

                Vector3 originalPos = waypoints[i].position;
                
                // Sample NavMesh position below the waypoint
                if (UnityEngine.AI.NavMesh.SamplePosition(originalPos, out var hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    // Move waypoint to NavMesh surface level + agent height offset
                    Vector3 snappedPos = hit.position;
                    snappedPos.y += agentHeightOffset;
                    waypoints[i].position = snappedPos;
                    
                    if (Vector3.Distance(originalPos, snappedPos) > 0.1f)
                    {
                        Debug.Log($"[PatrolRouteBehaviour] Snapped {waypoints[i].name}: Y {originalPos.y:F2} → {snappedPos.y:F2} (NavMesh {hit.position.y:F2} + offset {agentHeightOffset:F2})");
                    }
                }
                else
                {
                    Debug.LogWarning($"[PatrolRouteBehaviour] Could not snap {waypoints[i].name} to NavMesh! Position: {originalPos}");
                }
            }
        }

        /// <summary>
        /// Finds and sets the current index to the closest waypoint to the guard's position.
        /// Call this when starting patrol or resuming after pursuit.
        /// </summary>
        public void InitializeToClosestWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                Debug.LogWarning($"[PatrolRouteBehaviour] {gameObject.name} has no waypoints to initialize to!");
                return;
            }

            Vector3 currentPos = transform.position;
            float closestDistance = float.MaxValue;
            int closestIndex = 0;

            // Find the closest waypoint
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                    continue;

                float distance = Vector3.Distance(currentPos, waypoints[i].position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            currentIndex = closestIndex;
            hasInitializedToClosest = true;

            string waypointName = waypoints[currentIndex] != null ? waypoints[currentIndex].name : "null";
            Debug.Log($"[PatrolRouteBehaviour] {gameObject.name} initialized to closest waypoint {currentIndex} ({waypointName}) at distance {closestDistance:F2}m");
        }

        /// <summary>
        /// Resets the patrol route to start from the closest waypoint.
        /// Useful when resuming patrol after losing pursuit.
        /// </summary>
        public void ResetToClosestWaypoint()
        {
            hasInitializedToClosest = false;
            InitializeToClosestWaypoint();
        }

        public Transform GetCurrent()
        {
            if (waypoints == null || waypoints.Length == 0)
                return null;

            // Auto-initialize to closest waypoint on first call if not done yet
            if (!hasInitializedToClosest)
            {
                InitializeToClosestWaypoint();
            }

            return waypoints[currentIndex];
        }

        public void Advance()
        {
            if (waypoints == null || waypoints.Length == 0)
                return;

            // Check if we're being destroyed (scene unloading) - skip advancement
            if (this == null || !gameObject.activeInHierarchy)
                return;

            int previousIndex = currentIndex;
            currentIndex = (currentIndex + 1) % waypoints.Length;
            
            // Safety check: ensure waypoints still exist before accessing them
            string prevName = (waypoints[previousIndex] != null) ? waypoints[previousIndex].name : "destroyed";
            string currName = (waypoints[currentIndex] != null) ? waypoints[currentIndex].name : "destroyed";
            
            Debug.Log($"[PatrolRouteBehaviour] {gameObject.name} advanced from waypoint {previousIndex} ({prevName}) to {currentIndex} ({currName})");
        }

        /// <summary>
        /// Get the current waypoint index for debugging/diagnostics
        /// </summary>
        public int GetCurrentIndex()
        {
            return currentIndex;
        }

        /// <summary>
        /// Get total number of waypoints
        /// </summary>
        public int GetWaypointCount()
        {
            return waypoints != null ? waypoints.Length : 0;
        }
    }
}