using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerClimb : MonoBehaviour
{
    Rigidbody rb;
    PlayerMovement playerMovement;
    PlayerInput playerInput;
    [SerializeField] Transform orientation;
    [SerializeField] LayerMask climbableLayer;

    [SerializeField] float sphereCastRadius = 0.3f;
    [SerializeField] float climbCheckDistance = 0.5f;
    [SerializeField] Vector2 maxClimbSpeed = new(2,3);
    [SerializeField] Vector2 climbAcceleration = new(5, 5);
    RaycastHit climbableHit;
    Vector3 velocity;
    [SerializeField] bool climbableInFront;
    [SerializeField] bool isClimbing;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        playerInput.actions["Climb"].performed += OnClimb;
        playerInput.actions["Climb"].canceled += OnClimb;
    }

    private void OnDisable()
    {
        playerInput.actions["Climb"].performed -= OnClimb;
        playerInput.actions["Climb"].canceled -= OnClimb;
    }

    // Update is called once per frame
    void Update()
    {
        if (WallCheck() && isClimbing)
        {
            rb.useGravity = false;
            climbableInFront = true;
            playerMovement.canMove = false;
            playerMovement.canRotate = false;
        }
        else
        {
            rb.useGravity = true;
            climbableInFront = false;
            playerMovement.canMove = true;
            playerMovement.canRotate = true;
        }
    }

    void FixedUpdate()
    {
        Climb();
    }

    void OnClimb(InputAction.CallbackContext ctx)
    {
        isClimbing = ctx.ReadValueAsButton();
    }

    bool WallCheck()
    {
        if (Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out climbableHit, climbCheckDistance, climbableLayer))
        {
            return true;
        }
        return false;
    }

    void Climb()
    {
        if(!climbableInFront || !isClimbing) return;
        if (rb == null)
        {
            print($"No rigidbody on {gameObject}.");
            return;
        }

        // Convert velocity to local space
        velocity = transform.InverseTransformDirection(rb.linearVelocity);
        velocity.z = 0; // No forward/backward movement while climbing
        // Move when there is input
        if (playerMovement.moveInput != Vector2.zero)
        {
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
        {
            // Decelerate to a stop when no input
            velocity.y = Mathf.Lerp(velocity.y, 0f, 2 * maxClimbSpeed.y * Time.fixedDeltaTime);
            velocity.x = Mathf.Lerp(velocity.x, 0f, 2 * maxClimbSpeed.x * Time.fixedDeltaTime);
        }

        var clampedVelocity = transform.TransformDirection(velocity);
        // Apply clamped velocity
        rb.linearVelocity = clampedVelocity;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + orientation.forward * climbCheckDistance, sphereCastRadius);
    }
}
