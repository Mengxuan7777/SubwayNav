using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Camera {
    public class CameraManager : MonoBehaviour {
        [Header("Cameras")]
        public UnityEngine.Camera[] cameras;

        [Header("Screenshot Settings")]
        [Tooltip("Take screenshots automatically when the play button is pressed")] 
        public bool AutomaticScreenshot = true;
        [Tooltip("Interval between screenshots in seconds.")] 
        public float ScreenshotInterval = 30f;
        [Tooltip("Options: 1080p, 720p, 480p. Defaults to 720p.")] 
        public string Resolution = "720p";
        [Tooltip("Options: png or jpg. Defaults to jpg.")] 
        public string FileFormat = "jpg";
        public string FolderPath ;
        private int width, height;
        
        private void Awake() {
            // Create File Directory
            FolderPath = Application.persistentDataPath + "/Screenshots"; 
            if (!Directory.Exists(FolderPath)) {
                Directory.CreateDirectory(FolderPath);
                Debug.Log($"New Directory Created: {FolderPath}");
            }
            // Set resolution
            SetResolution();
        }
        
        void Start() {
            // Take screenshots automatically when the play button is pressed.
            if (AutomaticScreenshot) {
                StartCoroutine(ScreenshotRoutine());
            }
        }

        private void OnDisable() {
            // Delete all generated screenshots
            string[] files = Directory.GetFiles(FolderPath, $"*.{FileFormat}");
            foreach (var file in files) {
                File.Delete(file);
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
        
        private IEnumerator ScreenshotRoutine() {
            while (true) {
                TakeScreenshots();
                yield return new WaitForSeconds(ScreenshotInterval);
            }
            // ReSharper disable once IteratorNeverReturns
        }

        /// <summary>
        /// Takes a picture in each of the cameras
        /// </summary>
        private void TakeScreenshots() {
            foreach (var cam in cameras) {
                // Set up RenderTexture
                RenderTexture rt = new RenderTexture(width, height, 24);
                cam.targetTexture = rt;
                Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);

                cam.Render();
                RenderTexture.active = rt;
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                Destroy(rt);

                // Save file
                string fileName = $"{cam.name}_{DateTime.Now:yyyyMMdd_HHmmss}.{FileFormat}";
                string fullPath = Path.Combine(FolderPath, fileName);

                if (FileFormat == "jpg") {
                    File.WriteAllBytes(fullPath, screenshot.EncodeToJPG());
                }
                File.WriteAllBytes(fullPath, screenshot.EncodeToPNG());
            }
        }
    }
}