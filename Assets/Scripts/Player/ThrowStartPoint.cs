using UnityEngine;

public class ThrowStartPoint : MonoBehaviour
{
    bool available = true;
    [SerializeField] LayerMask wallLayer;

    public bool IsAvailable()
    {
        return available;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & wallLayer) != 0)
        {
            Debug.LogWarning($"Throw point blocked by wall. {other.name}");
            available = false;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & wallLayer) != 0)
        {
            Debug.LogWarning($"Throw point blocked by wall. {other.name}");
            available = false;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & wallLayer) != 0)
        {
            available = true;
        }
    }
}
