using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RiderSpawner : MonoBehaviour
{
    public List<GameObject> riderPrefabs;

    [Header("Low Density Group")]
    public List<Transform> lowDenseSpawnPoints;
    public List<Transform> lowDenseTargetPoints;
    public int ridersPerLowDenseSpawn = 3;
    public float lowDenseSpawnInterval = 2f;

    [Header("High Density Group")]
    public List<Transform> highDenseSpawnPoints;
    public List<Transform> highDenseTargetPoints;
    public int ridersPerHighDenseSpawn = 10;
    public float highDenseSpawnInterval = 0.8f;

    [Header("Spawn Settings")]
    public float spawnRadius = 1.5f;
    public float waitTimeAfterArrival = 3f;

    private void Start()
    {
        foreach (Transform spawn in lowDenseSpawnPoints)
        {
            StartCoroutine(SpawnFromGroup(
                spawn,
                lowDenseTargetPoints,
                ridersPerLowDenseSpawn,
                lowDenseSpawnInterval));
        }

        foreach (Transform spawn in highDenseSpawnPoints)
        {
            StartCoroutine(SpawnFromGroup(
                spawn,
                highDenseTargetPoints,
                ridersPerHighDenseSpawn,
                highDenseSpawnInterval));
        }
    }

    private IEnumerator SpawnFromGroup(
        Transform spawnPoint,
        List<Transform> targetPool,
        int riderCount,
        float interval)
    {
        for (int i = 0; i < riderCount; i++)
        {
            GameObject rider = SpawnRider(spawnPoint, targetPool);
            if (rider != null)
                StartCoroutine(MonitorAndDestroy(rider));
            yield return new WaitForSeconds(interval);
        }
    }

    private GameObject SpawnRider(Transform spawnPoint, List<Transform> targetPoints)
    {
        if (riderPrefabs.Count == 0 || targetPoints.Count <= 1)
            return null;

        GameObject prefab = riderPrefabs[Random.Range(0, riderPrefabs.Count)];

        List<Transform> validTargets = targetPoints
            .Where(tp => Vector3.Distance(tp.position, spawnPoint.position) > 1f)
            .ToList();

        if (validTargets.Count == 0)
            return null;

        Transform target = validTargets[Random.Range(0, validTargets.Count)];

        // === Spawn offset near spawn point ===
        Vector2 spawnOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = spawnPoint.position + new Vector3(spawnOffset.x, 0, spawnOffset.y);

        GameObject rider = Instantiate(prefab, spawnPosition, Quaternion.identity);

        // === Target offset near destination ===
        float arrivalRadius = 1f; // You can expose this in the Inspector if needed
        Vector2 targetOffset = Random.insideUnitCircle * arrivalRadius;
        Vector3 targetPosition = target.position + new Vector3(targetOffset.x, 0, targetOffset.y);

        NavMeshAgent agent = rider.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.SetDestination(targetPosition);
            if (agent != null)
            {
                agent.SetDestination(targetPosition);
                Debug.Log($"🧭 {rider.name} assigned to walk to: {targetPosition} from {spawnPosition}");
            }
            else
            {
                Debug.LogWarning($"❌ {rider.name} has no NavMeshAgent!");
            }

        return rider;
    }


    private IEnumerator MonitorAndDestroy(GameObject rider)
    {
        NavMeshAgent agent = rider.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogWarning("❌ Monitor failed: rider has no NavMeshAgent.");
            yield break;
        }

        Debug.Log($"👀 Monitoring {rider.name} for arrival...");

        // Wait until agent finishes moving
        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
        {
            Debug.Log($"⏳ {rider.name} moving... remaining distance: {agent.remainingDistance:F2}");
            yield return null;
        }

        Debug.Log($"✅ {rider.name} has arrived. Waiting {waitTimeAfterArrival} sec before destroy.");

        yield return new WaitForSeconds(waitTimeAfterArrival);

        Debug.Log($"💥 Destroying {rider.name}");
        Destroy(rider);
    }
}
