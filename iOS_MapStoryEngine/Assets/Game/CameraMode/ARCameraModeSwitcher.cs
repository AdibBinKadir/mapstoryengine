using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

// Medusa's camera swap on iPhone: pause the rear-AR features, hide the camera feed and turn on
// front-camera (TrueDepth) face tracking on the same AR session. Exit restores exactly what was on.
public class ARCameraModeSwitcher : MonoBehaviour, ICameraModeSwitcher
{
    readonly List<Behaviour> pausedComponents = new List<Behaviour>();

    public bool InFrontMode { get; private set; }
    public ARFaceManager FaceManager { get; private set; }
    public WebCamTexture FrontCameraTexture { get { return null; } }   // Face ID path: no texture

    public void EnterFrontCameraMode()
    {
        if (InFrontMode) return;

        var origin = FindObjectOfType<ARSessionOrigin>();
        if (origin == null)
        {
            Debug.LogWarning("[ARCameraModeSwitcher] No ARSessionOrigin in the scene; front-camera mode not started.");
            return;
        }

        pausedComponents.Clear();
        foreach (var behaviour in origin.GetComponents<Behaviour>())
        {
            if (behaviour == null || !behaviour.enabled) continue;
            if (behaviour is ARSessionOrigin || behaviour is ARFaceManager) continue;
            if (behaviour.GetType().Namespace != "UnityEngine.XR.ARFoundation") continue;
            pausedComponents.Add(behaviour);
        }
        if (origin.camera != null)
        {
            var background = origin.camera.GetComponent<ARCameraBackground>();
            if (background != null && background.enabled) pausedComponents.Add(background);   // never show the front feed
        }
        foreach (var behaviour in pausedComponents) behaviour.enabled = false;

        if (FaceManager == null)
        {
            FaceManager = origin.GetComponent<ARFaceManager>();
            if (FaceManager == null) FaceManager = origin.gameObject.AddComponent<ARFaceManager>();
        }
        FaceManager.maximumFaceCount = 1;
        FaceManager.enabled = true;
        InFrontMode = true;

        Debug.Log("[ARCameraModeSwitcher] front mode on; paused: " + Names(pausedComponents));
    }

    public void ExitFrontCameraMode()
    {
        if (!InFrontMode) return;

        if (FaceManager != null) FaceManager.enabled = false;
        foreach (var behaviour in pausedComponents)
        {
            if (behaviour != null) behaviour.enabled = true;
        }
        Debug.Log("[ARCameraModeSwitcher] front mode off; restored: " + Names(pausedComponents));
        pausedComponents.Clear();
        InFrontMode = false;
    }

    void OnDisable()
    {
        ExitFrontCameraMode();
    }

    static string Names(List<Behaviour> behaviours)
    {
        var sb = new StringBuilder();
        foreach (var b in behaviours)
        {
            if (b == null) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(b.GetType().Name);
        }
        return sb.Length > 0 ? sb.ToString() : "(none)";
    }
}
