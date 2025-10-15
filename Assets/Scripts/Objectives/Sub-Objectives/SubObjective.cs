using UnityEngine;

[CreateAssetMenu(fileName = "New Sub-Objective", menuName = "Objectives/Sub-Objective")]
public class SubObjective : ScriptableObject
{
    public bool isCompleted;
    public string description;
    [TextArea] public string goal;
    [TextArea] public string completion;

    public void MarkAsCompleted()
    {
        isCompleted = true;
        description = completion;
    }
}
