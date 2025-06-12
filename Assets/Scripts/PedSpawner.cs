using UnityEngine;

public class PedSpawner : MonoBehaviour
{
    [Header("Pedestrian Prefabs")]
    public GameObject[] pedestrianPrefabs; // Array of prefab options

    [Header("Spawn Configuration")]
    public Transform[] spawnPoints;
    public Transform[] pathParents;
    public int numberOfPedestrians = 10;

    public void SpawnNPCsFromTrain()
    {
        for (int i = 0; i < numberOfPedestrians; i++)
        {
            // Pick random prefab
            GameObject selectedPrefab = pedestrianPrefabs[Random.Range(0, pedestrianPrefabs.Length)];

            // Pick spawn point in order or randomly
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
            Vector3 spawnPosition = spawnPoint.position;

            // Instantiate selected prefab
            GameObject pedestrian = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);

            // Assign unique path
            WaypointFollower follower = pedestrian.GetComponent<WaypointFollower>();
            if (follower != null && i < pathParents.Length)
            {
                Transform pathParent = pathParents[i];
                Transform[] waypoints = new Transform[pathParent.childCount];

                for (int j = 0; j < waypoints.Length; j++)
                {
                    waypoints[j] = pathParent.GetChild(j);
                }

                follower.waypoints = waypoints;
            }
        }
    }
}

