using UnityEngine;
using UnityEngine.AI;

public class WaypointFollower : MonoBehaviour
{
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private NavMeshAgent agent;

    public bool loop = false; // Enable looping if desired

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        Debug.Log($"{gameObject.name} waypoints assigned: {waypoints?.Length}");


        if (waypoints != null && waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
        else
        {
            Debug.LogWarning("WaypointFollower: No waypoints assigned.");
        }
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0 || agent.pathPending)
            return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                currentWaypointIndex++;

                if (currentWaypointIndex < waypoints.Length)
                {
                    agent.SetDestination(waypoints[currentWaypointIndex].position);
                }
                else if (loop)
                {
                    currentWaypointIndex = 0;
                    agent.SetDestination(waypoints[currentWaypointIndex].position);
                }
                else
                {
                    // Stop movement
                    agent.isStopped = true;
                    enabled = false;
                }
            }
        }
    }
}
