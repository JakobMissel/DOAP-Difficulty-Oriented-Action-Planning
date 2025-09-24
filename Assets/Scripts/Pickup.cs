using UnityEngine;

public class Pickup : MonoBehaviour
{
    [Header("\"Animation\"")]
    [SerializeField][Tooltip("Should the pickup move vertically?")] protected bool verticalAnimation = true;
    [SerializeField][Tooltip("The vertical speed of the pickup. \nDefault: 2")] protected float verticalSpeed = 2;
    [SerializeField][Tooltip("The height of the vertical movement. \nDefault: 0.5")] protected float verticalHeight = 0.5f;
    [SerializeField][Tooltip("Should the pickup rotate around itself?")] protected bool rotate = true;
    [SerializeField][Tooltip("The rotation speed of the pickup. \nDefault: 100")] protected float rotationSpeed = 100;
    float initialY;
    [Header("Audio")]
    [SerializeField] GameObject audioGameObject;
    [SerializeField] AudioClip audioClip;
    GameObject newAudioGameObject;
    [Header("Interactable")]
    [SerializeField] bool canBepickedUp = true;
    [SerializeField] public bool buttonRequired;
    [SerializeField] public bool holdRequired;
    [SerializeField] float holdDuration;
    [SerializeField] public bool buttonHeld;
    [HideInInspector] public bool buttonPressed;
    public float holdTime;

    void Awake()
    {
        initialY = transform.position.y;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!canBepickedUp) return;
        if (other.CompareTag("Player"))
        {
            if (buttonRequired)
            {
                PlayerInteract.OnAddPickup(this);
            }
            else
                ActivatePickup(other);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && buttonRequired)
        {
            if(buttonPressed)
                ActivatePickup(other);
            if (holdRequired)
            {
                HoldButton(other);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!canBepickedUp) return;
        if (other.CompareTag("Player") && buttonRequired)
        {
            PlayerInteract.OnRemovePickup(this);
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
        PlayerInteract.OnRemovePickup(this);
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
        transform.Rotate(Vector3.up * speed * Time.deltaTime);
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
        if (!canBepickedUp) return;
        if (buttonHeld)
        {
            holdTime += Time.deltaTime;
            if (holdTime >= holdDuration)
            {
                ActivatePickup(other);
                canBepickedUp = false;   
            }
        }
        else
        {
            holdTime -= Time.deltaTime;
            if(holdTime <= 0)
                holdTime = 0;
        }
    }
}
