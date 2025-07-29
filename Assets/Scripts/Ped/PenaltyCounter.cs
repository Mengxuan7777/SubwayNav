using System;
using System.IO;
using UnityEngine;

public class PenaltyCounter : MonoBehaviour {
    // Start is called before the first frame update
    [Header("Penalty Counters")] 
    private static int collisionWithFire;
    private static int collisionWithPedestrian;

    [Header("Log Settings")] 
    private const string LogPath = "Users/tower/Documents/Unity Projects/SubwayNav/Assets/Logs/";
    private static StreamWriter writer;

    private void Start() {
        // Create a new file name
        string fileName = "penalty_counter_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".csv";
        
        // Create a new stream writer
        try {
            writer = new StreamWriter(LogPath + fileName);
        } catch (DirectoryNotFoundException) {
            Debug.LogError($"Penalty Counter: Log Path location doesn't exits. Please change it. ");
        } catch (Exception e) {
            Debug.LogError($"{e.Message}");
        }
        
        // Write column names
        writer.WriteLineAsync("Collision with Fire, Collision with Pedestrian" + Environment.NewLine);
    }

    private void OnDisable() {
        // Log data
        writer.WriteLine($"{collisionWithFire}, {collisionWithPedestrian}");
        writer.Close();
        Debug.Log("Penalty Counter: Log saved to: " + LogPath);
    }

    // Increase counters it collides with a fire or 
    private void OnTriggerEnter(Collider other) {
        var layer = other.gameObject.layer;
        if (layer == 8) {
            collisionWithFire++;
        } else if (layer == 9) {
            collisionWithPedestrian++;
        }
    }
}