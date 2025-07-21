using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding {
    public class Node : MonoBehaviour {
        // Parameters
        public new string name;
        public Vector3 Position;
        public List<Edge> Edges = new List<Edge>();

        private void Awake() {
            // Set initial parameters
            name  = gameObject.name;
            Position = gameObject.transform.position;
        }
        
        public Node(string name, Vector3 position) {}
    }
}
