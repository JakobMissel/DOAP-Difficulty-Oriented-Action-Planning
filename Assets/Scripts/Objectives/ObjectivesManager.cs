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


    public static Action<Objective> onDisplayObjective;
    public static void OnUpdateObjective(Objective objective) => onDisplayObjective?.Invoke(objective);

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
    }

    void OnEnable()
    {
        onSetNewObjective += SetNewObjective;
    }

    void OnDisable()
    {
        onSetNewObjective -= SetNewObjective;
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
        OnUpdateObjective(currentObjective);
    }
}
