using UnityEngine;

public class ThrownObject : MonoBehaviour
{
    [SerializeField] GameObject noiseAreaPrefab;
    [SerializeField] public Sprite thrownObjectImage;
    [SerializeField] bool destroyOnImpact = false;
    [HideInInspector] public float noiseRadius = 5f;
    bool hasCollided = false;

    void OnCollisionEnter(Collision collision)
    {
        if (hasCollided) return;
        hasCollided = true;
        GameObject noiseArea = null;
        noiseArea = Instantiate(noiseAreaPrefab, transform.position, Quaternion.identity);
        noiseArea.GetComponent<NoiseArea>().SetScale(noiseRadius);
        if(!destroyOnImpact || !gameObject) return;
        Destroy(gameObject);
    }
}
