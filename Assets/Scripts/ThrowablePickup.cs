using UnityEngine;

public class ThrowablePickup : Pickup
{
    [Header("Throwable")]
    [SerializeField] GameObject thrownObjectPrefab;
    [SerializeField] int ammoCount = 5;

    protected override void ActivatePickup(Collider other)
    {
        other.GetComponent<PlayerThrow>().AddThrowable(thrownObjectPrefab, ammoCount);
        base.ActivatePickup(other);
    }
}
