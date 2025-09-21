using UnityEngine;

public class NoiseArea : MonoBehaviour
{
    [SerializeField] public Transform noiseArea;
    [SerializeField] public Transform noiseCenter;
    [HideInInspector] public float noiseRadius = 5f;

    void Awake()
    {
        noiseArea.localScale = new Vector3(noiseRadius, noiseRadius, noiseRadius);
    }
}
