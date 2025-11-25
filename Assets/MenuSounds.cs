using UnityEngine;

public class MenuSounds : MonoBehaviour
{
    [SerializeField] AudioClip audioClip;
    [SerializeField] AudioSource audioSource;
    float randomPitch;

    void Awake()
    {
        audioSource.GetComponent<AudioSource>();
    }
    public void PlaySound()
    {
        randomPitch = 1 + Random.Range(-0.1f, 0.1f);
        audioSource.pitch = randomPitch;
        audioSource.PlayOneShot(audioClip);
    }
}
