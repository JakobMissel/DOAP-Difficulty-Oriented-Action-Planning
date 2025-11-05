using UnityEngine;

public class CameraFollowTargetAdjuster : MonoBehaviour
{
    [SerializeField] Vector3 targetPosition = new(0.5f, 1.4f, 0);
    [SerializeField] Vector3 hitWallPosition = new(0, 1.4f, 0);
    [SerializeField] float moveSpeed = 1f;

    bool isInContactWithWall = false;


    void Update()
    {
        if (isInContactWithWall)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, hitWallPosition, moveSpeed * Time.deltaTime);
        }
        else
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, moveSpeed * Time.deltaTime);
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


    //void OnTriggerStay(Collider other)
    //{
    //    if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))  
    //    {
    //        if (!isInContactWithWall)  
    //        {
    //            isInContactWithWall = true;
    //            transform.localPosition = hitWallPosition; 
    //        }
    //    }
    //}

    //void OnTriggerExit(Collider other)
    //{
    //    if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
    //    {
    //        if (isInContactWithWall) 
    //        {
    //            isInContactWithWall = false;  
    //            transform.localPosition = targetPosition;  
    //        }
    //    }
    //}
}
