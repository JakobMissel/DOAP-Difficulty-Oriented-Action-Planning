using System;
using System.Collections.Generic;
using UnityEngine;

public class StealPainting : MonoBehaviour
{
    GameObject playerPaintingPosition;
    [SerializeField] Vector3 paintingPositionOffset;
    [SerializeField] Vector3 paintingRotationOffset;
    [SerializeField] Objective objective;
    [SerializeField] string currentPaintingName;
    [SerializeField] StealablePickup[] paintings;
    [SerializeField] List<StealablePickup> stolenPaintings;

    public static Action<int, float> paintingStolen;
    public static void OnPaintingStolen(int subObjectiveIndex, float delay) => paintingStolen?.Invoke(subObjectiveIndex, delay);

    void Awake()
    {
        playerPaintingPosition = GameObject.FindGameObjectWithTag("PlayerPaintingPosition");
        UpdatePaintingNames();
    }

    void OnEnable()
    {
        PlayerActions.stealItem += ItemStolen;
        paintingStolen += PaintingStolen;
        PlayerActions.paintingDelivered += PaintingDelivered;
    }

    void OnDisable()
    {
        PlayerActions.stealItem -= ItemStolen;
        paintingStolen -= PaintingStolen;
        PlayerActions.paintingDelivered -= PaintingDelivered;
    }

    void ItemStolen(StealablePickup item)
    {
        if (objective.isActive)
        {
            for (int i = 0; i < objective.subObjectives.Count; i++)
            {
                if (objective.subObjectives[i].name.StartsWith("Painting") && !objective.subObjectives[i].isCompleted)
                {
                    currentPaintingName = item.paintingName;
                    objective.subObjectives[i].completionText = $"You have stolen the painting \"{currentPaintingName}\". Now place it outside.";
                    objective.subObjectives[i + 1].descriptionText = $"Place the painting \"{currentPaintingName}\" back at the entrance.";
                    objective.subObjectives[i + 1].completionText = $"\"{currentPaintingName}\" has been placed at the entrance.";
                    OnPaintingStolen(objective.currentSubObjectiveIndex, 8);
                    break;
                }
            }
        }
    }

    void PaintingStolen(int subObjectiveIndex, float delay)
    {
        print("sub objective: " + subObjectiveIndex);
        AddPaintingToStolenList(currentPaintingName);
        objective.CompleteSubObjective(subObjectiveIndex);
        objective.DisplayNextSubObjective(delay);
    }

    void PaintingDelivered()
    {
        UpdatePaintingNames();
        print("sub objective: " + objective.currentSubObjectiveIndex);
        objective.CompleteSubObjective(objective.currentSubObjectiveIndex);
        objective.DisplayNextSubObjective(5);
    }

    void AddPaintingToStolenList(string name)
    {
        for (int i = 0; i < paintings.Length; i++)
        {
            if (paintings[i].paintingName == name)
            {
                stolenPaintings.Add(paintings[i]);
                GameObject painting = Instantiate(paintings[i].gameObject, playerPaintingPosition.transform);
                painting.transform.localPosition = paintingPositionOffset;
                painting.transform.localRotation = Quaternion.Euler(paintingRotationOffset);
                break;
            }
        }
    }

    void UpdatePaintingNames()
    {
        if(playerPaintingPosition.transform.childCount > 0)
        {
            Destroy(playerPaintingPosition.transform.GetChild(0).gameObject);
        }
        foreach (var subObjective in objective.subObjectives)
        {
            if (subObjective.name.StartsWith("Painting"))
            {
                subObjective.descriptionText = "";
                print("Initializing painting sub objective: " + subObjective.name);
                for (int i = 0; i < paintings.Length; i++)
                {
                    if (stolenPaintings.Contains(paintings[i]))
                    {
                        print("Painting stolen: " + paintings[i].paintingName);
                        subObjective.descriptionText += $"<color=red>{paintings[i].paintingName}</color>\n";
                        continue;
                    }
                    else
                    {
                        subObjective.descriptionText += $"{paintings[i].paintingName}\n";
                    }
                }
            }
        }
    }
}
