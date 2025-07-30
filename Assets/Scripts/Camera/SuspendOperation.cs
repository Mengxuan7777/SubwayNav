using UnityEngine;

namespace Camera {
    public class SuspendOperation : MonoBehaviour {
    
        // References
        public CameraManager cameraManager;

        private void OnTriggerEnter(Collider other) {
            // If the agent reaches exit, stop the coroutine.
            Debug.Log("Trigger Enter");
            if (other.gameObject.layer == 7) {
                Debug.Log("Suspending all operations.");
                StopCoroutine(cameraManager.ScreenshotRoutine());
            }
        }
        
    }
}
