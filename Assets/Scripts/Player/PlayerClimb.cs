using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerClimb : MonoBehaviour
{
    Rigidbody rb;
    PlayerMovement playerMovement;
    PlayerInput playerInput;
    [SerializeField] Transform orientation;
    [SerializeField] LayerMask climbableLayer;

    [Header("Detection")]
    [SerializeField] float sphereCastRadius = 0.3f;
    [SerializeField] float climbCheckDistance = 0.5f;
    [Header("Speed")]
    [SerializeField] float stickToWallForce = 0.2f;
    [SerializeField] Vector2 maxClimbSpeed = new(2,3);
    [SerializeField] Vector2 climbAcceleration = new(5, 5);
    [SerializeField] Vector2 climbDeceleration = new(15, 15);
    [Header("Stamina")]
    [SerializeField] public float maxStamina = 5f;
    [SerializeField] float staminaDrainRate = 1f;
    public float currentStamina;
    [Header("Rotation")]
    [SerializeField] float rotationSmoothTime = 0.1f;
    
    RaycastHit climbableHit;
    Vector3 velocity;
    float rotationSmoothAngle;

    [Header("Debugging")]
    [SerializeField] bool climbButtonHeld;
    [SerializeField] public bool onWall;
    [SerializeField] bool isClimbing;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        playerInput = GetComponent<PlayerInput>();
        currentStamina = maxStamina;
    }

    void OnEnable()
    {
        // Subscribe to input events
        playerInput.actions["Climb"].performed += OnClimb;
        playerInput.actions["Climb"].canceled += OnClimb;
    }

    void OnDisable()
    {
        // Unsubscribe from input events
        playerInput.actions["Climb"].performed -= OnClimb;
        playerInput.actions["Climb"].canceled -= OnClimb;
    }
    
    void Update()
    {
        if (WallCheck() && climbButtonHeld && currentStamina > 0)
        {
            rb.useGravity = false;
            playerMovement.canMove = false;
            playerMovement.canRotate = false;
        }
        else
        {
            rb.useGravity = true;
            playerMovement.canMove = true;
            playerMovement.canRotate = true;
            onWall = false;
        }
        DepleteStamina();
        RegenerateStamina();
    }

    void FixedUpdate()
    {
        Climb();
    }

    void OnClimb(InputAction.CallbackContext ctx)
    {
        climbButtonHeld = ctx.ReadValueAsButton();
    }

    bool WallCheck()
    {
        // Do something else than sphere cast at some point - at the moment it is a bit unreliable for some reason 
        if (Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out climbableHit, climbCheckDistance, climbableLayer))
        {
            return true;
        }
        return false;
    }

    void Climb()
    {
        if (!WallCheck() || !climbButtonHeld || currentStamina <= 0)
        {
            isClimbing = false;
            return;
        }

        FaceClimbableSurface();

        onWall = true;
        
        // Convert velocity to local space
        velocity = transform.InverseTransformDirection(rb.linearVelocity);
        velocity.z = stickToWallForce;

        // Move when there is input
        if (playerMovement.moveInput != Vector2.zero)
        {
            isClimbing = true;
            // Convert moveInput to world space direction
            var forceY = orientation.up * playerMovement.moveInput.y * maxClimbSpeed.y;
            var forceX = orientation.right * playerMovement.moveInput.x * maxClimbSpeed.x;

            // Apply forces
            rb.AddForce(forceY * (climbAcceleration.y * Time.deltaTime), ForceMode.VelocityChange);
            rb.AddForce(forceX * (climbAcceleration.x * Time.deltaTime), ForceMode.VelocityChange);

            // Clamp velocity
            velocity.y = Mathf.Clamp(velocity.y, -maxClimbSpeed.y, maxClimbSpeed.y);
            velocity.x = Mathf.Clamp(velocity.x, -maxClimbSpeed.x, maxClimbSpeed.x);
        }
        else
            isClimbing = false;
        
        // Decelerate to a stop when no input
        if (playerMovement.moveInput.y == 0)
            velocity.y = Mathf.Lerp(velocity.y, 0f, climbDeceleration.y * Time.fixedDeltaTime);
        if(playerMovement.moveInput.x == 0)
            velocity.x = Mathf.Lerp(velocity.x, 0f, climbDeceleration.x * Time.fixedDeltaTime);

        var clampedVelocity = transform.TransformDirection(velocity);
        
        // Apply clamped velocity
        rb.linearVelocity = clampedVelocity;
    }

    void DepleteStamina()
    {
        if(!onWall || playerMovement.IsGrounded()) return;
        if(isClimbing)
            currentStamina -= staminaDrainRate * Time.deltaTime;
        else
            currentStamina -= (staminaDrainRate / 4) * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    void RegenerateStamina()
    {
        if (onWall || !playerMovement.IsGrounded()) return;
        currentStamina += (staminaDrainRate * 1.2f) * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    void FaceClimbableSurface()
    {
        var lookDirection = climbableHit.point - transform.position;
        lookDirection.y = 0;
        var targetDirection = Quaternion.LookRotation(lookDirection);
        var lerpAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetDirection.eulerAngles.y, ref rotationSmoothAngle, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0, lerpAngle, 0);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + orientation.forward * climbCheckDistance, sphereCastRadius);
    }
}
