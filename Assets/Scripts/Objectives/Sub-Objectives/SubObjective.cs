using UnityEngine;

[CreateAssetMenu(fileName = "New Sub-Objective", menuName = "Objectives/Sub-Objective")]
public class SubObjective : ScriptableObject
{
    public bool isCompleted;
    public bool isActive;
    [TextArea] public string descriptionText;
    [TextArea] public string goalText;
    [TextArea] public string completionText;

    public void MarkAsCompleted()
    {
        isCompleted = true;
    }
}
