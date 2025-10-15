using TMPro;
using UnityEngine;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descriptionText;
    void OnEnable()
    {
        ObjectivesManager.onDisplayObjective += UpdateObjectiveUI;
    }

    void OnDisable()
    {
        ObjectivesManager.onDisplayObjective -= UpdateObjectiveUI;
    }

    void UpdateObjectiveUI(Objective objective)
    {
        nameText.text = objective.name;
        descriptionText.text = "";
        for (int i = 0; i < objective.goals.Count; i++)
        {
            descriptionText.text += objective.goals[i].description;
        }
    }
}
