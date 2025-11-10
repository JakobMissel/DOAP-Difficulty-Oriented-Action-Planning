using UnityEngine;

public class NoiseArea : MonoBehaviour
{
    [SerializeField] bool drawGizmos;
    [SerializeField] public Transform noiseArea;
    [SerializeField] public Transform noiseCenter;
    [SerializeField] public float noiseRadius;
    
    private bool hasTriggered = false;

    
    private void Start()
    {
        // Trigger noise when this object is created/spawned
        TriggerNoise();
    }
    
    public void SetScale(float scale)
    {
        noiseRadius = scale;
        var newScale = noiseArea.localScale;
        newScale.Set(scale, scale, scale);
        noiseArea.localScale = newScale;
    }

    // Call this method to trigger the noise and notify agents
    void TriggerNoise()
    {
        if (hasTriggered)
            return;

        hasTriggered = true;
    }

    void SendNoise(Collider other)
    {
        Vector3 noisePosition = noiseCenter != null ? noiseCenter.position : transform.position;

        Debug.Log($"[NoiseArea] Noise triggered at {noisePosition}");

        // Find all agents in NoiseArea, with BrainBehaviour, and notify them
        var agent = other.gameObject?.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();

        agent.OnNoiseHeard(noisePosition);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agent"))
        {
            SendNoise(other);
        }
    }

    void OnDrawGizmos()
    {
        if (noiseArea != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(noiseArea.position, noiseRadius);
        }
    }
}

