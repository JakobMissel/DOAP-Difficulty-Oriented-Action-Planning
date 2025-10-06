using Assets.Scripts.GOAP.Goals;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

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
        
        private void Awake()
        {
            this.goap = FindAnyObjectByType<GoapBehaviour>();
            this.agent = this.GetComponent<AgentBehaviour>();
            this.provider = this.GetComponent<GoapActionProvider>();
        }
        private void Start()
        {
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
        
        // Method called when a noise is heard (called from NoiseArea or thrown object)
        public void OnNoiseHeard(Vector3 noisePosition)
        {
            float distance = Vector3.Distance(transform.position, noisePosition);
            
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