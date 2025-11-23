using UnityEngine;

public class GuardAnimation : MonoBehaviour
{
    [SerializeField] Animator animator;
    
    private enum AnimationState
    {
        None,
        Walking,
        Running,
        Idle,
        Searching,
        Recharging
    }
    
    private AnimationState currentState = AnimationState.None;

    public void Walk()
    {
        if (currentState != AnimationState.Walking)
        {
            animator.SetTrigger("Walking");
            currentState = AnimationState.Walking;
            Debug.Log("[GuardAnimation] Walking");
        }
    }

    public void Run()
    {
        if (currentState != AnimationState.Running)
        {
            animator.SetTrigger("Running");
            currentState = AnimationState.Running;
            Debug.Log("[GuardAnimation] Running");
        }
    }

    public void Idle()
    {
        if (currentState != AnimationState.Idle)
        {
            animator.SetTrigger("Idle");
            currentState = AnimationState.Idle;
            Debug.Log("[GuardAnimation] Idle");
        }
    }

    public void Search()
    {
        if (currentState != AnimationState.Searching)
        {
            animator.SetTrigger("Searching");
            currentState = AnimationState.Searching;
            Debug.Log("[GuardAnimation] Searching");
        }
    }
    
    public void Recharge()
    {
        if (currentState != AnimationState.Recharging)
        {
            animator.SetTrigger("Recharging");
            currentState = AnimationState.Recharging;
            Debug.Log("[GuardAnimation] Recharging");
        }
    }
}
