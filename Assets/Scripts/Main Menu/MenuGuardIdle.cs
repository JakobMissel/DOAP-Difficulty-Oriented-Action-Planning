using UnityEngine;

namespace Assets.Scripts.MainMenu
{
    /// <summary>
    /// Simple component for decorative guards in the main menu.
    /// Ensures they stay in idle animation without GOAP system.
    /// </summary>
    public class MenuGuardIdle : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Name of the idle animation state in the Animator")]
        [SerializeField] private string idleStateName = "Idle";

        private void Start()
        {
            // Try to use GuardAnimation if available (your existing animation system)
            var guardAnimation = GetComponent<GuardAnimation>();
            if (guardAnimation != null)
            {
                guardAnimation.Idle();
                Debug.Log($"[MenuGuardIdle] {gameObject.name} set to idle using GuardAnimation");
                return;
            }

            // Fallback to direct Animator control
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play(idleStateName);
                Debug.Log($"[MenuGuardIdle] {gameObject.name} set to idle using Animator state '{idleStateName}'");
            }
            else
            {
                Debug.LogWarning($"[MenuGuardIdle] {gameObject.name} has no Animator or GuardAnimation component!");
            }
        }
    }
}
