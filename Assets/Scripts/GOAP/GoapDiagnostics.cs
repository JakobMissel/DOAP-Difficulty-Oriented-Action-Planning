using UnityEngine;
using CrashKonijn.Goap.Runtime;

namespace Assets.Scripts.GOAP
{
    /// <summary>
    /// Diagnostic script to check GOAP system state.
    /// Attach this to the GOAPConfig GameObject to see if the GOAP runner is working.
    /// </summary>
    public class GoapDiagnostics : MonoBehaviour
    {
        private GoapBehaviour goapBehaviour;
        private ReactiveControllerBehaviour reactiveController;
        private float lastLogTime;
        private float logInterval = 2.0f; // Log every 2 seconds

        private void Awake()
        {
            goapBehaviour = GetComponent<GoapBehaviour>();
            reactiveController = GetComponent<ReactiveControllerBehaviour>();

            Debug.Log($"[GoapDiagnostics] Awake - GameObject: {gameObject.name}, Scene: {gameObject.scene.name}, GoapBehaviour: {(goapBehaviour != null ? "Found" : "NULL")}, ReactiveController: {(reactiveController != null ? "Found" : "NULL")}");
            Debug.Log($"[GoapDiagnostics] Awake - GameObject active: {gameObject.activeSelf}, Component enabled: {enabled}");

            // Check if we're about to be destroyed
            Invoke(nameof(CheckIfAlive), 0.1f);
        }

        private void CheckIfAlive()
        {
            Debug.Log($"[GoapDiagnostics] CheckIfAlive - Still alive 0.1s after Awake!");
        }

        private void OnDestroy()
        {
            Debug.LogError($"[GoapDiagnostics] GameObject {gameObject.name} is being DESTROYED! Stack trace:\n{System.Environment.StackTrace}");
        }

        private void Start()
        {
            Debug.Log($"[GoapDiagnostics] Start - GameObject active: {gameObject.activeSelf}, GoapBehaviour enabled: {goapBehaviour?.enabled}, ReactiveController enabled: {reactiveController?.enabled}");
        }

        private void OnDisable()
        {
            Debug.LogError($"[GoapDiagnostics] GameObject DISABLED! This is the problem - GOAPConfig should never be disabled!");
            Debug.LogError($"[GoapDiagnostics] Stack trace:\n{System.Environment.StackTrace}");
        }

        private void OnEnable()
        {
            Debug.Log($"[GoapDiagnostics] GameObject ENABLED at frame {Time.frameCount}");
        }

        private void Update()
        {
            // Log periodically to confirm Update is running
            if (Time.time - lastLogTime > logInterval)
            {
                lastLogTime = Time.time;

                if (goapBehaviour != null)
                {
                    Debug.Log($"[GoapDiagnostics] GOAP System Update Running - Frame: {Time.frameCount}, GoapBehaviour enabled: {goapBehaviour.enabled}, isActiveAndEnabled: {goapBehaviour.isActiveAndEnabled}");
                }
                else
                {
                    Debug.LogError("[GoapDiagnostics] GoapBehaviour is NULL in Update!");
                }

                if (reactiveController != null)
                {
                    Debug.Log($"[GoapDiagnostics] ReactiveController enabled: {reactiveController.enabled}, isActiveAndEnabled: {reactiveController.isActiveAndEnabled}");
                }
                else
                {
                    Debug.LogError("[GoapDiagnostics] ReactiveControllerBehaviour is NULL in Update!");
                }
            }
        }
    }
}
