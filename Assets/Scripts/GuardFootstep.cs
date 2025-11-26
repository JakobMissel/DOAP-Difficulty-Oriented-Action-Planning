using UnityEngine;

public class GuardFootstep : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] float pitchVariance = 0.1f;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayFootstepSound()
    {
        if (audioSource != null)
        {
            audioSource.pitch = 1 + Random.Range(-pitchVariance, pitchVariance);
            audioSource.PlayOneShot(audioSource.clip);
            return;
        }
        Debug.LogWarning($"GuardFootstep ({name}) has no AudioSource component to play audio.");
    }
}
