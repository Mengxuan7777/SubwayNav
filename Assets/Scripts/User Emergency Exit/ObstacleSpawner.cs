using System.Collections;
using UnityEngine;
using Random = System.Random;

public class ObstacleSpawner : MonoBehaviour {
    // Start is called before the first frame update
    public GameObject[] obstacle;
    public GeneratePath user;
    
    // Prevent multiple spawns
    private bool isSpawning = false; 
    
    // Random Generator
    private readonly Random rnd = new Random();

    private void Update() {
        // Deactivate all obstacles
        foreach (var obj in obstacle) {
            obj.SetActive(false);
        }
        
        // Randomly select a obstacle to set 
        if (!isSpawning && obstacle is not null  && user) {
            StartCoroutine(PeriodicSpawn());
            isSpawning = true;
        }
    }

    private IEnumerator PeriodicSpawn() {
        // Wait for at least a 1 second
        //int wait = rnd.Next(1, 10);
        var wait = 1;
        yield return new WaitForSeconds(wait);
        
        // pick a random obstacle
        //var randomObstacle = rnd.Next(0, obstacle.Length);
        var randomObstacle = 0;
        GameObject selectedObstacle = obstacle[randomObstacle];
        selectedObstacle.SetActive(true);
        user.UpdatePath(selectedObstacle.transform);
    }
}
