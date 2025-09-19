using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    PlayerInput playerInput;
    Rigidbody rb;
    [Header("Move")]
    [SerializeField] Vector3 maxMoveSpeed = new(3, 0, 4);
    [SerializeField] Vector3 moveAcceleration = new(5, 0, 5);
    [SerializeField] Vector3 moveDeceleration = new(6, 0, 6);
    [HideInInspector] public bool canMove = true;
    [HideInInspector] public Vector2 moveInput;
    Vector3 velocity;
    float currentZSpeed;
    float currentXSpeed;

    [Header("Ground")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float groundCheckDistance = 0.1f;
    [SerializeField] float groundCheckRadius = 0.3f;
    [SerializeField] float groundCheckHeight = 0.1f;
    bool isGrounded;

    [Header("Rotation")]
    [SerializeField] Transform orientation;
    [SerializeField] [Range(0, 0.5f)] float smoothTime = 0.15f;
    [SerializeField] bool lockCursor = true;
    [HideInInspector] public bool canRotate = true;
    float smoothAngle;
    float targetAngle;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
    }

    void OnEnable()
    {
        // Subscribe to input events
        playerInput.actions["Move"].performed += OnMove;
        playerInput.actions["Move"].canceled += OnMove;
    }

    void OnDisable()
    {
        // Unsubscribe from input events
        playerInput.actions["Move"].performed -= OnMove;
        playerInput.actions["Move"].canceled -= OnMove;
    }

    private void LateUpdate()
    {
        Rotate();
    }

    void FixedUpdate()
    {
        Move();
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        // Read movement input
        moveInput = ctx.ReadValue<Vector2>();
    }

    void Move()
    {
        if(!canMove) return;

        // Convert velocity to local space
        velocity = transform.InverseTransformDirection(rb.linearVelocity);

        GetCurrentSpeed();

        // Move when there is input
        if (moveInput != Vector2.zero)
        {
            // Convert moveInput to world space direction
            var forceZ = orientation.forward * moveInput.y * currentZSpeed;
            var forceX = orientation.right * moveInput.x * currentXSpeed;

            // Apply forces
            rb.AddForce(forceZ * (moveAcceleration.z * Time.deltaTime), ForceMode.VelocityChange);
            rb.AddForce(forceX * (moveAcceleration.x * Time.deltaTime), ForceMode.VelocityChange);

            // Clamp velocity
            velocity.z = Mathf.Clamp(velocity.z, -maxMoveSpeed.z, maxMoveSpeed.z);
            velocity.x = Mathf.Clamp(velocity.x, -maxMoveSpeed.x, maxMoveSpeed.x);
        }
        else
        {
            // Decelerate to a stop when no input
            velocity.z = Mathf.Lerp(velocity.z, 0f, moveDeceleration.z * Time.fixedDeltaTime);
            velocity.x = Mathf.Lerp(velocity.x, 0f, moveDeceleration.x * Time.fixedDeltaTime);
        }

        // Preserve Y velocity
        var velocityY = rb.linearVelocity.y;

        // Convert back to world space
        var clampedVelocity = transform.TransformDirection(velocity);
        clampedVelocity.y = velocityY;

        // Apply clamped velocity
        rb.linearVelocity = clampedVelocity;
    }

    void GetCurrentSpeed()
    {
        // Forward/backward movement
        if (moveInput.y < 0)
            currentZSpeed = maxMoveSpeed.z / 2;
        else if (moveInput.y > 0)
            currentZSpeed = maxMoveSpeed.z;
        else
            currentZSpeed = 0;

        // Side to side movement
        if (moveInput.x != 0)
            currentXSpeed = maxMoveSpeed.x;
        else
            currentXSpeed = 0;
    }

    public bool IsGrounded()
    {
        return Physics.CheckBox(transform.position + Vector3.down * groundCheckDistance, new Vector3(groundCheckRadius, groundCheckHeight, groundCheckRadius), Quaternion.identity, groundLayer);
    }


    void Rotate()
    {
        if (!canRotate) return;

        // Lock or unlock cursor based on setting
        if (lockCursor && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if(!lockCursor && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Rotate player to match camera's Y rotation if moving
        var cameraYRotation = orientation.rotation.eulerAngles.y;
        if (moveInput == Vector2.zero) return;
            targetAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraYRotation, ref smoothAngle, smoothTime);
        rb.MoveRotation(Quaternion.Euler(0, targetAngle, 0) * Quaternion.Euler(transform.forward));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.down * groundCheckDistance, new Vector3(groundCheckRadius * 2, groundCheckHeight, groundCheckRadius * 2));
    }
}
