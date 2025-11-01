using UnityEngine;

public class PaintingDropPoint : Pickup
{
    protected override void Awake()
    {
        canBepickedUp = false;
        base.Awake();
    }
    
    protected override void OnTriggerEnter(Collider other)
    {
        if (PlayerActions.Instance.carriesPainting)
        {
            canBepickedUp = true;
            base.OnTriggerEnter(other);
        }
        if (PlayerActions.Instance.canEscape)
        {
            Debug.Log("Player escaped with the painting(s)!");
        }
    }
    
    protected override void OnTriggerStay(Collider other)
    {
        if (!PlayerActions.Instance.carriesPainting) return;
        base.OnTriggerStay(other);
    }

    protected override void OnTriggerExit(Collider other)
    {
        if (!PlayerActions.Instance.carriesPainting) return;
        base.OnTriggerExit(other);
    }

    protected override void ActivatePickup(Collider other)
    {
        if (!PlayerActions.Instance.carriesPainting) return;
        PlayerActions.Instance.carriesPainting = false;
        PlayerActions.OnPaintingDelivered();
        base.ActivatePickup(other);
    }
}
