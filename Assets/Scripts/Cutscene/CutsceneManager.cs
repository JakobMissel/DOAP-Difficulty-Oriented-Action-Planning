using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace Assets.Scripts.Cutscene
{
    public class CutsceneManager : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private bool skipCutscene = false;
#endif

        [SerializeField] private CutsceneSelect whichCutscene = CutsceneSelect.None;
        [SerializeField] private GameObject[] disableDuringCutscene;
        [SerializeField] private AudioMixer audioMixer;

        private PlayableDirector director;

        private float baseReflectionIntensity = 1f;
        private float baseAmbientIntensity = 1f;
        private Color baseAmbientColor = Color.grey;
        private Color baseShadowColor = Color.blue;

        private float baseGameplayAudioLevel = 1f;

        private void Awake()
        {
            director = GetComponent<PlayableDirector>();

            switch (whichCutscene)
            {
                case CutsceneSelect.Intro:
#if UNITY_EDITOR
                    if (!skipCutscene)
#endif
                    IntroCutsceneSetup();
                    break;
                case CutsceneSelect.Outro:
                    break;
                default:
                    break;
            }

#if UNITY_EDITOR
            // If cutscene should be skipped, just say that the cutscene is done playing
            if (skipCutscene)
            {
                return;
            }
#endif

            // Start playing cutscene
            director.Play();
        }

#if UNITY_EDITOR
        private void Start()
        {
            if (skipCutscene)
                ObjectivesManager.Instance.StartObjective();
        }
#endif

        public void CutsceneDone()
        {
            Debug.Log($"{whichCutscene} Cutscene done");

            switch (whichCutscene)
            {
                case CutsceneSelect.Intro:
                    IntroCutsceneDone();
                    break;
                case CutsceneSelect.Outro:
                    break;
                default:
                    break;
            }

        }

        private void DisableEnvironmentLighting()
        {
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.ambientIntensity = 0f;
            RenderSettings.ambientSkyColor = Color.black;
            RenderSettings.subtractiveShadowColor = Color.black;
        }

        public void EnableEnvironmentLighting()
        {
            RenderSettings.reflectionIntensity = baseReflectionIntensity;
            RenderSettings.ambientIntensity = baseAmbientIntensity;
            RenderSettings.ambientSkyColor = baseAmbientColor;
            RenderSettings.subtractiveShadowColor = baseShadowColor;
        }

        /// <summary>
        /// Disables player movement and sets right lighting environment
        /// </summary>
        private void IntroCutsceneSetup()
        {
            // Get gameplay audio info and disable it
            audioMixer.GetFloat("GameplayVolume", out baseGameplayAudioLevel);
            Debug.Log($"Setting gameplay volume to -80f, also got base volume level of {baseGameplayAudioLevel}");
            audioMixer.SetFloat("GameplayVolume", -80f);

            // Get lighting info and disable it
            baseReflectionIntensity = RenderSettings.reflectionIntensity;
            baseAmbientIntensity = RenderSettings.ambientIntensity;
            baseAmbientColor = RenderSettings.ambientSkyColor;
            baseShadowColor = RenderSettings.subtractiveShadowColor;
            DisableEnvironmentLighting();

            for (int i = 0; i < disableDuringCutscene.Length; i++)
            {
                disableDuringCutscene[i].SetActive(false);
            }
        }

        /// <summary>
        /// Enables player movement, resets lighting environment, and starts tutorial
        /// </summary>
        private void IntroCutsceneDone()
        {
            Debug.Log("Starting first objective as intro is done");
            for (int i = 0; i < disableDuringCutscene.Length; i++)
            {
                disableDuringCutscene[i].SetActive(true);
            }

            // Re-enable gameplay audio
            audioMixer.SetFloat("GameplayVolume", baseGameplayAudioLevel);

            // Start tutorial
            ObjectivesManager.Instance.StartObjective();
        }
    }

    public enum CutsceneSelect
    {
        None,
        Intro,
        Outro,
    }
}
