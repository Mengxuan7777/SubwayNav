using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class RiderManager : MonoBehaviour
{
    [Header("Pool Settings")]
    public RiderPooler riderPool;

    [Header("Group Settings")]
    public RiderGroup southGroup;
    public RiderGroup northGroup;
    public RiderGroup circlingGroup;

    private List<RiderTask> activeRiders = new List<RiderTask>();

    void Start()
    {
        StartCoroutine(SpawnGroupRoutine(southGroup));
        StartCoroutine(SpawnGroupRoutine(northGroup));
        StartCoroutine(SpawnGroupRoutine(circlingGroup));
    }

    void Update()
    {
        for (int i = activeRiders.Count - 1; i >= 0; i--)
        {
            RiderTask rider = activeRiders[i];
            NavMeshAgent agent = rider.agent;

            if (agent.enabled &&
                !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance &&
                agent.velocity.magnitude == 0f)
            {
                ReturnToPool(rider);
                activeRiders.RemoveAt(i);
            }
        }
    }

    IEnumerator SpawnGroupRoutine(RiderGroup group)
    {
        while (true)
        {
            yield return new WaitForSeconds(group.spawnInterval);

            GameObject prefab = riderPool.GetRandomPrefab();
            if (prefab == null) continue;

            Transform spawn = group.spawnPoints[Random.Range(0, group.spawnPoints.Count)];
            Transform target = group.targetPoints[Random.Range(0, group.targetPoints.Count)];

            GameObject riderObj = riderPool.GetObject(prefab, spawn.position, Quaternion.identity);
            if (riderObj == null) continue;

            NavMeshAgent agent = riderObj.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogWarning("Rider prefab missing NavMeshAgent!");
                continue;
            }

            agent.enabled = true;
            agent.SetDestination(target.position);

            activeRiders.Add(new RiderTask
            {
                gameObject = riderObj,
                agent = agent,
                prefabRef = prefab
            });
        }
    }

    void ReturnToPool(RiderTask rider)
    {
        rider.agent.ResetPath();
        rider.agent.enabled = false;
        riderPool.ReturnObject(rider.prefabRef, rider.gameObject);
    }

    [System.Serializable]
    public class RiderGroup
    {
        public string groupName;
        public List<Transform> spawnPoints;
        public List<Transform> targetPoints;
        public float spawnInterval = 2f;
    }

    class RiderTask
    {
        public GameObject gameObject;
        public NavMeshAgent agent;
        public GameObject prefabRef;
    }
}
