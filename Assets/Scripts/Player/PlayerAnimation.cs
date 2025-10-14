using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] Animator animator;
    PlayerMovement playerMovement;
    int moveLayer;
    int sneakLayer;
    int climbLayer;
    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
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
        MoveAnimation();
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
        animator?.SetLayerWeight(moveLayer, status ? 0 : 1);
        animator?.SetLayerWeight(climbLayer, status ? 1 : 0);
    }

    void GetAnimationLayers()
    {
        if(!animator) return;
        moveLayer = animator.GetLayerIndex("Move Layer");
        sneakLayer = animator.GetLayerIndex("Sneak Layer");
        climbLayer = animator.GetLayerIndex("Climb Layer");
    }
}
