using UnityEngine;

public class PlayerFootstep : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] AudioClip normalClip;
    [SerializeField] AudioClip squeekClip;
    [Header("Values")]
    [SerializeField] float sneakVolume = 0.3f;
    [SerializeField] [Range(0f,1f)] float squeekChance = 0.1f;
    [SerializeField] [Range(0f,0.5f)] float pitchVariation = 0.05f;

    bool isSneaking = false;
    void OnEnable()
    {
        PlayerActions.sneakStatus += OnSneakStatus;
    }

    void OnDisable()
    {
        PlayerActions.sneakStatus -= OnSneakStatus;
    }

    void OnSneakStatus(bool status)
    {
        isSneaking = status;
    }

    void Footstep()
    {
        var clipToPlay = Random.value < squeekChance ? squeekClip : normalClip;
        float volume = 1f;
        float pitch = 1 + Random.Range(-pitchVariation, pitchVariation);
        if (isSneaking)
        {
            volume = sneakVolume;
        }
        PlayerAudio.Instance.PlayAudio(clipToPlay, volume, pitch);
    }

    void OnTriggerEnter(Collider other)
    {
        Footstep();
    }
}
