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
        if(isMoving && IsPreviousGoalCompleted(0) && IsSubObjectiveActive(0))
        {
            moveT -= Time.deltaTime;
            //print(moveT);
            timerImage.fillAmount = 1 - (moveT / moveTime);
            
            if (moveT > 0) return;
            // Unsubscribe after completing the goal
            PlayerActions.moveStatus -= PlayerMoved;

            CompleteSubObjective(0, delayBetweenGoals);
        }
    }

    void PlayerSneaked(bool isSneaking)
    {
        if (isSneaking && IsPreviousGoalCompleted(1) && IsSubObjectiveActive(1))
        {
            sneakT -= Time.deltaTime;
            //print(sneakT);
            timerImage.fillAmount = 1 - (sneakT / sneakTime);
            if (sneakT > 0) return;
            // Unsubscribe after completing the goal
            PlayerActions.isSneaking -= PlayerSneaked;
            
            CompleteSubObjective(1, delayBetweenGoals);
        }
    }

    void PlayerClimbed(bool isClimbing)
    {
        if (isClimbing && IsPreviousGoalCompleted(2) && IsSubObjectiveActive(2))
        {
            climbT -= Time.deltaTime;
            //print(climbT);
            timerImage.fillAmount = 1 - (climbT / climbTime);
            if (climbT > 0) return;
            // Unsubscribe after completing the goal
            PlayerActions.climbStatus -= PlayerClimbed;
            
            CompleteSubObjective(2, delayBetweenGoals);
        }
    }

    void PlayerPickedUpItem(Pickup item)
    {
        if (IsPreviousGoalCompleted(3) && IsSubObjectiveActive(3))
        {
            // Unsubscribe after completing the goal
            PlayerActions.pickedUpItem -= PlayerPickedUpItem;
            
            CompleteSubObjective(3, delayBetweenGoals);
        }
    }

    void PlayerAimed(bool isAiming)
    {
        if (isAiming && IsPreviousGoalCompleted(4) && IsSubObjectiveActive(4))
        {
            aimT -= Time.deltaTime;
            //print(aimT);
            timerImage.fillAmount = 1 - (aimT / aimTime);
            if (aimT > 0) return;
            // Unsubscribe after completing the goal
            PlayerActions.isAiming -= PlayerAimed;
            
            CompleteSubObjective(4, delayBetweenGoals);
        }
    }

    void PlayerAmmoUpdated(int ammo)
    {
        if (ammo < 1 && IsPreviousGoalCompleted(5) && IsSubObjectiveActive(5))
        {
            // Unsubscribe after completing the goal
            PlayerActions.ammoUpdate -= PlayerAmmoUpdated;
            
            CompleteSubObjective(5, delayBetweenGoals);
        }
    }

    /// <summary>
    /// Is the previous sub objective completed? Relative to the current sub objective (index).
    /// </summary>
    bool IsPreviousGoalCompleted(int subObjectiveIndex)
    {
        if (subObjectiveIndex == 0)
        {
            return true;
        }
        if (objective.subObjectives[subObjectiveIndex - 1].isCompleted)
        {
            return true;
        }
        Debug.LogWarning("Complete previous goals first!");
        return false;
    }

    bool IsSubObjectiveActive(int subObjectiveIndex)
    {
        return objective.subObjectives[subObjectiveIndex].isActive;
    }

    void CompleteSubObjective(int subObjectiveIndex, float delay)
    {
        timerImage.fillAmount = 0;
        objective.CompleteSubObjective(subObjectiveIndex);
        objective.DisplayNextSubObjective(delay);
    }
}
