using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Pathfinding {
    public class PathManager : MonoBehaviour {
        // References
        public GameObject Agent;
        private NavMeshAgent AgentNavMesh;
        private StationNode AgentNode;
        
        // Nodes 
        
        private Node StartNode;
        public StationNode[] Nodes, Exits;
        private readonly List<Node> ExitsList = new ();
        private int NodeCount;
        
        // Pathfinding
        private readonly AStar pathfinder = new AStar();
        private bool isPathfinding = false;

        private void Start() {
            // Gather agent info
            AgentNavMesh = Agent.GetComponent<NavMeshAgent>();
            AgentNode =  Agent.GetComponent<StationNode>();
            StartNode = AgentNode.node;
            
            // Gather Information Regarding Nodes
            NodeCount = Nodes.Length;
            
            // Create Node List
            foreach (var t in Exits) {
                ExitsList.Add(t.node);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown((KeyCode.A)) && !isPathfinding) {
                ReCalculatePath();
                isPathfinding = true;
            }
        }


        /// <summary>
        /// Recalculates the path based on the new edge weights.
        /// </summary>
        private void ReCalculatePath() { 
            List<Node> path = pathfinder.FindPath(NodeCount, StartNode, ExitsList);
            foreach (var node in path)
            {
                Debug.Log($"[{node.Position.x}, {node.Position.y}, {node.Position.z}]");
            }
        }
        
        /// <summary>
        /// Updates all edge weights for a given node.
        /// Yes, this runs in O(N^2) time. However, in the worst-case scenario N=6,
        /// which means this basically runs in basically O(1).
        /// </summary>
        /// <param name="node"></param>
        /// <param name="crowdCost"></param>
        /// <param name="fireCost"></param>
        private void UpdateEdgeWeight(Node node, float crowdCost, float fireCost) {
            foreach (var edge in node.Edges) {
                // Update the outgoing edge weight
                edge.Weight = edge.DistanceCost + crowdCost + fireCost;
                
                // Update the incoming edge weight
                foreach (var NBREdge in edge.TargetNode.Edges) {
                    if (NBREdge.TargetNode == node) {
                        NBREdge.Weight = edge.Weight;
                    }
                }
            }
        }

    }
}
