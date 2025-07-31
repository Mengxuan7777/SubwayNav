using System.Collections.Generic;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Camera {
    public class ImageAnalyzer: MonoBehaviour {
        // References
        [SerializeField]  public CameraManager cameraManager;
        [SerializeField]  public Pathfinding.PathManager pathfindingManager;

        // Path to your Python interpreter and script
        private readonly string pythonExePath = @"C:\Users\tower\AppData\Local\Microsoft\WindowsApps\python.exe"; 
        private readonly string pythonScriptPath = Path.Combine(Application.dataPath, "Scripts/Camera/", "analyze_images.py");
        
        // Reference Image folder
        private static readonly string referenceImageFolder = Application.streamingAssetsPath + "/VLM_Training";
        
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Pushes analysis of images and calculation of path to a background thread
        /// </summary>
        /// <param name="path"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public IEnumerator AnalyzeImagesAndRecalculatePathCoroutine(string path, string extension = "jpg") {
            // Start the analysis/task
            var analysisTask = AnalyzeImagesAndRecalculatePath(path, extension);

            // Wait for the task to finish without blocking the main thread
            while (!analysisTask.IsCompleted) yield return null;

            // Handle exceptions (if any)
            if (analysisTask.Exception != null)
                Debug.LogError(analysisTask.Exception);
        }
        
        
        /// <summary>
        /// Calls python function to analyze the images using the OpenAI AI
        /// </summary>
        /// <param name="path">Folder where images are stored</param>
        /// <param name="extension">File format of images</param>
        private async Task AnalyzeImagesAndRecalculatePath(string path, string extension = "jpg") {
            // Make sure folder is not empty
            var imagesPaths = GetImagesPaths(path, extension);
            if (imagesPaths.Count == 0) {
                Debug.LogWarning("[Python]: No images for analysis.");
                return;
            }

            // Call Python script asynchronously
            // It also updated the edge costs
            await RunPythonScript(path, referenceImageFolder);
            
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
        /// <param name="referenceFolder"></param>
        /// <returns></returns>
        private async Task RunPythonScript(string imageFolder, string referenceFolder) {
            await Task.Run(() => {
                var start = new ProcessStartInfo {
                    FileName = pythonExePath,
                    Arguments = $"\"{pythonScriptPath}\" \"{imageFolder}\" \"{referenceFolder}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var process = new Process();
                process.StartInfo = start;
                process.Start();

                // Read errors in the background
                _ = Task.Run(async () => {
                    while (!process.StandardError.EndOfStream) {
                        var errLine = await process.StandardError.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(errLine)) Debug.LogError($"[Python Error]: {errLine}");
                    }
                });

                // Stream output line by line as batches finish
                while (!process.StandardOutput.EndOfStream) {
                    var line = process.StandardOutput.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line)) {
                        // This line updates Unity objects: needs to be back on main thread
                        MainThreadDispatcher.Enqueue(() => pathfindingManager.UpdateEdgeWeights(line));
                        Debug.Log($"[Python Result (batch)]: {line}");
                    }
                }
                process.WaitForExit();
            });
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
                images.AddRange(Directory.GetFiles(path, $"*.{extension}"));
            } catch (Exception e) {
                Debug.LogWarning($"An error occurred while searching for images: {e.Message}");
            }

            return images;
        }
    }
}