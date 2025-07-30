using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;

namespace Pathfinding {
    public class AStar {
        /// <summary>
        /// Generates an optimal path using A* algorithm.
        /// </summary>
        /// <param name="nodeCount">Total Number of Nodes</param>
        /// <param name="start">Starting Node</param>
        /// <param name="targets">Exit Nodes (Always 4 total)</param>
        /// <returns>Path in the form of a Vector3 list.</returns>
        public List<Node> FindPath(int nodeCount, Node start, List<Node> targets) {
            // Create set for nodes to explore (openSet) and sets for the cost to reach node (gScore),
            // cost to reach target (fScore), and denote where a node came from (cameFrom).
            var openSet = new FastPriorityQueue<Node>(nodeCount); 
            var gScore = new Dictionary<Node, float>();
            var fScore = new Dictionary<Node, float>();
            var cameFrom = new Dictionary<Node, Node>();

            // Set a starting point
            gScore[start] = 0;
            fScore[start] = MinHeuristic(start, targets);
            openSet.Enqueue(start, fScore[start]);
            var goal = (Node)null; // Set goal to null for now

            // Loop until we find a path or run out of nodes to explore
            while (openSet.Count > 0) {
                Node current = openSet.Dequeue();
                
                // The current node is a target, so stop
                if (targets.Contains(current)) {
                    goal = current;
                    break;
                }
                
                // Otherwise, check neighboring nodes 
                foreach (var edge in current.Edges) {
                    Node neighbor = edge.TargetNode;
                    float tentativeG = gScore[current] + edge.Weight;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor]) {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        float heur = MinHeuristic(neighbor, targets);
                        float f = tentativeG + heur;
                        fScore[neighbor] = f;

                        if (openSet.Contains(neighbor))
                            openSet.UpdatePriority(neighbor, f);
                        else
                            openSet.Enqueue(neighbor, f);
                    }
                }
            }

            // All nodes were check, and no path was found
            if (goal == null)
                return null;

            // Reconstruct a path
            List<Node> path = new();
            var n = goal;
            while (cameFrom.ContainsKey(n)) {
                path.Add(n);
                n = cameFrom[n];
            }

            path.Add(start);
            path.Reverse();
            return path;
        }

        /// <summary>
        /// Calculates the minimum heuristic distance between a node and a list of target nodes.
        /// </summary>
        /// <param name="node">The starting node.</param>
        /// <param name="targets">Exit Nodes</param>
        /// <returns>The minimum distance from the given node to any target node.</returns>
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
    
    /// <summary>
    /// Node Class
    /// </summary>
    public class Node : FastPriorityQueueNode {
        // Parameters
        public string name;
        public Vector3 Position;
        public readonly List<Edge> Edges = new List<Edge>();
        
        // Constructor
        public Node(string name, Vector3 position) {
            this.name = name;
            Position = position;
        }
    }
    
    
    /// <summary>
    /// Edge utility class 
    /// </summary>
    public class Edge {
        // Parameters
        public Node TargetNode;
        public float Weight;
        public readonly float DistanceCost;

        // Constructor
        public Edge(Node target, float distanceCost) {
            TargetNode = target;
            DistanceCost = Weight =  distanceCost;
        }
    }
}