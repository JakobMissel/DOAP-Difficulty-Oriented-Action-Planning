using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.UI;
using System.Collections;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    [SerializeField] float fadeTime = 1f;
    float time;

    Image checkpointLoadingScreen;
    [HideInInspector] public bool isLoading = false;

    Rigidbody playerRb;
    CinemachineOrbitalFollow freeLookCamera;

    List<GameObject> thrownObjects;
    int ammoCount;

    Vector3 playerCheckpointPosition;
    Quaternion playerCheckpointRotation;

    float cameraCheckpointHorizontal;
    float cameraCheckpointVertical;

    public static Action loadCheckpoint;
    public static void OnLoadCheckpoint() => loadCheckpoint?.Invoke();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        checkpointLoadingScreen = GameObject.Find("CheckpointLoadingScreen").GetComponent<Image>();
        checkpointLoadingScreen.color = new Color(0, 0, 0, 0);
        
        playerRb = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody>();
        playerCheckpointPosition = playerRb.position;
        playerCheckpointRotation = playerRb.rotation;

        freeLookCamera = GameObject.Find("Free Look Camera").GetComponent<CinemachineOrbitalFollow>();
        cameraCheckpointHorizontal = freeLookCamera.HorizontalAxis.Value;
        cameraCheckpointVertical = freeLookCamera.VerticalAxis.Value;
    }

    void OnEnable()
    {
        StealPainting.paintingStolen += (int a, float b) => SaveCheckpoint();
        PlayerActions.paintingDelivered += () => SaveCheckpoint();
        loadCheckpoint += LoadCheckpoint;
    }

    void OnDisable()
    {
        StealPainting.paintingStolen -= (int a, float b) => SaveCheckpoint();
        PlayerActions.paintingDelivered -= () => SaveCheckpoint();
        loadCheckpoint -= LoadCheckpoint;
    }

    void Update()
    {
        // Press K to test loading checkpoint
        //if (Input.GetKeyDown(KeyCode.K))
        //{
        //    BeginLoading();
        //}
    }

    /// <summary>
    /// This starts the loading sequence coroutine
    /// </summary>
    public void BeginLoading()
    {
        // If a loading sequence is already running, do not start another
        if(isLoading) return;
        if(checkpointLoadingScreen == null)
        {
            Debug.LogWarning("Checkpoint loading screen not found, attempting find again.");
            checkpointLoadingScreen = GameObject.Find("CheckpointLoadingScreen").GetComponent<Image>();
            return;
        }
        StartCoroutine(LoadingSequence());
    }

    IEnumerator LoadingSequence()
    {
        isLoading = true;
        ChangePlayerMovement();

        // Fade to black
        //while (time < fadeTime)
        //{
        //    time += Time.deltaTime;
        //    // Fade to black
        //    checkpointLoadingScreen.color = new Color(0, 0, 0, time / fadeTime);
        //    yield return null;
        //}
        //time = 0;

        checkpointLoadingScreen.color = new Color(0, 0, 0, 1);
        // While black, load checkpoint
        OnLoadCheckpoint();
        
        // Keep black for a moment
        yield return new WaitForSeconds(fadeTime);
        
        // Fade back from black
        while (time < fadeTime)
        {
            time += Time.deltaTime;
            // Fade to black
            checkpointLoadingScreen.color = new Color(0, 0, 0, 1 - (time / fadeTime));
            yield return null;
        }
        time = 0;
        
        isLoading = false;
        ChangePlayerMovement();
    }

    /// <summary>
    /// Enable/Disable player movement depending on loading state
    /// </summary>
    void ChangePlayerMovement()
    {
        if (isLoading)
        {
            if(playerRb == null)
            {
                Debug.LogWarning("Player Rigidbody not found, cannot disable player movement.");
                return;
            }
            // Disable player movement
            playerRb.GetComponent<PlayerMovement>().canMove = false;
            playerRb.GetComponent<PlayerMovement>().moveInput = Vector2.zero;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.GetComponent<PlayerMovement>().velocity = Vector3.zero;
            playerRb.GetComponent<PlayerMovement>().enabled = false;
            return;
        }
        else
        {
            if (playerRb == null)
            {
                Debug.LogWarning("Player Rigidbody not found, cannot re-enable player movement.");
                return;
            }
            // Re-enable player movement
            playerRb.GetComponent<PlayerMovement>().enabled = true;
            playerRb.GetComponent<PlayerMovement>().canMove = true;
        }
    }

    void SaveCheckpoint()
    {
        if (playerRb == null)
        {
            Debug.LogWarning("Player Rigidbody not found, cannot save checkpoint.");
            return;
        }
        playerCheckpointPosition = playerRb.position;
        playerCheckpointRotation = playerRb.rotation;

        if(freeLookCamera == null)
        {
            Debug.LogWarning("Cinemachine Free Look Camera not found, cannot save camera checkpoint.");
            return;
        }
        cameraCheckpointHorizontal = freeLookCamera.HorizontalAxis.Value;
        cameraCheckpointVertical = freeLookCamera.VerticalAxis.Value;

        // Out commented out for now since throwables do not respawn between checkpoints
        //thrownObjects = playerRb.GetComponent<PlayerThrow>().throwablePrefabsList;
        //ammoCount = playerRb.GetComponent<PlayerThrow>().ammoCount;
    }

    public void LoadCheckpoint()
    {
        if (playerRb == null)
        {
            Debug.LogWarning("Player Rigidbody not found, cannot load checkpoint.");
            return;
        }
        playerRb.position = playerCheckpointPosition;
        playerRb.rotation = playerCheckpointRotation;

        if(freeLookCamera == null)
        {
            Debug.LogWarning("Cinemachine Free Look Camera not found, cannot load camera checkpoint.");
            return;
        }
        freeLookCamera.HorizontalAxis.Value = cameraCheckpointHorizontal;
        freeLookCamera.VerticalAxis.Value = cameraCheckpointVertical;

        // Out commented out for now since throwables do not respawn between checkpoints
        //playerRb.GetComponent<PlayerThrow>().throwablePrefabsList = thrownObjects;
        //playerRb.GetComponent<PlayerThrow>().ammoCount = ammoCount;
    }
}
