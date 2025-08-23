using UnityEngine;
using UnityEngine.AI;

public class PedestrianMovement : MonoBehaviour {
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private NavMeshAgent agent;

    public bool loop = false; // Enable looping if desired

    void Start() {
        agent = GetComponent<NavMeshAgent>();
        //Debug.Log($"{gameObject.name} waypoints assigned: {waypoints?.Length}");
        
        // Set Pedestrians initial destination
        if (waypoints != null && waypoints.Length > 0) {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        } else {
            // Debug.LogWarning("WaypointFollower: No waypoints assigned.");
        }
    }

    void Update() {
        // Avoid updating if no waypoints or if calculating path.
        if (waypoints == null || waypoints.Length == 0 || agent.pathPending)
            return;

        // Make Pedestrian move to the next waypoint. 
        if (agent.remainingDistance <= agent.stoppingDistance) {
            MoveToNextWaypoint();
        }
    }

    /// <summary>
    /// Sets pedestrian's next destination if close enough to the current destination.
    /// It enables loop of waypoints if 
    /// </summary>
    private void MoveToNextWaypoint() {
        if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f) {
            currentWaypointIndex++;

            if (currentWaypointIndex < waypoints.Length) {
                agent.SetDestination(waypoints[currentWaypointIndex].position);
            } else if (loop) {
                currentWaypointIndex = 0;
                agent.SetDestination(waypoints[currentWaypointIndex].position);
            } else {
                // Stop movement
                agent.isStopped = true;
                enabled = false;
            }
        }
    }
}
