using UnityEngine;

public class StealablePickup : Pickup
{
    protected override void ActivatePickup(Collider other)
    {
        print($"Player yoinked {gameObject.name}!");
        base.ActivatePickup(other);
    }
}
