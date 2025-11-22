using UnityEngine;

public class TutorialPole : MonoBehaviour
{
    Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        PlayerActions.tutorialCompletion += TutorialCompleded;
    }

    void OnDisable()
    {
        PlayerActions.tutorialCompletion -= TutorialCompleded;
    }

    void TutorialCompleded()
    {
        RemovePole();
    }

    void RemovePole()
    {
        animator.Play("RemovePole");
    }

    public void DestroyPole()
    {
        Destroy(gameObject);
    }
}
