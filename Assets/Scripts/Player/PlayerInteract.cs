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
        ResolveUiReferencesIfMissing();

        if (interactButton != null)
            interactButton.SetActive(false);
        else
            Debug.LogWarning("[PlayerInteract] 'interactButton' is not assigned and could not be auto-found. Interact UI will be hidden.", this);
    }

    void OnEnable()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        var actions = playerInput != null ? playerInput.actions : null;
        var interactAction = actions != null ? actions["Interact"] : null;

        if (interactAction == null)
        {
            Debug.LogWarning("[PlayerInteract] No 'Interact' action found on PlayerInput. Interact input will not be handled.", this);
        }
        else
        {
            interactAction.performed += Interact;
            interactAction.canceled += Interact;
        }

        PlayerActions.addPickup += AddPickup;
        PlayerActions.removePickup += RemovePickup;
    }

    void OnDisable()
    {
        var actions = playerInput != null ? playerInput.actions : null;
        var interactAction = actions != null ? actions["Interact"] : null;

        if (interactAction != null)
        {
            interactAction.performed -= Interact;
            interactAction.canceled -= Interact;
        }
        
        PlayerActions.addPickup -= AddPickup;
        PlayerActions.removePickup -= RemovePickup;
    }

    void Update()
    {
        var closest = ClosestPickup();
        ShowInteractGraphic(closest);
        FillInteractGraphic(closest);
    }

    void Interact(InputAction.CallbackContext ctx)
    {
        ResetGraphics();

        var closest = ClosestPickup();
        if (!closest || !pickups.Contains(closest)) return;
        Pickup pickup = closest.GetComponent<Pickup>();

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
        currentDistance = float.MaxValue;

        foreach (var pickup in pickups)
        {
            if (!pickup) continue;
            float distance = Vector3.Distance(transform.position, pickup.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                currentDistance = distance;
                closestPickup = pickup;
            }
        }
        return closestPickup;
    }


    void ShowInteractGraphic(Pickup pickup)
    {
        if (interactButton != null)
            interactButton.SetActive(pickup);

        if (!pickup || displayName == null)
            return;

        displayName.text = pickup.DisplayName;
    }

    void FillInteractGraphic(Pickup pickup)
    {
        if (interactImage == null)
            return;

        if (pickup)
            interactImage.fillAmount = Mathf.Lerp(0, 1, pickup.HoldTime / pickup.HoldDuration);
        else
            interactImage.fillAmount = 0;
    }

    void ResetGraphics()
    {
        if (interactImage)
            interactImage.fillAmount = 0;
        if (displayName)
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

    private void ResolveUiReferencesIfMissing()
    {
        // Try to auto-find button by common name if not assigned
        if (interactButton == null)
        {
            var foundByName = GameObject.Find("InteractButton");
            if (foundByName != null)
                interactButton = foundByName;
            else
            {
                // Try to find any Button in children with a similar name
                foreach (var btn in GetComponentsInChildren<Button>(true))
                {
                    if (btn.name.ToLower().Contains("interact"))
                    {
                        interactButton = btn.gameObject;
                        break;
                    }
                }
            }
        }

        // Try to auto-find display name if not assigned
        if (displayName == null)
        {
            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.name.ToLower().Contains("interact") || tmp.name.ToLower().Contains("name"))
                {
                    displayName = tmp;
                    break;
                }
            }
            // Fallback to any TMP if none matched
            if (displayName == null)
            {
                displayName = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        // Try to auto-find interact image if not assigned
        if (interactImage == null)
        {
            if (interactButton != null)
            {
                interactImage = interactButton.GetComponentInChildren<Image>(true);
            }
            if (interactImage == null)
            {
                // Search for an image named like a fill
                foreach (var img in GetComponentsInChildren<Image>(true))
                {
                    if (img.name.ToLower().Contains("interact") || img.name.ToLower().Contains("fill"))
                    {
                        interactImage = img;
                        break;
                    }
                }
            }
        }
    }
}
