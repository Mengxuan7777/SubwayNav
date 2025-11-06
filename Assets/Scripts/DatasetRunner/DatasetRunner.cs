using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class DatasetRunnerZones : MonoBehaviour
{
    [System.Serializable]
    public class CameraSetup
    {
        public string cameraId;                // e.g. "EX1", "DP3", "UP5"
        public Camera camera;                  // actual Camera component
        public Transform fireSpot;             // child named "FireSpot"
        public BoxCollider pedestrianZone;     // child named "PedestrianZone"
    }

    public string cameraRootName = "SurvCamera";  // <- this matches your hierarchy
    public GameObject firePrefab;
    public GameObject[] npcPrefabs;

    public string outputImagePath = "C:/Dataset/Images";
    public string outputLabelPath = "C:/Dataset/labels.jsonl";

    public int minFire = 0;
    public int maxFire = 3;
    public int minNPC = 0;
    public int maxNPC = 8;

    public float npcYOffset = 0f;
    public float fireScalePerLevel = 0.5f;

    private List<CameraSetup> cameraSetups = new List<CameraSetup>();
    private List<GameObject> spawnedNPCs = new List<GameObject>();
    private GameObject spawnedFire;
    private int imageCounter = 0;

    void Start()
    {
        AutoDiscoverFromParent();
        if (!Directory.Exists(outputImagePath))
            Directory.CreateDirectory(outputImagePath);

        StartCoroutine(Run());
    }

    // 🔍 find "SurvCamera" and use all its children as surveillance cameras
    void AutoDiscoverFromParent()
    {
        cameraSetups.Clear();

        GameObject root = GameObject.Find(cameraRootName);
        if (root == null)
        {
            Debug.LogError("DatasetRunnerZones: cannot find root object named " + cameraRootName);
            return;
        }

        // iterate direct children under SurvCamera
        foreach (Transform child in root.transform)
        {
            // child should have a Camera component somewhere (usually on itself)
            Camera cam = child.GetComponent<Camera>();
            if (cam == null)
            {
                cam = child.GetComponentInChildren<Camera>(true);
            }

            if (cam == null)
            {
                Debug.LogWarning("No Camera component found under " + child.name + ", skipping.");
                continue;
            }

            CameraSetup setup = new CameraSetup();
            setup.cameraId = child.name;  // keep your naming: EX1, DS2, DP5...
            setup.camera = cam;

            // look for FireSpot and PedestrianZone under this camera object
            Transform[] descendants = child.GetComponentsInChildren<Transform>(true);
            foreach (Transform d in descendants)
            {
                if (d.name == "FireSpot")
                {
                    setup.fireSpot = d;
                }
                else if (d.name == "PedestrianZone")
                {
                    BoxCollider bc = d.GetComponent<BoxCollider>();
                    if (bc != null)
                        setup.pedestrianZone = bc;
                }
            }

            cameraSetups.Add(setup);
        }

        Debug.Log("Discovered " + cameraSetups.Count + " surveillance cameras under " + cameraRootName);
    }

    IEnumerator Run()
    {
        using (StreamWriter writer = new StreamWriter(outputLabelPath, false))
        {
            foreach (var camSetup in cameraSetups)
            {
                for (int fire = minFire; fire <= maxFire; fire++)
                {
                    for (int npcCount = minNPC; npcCount <= maxNPC; npcCount++)
                    {
                        ClearSpawned();

                        SpawnFire(camSetup, fire);
                        SpawnNPCsInZone(camSetup, npcCount);

                        yield return new WaitForEndOfFrame();

                        string fileName = string.Format("{0}_fire-{1}_npc-{2}_{3:D4}.png",
                            camSetup.cameraId, fire, npcCount, imageCounter);
                        string fullPath = Path.Combine(outputImagePath, fileName);

                        CaptureFromCamera(camSetup.camera, fullPath);
                        imageCounter++;

                        // write label line
                        var labelObj = new
                        {
                            camera_id = camSetup.cameraId,
                            fire_intensity = fire,
                            npc_count = npcCount,
                            image = fileName
                        };
                        writer.WriteLine(JsonUtility.ToJson(labelObj));
                        writer.Flush();

                        yield return null;
                    }
                }
            }
        }

        Debug.Log("✅ Dataset generation finished.");
    }

    void SpawnFire(CameraSetup setup, int fireLevel)
    {
        if (fireLevel == 0) return;
        if (firePrefab == null) return;
        if (setup.fireSpot == null) return;

        spawnedFire = Instantiate(firePrefab, setup.fireSpot.position, setup.fireSpot.rotation);

        ParticleSystem ps = spawnedFire.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startSize = main.startSize.constant * (1f + fireScalePerLevel * (fireLevel - 1));
        }

        Light l = spawnedFire.GetComponentInChildren<Light>();
        if (l != null)
        {
            l.intensity = 1f * fireLevel;
        }
    }

    void SpawnNPCsInZone(CameraSetup setup, int count)
    {
        if (count <= 0) return;
        if (npcPrefabs == null || npcPrefabs.Length == 0) return;
        if (setup.pedestrianZone == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = GetRandomPointInBox(setup.pedestrianZone);
            pos.y += npcYOffset;

            GameObject prefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
            GameObject npc = Instantiate(prefab, pos, Quaternion.identity);
            spawnedNPCs.Add(npc);
        }
    }

    Vector3 GetRandomPointInBox(BoxCollider box)
    {
        Vector3 center = box.transform.TransformPoint(box.center);
        Vector3 size = Vector3.Scale(box.size, box.transform.lossyScale);

        float x = center.x + (Random.value - 0.5f) * size.x;
        float y = center.y + (Random.value - 0.5f) * size.y;
        float z = center.z + (Random.value - 0.5f) * size.z;

        return new Vector3(x, y, z);
    }

    void ClearSpawned()
    {
        if (spawnedFire != null)
        {
            Destroy(spawnedFire);
            spawnedFire = null;
        }

        for (int i = 0; i < spawnedNPCs.Count; i++)
        {
            if (spawnedNPCs[i] != null)
                Destroy(spawnedNPCs[i]);
        }
        spawnedNPCs.Clear();
    }

    void CaptureFromCamera(Camera cam, string path)
    {
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("Captured " + path);
    }
}
