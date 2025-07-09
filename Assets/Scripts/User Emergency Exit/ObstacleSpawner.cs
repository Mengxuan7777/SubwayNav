using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

public class ObstacleSpawner : MonoBehaviour {
    // Start is called before the first frame update
    public GameObject[] obstacles;
    public GeneratePath user;
    
    // Prevent multiple spawns
    private bool isSpawning = false; 
    
    // Random Generator
    private readonly Random rnd = new Random();
    
    private void Start() {
        // Deactivate all obstacles
        foreach (var obj in obstacles) {
            obj.SetActive(false);
        }
        
        // Randomly select a obstacle to set 
        if (!isSpawning && obstacles is not null  && user) {
            StartCoroutine(PeriodicSpawn());
            isSpawning = true;
        }
    }
    
    private void Update() {
        Debug.Log(NavMesh.GetAreaCost(NavMesh.GetAreaFromName("Platform_NB_1")));
    }

    private IEnumerator PeriodicSpawn() {
        // Wait for at least a 1 second
        //int wait = rnd.Next(1, 10);
        var wait = 1;
        yield return new WaitForSeconds(wait);
        
        // pick a random obstacle
        // rnd.Next(0, obstacles.Length);
        var randomObstacle = 0;
        GameObject selectedObstacle = obstacles[randomObstacle];
        selectedObstacle.SetActive(true);
        user.UpdatePath(selectedObstacle.transform);
    }
}
