using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    public static Tutorial Instance;
    [SerializeField] Objective objective;

    [SerializeField] Image timerImage;
    [SerializeField] float timerFadeTime = 0.5f;
    float fadeT;
    [SerializeField] int tutorialThrowCount = 5;
    int throwCount;
    
    [SerializeField] float delayBetweenGoals = 1f;
    [SerializeField] float moveTime;
    float moveT;
    [SerializeField] float sneakTime;
    float sneakT;
    [SerializeField] float climbTime;
    float climbT;
    [SerializeField] float aimTime;
    float aimT;

    [Header("Painting")]
    [SerializeField] StealablePickup tutorialPainting;

    bool paintingStolen = false;
    GameObject paintingCarry;

    [Header("Skip settings")]
    [Tooltip("Key used to instantly skip the tutorial.")]
    [SerializeField] private KeyCode skipKey = KeyCode.P;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        timerImage.fillAmount = 0;
        throwCount = 0;
        moveT = moveTime;
        sneakT = sneakTime;
        climbT = climbTime;
        aimT = aimTime;
        if(timerImage == null)
            timerImage = GameObject.Find("TimerImage").GetComponent<Image>();
    }
    void OnEnable()
    {
        PlayerActions.moveStatus += PlayerMoved;
        PlayerActions.isSneaking += PlayerSneaked;
        PlayerActions.climbStatus += PlayerClimbed;
        PlayerActions.pickedUpItem += PlayerPickedUpItem;
        PlayerActions.isAiming += PlayerAimed;
        PlayerActions.ammoUpdate += PlayerAmmoUpdated;
        PlayerActions.stealItem += PaintingSteal;
        PlayerActions.paintingDelivered += DeliverPainting;
    }

    void OnDisable()
    {
        PlayerActions.moveStatus -= PlayerMoved;
        PlayerActions.isSneaking -= PlayerSneaked;
        PlayerActions.climbStatus -= PlayerClimbed;
        PlayerActions.pickedUpItem -= PlayerPickedUpItem;
        PlayerActions.isAiming -= PlayerAimed;
        PlayerActions.ammoUpdate -= PlayerAmmoUpdated;
        PlayerActions.stealItem -= PaintingSteal;
        PlayerActions.paintingDelivered -= DeliverPainting;
    }

    void Update()
    {
        //if (objective != null && objective.isActive && Input.GetKeyDown(skipKey))
        //{
        //    SkipTutorial();
        //}
    }

    public void SkipTutorial()
    {
        // Mark all remaining subobjectives as completed in order
        for (int i = 0; i < objective.subObjectives.Count; i++)
        {
            var sub = objective.subObjectives[i];
            if (!sub.isCompleted)
            {
                objective.CompleteSubObjective(i);
            }
        }

        // Ensure the player has full interaction/throw capabilities as if they completed the tutorial
        PlayerActions.OnCanInteract(true);
        PlayerActions.OnCanThrow(true);

        StealTutorialPainting();

        // Mark the whole tutorial objective as completed and advance to the next one
        ObjectivesManager.OnCompleteObjective(objective);

        // Immediately hide any tutorial timer visuals
        fadeT = 0f;
        timerImage.fillAmount = 0f;
        timerImage.color = new Color(timerImage.color.r, timerImage.color.g, timerImage.color.b, 0f);

        // Prevent further tutorial logic from running
        enabled = false;
    }

    void PlayerMoved(bool isMoving)
    {
        if (!objective.isActive) return;
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
        if (!objective.isActive) return;
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
        if (!objective.isActive) return;
        if (isClimbing && IsPreviousGoalCompleted(2) && IsSubObjectiveActive(2))
        {
            climbT -= Time.deltaTime;
            //print(climbT);
            timerImage.fillAmount = 1 - (climbT / climbTime);
            if (climbT > 0) return;
            // Unsubscribe after completing the goal
            PlayerActions.climbStatus -= PlayerClimbed;
            StartCoroutine(EnableInteraction());
            CompleteSubObjective(2, delayBetweenGoals);
        }
    }

    void PlayerPickedUpItem(Pickup item)
    {
        if (!objective.isActive) return;
        if (IsPreviousGoalCompleted(3) && IsSubObjectiveActive(3))
        {
            // Unsubscribe after completing the goal
            PlayerActions.pickedUpItem -= PlayerPickedUpItem;
            CompleteSubObjective(3, delayBetweenGoals);
        }
    }

    void PlayerAimed(bool isAiming)
    {
        if (!objective.isActive) return;
        if (isAiming && IsPreviousGoalCompleted(4) && IsSubObjectiveActive(4))
        {
            aimT -= Time.deltaTime;
            //print(aimT);
            timerImage.fillAmount = 1 - (aimT / aimTime);
            if (aimT > 0) return;
            // Unsubscribe after completing the goal
            PlayerActions.isAiming -= PlayerAimed;
            StartCoroutine(EnableThrow());
            CompleteSubObjective(4, delayBetweenGoals);
        }
    }

    void PlayerAmmoUpdated(int ammo)
    {
        if (!objective.isActive) return;
        if (IsPreviousGoalCompleted(5) && IsSubObjectiveActive(5))
        {
            throwCount++;
            timerImage.fillAmount = (float)throwCount / tutorialThrowCount;
            if(throwCount < tutorialThrowCount) return;
            // Unsubscribe after completing the goal
            PlayerActions.ammoUpdate -= PlayerAmmoUpdated;
            PlayerActions.OnLoseThrowables();
            StartCoroutine(EnableTutorialPaintingSteal());
            CompleteSubObjective(5, delayBetweenGoals);
        }
    }

    void PaintingSteal(StealablePickup painting)
    {
        if (!objective.isActive) return;
        if (IsPreviousGoalCompleted(6) && IsSubObjectiveActive(6))
        {
            paintingStolen = true;

            objective.subObjectives[6].completionText = $"When you carry a painting you cannot perform actions such as throwing coins or climbing. Be mindful and plan ahead!";
            objective.subObjectives[7].goalText = $"Return to the entrance and place the painting."; 
            objective.subObjectives[7].completionText = $"Good job. Now go get the rest of the paintings framed in gold.";

            painting.gameObject.SetActive(false);

            // Carry painting visually
            paintingCarry = painting.gameObject.GetComponent<StealablePickup>().paintingCarryPrefab;
            GameObject newPainting = Instantiate(paintingCarry);
            StealPainting.OnSendPaintingPrefab(paintingCarry);
            newPainting.transform.SetParent(StealPainting.Instance.playerPaintingPosition.transform);
            newPainting.transform.localPosition = StealPainting.Instance.paintingPositionOffset;
            newPainting.transform.localRotation = Quaternion.Euler(StealPainting.Instance.paintingRotationOffset);

            PlayerActions.stealItem -= PaintingSteal;
            CompleteSubObjective(6, delayBetweenGoals);
        }
    }

    void DeliverPainting()
    {
        if (!objective.isActive) return;
        if (IsPreviousGoalCompleted(7) && IsSubObjectiveActive(7))
        {
            if (!paintingStolen) return;
            if (StealPainting.Instance.playerPaintingPosition.transform.childCount > 2)
            {
                Destroy(StealPainting.Instance.playerPaintingPosition.transform.GetChild(2).gameObject);
            }
            StealPainting.Instance.stolenPaintings.Add(tutorialPainting);
            PlayerActions.paintingDelivered -= DeliverPainting;
            CompleteSubObjective(7, delayBetweenGoals);
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
        StartCoroutine(TimerFadeOut(timerFadeTime));
        objective.CompleteSubObjective(subObjectiveIndex);
        objective.DisplayNextSubObjective(delay);
    }

    IEnumerator TimerFadeOut(float delay)
    {
        while (fadeT < delay)
        {
            fadeT += Time.deltaTime;
            var alpha = Mathf.Lerp(1, 0, fadeT / delay);
            timerImage.color = new Color(timerImage.color.r, timerImage.color.g, timerImage.color.b, alpha);
            yield return null;
        }
        fadeT = 0;
        timerImage.fillAmount = 0;
        timerImage.color = new Color(timerImage.color.r, timerImage.color.g, timerImage.color.b, 1);
    }

    IEnumerator EnableInteraction()
    {
        yield return new WaitForSeconds(delayBetweenGoals);
        PlayerActions.OnCanInteract(true);
    }

    IEnumerator EnableThrow()
    {
        yield return new WaitForSeconds(delayBetweenGoals);
        PlayerActions.OnCanThrow(true);
    }

    IEnumerator EnableTutorialPaintingSteal()
    {
        objective.subObjectives[6].goalText = $"Hold [E] to steal the painting framed in gold by {tutorialPainting.painterName}.";
        yield return new WaitForSeconds(delayBetweenGoals);
        tutorialPainting.tutorialPainting = true;
    }

    /// <summary>
    /// Used to force steal the tutorial painting. Called when "skipping" the tutorial.
    /// </summary>
    void StealTutorialPainting()
    {
        tutorialPainting.tutorialPainting = true;
        StealPainting.OnSendPaintingPrefab(tutorialPainting.paintingCarryPrefab);
        StealPainting.Instance.stolenPaintings.Add(tutorialPainting);
        tutorialPainting.gameObject.SetActive(false);
        PaintingDropPoint.OnPlacePainting();
    }
}
