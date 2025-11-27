using UnityEngine;
using System.Collections.Generic;

public class CoinRespawner : MonoBehaviour
{
    public static CoinRespawner Instance;

    [SerializeField] List<GameObject> coins = new();

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (coins.Count < transform.childCount)
        for(int i = 0; i < transform.childCount; i++)
        {
            coins.Add(transform.GetChild(i).gameObject);
        }
    }

    void OnEnable()
    {
        CheckpointManager.loadCheckpoint += RespawnCoins;
    }

    void OnDisable()
    {
        CheckpointManager.loadCheckpoint -= RespawnCoins;
    }

    void RespawnCoins()
    {
        if (coins == null || coins.Count < transform.childCount)
        {
            Debug.LogWarning("No coins to respawn.");
            return;
        }
        foreach (GameObject coin in coins)
        {
            if (!coin.activeSelf)
            {
                coin.SetActive(true);
            }
        }
    }
}
