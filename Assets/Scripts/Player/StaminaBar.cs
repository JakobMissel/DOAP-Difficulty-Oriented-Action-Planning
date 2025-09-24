using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    Image image;
    [SerializeField] PlayerClimb playerClimb;
    [SerializeField] float fateDuration = 1f;
    float time;
    Color color;
    Color transparentColor;
    void Awake()
    {
        image = GetComponent<Image>();
        color = image.color;
        transparentColor = new Color(color.r, color.g, color.b, 0);
        image.color = transparentColor;
    }

    void Update()
    {
        UpdateStaminaBar();
    }

    void UpdateStaminaBar()
    {
        if (playerClimb.onWall)
        {
            time = 0;
            image.color = color;
        }
        else if (!playerClimb.onWall && playerClimb.currentStamina >= playerClimb.maxStamina)
        {
            time += Time.deltaTime / fateDuration;
            image.color = Color.Lerp(image.color, transparentColor, time);
        }
        image.fillAmount = playerClimb.currentStamina / playerClimb.maxStamina;
    }
}
