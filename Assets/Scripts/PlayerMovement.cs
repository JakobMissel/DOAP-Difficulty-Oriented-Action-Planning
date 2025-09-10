using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    PlayerInput playerInput;
    Rigidbody rb;
    [Header("Move")]
    [SerializeField] float movementSpeed = 5f;
    [SerializeField] float moveAcceleration = 10f;
    Vector2 moveInput;
    [Header("Rotation")]
    [SerializeField] float rotationSpeed = 720f;
    [SerializeField] float rotationAcceleration = 10f;
    Vector2 lookInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
    }

    void OnEnable()
    {
        playerInput.actions["Move"].performed += OnMove;
        playerInput.actions["Move"].canceled += OnMove;
        playerInput.actions["Look"].performed += OnLook;
        playerInput.actions["Look"].canceled += OnLook;
    }

    void OnDisable()
    {
        playerInput.actions["Move"].performed -= OnMove;
        playerInput.actions["Move"].canceled -= OnMove;
        playerInput.actions["Look"].performed -= OnLook;
        playerInput.actions["Look"].canceled -= OnLook;
    }
    void Update()
    {
        Move();
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    void Move()
    {
        if (rb == null)
        {
            print($"No rigidbody on {gameObject}.");
            return;
        }
        var targetVelocity = new Vector3(moveInput.x, 0, moveInput.y) * movementSpeed;
        rb.linearVelocity = targetVelocity;
    }

    void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }
}
