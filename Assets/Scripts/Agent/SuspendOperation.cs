using Camera;
using UnityEditor;
using UnityEngine;

namespace Agent {
    public class SuspendOperation : MonoBehaviour {

        // References
        [SerializeField] public CameraManager cameraManager;

        private void OnTriggerEnter(Collider other) {
            // If the agent reaches exit, stop the coroutine.
            if (other.gameObject.layer == 7) {
                Debug.Log("Suspending all operations.");
                StopCoroutine(cameraManager.ScreenshotRoutine());
                
                // Disable Editor
                if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            }

            
        }
    }
}
