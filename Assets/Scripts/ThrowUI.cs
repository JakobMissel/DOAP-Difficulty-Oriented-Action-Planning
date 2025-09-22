using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ThrowUI : MonoBehaviour
{
    PlayerThrow playerThrow;
    [SerializeField] TextMeshProUGUI throwAmmunition;
    [SerializeField] Image throwImage;

    void Awake()
    {
        playerThrow = GameObject.FindWithTag("Player").GetComponent<PlayerThrow>();
    }

    void Start()
    {
            
    }

    void Update()
    {
        throwAmmunition.text = playerThrow.ammoCount.ToString();
    }
}
