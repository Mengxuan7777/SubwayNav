using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class CamTag : MonoBehaviour
{
    [Header("References")]
    public UnityEngine.Camera boundCamera;   // camera this tag belongs to
    public Canvas canvas;                    // overlay canvas
    public TextMeshProUGUI label;            // text element

    private void Awake()
    {
        if (boundCamera == null)
            boundCamera = GetComponentInParent<UnityEngine.Camera>();

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = boundCamera;
            canvas.planeDistance = 1f;
        }

        UpdateLabel();
    }

    public void Bind(UnityEngine.Camera cam)
    {
        boundCamera = cam;
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;   // <— this is the missing link
            canvas.planeDistance = 1f;
        }
        // Debug.Log($"[CamTag] Bound to {cam.name}");
        UpdateLabel();
    }

    public void UpdateLabel()
    {
        if (label == null || boundCamera == null) return;
        label.text = boundCamera.name;   // only show the camera’s name
    }
}
