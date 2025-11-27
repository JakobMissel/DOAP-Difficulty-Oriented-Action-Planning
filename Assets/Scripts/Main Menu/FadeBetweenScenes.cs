using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FadeBetweenScenes : MonoBehaviour
{
    [SerializeField] Image fadeScreen;
    [SerializeField] float fadeDuration = 1f;

    bool isFading = false;
    float time = 0f;

    void Awake()
    {
        if(fadeScreen == null)
        {
            fadeScreen = GetComponent<Image>();
        }
        time = 0f;
    }

    void OnEnable()
    {
        MainMenu.fadeToGameplayScene += BeginFadeOut;
        PlayerActions.playerEscaped += BeginFadeOut;
        SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => 
        {
            // Start fade in when a new scene is loaded
            StartCoroutine(FadeIn());
        };
    }

    void OnDisable()
    {
        MainMenu.fadeToGameplayScene -= BeginFadeOut;
        PlayerActions.playerEscaped -= BeginFadeOut;
        SceneManager.sceneLoaded -= (Scene scene, LoadSceneMode mode) => 
        {
            // Start fade in when a new scene is loaded
            StartCoroutine(FadeIn());
        };
    }

    /// <summary>
    /// Begins the fade out process to black screen transition to the outro scene or to game.
    /// </summary>
    public void BeginFadeOut()
    {
        // If a loading sequence is already running, do not start another
        if (isFading) return;
        if (fadeScreen == null)
        {
            Debug.LogWarning("Checkpoint loading screen not found, attempting find again.");
            fadeScreen = GameObject.Find("FadeScreen").GetComponent<Image>();
            return;
        }
        
        if(PlayerActions.Instance != null && PlayerActions.Instance.canEscape)
        {
            print("Beginning fade out to outro.");
            StartCoroutine(FadeOut(PlayerActions.Instance.canEscape));
        }
        else
        {
            print("Beginning fade out to game.");
            StartCoroutine(FadeOut());
        }
    }

    IEnumerator FadeOut(bool outro = false)
    {
        isFading = true;
        float timeScale = Time.timeScale;

        // Ensure time scale is normal for fade out
        Time.timeScale = 1f;

        // Fade to black
        while (time < fadeDuration)
        {
            print($"Fading out... {time/fadeDuration:P0}");
            time += Time.deltaTime;
            fadeScreen.color = new Color(0, 0, 0, time / fadeDuration);
            yield return null;
        }
        isFading = false;
        time = 0f;

        // Restore time scale
        Time.timeScale = timeScale;
        
        // Load outro if player can escape.
        if (outro)
        {
            SceneManager.LoadScene(2);
            yield break;
        }
        else
        {
            MainMenu.Instance.StartGameplayScene();
        }
    }

    IEnumerator FadeIn()
    {
        isFading = true;
        float timeScale = Time.timeScale;
        // Ensure time scale is normal for fade in
        Time.timeScale = 1f;
        // Fade from black
        while (time < fadeDuration)
        {
            print($"Fading in... {time/fadeDuration:P0}");
            time += Time.deltaTime;
            fadeScreen.color = new Color(0, 0, 0, 1 - (time / fadeDuration));
            yield return null;
        }
        isFading = false;
        time = 0f;
        // Restore time scale
        Time.timeScale = timeScale;
    }
}
