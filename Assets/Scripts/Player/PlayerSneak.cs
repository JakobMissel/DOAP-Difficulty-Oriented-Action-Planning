using System;
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

    public static Action<bool> sneakStatus;
    public static void OnSneakStatus(bool isSneaking) => sneakStatus?.Invoke(isSneaking);

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        startColor = vignette.color;
        transparentColor = new Color(startColor.r, startColor.g, startColor.b, 0);
        vignette.color = transparentColor;
    }

    void OnEnable()
    {
        playerInput.actions["Sneak"].performed += OnSneak;
        playerInput.actions["Sneak"].canceled += OnSneak;
    }

    void OnDisable()
    {
        playerInput.actions["Sneak"].performed -= OnSneak;
        playerInput.actions["Sneak"].canceled -= OnSneak;
    }

    void OnSneak(InputAction.CallbackContext context)
    {
        isSneaking = context.ReadValueAsButton();
        OnSneakStatus(isSneaking);
    }

    void Update()
    {
        Sneak();
    }

    void Sneak()
    {
        if (isSneaking)
        {
            time += Time.deltaTime / fateDuration;
            vignette.color = Color.Lerp(vignette.color, startColor, time);
        }
        else
        {
            time = 0;
            vignette.color = transparentColor;
        }
    }
}
