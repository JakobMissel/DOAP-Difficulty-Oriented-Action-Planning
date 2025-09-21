using UnityEngine;

public class ThrownObject : MonoBehaviour
{
    [SerializeField] GameObject noiseAreaPrefab;
    [SerializeField] float noiseRadius = 5f;
    bool hasCollided = false;

    void OnCollisionEnter(Collision collision)
    {
        if (hasCollided) return;
        hasCollided = true;
        GameObject noiseArea = Instantiate(noiseAreaPrefab, transform.position, Quaternion.identity);
        noiseArea.GetComponent<NoiseArea>().noiseRadius = noiseRadius;
    }
}
