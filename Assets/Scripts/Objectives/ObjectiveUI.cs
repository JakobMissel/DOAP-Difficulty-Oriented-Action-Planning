using TMPro;
using UnityEngine;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] GameObject objectivesTextPrefab;
    [SerializeField] GameObject middlePanel;
    [SerializeField] GameObject objectiveMiddlePanel;
    [SerializeField] GameObject sidePanel;
    [SerializeField] GameObject objectiveSidePanel;

    Objective currentObjective;
    int currentSubObjectiveIndex = 0;

    void Awake()
    {
        objectiveMiddlePanel.SetActive(false);
        objectiveSidePanel.SetActive(false);
    }

    void OnEnable()
    {
        ObjectivesManager.displayObjective += UpdateObjectiveUI;
        ObjectivesManager.trackPaintings += TrackPaintings;
        PlayerActions.stealItem += ActivateTextArea;
        PlayerActions.paintingDelivered += DeactivateTextArea;
    }


    void OnDisable()
    {
        ObjectivesManager.displayObjective -= UpdateObjectiveUI;
        ObjectivesManager.trackPaintings -= TrackPaintings;
        PlayerActions.stealItem += ActivateTextArea;
    }

    void ActivateTextArea(StealablePickup pickup)
    {
        //objectivesTextAreaMiddle.SetActive(true);
    }
    
    void DeactivateTextArea()
    {
        objectiveMiddlePanel.SetActive(false);
    }
    
    void CreateObjectiveText(string text)
    {
        GameObject newText = Instantiate(objectivesTextPrefab, middlePanel.transform);
        newText.GetComponent<TextMeshProUGUI>().text = text;
    }

    void UpdateObjectiveUI(Objective objective, int subObjectiveIndex, float delay)
    {
        objectiveMiddlePanel.SetActive(true);
        // Update current objective and sub-goal index
        currentObjective = objective;
        currentSubObjectiveIndex = subObjectiveIndex;
        // Show completion text before next objective if applicable
        if (currentObjective.completions.Count > 0 && currentObjective.subObjectives.Count != currentObjective.completions.Count)
        {
            // Show completion text for the most recent completed sub-goal (most recent is the one before this, so - 1)
            CreateObjectiveText(currentObjective.completions[currentSubObjectiveIndex - 1].completionText);
        }
        if (currentObjective.completions.Count > 0 && currentObjective.subObjectives.Count == currentObjective.completions.Count)
        {
            // Show completion text for the last completed sub-goal (this time just look at current)
            CreateObjectiveText(currentObjective.completions[currentSubObjectiveIndex].completionText);
        }

        // If the objective is already completed, do not update UI / clear texts
        Invoke(nameof(DelayedUpdateUI), delay);
    }

    void DelayedUpdateUI()
    {
        // Show completion text
        if (currentObjective.isCompleted)
        {
            ClearTextArea(middlePanel);
            CreateObjectiveText($"{currentObjective.name} has been completed!");
            return;
        }
        
        // Update UI texts
        nameText.text = currentObjective.name;
        ClearTextArea(middlePanel);

        if (ObjectivesManager.Instance.completedTutorial && currentSubObjectiveIndex == 0)
        {
            DeactivateTextArea();

            return; 
        }

        // Activate the current sub-objective
        if(currentObjective && currentObjective.subObjectives.Count > currentSubObjectiveIndex)
        {
            ObjectivesManager.OnActivateSubObjective(currentObjective?.subObjectives[currentSubObjectiveIndex]);
            CreateObjectiveText(currentObjective?.subObjectives[currentSubObjectiveIndex].goalText);
        }
    }

    void ClearTextArea(GameObject gameObject)
    {
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            Destroy(gameObject.transform.GetChild(i).gameObject);
        }
    }

    void TrackPaintings(string text, string objectiveName)
    {
        objectiveSidePanel.SetActive(true);
        nameText.text = objectiveName;
        ClearTextArea(sidePanel);
        GameObject newText = Instantiate(objectivesTextPrefab, sidePanel.transform);
        newText.GetComponent<TextMeshProUGUI>().text = text;
    }
}
