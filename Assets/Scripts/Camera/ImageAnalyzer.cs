using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Camera {
    public class ImageAnalyzer: MonoBehaviour {
        // References
        public CameraManager cameraManager;
        public Pathfinding.PathManager pathfindingManager;

        // Prompt
        private readonly string prompt = "Analyze the following images and provide a reasoning for the results.1. For each image, return an object with fields: name (string), crowdCost (integer), fireCost (integer). 'name' the image filename without the file format. crowdCost represents the crowdness on a scale of 0-10. fireCost represents the fire danger on a scale of 0-10. Output a JSON array of these objects, and nothing else.";

        // Path to your Python interpreter and script
        private readonly string pythonExePath = @"C:\Users\tower\AppData\Local\Microsoft\WindowsApps\python.exe"; // Change this to your python.exe
        private readonly string pythonScriptPath = Path.Combine(Application.dataPath, "Scripts/Camera/", "analyze_images.py"); // adjust subfolder as needed

        /// <summary>
        /// Calls python function to analyze the images using the OpenAI AI
        /// </summary>
        /// <param name="path">Folder where images are stored</param>
        /// <param name="extension">File format of images</param>
        public async Task AnalyzeImagesAndRecalculatePath(string path, string extension = "jpg") {
            // Make sure folder is not empty
            var imagesPaths = GetImagesPaths(path, extension);
            if (imagesPaths.Count == 0) {
                Debug.LogWarning("[Python]: No images for analysis.");
                return;
            }

            // Call Python script asynchronously
            // It also updated the edge costs
            await RunPythonScript(path, prompt);
            
            // Re-calculate the path using A*
            pathfindingManager.ReCalculatePath();
            try {
                
            } catch (Exception e) {
                Debug.LogError($"[Python]: An error occurred while analyzing images: {e.Message}");;
            }
        }

        /// <summary>
        /// Calls python function to analyze images and updated edge weights
        /// </summary>
        /// <param name="imageFolder">Folder where images are stored</param>
        /// <param name="promptText">Prompt to give to ChatGPT</param>
        /// <returns></returns>
        private async Task RunPythonScript(string imageFolder, string promptText) {
            var start = new ProcessStartInfo {
                FileName = pythonExePath,
                Arguments = $"\"{pythonScriptPath}\" \"{imageFolder}\" \"{promptText.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = new Process { StartInfo = start };
            process.Start();

            // Read errors in the background
            _ = Task.Run(async () => {
                while (!process.StandardError.EndOfStream) {
                    var errLine = await process.StandardError.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(errLine))
                    {
                        Debug.LogError($"[Python Error]: {errLine}");
                    }
                }
            });
            
            // Stream output line by line as batches finish
            while (!process.StandardOutput.EndOfStream) {
                var line = await process.StandardOutput.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line)) {
                    Debug.Log($"[Python Result (batch)]: {line}");
                    pathfindingManager.UpdateEdgeWeights(line);
                }
            }
            process.WaitForExit();
        }

        /// <summary>
        /// Gathers the directories of all the images in the given folder.
        /// </summary>
        private List<string> GetImagesPaths(string path, string extension = "jpg") {
            if (!Directory.Exists(path)) {
                Debug.LogError($"Directory does not exist: {cameraManager.FolderPath}");
                return new List<string>();
            }

            var images = new List<string>();
            try {
                foreach (var file in Directory.GetFiles(path, $"*.{extension}")) {
                    images.Add(file);
                }
                Debug.Log($"Found {images.Count} images in {path}");
            } catch (Exception e) {
                Debug.LogWarning($"An error occurred while searching for images: {e.Message}");
            }

            return images;
        }
    }
}