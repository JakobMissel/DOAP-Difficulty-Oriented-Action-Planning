using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    public static PlayerAudio Instance;
    AudioSource audioSource;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayAudio(AudioClip clip, float volume = 1, float pitch = 1)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
            return;
        }
        Debug.LogWarning($"PlayerAudio ({name}) has no AudioSource component to play audio.");
    }
}
