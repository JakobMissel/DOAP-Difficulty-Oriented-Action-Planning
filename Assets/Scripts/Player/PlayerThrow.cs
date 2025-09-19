using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerThrow : MonoBehaviour
{
    PlayerInput playerInput;
    [Header("Aiming")]
    [SerializeField] Transform cameraTransform;
    [SerializeField] Vector3 aimOffset = new(0,1.5f,0);
    [SerializeField] LayerMask throwableLayer;
    [Header("Thrown Object")]
    [SerializeField] GameObject throwablePrefab;
    [SerializeField] Transform throwPoint;
    [Header("Throw Visualization")]
    [SerializeField] LineRenderer throwLineRenderer;
    [SerializeField] int segmentCount = 200;
    [SerializeField] float segmentLength = 0.02f;
    [SerializeField] GameObject hitAreaPrefab;
    GameObject hitArea;
    [SerializeField] float hitRadius = 0.2f;
    [SerializeField] float hitDistance = 0.2f;
    [Header("Throw Settings")]
    [SerializeField] float throwForce = 7f;
    [SerializeField] float throwGravity = -9.81f;

    Vector3 throwDirection;

    [HideInInspector] public bool isAiming = false;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        hitArea = Instantiate(hitAreaPrefab);
        hitArea.SetActive(false);
    }
    void OnEnable()
    {
        playerInput.actions["Throw"].performed += OnThrow;
        playerInput.actions["Throw"].canceled += OnThrow;
        playerInput.actions["Aim"].performed += OnAim;
        playerInput.actions["Aim"].canceled += OnAim;
    }

    void OnDisable()
    {
        playerInput.actions["Throw"].performed -= OnThrow;
        playerInput.actions["Throw"].canceled -= OnThrow;
        playerInput.actions["Aim"].performed -= OnAim;
        playerInput.actions["Aim"].canceled -= OnAim;    
    }

    void Update()
    {
        VisualizeThrowLine();
    }

    void OnThrow(InputAction.CallbackContext ctx)
    {

    }

    void OnAim(InputAction.CallbackContext ctx)
    {
        isAiming = ctx.ReadValueAsButton();
    }

    void VisualizeThrowLine()
    {
        if (isAiming)
        {
            // Show the throw line and hit area
            throwLineRenderer.enabled = true;
            hitArea.SetActive(true);

            // Set up the line renderer count and positions
            throwLineRenderer.positionCount = segmentCount;
            throwDirection = cameraTransform.forward + aimOffset;

            // Calculate the throw trajectory
            Vector3 currentPosition = throwPoint.position;
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
                if (Physics.CheckSphere(currentPosition, hitRadius*2, throwableLayer))
                {
                    // Stop drawing the line if we hit something and set the hit area position
                    throwLineRenderer.positionCount = i + 1;
                    hitArea.transform.position = currentPosition;
                    break;
                }
            }
        }
        else
        {
            // Hide the throw line and hit area when not aiming
            throwLineRenderer.enabled = false;
            hitArea.SetActive(false);
        }
    }
}
