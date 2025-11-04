using System;
using System.Collections.Generic;
using UnityEngine;

public class StealPainting : MonoBehaviour
{
    [SerializeField] string stealablePaintingName;
    [SerializeField] GameObject playerPaintingPosition;
    [SerializeField] Vector3 paintingPositionOffset;
    [SerializeField] Vector3 paintingRotationOffset;
    [SerializeField] Objective objective;
    [SerializeField] string currentPaintingName;
    [SerializeField] StealablePickup[] paintings;
    [SerializeField] List<StealablePickup> stolenPaintings;

    StealablePickup currentPainting;

    public static Action<int, float> paintingStolen;
    public static void OnPaintingStolen(int subObjectiveIndex, float delay) => paintingStolen?.Invoke(subObjectiveIndex, delay);

    public static Action<GameObject> sendPaintingPrefab;
    public static void OnSendPaintingPrefab(GameObject painting) => sendPaintingPrefab?.Invoke(painting);

    void Awake()
    {
        playerPaintingPosition = GameObject.FindGameObjectWithTag("PlayerPaintingPosition");
    }

    void OnEnable()
    {
        PlayerActions.stealItem += ItemStolen;
        paintingStolen += PaintingStolen;
        PlayerActions.paintingDelivered += PaintingDelivered;
        ObjectivesManager.displayObjective += CheckActive;
    }

    void OnDisable()
    {
        PlayerActions.stealItem -= ItemStolen;
        paintingStolen -= PaintingStolen;
        PlayerActions.paintingDelivered -= PaintingDelivered;
        ObjectivesManager.displayObjective -= CheckActive;
    }

    void CheckActive(Objective objective, int arg2, float arg3)
    {
        if (objective != this.objective) return;
        UpdatePaintingNames();
    }

    void ItemStolen(StealablePickup item)
    {
        if (objective.isActive)
        {
            for (int i = 0; i < objective.subObjectives.Count; i++)
            {
                if (objective.subObjectives[i].name.StartsWith(stealablePaintingName) && !objective.subObjectives[i].isCompleted)
                {
                    currentPaintingName = item.paintingName;
                    objective.subObjectives[i].completionText = $"You have stolen the painting \"{currentPaintingName}\". Now place it outside.";
                    objective.subObjectives[i + 1].goalText = $"Place the painting \"{currentPaintingName}\" back at the entrance.";
                    objective.subObjectives[i + 1].completionText = $"\"{currentPaintingName}\" has been placed at the entrance.";
                    OnPaintingStolen(objective.currentSubObjectiveIndex, 0);
                    break;
                }
            }
        }
    }

    void PaintingStolen(int subObjectiveIndex, float delay)
    {
        AddPaintingToStolenList(currentPaintingName);
        objective.CompleteSubObjective(subObjectiveIndex);
        objective.DisplayNextSubObjective(delay);
    }

    void PaintingDelivered()
    {
        // Add to "stolen list" and destroy the carried painting accounting for its siblings.
        stolenPaintings.Add(currentPainting);
        Destroy(playerPaintingPosition.transform.GetChild(2).gameObject);
        objective.CompleteSubObjective(objective.currentSubObjectiveIndex);
        objective.DisplayNextSubObjective(0);
    }

    void AddPaintingToStolenList(string name)
    {
        for (int i = 0; i < paintings.Length; i++)
        {
            if (paintings[i].paintingName == name)
            {
                currentPainting = paintings[i];
                GameObject painting = Instantiate(currentPainting.gameObject);
                OnSendPaintingPrefab(currentPainting.gameObject);
                painting.transform.SetParent(playerPaintingPosition.transform);
                painting.transform.localPosition = paintingPositionOffset;
                painting.transform.localRotation = Quaternion.Euler(paintingRotationOffset);
                break;
            }
        }
    }


    void UpdatePaintingNames()
    {
        var paintingNames = "";
        foreach (var subObjective in objective.subObjectives)
        {
            if (subObjective.name.StartsWith(stealablePaintingName))
            {
                subObjective.goalText = "";
                for (int i = 0; i < paintings.Length; i++)
                {
                    if (stolenPaintings.Contains(paintings[i]))
                    {
                        subObjective.goalText += $"<color=#8FCDA1>{paintings[i].paintingName}</color>\n"; // change color of stolen paintings
                        continue;
                    }
                    else
                    {
                        subObjective.goalText += $"{paintings[i].paintingName}\n";
                    }
                    paintingNames = subObjective.goalText;
                }
            }
        }
        ObjectivesManager.OnTrackPaintings(paintingNames, objective.name);
    }
}
