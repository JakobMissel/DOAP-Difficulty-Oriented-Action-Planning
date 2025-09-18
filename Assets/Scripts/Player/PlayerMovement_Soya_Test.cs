using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
	private Camera mainCamera;
	private Camera fullViewCamera;
	[Header("Movement Settings")]
	public float moveSpeed = 200f;
	public float jumpForce = 8f;
	public float gravity = 20f;

	[Header("Camera Settings")]
	public Transform cameraTransform; // Assign Main Camera here or in inspector
	public float mouseSensitivity = 2f;
	public float cameraDistance = 5f;
	public float minYAngle = -35f;
	public float maxYAngle = 60f;

	private float yaw = 0f;
	private float pitch = 0f;

	private CharacterController controller;
	private Vector3 moveDirection = Vector3.zero;

	void Start()
	{
		controller = GetComponent<CharacterController>();

		// Find cameras by name
		GameObject mainCamObj = GameObject.Find("Main Camera");
		GameObject fullViewCamObj = GameObject.Find("Camera Full_View");
		if (mainCamObj != null)
		{
			mainCamera = mainCamObj.GetComponent<Camera>();
			cameraTransform = mainCamera.transform;
		}
		if (fullViewCamObj != null)
		{
			fullViewCamera = fullViewCamObj.GetComponent<Camera>();
		}

		// Enable main camera, disable full view at start
		if (mainCamera != null) mainCamera.enabled = true;
		if (fullViewCamera != null) fullViewCamera.enabled = false;

		// Initialize yaw and pitch from current camera rotation
		if (cameraTransform != null)
		{
			Vector3 angles = cameraTransform.eulerAngles;
			yaw = angles.y;
			pitch = angles.x;
		}
	}

	void Update()
	{
		HandleMovement();
		HandleCamera();
		HandleCameraToggle();
	}
	void HandleCameraToggle()
	{
		if (Input.GetKeyDown(KeyCode.M))
		{
			if (mainCamera != null && fullViewCamera != null)
			{
				bool mainActive = mainCamera.enabled;
				mainCamera.enabled = !mainActive;
				fullViewCamera.enabled = mainActive;
			}
		}
	}

	void HandleMovement()
	{
		if (controller.isGrounded)
		{
			// Get input
			float horizontal = Input.GetAxis("Horizontal");
			float vertical = Input.GetAxis("Vertical");

			// Calculate movement direction relative to camera
			Vector3 forward = Vector3.zero;
			Vector3 right = Vector3.zero;

			if (cameraTransform != null && mainCamera != null && mainCamera.enabled)
			{
				// Get camera's forward and right directions, but keep them horizontal
				forward = cameraTransform.forward;
				forward.y = 0;
				forward.Normalize();

				right = cameraTransform.right;
				right.y = 0;
				right.Normalize();
			}
			else
			{
				// Fallback to world directions if no camera or if full view camera is active
				forward = transform.forward;
				right = transform.right;
			}

			// Calculate movement direction based on camera orientation
			moveDirection = (forward * vertical) + (right * horizontal);
			moveDirection *= moveSpeed;

			// Jump
			if (Input.GetButton("Jump"))
			{
				moveDirection.y = jumpForce;
			}
		}

		// Apply gravity
		moveDirection.y -= gravity * Time.deltaTime;

		// Move the character
		controller.Move(moveDirection * Time.deltaTime);
	}

	void HandleCamera()
	{
		if (cameraTransform == null)
			return;

		float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
		float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

		yaw += mouseX;
		pitch -= mouseY;
		pitch = Mathf.Clamp(pitch, minYAngle, maxYAngle);

		// Calculate camera position and rotation
		Vector3 targetPosition = transform.position;
		Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
		Vector3 offset = rotation * new Vector3(0, 0, -cameraDistance);
		cameraTransform.position = targetPosition + offset + Vector3.up * 2f; // 2f is camera height offset
		cameraTransform.LookAt(targetPosition + Vector3.up * 1.5f); // Look at player's head
	}
}