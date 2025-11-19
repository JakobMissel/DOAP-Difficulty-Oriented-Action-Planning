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
        Searching
    }
    
    private AnimationState currentState = AnimationState.None;

    public void Walk()
    {
        if (currentState != AnimationState.Walking)
        {
            animator.SetTrigger("Walking");
            currentState = AnimationState.Walking;
        }
    }

    public void Run()
    {
        if (currentState != AnimationState.Running)
        {
            animator.SetTrigger("Running");
            currentState = AnimationState.Running;
        }
    }

    public void Idle()
    {
        if (currentState != AnimationState.Idle)
        {
            animator.SetTrigger("Idle");
            currentState = AnimationState.Idle;
        }
    }

    public void Search()
    {
        if (currentState != AnimationState.Searching)
        {
            animator.SetTrigger("Searching");
            currentState = AnimationState.Searching;
        }
    }
}
