using UnityEngine;

namespace Assets.Scripts.GOAP.Behaviours
{
    /// <summary>
    /// Centralises all guard-related SFX triggers so that GOAP actions can stay lean.
    /// Attach this to the guard prefab (and optionally laser interactables) and wire the clips in the inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class ActionAudioBehaviour : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private AudioSource loopSource;

        [Header("Clips")]
        [SerializeField] private AudioClip capturedClip;
        [SerializeField] private AudioClip guardChargingClip;
        [SerializeField] private AudioClip guardHuhClip;
        [SerializeField] private AudioClip laserActivateClip;
        [SerializeField] private AudioClip laserDeactivateClip;
        [SerializeField] private AudioClip walkGuardClip;

        private AudioClip currentLoopClip;

        public void PlayCaptured(float volume = 1f) => PlayOneShot(capturedClip, volume);
        public void PlayGuardHuh(float volume = 1f) => PlayOneShot(guardHuhClip, volume);
        public void PlayLaserActivate(float volume = 1f) => PlayOneShot(laserActivateClip, volume);
        public void PlayLaserDeactivate(float volume = 1f) => PlayOneShot(laserDeactivateClip, volume);

        public void PlayRechargeLoop() => PlayLoop(guardChargingClip);
        public void StopRechargeLoop() => StopLoop(guardChargingClip);

        public void PlayWalkLoop() => PlayLoop(walkGuardClip);
        public void StopWalkLoop() => StopLoop(walkGuardClip);

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null)
                return;

            var source = EnsureOneShotSource();
            if (source == null)
                return;

            source.pitch = 1f;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void PlayLoop(AudioClip clip)
        {
            //if (clip == null)
            //    return;

            //var source = EnsureLoopSource();
            //if (source == null)
            //    return;

            //if (currentLoopClip == clip && source.isPlaying)
            //    return;

            //currentLoopClip = clip;
            //source.clip = clip;
            //source.loop = true;
            //source.Play();
        }

        private void StopLoop(AudioClip clip)
        {
            //if (loopSource == null)
            //    return;

            //if (clip != null && currentLoopClip != clip)
            //    return;

            //loopSource.Stop();
            //loopSource.clip = null;
            //currentLoopClip = null;
        }

        private AudioSource EnsureOneShotSource()
        {
            if (oneShotSource == null)
            {
                oneShotSource = GetComponent<AudioSource>();
                if (oneShotSource == null)
                {
                    oneShotSource = gameObject.AddComponent<AudioSource>();
                    oneShotSource.playOnAwake = false;
                    oneShotSource.loop = false;
                    oneShotSource.spatialBlend = 1f;
                }
            }

            oneShotSource.playOnAwake = false;
            oneShotSource.loop = false;
            return oneShotSource;
        }

        private AudioSource EnsureLoopSource()
        {
            if (loopSource == null)
            {
                loopSource = gameObject.AddComponent<AudioSource>();
                loopSource.playOnAwake = false;
                loopSource.loop = true;
                loopSource.spatialBlend = 1f;
            }

            loopSource.playOnAwake = false;
            loopSource.loop = true;
            return loopSource;
        }

        public void StopAllLooping()
        {
            if (loopSource == null)
                return;

            loopSource.Stop();
            loopSource.clip = null;
            currentLoopClip = null;
        }

        private void OnDisable()
        {
            StopAllLooping();
        }
    }
}

