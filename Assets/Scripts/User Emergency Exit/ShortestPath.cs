using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OpenAI.Chat;
using Newtonsoft.Json;
using UnityEngine.AI;
using User_Emergency_Exit;

public class ShortestPath : MonoBehaviour {
    // Navmesh agent
    public NavMeshAgent agent;
    public Transform[] Exists;
    
    // Path Generation
    private List<NavMeshPath> paths = new List<NavMeshPath>();
    public GameObject waypoint, connector;
    private GameObject _container;
    private readonly List<GameObject> waypoints = new(), connectors = new();
    private readonly List<LineRenderer> connectorLines = new();
    
    // File Reader
    private static readonly string filepath = "/Users/tower/Documents/API Keys/OpenAI_ChatGPT_Key.txt";
    private readonly StreamReader reader = new (filepath);
    
    // Open AI
    private string apiKey;
    private ChatClient client;
   
    

    void Start() {
        // Set up GPT client
        apiKey = reader.ReadLine();
        client = new ChatClient("gpt-4o", apiKey);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public void UpdatePath() {
        // Get all the paths
        for (int i = 0; i < Exists.Length; i++) {
            NavMeshPath path = new NavMeshPath();
            bool  hasPath =  NavMesh.CalculatePath(transform.position, Exists[i].position,
                NavMesh.AllAreas, path);
            if (hasPath) {
                paths.Add(path);
            }
        }
        // Get answer from GPT and Display it.
        GetShortestPath();
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private async void GetShortestPath() {
        try {
            // Convert paths to json.
            string jsonPaths = serializePaths();
        
            // Get Response from GPT
            ChatCompletion completion = await client.CompleteChatAsync(
                "Given the following paths represented as arrays of Vector3 coordinates, " +
                "please calculate the total distances for each path and determine which path has the shortest distance. " +
                "Provide me with just the index of the path with the shortest distance. Only provide a single number. " +
                $"{jsonPaths}");
            string index = completion.Content[0].Text;
            Debug.Log($"[GPT]: {index}");
        
            UpdatePath(paths[int.Parse(index)]);
        } catch (Exception e) {
            Debug.Log(e.Message);
        }
    }

    /// <summary>
    /// Serialize paths to json to be feed to the ChatGPT API.
    /// This process requires the way points of the paths to change from
    /// Vector3's to SerializableVector3's due to Vector3's not being serializable.
    /// </summary>
    /// <returns></returns>
    private string serializePaths() {
        SerializableVector3[][] pathsArray = new SerializableVector3[paths.Count][];
        for (int i = 0; i < paths.Count; i++) {
            if (paths[i] == null) continue; //ignore empty paths
            pathsArray[i] = new SerializableVector3[paths[i].corners.Length];
            for (int j = 0; j < paths[i].corners.Length; j++) {
                pathsArray[i][j] = new SerializableVector3(paths[i].corners[j]);
            }
        }
        return JsonConvert.SerializeObject(pathsArray);
    }
    
    /// <summary>
    /// Create and Updates the position of the emergency exit path
    /// based on what is the new shortest path. 
    /// </summary>
    /// <param name="path"></param>
    private void UpdatePath(NavMeshPath path) {
        Vector3[] corners = path.corners;
        
        // Create path components if they don't exist
        CreatePath(corners);
            
        // Update positions of path
        for (var i = 0; i < waypoints.Count; i++) {
            waypoints[i].transform.position = corners[i];
        }
        
        // Update the endpoints of connecting lines
        for (var i = 0; i < connectors.Count; i++) {
            connectorLines[i].SetPosition(0, corners[i]);
            connectorLines[i].SetPosition(1, corners[i+1]);
        }
    }
    
    /// <summary>
    /// Creates the components for displaying the emergency exit path.
    /// It stores this components (i.e. GameObjects) if a pool for later use.
    /// </summary>
    /// <param name="path"></param>
    private void CreatePath(Vector3[] path) {
        // Create a container if it doesn't exist
        if (_container == null) {
            _container = new GameObject("Exit Path");
        }
        
        // Create or activate traj objects
        // Creates extra waypoints if needed.
        var count = path.Length;
        for (int i = 0; i < count; i++) {
            if (i >= waypoints.Count) {
                GameObject wp = Instantiate(waypoint, Vector3.zero, Quaternion.identity, _container.transform);
                wp.name = "Waypoint_" + i;
                waypoints.Add(wp);
            }
            waypoints[i].SetActive(true); // Activate for usage
        }
        // Hide extra waypoint objects
        for (int i = count; i < waypoints.Count; i++) waypoints[i].SetActive(false);
        
        // Instantiate or activate trajectory connectors (i.e. LineRenders)
        for (int i = 0; i < count - 1; i++) {
            if (i >= connectors.Count) {
                // Instantiate new connectors for pooling
                GameObject conn = Instantiate(connector, Vector3.zero, Quaternion.identity, _container.transform);
                conn.name = "Connector_" + i;
                connectors.Add(conn);

                LineRenderer trajLine = conn.GetComponent<LineRenderer>();
                connectorLines.Add(trajLine);
            }
            // Activate for usage if, if not already;
            connectors[i].SetActive(true); 
        }
        // Hide extra connectors
        for (int i = count - 1; i < connectors.Count; i++) connectors[i].SetActive(false);

    }
}
