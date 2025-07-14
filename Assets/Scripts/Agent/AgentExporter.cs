using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;

public class AgentExporter : MonoBehaviour
{
    [Tooltip("Nearby station nodes (assign GameObjects)")]
    public GameObject[] nearbyNodes;

    [Tooltip("Send to this server endpoint")]
    public string serverUrl = "http://localhost:5000/start_path_decision";

    public int sendInterval = 15;

    private void Start()
    {
        StartCoroutine(SendAgentEdgesRoutine());
    }

    private IEnumerator SendAgentEdgesRoutine()
    {
        while (true)
        {
            string json = GenerateClosestTwoEdgesJson();
            Debug.Log($"📤 Payload to send:\n{json}");

            using (UnityWebRequest req = UnityWebRequest.Put(serverUrl, json))
            {
                req.method = UnityWebRequest.kHttpVerbPOST;
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"❌ Failed to send agent edges: {req.error}");
                }
                else
                {
                    Debug.Log($"✅ Agent edges sent at {System.DateTime.Now:T}");
                }
            }

            yield return new WaitForSeconds(sendInterval);
        }
    }

    private string GenerateClosestTwoEdgesJson()
    {
        Vector3 agentPos = SnapToNavMesh(transform.position);
        Dictionary<string, float> edgeDistances = new Dictionary<string, float>();

        foreach (GameObject node in nearbyNodes)
        {
            if (node == null || node.name == "Agent") continue;

            Vector3 nodePos = SnapToNavMesh(node.transform.position);

            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(agentPos, nodePos, NavMesh.AllAreas, path))
            {
                float navDistance = 0f;
                Vector3[] corners = path.corners;
                for (int i = 0; i < corners.Length - 1; i++)
                {
                    navDistance += Vector3.Distance(corners[i], corners[i + 1]);
                }

                string forwardEdge = $"Agent->{node.name}";
                string backwardEdge = $"{node.name}->Agent";
                edgeDistances[forwardEdge] = navDistance;
                edgeDistances[backwardEdge] = navDistance;
            }
            else
            {
                Debug.LogWarning($"⚠️ No valid NavMesh path to {node.name}");
            }
        }

        var closestEdges = new Dictionary<string, float>();
        foreach (var pair in GetTopK(edgeDistances, 4))  // 2 bidirectional edges = 4
        {
            closestEdges[pair.Key] = pair.Value;
        }

        var payload = new FullPayload
        {
            position = new float[] { agentPos.x, agentPos.y, agentPos.z },
            edges = closestEdges
        };

        return JsonUtility.ToJson(payload);
    }

    private Vector3 SnapToNavMesh(Vector3 position, float maxDistance = 2f)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }
        else
        {
            Debug.LogWarning($"⚠️ Failed to snap to NavMesh near {position}");
            return position;
        }
    }

    private List<KeyValuePair<string, float>> GetTopK(Dictionary<string, float> dict, int k)
    {
        var list = new List<KeyValuePair<string, float>>(dict);
        list.Sort((a, b) => a.Value.CompareTo(b.Value));
        return list.GetRange(0, Mathf.Min(k, list.Count));
    }

    [System.Serializable]
    private class FullPayload
    {
        public float[] position;
        public Dictionary<string, float> edges;
    }
}
