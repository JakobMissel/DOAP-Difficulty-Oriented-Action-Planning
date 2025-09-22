using UnityEngine;

public class NoiseArea : MonoBehaviour
{
    [SerializeField] public Transform noiseArea;
    [SerializeField] public Transform noiseCenter;
    [HideInInspector] public float noiseRadius;

    public void SetScale(float scale)
    {
        var newScale = noiseArea.localScale;
        newScale.Set(scale, scale, scale);
        noiseArea.localScale = newScale;
    }
}
