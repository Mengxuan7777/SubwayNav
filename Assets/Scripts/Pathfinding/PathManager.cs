using System.Collections.Generic;
using Camera;
using UnityEngine;
using UnityEngine.AI;
using Newtonsoft.Json;

namespace Pathfinding {
    public class PathManager : MonoBehaviour {
        // References
        public GameObject Agent;
        private NavMeshAgent AgentNavMesh;
        private StationNode AgentNode;
        
        // Nodes 
        public StationNode[] Nodes, Exits;
        private readonly Dictionary<string, Node> NodeLookup = new();
        private readonly List<Node> ExitsList = new ();
        private int NodeCount;
        
        // Pathfinding
        private Node StartNode, OldNode, currentNode;
        private List<Node> Path = new();
        private int PathIndex = 0;
        private readonly AStar pathfinder = new AStar();
        
        private void Start() {
            // Gather agent info
            AgentNavMesh = Agent.GetComponent<NavMeshAgent>();
            AgentNode =  Agent.GetComponent<StationNode>();
            StartNode = AgentNode.node;
            
            // Gather Information Regarding Nodes
            NodeCount = Nodes.Length;
            
            // Create Node List/Dictionary
            foreach (var t in Exits) {
                ExitsList.Add(t.node);
            }
            foreach (var node in Nodes) {
                NodeLookup.Add(node.name, node.node);
            }
        }

        private void Update() {
            // If path is empty, don't bother to check
            if (Path.Count == 0) return;
            // Set agent to the next destination
            if (AgentNavMesh.remainingDistance <= AgentNavMesh.stoppingDistance + 1 && PathIndex < Path.Count) {
                 currentNode = Path[PathIndex];
                if (PathIndex > 0) OldNode = Path[PathIndex - 1];
                AgentNavMesh.SetDestination(currentNode.Position);
                PathIndex++;
            }
        }


        /// <summary>
        /// Recalculates the path based on the new edge weights.
        /// </summary>
        public void ReCalculatePath() {
            // If agent is on a path, determine the nearest node. 
            if (AgentNavMesh.hasPath) {
                var d1 = Vector3.Distance(Agent.transform.position, OldNode.Position);
                var d2 = Vector3.Distance(Agent.transform.position, AgentNavMesh.destination);
                StartNode = d2 <= d1 ? currentNode : OldNode;
            }
            // Update agent path
            Path = new List<Node>(); // Set path temporarily to nothing
            PathIndex = 0;
            Path = pathfinder.FindPath(NodeCount, StartNode, ExitsList);
        }

        /// <summary>
        /// Takes updates from VLM to update the edges of the node
        /// </summary>
        /// <param name="updates"></param>
        public void UpdateEdgeWeights(string updates) {
            Debug.Log($"[Python]: Updating Edge Weights");
            //Make sure string is not empty
            if (string.IsNullOrEmpty(updates)) { return; }
            
            // Parse json file
            GPTMessage[] message = JsonConvert.DeserializeObject<GPTMessage[]>(updates);
            // Update cost for each node.
            foreach (var update in message) {
                // Make sure node exists, and then update
                if (NodeLookup.TryGetValue(update.name, out var node)) {
                    UpdateEdgeWeight(node, update.crowdCost, update.fireCost);
                } else {
                    Debug.LogWarning($"[UpdateEdgeWeights] Node '{update.name}' not found in NodeLookup. Skipping.");
                }
            }
        }
        
        /// <summary>
        /// Updates all edge weights for a given node.
        /// Yes, this runs in O(N^2) time. However, in the worst-case scenario N=6,
        /// which means this basically runs in basically O(1).
        /// </summary>
        /// <param name="node">Node to update costs</param>
        /// <param name="crowdCost">Level of crowdedness </param>
        /// <param name="fireCost">Hazard level of fire</param>
        private void UpdateEdgeWeight(Node node, float crowdCost, float fireCost) {
            foreach (var edge in node.Edges) {
                // Update the outgoing edge weight
                Debug.Log($"{node.name} -> {edge.TargetNode.name}: {edge.DistanceCost} ");
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
