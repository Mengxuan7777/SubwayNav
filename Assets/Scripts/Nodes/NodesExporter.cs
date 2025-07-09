using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class NodeExporter : MonoBehaviour
{
    [System.Serializable]
    public class Node
    {
        public string name;
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class Wrapper
    {
        public List<Node> nodes;
    }

    public List<GameObject> stationNodes;

    void Start()
    {
        if (stationNodes == null || stationNodes.Count == 0)
        {
            Debug.LogWarning("⚠️ No station nodes assigned in the Inspector.");
            return;
        }

        List<Node> nodeList = new List<Node>();

        foreach (GameObject obj in stationNodes)
        {
            Vector3 pos = obj.transform.position;
            Debug.Log($"✅ Exporting: {obj.name} at {pos}");
            nodeList.Add(new Node { name = obj.name, x = pos.x, y = pos.y, z = pos.z });
        }

        // ✅ Use wrapper class
        Wrapper wrapper = new Wrapper { nodes = nodeList };
        string json = JsonUtility.ToJson(wrapper, true);

        // Save path
        string folderPath = Path.Combine(Application.dataPath, "Scripts/Nodes");
        string filePath = Path.Combine(folderPath, "station_nodes.json");
        File.WriteAllText(filePath, json);
        Debug.Log("Saved node positions to: " + filePath);
    }
}
