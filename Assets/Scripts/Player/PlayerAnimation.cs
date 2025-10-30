using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] Animator animator;

    [Header("Climbing")]
    [SerializeField] GameObject playerHips;
    [SerializeField] Vector3 hipsRotationOffset;

    PlayerMovement playerMovement;
    PlayerClimb playerClimb;
    int moveLayer;
    int sneakLayer;
    int climbLayer;
    bool onWall;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerClimb = GetComponent<PlayerClimb>();
        GetAnimationLayers();
    }

    void OnEnable()
    {
        PlayerActions.sneakStatus += SneakAnimation;
        PlayerActions.climbStatus += ClimbAnimation;
    }

    void OnDisable()
    {
        PlayerActions.sneakStatus -= SneakAnimation;
        PlayerActions.climbStatus -= ClimbAnimation;
    }

    void Update()
    {
        if(!playerClimb.onWall) 
            MoveAnimation();
        else
            ClimbAnimation();
    }

    void MoveAnimation()
    {
        animator?.SetFloat("VelocityZ", playerMovement.velocity.z);
        if (playerMovement.moveInput.y == 0)
            animator?.SetFloat("VelocityX", playerMovement.velocity.x);
        else
            animator?.SetFloat("VelocityX", playerMovement.velocity.x / 2);
    }

    void SneakAnimation(bool status)
    {
        animator?.SetLayerWeight(moveLayer, status ? 0 : 1);
        animator?.SetLayerWeight(sneakLayer, status ? 1 : 0);
    }

    void ClimbAnimation(bool status)
    {
        animator?.SetLayerWeight(moveLayer, playerClimb.onWall ? 0 : 1);
        animator?.SetLayerWeight(climbLayer, playerClimb.onWall ? 1 : 0);
    }

    void ClimbAnimation()
    {
        animator?.SetFloat("VelocityZ", playerClimb.velocity.y);
        if (playerMovement.moveInput.y == 0)
            animator?.SetFloat("VelocityX", playerClimb.velocity.x);
        else
            animator?.SetFloat("VelocityX", playerClimb.velocity.x / 2);

        //if (onWall)
        //{
        //    playerHips.transform.localEulerAngles += hipsRotationOffset;
        //}
        //else
        //{
        //    playerHips.transform.localEulerAngles = Vector3.zero;
        //}
    }

    void GetAnimationLayers()
    {
        if(!animator) return;
        moveLayer = animator.GetLayerIndex("Move Layer");
        sneakLayer = animator.GetLayerIndex("Sneak Layer");
        climbLayer = animator.GetLayerIndex("Climb Layer");
    }
}
