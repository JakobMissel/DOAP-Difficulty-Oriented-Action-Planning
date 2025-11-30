using UnityEngine;

namespace Assets.Scripts.GOAP
{
    /// <summary>
    /// Ensures the GOAP system persists across scene loads and isn't destroyed.
    /// Attach this to the GOAPConfig GameObject.
    /// Uses singleton pattern to prevent duplicates.
    /// </summary>
    [DefaultExecutionOrder(-200)] // Run very early, before other GOAP components
    public class GoapPersistence : MonoBehaviour
    {
        private static GoapPersistence instance;
        private static bool applicationQuitting = false;
        private static bool forceDestroyInstance = false;

        public static void ForceDestroyInstance()
        {
            if (instance == null)
                return;

            forceDestroyInstance = true;
            var go = instance.gameObject;
            instance = null;
            if (Application.isPlaying)
            {
                Object.Destroy(go);
            }
            else
            {
                Object.DestroyImmediate(go);
            }
        }

        private void Awake()
        {
            // Singleton pattern - only allow one instance
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"[GoapPersistence] Duplicate GOAPConfig detected! Destroying {gameObject.name}, keeping existing instance.");
                Destroy(gameObject);
                return;
            }

            instance = this;

            // Make this GameObject persistent across scene loads
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[GoapPersistence] Made {gameObject.name} persistent with DontDestroyOnLoad. Scene: {gameObject.scene.name}");
        }

        private void Start()
        {
            Debug.Log($"[GoapPersistence] Start called - GameObject still alive. Scene: {gameObject.scene.name}");
        }

        private void OnApplicationQuit()
        {
            applicationQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                if (!applicationQuitting && !forceDestroyInstance)
                {
                    Debug.LogError($"[GoapPersistence] Singleton instance is being destroyed during gameplay!");
                    Debug.LogError($"[GoapPersistence] Current scene: {gameObject.scene.name}, Time: {Time.time}, Frame: {Time.frameCount}");
                    for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    {
                        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                        Debug.LogError($"[GoapPersistence] Loaded scene {i}: {scene.name}, isLoaded: {scene.isLoaded}");
                    }
                }

                instance = null;
                forceDestroyInstance = false;
                return;
            }

            if (applicationQuitting)
            {
                Debug.Log("[GoapPersistence] Destroyed during application quit - this is normal.");
            }
        }
    }
}
