using UnityEngine;

public class ThrowablePickup : Pickup
{
    [Header("Throwable")]
    [SerializeField] GameObject thrownObjectPrefab;
    [SerializeField] int ammoCount = 5;
    [SerializeField] float noiseRadius = 5f;

    protected override void Awake()
    {
        string newDisplayName = $"{ammoCount}x {displayName}";
        if(ammoCount > 1)
        {
            newDisplayName += "s";
        }
        displayName = newDisplayName;
        thrownObjectPrefab.GetComponent<ThrownObject>().noiseRadius = noiseRadius;
        base.Awake();
    }

    protected override void ActivatePickup(Collider other)
    {
        other.GetComponent<PlayerThrow>().AddThrowable(thrownObjectPrefab, ammoCount);
        base.ActivatePickup(other);
    }
}
