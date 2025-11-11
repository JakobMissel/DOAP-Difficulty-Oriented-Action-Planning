using UnityEngine;

namespace Assets.Scripts.GOAP.Behaviours
{
    /// <summary>
    /// Simple container component that marks a GameObject as a patrol zone.
    /// Add this to an empty GameObject, then make waypoints children of it.
    /// Assign this zone to a guard's PatrolRouteBehaviour.waypointParent.
    /// </summary>
    public class GuardPatrolZone : MonoBehaviour
    {
        [Header("Zone Info")]
        [Tooltip("Name of this patrol zone (e.g., 'Main Hall', 'East Wing', 'Entire Museum')")]
        public string zoneName = "Patrol Zone";
        
        [Header("Visualization")]
        [Tooltip("Color to draw patrol route in Scene view")]
        public Color gizmoColor = Color.yellow;
        
        [Tooltip("Show waypoint numbers in Scene view")]
        public bool showWaypointNumbers = true;

        private void OnDrawGizmos()
        {
            if (transform.childCount < 2)
                return;

            Gizmos.color = gizmoColor;
            
            // Draw lines connecting waypoints
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform current = transform.GetChild(i);
                Transform next = transform.GetChild((i + 1) % transform.childCount);
                
                Gizmos.DrawLine(current.position, next.position);
                Gizmos.DrawWireSphere(current.position, 0.3f);
                
#if UNITY_EDITOR
                if (showWaypointNumbers)
                {
                    UnityEditor.Handles.Label(current.position + Vector3.up * 0.5f, $"{i}", 
                        new GUIStyle() { normal = new GUIStyleState() { textColor = gizmoColor } });
                }
#endif
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw larger spheres when selected
            Gizmos.color = gizmoColor;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform waypoint = transform.GetChild(i);
                Gizmos.DrawWireSphere(waypoint.position, 0.5f);
            }
        }
    }
}

