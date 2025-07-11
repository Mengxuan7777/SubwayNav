using System.Collections;
using UnityEngine;
using Random = System.Random;

public class ObstacleSpawner : MonoBehaviour {
    // Start is called before the first frame update
    [Header("Fire Locations")]
    public GameObject[] obstacles;
    
    // Prevent multiple spawns
    [Header("Spawn Settings")]
    private bool isSpawning = false;
    public int fireIndex = -1;
    public float fireSize = 0;
    
    // Random Generator
    private readonly Random rnd = new Random();
    
    private void Start() {
        // Deactivate all obstacles
        foreach (var obj in obstacles) {
            obj.SetActive(false);
        }
        
        // Randomly select a obstacle to set 
        if (!isSpawning && obstacles is not null) {
            StartCoroutine(PeriodicSpawn());
            isSpawning = true;
        }
    }

    private IEnumerator PeriodicSpawn() {
        // Wait for at least a 1 second
        int wait = rnd.Next(1, 4);
        yield return new WaitForSeconds(wait);
        
        // Pick a random obstacle, if not defined
        if (fireIndex == -1) {
            fireIndex = rnd.Next(0, obstacles.Length);
        }
        GameObject selectedObstacle = obstacles[fireIndex];
        selectedObstacle.SetActive(true);
        
        
        // Change size of fire if was pre-determined
        if (fireSize != 0) {
            selectedObstacle.transform.localScale = new Vector3(fireSize, fireSize, fireSize);
        }
    }
}
