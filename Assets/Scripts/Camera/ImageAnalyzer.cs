namespace Camera {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using OpenAI.Chat;
    using UnityEngine;
    using UnityEngine.AI;

    public class ImageAnalyzer {
        // References
        public CameraManager cameraManager;
        public Pathfinding.PathManager pathfindingManager;

        // File Reader
        private static readonly string filepath = "/Users/tower/Documents/API Keys/OpenAI_ChatGPT_Key.txt";
        private readonly StreamReader reader = new(filepath);

        // Open AI
        private string apiKey;
        private ChatClient client;

        void Start() {
            // Set up GPT client
            apiKey = reader.ReadLine();
            client = new ChatClient("gpt-4o-mini", apiKey);
        }


        // ReSharper disable Unity.PerformanceAnalysis
        private async void GetBestPath(List<NavMeshPath> paths, string jsonPaths) {
            try {
                // Get Response from GPT
                ChatCompletion completion = await client.CompleteChatAsync(
                    "Below is information regarding paths between a user multiple subway exits" +
                    "Give the distances of each of the paths and the distance of each waypoint of a path to the fire, provide the" +
                    "index of the best exit path. Prioritize avoiding the fire. Only provide a single number" +
                    $"{jsonPaths}");
                string msg = completion.Content[0].Text;
                Debug.Log($"[GPT]: {msg}");
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }
    }
}