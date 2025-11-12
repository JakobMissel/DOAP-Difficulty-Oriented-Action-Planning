using UnityEngine;

public class GuardAnimation : MonoBehaviour
{
    Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

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
