using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    [SerializeField] Objective objective;

    [SerializeField] Image timerImage;
    [SerializeField] float moveTime;
    float moveT;
    [SerializeField] float sneakTime;
    float sneakT;
    [SerializeField] float climbTime;
    float climbT;
    [SerializeField] float aimTime;
    float aimT;

    void Awake()
    {
        for (int i = 0; i < objective.goals.Count; i++)
        {
            objective.goals[i].isCompleted = false;
            objective.goals[i].description = objective.goals[i].goal;
        }
        
        timerImage.fillAmount = 0;
        
        moveT = moveTime;
        sneakT = sneakTime;
        climbT = climbTime;
        aimT = aimTime;
    }
    void OnEnable()
    {
        PlayerActions.moveStatus += PlayerMoved;
        PlayerActions.isSneaking += PlayerSneaked;
        PlayerActions.climbStatus += PlayerClimbed;
        PlayerActions.pickedUpItem += PlayerPickedUpItem;
        PlayerActions.isAiming += PlayerAimed;
        PlayerActions.ammoUpdate += PlayerAmmoUpdated;
    }

    void OnDisable()
    {
        PlayerActions.moveStatus -= PlayerMoved;
        PlayerActions.isSneaking -= PlayerSneaked;
        PlayerActions.climbStatus -= PlayerClimbed;
        PlayerActions.pickedUpItem -= PlayerPickedUpItem;
        PlayerActions.isAiming -= PlayerAimed;
        PlayerActions.ammoUpdate -= PlayerAmmoUpdated;
    }

    void PlayerMoved(bool isMoving)
    {
        if(isMoving && IsPreviousGoalCompleted(0))
        {
            moveT -= Time.deltaTime;
            print(moveT);
            timerImage.fillAmount = 1 - (moveT / moveTime);
            if (moveT > 0) return;
            CompleteGoal(0);
            print("Completed Move Goal");
            // Unsubscribe after completing the goal
            PlayerActions.moveStatus -= PlayerMoved;
        }
    }

    void PlayerSneaked(bool isSneaking)
    {
        if (isSneaking && IsPreviousGoalCompleted(1))
        {
            sneakT -= Time.deltaTime;
            print(sneakT);
            timerImage.fillAmount = 1 - (sneakT / sneakTime);
            if (sneakT > 0) return;
            CompleteGoal(1);
            // Unsubscribe after completing the goal
            PlayerActions.isSneaking -= PlayerSneaked;
        }
    }

    void PlayerClimbed(bool isClimbing)
    {
        if (isClimbing && IsPreviousGoalCompleted(2))
        {
            climbT -= Time.deltaTime;
            print(climbT);
            timerImage.fillAmount = 1 - (climbT / climbTime);
            if (climbT > 0) return;
            CompleteGoal(2);
            // Unsubscribe after completing the goal
            PlayerActions.climbStatus -= PlayerClimbed;
        }
    }

    void PlayerPickedUpItem(Pickup item)
    {
        if (IsPreviousGoalCompleted(3))
        {
            CompleteGoal(3);
            // Unsubscribe after completing the goal
            PlayerActions.pickedUpItem -= PlayerPickedUpItem;
        }
    }

    void PlayerAimed(bool isAiming)
    {
        if (isAiming && IsPreviousGoalCompleted(4))
        {
            aimT -= Time.deltaTime;
            print(aimT);
            timerImage.fillAmount = 1 - (aimT / aimTime);
            if (aimT > 0) return;
            CompleteGoal(4);
            // Unsubscribe after completing the goal
            PlayerActions.isAiming -= PlayerAimed;
        }
    }

    void PlayerAmmoUpdated(int ammo)
    {
        if (ammo < 1 && IsPreviousGoalCompleted(5))
        {
            CompleteGoal(5);
            // Unsubscribe after completing the goal
            PlayerActions.ammoUpdate -= PlayerAmmoUpdated;
        }
    }

    /// <summary>
    /// Is the previous goal completed? Relative to the current goal index.
    /// </summary>
    bool IsPreviousGoalCompleted(int goalIndex)
    {
        if (goalIndex == 0)
        {
            return true;
        }
        if (objective.goals[goalIndex - 1].isCompleted)
        {
            return true;
        }
        Debug.LogWarning("Complete previous goals first!");
        return false;
    }

    void CompleteGoal(int goalIndex)
    {
        timerImage.fillAmount = 0;
        objective.goals[goalIndex].MarkAsCompleted();
        ObjectivesManager.OnUpdateObjective(objective);
    }
}
