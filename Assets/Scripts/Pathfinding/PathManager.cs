using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Pathfinding {
    public class PathManager : MonoBehaviour {
        // References
        public GameObject Agent;
        private NavMeshAgent AgentNavMesh;
        
    
        // Nodes 
        private Node StartNode;
        public List<Node> Nodes;
        public List<Node> Exits;
        
        // Settings


        private void Awake() {
            // Gather agent info
            AgentNavMesh = Agent.GetComponent<NavMeshAgent>();
            StartNode = new Node("Start", Agent.transform.position);
        }


        
        private void UpdateEdgeWeight(Node node, float newWeight) {
            foreach (var edge in node.Edges) {
                edge.Weight = newWeight;
            }
        }

    }
}
