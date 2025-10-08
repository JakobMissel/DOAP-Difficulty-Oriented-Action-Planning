using Assets.Scripts.GOAP.Goals;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts.GOAP.Behaviours
{
    public class BrainBehaviour : MonoBehaviour
    {
        private AgentBehaviour agent;
        private GoapActionProvider provider;
        private GoapBehaviour goap;
        
        // Track if player has been caught
        public bool IsPlayerCaught { get; private set; } = false;

        // Track noise/distraction
        public bool HasHeardNoise { get; private set; } = false;
        public Vector3 LastNoisePosition { get; private set; }
        public float NoiseHearingRadius = 20f; // How far the agent can hear noises
        
        // Track last known player position
        public bool HasLastKnownPosition { get; private set; } = false;
        public Vector3 LastKnownPlayerPosition { get; private set; }
        
        [Header("NavMesh Settings")]
        [SerializeField] private float angularSpeed = 360f; // Default is 120, increase for faster turning
        [SerializeField] private float acceleration = 12f; // Default is 8, increase for faster acceleration
        
        private void Awake()
        {
            this.goap = FindAnyObjectByType<GoapBehaviour>();
            this.agent = this.GetComponent<AgentBehaviour>();
            this.provider = this.GetComponent<GoapActionProvider>();
            
            // Configure NavMeshAgent turning speed
            var navAgent = GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.angularSpeed = angularSpeed;
                navAgent.acceleration = acceleration;
                Debug.Log($"[BrainBehaviour] Set angular speed to {angularSpeed} degrees/sec");
            }
        }
        private void Start()
        {
            // Add all goals to the provider
            this.provider.RequestGoal<PatrolGoal, PursuitGoal, RechargeGoal, CatchGoal, InvestigateNoiseGoal>();
        }
        
        // Method to set player caught state (called from PursuitAction or other sources)
        public void SetPlayerCaught(bool caught)
        {
            IsPlayerCaught = caught;
            
            if (caught)
            {
                Debug.Log("[BrainBehaviour] Player has been caught!");
            }
        }
        
        // Method to update last known player position
        public void UpdateLastKnownPlayerPosition(Vector3 position)
        {
            HasLastKnownPosition = true;
            LastKnownPlayerPosition = position;
            Debug.Log($"[BrainBehaviour] Updated last known player position: {position}");
        }
        
        // Method to clear last known position
        public void ClearLastKnownPlayerPosition()
        {
            HasLastKnownPosition = false;
            Debug.Log("[BrainBehaviour] Cleared last known player position");
        }
        
        // Method called when a noise is heard (called from NoiseArea or thrown object)
        public void OnNoiseHeard(Vector3 noisePosition)
        {
            float distance = Vector3.Distance(transform.position, noisePosition);
            Debug.Log($"[BrainBehaviour] Noise made at {noisePosition}, distance: {distance:F1}m");
            if (distance <= NoiseHearingRadius)
            {
                HasHeardNoise = true;
                LastNoisePosition = noisePosition;
                Debug.Log($"[BrainBehaviour] Heard noise at {noisePosition}, distance: {distance:F1}m");
            }
        }

        // Method to clear noise after investigation
        public void ClearNoise()
        {
            HasHeardNoise = false;
            Debug.Log("[BrainBehaviour] Noise cleared");
        }
    }
}