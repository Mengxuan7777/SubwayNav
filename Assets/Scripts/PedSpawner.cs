using UnityEngine;

public class PedSpawner : MonoBehaviour
{
    public GameObject pedestrianPrefab;
    public Transform[] spawnPoints;
    public Transform[] pathParents;
    public int numberOfPedestrians = 10;

    // REMOVE Start()

    public void SpawnNPCsFromTrain()
    {
        for (int i = 0; i < numberOfPedestrians; i++)
        {
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
            Vector3 spawnPosition = spawnPoint.position;

            GameObject pedestrian = Instantiate(pedestrianPrefab, spawnPosition, Quaternion.identity);

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
