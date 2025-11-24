using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ThrowUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI throwAmmunition;
    [SerializeField] Material[] coinMaps;
    [SerializeField] MeshRenderer renderCoin;
    [SerializeField] RawImage throwRawImage;

    void Awake()
    {
        throwAmmunition.text = "";
        UpdateImage(null);
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
        throwAmmunition.text = ammo.ToString();
    }

    void UpdateImage(object sprite)
    {
        if (sprite is not Texture)
        {
            throwRawImage.color = new Color(1, 1, 1, 0.4f);
            renderCoin.material = coinMaps[0];
            return;
        }
        else if (sprite is Texture)
        {
            throwRawImage.color = new Color(1, 1, 1, 1);
            renderCoin.material = coinMaps[1];
        }
    }
}
