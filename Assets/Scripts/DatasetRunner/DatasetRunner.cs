using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class DatasetRunnerZones : MonoBehaviour
{
    [System.Serializable]
    public class CameraSetup
    {
        public string cameraId;
        public Camera camera;
        public Transform fireSpot;
        public BoxCollider npcSpawnZone;
    }

    [System.Serializable]
    public class LabelEntry
    {
        public string camera_id;
        public int fire_intensity;
        public int npc_count;
        public string image;
    }


    [Header("Scene Root")]
    public string cameraRootName = "SurvCamera (1)"; // parent object of all 36 cameras

    [Header("Prefabs")]
    public GameObject firePrefab;
    public GameObject[] npcPrefabs;

    [Header("Output Settings")]
    public string outputImagePath = "C:/Dataset/Images";
    public string outputLabelPath = "C:/Dataset/labels.jsonl";

    [Header("Simulation Settings")]
    public int minFire = 0;
    public int maxFire = 2;
    public int minNPC = 0;
    public int maxNPC = 8;

    public float fireRadius = 0.6f; // radius around FireSpot for additional fires
    public float npcYOffset = 0f;   // adjust NPC spawn height
    public float npcSpawnHeightCheck = 2f; // raycast downward to ground
    public bool snapNPCToGround = true;

    private List<CameraSetup> cameraSetups = new List<CameraSetup>();
    private List<GameObject> spawnedFires = new List<GameObject>();
    private List<GameObject> spawnedNPCs = new List<GameObject>();
    private int imageCounter = 0;

    void Start()
    {
        AutoDiscoverCameras();

        if (!Directory.Exists(outputImagePath))
            Directory.CreateDirectory(outputImagePath);

        StartCoroutine(RunDataset());
    }

    // 🔍 Automatically detect all surveillance cameras under "SurvCamera"
    void AutoDiscoverCameras()
    {
        cameraSetups.Clear();

        GameObject root = GameObject.Find(cameraRootName);
        if (root == null)
        {
            Debug.LogError($"❌ Cannot find object named '{cameraRootName}' in scene!");
            return;
        }

        foreach (Transform child in root.transform)
        {
            // find or create a camera
            Camera cam = child.GetComponent<Camera>();
            if (cam == null)
            {
                GameObject tempCamObj = new GameObject(child.name + "_TempCam");
                tempCamObj.transform.SetPositionAndRotation(child.position, child.rotation);
                cam = tempCamObj.AddComponent<Camera>();

                // Optionally parent under the dataset runner (keeps hierarchy clean)
                tempCamObj.transform.parent = transform;
            }

            Transform fireSpot = null;
            BoxCollider npcZone = null;

            foreach (Transform sub in child.GetComponentsInChildren<Transform>(true))
            {
                if (sub.name == "FireSpot") fireSpot = sub;
                if (sub.name == "NPCzone" || sub.name == "NPCSpawnZone")
                    npcZone = sub.GetComponent<BoxCollider>();
            }

            if (fireSpot == null || npcZone == null)
            {
                Debug.LogWarning($"⚠️ {child.name} missing FireSpot or NPCzone — skipped.");
                continue;
            }

            cameraSetups.Add(new CameraSetup
            {
                cameraId = child.name,
                camera = cam,
                fireSpot = fireSpot,
                npcSpawnZone = npcZone
            });
        }

        Debug.Log($"✅ Found {cameraSetups.Count} surveillance nodes under '{cameraRootName}'.");
    }


    IEnumerator RunDataset()
    {
        using (StreamWriter writer = new StreamWriter(outputLabelPath, false))
        {
            foreach (var camSetup in cameraSetups)
            {
                for (int fire = minFire; fire <= maxFire; fire++)
                {
                    for (int npc = minNPC; npc <= maxNPC; npc++)
                    {
                        ClearSpawned(); // clean before each step

                        SpawnFires(camSetup.fireSpot, fire);
                        SpawnNPCsInZone(camSetup.npcSpawnZone, npc, camSetup.fireSpot.position, 1.5f);


                        yield return new WaitForEndOfFrame(); // let scene render

                        string fileName = $"{camSetup.cameraId}_fire-{fire}_npc-{npc}_{imageCounter:D4}.png";
                        string fullPath = Path.Combine(outputImagePath, fileName);
                        CaptureFromCamera(camSetup.camera, fullPath);
                        imageCounter++;

                        // Write label line
                        LabelEntry label = new LabelEntry
                        {
                            camera_id = camSetup.cameraId,
                            fire_intensity = fire,
                            npc_count = npc,
                            image = fileName
                        };
                        writer.WriteLine(JsonUtility.ToJson(label));


                        yield return null;
                    }
                }
            }
        }

        Debug.Log("🎉 Dataset generation complete!");
    }

    // 🔥 Spawn multiple fire prefabs to indicate intensity
    void SpawnFires(Transform fireSpot, int fireLevel)
    {
        if (fireLevel <= 0) return;
        if (firePrefab == null) return;
        if (fireSpot == null) return;

        if (fireLevel == 1)
        {
            // exactly one fire, right on the spot
            GameObject f = Instantiate(firePrefab, fireSpot.position, fireSpot.rotation);
            spawnedFires.Add(f);
        }
        else if (fireLevel == 2)
        {
            // center fire
            GameObject f0 = Instantiate(firePrefab, fireSpot.position, fireSpot.rotation);
            spawnedFires.Add(f0);

            float radius = 0.6f;

            // fixed offsets (120° apart)
            Vector3 p1 = fireSpot.position + new Vector3(radius, 0f, 0f);
            Vector3 p2 = fireSpot.position + Quaternion.Euler(0f, 120f, 0f) * new Vector3(radius, 0f, 0f);
            Vector3 p3 = fireSpot.position + Quaternion.Euler(0f, 240f, 0f) * new Vector3(radius, 0f, 0f);

            GameObject f1 = Instantiate(firePrefab, p1, fireSpot.rotation);
            GameObject f2 = Instantiate(firePrefab, p2, fireSpot.rotation);

            // we already spawned center, so we only need two more to make 3 total
            spawnedFires.Add(f1);
            spawnedFires.Add(f2);
        }
    }


    // 🧍 Spawn NPCs randomly inside the collider zone
    void SpawnNPCsInZone(BoxCollider zone, int count, Vector3 firePos, float fireExclusionRadius)
    {
        if (count <= 0) return;
        if (npcPrefabs == null || npcPrefabs.Length == 0) return;
        if (zone == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = Vector3.zero; // ✅ initialize it
            bool found = false;

            // try multiple times to find a point not near fire
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector3 randomPoint = GetRandomPointInBox(zone);

                // skip if too close to fire
                if (Vector3.Distance(randomPoint, firePos) < fireExclusionRadius)
                    continue;

                // project to NavMesh
                UnityEngine.AI.NavMeshHit nmHit;
                if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out nmHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    spawnPos = nmHit.position;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // couldn’t find a valid location after 10 tries → skip NPC
                continue;
            }

            GameObject prefab = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
            GameObject npc = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
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

    // 🧹 Clean up between iterations
    void ClearSpawned()
    {
        foreach (var fire in spawnedFires) if (fire != null) Destroy(fire);
        foreach (var npc in spawnedNPCs) if (npc != null) Destroy(npc);
        spawnedFires.Clear();
        spawnedNPCs.Clear();
    }

    // 📸 Capture from the specific surveillance camera
    void CaptureFromCamera(Camera cam, string path)
    {
        // choose your resolution
        int width = 1920;
        int height = 1080;

        // 1. make a temporary render texture
        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;

        // 2. render this camera
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        cam.Render();

        // 3. read pixels from that render
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // 4. clean up
        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // 5. save to disk
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        Destroy(tex);

        Debug.Log("Captured from camera: " + cam.name + " → " + path);
    }

    // try to land on actual geometry (floor, platform, etc.)
    bool TrySnapToNavMesh(Vector3 source, float maxDistance, out Vector3 result)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(source, out hit, maxDistance, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        result = Vector3.zero;
        return false;
    }

    bool TrySnapToGround(Vector3 source, out Vector3 result)
    {
        RaycastHit hit;
        if (Physics.Raycast(source + Vector3.up * 3f, Vector3.down, out hit, 10f))
        {
            result = hit.point;
            return true;
        }
        result = Vector3.zero;
        return false;
    }


}
