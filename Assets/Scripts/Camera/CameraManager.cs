using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.Serialization;

namespace Camera {
    public class CameraManager : MonoBehaviour {
        [Header("Cameras")]
        public UnityEngine.Camera[] cameras;
        private RecorderController[] recorders; // [ cameras.Length]
    
        [Header("Recording Settings")]
        [Tooltip("To start recording automatically when the play button is pressed")] 
        public bool AutomaticRecording = true;
        [Tooltip("Interval between recordings in seconds.")] 
        public float RecordingInterval = 60f;
        [Tooltip("Duration of recordings in seconds.")] 
        public float RecordingDuration = 10f;
        public float FrameRate = 30f;
        [Tooltip("Options: 1080p, 720p, 480p. Defaults to 720p.")] 
        public string Resolution = "720p";
        private int width, height;
       
        void Start() {
            // Generate recorders
            SetResolution();
            GenerateRecorders();
            
            // Start Recording from the start
            if (AutomaticRecording) {
                StartCoroutine(RecordRoutine());
            }
           
        }
        
        private void GenerateRecorders() {
        #if UNITY_EDITOR
            recorders = new RecorderController[cameras.Length];
            for (int i = 0; i < cameras.Length; i++) {
                // Recorder Settings
                var cameraControllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
                cameraControllerSettings.ExitPlayMode = false;
                cameraControllerSettings.FrameRate = FrameRate;
                
                // Video Settings
                var movieRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
                movieRecorder.name = cameras[i].name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                movieRecorder.Enabled = true;
                movieRecorder.ImageInputSettings = new CameraInputSettings {
                    Source = ImageSource.TaggedCamera,
                    CameraTag = cameras[i].tag,
                    OutputHeight = height,
                    OutputWidth = width,
                    FlipFinalOutput = true // Ensures video is not flipped.
                };
                
                // Video File Settings
                movieRecorder.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
                movieRecorder.VideoBitRateMode = VideoBitrateMode.Medium;
                movieRecorder.CaptureAudio = false; // Reduce file size 
                movieRecorder.OutputFile = $"Recordings/{movieRecorder.name}";

                cameraControllerSettings.AddRecorderSettings(movieRecorder);
                cameraControllerSettings.SetRecordModeToManual();
                
                // Add recorders to list
                recorders[i] = new RecorderController(cameraControllerSettings);
            }
        #endif
        }

        /// <summary>
        /// Determines the resolution based on what the user wants. 
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
                default: // Defaults to 720p
                    width = 1280; height = 720;
                    Debug.LogWarning($"{Resolution} is not a valid resolution. Resolution set to 720p.");
                    break;
            }
        }

        /// <summary>
        /// Records clips to use with VLM.
        /// </summary>
        /// <returns>Fake returns.</returns>
        private IEnumerator RecordRoutine() {
            while (true) {
                // Start 10 sec recording
                StartAllRecordings();
                yield return new WaitForSeconds(RecordingDuration);
                
                // Stop recording and wait a minute
                StopAllRecordings();
                yield return new WaitForSeconds(RecordingInterval);
            }
        }
        
        /// <summary>
        /// Start all recordings for all cameras
        /// </summary>
        private void StartAllRecordings() {
        #if UNITY_EDITOR
            if (recorders == null) return;
            foreach (var controller in recorders) {
                if (controller != null && controller.IsRecording() == false) {
                    controller.PrepareRecording();
                    controller.StartRecording();

                }
            }
        #endif
        }

        /// <summary>
        /// Stops all recordings
        /// </summary>
        private void StopAllRecordings() {
        #if UNITY_EDITOR
            if (recorders == null) return;
            foreach (var controller in recorders) {
                if (controller != null && controller.IsRecording()) {
                    controller.StopRecording();
                }
            }
        #endif
 
        }
    }
}
