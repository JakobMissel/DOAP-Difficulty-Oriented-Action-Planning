using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ThrowUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI throwAmmunition;
    [SerializeField] Image throwImage;
    [SerializeField] RawImage throwRawImage;
    Sprite defaultSprite;

    void Awake()
    {
        throwAmmunition.text = "";
        defaultSprite = throwImage.sprite;
        throwRawImage.texture = null;
    }

    void OnEnable()
    {
        PlayerActions.ammoUpdate += UpdateAmmo;
        PlayerActions.spriteUpdate += UpdateImage;
    }

    void OnDisable()
    {
        PlayerActions.ammoUpdate -= UpdateAmmo;
        PlayerActions.spriteUpdate -= UpdateImage;
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

    void UpdateImage(object sprite)
    {
        if (sprite == null)
        {
            throwRawImage.color = Color.clear;
            throwImage.color = Color.white;
            throwImage.sprite = defaultSprite;
            return;
        }
        if (sprite is Sprite)
        {
            throwRawImage.color = Color.clear;
            throwImage.color = Color.white;
            throwImage.sprite = sprite as Sprite;

        }
        else if (sprite is Texture)
        {
            throwImage.color = Color.clear;
            throwRawImage.color = Color.white;
            throwRawImage.texture = sprite as Texture;
        }
    }
}
