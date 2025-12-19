using TMPro;
using UnityEngine;

public class GoodBreadPercentage : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text;
    
    int currentPercentage;

    public void AddPercent(int percent)
    {
        currentPercentage = percent;
        if(currentPercentage > 100)
        {
            currentPercentage = 100;
        }
        text.text = currentPercentage.ToString() + "%";
    }
}
