using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(BoxCollider))]
public class LaserBeam : MonoBehaviour
{
    [Header("Endpoints")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Beam Visual")]
    [Min(0.001f)] public float thickness = 0.05f;
    public Color color = Color.red;

    [Header("Alert")]
    public string triggerTag = "Player";
    public bool autoReset = true;
    [Tooltip("Seconds until the laser will detect again after being triggered.")]
    public float resetTime = 3f;
    public AudioClip alertClip;
    public bool playSound = true;

    [Header("Events")]
    public UnityEvent onTriggered;

    // internals
    private LineRenderer lr;
    private BoxCollider boxCol;
    private bool triggered = false;
    private AudioSource audioSource;
    private Material runtimeMat;

    void Awake()
    {
        EnsureComponents();
        SetupAudio();
        UpdateBeam(); // do an initial build
    }

    void Reset()
    {
        EnsureComponents();
    }

    void OnEnable()
    {
        // keep things in sync in edit mode too
        UpdateBeam();
    }

    void Update()
    {
        // Update every frame so moving endpoints keep the beam/collider aligned
        UpdateBeam();
    }

    void OnValidate()
    {
        EnsureComponents();
        UpdateBeam();
    }

    void EnsureComponents()
    {
        if (!lr) lr = GetComponent<LineRenderer>();
        if (!boxCol) boxCol = GetComponent<BoxCollider>();

        // LineRenderer baseline settings
        lr.positionCount = 2;
        lr.useWorldSpace = false; // we'll move/rotate the Laser object instead
        lr.startWidth = thickness;
        lr.endWidth = thickness;

        // Make sure we have a material that we can tint safely
        if (lr.material == null || (Application.isPlaying && runtimeMat == null))
        {
            var shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default"); // fallback
            runtimeMat = new Material(shader);
            lr.material = runtimeMat;
        }

        // In edit mode, tint the assigned material directly; at runtime, tint our runtimeMat
        lr.material.color = color;

        boxCol.isTrigger = true;
    }

    void SetupAudio()
    {
        if (playSound)
        {
            audioSource = GetComponent<AudioSource>();
            if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = alertClip;
        }
    }

    void UpdateBeam()
    {
        if (startPoint == null || endPoint == null) return;

        Vector3 a = startPoint.position;
        Vector3 b = endPoint.position;
        Vector3 dir = b - a;
        float length = dir.magnitude;

        // Avoid zero-length issues
        if (length < 0.0001f)
        {
            // Collapse beam & collider
            transform.SetPositionAndRotation(a, Quaternion.identity);
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, Vector3.zero);
            boxCol.size = Vector3.zero;
            return;
        }

        Vector3 mid = (a + b) * 0.5f;
        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        // Place this Laser object at the midpoint, facing along +Z toward EndPoint
        transform.SetPositionAndRotation(mid, rot);

        // Draw the line in local space from -Z/2 to +Z/2
        float half = length * 0.5f;
        lr.startWidth = thickness;
        lr.endWidth = thickness;
        lr.SetPosition(0, Vector3.back * half);
        lr.SetPosition(1, Vector3.forward * half);

        // Size the trigger collider to cover the beam
        // Axis-aligned to this object's local frame (so it matches the line).
        boxCol.center = Vector3.zero;
        boxCol.size = new Vector3(thickness * 2f, thickness * 2f, length);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;

        TriggeredBy(other.gameObject);
    }

    void TriggeredBy(GameObject who)
    {
        triggered = true;
        onTriggered?.Invoke();
        Debug.Log($"Laser triggered by {who.name}");

        if (playSound && audioSource && alertClip)
            audioSource.PlayOneShot(alertClip);

        // quick flash feedback
        StartCoroutine(FlashBeamRoutine());

        if (autoReset)
            StartCoroutine(ResetRoutine());
    }

    IEnumerator FlashBeamRoutine()
    {
        var mat = lr.material;
        var orig = mat.color;
        mat.color = Color.white;
        yield return new WaitForSeconds(0.12f);
        mat.color = color;
    }

    IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(resetTime);
        triggered = false;
    }

    void OnDrawGizmosSelected()
    {
        if (startPoint == null || endPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(startPoint.position, endPoint.position);

        // draw collider box outline (approx)
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1, 0, 0, 0.7f);
        Vector3 size = new Vector3(thickness * 2f, thickness * 2f, Vector3.Distance(startPoint.position, endPoint.position));
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
