using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using Newtonsoft.Json;
using Utils;
using System;
using System.IO;
using Debug = UnityEngine.Debug;

namespace Pathfinding {
    public class PathManager : MonoBehaviour {
        // References
        [Header("References")]
        public GameObject Agent;
        private NavMeshAgent AgentNavMesh;
        private StationNode AgentNode;
        
        // Nodes 
        [Header("Station Nodes")]
        public StationNode[] Nodes, Exits;
        private readonly Dictionary<string, Node> NodeLookup = new();
        private readonly List<Node> NodeList = new(), ExitsList = new ();
        private int NodeCount;
        
        // Pathfinding
        [Header("PathFinding")]
        public bool LLMPathfindingEnabled, pathfinding;
        private Node StartNode, OldNode, currentNode;
        private List<Node> PathNodes = new();
        private List<string> PathNodesString = new();
        private int PathIndex = 0;
        private readonly AStar pathfinder = new AStar();
        private readonly LLMPathfinding llmPathfinder = new LLMPathfinding();
        
        // Constants
        private const int crowdPenaltyMultiplier = 5,
            firePenaltyMultiplier = 500;
        public readonly string logFilePath = Application.streamingAssetsPath + "/TimeLog/" +"s1_T1.csv";
        
        private void Start() {
            // Gather agent info
            AgentNavMesh = Agent.GetComponent<NavMeshAgent>();
            AgentNode =  Agent.GetComponent<StationNode>();
            StartNode = AgentNode.node;
            
            // Gather Information Regarding Nodes
            NodeCount = Nodes.Length;
            
            // Create Node List for Exists
            foreach (var t in Exits) {
                ExitsList.Add(t.node);
            }
            
            // Create Node List for All Nodes
            foreach (var node in Nodes) {
                NodeList.Add(node.node);
                NodeLookup.Add(node.name, node.node);
            }
            NodeList.Add(StartNode);
        }

        private void Update() {
            if (!pathfinding) { UpdateDestination(); }
        }
        
        /// <summary>
        /// Recalculates the path based on the new edge weights.
        /// </summary>
        public async Task ReCalculatePath() {
            var stopwatch = Stopwatch.StartNew();
            Debug.Log("Recalculating Path");
            // If agent is on a path, determine the nearest node. 
            if (AgentNavMesh.hasPath) {
                var d1 = Vector3.Distance(Agent.transform.position, OldNode.Position.ToVector3());
                var d2 = Vector3.Distance(Agent.transform.position, AgentNavMesh.destination);
                StartNode = d2 <= d1 ? currentNode : OldNode;
            }
            

            // Get snapshot and Update agent path
            pathfinding = true;
            var start = NodeList.Find(n=> n.Name == StartNode.Name);
            var (clonedNodes, clonedStart, clonedTargets) = SnapshotGraph(NodeList, start, ExitsList);
            PathIndex = 0;
            if (LLMPathfindingEnabled) {
                PathNodesString = await LLMPathfinding.FindPath(clonedNodes, clonedTargets, clonedStart);
            } else {
                PathNodes = pathfinder.FindPath(clonedNodes.Count, clonedStart, clonedTargets);
            }
            pathfinding = false;
            // Log time
            stopwatch.Stop();
            LogExecutionTime("ReCalculatePath", stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Generates a snapshot of the subway station graph
        /// </summary>
        /// <param name="allNodes">List of all nodes</param>
        /// <param name="startNode">Starting Node</param>
        /// <param name="targetNodes">List of target nodes</param>
        /// <returns></returns>
        private static (List<Node> clonedNodes, Node clonedStart, List<Node> clonedTargets)
            SnapshotGraph(List<Node> allNodes, Node startNode, List<Node> targetNodes) {
            // Clone all nodes 
            var nodeMap = new Dictionary<Node, Node>();
            foreach (var original in allNodes)
                original.Clone(nodeMap);

            // Clone edges
            foreach (var original in allNodes) {
                var clone = nodeMap[original];
                clone.Edges.Clear();
                foreach (var edge in original.Edges)
                    clone.Edges.Add(Edge.Clone(edge, nodeMap));
            }

            // Map start & targets to clones
            var clonedStart = nodeMap[startNode];
            var clonedTargets = new List<Node>();
            foreach (var t in targetNodes) {
                clonedTargets.Add(nodeMap[t]);
            }
            return (new List<Node>(nodeMap.Values), clonedStart, clonedTargets);
        }


        /// <summary>
        /// Takes updates from VLM to update the edges of the node
        /// </summary>
        /// <param name="updates"></param>
        public void UpdateEdgeWeights(string updates) {
            //Make sure string is not empty
            if (string.IsNullOrEmpty(updates)) { return; }
            
            // Parse json file
            GPTMessage[] message = JsonConvert.DeserializeObject<GPTMessage[]>(updates);
            // Update cost for each node.
            foreach (var update in message) {
                // Make sure node exists, and then update
                if (NodeLookup.TryGetValue(update.name, out var node)) {
                    UpdateEdgeWeight(node, 
                        update.crowdCost * crowdPenaltyMultiplier,
                        update.fireCost * firePenaltyMultiplier);
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
            // Update the danger level of the node
            node.DangerLevel = crowdCost + fireCost;
            //Debug.Log($"[{node.Name}] Danger Level: {node.DangerLevel}");
            
            // Update incoming edges
            foreach (var edge in node.Edges) {
                var neighborNode = edge.TargetNode;  
                foreach (var inEdge in neighborNode.Edges) {
                    if (inEdge.TargetNode == node) {
                        //Debug.Log($"[{node.Name}] Updating Edge: {neighborNode.Name} -> {inEdge.TargetNode.Name}");
                        inEdge.Weight = inEdge.DistanceCost + node.DangerLevel;
                    }
                }
            }
        }

        private void UpdateDestination() {
            // If path is empty, don't bother to check
            if (PathNodes.Count == 0 && PathNodesString.Count == 0) return;
            
            // Set agent to the next destination
            if (AgentNavMesh.remainingDistance <= AgentNavMesh.stoppingDistance + 1 && (PathIndex < PathNodesString.Count || PathIndex < PathNodes.Count)) {
                if (LLMPathfindingEnabled ) {
                    if (PathNodesString[PathIndex] == "Agent"){
                        PathIndex++;
                        currentNode = NodeLookup[PathNodesString[PathIndex]];
                    } else {
                        currentNode = NodeLookup[PathNodesString[PathIndex]];
                        if (PathIndex > 0) OldNode = NodeLookup[PathNodesString[PathIndex - 1]];
                    }
                } else {
                    currentNode = PathNodes[PathIndex];
                    if (PathIndex > 0) OldNode = PathNodes[PathIndex - 1];
                } 
                AgentNavMesh.SetDestination(currentNode.Position.ToVector3());
                //Debug.Log($"SetDestination called: {AgentNavMesh.destination}");
                //Debug.Log($"Agent Path Status: {AgentNavMesh.hasPath}, {AgentNavMesh.remainingDistance}, {AgentNavMesh.stoppingDistance}");
                PathIndex++;
            }
            //Debug.Log($"Agent Destination:{AgentNavMesh.destination}");
            //Debug.Log($"AGENT STATUS. Stopped: {AgentNavMesh.isStopped}, HasPath: {AgentNavMesh.hasPath}");
        }
        
        /// <summary>
        /// Logs the time it takes for a method to run.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="executionTimeMs"></param>
        private void LogExecutionTime(string methodName, long executionTimeMs) {
            if (!File.Exists(logFilePath)) {
                File.AppendAllText(logFilePath, "Timestamp,Method,ExecutionTime(ms)\n");
            }

            var logEntry = $"{DateTime.Now:O},{methodName},{executionTimeMs}\n";
            File.AppendAllText(logFilePath, logEntry);
        }
    }
}