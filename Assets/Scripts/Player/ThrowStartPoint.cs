using UnityEngine;

public class ThrowStartPoint : MonoBehaviour
{
    bool available = true;
    [SerializeField] string[] wallLayer = new[] {"Wall"};

    public bool IsAvailable()
    {
        return available;
    }

    void OnTriggerStay(Collider other)
    {
        foreach (var layer in wallLayer)
        {
            if (other.gameObject.layer.ToString().ToLower() == LayerMask.NameToLayer(layer).ToString().ToLower())
            {
                available = false;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        foreach (var layer in wallLayer)
        {
            if (other.gameObject.layer.ToString().ToLower() == LayerMask.NameToLayer(layer).ToString().ToLower())
            {
                available = true;
            }
        }
    }
}
