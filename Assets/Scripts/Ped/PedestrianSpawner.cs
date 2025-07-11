using UnityEngine;
using System.Collections;

public class PedestrianSpawner : MonoBehaviour {
    
    [Header("Pedestrian Prefabs")]
    public GameObject[] pedestrianPrefabs;

    [Header("Spawn Configuration")]
    public Transform[] spawnPoints;       // Only 2 spawn points
    public Transform[] pathParents;
    public int npcsPerSpawnPoint = 5;
    public float spawnInterval = 2f;      // Delay between spawns per point

    public void SpawnNPCsFromTrain() {
        // Start coroutine for each spawn point
        for (int i = 0; i < spawnPoints.Length; i++) {
            StartCoroutine(SpawnFromPoint(i));
        }
    }

    private IEnumerator SpawnFromPoint(int spawnIndex) {
        Transform spawnPoint = spawnPoints[spawnIndex];

        for (int i = 0; i < npcsPerSpawnPoint; i++) {
            // Pick random prefab
            GameObject selectedPrefab = pedestrianPrefabs[Random.Range(0, pedestrianPrefabs.Length)];
            Vector3 spawnPosition = spawnPoint.position;

            // Instantiate NPC
            GameObject pedestrian = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);

            // Assign random path
            PedestrianMovement follower = pedestrian.GetComponent<PedestrianMovement>();
            if (follower != null && pathParents.Length > 0) {
                Transform pathParent = pathParents[Random.Range(0, pathParents.Length)];
                Transform[] waypoints = new Transform[pathParent.childCount];

                for (int j = 0; j < waypoints.Length; j++) {
                    waypoints[j] = pathParent.GetChild(j);
                }

                follower.waypoints = waypoints;
            }

            yield return new WaitForSeconds(spawnInterval); // Delay before next NPC from this point
        }
    }
}
