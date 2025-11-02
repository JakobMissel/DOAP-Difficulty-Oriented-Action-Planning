using System.Collections.Generic;
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
    public Color inactiveColor = Color.red;
    public Color activeColor = Color.white; // shown while player stands in the beam

    [Header("Detection")]
    public string triggerTag = "Player";

    [Header("Audio (optional)")]
    public AudioClip activateClip;   // one-shot on enter
    public AudioClip loopClip;       // loops while active
    public AudioClip deactivateClip; // one-shot on exit
    public bool playAudio = true;

    [Header("Events")]
    public UnityEvent onActivated;              // fired once when first valid collider enters
    public UnityEvent<float> onActiveTick;      // fired every frame while active (passes active duration seconds)
    public UnityEvent onDeactivated;            // fired once when last valid collider exits

    [Header("State")]
    [Tooltip("If true, the laser line + trigger collider are enabled on start.")]
    public bool startEnabled = false;

    // internals
    private LineRenderer lr;
    private BoxCollider boxCol;
    private AudioSource audioSource;
    private Material runtimeMat;

    private readonly HashSet<Collider> _inside = new HashSet<Collider>();
    private bool _active = false;
    private float _activeSince = 0f;
    private bool _isEnabled = false;
    
    // Cache to prevent feedback loops during editing
    private Vector3 _lastStartPos;
    private Vector3 _lastEndPos;
    private const float UPDATE_THRESHOLD = 0.001f;
    private const float MAX_BEAM_LENGTH = 1000f; // safety limit

    public bool IsEnabled => _isEnabled;

    void Awake()
    {
        EnsureComponents();
        InitializePositionCache();
        UpdateBeam();
        SetBeamColor(inactiveColor);
    }

    void OnEnable()
    {
        EnsureComponents();
        InitializePositionCache();
        UpdateBeam();
        if (Application.isPlaying) SetBeamColor(_active ? activeColor : inactiveColor);
        // Apply starting enabled state
        if (Application.isPlaying)
            SetEnabled(startEnabled);
        else
            EditorApplyEnabled(startEnabled);
    }

    void Update()
    {
        UpdateBeamIfNeeded();

        if (Application.isPlaying && _active)
        {
            float activeFor = Time.time - _activeSince;
            onActiveTick?.Invoke(activeFor);
        }
    }

    void OnValidate()
    {
        EnsureComponents();
        InitializePositionCache();
        UpdateBeam();
        if (!Application.isPlaying) SetBeamColor(_active ? activeColor : inactiveColor);
        // Keep editor visuals in sync
        if (!Application.isPlaying)
            EditorApplyEnabled(startEnabled);
    }
    
    void InitializePositionCache()
    {
        if (startPoint != null) _lastStartPos = startPoint.position;
        if (endPoint != null) _lastEndPos = endPoint.position;
    }
    
    void UpdateBeamIfNeeded()
    {
        if (startPoint == null || endPoint == null) return;
        
        Vector3 currentStart = startPoint.position;
        Vector3 currentEnd = endPoint.position;
        
        // Only update if positions have meaningfully changed
        if (Vector3.Distance(currentStart, _lastStartPos) > UPDATE_THRESHOLD ||
            Vector3.Distance(currentEnd, _lastEndPos) > UPDATE_THRESHOLD)
        {
            UpdateBeam();
            _lastStartPos = currentStart;
            _lastEndPos = currentEnd;
        }
    }

    void EnsureComponents()
    {
        if (!lr) lr = GetComponent<LineRenderer>();
        if (!boxCol) boxCol = GetComponent<BoxCollider>();

        lr.positionCount = 2;
        lr.useWorldSpace = false;
        lr.startWidth = thickness;
        lr.endWidth = thickness;

        if (lr.sharedMaterial == null || (Application.isPlaying && runtimeMat == null))
        {
            var shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            runtimeMat = new Material(shader);
            lr.sharedMaterial = runtimeMat;
        }

        boxCol.isTrigger = true;

        if (playAudio)
        {
            if (!audioSource) audioSource = GetComponent<AudioSource>();
            if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    void UpdateBeam()
    {
        if (startPoint == null || endPoint == null) return;

        Vector3 a = startPoint.position;
        Vector3 b = endPoint.position;
        Vector3 dir = b - a;
        float length = dir.magnitude;

        if (length < 0.0001f)
        {
            transform.SetPositionAndRotation(a, Quaternion.identity);
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, Vector3.zero);
            boxCol.size = Vector3.zero;
            return;
        }
        
        // Safety check: prevent ridiculous distances
        if (length > MAX_BEAM_LENGTH)
        {
            Debug.LogWarning($"LaserBeam [{gameObject.name}]: Distance between start and end points ({length:F2}) exceeds maximum ({MAX_BEAM_LENGTH}). Clamping.", this);
            length = MAX_BEAM_LENGTH;
            dir = dir.normalized * length;
            b = a + dir;
        }

        Vector3 mid = (a + b) * 0.5f;
        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        
        // Only update position if not NaN and within reasonable bounds
        if (!float.IsNaN(mid.x) && !float.IsNaN(mid.y) && !float.IsNaN(mid.z))
        {
            transform.SetPositionAndRotation(mid, rot);
        }
        else
        {
            Debug.LogError($"LaserBeam [{gameObject.name}]: Invalid midpoint calculated. Skipping position update.", this);
            return;
        }

        float half = length * 0.5f;
        lr.startWidth = thickness;
        lr.endWidth = thickness;
        lr.SetPosition(0, Vector3.back * half);
        lr.SetPosition(1, Vector3.forward * half);

        boxCol.center = Vector3.zero;
        boxCol.size = new Vector3(thickness * 2f, thickness * 2f, length);
    }

    void SetBeamColor(Color c)
    {
        if (lr && lr.sharedMaterial) lr.sharedMaterial.color = c;
    }

    // Public API to enable/disable the laser visuals + trigger
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (lr) lr.enabled = enabled;
        if (boxCol) boxCol.enabled = enabled;

        // If disabling while active, clean up state
        if (!enabled)
        {
            if (_active)
                Deactivate();
        }
    }

    // Editor-only helper to avoid playing audio when toggling in editor
    private void EditorApplyEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (lr) lr.enabled = enabled;
        if (boxCol) boxCol.enabled = enabled;
    }

    // --- Trigger logic: activate on first enter, stay active while any valid collider remains, deactivate on last exit ---

    void OnTriggerEnter(Collider other)
    {
        if (!IsValid(other)) return;

        if (_inside.Add(other))
        {
            if (!_active)
                Activate();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsValid(other)) return;

        if (_inside.Remove(other))
        {
            if (_active && _inside.Count == 0)
                Deactivate();
        }
    }

    bool IsValid(Collider other)
    {
        if (!other) return false;
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return false;
        return true;
    }

    void Activate()
    {
        _active = true;
        _activeSince = Time.time;
        SetBeamColor(activeColor);
        onActivated?.Invoke();

        if (playAudio && audioSource)
        {
            // one-shot start
            if (activateClip) audioSource.PlayOneShot(activateClip);

            // start looping alarm
            if (loopClip)
            {
                audioSource.clip = loopClip;
                audioSource.loop = true;
                // if a one-shot just played, layering is fine; otherwise start loop immediately
                audioSource.Play();
            }
        }
    }

    void Deactivate()
    {
        _active = false;
        SetBeamColor(inactiveColor);
        onDeactivated?.Invoke();

        if (playAudio && audioSource)
        {
            // stop loop if any
            if (audioSource.loop)
            {
                audioSource.loop = false;
                audioSource.Stop();
                audioSource.clip = null;
            }
            // play end blip
            if (deactivateClip) audioSource.PlayOneShot(deactivateClip);
        }
    }

    void OnDisable()
    {
        // clean state to avoid stuck audio in editor
        _inside.Clear();
        if (_active) Deactivate();
    }

    void OnDrawGizmosSelected()
    {
        if (startPoint == null || endPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(startPoint.position, endPoint.position);

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1, 0, 0, 0.6f);
        if (boxCol != null) Gizmos.DrawWireCube(boxCol.center, boxCol.size);
    }
}
