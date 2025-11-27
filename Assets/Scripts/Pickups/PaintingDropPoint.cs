using System;
using UnityEngine;

public class PaintingDropPoint : Pickup
{
    [Header("PaintingDropPoint")]
    [SerializeField] GameObject paintingDeliveryPosition;
    [SerializeField] GameObject visualZone;

    GameObject paintingPrefab;
    bool escaped = false;

    public static Action placePainting;
    public static void OnPlacePainting() => placePainting?.Invoke();

    public static Action canDropOffPainting;
    public static void OnCanDropOffPainting() => canDropOffPainting?.Invoke();

    protected override void Awake()
    {
        canBepickedUp = false;
        base.Awake();
    }

    protected override void OnEnable()
    {
        StealPainting.sendPaintingPrefab += GetPainting;
        placePainting += PlacePainting;
        CheckpointManager.loadCheckpoint += () => canBepickedUp = false;
        canDropOffPainting += () => canBepickedUp = true;
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        StealPainting.sendPaintingPrefab -= GetPainting;
        placePainting -= PlacePainting;
        CheckpointManager.loadCheckpoint -= () => canBepickedUp = false;
        canDropOffPainting -= () => canBepickedUp = true;
        base.OnDisable();
    }

    protected override void OnTriggerEnter(Collider other)
    { 
        if (PlayerActions.Instance.carriesPainting && ObjectivesManager.Instance.completedTutorial)
        {
            canBepickedUp = true;
            base.OnTriggerEnter(other);
            return;
        }
    }

    protected override void OnTriggerStay(Collider other)
    {
        // Allow drop-off during tutorial
        if (!ObjectivesManager.Instance.completedTutorial && canBepickedUp)
        {
            canBepickedUp = true;
            base.OnTriggerEnter(other);
            base.OnTriggerStay(other);
            return;
        }

        if (PlayerActions.Instance.canEscape && !escaped)
        {
            escaped = true;            
            PlayerActions.OnPlayerEscaped();

            return;
        }
        if (!PlayerActions.Instance.carriesPainting) return;
        base.OnTriggerStay(other);
    }

    protected override void OnTriggerExit(Collider other)
    {
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
        canBepickedUp = false;
    }

    void Update()
    {
        visualZone.SetActive(PlayerActions.Instance.carriesPainting);
    }
}
