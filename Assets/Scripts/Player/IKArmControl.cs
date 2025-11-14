using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IKArmControl : MonoBehaviour
{
    Rig rig;
    [SerializeField] float differenceThreshold = 0.4f;
    [Header("Left")]
    [SerializeField] GameObject leftArmConstraints;
    [SerializeField] GameObject leftTarget;
    [SerializeField] Transform leftShoulder;
    [SerializeField] Transform leftCarryTarget;
    [SerializeField] float leftDistanceToShoulder = 0.5f;
    
    Vector3 leftTargetStartPosition;
    TwoBoneIKConstraint leftArmIK;
    MultiRotationConstraint leftHandRotation;
    Vector3 leftClosestPoint;
    
    [Header("Right")]
    [SerializeField] GameObject rightArmConstraints;
    [SerializeField] GameObject rightTarget;
    [SerializeField] Transform rightShoulder;
    [SerializeField] Transform rightCarryTarget;
    [SerializeField] float rightDistanceToShoulder = 0.5f;
    
    Vector3 rightTargetStartPosition;
    TwoBoneIKConstraint rightArmIK;
    MultiRotationConstraint rightHandRotation;
    Vector3 rightClosestPoint;

    bool atWall;
    bool isAiming;

    void Awake()
    {
        rig = GetComponent<Rig>();
        rig.weight = 1;
        
        leftArmIK = leftArmConstraints.GetComponent<TwoBoneIKConstraint>();
        leftHandRotation = leftArmConstraints.GetComponent<MultiRotationConstraint>();
        leftArmIK.weight = 0;
        leftHandRotation.weight = 0;
        leftTargetStartPosition = leftTarget.transform.position;
        
        rightArmIK = rightArmConstraints.GetComponent<TwoBoneIKConstraint>();
        rightHandRotation = rightArmConstraints.GetComponent<MultiRotationConstraint>();
        rightArmIK.weight = 0;
        rightHandRotation.weight = 0;
        rightTargetStartPosition = rightTarget.transform.position;
    }

    void OnEnable()
    {
        PlayerActions.aimStatus += AimStatus;
    }

    void OnDisable()
    {
        PlayerActions.aimStatus -= AimStatus;
    }

    void AimStatus(bool aimStatus)
    {
        isAiming = aimStatus;
    }

    void FixedUpdate() // To match with physics updates
    {
        if (isAiming)
        {
            rig.weight = 0;
            leftTarget.transform.position = leftTargetStartPosition;
            rightTarget.transform.position = rightTargetStartPosition;
            return;
        }
        if (PlayerActions.Instance.carriesPainting)
        {
            rig.weight = 1;
            SetTargetsToPainting();
            return;
        }
        if (atWall)
        {
            SetTargetsToWall();
        }
    }

    void SetTargetsToWall()
    {
        var leftDistance = Vector3.Distance(leftShoulder.position, leftClosestPoint);
        var rightDistance = Vector3.Distance(rightShoulder.position, rightClosestPoint);

        if (Mathf.Abs(leftDistance - rightDistance) < differenceThreshold && leftDistance < leftDistanceToShoulder && rightDistance < rightDistanceToShoulder)
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
            return;
        }
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
    }

    void SetTargetsToPainting()
    {
        if (leftArmIK != null) 
        {
            leftArmIK.weight = 1;
            leftHandRotation.weight = 1;
            leftTarget.transform.position = leftCarryTarget.position;
            leftTarget.transform.localRotation = Quaternion.Euler(new Vector3(0,100,0));
        }
        if (rightArmIK != null) 
        {
            rightArmIK.weight = 1;
            rightHandRotation.weight = 1;
            rightTarget.transform.position = rightCarryTarget.position;
            rightTarget.transform.localRotation = Quaternion.Euler(new Vector3(0, -50, 0));
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Climbable") && !PlayerActions.Instance.carriesPainting)
        {
            atWall = true;
            leftClosestPoint = other.ClosestPoint(leftShoulder.position);
            rightClosestPoint = other.ClosestPoint(rightShoulder.position);
        }
        if (!other.CompareTag("Climbable") && !PlayerActions.Instance.carriesPainting)
        {
            atWall = false;
            leftArmIK.weight = 0;
            leftHandRotation.weight = 0;
            rightArmIK.weight = 0;
            rightHandRotation.weight = 0;
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
