using UnityEngine;

public class PaintingDropPoint : Pickup
{
    [Header("PaintingDropPoint")]
    [SerializeField] GameObject paintingDeliveryPosition;

    GameObject paintingPrefab;

    protected override void Awake()
    {
        canBepickedUp = false;
        base.Awake();
    }

    void OnEnable()
    {
        StealPainting.sendPaintingPrefab += GetPainting;
    }

    void OnDisable()
    {
        StealPainting.sendPaintingPrefab -= GetPainting;
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (PlayerActions.Instance.carriesPainting)
        {
            canBepickedUp = true;
            base.OnTriggerEnter(other);
        }
        
    }
    
    protected override void OnTriggerStay(Collider other)
    {
        if (!PlayerActions.Instance.carriesPainting) return;
        if (PlayerActions.Instance.canEscape)
        {
            Debug.Log("Player escaped with the painting(s)!");
            return;
        }
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
        PlacePainting();
        PlayerActions.OnPaintingDelivered();
        base.ActivatePickup(other);
    }

    void GetPainting(GameObject painting)
    {
        paintingPrefab = painting;
    }

    void PlacePainting()
    {
        GameObject painting = Instantiate(paintingPrefab);
        for (int i = 0; i < paintingDeliveryPosition.transform.childCount; i++)
        {
            if (paintingDeliveryPosition.transform.GetChild(i).childCount == 0)
            {
                painting.transform.SetParent(paintingDeliveryPosition.transform.GetChild(i));
                painting.GetComponent<BoxCollider>().excludeLayers = LayerMask.GetMask();
                painting.transform.localScale = Vector3.one;
                painting.transform.localPosition = Vector3.zero;
                painting.transform.localRotation = Quaternion.identity;
                break;
            }
        }
    }
}
