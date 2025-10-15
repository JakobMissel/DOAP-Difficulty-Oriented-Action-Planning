using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Objective", menuName = "Objectives/Objective")]
public class Objective : ScriptableObject
{
    public new string name;
    public List<SubObjective> goals;
    public List<SubObjective> completions;
}
