using TMPro;
using UnityEngine;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI completionText;

    Objective currentObjective;
    int currentSubGoalIndex = 0;

    void OnEnable()
    {
        ObjectivesManager.onDisplayObjective += UpdateObjectiveUI;
    }

    void OnDisable()
    {
        ObjectivesManager.onDisplayObjective -= UpdateObjectiveUI;
    }

    void UpdateObjectiveUI(Objective objective, int subGoalIndex, float delay)
    {
        // Update current objective and sub-goal index
        currentObjective = objective;
        currentSubGoalIndex = subGoalIndex;
        
        // Show completion text before next objective if applicable
        completionText.text = "";
        if (currentObjective.completions.Count > 0)
        {
            // Show completion text for the last completed sub-goal
            completionText.text = currentObjective?.completions[currentSubGoalIndex - 1].completionText;
        }

        // If the objective is already completed, do not update UI / clear texts
        Invoke(nameof(DelayedUpdateUI), delay);
    }

    void DelayedUpdateUI()
    {
        if (currentObjective.isCompleted)
        {
            ObjectivesManager.OnSetNewObjective(currentObjective.nextObjective);
            return;
        }

        nameText.text = currentObjective.name;
        descriptionText.text = "";
        completionText.text = "";
        descriptionText.text = currentObjective?.goals[currentSubGoalIndex].descriptionText;
    }
}
