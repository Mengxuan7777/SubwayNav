using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class AgentMover : MonoBehaviour
{
    public Transform[] nodeTransforms;  // Assigned in Inspector
    private Dictionary<string, Transform> nodeLookup;
    private LineRenderer pathLine;
    private NavMeshPath navPath;
    private NavMeshAgent agent;

    private void Awake()
    {
        // Build name-to-Transform map
        nodeLookup = new Dictionary<string, Transform>();
        foreach (Transform t in nodeTransforms)
        {
            if (t != null && !nodeLookup.ContainsKey(t.name))
            {
                nodeLookup.Add(t.name, t);
            }
        }

        pathLine = GetComponent<LineRenderer>();
        navPath = new NavMeshPath();
        agent = GetComponent<NavMeshAgent>();
    }

    public void MoveToNode(string nodeName)
    {
        Debug.Log("🔵 MoveToNode called with: " + nodeName);
        if (nodeLookup.ContainsKey(nodeName))
        {
            Transform target = nodeLookup[nodeName];
            if (NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, navPath))
            {
                Debug.Log("✅ NavMesh path calculation succeeded.");
                agent.SetPath(navPath);
            }
            else
            {
                Debug.LogWarning("❌ NavMesh path calculation FAILED.");
            }
        }
        else
        {
            Debug.LogWarning("❌ Node not found: " + nodeName);
        }
    }

    public IEnumerator MoveToAndWait(string nodeName)
    {
        if (!nodeLookup.ContainsKey(nodeName))
        {
            Debug.LogWarning("❌ Node not found: " + nodeName);
            yield break;
        }

        Transform target = nodeLookup[nodeName];
        agent.SetDestination(target.position);

        while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.05f)
        {
            yield return null;
        }

        Debug.Log("✅ Reached node: " + nodeName);
    }

    public Vector3 GetNodePosition(string nodeName)
    {
        if (nodeLookup.ContainsKey(nodeName))
            return nodeLookup[nodeName].position;
        return transform.position;
    }

    public bool HasNode(string nodeName)
    {
        return nodeLookup.ContainsKey(nodeName);
    }

    public void DrawFullLLMPath(string[] nodePath)
    {
        if (nodePath == null || nodePath.Length < 2)
        {
            Debug.LogWarning("⚠️ LLM path is too short to draw.");
            pathLine.positionCount = 0;
            return;
        }

        List<Vector3> allCorners = new List<Vector3>();

        for (int i = 0; i < nodePath.Length - 1; i++)
        {
            if (!HasNode(nodePath[i]) || !HasNode(nodePath[i + 1]))
            {
                Debug.LogWarning($"⚠️ Skipping segment: {nodePath[i]} → {nodePath[i + 1]}");
                continue;
            }

            Vector3 start = nodeLookup[nodePath[i]].position;
            Vector3 end = nodeLookup[nodePath[i + 1]].position;

            if (NavMesh.SamplePosition(start, out NavMeshHit hitStart, 1.0f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(end, out NavMeshHit hitEnd, 1.0f, NavMesh.AllAreas))
            {
                NavMeshPath partialPath = new NavMeshPath();
                if (NavMesh.CalculatePath(hitStart.position, hitEnd.position, NavMesh.AllAreas, partialPath))
                {
                    allCorners.AddRange(partialPath.corners);
                }
            }
        }

        if (allCorners.Count < 2)
        {
            Debug.LogWarning("⚠️ Not enough total corners to draw full path.");
            pathLine.positionCount = 0;
            return;
        }

        pathLine.positionCount = allCorners.Count;
        pathLine.startColor = Color.blue;
        pathLine.endColor = Color.blue;

        for (int i = 0; i < allCorners.Count; i++)
        {
            pathLine.SetPosition(i, allCorners[i] + Vector3.up * 0.05f);
        }

        Debug.Log($"🟦 Full LLM path drawn with {allCorners.Count} corners.");
    }

    public void ClearPathLine()
    {
        if (pathLine != null)
            pathLine.positionCount = 0;
    }
}
