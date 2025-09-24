using UnityEngine;

namespace Assets.Scripts.GOAP.Behaviours
{
    public class PatrolRouteBehaviour : MonoBehaviour
    {
        private Transform[] waypoints;
        private int currentIndex = 0;

        private void Awake()
        {
            // Collect all waypoints in the scene
            var objs = GameObject.FindGameObjectsWithTag("Waypoint");
            waypoints = new Transform[objs.Length];
            for (int i = 0; i < objs.Length; i++)
                waypoints[i] = objs[i].transform;
            
            Debug.Log($"[PatrolRouteBehaviour] Found {waypoints.Length} waypoints for {gameObject.name}");
            for (int i = 0; i < waypoints.Length; i++)
            {
                Debug.Log($"[PatrolRouteBehaviour] Waypoint {i}: {waypoints[i].name}");
            }
        }

        public Transform GetCurrent()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                Debug.LogWarning($"[PatrolRouteBehaviour] No waypoints available for {gameObject.name}");
                return null;
            }

            Debug.Log($"[PatrolRouteBehaviour] {gameObject.name} targeting waypoint index {currentIndex}: {waypoints[currentIndex].name}");
            return waypoints[currentIndex];
        }

        public void Advance()
        {
            if (waypoints == null || waypoints.Length == 0)
                return;

            currentIndex = (currentIndex + 1) % waypoints.Length;
            Debug.Log($"[PatrolRouteBehaviour] {gameObject.name} advanced to waypoint index {currentIndex}: {waypoints[currentIndex].name}");
        }

    }
}