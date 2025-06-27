using UnityEngine;
using BezierSolution;
using System.Collections;

[RequireComponent(typeof(BezierWalkerWithSpeed))]
public class TrainController : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource trainPassbyAudio;  // Assign in Inspector
    public float minPitch = 0.1f;         // Pitch at full stop
    public float maxPitch = 0.5f;         // Pitch at max speed
    public float minVolume = 0.0f;        // Volume at full stop
    public float maxVolume = 0.5f;        // Volume at max speed
    private float currentVolume = 0f;
    public float audioFadeSpeed = 0.1f; 

    [Header("Train Settings")]
    public float stopNormalizedT = 0.85f;       // Point on spline where train should stop
    public float brakeOffsetT = 0.06f;          // How early (normalizedT) to start braking
    public float stopDuration = 30f;            // How long train stays stopped
    public float maxSpeed = 50f;                // Top speed
    public float accelerationTime = 3f;         // Time to speed up or slow down

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

        // Reset stop flag on loop wraparound
        if (currentT < lastT)
        {
            hasStoppedThisLoop = false;
        }
        lastT = currentT;

        if (!isStopping && !hasStoppedThisLoop)
        {
            float brakeStartT = stopNormalizedT - brakeOffsetT;

            if (currentT >= 0.5f && currentT >= brakeStartT && currentT <= stopNormalizedT)
            {
                Debug.Log($"🚉 Start braking at T = {currentT}");
                isStopping = true;
                StartCoroutine(SlowDownAndStop());
            }
        }
        
        UpdateTrainAudio();

    }

    IEnumerator SlowDownAndStop()
    {
        float startSpeed = walker.speed;
        float elapsed = 0f;

        while (elapsed < accelerationTime)
        {
            walker.speed = Mathf.Lerp(startSpeed, 0f, elapsed / accelerationTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        walker.speed = 0f;
        Debug.Log("Train stopped at T = " + walker.NormalizedT);

        UpdateTrainAudio();

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
        UpdateTrainAudio();

        isStopping = false;
        hasStoppedThisLoop = true;
    }

    private void UpdateTrainAudio()
    {
        if (trainPassbyAudio == null)
            return;

        float speedRatio = Mathf.Clamp01(walker.speed / maxSpeed);

        // Desired target volume and pitch
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, speedRatio);
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);

        // Smoothly interpolate volume
        currentVolume = Mathf.MoveTowards(currentVolume, targetVolume, Time.deltaTime * audioFadeSpeed);
        trainPassbyAudio.volume = currentVolume;
        trainPassbyAudio.pitch = targetPitch;

        // Play or pause only when needed
        if (currentVolume > 0.01f && !trainPassbyAudio.isPlaying)
        {
            trainPassbyAudio.Play();
        }
        else if (currentVolume <= 0.01f && trainPassbyAudio.isPlaying)
        {
            trainPassbyAudio.Pause();
        }
    }

}
