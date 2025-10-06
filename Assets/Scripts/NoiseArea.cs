using UnityEngine;

public class NoiseArea : MonoBehaviour
{
    [SerializeField] public Transform noiseArea;
    [SerializeField] public Transform noiseCenter;
    [HideInInspector] public float noiseRadius;
    
    private bool hasTriggered = false;

    
    private void Start()
    {
        // Trigger noise when this object is created/spawned
        TriggerNoise();
    }
    
    public void SetScale(float scale)
    {
        var newScale = noiseArea.localScale;
        newScale.Set(scale, scale, scale);
        noiseArea.localScale = newScale;
    }
    
    // Call this method to trigger the noise and notify agents
    public void TriggerNoise()
    {
        if (hasTriggered)
            return;

        hasTriggered = true;

        Vector3 noisePosition = noiseCenter != null ? noiseCenter.position : transform.position;
        
        Debug.Log($"[NoiseArea] Noise triggered at {noisePosition}");

        // Find all agents with BrainBehaviour and notify them
        var agents = FindObjectsOfType<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();
        
        foreach (var agent in agents)
        {
            agent.OnNoiseHeard(noisePosition);
        }
    }
}
