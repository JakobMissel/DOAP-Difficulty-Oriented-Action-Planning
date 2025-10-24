using UnityEngine;

public class StealablePickup : Pickup
{
    protected override void ActivatePickup(Collider other)
    {
        PlayerActions.OnStealItem(this);
        base.ActivatePickup(other);
    }
}
