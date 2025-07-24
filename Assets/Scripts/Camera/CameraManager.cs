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
            // Set up
            FolderPath = Application.persistentDataPath + "/Screenshots"; 
            SetResolution();
        }
        
        void Start() {
            // Take screenshots automatically when the play button is pressed.
            if (AutomaticScreenshot) {
                StartCoroutine(ScreenshotRoutine());
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
                yield return StartCoroutine(TakeScreenshots());
                yield return new WaitForSeconds(ScreenshotInterval);
            }
        }

        private IEnumerator TakeScreenshots() {
            for (int i = 0; i < cameras.Length; i++) {
                yield return StartCoroutine(CaptureCameraScreenshot(cameras[i], i));
            }
        }

        private IEnumerator CaptureCameraScreenshot(UnityEngine.Camera cam, int index) {
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
            if (!Directory.Exists(FolderPath)) {
                Directory.CreateDirectory(FolderPath);
                Debug.Log($"New Directory Created: {FolderPath}");
            }
            
            string fileName = $"{cam.name}_{DateTime.Now:yyyyMMdd_HHmmss}.{FileFormat}";
            string fullPath = Path.Combine(FolderPath, fileName);

            if (FileFormat == "jpg") {
                File.WriteAllBytes(fullPath, screenshot.EncodeToJPG());
            }
            File.WriteAllBytes(fullPath, screenshot.EncodeToPNG());
            yield return null;
        }
    }
}