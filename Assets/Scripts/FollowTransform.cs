using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    [SerializeField] bool transformPosition = true;
    [SerializeField] Transform positionTarget;
    [SerializeField] bool transformRotationX;
    [SerializeField] bool transformRotationY;
    [SerializeField] bool transformRotationZ;
    [SerializeField] public Transform rotationTarget;
    [SerializeField] Vector3 rotationOffset;

    void Update()
    {
        if (transformPosition)
            transform.position = positionTarget.position;
        if (transformRotationX || transformRotationY || transformRotationZ)
        {
            Vector3 newRotation = transform.eulerAngles;
            if (transformRotationX)
                newRotation.x = rotationTarget.eulerAngles.x;
            if (transformRotationY)
                newRotation.y = rotationTarget.eulerAngles.y;
            if (transformRotationZ)
                newRotation.z = rotationTarget.eulerAngles.z;
            transform.eulerAngles = newRotation + rotationOffset;
        }
    }

    public void SetRotationTarget(Transform target)
    {
        rotationTarget = target;
    }
}
