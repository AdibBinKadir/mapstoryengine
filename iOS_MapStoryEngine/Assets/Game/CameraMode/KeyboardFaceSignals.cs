using UnityEngine;

// Editor/desktop mock for IFaceSignals. F = face, E = eyes closed, L = looking, M = mirror.
public class KeyboardFaceSignals : MonoBehaviour, IFaceSignals
{
    public bool face = true;
    public bool eyesClosed = false;
    public bool looking = false;
    public bool mirror = false;

    public bool FaceFound { get { return face; } }
    public bool EyesClosed { get { return eyesClosed; } }
    public bool LookingAtScreen { get { return face && !eyesClosed && looking; } }
    public bool MirrorSeen { get { return mirror; } }

    public void StartStreaming()
    {
        Debug.Log("[KeyboardFaceSignals] streaming on");
    }

    public void StopStreaming()
    {
        Debug.Log("[KeyboardFaceSignals] streaming off");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) face = !face;
        if (Input.GetKeyDown(KeyCode.E)) eyesClosed = !eyesClosed;
        if (Input.GetKeyDown(KeyCode.L)) looking = !looking;
        if (Input.GetKeyDown(KeyCode.M)) mirror = !mirror;
    }
}
