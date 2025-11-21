using UnityEngine;

namespace Assets.Scripts.GOAP.Behaviours
{
    /// <summary>
    /// Visualization component for StandGuardPoints.
    /// Add this to the parent GameObject that contains StandGuardWatchAnglePoint children.
    /// Shows the guard point position, watch angle arc, and direction indicators.
    /// </summary>
    public class StandGuardPointGizmo : MonoBehaviour
    {
        [Header("Guard Point Info")]
        [Tooltip("Name of this guard point (e.g., 'Painting 1 Guard', 'Main Entrance Guard')")]
        public string pointName = "Guard Point";
        
        [Header("Visualization")]
        [Tooltip("Color for the guard point sphere")]
        public Color guardPointColor = Color.magenta;
        
        [Tooltip("Color for the watch angle indicators")]
        public Color watchAngleColor = Color.yellow;
        
        [Tooltip("Color for the view arc between angle points")]
        public Color viewArcColor = new Color(1f, 0.25f, 0f, 0.3f); // Semi-transparent
        
        [Tooltip("Size of the guard point sphere")]
        [Range(0.1f, 2f)]
        public float guardPointSize = 0.5f;
        
        [Tooltip("Size of the angle point spheres")]
        [Range(0.1f, 1f)]
        public float anglePointSize = 0.3f;
        
        [Tooltip("Show labels in Scene view")]
        public bool showLabels = true;
        
        [Tooltip("Draw view arc between angle points")]
        public bool showViewArc = true;

        private void OnDrawGizmos()
        {
            // Find the tagged StandGuardPoint child (the actual guard position)
            Transform guardPointTransform = null;
            var allChildren = GetComponentsInChildren<Transform>();
            
            foreach (var child in allChildren)
            {
                if (child == transform) continue; // Skip self
                
                if (child.CompareTag("StandGuardPoint"))
                {
                    guardPointTransform = child;
                    break;
                }
            }
            
            // If no tagged child found, use parent position as fallback
            Vector3 guardPosition = guardPointTransform != null ? guardPointTransform.position : transform.position;
            
            // Draw the main guard point at the tagged child's position
            Gizmos.color = guardPointColor;
            Gizmos.DrawWireSphere(guardPosition, guardPointSize);
            
            // Find all StandGuardWatchAnglePoint children
            var anglePoints = GetComponentsInChildren<Transform>();
            Transform anglePoint1 = null;
            Transform anglePoint2 = null;
            
            int angleCount = 0;
            foreach (var child in anglePoints)
            {
                if (child == transform) continue; // Skip self
                
                // Check if child has "StandGuardWatchAnglePoint" in name or has the tag
                if (child.name.Contains("StandGuardWatchAngle") || child.CompareTag("StandGuardWatchAnglePoint"))
                {
                    angleCount++;
                    if (anglePoint1 == null)
                        anglePoint1 = child;
                    else if (anglePoint2 == null)
                        anglePoint2 = child;
                    
                    // Draw angle point
                    Gizmos.color = watchAngleColor;
                    Gizmos.DrawWireSphere(child.position, anglePointSize);
                    
                    // Draw line from guard point (tagged child position) to angle point
                    Gizmos.DrawLine(guardPosition, child.position);
                    
#if UNITY_EDITOR
                    if (showLabels)
                    {
                        UnityEditor.Handles.Label(child.position + Vector3.up * 0.5f, child.name, 
                            new GUIStyle() { normal = new GUIStyleState() { textColor = watchAngleColor } });
                    }
#endif
                }
            }
            
            // Draw view arc between the two angle points
            if (showViewArc && anglePoint1 != null && anglePoint2 != null)
            {
                DrawViewArc(guardPosition, anglePoint1.position, anglePoint2.position);
            }
            
#if UNITY_EDITOR
            // Draw label for guard point at the tagged child's position
            if (showLabels)
            {
                string label = $"{pointName}\n({angleCount} angle points)";
                UnityEditor.Handles.Label(guardPosition + Vector3.up * (guardPointSize + 0.5f), label, 
                    new GUIStyle() { 
                        normal = new GUIStyleState() { textColor = guardPointColor },
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12,
                        fontStyle = FontStyle.Bold
                    });
            }
#endif
        }

        private void DrawViewArc(Vector3 guardPos, Vector3 point1, Vector3 point2)
        {

            // Draw filled arc representing guard's view cone
            Vector3 dir1 = (point1 - guardPos).normalized;
            Vector3 dir2 = (point2 - guardPos).normalized;
            
            float distance1 = Vector3.Distance(guardPos, point1);
            float distance2 = Vector3.Distance(guardPos, point2);
            float avgDistance = (distance1 + distance2) / 2f;
            
            // Calculate angle between directions
            float angle = Vector3.Angle(dir1, dir2);
            
            // Draw arc lines
            Gizmos.color = viewArcColor;
            
            int arcSegments = Mathf.Max(3, (int)(angle / 5f)); // More segments for larger angles
            Vector3 prevPoint = guardPos + dir1 * avgDistance;
            
            for (int i = 1; i <= arcSegments; i++)
            {
                float t = (float)i / arcSegments;
                Vector3 direction = Vector3.Slerp(dir1, dir2, t);
                Vector3 point = guardPos + direction * avgDistance;
                
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
            
            // Draw outer arc
            Gizmos.color = new Color(viewArcColor.r, viewArcColor.g, viewArcColor.b, viewArcColor.a * 2f);
            Gizmos.DrawLine(guardPos + dir1 * avgDistance, point1);
            Gizmos.DrawLine(guardPos + dir2 * avgDistance, point2);
        }

        private void OnDrawGizmosSelected()
        {
            // Find the tagged StandGuardPoint child (the actual guard position)
            Transform guardPointTransform = null;
            var allChildren = GetComponentsInChildren<Transform>();
            
            foreach (var child in allChildren)
            {
                if (child == transform) continue;
                if (child.CompareTag("StandGuardPoint"))
                {
                    guardPointTransform = child;
                    break;
                }
            }
            
            Vector3 guardPosition = guardPointTransform != null ? guardPointTransform.position : transform.position;
            
            // Draw larger, more visible gizmos when selected
            Gizmos.color = new Color(guardPointColor.r, guardPointColor.g, guardPointColor.b, 0.8f);
            Gizmos.DrawSphere(guardPosition, guardPointSize);
            
            // Draw detection radius (2.5m from proximity threshold, adjustable)
            Gizmos.color = new Color(guardPointColor.r, guardPointColor.g, guardPointColor.b, 0.1f);
            Gizmos.DrawWireSphere(guardPosition, 2.5f); // Detection radius
            
            // Highlight angle points
            var anglePoints = GetComponentsInChildren<Transform>();
            foreach (var child in anglePoints)
            {
                if (child == transform) continue;
                
                if (child.name.Contains("StandGuardWatchAngle") || child.CompareTag("StandGuardWatchAnglePoint"))
                {
                    Gizmos.color = new Color(watchAngleColor.r, watchAngleColor.g, watchAngleColor.b, 0.6f);
                    Gizmos.DrawSphere(child.position, anglePointSize);
                }
            }
        }
    }
}

