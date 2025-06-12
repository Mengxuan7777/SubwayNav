using UnityEngine;
using BezierSolution;
using System.Collections;

[RequireComponent(typeof(BezierWalkerWithSpeed))]
public class TrainController : MonoBehaviour
{
    [Header("Train Settings")]
    public float stopNormalizedT = 0.25f;   // Normalized point to stop
    public float stopDuration = 30f;
    public float maxSpeed = 50f;
    public float accelerationTime = 3f;     // Time to speed up/down

    [Header("NPC Spawner")]
    public PedSpawner pedSpawner;

    private BezierWalkerWithSpeed walker;
    private float lastT = 0f;
    private bool isStopping = false;
    private bool hasStoppedThisLoop = false;

    void Start()
    {
        walker = GetComponent<BezierWalkerWithSpeed>();

        if (walker.spline == null)
        {
            Debug.LogError("TrainController: Spline not assigned!");
            enabled = false;
            return;
        }

        walker.speed = maxSpeed;
    }

    void Update()
    {
        float currentT = walker.NormalizedT;

        if (currentT < lastT)
        {
            hasStoppedThisLoop = false;
        }
        lastT = currentT;

        if (isStopping || hasStoppedThisLoop)
            return;

        if (currentT >= stopNormalizedT)
        {
            isStopping = true;

            StartCoroutine(SlowDownAndStop());
        }
    }

    IEnumerator SlowDownAndStop()
    {
        // Smoothly reduce speed to 0
        float startSpeed = walker.speed;
        float elapsed = 0f;

        while (elapsed < accelerationTime)
        {
            walker.speed = Mathf.Lerp(startSpeed, 0f, elapsed / accelerationTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        walker.speed = 0f;

        if (pedSpawner != null)
            pedSpawner.SpawnNPCsFromTrain();

        yield return new WaitForSeconds(stopDuration);

        StartCoroutine(SpeedUp());
    }

    IEnumerator SpeedUp()
    {
        float elapsed = 0f;

        while (elapsed < accelerationTime)
        {
            walker.speed = Mathf.Lerp(0f, maxSpeed, elapsed / accelerationTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        walker.speed = maxSpeed;
        isStopping = false;
        hasStoppedThisLoop = true;
    }
}
