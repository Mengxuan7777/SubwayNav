using System.Collections;
using UnityEngine;
using BezierSolution;

public class TrainController : MonoBehaviour {
    public GameObject trainPrefab;
    public BezierSpline spline;

    public float intervalBetweenTrains = 30f;
    public float dwellTimeAtPlatform = 100f;
    public float platformStopT = 0.5f;  // Adjust this based on your spline
    public float stopThreshold = 0.01f;

    public PedSpawner npcSpawner;

    private void Start() {
        StartCoroutine(TrainScheduleRoutine());
    }

    IEnumerator TrainScheduleRoutine() {
        Debug.Log("🟢 TrainScheduleRoutine STARTED");

        // Spawn immediately
        yield return StartCoroutine(RunSingleTrain());

        while (true) {
            yield return new WaitForSeconds(intervalBetweenTrains);
            Debug.Log("🟠 Spawning train...");
            yield return StartCoroutine(RunSingleTrain());
        }
    }


    IEnumerator RunSingleTrain() {
        GameObject train = Instantiate(trainPrefab);
        BezierWalkerWithSpeed walker = train.GetComponent<BezierWalkerWithSpeed>();
        
        walker.spline = spline;
        walker.speed = 5f;

        bool hasStoppedAtPlatform = false;

        while (walker.NormalizedT < 1f) {
            Debug.Log("Train T: " + walker.NormalizedT.ToString("F3"));
            // Stop when close to platform T
            if (!hasStoppedAtPlatform && Mathf.Abs(walker.NormalizedT - platformStopT) < stopThreshold) {
                hasStoppedAtPlatform = true;
                float originalSpeed = walker.speed;
                walker.speed = 0f;

                // Spawn NPCs
                npcSpawner?.SpawnNPCsFromTrain();

                Debug.Log("Train is stopping at platform for " + dwellTimeAtPlatform + " seconds.");


                // Wait at platform
                yield return new WaitForSeconds(dwellTimeAtPlatform);

                walker.speed = originalSpeed; // resume movement
            }

            yield return null;
        }

        Destroy(train);
    }
}
