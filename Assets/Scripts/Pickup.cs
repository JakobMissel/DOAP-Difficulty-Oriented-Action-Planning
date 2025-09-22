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
    float distance;
    [Header("Pickup")]
    [SerializeField] AudioClip audioClip;
    [SerializeField] bool canBepickedUp = true;

    private void Awake()
    {
        initialY = transform.position.y;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!canBepickedUp) return;
        if (other.CompareTag("Player"))
        {
            ActivatePickup(other.gameObject);
        }
    }

    /// <summary>
    /// Activates the pickup, plays a sound (on the player's parent's Audio Source component) if given an audio clip, and finally destroys it.
    /// </summary>
    /// <param name="player"></param>
    protected virtual void ActivatePickup(GameObject player)
    {
        if (audioClip != null)
        {
            player.GetComponentInParent<AudioSource>().PlayOneShot(audioClip);
        }
        Destroy(gameObject);
    }

    void Update()
    {
        Animate(verticalAnimation, verticalSpeed, rotate, rotationSpeed);
    }

    /// <summary>
    /// Animates the pickup by moving it up and down and/or rotating it, depending on the parameters.
    /// </summary>
    /// <param name="verticalAnimation"></param>
    /// <param name="verticalSpeed"></param>
    /// <param name="rotate"></param>
    /// <param name="rotationSpeed"></param>
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
    /// <param name="speed"></param>
    void Rotate(float speed)
    {
        transform.Rotate(Vector3.up * speed * Time.deltaTime);
    }

    /// <summary>
    /// Moves the pickup up and down in a sine wave pattern.
    /// </summary>
    /// <param name="speed"></param>
    void MoveUpAndDown(float speed)
    {
        float y = Mathf.PingPong(Time.time * speed, verticalHeight) + initialY;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }
}
