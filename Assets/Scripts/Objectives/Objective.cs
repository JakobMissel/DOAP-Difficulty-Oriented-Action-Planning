using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Objective", menuName = "Objectives/Objective")]
public class Objective : ScriptableObject
{
    public bool isCompleted;
    public new string name;
    public List<SubObjective> goals;
    public List<SubObjective> completions;
    public Objective nextObjective;

    public void BeginObjective()
    {
        goals[0].isActive = true;
    }

    public void CompleteGoal(int goalIndex)
    {
        goals[goalIndex].MarkAsCompleted();
        completions.Add(goals[goalIndex]);
    }

    public void DisplayNextGoal(float delay)
    {
        for (int i = 0; i < goals.Count; i++)
        {
            if (!goals[i].isCompleted)
            {
                ObjectivesManager.OnDisplayObjective(this, i, delay);
                break;
            }

            if(i == goals.Count - 1)
            {
                isCompleted = true;
                ObjectivesManager.OnDisplayObjective(this, i, delay);
                ObjectivesManager.OnCompleteObjective(this);
                break;
            }
        }
    }
}
