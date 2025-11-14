using UnityEngine;

public class CameraFollowTargetAdjuster : MonoBehaviour
{
    [SerializeField] Vector3 startPosition = new(0.5f, 1.4f, 0);
    [SerializeField] Vector3 hitWallPosition = new(0, 1.4f, 0);
    [SerializeField] float moveTime = 1f;
    [SerializeField] float delayBeforeReturn = 2f;

    private Vector3 velocity = Vector3.zero;  
    private bool isInContactWithWall = false;
    private float timer = 0f;

    void Update()
    {
        if (isInContactWithWall)
        {
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, hitWallPosition, ref velocity, moveTime);
            timer = 0f;
        }
        else
        {
            timer += Time.deltaTime;

            if (timer >= delayBeforeReturn)
            {
                transform.localPosition = Vector3.SmoothDamp(transform.localPosition, startPosition, ref velocity, moveTime);
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            isInContactWithWall = true;  
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            isInContactWithWall = false;
        }
    }
}
