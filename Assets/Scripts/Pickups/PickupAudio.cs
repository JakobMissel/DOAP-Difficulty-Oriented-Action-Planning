using UnityEngine;

public class PickupAudio : MonoBehaviour
{
    AudioSource audioSource;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        Invoke(nameof(DestroySelf), audioSource.clip.length);
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}
