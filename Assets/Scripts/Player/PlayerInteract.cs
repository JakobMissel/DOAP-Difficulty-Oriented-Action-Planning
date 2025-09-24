using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    PlayerInput playerInput;
    [SerializeField] float interactDistance = 4f;
    [SerializeField] GameObject interactButton;
    List<Pickup> pickups = new();
    bool buttonHeld;

    public static Action<InputAction.CallbackContext> playerInteract;
    public static void OnPlayerInteract(InputAction.CallbackContext cxt) => playerInteract?.Invoke(cxt);

    public static Action<Pickup> addPickup;
    public static void OnAddPickup(Pickup pickup) => addPickup?.Invoke(pickup);

    public static Action<Pickup> removePickup;
    public static void OnRemovePickup(Pickup pickup) => removePickup?.Invoke(pickup);

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        interactButton.SetActive(false);
    }
    void OnEnable()
    {
        playerInput.actions["Interact"].performed += Interact;
        playerInput.actions["Interact"].canceled += Interact;
        PlayerInteract.addPickup += AddPickup;
        PlayerInteract.removePickup += RemovePickup;
    }

    void OnDisable()
    {
        playerInput.actions["Interact"].performed -= Interact;
        playerInput.actions["Interact"].canceled -= Interact;
        PlayerInteract.addPickup -= AddPickup;
        PlayerInteract.removePickup -= RemovePickup;
    }

    void Update()
    {
        ShowInteractButton(ClosestPickup());
        if (!ClosestPickup()) return;
    }

    void Interact(InputAction.CallbackContext ctx)
    {
        OnPlayerInteract(ctx);
        if (!ClosestPickup() || !pickups.Contains(ClosestPickup())) return;
        Pickup pickup = ClosestPickup().GetComponent<Pickup>();
        if (pickup.holdRequired)
        {
            pickup.buttonHeld = ctx.ReadValueAsButton();
        }
        else
        {
            pickup.buttonPressed = ctx.ReadValueAsButton();
        }
    }

    Pickup ClosestPickup()
    {
        Pickup closestPickup = null;
        float minDistance = interactDistance;
        foreach (var pickup in pickups)
        {
            if (!pickup) continue;
            float distance = Vector3.Distance(transform.position, pickup.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPickup = pickup;
            }
        }
        return closestPickup;
    }

    void ShowInteractButton(bool pickupAvailable)
    {
        interactButton.SetActive(pickupAvailable);
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
