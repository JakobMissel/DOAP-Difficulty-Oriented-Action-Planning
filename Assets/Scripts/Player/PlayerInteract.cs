using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteract : MonoBehaviour
{
    PlayerInput playerInput;
    [Header("Distance")]
    [SerializeField] float interactDistance = 4f;
    float currentDistance;
    [Header("UI")]
    [SerializeField] TextMeshProUGUI displayName;
    [SerializeField] GameObject interactButton;
    [SerializeField] Image interactImage;
    
    List<Pickup> pickups = new();

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        interactButton.SetActive(false);
    }
    void OnEnable()
    {
        playerInput.actions["Interact"].performed += Interact;
        playerInput.actions["Interact"].canceled += Interact;
        PlayerActions.addPickup += AddPickup;
        PlayerActions.removePickup += RemovePickup;
    }

    void OnDisable()
    {
        playerInput.actions["Interact"].performed -= Interact;
        playerInput.actions["Interact"].canceled -= Interact;
        PlayerActions.addPickup -= AddPickup;
        PlayerActions.removePickup -= RemovePickup;
    }

    void Update()
    {
        ShowInteractGraphic(ClosestPickup());
        FillInteractGraphic(ClosestPickup());
    }

    void Interact(InputAction.CallbackContext ctx)
    {
        ResetGraphics();

        if (!ClosestPickup() || !pickups.Contains(ClosestPickup())) return;
        Pickup pickup = ClosestPickup().GetComponent<Pickup>();

        PlayerActions.OnPlayerInteract(ctx);

        // Send button status to closest pickup
        if (pickup.HoldRequired)
            pickup.buttonHeld = ctx.ReadValueAsButton();
        else
            pickup.buttonPressed = ctx.ReadValueAsButton();
    }

    Pickup ClosestPickup()
    {
        Pickup closestPickup = null;
        float minDistance = interactDistance;
        foreach (var pickup in pickups)
        {
            if (!pickup) continue;
            float distance = Vector3.Distance(transform.position, pickup.transform.position);
            if (currentDistance < minDistance)
            {
                minDistance = currentDistance;
                closestPickup = pickup;
            }
        }
        return closestPickup;
    }


    void ShowInteractGraphic(Pickup pickup)
    {
        if (!interactButton) return;
        interactButton.SetActive(pickup);
        if (!pickup || !displayName) return;
        displayName.text = pickup.DisplayName;
    }

    void FillInteractGraphic(Pickup pickup)
    {
        if (!interactImage) return;
        if(pickup)
            interactImage.fillAmount = Mathf.Lerp(0, 1, pickup.HoldTime/pickup.HoldDuration);
        else
            interactImage.fillAmount = 0;
    }

    void ResetGraphics()
    {
        if(interactImage)
            interactImage.fillAmount = 0;
        if(displayName)
            displayName.text = "";
    }

    public void AddPickup(Pickup pickup)
    {
        if (pickups.Contains(pickup)) return;
        pickups.Add(pickup);
    }

    public void RemovePickup(Pickup pickup)
    {
        if (!pickups.Contains(pickup)) return;
        pickups.Remove(pickup);
    }
}
