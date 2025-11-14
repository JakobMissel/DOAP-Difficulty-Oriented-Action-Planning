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
    [SerializeField] List<StealablePickup> paintings = new();
    [SerializeField] List<StealablePickup> stolenPaintings = new();

    StealablePickup currentPainting;
    GameObject wallPainting;

    public static Action<int, float> paintingStolen;
    public static void OnPaintingStolen(int subObjectiveIndex, float delay) => paintingStolen?.Invoke(subObjectiveIndex, delay);

    public static Action<GameObject> sendPaintingPrefab;
    public static void OnSendPaintingPrefab(GameObject painting) => sendPaintingPrefab?.Invoke(painting);

    void Start()
    {
        if(playerPaintingPosition == null)
            playerPaintingPosition = GameObject.FindGameObjectWithTag("PlayerPaintingPosition");
    }

    void OnEnable()
    {
        PlayerActions.stealItem += ItemStolen;
        paintingStolen += PaintingStolen;
        PlayerActions.paintingDelivered += PaintingDelivered;
        ObjectivesManager.displayObjective += CheckActive;
        CheckpointManager.loadCheckpoint += LoadCheckpoint;
    }

    void OnDisable()
    {
        PlayerActions.stealItem -= ItemStolen;
        paintingStolen -= PaintingStolen;
        PlayerActions.paintingDelivered -= PaintingDelivered;
        ObjectivesManager.displayObjective -= CheckActive;
        CheckpointManager.loadCheckpoint -= LoadCheckpoint;
    }

    void CheckActive(Objective objective, int arg2, float arg3)
    {
        if (objective != this.objective) return;
        UpdatePaintingNames();
    }

    void ItemStolen(StealablePickup painting)
    {
        if (objective.isActive)
        {
            for (int i = 0; i < objective.subObjectives.Count; i++)
            {
                if (objective.subObjectives[i].name.StartsWith(stealablePaintingName) && !objective.subObjectives[i].isCompleted)
                {
                    wallPainting = painting.gameObject;
                    currentPaintingName = painting.name;

                    objective.subObjectives[i].completionText = $"You have stolen the painting \"{painting.paintingName}\". Now place it outside.";
                    objective.subObjectives[i + 1].goalText = $"\"{painting.paintingName}\" by {painting.painterName} is valued at {painting.value}. Place it back at the entrance.";
                    objective.subObjectives[i + 1].completionText = $"\"{painting.paintingName}\" has been placed at the entrance.";
                    
                    wallPainting.gameObject.SetActive(false);
                    
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
        wallPainting = null;
        stolenPaintings.Add(currentPainting);
        paintings.Remove(currentPainting);
        if (playerPaintingPosition.transform.childCount > 2)
        {
            Destroy(playerPaintingPosition.transform.GetChild(2).gameObject);
        }
        objective.CompleteSubObjective(objective.currentSubObjectiveIndex);
        objective.DisplayNextSubObjective(0);
    }

    void AddPaintingToStolenList(string name)
    {
        for (int i = 0; i < paintings.Count; i++)
        {
            if (paintings[i].name == name)
            {
                currentPainting = paintings[i];
                GameObject newPainting = Instantiate(currentPainting.gameObject);
                OnSendPaintingPrefab(currentPainting.gameObject);
                newPainting.transform.SetParent(playerPaintingPosition.transform);
                newPainting.transform.localPosition = paintingPositionOffset;
                newPainting.transform.localRotation = Quaternion.Euler(paintingRotationOffset);
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
                for (int i = 0; i < paintings.Count; i++)
                {
                    var paintingText = $"<voffset=.4em><size=200%><sprite={i}></size></voffset>{paintings[i].paintingName}";
                    if (stolenPaintings.Contains(paintings[i]))
                    {
                        subObjective.goalText += $"<color=#8FCDA1>{paintingText}</color>\n"; // change color of stolen paintings
                    }
                    else
                    {
                        subObjective.goalText += $"{paintingText}\n";
                    }
                    paintingNames = subObjective.goalText;
                }
            }
        }
        ObjectivesManager.OnTrackPaintings(paintingNames, objective.name);
    }

    void LoadCheckpoint()
    {
        if(wallPainting != null)
        {
            wallPainting.SetActive(true);
            wallPainting = null;
        }
        if (playerPaintingPosition.transform.childCount > 2)
        { 
            Destroy(playerPaintingPosition.transform.GetChild(2).gameObject);
            currentPainting = null;
            currentPaintingName = "";
        }
    }
}
