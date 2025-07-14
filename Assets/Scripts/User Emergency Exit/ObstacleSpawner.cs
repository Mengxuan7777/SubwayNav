using System.Collections;
using UnityEngine;
using Random = System.Random;

public class ObstacleSpawner : MonoBehaviour {
    // Start is called before the first frame update
    [Header("Fire Locations")]
    public GameObject[] obstacles;
    
    [Header("Fire Spawn Parameters - Fire 1")]
    public int fireIndex1 = -1;                 // Index of first fire to spawn
    public float InitialFireSize1 = 0;          // Intensity it is initially set
    public float UpdateFireSize1 = 0;           // Intensity fire is set on after update delay
    public float fireSpawnDelay1 = 0.1f;             // Delay between start and the fire spawning
    public float fireUpdateDelay1 = 0;            // Delay between the fire first spawning and the intensity being updated
    public bool SizeUpdateIncremental1 = false; // Makes the increase in intensity happen slowly
    
    [Header("Fire Spawn Parameters - Fire 2")]
    public int fireIndex2 = -1;                 // Index of second fire to spawn
    public float InitialFireSize2 = 0;          // Intensity it is initially set
    public float UpdateFireSize2 = 0;           // Intensity fire is set on after update delay
    public float fireSpawnDelay2 = 0.1f;             // Delay between start and the fire spawning
    public float fireUpdateDelay2 = 0;            // Delay between the fire first spawning and the intensity being updated
    public bool SizeUpdateIncremental2 = false; // Makes the increase in intensity happen slowly
    
    // Random Generator
    private readonly Random rnd = new Random();
    
    private void Start() {
        // Deactivate all obstacles
        foreach (var obj in obstacles) {
            obj.SetActive(false);
        }
        
        // Spawn Fires
        if (obstacles is not null) {
            // Spawn first fire
            StartCoroutine(FireSpawn(fireIndex1,fireSpawnDelay1, fireUpdateDelay1, SizeUpdateIncremental1, InitialFireSize1, UpdateFireSize1 ));
            
            if (fireIndex2 != -1 && fireIndex2 != fireIndex1 && fireIndex2 > 0) {
                // Spawn second fire, if one is selected
                StartCoroutine(FireSpawn(fireIndex2, fireSpawnDelay2, fireUpdateDelay2, SizeUpdateIncremental2, InitialFireSize2, UpdateFireSize2));
            }
        }
    }

    private IEnumerator FireSpawn(int index, float spawnDelay, float updateDelay, bool incremental, float fireSize1, float fireSize2) {
        // Spawn delay
        yield return new WaitForSeconds(spawnDelay);
        
        // Pick a random obstacle, if not defined
        if (index == -1) { index = rnd.Next(0, obstacles.Length); }
        GameObject selectedObstacle = obstacles[index];
        selectedObstacle.SetActive(true);
        
        // Change size of fire if was pre-determined
        if (fireSize1 != 0) { selectedObstacle.transform.localScale = new Vector3(fireSize1, fireSize1, fireSize1); }
        
        // Delay for the second update
        if (fireSize2 != 0) {
            if (incremental) {
                var num = 20;
                var delay = updateDelay / num;
                var fireIncrement = (fireSize2 - fireSize1) / num;
                for (int i = 0; i < num; i++) {
                    // ReSharper disable once PossibleLossOfFraction
                    // small delay
                    yield return new WaitForSeconds(delay);
                    var inc = fireSize1 + fireIncrement * (i+1);
                    selectedObstacle.transform.localScale = new Vector3(inc, inc, inc);
                }
            } else {
                // Wait and update size all at once
                yield return new WaitForSeconds(updateDelay);
                selectedObstacle.transform.localScale = new Vector3(fireSize2, fireSize2, fireSize2);
            }
        }
    }
}
