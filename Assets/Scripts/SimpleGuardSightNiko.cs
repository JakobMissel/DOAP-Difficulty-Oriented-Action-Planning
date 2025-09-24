using UnityEngine;

public class SimpleGuardSightNiko : MonoBehaviour
{
    [SerializeField] LayerMask playerLayer;
    [SerializeField] Transform eyes;
    [SerializeField] GameObject exclamationMark;
    [SerializeField] float hFieldOfView = 100f;
    [SerializeField] float vFieldOfView = 100f;
    [SerializeField] float viewDistance = 10f;
    [SerializeField] Vector3 sightOffset = new(0,-0.55f,0);
    GameObject player;
    bool playerHit;

    [Header("Gizmo")]
    [SerializeField] [Range(3,50)] int rayCount;
    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        LookForPlayer();
    }

    void LookForPlayer()
    {
        var direction = (player.transform.position - (eyes.position)).normalized;
        var distance = Vector3.Distance(player.transform.position, eyes.position);
        
        if (distance <= viewDistance)
        {
            // Get the angle to the player ignoring the y-axis
            var flatEyeForward = new Vector3(eyes.forward.x, 0, eyes.forward.z).normalized;
            var flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
            
            var horizontalAngle = Vector3.Angle(flatEyeForward, flatDirection);
            var verticalAngle = Vector3.Angle(eyes.forward + sightOffset, direction) - horizontalAngle;
            
            if (horizontalAngle <= hFieldOfView / 2 && Mathf.Abs(verticalAngle) <= vFieldOfView / 2)
            { 
                // Check for line of sight
                if(Physics.Raycast(eyes.position, direction, distance, playerLayer))
                {
                    playerHit = true;
                    PlayerSpotted();
                }
            }
            else
                playerHit = false;
        }
        exclamationMark.SetActive(playerHit);
    }

    void PlayerSpotted()
    {
        print("Gotcha!!");
    }

    void OnDrawGizmosSelected()
    {
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
                Vector3 direction = rotation * eyes.forward + sightOffset;

                Gizmos.DrawRay(eyes.position, direction.normalized * viewDistance);
            }
        }

        if (!player) return;
        if (playerHit)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.blue;
        Gizmos.DrawLine(eyes.position, player.transform.position);
    }
}
