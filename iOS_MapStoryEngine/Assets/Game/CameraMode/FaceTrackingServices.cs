using UnityEngine;

// Registers the real iPhone face source and camera switcher. In the Editor it leaves the mocks alone.
// Farhan: put this on GameSystems.prefab when integrating.
[DefaultExecutionOrder(-100)]
public class FaceTrackingServices : MonoBehaviour
{
    ARCameraModeSwitcher switcher;
    ARKitFaceSignals signals;

    void Awake()
    {
#if UNITY_IOS && !UNITY_EDITOR
        switcher = gameObject.AddComponent<ARCameraModeSwitcher>();
        signals = gameObject.AddComponent<ARKitFaceSignals>();
        signals.switcher = switcher;
        if (Services.CameraMode == null) Services.CameraMode = switcher;
        if (Services.Face == null) Services.Face = signals;
        Debug.Log("[FaceTrackingServices] ARKit face tracking registered.");
#else
        Debug.Log("[FaceTrackingServices] editor: leaving mocks in place");
#endif
    }

    void OnDestroy()
    {
        if (switcher != null && ReferenceEquals(Services.CameraMode, switcher)) Services.CameraMode = null;
        if (signals != null && ReferenceEquals(Services.Face, signals)) Services.Face = null;
    }
}
