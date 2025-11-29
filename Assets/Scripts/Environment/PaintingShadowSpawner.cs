using UnityEngine;

public class PaintingShadowSpawner : MonoBehaviour
{
    [SerializeField] private GameObject shadowPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instantiate(shadowPrefab, transform.position, transform.rotation).GetComponent<PaintingShadowBehaviour>().AssignPainting(GetComponent<StealablePickup>().painterName);
        Destroy(this);
    }
}
