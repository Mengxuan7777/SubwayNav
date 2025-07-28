using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Text;

namespace Camera {
    public class ImageAnalyzer: MonoBehaviour {
        // References
        public CameraManager cameraManager;
        public Pathfinding.PathManager pathfindingManager;
        
        // Prompt
        private readonly string prompt = "Analyze the following images and provide a reasoning for the results.";

        // Path to your Python interpreter and script
        // Configure these paths according to your local Python installation
        private readonly string pythonExePath = @"C:\Users\tower\AppData\Local\Microsoft\WindowsApps\python.exe"; // Change this to your python.exe
        private readonly string pythonScriptPath = Path.Combine(Application.dataPath, "Scripts/Camera/", "analyze_images.py"); // adjust subfolder as needed
        
        /// <summary>
        /// Calls python function to analyze the images using the OpenAI AI
        /// </summary>
        /// <param name="path">Folder where images are stored</param>
        /// <param name="extension">File format of images</param>
        public async void AnalyzeImages(string path, string extension = "jpg") {
            try {
                // Make sure folder is not empty
                var imagesPaths = GetImagesPaths(path, extension);
                if (imagesPaths.Count == 0) {
                    Debug.LogWarning("[Python]: No images for analysis.");
                    return;
                }

                // Call Python script asynchronously
                string analysisResult = await RunPythonScript(path, prompt);

                Debug.Log($"[Python Result]: {analysisResult}");
                // Optionally: Handle parsing the JSON result here

            } catch (Exception e) {
                Debug.Log($"[Python]: {e.Message}");
            }
        }

        /// <summary>
        /// Calls python function to analyze images
        /// </summary>
        /// <param name="imageFolder">Folder where images are stored</param>
        /// <param name="promptText">Prompt to give to ChatGPT</param>
        /// <returns></returns>
        private async Task<string> RunPythonScript(string imageFolder, string promptText) {
            var output = new StringBuilder();
            var error = new StringBuilder();
            var start = new ProcessStartInfo {
                FileName = pythonExePath,
                Arguments = $"\"{pythonScriptPath}\" \"{imageFolder}\" \"{promptText.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = new Process { StartInfo = start };
            process.OutputDataReceived += (sender, args) => { if (args.Data != null) output.AppendLine(args.Data); };
            process.ErrorDataReceived += (sender, args) => { if (args.Data != null) error.AppendLine(args.Data); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Run(() => process.WaitForExit());
            if (error.Length > 0)
                throw new Exception($"Python error: {error}");
            return output.ToString().Trim();
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
            }catch (Exception e) {
                Debug.LogWarning($"An error occurred while searching for images: {e.Message}");
            }

            return images;
        }
    }
}