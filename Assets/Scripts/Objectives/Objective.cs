using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Objective", menuName = "Objectives/Objective")]
public class Objective : ScriptableObject
{
    public bool isCompleted;
    public bool isActive;
    public new string name;
    public float completionDelay;
    public List<SubObjective> subObjectives;
    public List<SubObjective> completions;
    public int currentSubObjectiveIndex;
    public Objective nextObjective;

    public void BeginObjective()
    {
        isActive = true;
        if(subObjectives.Count > 0)
        {
            subObjectives[0].isActive = true;
            currentSubObjectiveIndex = 0;
        }
    }

    public void CompleteSubObjective(int subObjectiveIndex)
    {
        subObjectives[subObjectiveIndex].MarkAsCompleted();
        completions.Add(subObjectives[subObjectiveIndex]);
    }

    public void DisplayNextSubObjective(float delay)
    {
        for (int i = 0; i < subObjectives.Count; i++)
        {
            if (!subObjectives[i].isCompleted)
            {
                currentSubObjectiveIndex = i;
                ObjectivesManager.OnDisplayObjective(this, i, delay);
                break;
            }

            if(i == subObjectives.Count - 1)
            {
                isCompleted = true;
                currentSubObjectiveIndex = i;
                ObjectivesManager.OnDisplayObjective(this, i, delay);
                ObjectivesManager.OnCompleteObjective(this);
                break;
            }
        }
    }
}
