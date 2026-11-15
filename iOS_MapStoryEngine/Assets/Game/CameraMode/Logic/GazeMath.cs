using UnityEngine;

// Pure rules for "is the player looking into Medusa's eyes", fed from ARKit face data.
public static class GazeMath
{
    public const float ClosedThreshold = 0.5f, GazeMaxDegrees = 20f, DropoutGraceSeconds = 0.1f;

    // Angle between where the eyes look (fixation - eye midpoint) and where the phone camera is (camera - eye midpoint).
    // 180 when either direction is degenerate, so a broken reading never counts as looking.
    public static float GazeAngleDegrees(Vector3 leftEye, Vector3 rightEye, Vector3 fixationPoint, Vector3 cameraPosition)
    {
        Vector3 eyeMid = (leftEye + rightEye) * 0.5f;
        Vector3 gaze = fixationPoint - eyeMid;
        Vector3 toCamera = cameraPosition - eyeMid;
        if (gaze.sqrMagnitude < 1e-8f || toCamera.sqrMagnitude < 1e-8f) return 180f;
        return Vector3.Angle(gaze, toCamera);
    }

    public static bool EyesClosed(float blinkLeft, float blinkRight, float threshold)
    {
        return blinkLeft >= threshold && blinkRight >= threshold;
    }

    public static bool Looking(bool faceFound, bool eyesClosed, float gazeAngleDegrees, float maxDegrees)
    {
        return faceFound && !eyesClosed && gazeAngleDegrees < maxDegrees;
    }
}
