using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;
    Spawnpoint[] spawnpoints;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(Instance.gameObject);
            Instance = this;
        }
        spawnpoints = GetComponentsInChildren<Spawnpoint>();
    }

    public int GetSpawnpointIdByRole(string role)
    {
        Spawnpoint spawnpoint;
        do spawnpoint = spawnpoints[Random.Range(0, spawnpoints.Length)];
        while (spawnpoint.role != role);
        return spawnpoint.id;
    }

    public Transform GetSpawnpointById(int _id)
    {
        Spawnpoint spawnpoint = null;

        foreach (Spawnpoint sp in spawnpoints)
            if (sp.id == _id)
                spawnpoint = sp;

        return spawnpoint.transform;
    }
}
