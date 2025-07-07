using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro; 

public class PathDecisionManager : MonoBehaviour
{
    public TMP_Text promptDisplay;  
    public TMP_Text decisionDisplay; 
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

            PathOption bestPath = response.paths[0];
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

        string prompt = GenerateLLMPrompt(currentNode, paths);  // 🧠 Construct the prompt string

        if (promptDisplay != null)
        {
            promptDisplay.text = prompt;  // 🖼️ Show the path options
        }

        string json = JsonUtility.ToJson(request);
        UnityWebRequest req = UnityWebRequest.Put("http://localhost:5000/query", json);
        req.method = UnityWebRequest.kHttpVerbPOST;
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            LLMResponse reply = JsonUtility.FromJson<LLMResponse>(req.downloadHandler.text);
            Debug.Log("💬 LLM Reasoning: " + reply.reason);

            if (decisionDisplay != null)
            {
                decisionDisplay.text = $"💡 LLM Decision:\n{reply.reason}\n➡️ Next Node: {reply.path?[1] ?? "N/A"}";
            }

            if (reply.path == null || reply.path.Length == 0)
            {
                Debug.LogWarning("⚠️ LLM returned an empty or null path.");
                yield break;
            }

            mover.DrawFullLLMPath(reply.path);
            StartCoroutine(MoveAlongPath(reply.path));
        }
        else
        {
            Debug.LogError("⚠️ LLM query error: " + req.error);
        }
        
    }

    private IEnumerator MoveAlongPath(string[] path)
    {
        if (path == null || path.Length == 0)
        {
            Debug.LogError("⚠️ Path is empty or null.");
            yield break;
        }

        List<string> cleanPath = new List<string>();
        foreach (string node in path)
        {
            if (!node.StartsWith("agent_") && mover.HasNode(node))
            {
                cleanPath.Add(node);
            }
            else
            {
                Debug.LogWarning($"❌ Node not found or skipped: {node}");
            }
        }

        Debug.Log("🧭 Cleaned LLM path: " + string.Join(" → ", cleanPath));

        foreach (string nodeName in cleanPath)
        {
            Debug.Log("🚶 Moving to: " + nodeName);
            yield return StartCoroutine(mover.MoveToAndWait(nodeName));
            yield return new WaitForSeconds(0.25f);  // optional pause
        }

        Debug.Log("🎯 Reached final destination.");
        mover.ClearPathLine();
    }

    private string GenerateLLMPrompt(string currentNode, PathOption[] paths)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"🤖 Current node: {currentNode}");
        sb.AppendLine("🔀 Path options:");

        foreach (var path in paths)
        {
            string nodeList = string.Join(" → ", path.nodes);
            sb.AppendLine($"  - Cost: {path.cost:F2}, Crowd: {path.factors.crowd}, Smoke: {path.factors.smoke}");
            sb.AppendLine($"    Path: {nodeList}");
        }

        return sb.ToString();
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
        public string[] path;
    }
}
