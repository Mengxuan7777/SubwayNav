using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Camera {
    public class CameraManager : MonoBehaviour {
        // References
        [Header("References")]
        [SerializeField] public ImageAnalyzer imageAnalyzer;
        
        // Camera References & Info
        [Header("Cameras")]
        [SerializeField] public UnityEngine.Camera[] cameras;
        private RenderTexture[] cameraRenderTextures;
        private Texture2D[] cameraTextures;
        private readonly object fileLock = new ();

        // Settings
        [Header("Screenshot Settings")]
        [Tooltip("Take screenshots automatically when the play button is pressed")] 
        [SerializeField] public bool AutomaticScreenshot = true;
        [Tooltip("Interval between screenshots in seconds.")] 
        [SerializeField] public float ScreenshotInterval = 30f;
        [Tooltip("Options: 1080p, 720p, 480p. Defaults to 720p.")] 
        [SerializeField] public string Resolution = "720p";
        private int width, height;
        [Tooltip("Options: png or jpg. Defaults to jpg.")] 
        [SerializeField] public string FileFormat = "jpg";
        
        // Storage of Images
        private static string path;
        [SerializeField] public string FolderPath;
        private int folderCount;
        private int gpuReadbacksInProgress = 0;
        private const int MaxConcurrentReadbacks = 16;

        
        private void Awake() {
            // Create primary folder is not already created
            path = Application.persistentDataPath + "/Screenshots/";
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
                Debug.Log($"New Directory Created: {path}");
            }
            
            // Configure Cameras
            SetResolution();
            InitializeTextures();
        }

        private void Start() {
            // Set textures to cameras
            for (int i = 0; i < cameras.Length; i++) {
                cameras[i].targetTexture = cameraRenderTextures[i];
            }
            
            // Take screenshots automatically when the play button is pressed.
            if (AutomaticScreenshot) {
                StartCoroutine(ScreenshotRoutine());
            }
        }

        private void OnDisable() {
            // Delete all generated screenshots
            string[] dirs = Directory.GetDirectories(path);
            foreach (var dir in dirs) {
                Directory.Delete(dir, true); 
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Routinely take screenshots of all cameras in a folder.
        /// </summary>
        /// <returns></returns>
        public IEnumerator ScreenshotRoutine() {
            // Add small delay
            yield return new WaitForSeconds(1f);
            
            // Continuously take screenshots until the play button is pressed
            while (true) {
                FolderPath = Path.Combine(path, "Folder" + folderCount++);
                var createTask = CreateScreenshotFolderAsync(FolderPath);
                while (!createTask.IsCompleted) yield return null;

                // Take all screenshots
                yield return TakeScreenshots();
                Debug.Log($"Finished taking screenshots of all {cameras.Length} cameras. Saved to: {FolderPath}");
                
                // Delete older images
                var deleteTask = DeleteOldFoldersAsync();
                while (!deleteTask.IsCompleted) yield return null;
                
                // Analyze and Re-calculate the path
                yield return StartCoroutine(
                    imageAnalyzer.AnalyzeImagesAndRecalculatePathCoroutine(FolderPath, FileFormat)
                );

                yield return new WaitForSeconds(ScreenshotInterval);
            }
            // ReSharper disable once IteratorNeverReturns
        }

        /// <summary>
        /// Takes a picture in each of the cameras in quick succession
        /// Warning: This only works in Unity 2018.2+
        /// </summary>
        private IEnumerator TakeScreenshots() {
            for (int i = 0; i < cameras.Length; i++) {
                // Wait until you have a slot for a new request
                while (gpuReadbacksInProgress >= MaxConcurrentReadbacks)
                    yield return null;

                UnityEngine.Camera cam = cameras[i];
                var rt = cameraRenderTextures[i];
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;

                // Start async GPU texture readback
                var cameraIndex = i;
                gpuReadbacksInProgress++;
                AsyncGPUReadback.Request(rt, 0, TextureFormat.RGB24, (req) => {
                    if (!req.hasError) {
                        // Gets raw texture data
                        var tex = cameraTextures[cameraIndex];
                        tex.LoadRawTextureData(req.GetData<byte>());
                        tex.Apply();
                        
                        // Determine encoding
                        byte[] imageBytes = (FileFormat.ToLower() == "jpg")
                            ? tex.EncodeToJPG(80)
                            : tex.EncodeToPNG();
                        
                        // Create filepath
                        string fileName = $"{cam.name}.{FileFormat}";
                        string fullPath = Path.Combine(FolderPath, fileName);

                        // Threaded file write
                        WriteImageFileAsync(fullPath, imageBytes);
                    }
                    gpuReadbacksInProgress--;
                });
            }
            // Wait for any remaining readbacks to fully complete
            while (gpuReadbacksInProgress > 0)
                yield return null;
        }
        
        /// <summary>
        /// Pushes folder creation to background thread, but notify the main thread on completion
        /// </summary>
        /// <param name="folderPath"></param>
        private static async Task CreateScreenshotFolderAsync(string folderPath) {
            await Task.Run(() => {
                if (!Directory.Exists(folderPath)) {
                    Directory.CreateDirectory(folderPath);
                }
            });
        }
        
        /// <summary>
        /// Pushes file writing to a background thread
        /// </summary>
        /// <param name="filePath">Path where image is to be written</param>
        /// <param name="data">Image data</param>
        private static void WriteImageFileAsync(string filePath, byte[] data) {
            Task.Run(() => {
                File.WriteAllBytes(filePath, data);
            });
        }
        
        /// <summary>
        /// Push the deletion of older images to a background thread.
        /// </summary>
        private static async Task DeleteOldFoldersAsync() {
            try {
                await Task.Run(() => {
                    string[] folders = Directory.GetDirectories(path);
                    if (folders.Length > 5) {
                        Array.Sort(folders); // Sort by name (assumes Folder0, Folder1, ...)
                        for (int i = 0; i < folders.Length - 5; i++) {
                            Directory.Delete(folders[i], true);
                        }
                    }
                });
            } catch (Exception e) {
                Debug.LogWarning($"Error Deleting Folder: {e.Message}");
            }
        }
        
        /// <summary>
        /// Determines the resolution of the pictures based on input string
        /// </summary>
        private void SetResolution() {
            switch (Resolution) {
                case "1080p":
                    width = 1920; height = 1080;
                    break;
                case "720p":
                    width = 1280; height = 720;
                    break;
                case "480p":
                    width = 854; height = 480;
                    break;
                default:
                    width = 1280; height = 720;
                    Debug.LogWarning($"{Resolution} is not a valid resolution. Resolution set to 720p.");
                    break;
            }
        }
        
        /// <summary>
        /// Initializes the render textures for all cameras.
        /// </summary>
        private void InitializeTextures() {
            cameraRenderTextures = new RenderTexture[cameras.Length];
            cameraTextures = new Texture2D[cameras.Length];
            for (int i = 0; i < cameras.Length; i++) {
                cameraRenderTextures[i] = new RenderTexture(width, height, 24);
                cameraTextures[i] = new Texture2D(width, height, TextureFormat.RGB24, false);
            }
        }
    }
}