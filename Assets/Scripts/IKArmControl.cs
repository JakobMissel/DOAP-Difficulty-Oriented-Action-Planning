using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IKArmControl : MonoBehaviour
{
    Rig rig;
    [SerializeField] float differenceThreshold = 0.25f;
    [Header("Left")]
    [SerializeField] GameObject leftArmConstraints;
    [SerializeField] GameObject leftTarget;
    [SerializeField] Transform leftShoulder;
    [SerializeField] float leftDistanceToShoulder = 0.5f;
    
    Vector3 leftTargetStartPosition;
    TwoBoneIKConstraint leftArmIK;
    MultiRotationConstraint leftHandRotation;
    Vector3 leftClosestPoint;
    
    [Header("Right")]
    [SerializeField] GameObject rightArmConstraints;
    [SerializeField] GameObject rightTarget;
    [SerializeField] Transform rightShoulder;
    [SerializeField] float rightDistanceToShoulder = 0.5f;
    
    Vector3 rightTargetStartPosition;
    TwoBoneIKConstraint rightArmIK;
    MultiRotationConstraint rightHandRotation;
    Vector3 rightClosestPoint;

    bool isAiming;

    void Awake()
    {
        rig = GetComponent<Rig>();
        rig.weight = 1;

        leftArmIK = leftArmConstraints.GetComponent<TwoBoneIKConstraint>();
        leftHandRotation = leftArmConstraints.GetComponent<MultiRotationConstraint>();
        
        rightArmIK = rightArmConstraints.GetComponent<TwoBoneIKConstraint>();
        rightHandRotation = rightArmConstraints.GetComponent<MultiRotationConstraint>();

        leftTargetStartPosition = leftTarget.transform.position;
        rightTargetStartPosition = rightTarget.transform.position;
    }

    void OnEnable()
    {
        PlayerThrow.aimStatus += AimStatus;
    }

    void OnDisable()
    {
        PlayerThrow.aimStatus -= AimStatus;
    }

    void AimStatus(bool aimStatus)
    {
        isAiming = aimStatus;
    }

    void FixedUpdate() // To match with physics updates
    {
        SetTargets();
    }

    void SetTargets()
    {
        if (isAiming)
        {
            rig.weight = 0;
            leftTarget.transform.position = leftTargetStartPosition;
            rightTarget.transform.position = rightTargetStartPosition;
            return;
        }

        var leftDistance = Vector3.Distance(leftShoulder.position, leftClosestPoint);
        var rightDistance = Vector3.Distance(rightShoulder.position, rightClosestPoint);

        //var leftWeight = Mathf.Clamp01(1 - (leftDistance / leftDistanceToShoulder));
        //leftArmIK.weight = leftWeight;
        //leftHandRotation.weight = leftWeight;
        //leftTarget.transform.position = leftClosestPoint;

        //var rightWeight = Mathf.Clamp01(1 - (rightDistance / rightDistanceToShoulder));
        //rig.weight = rightWeight;
        //rightArmIK.weight = rightWeight;
        //rightHandRotation.weight = rightWeight;
        //rightTarget.transform.position = rightClosestPoint;

        //rig.weight = leftDistance > rightDistance ? leftWeight : rightWeight;

        if ((leftDistance < leftDistanceToShoulder) && leftDistance < rightDistance)
        {
            var weight = Mathf.Clamp01(1 - (leftDistance / leftDistanceToShoulder));
            leftArmIK.weight = weight;
            leftHandRotation.weight = weight;
            leftTarget.transform.position = leftClosestPoint;
        }
        else
        {
            leftArmIK.weight = 0;
            leftHandRotation.weight = 0;
            leftTarget.transform.position = leftTargetStartPosition;
        }
        if ((rightDistance < rightDistanceToShoulder) && rightDistance < leftDistance)
        {
            var weight = Mathf.Clamp01(1 - (rightDistance / rightDistanceToShoulder));
            rightArmIK.weight = weight;
            rightHandRotation.weight = weight;
            rightTarget.transform.position = rightClosestPoint;
        }
        else
        {
            rightArmIK.weight = 0;
            rightHandRotation.weight = 0;
            rightTarget.transform.position = rightTargetStartPosition;
        }
        if (Mathf.Abs(rightDistance - leftDistanceToShoulder) < differenceThreshold)
        {
            var leftWeight = Mathf.Clamp01(1 - (leftDistance / leftDistanceToShoulder));
            var rightWeight = Mathf.Clamp01(1 - (rightDistance / rightDistanceToShoulder));
            var weight = leftDistance > rightDistance ? leftWeight + differenceThreshold : rightWeight + differenceThreshold;

            rightArmIK.weight = weight;
            rightHandRotation.weight = weight;
            rightTarget.transform.position = rightClosestPoint;

            leftArmIK.weight = weight;
            leftHandRotation.weight = weight;
            leftTarget.transform.position = leftClosestPoint;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Climbable"))
        {
            leftClosestPoint = other.ClosestPoint(leftShoulder.position);
            rightClosestPoint = other.ClosestPoint(rightShoulder.position);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Climbable"))
        {
            leftClosestPoint = other.ClosestPoint(leftShoulder.position);
            rightClosestPoint = other.ClosestPoint(rightShoulder.position);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Climbable"))
        {
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(leftClosestPoint, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(rightClosestPoint, 0.1f);
    }
}
