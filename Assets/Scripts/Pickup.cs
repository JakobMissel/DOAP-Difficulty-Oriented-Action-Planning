using UnityEngine;

public class Pickup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] [Tooltip("Text that is displayed when close enough to interact with pickup.")] [TextArea] protected string displayName;
    public string DisplayName => displayName;

    [Header("\"Animation\"")]
    [SerializeField][Tooltip("Should the pickup move vertically?")] protected bool verticalAnimation = true;
    [SerializeField][Tooltip("The vertical speed of the pickup.")] protected float verticalSpeed = 2;
    [SerializeField][Tooltip("The height of the vertical movement.")] protected float verticalHeight = 0.5f;
    [SerializeField][Tooltip("Should the pickup rotate around itself?")] protected bool rotate = true;
    [SerializeField][Tooltip("The rotation speed of the pickup.")] protected float rotationSpeed = 100;
    [SerializeField][Tooltip("Should the pickup rotate around the X axis?")] protected bool rotateX = false;
    [SerializeField][Tooltip("Should the pickup rotate around the Y axis?")] protected bool rotateY = true;
    [SerializeField][Tooltip("Should the pickup rotate around the Z axis?")] protected bool rotateZ = false;

    float initialY;
    [Header("Audio")]
    [SerializeField] GameObject audioGameObject;
    [SerializeField] AudioClip audioClip;
    GameObject newAudioGameObject;
    [Header("Interactable")]
    [SerializeField] protected bool destroyOnPickup = true;
    [SerializeField] protected bool canBepickedUp = true;
    [SerializeField] bool buttonRequired;
    [SerializeField] bool holdRequired;
    [SerializeField] float holdDuration;
    float holdTime;
    [HideInInspector] public bool buttonPressed;
    [HideInInspector] public bool buttonHeld;
    public bool ButtonRequired => buttonRequired;
    public bool HoldRequired => holdRequired;
    public float HoldDuration => holdDuration;
    public float HoldTime => holdTime;



    protected virtual void Awake()
    {
        initialY = transform.position.y;
    }

    protected virtual void OnEnable()
    {
        CheckpointManager.loadCheckpoint += ResetPickup;
    }

    protected virtual void OnDisable()
    {
        CheckpointManager.loadCheckpoint -= ResetPickup;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!canBepickedUp) return;
        if (other.CompareTag("Player"))
        {
            if (buttonRequired)
                PlayerActions.OnAddPickupToInteractableList(this);
            else
                ActivatePickup(other);
        }
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && buttonRequired)
        {
            if(buttonPressed)
                ActivatePickup(other);
            if (holdRequired)
                HoldButton(other);
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && buttonRequired)
        {
            PlayerActions.OnRemovePickupFromInteractableList(this);
            buttonHeld = false;
            holdTime = 0;
        }
    }

    /// <summary>
    /// Activates the pickup, plays a sound if given an audio clip, and finally destroys the pickup.
    /// </summary>
    protected virtual void ActivatePickup(Collider other)
    {
        if (audioClip)
        {
            audioGameObject.GetComponent<AudioSource>().clip = audioClip;
            newAudioGameObject = Instantiate(audioGameObject, transform.position, Quaternion.identity);
        }
        PlayerActions.OnRemovePickupFromInteractableList(this);
        PlayerActions.OnPickedUpItem(this);

        if (destroyOnPickup)
            Destroy(gameObject);
    }

    void Update()
    {
        Animate(verticalAnimation, verticalSpeed, rotate, rotationSpeed);
    }

    /// <summary>
    /// Animates the pickup by moving it up and down and/or rotating it, depending on the parameters.
    /// </summary>
    void Animate(bool verticalAnimation, float verticalSpeed, bool rotate, float rotationSpeed)
    {
        if (verticalAnimation)
            MoveUpAndDown(verticalSpeed);
        if (rotate)
            Rotate(rotationSpeed);
    }

    /// <summary>
    /// Rotates the pickup around itself.
    /// </summary>
    void Rotate(float speed)
    {
        if(rotateX)
            transform.Rotate(Vector3.right * speed * Time.deltaTime);
        if(rotateY)
            transform.Rotate(Vector3.up * speed * Time.deltaTime);
        if(rotateZ)
            transform.Rotate(Vector3.forward * speed * Time.deltaTime);
    }

    /// <summary>
    /// Moves the pickup up and down in a sine wave pattern.
    /// </summary>
    void MoveUpAndDown(float speed)
    {
        float y = Mathf.PingPong(Time.time * speed, verticalHeight) + initialY;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    void HoldButton(Collider other)
    {
        if (buttonHeld)
        {
            holdTime += Time.deltaTime;
            if (holdTime >= holdDuration)
            {
                holdTime = 0;
                buttonHeld = false;
                ActivatePickup(other);
            }
        }
        else
        {
            holdTime -= Time.deltaTime;
            if(holdTime <= 0)
                holdTime = 0;
        }
    }

    void ResetPickup()
    {
        holdTime = 0;
        buttonHeld = false;
        buttonPressed = false;
    }
}
