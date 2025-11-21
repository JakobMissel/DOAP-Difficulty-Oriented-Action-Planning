using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.GOAP.Behaviours;

public class GuardSight : MonoBehaviour
{
    [SerializeField] LayerMask obstacleLayer;
    [SerializeField] Vector3 playerOffset = new Vector3(0, 1, 0);
    [SerializeField] Transform eyes;
    [SerializeField] GameObject exclamationMark;
    [SerializeField] float hFieldOfView = 100f;
    [SerializeField] float vFieldOfView = 100f;
    [SerializeField] float viewDistance = 10f;
    [SerializeField] [Tooltip("Gives the detection cone an angle.")] Vector3 sightRotationOffset = new(0,-0.55f,0);
    [SerializeField] float detectionDelay = 1f;
    [SerializeField] [Tooltip("How long PlayerSpotted() remains true after losing sight of player")] float spottedGracePeriod = 2f;
    [SerializeField] Image detectionIcon;
    float detectionTime;
    float timeLastSawPlayer; // Track when we last had visual contact

    GameObject player;
    bool playerHit;
    bool playerSpotted;

    // Allow other systems to read/rotate the eyes explicitly
    public Transform Eyes => eyes;
    
    // Allow other systems to read the current detection charge (0 to 1)
    public float DetectionCharge => detectionDelay > 0 ? Mathf.Clamp01(detectionTime / detectionDelay) : 0f;

    [Header("Gizmo")]
    [SerializeField] [Range(0,50)] int rayCount;
    
    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    public bool CanSeePlayer()
    {
        exclamationMark.SetActive(playerSpotted);
        Quaternion sightRotation = Quaternion.Euler(sightRotationOffset);
        var direction = (player.transform.position + playerOffset - eyes.position).normalized;
        var distance = Vector3.Distance(player.transform.position, eyes.position);

        if (distance <= viewDistance)
        {
            // Get the angle to the player in relation to the up vector
            var forwardRotation = eyes.forward + sightRotationOffset;
            var horizontalEyeForward = Vector3.ProjectOnPlane(forwardRotation, Vector3.up);
            var hortizontalDirection = Vector3.ProjectOnPlane(direction, Vector3.up);

            var horizontalAngle = Vector3.Angle(horizontalEyeForward, hortizontalDirection);

            // Get the angle of the player in relation to the right vector of the eyes
            var verticalEyeForward = Vector3.ProjectOnPlane(forwardRotation, eyes.right);
            var verticalDirection = Vector3.ProjectOnPlane(direction, eyes.right);

            var verticalAngle = Vector3.Angle(verticalEyeForward, verticalDirection);

            
            if (horizontalAngle <= hFieldOfView / 2 && Mathf.Abs(verticalAngle) <= vFieldOfView / 2)
            {
                // Check for line of sight
                if (Physics.Raycast(eyes.position, direction, distance, obstacleLayer))
                {
                    playerHit = false;
                    return playerHit;
                }
                playerHit = true;
                return playerHit;
            }
            else
            {
                playerHit = false;
                return playerHit;
            }
        }
        else
        {
            playerHit = false;
            return playerHit;
        }
    }

    void Update()
    {
        // Update playerHit by checking if we can see the player
        CanSeePlayer();
        
        // Then handle detection charge/discharge based on playerHit
        DetectPlayer();
    }

    void DetectPlayer()
    {
        if (playerHit)
        {
            timeLastSawPlayer = Time.time; // Update the last seen time
            
            if(detectionTime < detectionDelay)
            {
                // Play HUH sound on first frame of detection (when detection just started)
                if (detectionTime <= 0f)
                {
                    var audio = GetComponent<ActionAudioBehaviour>();
                    if (audio != null)
                    {
                        audio.PlayGuardHuh();
                    }
                }
                
                // Charge up detection visual
                detectionTime += Time.deltaTime;
                detectionIcon.fillAmount = detectionTime / detectionDelay;
            }
            if (detectionTime >= detectionDelay)
            {
                // Player has been spotted
                detectionIcon.fillAmount = 0;
                detectionTime = detectionDelay;
                playerSpotted = true;
            }
        }
        else
        {
            // Check if we should maintain playerSpotted during grace period
            if(playerSpotted && Time.time - timeLastSawPlayer < spottedGracePeriod)
            {
                // Keep playerSpotted true during grace period
                // Don't reset detection time yet
                return;
            }
            
            if(playerSpotted)
            {
                // Grace period has ended, player is no longer spotted
                playerSpotted = false;
            }
            
            // Charge down detection visual
            detectionTime -= Time.deltaTime;
            detectionIcon.fillAmount = detectionTime / detectionDelay;
            if(detectionTime <= 0f)
            {
                // Reset detection time
                detectionTime = 0f;
            }
        }
    }

    public bool PlayerSpotted()
    {
        return playerSpotted;
    }
    
    /// <summary>
    /// Resets the detection state to initial values.
    /// Used when resetting guards to their starting positions.
    /// </summary>
    public void ResetDetection()
    {
        detectionTime = 0f;
        timeLastSawPlayer = -999f;
        playerHit = false;
        playerSpotted = false;
        if (detectionIcon != null)
        {
            detectionIcon.fillAmount = 0f;
        }
        if (exclamationMark != null)
        {
            exclamationMark.SetActive(false);
        }
        Debug.Log($"[GuardSight] {name} detection reset");
    }
    
    void OnDrawGizmosSelected()
    {
        // Prevent division by zero when rayCount is 0 or 1
        if (rayCount <= 1 || eyes == null)
            return;
            
        Gizmos.color = Color.red;
        for (int y = -rayCount / 2; y <= rayCount / 2; y++)
        {
            float verticalPercent = (float)y / (rayCount / 2);
            float verticalAngle = verticalPercent * (vFieldOfView / 2);

            for (int x = -rayCount / 2; x <= rayCount / 2; x++)
            {
                float horizontalPercent = (float)x / (rayCount / 2);
                float horizontalAngle = horizontalPercent * (hFieldOfView / 2);

                Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);
                Vector3 direction = rotation * eyes.forward + sightRotationOffset;

                Gizmos.DrawRay(eyes.position, direction.normalized * viewDistance);
            }
        }

        if (!player) return;
        if (playerHit)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.blue;
        Gizmos.DrawLine(eyes.position, player.transform.position + playerOffset);
    }
}