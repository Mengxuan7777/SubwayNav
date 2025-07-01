using System.Collections;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour {
    // Start is called before the first frame update
    public GameObject obstacle;
    public GeneratePath user;
    
    // Prevent multiple spawns
    private bool isSpawning = false; 

    private void Update() {
        if (!isSpawning && obstacle is not null  && user) {
            StartCoroutine(PeriodicSpawn());
            isSpawning = true;
        }
    }

    private IEnumerator PeriodicSpawn() {
        // Wait for obstacle to be recognized
        yield return new WaitForSeconds(1);
        
        while (true) {
            // Keep the obstacle hidden for 3 seconds
            obstacle.SetActive(false);
            yield return new WaitForSeconds(3);

            // Unhide the obstacle for 15 seconds
            obstacle.SetActive(true);
            yield return new WaitForSeconds(1);
            user.UpdatePath(obstacle.transform); // Update path for user
            yield return new WaitForSeconds(14);
        }
    }
}
