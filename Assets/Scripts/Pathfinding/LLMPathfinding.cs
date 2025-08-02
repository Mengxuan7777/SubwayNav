using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI.Chat;
using UnityEngine;

namespace Pathfinding {
    public class LLMPathfinding {
        private const string filepath = "/Users/tower/Documents/API Keys/OpenAI_ChatGPT_Key.txt";

        // Static ChatClient (lazy-loaded or created on-demand for each call)
        private static readonly ChatClient client;

        // Static constructor
        static LLMPathfinding() {
            try {
                // Initialize the client only once
                var apiKey = File.ReadAllText(filepath); // This is synchronous but runs only once
                client = new ChatClient("gpt-4o", apiKey);
            } catch (Exception e) {
                Debug.LogError($"[LLM Pathfinding] Failed to initialize ChatClient: {e.Message}");
            }
        }

        // Static method
        public static async Task<List<string>> FindPath(List<Node> nodes, List<Node> exists, Node start) {
            try {
                var nodesObj = new {nodes, exists, start};
                var jsonGraph = JsonConvert.SerializeObject(nodesObj);

                // Get Response from ChatGPT
                ChatCompletion completion = await client.CompleteChatAsync(
                    @$"Below is the information for all nodes, exit nodes, and the start node
                                     of a graph representing a subway station. Using this graph, identify the 
                                     path to the safest exit. Return the path as a list of nodes.  Do not provide
                                     any explanation or reasoning.
                                       {jsonGraph}");

                var response = completion.Content[0].Text;

                // Deserialize and return the result
                Debug.Log($"[LLM Pathfinding]: {response}");
                return JsonConvert.DeserializeObject<List<string>>(response);
            } catch (Exception e) {
                Debug.LogError($"[LLM Pathfinding]: {e.Message}");
                return null;
            }
        }
    }
}