using UnityEngine;

public class PaintingShadowBehaviour : MonoBehaviour
{
    [SerializeField] private GameObject mapCross;
    private string whatPaintingAmI = string.Empty;
    private string lastPaintingCarried = string.Empty;

    public void AssignPainting(string whichPainting)
    {
        whatPaintingAmI = whichPainting;
    }

    private void OnEnable()
    {
        PlayerActions.stealItem += CarryingNewPainting;
        PlayerActions.paintingDelivered += PaintingDelivered;
    }

    private void OnDisable()
    {
        PlayerActions.stealItem -= CarryingNewPainting;
        PlayerActions.paintingDelivered -= PaintingDelivered;
    }

    private void CarryingNewPainting(StealablePickup thingStolen, bool coolBool)
    {
        lastPaintingCarried = thingStolen.painterName;
    }

    private void PaintingDelivered()
    {
        if (whatPaintingAmI == lastPaintingCarried)
        {
            mapCross.SetActive(true);
            Destroy(this);
        }
    }
}
