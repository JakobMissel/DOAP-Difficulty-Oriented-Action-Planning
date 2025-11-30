using UnityEngine;

namespace Assets.Scripts.GOAP.Behaviours
{
    /// <summary>
    /// Links a StandGuardPoint to its corresponding painting.
    /// Automatically disables the guard point when the painting is stolen (inactive).
    /// Attach this to GameObjects tagged with "StandGuardPoint".
    /// </summary>
    public class StandGuardPointPaintingLink : MonoBehaviour
    {
        [Header("Painting Reference")]
        [Tooltip("The painting GameObject that this guard point protects. When the painting becomes inactive (stolen), this guard point will be disabled.")]
        [SerializeField] private GameObject painting;

        [Header("Auto-Find Settings")]
        [Tooltip("If true, will automatically find the nearest painting with StealablePickup component on Start")]
        [SerializeField] private bool autoFindNearestPainting = false;

        [Tooltip("Maximum distance to search for paintings when auto-finding (meters)")]
        [SerializeField] private float searchRadius = 10f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        private bool wasActiveLastFrame = true;

        private void Start()
        {
            // Auto-find painting if enabled and no painting is assigned
            if (autoFindNearestPainting && painting == null)
            {
                FindNearestPainting();
            }

            // Validate painting reference
            if (painting == null)
            {
                Debug.LogWarning($"[StandGuardPointPaintingLink] {name} has no painting assigned! This guard point will always be active. Assign a painting in the inspector or enable 'Auto Find Nearest Painting'.");
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[StandGuardPointPaintingLink] {name} is linked to painting: {painting.name}");
                }
            }
        }

        private void Update()
        {
            // If no painting is assigned, always stay active
            if (painting == null)
                return;

            // Check if painting is active in hierarchy
            bool isPaintingActive = painting.activeInHierarchy;

            // If painting state changed from active to inactive, disable this guard point
            if (wasActiveLastFrame && !isPaintingActive)
            {
                DisableGuardPoint();
            }
            // If painting became active again (e.g., checkpoint reload), re-enable this guard point
            else if (!wasActiveLastFrame && isPaintingActive)
            {
                EnableGuardPoint();
            }

            wasActiveLastFrame = isPaintingActive;
        }

        /// <summary>
        /// Disables this StandGuardPoint (guards will no longer select it as a target)
        /// </summary>
        private void DisableGuardPoint()
        {
            if (showDebugLogs)
            {
                Debug.Log($"[StandGuardPointPaintingLink] {name} disabled - painting '{painting.name}' has been stolen!");
            }

            // Disable this GameObject so it won't be found by FindGameObjectsWithTag
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Re-enables this StandGuardPoint (e.g., after checkpoint reload)
        /// </summary>
        private void EnableGuardPoint()
        {
            if (showDebugLogs)
            {
                Debug.Log($"[StandGuardPointPaintingLink] {name} re-enabled - painting '{painting.name}' is active again!");
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Finds the nearest painting with StealablePickup component within searchRadius
        /// </summary>
        private void FindNearestPainting()
        {
            StealablePickup[] allPaintings = FindObjectsOfType<StealablePickup>();

            if (allPaintings == null || allPaintings.Length == 0)
            {
                Debug.LogWarning($"[StandGuardPointPaintingLink] {name} could not auto-find painting - no StealablePickup components found in scene!");
                return;
            }

            StealablePickup nearestPainting = null;
            float nearestDistance = float.MaxValue;

            foreach (StealablePickup paintingPickup in allPaintings)
            {
                if (paintingPickup == null)
                    continue;

                float distance = Vector3.Distance(transform.position, paintingPickup.transform.position);

                if (distance <= searchRadius && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPainting = paintingPickup;
                }
            }

            if (nearestPainting != null)
            {
                painting = nearestPainting.gameObject;
                Debug.Log($"[StandGuardPointPaintingLink] {name} auto-found nearest painting: {painting.name} at distance {nearestDistance:F2}m");
            }
            else
            {
                Debug.LogWarning($"[StandGuardPointPaintingLink] {name} could not find any paintings within {searchRadius}m radius!");
            }
        }

        /// <summary>
        /// Returns true if the linked painting is still active (not stolen)
        /// </summary>
        public bool IsPaintingActive()
        {
            if (painting == null)
                return true; // If no painting is linked, consider it always active

            return painting.activeInHierarchy;
        }

        /// <summary>
        /// Static method to refresh all StandGuardPoints based on their painting status.
        /// Call this when paintings might have been reactivated (e.g., checkpoint reload, retry).
        /// This is necessary because disabled GameObjects don't run Update().
        /// </summary>
        public static void RefreshAllGuardPoints()
        {
            // Find all StandGuardPointPaintingLink components, including on inactive objects
            StandGuardPointPaintingLink[] allLinks = Resources.FindObjectsOfTypeAll<StandGuardPointPaintingLink>();

            int reenabledCount = 0;
            int disabledCount = 0;

            foreach (StandGuardPointPaintingLink link in allLinks)
            {
                if (link == null || link.gameObject == null)
                    continue;

                // Skip prefabs and assets (only process scene objects)
                if (link.gameObject.scene.name == null || link.gameObject.scene.name == "")
                    continue;

                bool isPaintingActive = link.painting != null && link.painting.activeInHierarchy;
                bool isGuardPointActive = link.gameObject.activeInHierarchy;

                // If painting is active but guard point is disabled, re-enable it
                if (isPaintingActive && !isGuardPointActive)
                {
                    link.gameObject.SetActive(true);
                    link.wasActiveLastFrame = true; // Reset the flag
                    reenabledCount++;
                    Debug.Log($"[StandGuardPointPaintingLink] Re-enabled guard point '{link.name}' - painting '{link.painting.name}' is active");
                }
                // If painting is inactive but guard point is enabled, disable it
                else if (!isPaintingActive && isGuardPointActive && link.painting != null)
                {
                    link.gameObject.SetActive(false);
                    disabledCount++;
                    Debug.Log($"[StandGuardPointPaintingLink] Disabled guard point '{link.name}' - painting '{link.painting.name}' is inactive");
                }
            }

            if (reenabledCount > 0 || disabledCount > 0)
            {
                Debug.Log($"[StandGuardPointPaintingLink] Refreshed guard points: {reenabledCount} re-enabled, {disabledCount} disabled");
            }
        }

        /// <summary>
        /// Editor helper - draw a line to the linked painting
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (painting != null)
            {
                // Draw a yellow line to the linked painting
                Gizmos.color = painting.activeInHierarchy ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, painting.transform.position);

                // Draw sphere at painting location
                Gizmos.DrawWireSphere(painting.transform.position, 0.5f);
            }

            // Draw search radius if auto-find is enabled
            if (autoFindNearestPainting)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, searchRadius);
            }
        }
    }
}
