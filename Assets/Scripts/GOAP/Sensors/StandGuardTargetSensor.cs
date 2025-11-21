using UnityEngine;
using UnityEngine.AI;
using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("StandGuardTargetSensor-b8c9d0e1-f2a3-4b4c-5d6e-7f8a9b0c1d2e")]
    public class StandGuardTargetSensor : LocalTargetSensorBase
    {
        private const float UPDATE_INTERVAL = 0.2f; // Check every 0.2 seconds
        
        private float lastUpdateTime = 0f;
        private Vector3 cachedTargetPosition;
        private bool hasCachedTarget = false;
        private GameObject[] cachedGuardPoints;

        public override void Created()
        {
            Debug.Log("[StandGuardTargetSensor] Created() called - searching for StandGuardPoint tag...");
            
            // Cache guard points on creation
            cachedGuardPoints = GameObject.FindGameObjectsWithTag("StandGuardPoint");
            
            Debug.Log($"[StandGuardTargetSensor] Found {cachedGuardPoints?.Length ?? 0} StandGuardPoint(s) in scene");
            
            if (cachedGuardPoints != null && cachedGuardPoints.Length > 0)
            {
                // Snap all guard points to NavMesh surface
                SnapGuardPointsToNavMesh();
                
                foreach (var point in cachedGuardPoints)
                {
                    if (point != null)
                    {
                        Debug.Log($"[StandGuardTargetSensor]   - {point.name} at position {point.transform.position}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[StandGuardTargetSensor] NO StandGuardPoint objects found!");
            }
        }
        
        /// <summary>
        /// Snaps all guard points to the nearest NavMesh surface to ensure they're on walkable ground
        /// </summary>
        private void SnapGuardPointsToNavMesh()
        {
            if (cachedGuardPoints == null || cachedGuardPoints.Length == 0)
                return;
            
            Debug.Log("[StandGuardTargetSensor] Starting NavMesh snapping process...");
            int snappedCount = 0;
            
            foreach (var point in cachedGuardPoints)
            {
                if (point == null)
                    continue;
                
                Vector3 originalPos = point.transform.position;
                
                // Sample NavMesh within 5 meters vertically to find nearest valid position
                if (NavMesh.SamplePosition(originalPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    // Snap the point to the NavMesh surface
                    point.transform.position = hit.position;
                    
                    float verticalDiff = Mathf.Abs(originalPos.y - hit.position.y);
                    if (verticalDiff > 0.01f)
                    {
                        Debug.Log($"[StandGuardTargetSensor] Snapped '{point.name}' to NavMesh: {originalPos:F2} → {hit.position:F2} (moved {verticalDiff:F2}m vertically)");
                        snappedCount++;
                    }
                }
                else
                {
                    Debug.LogWarning($"[StandGuardTargetSensor] Could not snap '{point.name}' to NavMesh! Point may be too far from walkable surface. Original position: {originalPos}");
                }
            }
            
            if (snappedCount > 0)
            {
                Debug.Log($"[StandGuardTargetSensor] Snapped {snappedCount} guard point(s) to NavMesh surface");
            }
            else
            {
                Debug.Log("[StandGuardTargetSensor] No guard points needed snapping (already on NavMesh)");
            }
        }
        
        public override void Update() { }

        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            // Check if we need to update (every 0.2 seconds)
            if (hasCachedTarget && Time.time - lastUpdateTime < UPDATE_INTERVAL)
            {
                if (existingTarget is PositionTarget pt)
                    return pt.SetPosition(cachedTargetPosition);
                    
                return new PositionTarget(cachedTargetPosition);
            }
            
            lastUpdateTime = Time.time;
            
            // Refresh guard points list if it was null or empty
            if (cachedGuardPoints == null || cachedGuardPoints.Length == 0)
            {
                cachedGuardPoints = GameObject.FindGameObjectsWithTag("StandGuardPoint");
                if (cachedGuardPoints != null && cachedGuardPoints.Length > 0)
                {
                    Debug.Log($"[StandGuardTargetSensor] Found {cachedGuardPoints.Length} StandGuardPoint(s)");
                }
                else
                {
                    Debug.LogWarning("[StandGuardTargetSensor] No StandGuardPoint objects found! Make sure tag exists and is applied.");
                }
            }
            
            if (cachedGuardPoints == null || cachedGuardPoints.Length == 0)
            {
                hasCachedTarget = false;
                return null;
            }

            // Find the nearest guard point (2D distance, ignore Y) that is visible (not behind wall)
            GameObject nearestPoint = null;
            float nearestDistance = float.MaxValue;
            Vector3 agentPos = agent.Transform.position;

            foreach (var point in cachedGuardPoints)
            {
                if (point != null)
                {
                    Vector3 pointPos = point.transform.position;
                    
                    // Calculate 2D distance (ignore Y-axis)
                    float dx = agentPos.x - pointPos.x;
                    float dz = agentPos.z - pointPos.z;
                    float distance2D = Mathf.Sqrt(dx * dx + dz * dz);
                    
                    if (distance2D < nearestDistance)
                    {
                        // Check line of sight - raycast to ensure no walls blocking
                        Vector3 directionToPoint = (pointPos - agentPos).normalized;
                        float actualDistance = Vector3.Distance(agentPos, pointPos);
                        
                        bool isVisible = true;
                        
                        // Create layer mask that ignores floor (assumes floor is on Default layer or has "Floor" layer)
                        // This allows seeing through floors but blocks on walls
                        int layerMask = ~LayerMask.GetMask("Ground", "FloorNavMesh", "Museum Stuff"); // Ignore these layers
                        
                        // Raycast from agent to guard point (ignoring floors)
                        if (Physics.Raycast(agentPos + Vector3.up * 0.5f, directionToPoint, out RaycastHit hit, actualDistance, layerMask))
                        {
                            // If we hit something before reaching the point, check if it's a wall
                            if (hit.collider.gameObject != point && !hit.collider.isTrigger)
                            {
                                // Hit a wall or obstacle before reaching the point
                                Debug.Log($"[StandGuardTargetSensor] Point '{point.name}' blocked by '{hit.collider.name}' (layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}) at distance {hit.distance:F2}m - skipping");
                                isVisible = false;
                            }
                        }
                        
                        if (isVisible)
                        {
                            nearestDistance = distance2D;
                            nearestPoint = point;
                        }
                    }
                }
            }

            if (nearestPoint == null)
            {
                hasCachedTarget = false;
                Debug.LogWarning("[StandGuardTargetSensor] No visible guard points found! All points may be behind walls.");
                return null;
            }

            // Cache the target position
            cachedTargetPosition = nearestPoint.transform.position;
            hasCachedTarget = true;

            // Debug log every update to see what target is returned
            if (Time.frameCount % 300 == 0) // Log every 5 seconds
            {
                Debug.Log($"[StandGuardTargetSensor] Returning target: {nearestPoint.name} at {cachedTargetPosition} (distance: {nearestDistance:F2}m)");
            }

            if (existingTarget is PositionTarget posTarget)
                return posTarget.SetPosition(cachedTargetPosition);

            return new PositionTarget(cachedTargetPosition);
        }
    }
}

