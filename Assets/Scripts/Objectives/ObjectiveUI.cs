using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [Header("Middle")]
    [SerializeField] GameObject objectivesTextPrefab;
    [SerializeField] GameObject middlePanel;
    [SerializeField] GameObject objectiveMiddlePanel;
    [SerializeField] Image delayBar;
    [Header("Side")]
    [SerializeField] GameObject sidePanel;
    [SerializeField] GameObject objectiveSidePanel;

    Objective currentObjective;
    int currentSubObjectiveIndex = 0;
    float delayBarProgress;


    void Awake()
    {
        objectiveMiddlePanel.SetActive(false);
        objectiveSidePanel.SetActive(false);
        delayBar.fillAmount = 0;
    }

    void OnEnable()
    {
        ObjectivesManager.displayObjective += UpdateObjectiveUI;
        ObjectivesManager.trackPaintings += TrackPaintings;
        PlayerActions.paintingDelivered += DeactivateTextArea;
    }


    void OnDisable()
    {
        ObjectivesManager.displayObjective -= UpdateObjectiveUI;
        ObjectivesManager.trackPaintings -= TrackPaintings;
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
        ClearTextArea(middlePanel);
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
        StartCoroutine(DelayedUpdateUI(delay));
    }

    IEnumerator DelayedUpdateUI(float delay)
    {
        while (delayBarProgress < delay)
        {
            delayBarProgress += Time.deltaTime;
            delayBar.fillAmount = Mathf.Lerp(0, 1, delayBarProgress / delay);
            yield return null;
        }
        delayBarProgress = 0;
        delayBar.fillAmount = 0;
        // Show completion text
        if (currentObjective.isCompleted)
        {
            ClearTextArea(middlePanel);
            CreateObjectiveText($"{currentObjective.name} has been completed!");
            yield break;
        }
        
        // Update UI texts
        nameText.text = currentObjective.name;
        ClearTextArea(middlePanel);

        if (ObjectivesManager.Instance.completedTutorial && currentObjective.subObjectives[currentSubObjectiveIndex].name.Contains("Golden"))
        {
            DeactivateTextArea();
            yield break; 
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
