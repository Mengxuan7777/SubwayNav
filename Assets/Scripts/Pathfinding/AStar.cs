using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding {
    public class AStar {
         public static List<Node> AStar(Node start, List<Node> targets) {
        var openSet = new SortedSet<(float fCost, Node node)>(Comparer<(float, Node)>.Create((a, b) => 
            a.fCost != b.fCost ? a.fCost.CompareTo(b.fCost) : a.node.GetHashCode().CompareTo(b.node.GetHashCode())));
        var gScore = new Dictionary<Node, float>();
        var fScore = new Dictionary<Node, float>();
        var cameFrom = new Dictionary<Node, Node>();

        gScore[start] = 0;
        fScore[start] = MinHeuristic(start, targets);
        openSet.Add((fScore[start], start));

        var goal = (Node)null;

        while (openSet.Count > 0)
        {
            var current = openSet.Min.node;
            openSet.Remove(openSet.Min);

            // If current is one of the targets, reconstruct and return the path
            if (targets.Contains(current))
            {
                goal = current;
                break;
            }

            foreach (var edge in current.Edges)
            {
                var neighbor = edge.TargetNode;
                float tentativeG = gScore[current] + edge.Weight;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    float heur = MinHeuristic(neighbor, targets);
                    float f = tentativeG + heur;
                    fScore[neighbor] = f;
                    openSet.Add((f, neighbor));
                }
            }
        }

        // No path found
        if (goal == null)
            return null;

        // Reconstruct path
        List<Node> path = new();
        var n = goal;
        while (cameFrom.ContainsKey(n))
        {
            path.Add(n);
            n = cameFrom[n];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    private static float MinHeuristic(Node node, List<Node> targets)
    {
        float min = float.MaxValue;
        foreach (var t in targets)
        {
            float dist = Vector3.Distance(node.Position, t.Position); // Euclidean in 3D
            if (dist < min) min = dist;
        }
        return min;
    }


    }
    
    public class Edge {
        // Parameters
        public Node Start;
        public Node End;
        public float Weight;
        
        // Weight Setting Function
        public Edge(Node target, float weight) {
            End = target;
            Weight = weight;
        }
    }
}