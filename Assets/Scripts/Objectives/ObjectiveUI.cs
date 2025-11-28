using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] AudioClip[] audioClip;
    AudioSource audioSource;
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
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        ObjectivesManager.displayObjective += UpdateObjectiveUI;
        ObjectivesManager.trackPaintings += TrackPaintings;
        //PlayerActions.paintingDelivered += DeactivateTextArea;
    }


    void OnDisable()
    {
        ObjectivesManager.displayObjective -= UpdateObjectiveUI;
        ObjectivesManager.trackPaintings -= TrackPaintings;
        //PlayerActions.paintingDelivered -= DeactivateTextArea;
    }

    void DeactivateTextArea()
    {
        objectiveMiddlePanel.SetActive(false);
    }
    
    void CreateObjectiveText(string text)
    {
        GameObject newText = Instantiate(objectivesTextPrefab, middlePanel.transform);
        newText.GetComponent<TextMeshProUGUI>().text = text;
        newText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    }

    void NewSubObjectiveSound()
    {
        if (audioClip.Length == 0) return;
        audioSource.PlayOneShot(audioClip[0]);
    }

    void CompletedSubObjectiveSound()
    {
        if (audioClip.Length < 2) return;
        audioSource.PlayOneShot(audioClip[1]);
    }

    void UpdateObjectiveUI(Objective objective, int subObjectiveIndex, float delay)
    {
        ClearTextArea(middlePanel);
        objectiveMiddlePanel.SetActive(true);

        // Play completed sound except for the first sub-objective of the first objective
        if (objective == ObjectivesManager.Instance.objectives[0] && subObjectiveIndex >= 1 || objective != ObjectivesManager.Instance.objectives[0])
        {
            CompletedSubObjectiveSound();
        }

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
            DeactivateTextArea();
            yield break;
        }
        
        // Update UI texts
        nameText.text = currentObjective.name;
        ClearTextArea(middlePanel);

        // Special case: If the current objective is the second one (the objective where the player steals paintings) and the sub-objective contains "Golden" (need to pick up painting), do not show it
        if (currentObjective == ObjectivesManager.Instance.objectives[1] && currentObjective.subObjectives[currentSubObjectiveIndex].name.Contains("Golden"))
        {
            DeactivateTextArea();
            yield break; 
        }

        // Activate the current sub-objective
        if(currentObjective && currentObjective.subObjectives.Count > currentSubObjectiveIndex)
        {
            NewSubObjectiveSound();
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
