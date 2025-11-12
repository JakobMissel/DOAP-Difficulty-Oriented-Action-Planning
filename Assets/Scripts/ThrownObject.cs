using UnityEngine;

public class ThrownObject : MonoBehaviour
{
    Rigidbody rb;
    [SerializeField] GameObject noiseAreaPrefab;
    [SerializeField] public Sprite thrownObjectImage;
    [SerializeField] bool destroyOnImpact = false;
    [SerializeField] float bounceMultiplier = 0.5f;
    [HideInInspector] public float noiseRadius = 5f;
    bool hasCollided = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if(hasCollided)
        {
            // Slow down linear and rotation velocity after collision
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.deltaTime * 2f);
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * 2f);
            return;
        }
        transform.Rotate(500 * Time.deltaTime, 0, 0);
    }

    void OnCollisionEnter(Collision collision)
    {
        Vector3 bounceDirection = Vector3.Reflect(-collision.relativeVelocity, collision.GetContact(0).normal);
        rb.linearVelocity = bounceDirection * bounceMultiplier;
        if (hasCollided) return;
        hasCollided = true;
        GameObject noiseArea = null;
        noiseArea = Instantiate(noiseAreaPrefab, transform.position, Quaternion.identity);
        noiseArea.GetComponent<NoiseArea>().SetScale(noiseRadius);
        if(!destroyOnImpact || !gameObject) return;
        Destroy(gameObject);
    }
}
