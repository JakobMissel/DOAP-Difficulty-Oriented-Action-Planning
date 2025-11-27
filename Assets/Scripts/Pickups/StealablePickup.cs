using UnityEngine;

public class StealablePickup : Pickup
{
    [Header("Stealable Info")]
    public string paintingName;
    public string painterName;
    public string value;
    public bool tutorialPainting = false;
    public GameObject paintingCarryPrefab;
    bool isPaintingNew = true;

    [Header("Glitter")]
    [SerializeField] [Tooltip("**IMPORTANT** This should ONLY be assigned on the painting in the tutorial area!")] GameObject glitter;
    protected override void Awake()
    {
        displayName = $"Steal painting \n by \n{painterName}";
        base.Awake();

        if(glitter == null) return;
        glitter.SetActive(false);
    }

    public void ShowGlitter()
    {
        if(glitter == null) return;
        glitter.SetActive(true);
    }

    protected override void OnTriggerEnter(Collider other)
    { 
        if (PlayerActions.Instance.carriesPainting || !ObjectivesManager.Instance.completedTutorial && !tutorialPainting) return;
        base.OnTriggerEnter(other);
    }

    protected override void OnTriggerStay(Collider other)
    {
        if (PlayerActions.Instance.carriesPainting || !ObjectivesManager.Instance.completedTutorial && !tutorialPainting) return;
        base.OnTriggerStay(other);
    }

    protected override void OnTriggerExit(Collider other)
    {
        if (PlayerActions.Instance.carriesPainting || !ObjectivesManager.Instance.completedTutorial && !tutorialPainting) return;
        base.OnTriggerExit(other);
    }

    protected override void ActivatePickup(Collider other)
    {
        if (PlayerActions.Instance.carriesPainting || !ObjectivesManager.Instance.completedTutorial && !tutorialPainting) return;
        PlayerActions.Instance.carriesPainting = true;
        PlayerActions.OnStealItem(this, isPaintingNew);
        isPaintingNew = false;
        base.ActivatePickup(other);
    }
}
