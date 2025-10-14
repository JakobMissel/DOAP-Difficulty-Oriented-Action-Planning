using UnityEngine;

public class StealablePickup : Pickup
{
    protected override void ActivatePickup(Collider other)
    {
        base.ActivatePickup(other);
    }
}
