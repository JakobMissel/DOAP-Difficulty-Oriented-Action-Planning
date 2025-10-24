using System;
using UnityEngine;

public class ObjectivesManager : MonoBehaviour
{
    [SerializeField] Objective[] objectives;
    //int currentObjectiveIndex = 0;
    Objective currentObjective;
    public Objective CurrentObjective => currentObjective;
    public static ObjectivesManager Instance;

    public static Action<Objective> onSetNewObjective;
    public static void OnSetNewObjective(Objective newObjective) => onSetNewObjective?.Invoke(newObjective);

    public static Action<SubObjective> onActivateSubObjective;
    public static void OnActivateSubObjective(SubObjective subObjective) => onActivateSubObjective?.Invoke(subObjective);

    public static Action<Objective> onCompleteObjective;
    public static void OnCompleteObjective(Objective completedObjective) => onCompleteObjective?.Invoke(completedObjective);

    public static Action<Objective, int, float> onDisplayObjective;
    public static void OnDisplayObjective(Objective objective, int subGoalIndex, float delay) => onDisplayObjective?.Invoke(objective, subGoalIndex, delay);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        for (int i = 0; i < objectives.Length; i++) 
        {
            objectives[i].isCompleted = false;
            objectives[i].completions.Clear();

            for (int j = 0; j < objectives[i].goals.Count; j++) 
            {
                objectives[i].goals[j].isCompleted = false;
                objectives[i].goals[j].isActive = false;
                objectives[i].goals[j].descriptionText = objectives[i].goals[j].goalText;
            }
        }
    }

    void OnEnable()
    {
        onSetNewObjective += SetNewObjective;
        onActivateSubObjective += ActivateSubObjective;
    }

    void OnDisable()
    {
        onSetNewObjective -= SetNewObjective;
        onActivateSubObjective -= ActivateSubObjective;
    }

    void Start()
    {
        if (objectives.Length > 0)
        {
            currentObjective = objectives[0];
            onSetNewObjective?.Invoke(currentObjective);
        }
        else
        {
            Debug.LogWarning("No objectives set in ObjectivesManager.");
        }
    }

    void SetNewObjective(Objective newObjective)
    {
        currentObjective = newObjective;
        OnDisplayObjective(currentObjective, 0, 0);
    }

    void ActivateSubObjective(SubObjective subObjective)
    {
        subObjective.isActive = true;
    }
}
