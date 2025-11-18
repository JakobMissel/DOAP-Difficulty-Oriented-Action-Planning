using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerSneak : MonoBehaviour
{
    PlayerInput playerInput;
    [Header("UI")]
    [SerializeField] Image vignette;
    [SerializeField] float fateDuration = 1.0f;
    bool isSneaking;
    float time;
    Color startColor;
    Color transparentColor;
    bool isInputBlocked;

    void Awake()
    {
        // Try to find PlayerInput on this or parent object
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
            playerInput = GetComponentInParent<PlayerInput>();

        if (vignette != null)
        {
            startColor = vignette.color;
            transparentColor = new Color(startColor.r, startColor.g, startColor.b, 0);
            vignette.color = transparentColor;
            if (vignette.gameObject.activeSelf)
                vignette.gameObject.SetActive(false); // ensure hidden at start
        }
        else
        {
            Debug.LogWarning("[PlayerSneak] Vignette Image is not assigned. Sneak UI feedback will be disabled.", this);
        }
    }

    void OnEnable()
    {
        if (playerInput == null)
        {
            Debug.LogWarning("[PlayerSneak] No PlayerInput found. Sneak input will not be handled.", this);
            return;
        }

        if (playerInput.actions == null)
        {
            Debug.LogWarning("[PlayerSneak] PlayerInput has no actions asset assigned.", this);
            return;
        }

        var sneakAction = playerInput.actions["Sneak"];
        if (sneakAction == null)
        {
            Debug.LogWarning("[PlayerSneak] No 'Sneak' action found in the actions asset.", this);
            return;
        }

        sneakAction.performed += OnSneak;
        sneakAction.canceled += OnSneak;
        PlayerActions.gameOverState += OnGameOverState;
    }

    void OnDisable()
    {
        if (playerInput?.actions == null)
            return;

        var sneakAction = playerInput.actions["Sneak"];
        if (sneakAction == null)
            return;

        sneakAction.performed -= OnSneak;
        sneakAction.canceled -= OnSneak;
        PlayerActions.gameOverState -= OnGameOverState;
    }

    void OnSneak(InputAction.CallbackContext context)
    {
        if (isInputBlocked)
            return;
        isSneaking = context.ReadValueAsButton();
        PlayerActions.OnSneakStatus(isSneaking);
    }

    void Update()
    {
        Sneak();
    }

    void Sneak()
    {
        if (vignette == null)
            return;

        if (isSneaking)
        {
            // Enable on first frame of sneak and start fading in
            if (!vignette.gameObject.activeSelf)
                vignette.gameObject.SetActive(true);

            if (isInputBlocked)
            {
                isSneaking = false;
                PlayerActions.OnSneakStatus(false);
                vignette.gameObject.SetActive(false);
                return;
            }

            time += Time.deltaTime / fateDuration;
            vignette.color = Color.Lerp(vignette.color, startColor, time);
        }
        else
        {
            // Fade out instantly and hide to avoid always-on visuals
            time = 0;
            vignette.color = transparentColor;
            if (vignette.gameObject.activeSelf)
                vignette.gameObject.SetActive(false);
        }
    }

    void OnGameOverState(bool isBlocked)
    {
        isInputBlocked = isBlocked;
        if (isBlocked && isSneaking)
        {
            isSneaking = false;
            PlayerActions.OnSneakStatus(false);
        }
    }
}
