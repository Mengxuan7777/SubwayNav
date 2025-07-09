using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class PathVisualizer : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public Transform agentTransform;

    public void DrawFullPath(string[] pathNodeNames, Dictionary<string, Transform> nodeLookup)
    {
        List<Vector3> finalCorners = new List<Vector3>();

        for (int i = 0; i < pathNodeNames.Length - 1; i++)
        {
            if (!nodeLookup.ContainsKey(pathNodeNames[i]) || !nodeLookup.ContainsKey(pathNodeNames[i + 1]))
            {
                Debug.LogWarning($"❌ Skipping segment: {pathNodeNames[i]} → {pathNodeNames[i + 1]}");
                continue;
            }

            Vector3 start = nodeLookup[pathNodeNames[i]].position;
            Vector3 end = nodeLookup[pathNodeNames[i + 1]].position;

            if (NavMesh.SamplePosition(start, out NavMeshHit hitStart, 1.0f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(end, out NavMeshHit hitEnd, 1.0f, NavMesh.AllAreas))
            {
                NavMeshPath partialPath = new NavMeshPath();
                if (NavMesh.CalculatePath(hitStart.position, hitEnd.position, NavMesh.AllAreas, partialPath))
                {
                    finalCorners.AddRange(partialPath.corners);
                }
            }
        }

        lineRenderer.positionCount = finalCorners.Count;
        lineRenderer.SetPositions(finalCorners.ToArray());
    }
}
