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
        }

        public Transform GetCurrent()
        {
            if (waypoints == null || waypoints.Length == 0)
                return null;

            return waypoints[currentIndex];
        }

        public void Advance()
        {
            if (waypoints == null || waypoints.Length == 0)
                return;

            currentIndex = (currentIndex + 1) % waypoints.Length;
        }
    }
}