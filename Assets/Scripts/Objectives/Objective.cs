using UnityEngine;

[CreateAssetMenu(fileName = "New Objective", menuName = "Objectives/Objective")]
public class Objective : ScriptableObject
{
    public new string name;
    [TextArea] public string description;
}
