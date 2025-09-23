using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;


public class PlayerThrow : MonoBehaviour
{
    PlayerInput playerInput;
    [Header("Aiming")]
    [SerializeField] FollowTransform orientation;
    [SerializeField] CinemachineCamera freeLookCamera;
    [SerializeField] CinemachineCamera aimCamera;
    [SerializeField] Vector3 aimOffset = new(0,1.5f,0);
    [SerializeField] LayerMask throwableLayer;
    [Header("Thrown Object")]
    [SerializeField] Transform throwStartPoint;
    [SerializeField] public List<GameObject> throwablePrefabsList = new();
    [SerializeField] public int ammoCount;
    [Header("Throw Visualization")]
    [SerializeField] LineRenderer throwLineRenderer;
    [SerializeField] int segmentCount = 5000;
    [SerializeField] float segmentLength = 0.001f;
    [SerializeField] GameObject hitAreaPrefab;
    GameObject hitArea;
    [SerializeField] float hitRadius = 0.1f;
    [SerializeField] float hitDistance = 0.2f;
    [Header("Throw Settings")]
    [SerializeField] float throwForce = 7f;
    [SerializeField] float throwGravity = -9.81f;
    [SerializeField] float throwCooldown = 1f;
    float cooldownTimer = 0f;
    bool canThrow = true;
    bool resetCamera;

    Vector3 throwDirection;
    RaycastHit currentHit;

    [HideInInspector] public bool isAiming = false;

    // Player throw Actions
    public static Action<bool> aimStatus;
    public static void OnAimStatus(bool isAiming) => aimStatus?.Invoke(isAiming);

    public static Action<int> ammoUpdate;
    public static void OnAmmoUpdate(int ammo) => ammoUpdate?.Invoke(ammo);

    public static Action<Sprite> spriteUpdate;
    public static void OnSpriteUpdate(Sprite sprite) => spriteUpdate?.Invoke(sprite);



    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        hitArea = Instantiate(hitAreaPrefab);
        hitArea.SetActive(false);
        throwGravity = Physics.gravity.y;
    }
    void OnEnable()
    {
        playerInput.actions["Throw"].performed += OnThrow;
        playerInput.actions["Aim"].performed += OnAim;
        playerInput.actions["Aim"].canceled += OnAim;
    }

    void OnDisable()
    {
        playerInput.actions["Throw"].performed -= OnThrow;
        playerInput.actions["Aim"].performed -= OnAim;
        playerInput.actions["Aim"].canceled -= OnAim;    
    }

    void Update()
    {
        VisualizeThrowLine();
        ThrowCooldown();
    }

    void OnThrow(InputAction.CallbackContext ctx)
    {
        if (!isAiming || !canThrow || ammoCount <= 0) return;
        canThrow = false;
        SpawnThrownObject();
        ammoCount = throwablePrefabsList.Count;
        UpdateUICall();
    }

    void OnAim(InputAction.CallbackContext ctx)
    {
        isAiming = ctx.ReadValueAsButton();
        OnAimStatus(isAiming);
    }

    void ThrowCooldown()
    {
        if(canThrow) return;
        cooldownTimer += Time.deltaTime;
        if(cooldownTimer >= throwCooldown)
        {
            canThrow = true;
            cooldownTimer = 0f;
        }
    }

    void SpawnThrownObject()
    {
        var thrownObjectFromList = throwablePrefabsList[0].gameObject;
        var thrownObject = Instantiate(thrownObjectFromList, throwStartPoint.position, Quaternion.identity);
        throwablePrefabsList.Remove(thrownObjectFromList);
        thrownObject.GetComponent<Rigidbody>().linearVelocity = throwDirection * throwForce;
    }

    void VisualizeThrowLine()
    {
        if (isAiming)
        {
            // Set the camera to aim
            resetCamera = false;
            aimCamera.Priority = 1;
            freeLookCamera.Priority = 0;
            orientation.SetRotationTarget(aimCamera.transform);

            // Show the throw line
            throwLineRenderer.enabled = true;

            // Set up the line renderer count and positions
            throwLineRenderer.positionCount = segmentCount;
            throwDirection = freeLookCamera.transform.forward + aimOffset;

            // Calculate the throw trajectory
            Vector3 currentPosition = throwStartPoint.position;
            Vector3 currentVelocity = throwDirection * throwForce;

            // Iterate through each segment to calculate positions
            for (int i = 0; i < segmentCount; i++)
            {
                // Set the position for this segment
                throwLineRenderer.SetPosition(i, currentPosition);

                // Update velocity and position for the next segment
                currentVelocity.y += throwGravity * segmentLength;
                currentPosition += currentVelocity * segmentLength;

                // Check for collisions with throwable objects
                if (Physics.CheckSphere(currentPosition, hitRadius, throwableLayer))
                {
                    // Stop drawing the line if we hit something and set the hit area position and rotation
                    throwLineRenderer.positionCount = i + 1;
                    hitArea.SetActive(true);
                    hitArea.transform.position = SetHitPosition(currentPosition);
                    hitArea.transform.rotation = Quaternion.FromToRotation(Vector3.up, currentHit.normal);
                    
                    // Set look target of the aim camera
                    aimCamera.LookAt = hitArea.transform;
                    
                    break;
                }
                else
                    hitArea.SetActive(false);
            }
        }
        else
        {
            if(!resetCamera && !isAiming)
            {
                // Set the camera to free look and reset look target
                resetCamera = true;
                aimCamera.Priority = 0;
                freeLookCamera.Priority = 1;
                orientation.SetRotationTarget(freeLookCamera.transform);
                freeLookCamera.GetComponent<CinemachineOrbitalFollow>().VerticalAxis.Value = 27;
            }
            if(!isAiming)
            {
                // Hide the throw line and hit area when not aiming
                throwLineRenderer.positionCount = 0;
                throwLineRenderer.enabled = false;
                hitArea.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Account for different angles of impact by checking all directions to find the closest hit point
    /// </summary>
    Vector3 SetHitPosition(Vector3 currentPosition)
    {
        RaycastHit hit;
        if (Physics.SphereCast(currentPosition + hitDistance * Vector3.down, hitRadius, Vector3.up, out hit, hitDistance, throwableLayer))
        {
            print("Hit Up");
            currentHit = hit;
            return hit.point;
        }
        if (Physics.SphereCast(currentPosition + hitDistance * Vector3.up, hitRadius, Vector3.down, out hit, hitDistance, throwableLayer))
        {
            print("Hit Down");
            currentHit = hit;
            return hit.point;
        }
        if (Physics.SphereCast(currentPosition + hitDistance * Vector3.right, hitRadius, Vector3.left, out hit, hitDistance, throwableLayer))
        {
            print("Hit Left");
            currentHit = hit;
            return hit.point;
        }
        if (Physics.SphereCast(currentPosition + hitDistance * Vector3.left, hitRadius, Vector3.right, out hit, hitDistance, throwableLayer))
        {
            print("Hit Right");
            currentHit = hit;
            return hit.point;
        }
        if (Physics.SphereCast(currentPosition + hitDistance * Vector3.back, hitRadius, Vector3.forward, out hit, hitDistance, throwableLayer))
        {
            print("Hit Forward");
            currentHit = hit;
            return hit.point;
        }
        if (Physics.SphereCast(currentPosition + hitDistance * Vector3.forward, hitRadius, Vector3.back, out hit, hitDistance, throwableLayer))
        {
            print("Hit Back");
            currentHit = hit;
            return hit.point;
        }
        return currentPosition;
    }

    /// <summary>
    /// Adds the specified throwable object to the list a given number of times.
    /// </summary>
    public void AddThrowable(GameObject throwableOject, int count)
    {
        for (int i = 0; i < count; i++)
        {
            throwablePrefabsList.Add(throwableOject);
        }
        ammoCount = throwablePrefabsList.Count;
        UpdateUICall();
    }

    void UpdateUICall()
    {
        if (throwablePrefabsList.Count > 0)
            OnSpriteUpdate(throwablePrefabsList[0].GetComponent<ThrownObject>().thrownObjectImage);
        else
            OnSpriteUpdate(null); 
        OnAmmoUpdate(ammoCount);
    }
}
