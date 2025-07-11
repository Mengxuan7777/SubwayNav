using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.IO;
using System;

public class NavMeshDistanceExporter : MonoBehaviour
{
    [Serializable]
    public class NodeDistance { public string from; public string to; public float distance; }
    [Serializable]
    public class DistanceMap { public List<NodeDistance> distances = new(); }
    [Serializable]
    public class StationNode { public string name; public float x, y, z; }
    [Serializable]
    public class NodeFile { public List<StationNode> nodes; }

    public string stationJsonFileName = "station_nodes.json";
    public string outputFileName = "navmesh_distances.json";

    // Only these edges will be evaluated (and made bidirectional)
    private readonly List<(string, string)> originalEdges = new()
    {
        ("US1", "US2"), ("US1", "US3"), ("US3", "US2"), ("DS1", "DS2"), ("DS1", "DS3"), ("DS3", "DS2"),
        ("US1", "NM"), ("US2", "NM"), ("US3", "SM"),
        ("DS1", "NM"), ("DS2", "NM"), ("DS3", "SM"),
        ("NM", "EX1"), ("NM", "EX2"),
        ("SM", "EX3"), ("SM", "EX4")
    };

    void Start()
    {
        Export();
    }


    public void Export()
    {
        string folderPath = Path.Combine(Application.dataPath, "Scripts/Nodes");
        string stationPath = Path.Combine(folderPath, stationJsonFileName);
        if (!File.Exists(stationPath)) { Debug.LogError("❌ station_nodes.json not found."); return; }

        string json = File.ReadAllText(stationPath);
        NodeFile nodeFile = JsonUtility.FromJson<NodeFile>(json);

        // Find GameObjects by name
        Dictionary<string, Transform> nodeTransforms = new();
        foreach (var node in nodeFile.nodes)
        {
            GameObject go = GameObject.Find(node.name);
            if (go != null) nodeTransforms[node.name] = go.transform;
            else Debug.LogWarning($"⚠️ Node '{node.name}' not found in scene.");
        }

        DistanceMap map = new();

        // Make bidirectional
        List<(string, string)> bidirectionalEdges = new();
        foreach (var (u, v) in originalEdges)
        {
            bidirectionalEdges.Add((u, v));
            bidirectionalEdges.Add((v, u));
        }

        foreach (var (fromName, toName) in bidirectionalEdges)
        {
            if (!nodeTransforms.ContainsKey(fromName) || !nodeTransforms.ContainsKey(toName)) continue;

            Transform from = nodeTransforms[fromName];
            Transform to = nodeTransforms[toName];

            if (NavMesh.SamplePosition(from.position, out NavMeshHit hitFrom, 1f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(to.position, out NavMeshHit hitTo, 1f, NavMesh.AllAreas))
            {
                NavMeshPath path = new();
                if (NavMesh.CalculatePath(hitFrom.position, hitTo.position, NavMesh.AllAreas, path) &&
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    float total = 0f;
                    for (int i = 1; i < path.corners.Length; i++)
                        total += Vector3.Distance(path.corners[i - 1], path.corners[i]);

                    map.distances.Add(new NodeDistance { from = fromName, to = toName, distance = total });
                    //Debug.Log($"✅ {fromName} -> {toName}: {total:F2}");
                }
                else
                    Debug.LogWarning($"❌ Failed to find path: {fromName} -> {toName}");
            }
        }

        string outputJson = JsonUtility.ToJson(map, true);
        string outputPath = Path.Combine(folderPath, outputFileName);
        File.WriteAllText(outputPath, outputJson);
        Debug.Log("✅ Exported NavMesh distances to: " + outputPath);
    }


}
