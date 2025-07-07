using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AgentMover : MonoBehaviour
{
    public Transform[] nodeTransforms;  // Assigned in inspector
    private Dictionary<string, Transform> nodeLookup;

    public float moveSpeed = 3f;

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
    }

    public void MoveToNode(string nodeName)
    {
        Debug.Log("🔵 MoveToNode called with: " + nodeName);

        if (string.IsNullOrEmpty(nodeName))
        {
            Debug.LogError("❌ nodeName is null or empty — cannot move.");
            return;
        }

        foreach (var key in nodeLookup.Keys)
        {
            Debug.Log("📌 Known node in lookup: " + key);
        }

        if (nodeLookup.ContainsKey(nodeName))
        {
            Transform target = nodeLookup[nodeName];
            StopAllCoroutines();
            StartCoroutine(MoveTo(target));
        }
        else
        {
            Debug.LogWarning("❌ Node not found: " + nodeName);
        }
    }

    private IEnumerator MoveTo(Transform target)
    {
        while (Vector3.Distance(transform.position, target.position) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
            yield return null;
        }
        Debug.Log("✅ Reached node: " + target.name);
    }
}
