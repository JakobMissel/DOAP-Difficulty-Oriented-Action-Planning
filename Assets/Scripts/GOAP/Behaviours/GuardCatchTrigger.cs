using UnityEngine;

namespace Assets.Scripts.GOAP.Behaviours
{
    /// <summary>
    /// Detects physical contact between guard and player using Unity's collision/trigger system.
    /// When the guard touches the player, immediately sets PlayerCaught flag.
    /// Attach this to the guard GameObject and ensure it has a trigger collider.
    /// </summary>
    [RequireComponent(typeof(BrainBehaviour))]
    public class GuardCatchTrigger : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool useTrigger = true;
        [SerializeField] private bool debugMode = true;
        [SerializeField] private string playerTag = "Player";
        
        private BrainBehaviour brain;
        private bool hasAlreadyCaught = false;

        private void Awake()
        {
            brain = GetComponent<BrainBehaviour>();
            
            if (brain == null)
            {
                Debug.LogError($"[GuardCatchTrigger] No BrainBehaviour found on {name}! This component requires BrainBehaviour.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!useTrigger)
                return;

            HandleContact(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (useTrigger)
                return;

            HandleContact(collision.gameObject);
        }

        private void HandleContact(GameObject contactObject)
        {
            // Avoid catching the player multiple times
            if (hasAlreadyCaught)
                return;

            // Check if we touched the player
            if (contactObject.CompareTag(playerTag))
            {
                hasAlreadyCaught = true;

                if (debugMode)
                {
                    Debug.Log($"[GuardCatchTrigger] {name} made physical contact with player! Setting PlayerCaught.");
                }

                // Set the caught flag
                if (brain != null)
                {
                    brain.SetPlayerCaught(true);
                }
                else
                {
                    Debug.LogError($"[GuardCatchTrigger] Brain is null on {name}!");
                }
            }
        }

        /// <summary>
        /// Reset the catch state (useful for testing or respawning)
        /// </summary>
        public void ResetCatch()
        {
            hasAlreadyCaught = false;
            
            if (debugMode)
            {
                Debug.Log($"[GuardCatchTrigger] {name} catch state reset.");
            }
        }
    }
}

