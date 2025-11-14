using UnityEngine;

public class GuardAnimation : MonoBehaviour
{
    [SerializeField] Animator animator;

    public void Walk()
    {
        animator.SetTrigger("Walking");
    }

    public void Run()
    {
        animator.SetTrigger("Running");
    }

    public void Idle()
    {
        animator.SetTrigger("Idle");
    }

    public void Search()
    {
        animator.SetTrigger("Searching");
    }
}
