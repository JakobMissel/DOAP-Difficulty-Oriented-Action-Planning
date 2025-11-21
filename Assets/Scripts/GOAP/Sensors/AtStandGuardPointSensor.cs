using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Goap.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("AtStandGuardPointSensor-f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0c")]
    public class AtStandGuardPointSensor : LocalWorldSensorBase
    {
        private const float PROXIMITY_THRESHOLD = 5f;
        private const float UPDATE_INTERVAL = 0.1f; // Check every 0.2 seconds
        
        private float lastUpdateTime = 0f;
        private int cachedValue = 0;
        private GameObject[] cachedGuardPoints;

        public override void Created()
        {
            Debug.Log("[AtStandGuardPointSensor] Created() called - searching for StandGuardPoint tag...");
            
            // Cache guard points on creation
            cachedGuardPoints = GameObject.FindGameObjectsWithTag("StandGuardPoint");
            
            Debug.Log($"[AtStandGuardPointSensor] Found {cachedGuardPoints?.Length ?? 0} StandGuardPoint(s) in scene");
            
            if (cachedGuardPoints != null && cachedGuardPoints.Length > 0)
            {
                // Snap all guard points to NavMesh surface
                SnapGuardPointsToNavMesh();
                
                foreach (var point in cachedGuardPoints)
                {
                    if (point != null)
                    {
                        Debug.Log($"[AtStandGuardPointSensor]   - {point.name} at position {point.transform.position}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[AtStandGuardPointSensor] NO StandGuardPoint objects found! Make sure:");
                Debug.LogWarning("  1. Tag 'StandGuardPoint' exists in Tags & Layers");
                Debug.LogWarning("  2. GameObjects have this tag applied");
                Debug.LogWarning("  3. GameObjects are active in hierarchy");
            }
        }
        
        /// <summary>
        /// Snaps all guard points to the nearest NavMesh surface to ensure they're on walkable ground
        /// </summary>
        private void SnapGuardPointsToNavMesh()
        {
            if (cachedGuardPoints == null || cachedGuardPoints.Length == 0)
                return;
            
            Debug.Log("[AtStandGuardPointSensor] Starting NavMesh snapping process...");
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
                        Debug.Log($"[AtStandGuardPointSensor] Snapped '{point.name}' to NavMesh: {originalPos:F2} → {hit.position:F2} (moved {verticalDiff:F2}m vertically)");
                        snappedCount++;
                    }
                }
                else
                {
                    Debug.LogWarning($"[AtStandGuardPointSensor] Could not snap '{point.name}' to NavMesh! Point may be too far from walkable surface. Original position: {originalPos}");
                }
            }
            
            if (snappedCount > 0)
            {
                Debug.Log($"[AtStandGuardPointSensor] Snapped {snappedCount} guard point(s) to NavMesh surface");
            }
            else
            {
                Debug.Log("[AtStandGuardPointSensor] No guard points needed snapping (already on NavMesh)");
            }
        }
        
        public override void Update() { }

        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            // Log first call
            if (lastUpdateTime == 0)
            {
                Debug.Log($"[AtStandGuardPointSensor] First Sense() call for {agent.Transform.name}");
            }
            
            // Check if we need to update (every 0.2 seconds)
            if (Time.time - lastUpdateTime < UPDATE_INTERVAL)
            {
                return new SenseValue(cachedValue);
            }
            
            lastUpdateTime = Time.time;
            
            // Refresh guard points list if it was null or empty
            if (cachedGuardPoints == null || cachedGuardPoints.Length == 0)
            {
                cachedGuardPoints = GameObject.FindGameObjectsWithTag("StandGuardPoint");
                if (cachedGuardPoints == null || cachedGuardPoints.Length == 0)
                {
                    Debug.LogWarning("[AtStandGuardPointSensor] No StandGuardPoint objects found! Make sure tag exists and is applied.");
                }
            }
            
            if (cachedGuardPoints == null || cachedGuardPoints.Length == 0)
            {
                cachedValue = 0;
                return new SenseValue(0);
            }

            // Check if any guard point is within threshold meters (2D distance, ignore Y) AND visible
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
                    
                    if (distance2D <= PROXIMITY_THRESHOLD)
                    {
                        // Check line of sight - raycast to ensure no walls blocking
                        Vector3 directionToPoint = (pointPos - agentPos).normalized;
                        float actualDistance = Vector3.Distance(agentPos, pointPos);
                        
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
                                Debug.Log($"[AtStandGuardPointSensor] Point '{point.name}' blocked by '{hit.collider.name}' (layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}) - skipping");
                                continue;
                            }
                        }
                        
                        // Point is near AND visible
                        cachedValue = 1;
                        Debug.Log($"[AtStandGuardPointSensor] Agent is NEAR guard point '{point.name}' - Distance: {distance2D:F2}m, visible (returning 1)");
                        return new SenseValue(1);
                    }
                }
            }

            cachedValue = 0;
            
            // Debug log every 5 seconds to show sensor value
            if (Time.frameCount % 300 == 0)
            {
                Debug.Log($"[AtStandGuardPointSensor] Sensor value: {cachedValue} (0=not near/visible, 1=near+visible)");
            }
            
            return new SenseValue(0);
        }
    }
}

