using UnityEngine.XR.ARFoundation;
#if ARKIT_FACE_TRACKING && UNITY_IOS && !UNITY_EDITOR
using Unity.Collections;
using UnityEngine.XR.ARKit;
#endif

// Eye-blink values from ARKit blend shapes. Needs the optional com.unity.xr.arkit-face-tracking package;
// without it (or in the Editor) this compiles to a stub that reports "not available".
public static class ARKitBlinks
{
    public static bool Available
    {
        get
        {
#if ARKIT_FACE_TRACKING && UNITY_IOS && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }

    public static bool TryGetBlinks(ARFaceManager manager, ARFace face, out float left, out float right)
    {
        left = 0f;
        right = 0f;
#if ARKIT_FACE_TRACKING && UNITY_IOS && !UNITY_EDITOR
        if (manager == null || face == null) return false;
        var subsystem = manager.subsystem as ARKitFaceSubsystem;
        if (subsystem == null) return false;

        bool foundLeft = false, foundRight = false;
        using (var coefficients = subsystem.GetBlendShapeCoefficients(face.trackableId, Allocator.Temp))
        {
            for (int i = 0; i < coefficients.Length; i++)
            {
                var c = coefficients[i];
                if (c.blendShapeLocation == ARKitBlendShapeLocation.EyeBlinkLeft) { left = c.coefficient; foundLeft = true; }
                else if (c.blendShapeLocation == ARKitBlendShapeLocation.EyeBlinkRight) { right = c.coefficient; foundRight = true; }
            }
        }
        return foundLeft && foundRight;
#else
        return false;
#endif
    }
}
