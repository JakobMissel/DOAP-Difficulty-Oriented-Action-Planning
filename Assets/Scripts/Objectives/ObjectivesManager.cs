using System;
using System.Collections;
using UnityEngine;

public class ObjectivesManager : MonoBehaviour
{
    [SerializeField] Objective currentObjective;
    public Objective CurrentObjective => currentObjective;

    [SerializeField] bool startFromFirstObjective = true;
    [SerializeField] public bool completedTutorial;

    [SerializeField] Objective[] objectives;
    public static ObjectivesManager Instance;

    public static Action<Objective, int, float, float> setNewObjective;
    public static void OnSetNewObjective(Objective newObjective, int subObjectiveIndex, float delay, float enumeratorDelay) => setNewObjective?.Invoke(newObjective, subObjectiveIndex, delay, enumeratorDelay);

    public static Action<SubObjective> activateSubObjective;
    public static void OnActivateSubObjective(SubObjective subObjective) => activateSubObjective?.Invoke(subObjective);

    public static Action<Objective> completeObjective;
    public static void OnCompleteObjective(Objective completedObjective) => completeObjective?.Invoke(completedObjective);

    public static Action<Objective, int, float> displayObjective;
    public static void OnDisplayObjective(Objective objective, int subGoalIndex, float delay) => displayObjective?.Invoke(objective, subGoalIndex, delay);


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
            if(objectives[i] == null)
            {
                Debug.LogWarning($"Objective at index {i} is null in ObjectivesManager.");
                continue;
            }
            objectives[i].isCompleted = false;
            objectives[i].isActive = false;
            objectives[i].completions.Clear();

            for (int j = 0; j < objectives[i].subObjectives.Count; j++) 
            {
                if(objectives[i].subObjectives[j] == null)
                {
                    Debug.LogWarning($"SubObjective at index {j} in Objective {objectives[i].name} is null in ObjectivesManager.");
                    continue;
                }
                objectives[i].subObjectives[j].isCompleted = false;
                objectives[i].subObjectives[j].isActive = false;
                objectives[i].subObjectives[j].descriptionText = objectives[i].subObjectives[j].goalText;
            }
        }
        completedTutorial = false;
    }

    void OnEnable()
    {
        setNewObjective += SetNewObjective;
        activateSubObjective += ActivateSubObjective;
        completeObjective += FlagCompletedObjective;
    }

    void OnDisable()
    {
        setNewObjective -= SetNewObjective;
        activateSubObjective -= ActivateSubObjective;
        completeObjective -= FlagCompletedObjective;
    }

    void Start()
    {
        if (objectives.Length > 0)
        {
            if (startFromFirstObjective)
                currentObjective = objectives[0];
            if(currentObjective != null)
                setNewObjective?.Invoke(currentObjective, 0, 0, 0);
        }
        else
        {
            Debug.LogWarning("No objectives set in ObjectivesManager.");
        }
    }

    void SetNewObjective(Objective newObjective, int subObjectiveIndex, float delay, float enumeratorDelay)
    {
        currentObjective = newObjective;
        currentObjective.isActive = false;
        StartCoroutine(SetNewObjectiveEnumerator(newObjective, subObjectiveIndex, delay, enumeratorDelay));
    }

    IEnumerator SetNewObjectiveEnumerator(Objective newObjective, int subObjectiveIndex, float delay, float enumeratorDelay)
    {
        yield return new WaitForSeconds(enumeratorDelay);
        currentObjective = newObjective;
        currentObjective.BeginObjective();
        OnDisplayObjective(currentObjective, subObjectiveIndex, delay);
        if (currentObjective == objectives[objectives.Length - 1])
        {
            PlayerActions.Instance.canEscape = true;
        }
    }

    void ActivateSubObjective(SubObjective subObjective)
    {
        subObjective.isActive = true;
    }

    void FlagCompletedObjective(Objective objective)
    {
        // If there is a next objective, set it and show after 2 seconds
        if(objective.nextObjective != null)
            StartCoroutine(SetNewObjectiveEnumerator(objective.nextObjective, 0, 0, objective.completionDelay));

    }
}
