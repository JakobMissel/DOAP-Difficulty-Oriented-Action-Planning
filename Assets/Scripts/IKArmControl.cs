using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IKArmControl : MonoBehaviour
{
    Rig rig;
    [Header("Left")]
    [SerializeField] GameObject leftTarget;
    [Header("Right")]
    [SerializeField] GameObject rightTarget;

    Vector3 closestPoint;

    void Awake()
    {
        rig = GetComponent<Rig>();
    }

    void Update()
    {
        SetTargets();
    }

    void SetTargets()
    {
        leftTarget.transform.position = closestPoint;
        rightTarget.transform.position = closestPoint;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Climbable"))
        {
            closestPoint = other.ClosestPoint(transform.position).normalized;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Climbable"))
        {
            closestPoint = other.ClosestPoint(transform.position).normalized;
            rig.weight = 1;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Climbable"))
        {
            rig.weight = 0;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(leftTarget.transform.position, 0.1f);
        Gizmos.DrawWireSphere(rightTarget.transform.position, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(closestPoint, 0.1f);
    }
}
