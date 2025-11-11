using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;
    
    Rigidbody playerRb;
    CinemachineOrbitalFollow freeLookCamera;

    List<GameObject> thrownObjects = new();
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
    }

    void Start()
    {
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
        if (Input.GetKeyDown(KeyCode.K))
        {
            OnLoadCheckpoint();
        }
    }

    void SaveCheckpoint()
    {
        playerCheckpointPosition = playerRb.position;
        playerCheckpointRotation = playerRb.rotation;

        cameraCheckpointHorizontal = freeLookCamera.HorizontalAxis.Value;
        cameraCheckpointVertical = freeLookCamera.VerticalAxis.Value;
        
        thrownObjects = playerRb.GetComponent<PlayerThrow>().throwablePrefabsList;
        ammoCount = playerRb.GetComponent<PlayerThrow>().ammoCount;
    }

    public void LoadCheckpoint()
    {
        playerRb.position = playerCheckpointPosition;
        playerRb.rotation = playerCheckpointRotation;

        freeLookCamera.HorizontalAxis.Value = cameraCheckpointHorizontal;
        freeLookCamera.VerticalAxis.Value = cameraCheckpointVertical;
        
        playerRb.GetComponent<PlayerThrow>().throwablePrefabsList = thrownObjects;
        playerRb.GetComponent<PlayerThrow>().ammoCount = ammoCount;
    }
}
