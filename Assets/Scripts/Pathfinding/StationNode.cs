using UnityEngine;

namespace Pathfinding {
    public class StationNode : MonoBehaviour {
        // References
        [SerializeField]  public StationNode[] NeighbourNodes;
        
        // Node Information
        internal Node node;

        private void Awake() {
            // Initialize node
            node = new Node(gameObject.name, gameObject.transform.position);
        }
        
        private void Start() {
            // Initialized Nodes
            foreach (var neighbour in NeighbourNodes) {
                // Debug.Log($"[{node.Name}]NeighbourNodes contains: {neighbour.node.Name}");
                float distance = Vector3.Distance(gameObject.transform.position, neighbour.node.Position.ToVector3());
                Edge edge = new Edge(neighbour.node, distance);
                node.Edges.Add(edge);
            }
        }
    }
}
