using UnityEngine;

public class TutorialPole : MonoBehaviour
{
    Animator animator;
    AudioSource audioSource;
    [SerializeField] AudioClip[] audioClips;

    void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
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

    void BuckleSound()
    {
        if (audioClips.Length == 0) return;
        audioSource.PlayOneShot(audioClips[0]);
    }

    void RibbonSound()
    {
        if (audioClips.Length == 0) return;
        audioSource.PlayOneShot(audioClips[1]);
    }

    public void DestroyPole()
    {
        Destroy(gameObject);
    }
}
