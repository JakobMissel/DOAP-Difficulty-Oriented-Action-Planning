using System;
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
    [SerializeField] float diagonalDampener = 0.75f;
    [HideInInspector] public bool canMove = true;
    [HideInInspector] public Vector2 moveInput;
    public Vector3 velocity;
    float currentZSpeed;
    float currentXSpeed;

    [Header("Sneak")]
    [SerializeField] Vector3 maxSneakMoveSpeed = new(1, 0, 1.5f);
    [SerializeField] Vector3 sneakMoveAcceleration = new(10, 0, 10);
    [SerializeField] Vector3 sneakMoveDeceleration = new(6, 0, 6);

    [Header("Ground")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] LayerMask stairLayer;
    [SerializeField] float groundCheckDistance = 0.1f;
    [SerializeField] float groundCheckRadius = 0.3f;
    [SerializeField] float groundCheckHeight = 0.1f;
    [SerializeField] float dampeningOnStairs = 4;

    [Header("Rotation")]
    [SerializeField] Transform orientation;
    [SerializeField] [Range(0, 0.5f)] float smoothTime = 0.15f;
    [SerializeField] bool lockCursor = true;
    [HideInInspector] public bool canRotate = true;
    float smoothAngle;
    float targetAngle;
    bool isAiming;
    bool isSneaking;
    

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
    }

    void OnEnable()
    {
        playerInput.actions["Move"].performed += OnMove;
        playerInput.actions["Move"].canceled += OnMove;
        PlayerActions.aimStatus += OnAimStatus;
        PlayerActions.sneakStatus += OnSneakStatus;
    }

    void OnDisable()
    {
        playerInput.actions["Move"].performed -= OnMove;
        playerInput.actions["Move"].canceled -= OnMove;
        PlayerActions.aimStatus -= OnAimStatus;
        PlayerActions.sneakStatus -= OnSneakStatus;
    }

    void LateUpdate()
    {
        Rotate();
    }

    void FixedUpdate()
    {
        if (isSneaking)
            Move(maxSneakMoveSpeed, sneakMoveAcceleration, sneakMoveDeceleration);
        else
            Move(maxMoveSpeed, moveAcceleration, moveDeceleration);
        if(OnStairs())
        {
            rb.linearDamping = dampeningOnStairs;
        }
        else
        {
            rb.linearDamping = 0;
        }
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    void Move(Vector3 maxSpeed, Vector3 acceleration, Vector3 deceleration)
    {
        if (!canMove) return;

        bool isCurrentlyMoving = moveInput != Vector2.zero;
        
        // Send move action (for tutorial)
        PlayerActions.OnMoveStatus(isCurrentlyMoving);
        
        // Debug log to verify event is firing
        if (Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
        {
            Debug.Log($"[PlayerMovement] Move status: {isCurrentlyMoving}, moveInput: {moveInput}, velocity: {rb.linearVelocity.magnitude:F2}");
        }

        // Convert velocity to local space
        velocity = transform.InverseTransformDirection(rb.linearVelocity);

        GetCurrentSpeed(maxSpeed);

        // Move when there is input
        if (moveInput != Vector2.zero)
        {
            // Send sneaking action when moving (for tutorial)
            PlayerActions.OnIsSneaking(isSneaking);
            
            // Convert moveInput to world space direction
            var forceZ = orientation.forward * moveInput.y * currentZSpeed;
            var forceX = orientation.right * moveInput.x * currentXSpeed;

            // Apply forces
            rb.AddForce(forceZ * (acceleration.z * Time.deltaTime), ForceMode.VelocityChange);
            rb.AddForce(forceX * (acceleration.x * Time.deltaTime), ForceMode.VelocityChange);

            // Clamp velocity (adjust for diagonal movement)
            if(moveInput.x == 0)
                velocity.z = Mathf.Clamp(velocity.z, -currentZSpeed, currentZSpeed);
            else
                velocity.z = Mathf.Clamp(velocity.z, -currentZSpeed * diagonalDampener, currentZSpeed * diagonalDampener);
            velocity.x = Mathf.Clamp(velocity.x, -currentXSpeed, currentXSpeed);

        }

        if (moveInput.y == 0)
            velocity.z = Mathf.Lerp(velocity.z, 0f, deceleration.z * Time.fixedDeltaTime);
        if (moveInput.x == 0)
            velocity.x = Mathf.Lerp(velocity.x, 0f, deceleration.x * Time.fixedDeltaTime);

        // Preserve Y velocity
        var velocityY = rb.linearVelocity.y;

        // Convert back to world space
        var clampedVelocity = transform.TransformDirection(velocity);
        clampedVelocity.y = velocityY;

        // Apply clamped velocity
        rb.linearVelocity = clampedVelocity;
    }

    void GetCurrentSpeed(Vector3 maxSpeed)
    {
        // Forward/backward movement
        if (moveInput.y > 0)
            currentZSpeed = maxSpeed.z;
        else if (moveInput.y < 0)
            currentZSpeed = maxSpeed.z * 0.7f;
        else
            currentZSpeed = 0;

        // Side to side movement
        if (moveInput.x != 0)
            currentXSpeed = maxSpeed.x;
        else
            currentXSpeed = 0;
    }

    public bool IsGrounded()
    {
        return Physics.CheckBox(transform.position + Vector3.down * groundCheckDistance, new Vector3(groundCheckRadius, groundCheckHeight, groundCheckRadius), Quaternion.identity, groundLayer);
    }

    public bool OnStairs()
    {
        RaycastHit hit;
        if(Physics.Raycast(transform.position, Vector3.down * groundCheckDistance * 10, out hit))
        {
            if(hit.collider.CompareTag("Stair"))
            {
                return true;
            }
        }
        return false;
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
        if (!lockCursor && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Rotate player to match camera's Y rotation if moving
        var cameraYRotation = orientation.rotation.eulerAngles.y;
        if (moveInput == Vector2.zero && !isAiming) return;
        targetAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraYRotation, ref smoothAngle, smoothTime);
        rb.MoveRotation(Quaternion.Euler(0, targetAngle, 0) * Quaternion.Euler(transform.forward));
    }

    void OnAimStatus(bool aimStatus)
    {
        isAiming = aimStatus;
    }

    void OnSneakStatus(bool isSneaking)
    {
        this.isSneaking = isSneaking;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.down * groundCheckDistance, new Vector3(groundCheckRadius * 2, groundCheckHeight, groundCheckRadius * 2));
    }
}
