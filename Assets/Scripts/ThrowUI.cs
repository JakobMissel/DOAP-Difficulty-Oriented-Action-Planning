using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ThrowUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI throwAmmunition;
    [SerializeField] Image throwImage;
    Sprite defaultSprite;

    void Awake()
    {
        throwAmmunition.text = "";
        defaultSprite = throwImage.sprite;
    }

    void OnEnable()
    {
        PlayerThrow.ammoUpdate += UpdateAmmo;
        PlayerThrow.spriteUpdate += UpdateSprite;
    }

    void OnDisable()
    {
        PlayerThrow.ammoUpdate -= UpdateAmmo;
        PlayerThrow.spriteUpdate -= UpdateSprite;
    }

    void UpdateAmmo(int ammo)
    {
        if (ammo == 0)
        {
            throwAmmunition.text = "";
            return;
        }
        throwAmmunition.text = ammo.ToString();
    }

    void UpdateSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            throwImage.sprite = defaultSprite;
            return;
        }
        throwImage.sprite = sprite;
    }
}
