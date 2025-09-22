using UnityEngine;
using UnityEngine.UI;

public class ThrownObject : MonoBehaviour
{
    [SerializeField] GameObject noiseAreaPrefab;
    [SerializeField] public Sprite thrownObjectImage;
    [SerializeField] float noiseRadius = 5f;
    [SerializeField] bool destroyOnImpact = false;
    bool hasCollided = false;

    void OnCollisionEnter(Collision collision)
    {
        // On first collision create a noise area and destroy the object if destroyOnImpact is true.
        if (hasCollided) return;
        hasCollided = true;
        GameObject noiseArea = null;
        noiseArea = Instantiate(noiseAreaPrefab, transform.position, Quaternion.identity);
        noiseArea.GetComponent<NoiseArea>().SetScale(noiseRadius);
        if(!destroyOnImpact || !gameObject) return;
        Destroy(gameObject);
    }
}
