using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    [SerializeField] Objective objective;

    [SerializeField] Image timerImage;
    [SerializeField] float delayBetweenGoals = 1f;

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
        if(isMoving && IsPreviousGoalCompleted(0) && IsGoalActive(0))
        {
            moveT -= Time.deltaTime;
            print(moveT);
            timerImage.fillAmount = 1 - (moveT / moveTime);
            
            if (moveT > 0) return;
            // Unsubscribe after completing the goal
            PlayerActions.moveStatus -= PlayerMoved;

            CompleteGoal(0, delayBetweenGoals);
        }
    }

    void PlayerSneaked(bool isSneaking)
    {
        if (isSneaking && IsPreviousGoalCompleted(1) && IsGoalActive(1))
        {
            sneakT -= Time.deltaTime;
            print(sneakT);
            timerImage.fillAmount = 1 - (sneakT / sneakTime);
            if (sneakT > 0) return;
            // Unsubscribe after completing the goal
            PlayerActions.isSneaking -= PlayerSneaked;
            
            CompleteGoal(1, delayBetweenGoals);
        }
    }

    void PlayerClimbed(bool isClimbing)
    {
        if (isClimbing && IsPreviousGoalCompleted(2) && IsGoalActive(2))
        {
            climbT -= Time.deltaTime;
            print(climbT);
            timerImage.fillAmount = 1 - (climbT / climbTime);
            if (climbT > 0) return;
            // Unsubscribe after completing the goal
            PlayerActions.climbStatus -= PlayerClimbed;
            
            CompleteGoal(2, delayBetweenGoals);
        }
    }

    void PlayerPickedUpItem(Pickup item)
    {
        if (IsPreviousGoalCompleted(3) && IsGoalActive(3))
        {
            // Unsubscribe after completing the goal
            PlayerActions.pickedUpItem -= PlayerPickedUpItem;
            
            CompleteGoal(3, delayBetweenGoals);
        }
    }

    void PlayerAimed(bool isAiming)
    {
        if (isAiming && IsPreviousGoalCompleted(4) && IsGoalActive(4))
        {
            aimT -= Time.deltaTime;
            print(aimT);
            timerImage.fillAmount = 1 - (aimT / aimTime);
            if (aimT > 0) return;
            // Unsubscribe after completing the goal
            PlayerActions.isAiming -= PlayerAimed;
            
            CompleteGoal(4, delayBetweenGoals);
        }
    }

    void PlayerAmmoUpdated(int ammo)
    {
        if (ammo < 1 && IsPreviousGoalCompleted(5) && IsGoalActive(5))
        {
            // Unsubscribe after completing the goal
            PlayerActions.ammoUpdate -= PlayerAmmoUpdated;
            
            CompleteGoal(5, delayBetweenGoals);
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
        if (objective.subObjectives[goalIndex - 1].isCompleted)
        {
            return true;
        }
        Debug.LogWarning("Complete previous goals first!");
        return false;
    }

    bool IsGoalActive(int goalIndex)
    {
        return objective.subObjectives[goalIndex].isActive;
    }

    void CompleteGoal(int subObjectiveIndex, float delay)
    {
        timerImage.fillAmount = 0;
        objective.CompleteSubObjective(subObjectiveIndex);
        objective.DisplayNextSubObjective(delay);
    }
}
