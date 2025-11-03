using UnityEngine;

// Emits periodic noise pulses around the player.
// - Larger radius when moving and not sneaking ("running").
// - Smaller radius when moving and sneaking.
// Agents with BrainBehaviour within the pulse radius will be notified via OnPlayerNoiseHeard(position, radius).
[DefaultExecutionOrder(10)]
public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("Noise Radii (meters)")]
    [Tooltip("Noise radius when moving and NOT sneaking")] public float runNoiseRadius = 12f;
    [Tooltip("Noise radius when moving and sneaking")] public float sneakNoiseRadius = 4f;
    [Tooltip("Optional noise radius when idle (0 to disable)")] public float idleNoiseRadius = 0f;

    [Header("Emission")]
    [Tooltip("Seconds between noise pulses while active")] public float pulseInterval = 0.25f;

    [Header("Debug")] public bool drawGizmos = true;
    [Tooltip("Seconds to keep debug gizmo visible")] public float gizmoFade = 0.3f;

    private bool isMoving;
    private bool isSneaking;

    private float nextPulseTime;
    private float lastPulseRadius;
    private float lastPulseTime;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("[PlayerNoiseEmitter] No Rigidbody found on player! Noise emission requires a Rigidbody to check velocity.");
        }
        else
        {
            Debug.Log("[PlayerNoiseEmitter] Initialized with Rigidbody");
        }
    }

    void OnEnable()
    {
        Debug.Log("[PlayerNoiseEmitter] OnEnable - Subscribing to PlayerActions events");
        PlayerActions.moveStatus += OnMoveStatus;
        PlayerActions.sneakStatus += OnSneakStatus;
        PlayerActions.isSneaking += OnSneakStatus;
    }

    void OnDisable()
    {
        Debug.Log("[PlayerNoiseEmitter] OnDisable - Unsubscribing from PlayerActions events");
        PlayerActions.moveStatus -= OnMoveStatus;
        PlayerActions.sneakStatus -= OnSneakStatus;
        PlayerActions.isSneaking -= OnSneakStatus;
    }

    void OnMoveStatus(bool moving)
    {
        isMoving = moving;
        Debug.Log($"[PlayerNoiseEmitter] Move status changed: {moving}");
    }

    void OnSneakStatus(bool sneaking)
    {
        isSneaking = sneaking;
        Debug.Log($"[PlayerNoiseEmitter] Sneak status changed: {sneaking}");
    }

    void Update()
    {
        float currentRadius = GetCurrentNoiseRadius();
        if (currentRadius <= 0f)
        {
            // Only log once when stopping noise emission
            if (Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
            {
                Debug.Log($"[PlayerNoiseEmitter] Not emitting noise - currentRadius={currentRadius:F2}, isMoving={isMoving}, isSneaking={isSneaking}, velocity={(rb != null ? rb.linearVelocity.magnitude : 0):F2}");
            }
            return;
        }

        if (Time.time >= nextPulseTime)
        {
            EmitNoise(currentRadius);
            nextPulseTime = Time.time + Mathf.Max(0.02f, pulseInterval);
        }
    }

    float GetCurrentNoiseRadius()
    {
        if (isMoving)
        {
            if (rb != null && rb.linearVelocity.sqrMagnitude < 0.01f)
            {
                // Only log occasionally to avoid spam
                if (Time.frameCount % 120 == 0)
                {
                    Debug.Log($"[PlayerNoiseEmitter] Moving but velocity too low - no noise (velocity: {rb.linearVelocity.magnitude:F3})");
                }
                return 0f;
            }

            float radius = isSneaking ? Mathf.Max(0f, sneakNoiseRadius) : Mathf.Max(0f, runNoiseRadius);
            return radius;
        }

        return Mathf.Max(0f, idleNoiseRadius);
    }

    void EmitNoise(float radius)
    {
        lastPulseRadius = radius;
        lastPulseTime = Time.time;

        int brainCount = 0;
        int notifiedCount = 0;

        // Notify all active brains within radius (no collider dependency)
        foreach (var brain in Assets.Scripts.GOAP.Behaviours.BrainBehaviour.GetActiveBrains())
        {
            brainCount++;
            if (brain == null)
                continue;

            float dist = Vector3.Distance(brain.transform.position, transform.position);
            if (dist <= radius)
            {
                brain.OnPlayerNoiseHeard(transform.position, radius);
                notifiedCount++;
            }
        }

        Debug.Log($"[PlayerNoiseEmitter] Emitted noise r={radius:F1}m, found {brainCount} brains, notified {notifiedCount}");
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        float previewRadius = Application.isPlaying ? GetCurrentNoiseRadius() : runNoiseRadius;
        if (previewRadius > 0f)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, previewRadius);
        }

        if (Application.isPlaying && lastPulseRadius > 0f && (Time.time - lastPulseTime) <= gizmoFade)
        {
            float t = 1f - Mathf.Clamp01((Time.time - lastPulseTime) / Mathf.Max(0.0001f, gizmoFade));
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f + 0.35f * t);
            Gizmos.DrawSphere(transform.position, lastPulseRadius);
        }
    }
}
