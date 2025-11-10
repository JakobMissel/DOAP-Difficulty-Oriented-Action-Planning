using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;
    
    GameObject player;
    CinemachineOrbitalFollow freeLookCamera;

    List<GameObject> thrownObjects = new();
    int ammoCount;

    Vector3 playerCheckpointPosition;
    Vector3 playerCheckpointRotation;

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
        player = GameObject.FindGameObjectWithTag("Player");
        playerCheckpointPosition = player.transform.position;
        playerCheckpointRotation = player.transform.eulerAngles;

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
        playerCheckpointPosition = player.transform.position;
        playerCheckpointRotation = player.transform.eulerAngles;

        cameraCheckpointHorizontal = freeLookCamera.HorizontalAxis.Value;
        cameraCheckpointVertical = freeLookCamera.VerticalAxis.Value;
        
        thrownObjects = player.GetComponent<PlayerThrow>().throwablePrefabsList;
        ammoCount = player.GetComponent<PlayerThrow>().ammoCount;
    }

    public void LoadCheckpoint()
    {
        player.transform.position = playerCheckpointPosition;
        player.transform.eulerAngles = playerCheckpointRotation;

        freeLookCamera.HorizontalAxis.Value = cameraCheckpointHorizontal;
        freeLookCamera.VerticalAxis.Value = cameraCheckpointVertical;
        
        player.GetComponent<PlayerThrow>().throwablePrefabsList = thrownObjects;
        player.GetComponent<PlayerThrow>().ammoCount = ammoCount;
    }
}
