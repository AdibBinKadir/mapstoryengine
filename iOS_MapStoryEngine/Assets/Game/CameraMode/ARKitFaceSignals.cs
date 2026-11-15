using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Real IFaceSignals on iPhone (TrueDepth): reads the tracked face from ARCameraModeSwitcher's ARFaceManager.
// Looking = face tracked, eyes open, and the eyes' fixation point aims at the phone camera.
public class ARKitFaceSignals : MonoBehaviour, IFaceSignals
{
    public ARCameraModeSwitcher switcher;
    public float closedThreshold = GazeMath.ClosedThreshold;
    public float gazeMaxDegrees = GazeMath.GazeMaxDegrees;
    public float dropoutGraceSeconds = GazeMath.DropoutGraceSeconds;

    DropoutGrace grace;
    bool streaming;
    bool warnedNoBlinks;

    public bool FaceFound { get; private set; }
    public bool EyesClosed { get; private set; }
    public bool LookingAtScreen { get; private set; }
    public bool MirrorSeen { get { return false; } }
    public float GazeAngle { get; private set; }   // debug readout

    public void StartStreaming()
    {
        streaming = true;
        grace = new DropoutGrace(dropoutGraceSeconds);
    }

    public void StopStreaming()
    {
        streaming = false;
        if (grace != null) grace.Reset();
        Clear();
    }

    void Update()
    {
        if (!streaming || switcher == null || switcher.FaceManager == null || !switcher.FaceManager.enabled)
        {
            Clear();
            return;
        }

        ARFace face = null;
        foreach (var f in switcher.FaceManager.trackables)
        {
            if (f.trackingState == TrackingState.Tracking) { face = f; break; }
        }

        FaceFound = face != null;
        EyesClosed = false;
        GazeAngle = 180f;

        if (FaceFound)
        {
            float left, right;
            if (ARKitBlinks.TryGetBlinks(switcher.FaceManager, face, out left, out right))
            {
                EyesClosed = GazeMath.EyesClosed(left, right, closedThreshold);
            }
            else if (!warnedNoBlinks)
            {
                warnedNoBlinks = true;
                Debug.LogWarning("[ARKitFaceSignals] ARKit Face Tracking package missing: eyes-closed detection off.");
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                var origin = FindObjectOfType<ARSessionOrigin>();
                if (origin != null) cam = origin.camera;
            }
            if (cam != null && face.leftEye != null && face.rightEye != null && face.fixationPoint != null)
            {
                GazeAngle = GazeMath.GazeAngleDegrees(face.leftEye.position, face.rightEye.position,
                    face.fixationPoint.position, cam.transform.position);
            }
        }

        bool raw = GazeMath.Looking(FaceFound, EyesClosed, GazeAngle, gazeMaxDegrees);
        LookingAtScreen = grace.Step(raw, Time.unscaledDeltaTime);
    }

    void Clear()
    {
        FaceFound = false;
        EyesClosed = false;
        LookingAtScreen = false;
        GazeAngle = 180f;
    }
}
