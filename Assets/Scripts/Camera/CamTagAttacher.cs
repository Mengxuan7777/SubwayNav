using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CamTagAttacher : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Your CamTag canvas prefab (root has the CamTag script).")]
    [SerializeField] private GameObject camTagPrefab;

    [Header("Target Cameras")]
    [Tooltip("Optional: assign specific cameras. If empty and Auto Find is ON, all scene cameras are used.")]
    [SerializeField] private List<UnityEngine.Camera> cameras = new List<UnityEngine.Camera>();
    [SerializeField] private bool autoFindAllCameras = true;

    [Header("When To Attach")]
    [SerializeField] private bool attachOnStart = true;

    private void Start()
    {
        if (attachOnStart) AttachNow();
    }

    /// <summary>
    /// Attach CamTag prefab to each target camera (safe to call multiple times).
    /// </summary>
    public void AttachNow()
    {
        if (camTagPrefab == null)
        {
            Debug.LogError("[CamTagAttacher] camTagPrefab is not assigned.");
            return;
        }

        // Collect target cameras
        var targets = new List<UnityEngine.Camera>();
        if (cameras != null && cameras.Count > 0)
        {
            targets.AddRange(cameras);
        }
        else if (autoFindAllCameras)
        {
            targets.AddRange(GameObject.FindObjectsOfType<UnityEngine.Camera>());
        }
        else
        {
            Debug.LogWarning("[CamTagAttacher] No cameras provided and autoFindAllCameras is false.");
            return;
        }

        // Attach or refresh
        foreach (var cam in targets)
        {
            if (cam == null) continue;

            var existing = cam.GetComponentInChildren<CamTag>(true);
            if (existing != null)
            {
                BindAndRefresh(existing, cam);
                continue;
            }

            var instance = Instantiate(camTagPrefab, cam.transform);
            instance.name = $"CamTag_{cam.name}";

            var tag = instance.GetComponent<CamTag>();
            if (tag == null)
            {
                Debug.LogWarning($"[CamTagAttacher] Prefab '{camTagPrefab.name}' has no CamTag component on root.");
                continue;
            }

            BindAndRefresh(tag, cam);
        }
    }

    /// <summary>
    /// If camera names change at runtime, call this to refresh all labels.
    /// </summary>
    public void RefreshAllTags()
    {
        var allTags = GameObject.FindObjectsOfType<CamTag>(true);
        foreach (var tag in allTags) tag.UpdateLabel();
    }

    private void BindAndRefresh(CamTag tag, UnityEngine.Camera cam)
    {
        tag.Bind(cam);     // simplified CamTag only needs the Camera
        tag.UpdateLabel(); // shows cam.name
    }
}
