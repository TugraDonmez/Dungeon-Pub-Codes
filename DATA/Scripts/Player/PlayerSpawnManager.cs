using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    void Start()
    {
        string spawnId = PlayerPrefs.GetString("spawnId", "default");

        SpawnPoint[] points = FindObjectsOfType<SpawnPoint>();
        foreach (var point in points)
        {
            if (point.spawnId == spawnId)
            {
                transform.position = point.transform.position;
                break;
            }
        }
    }
}
