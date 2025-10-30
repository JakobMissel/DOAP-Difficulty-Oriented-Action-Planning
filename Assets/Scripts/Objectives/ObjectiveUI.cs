using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    //[SerializeField] TextMeshProUGUI descriptionText;
    //[SerializeField] TextMeshProUGUI completionText;
    [SerializeField] GameObject objectivesTextArea;
    [SerializeField] GameObject objectivesTextPrefab;
    [SerializeField] List<GameObject> objectivesTexts;

    Objective currentObjective;
    int currentSubGoalIndex = 0;

    void OnEnable()
    {
        ObjectivesManager.displayObjective += UpdateObjectiveUI;
    }

    void OnDisable()
    {
        ObjectivesManager.displayObjective -= UpdateObjectiveUI;
    }

    void CreateObjectiveText(string text)
    {
        GameObject newText = Instantiate(objectivesTextPrefab, objectivesTextArea.transform);
        newText.GetComponent<TextMeshProUGUI>().text = text;
        objectivesTexts.Add(newText);
    }

    void UpdateObjectiveUI(Objective objective, int subGoalIndex, float delay)
    {
        // Update current objective and sub-goal index
        currentObjective = objective;
        currentSubGoalIndex = subGoalIndex;
        
        // Show completion text before next objective if applicable
        if (currentObjective.completions.Count > 0 && currentObjective.subObjectives.Count != currentObjective.completions.Count)
        {
            // Show completion text for the most recent completed sub-goal
            CreateObjectiveText(currentObjective.completions[currentSubGoalIndex - 1].completionText);
        }
        if (currentObjective.completions.Count > 0 && currentObjective.subObjectives.Count == currentObjective.completions.Count)
        {
            // Show completion text for the last completed sub-goal
            CreateObjectiveText(currentObjective.completions[currentSubGoalIndex].completionText);
        }

        // If the objective is already completed, do not update UI / clear texts
        Invoke(nameof(DelayedUpdateUI), delay);
    }

    void DelayedUpdateUI()
    {
        // Show completion text
        if (currentObjective.isCompleted)
        {
            ClearTextArea();
            CreateObjectiveText($"{currentObjective} has been completed!");
            return;
        }
        
        // Update UI texts
        nameText.text = currentObjective.name;
        ClearTextArea();

        // Activate the current sub-objective
        if(currentObjective && currentObjective.subObjectives.Count > currentSubGoalIndex)
        {
            ObjectivesManager.OnActivateSubObjective(currentObjective?.subObjectives[currentSubGoalIndex]);
            CreateObjectiveText(currentObjective?.subObjectives[currentSubGoalIndex].descriptionText);
        }
    }

    void ClearTextArea()
    {
        objectivesTexts.Clear();
        for (int i = 0; i < objectivesTextArea.transform.childCount; i++)
        {
            Destroy(objectivesTextArea.transform.GetChild(i).gameObject);
        }
    }
}
