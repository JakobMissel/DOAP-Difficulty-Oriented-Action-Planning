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
        
        // Track if we were seeing the player in the previous frame
        private bool wasSeenLastFrame = false;
        
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
        
        // Method to update player visibility state - called every frame from sensor
        public void UpdatePlayerVisibility(bool canSee, Vector3 currentPlayerPosition)
        {
            if (canSee)
            {
                // Keep updating while visible so we have the freshest position
                LastKnownPlayerPosition = currentPlayerPosition;
                wasSeenLastFrame = true;
        
                // Don't clear HasLastKnownPosition - that gets cleared after investigation
            }
            else
            {
                // If we JUST lost sight this frame, mark that we have a last known position
                if (wasSeenLastFrame && !HasLastKnownPosition)
                {
                    HasLastKnownPosition = true;
                    Debug.Log($"[BrainBehaviour] Player just left sight! Last known position frozen at: {LastKnownPlayerPosition}");
                }
        
                wasSeenLastFrame = false;
            }
        }
        
        // Method to clear last known position (called after investigation is complete)
        public void ClearLastKnownPlayerPosition()
        {
            HasLastKnownPosition = false;
            wasSeenLastFrame = false;
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