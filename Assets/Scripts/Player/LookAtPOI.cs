using UnityEngine;
using UnityEngine.Animations.Rigging;

public class LookAtPOI : MonoBehaviour
{
    [SerializeField] bool lookAtInteract;
    [SerializeField] float lookDistance = 10f;
    [SerializeField] GameObject headTrackerSourceObject;
    [SerializeField] Rig headTracker;
    GameObject hitArea;
    float distance;
    bool isAiming;
    GameObject[] pois;

    void Awake()
    {
        pois = GameObject.FindGameObjectsWithTag("POI");
    }

    void OnEnable()
    {
        PlayerActions.aimStatus += AimStatus;
        PlayerActions.sethitArea += SetHitArea;
    }

    void OnDisable()
    {
        PlayerActions.aimStatus -= AimStatus;
        PlayerActions.sethitArea -= SetHitArea;
    }


    void AimStatus(bool aimStatus)
    {
        isAiming = aimStatus;
    }

    void SetHitArea(GameObject hitArea)
    {
        this.hitArea = hitArea;
        print(this.hitArea.name);
    }

    void Update()
    {
        LookAtTarget(ClosestPOI());
    }

    GameObject ClosestPOI()
    {
        GameObject closestPOI = null;
        float minDistance = lookDistance;
        foreach (var poi in pois)
        {
            if (!poi) continue;
            float distance = Vector3.Distance(transform.position, poi.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                this.distance = distance;
                closestPOI = poi;
            }
        }
        return closestPOI;
    }

    void LookAtTarget(GameObject poi)
    {
        if(poi && !isAiming)
        {
            headTrackerSourceObject.transform.position = poi.transform.position;
            headTracker.weight = Mathf.Lerp(0, 1, Mathf.Clamp01(1 - (distance / lookDistance)));
        }
        if(isAiming)
        {
            headTrackerSourceObject.transform.position = hitArea.transform.position;
            headTracker.weight = 1;
        }
    }
}
