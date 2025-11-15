using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class NoiseArea : MonoBehaviour
{
    [SerializeField] bool drawGizmos;
    [SerializeField] public Transform noiseArea;
    [SerializeField] public Transform noiseCenter;
    [SerializeField] public float noiseRadius;
    [SerializeField] private float noiseDuration = 8f; // How long the noise persists
    [Header("Visual")] // The visual representation of the noise area
    [SerializeField] GameObject noiseVisual;
    [SerializeField] float expansionTime = 1f;
    float maxScale = 0.125f; // Maximum scale for the visual representation (size scales with the noise area since noiseVisual already is a child of noiseArea)

    private bool hasTriggered = false;
    private float spawnTime;

    
    private void Start()
    {
        spawnTime = Time.time;
        // Trigger noise when this object is created/spawned
        TriggerNoise();

        StartCoroutine(ExpandNoiseVisual());
        
        // Destroy this noise area after the duration
        Destroy(gameObject, noiseDuration);
    }
    
    public void SetScale(float scale)
    {
        noiseRadius = scale;
        var newScale = noiseArea.localScale;
        newScale.Set(scale, scale, scale);
        noiseArea.localScale = newScale;
    }

    IEnumerator ExpandNoiseVisual()
    {
        float elapsedTime = 0f;
        noiseVisual.transform.localScale = Vector3.zero;
        while (elapsedTime < expansionTime)
        {
            float scale = Mathf.Lerp(0f, maxScale, elapsedTime / expansionTime);
            noiseVisual.transform.localScale = new Vector3(scale, scale, scale);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        yield return null;
    }

    // Call this method to trigger the noise and notify agents
    void TriggerNoise()
    {
        if (hasTriggered)
            return;

        hasTriggered = true;
        
        Vector3 noisePosition = noiseCenter != null ? noiseCenter.position : transform.position;
        Debug.Log($"[NoiseArea] Noise triggered at {noisePosition} with radius {noiseRadius}");

        // Immediately broadcast to all guards within range instead of waiting for OnTriggerEnter
        BroadcastNoiseToNearbyGuards(noisePosition);
    }

    void BroadcastNoiseToNearbyGuards(Vector3 noisePosition)
    {
        // Find all active guard brains and notify those within range
        var allBrains = Assets.Scripts.GOAP.Behaviours.BrainBehaviour.GetActiveBrains();
        
        int guardsNotified = 0;
        foreach (var brain in allBrains)
        {
            if (brain == null || brain.gameObject == null)
                continue;
                
            float distance = Vector3.Distance(brain.transform.position, noisePosition);
            
            // Only notify guards within the noise radius
            if (distance <= noiseRadius)
            {
                brain.OnDistractionNoiseHeard(noisePosition, noiseRadius);
                guardsNotified++;
                Debug.Log($"[NoiseArea] Notified guard '{brain.name}' at distance {distance:F1}m");
            }
        }
        
        if (guardsNotified == 0)
        {
            Debug.Log($"[NoiseArea] No guards within radius {noiseRadius}m to notify");
        }
    }

    void SendNoise(Collider other)
    {
        Vector3 noisePosition = noiseCenter != null ? noiseCenter.position : transform.position;

        Debug.Log($"[NoiseArea] Additional guard entered noise area at {noisePosition}");

        // Find all agents in NoiseArea, with BrainBehaviour, and notify them
        var agent = other.gameObject?.GetComponent<Assets.Scripts.GOAP.Behaviours.BrainBehaviour>();

        if (agent != null)
        {
            agent.OnDistractionNoiseHeard(noisePosition, noiseRadius);
        }
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
