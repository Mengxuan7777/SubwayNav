using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class PathDecisionManager : MonoBehaviour
{
    private AgentMover mover;

    private void Awake()
    {
        mover = GetComponent<AgentMover>();
    }

    public void UpdateAgentPosition(Vector3 position)
    {
        StartCoroutine(GetPathFromServer(position));
    }

    private IEnumerator GetPathFromServer(Vector3 position)
    {
        string json = JsonUtility.ToJson(new PositionPayload(position));
        UnityWebRequest req = UnityWebRequest.Put("http://localhost:5001/path", json);
        req.method = UnityWebRequest.kHttpVerbPOST;
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string jsonText = req.downloadHandler.text;
            Debug.Log("📥 Path response JSON:\n" + jsonText);

            PathResponse response = JsonUtility.FromJson<PathResponse>(jsonText);

            if (response.paths == null || response.paths.Length == 0)
            {
                Debug.LogWarning("⚠️ No valid paths returned from server.");
                yield break;
            }

            PathOption bestPath = response.paths[0];  // Use first path for now
            string currentNode = bestPath.nodes[0];
            StartCoroutine(QueryLLM(currentNode, response.paths));
        }
        else
        {
            Debug.LogError("⚠️ Path server error: " + req.error);
        }
    }

    private IEnumerator QueryLLM(string currentNode, PathOption[] paths)
    {
        LLMRequest request = new LLMRequest
        {
            start_node = currentNode,
            paths = paths
        };

        string json = JsonUtility.ToJson(request);
        UnityWebRequest req = UnityWebRequest.Put("http://localhost:5000/query", json);
        req.method = UnityWebRequest.kHttpVerbPOST;
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            LLMResponse reply = JsonUtility.FromJson<LLMResponse>(req.downloadHandler.text);
            Debug.Log("💬 LLM Reasoning: " + reply.reason);
            Debug.Log("📎 Raw nextNode from LLM: '" + reply.nextNode + "'");
            mover.MoveToNode(reply.nextNode);
        }

        else
        {
            Debug.LogError("⚠️ LLM query error: " + req.error);
        }
    }

    private string ExtractNodeFromReply(string reply)
    {
        int start = reply.IndexOf("[") + 1;
        int end = reply.IndexOf("]");
        if (start >= 0 && end > start)
        {
            string insideBrackets = reply.Substring(start, end - start);
            string[] nodes = insideBrackets.Replace("'", "").Split(',');
            return nodes.Length > 1 ? nodes[1].Trim() : nodes[0].Trim(); // return next node
        }
        return "";
    }

    // --- Data Classes ---
    [System.Serializable]
    public class PositionPayload { public float x, y, z; public PositionPayload(Vector3 pos) { x = pos.x; y = pos.y; z = pos.z; } }

    [System.Serializable]
    public class PathFactors
    {
        public float distance;
        public float crowd;
        public float smoke;
        public float fire;
    }

    [System.Serializable]
    public class PathOption
    {
        public string[] nodes;
        public float cost;
        public PathFactors factors;
    }

    [System.Serializable]
    public class PathResponse
    {
        public PathOption[] paths;
    }

    [System.Serializable]
    public class LLMRequest
    {
        public string start_node;
        public PathOption[] paths;
    }

    [System.Serializable]
    public class LLMResponse
    {
        public string reason;
        public string nextNode;
    }

}
