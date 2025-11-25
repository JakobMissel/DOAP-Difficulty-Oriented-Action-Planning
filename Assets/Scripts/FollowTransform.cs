using Unity.Cinemachine;
using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    [SerializeField] bool subscribeToCameraChange;
    [Header("Position")]
    [SerializeField] bool transformPosition = true;
    [SerializeField] Transform positionTarget;
    [SerializeField] Vector3 positionOffset;
    [Header("Rotation")]
    [SerializeField] bool transformRotationX;
    [SerializeField] bool transformRotationY;
    [SerializeField] bool transformRotationZ;
    [SerializeField] public Transform rotationTarget;
    [SerializeField] Vector3 rotationOffset;

    void OnEnable()
    {
        if(subscribeToCameraChange)
            PlayerActions.changedCamera += SetNewCamera;
    }

    void OnDisable()
    {
        if(subscribeToCameraChange)
            PlayerActions.changedCamera -= SetNewCamera;        
    }

    void Update()
    {
        if (transformPosition && positionTarget != null)
            transform.position = positionTarget.position + positionOffset;
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

    void SetNewCamera(CinemachineCamera target)
    {
        rotationTarget = target.transform;
    }
}
