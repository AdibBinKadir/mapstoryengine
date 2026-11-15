using NUnit.Framework;
using UnityEngine;

public class GazeMathTests
{
    static readonly Vector3 L = new Vector3(-0.03f, 0f, 0f);
    static readonly Vector3 R = new Vector3(0.03f, 0f, 0f);
    static readonly Vector3 Cam = new Vector3(0f, 0f, 0.4f);

    static Vector3 AtDegrees(float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r)) * 0.3f;
    }

    [Test] public void LookingStraightAtCameraIsZero() { Assert.Less(GazeMath.GazeAngleDegrees(L, R, new Vector3(0f, 0f, 0.3f), Cam), 0.01f); }
    [Test] public void LookingSidewaysIsNinety() { Assert.AreEqual(90f, GazeMath.GazeAngleDegrees(L, R, new Vector3(1f, 0f, 0f), Cam), 0.01f); }
    [Test] public void FifteenDegreesOffIsLooking() { float a = GazeMath.GazeAngleDegrees(L, R, AtDegrees(15f), Cam); Assert.IsTrue(GazeMath.Looking(true, false, a, 20f)); }
    [Test] public void TwentyFiveDegreesOffIsNot() { float a = GazeMath.GazeAngleDegrees(L, R, AtDegrees(25f), Cam); Assert.IsFalse(GazeMath.Looking(true, false, a, 20f)); }
    [Test] public void ZeroGazeVectorIsNotLooking() { Assert.AreEqual(180f, GazeMath.GazeAngleDegrees(L, R, Vector3.zero, Cam)); }
    [Test] public void EyesClosedNeedsBoth() { Assert.IsTrue(GazeMath.EyesClosed(0.6f, 0.6f, 0.5f)); Assert.IsFalse(GazeMath.EyesClosed(0.6f, 0.2f, 0.5f)); Assert.IsTrue(GazeMath.EyesClosed(0.5f, 0.5f, 0.5f)); }
    [Test] public void ClosedEyesAreNeverLooking() { Assert.IsFalse(GazeMath.Looking(true, true, 0f, 20f)); }
    [Test] public void NoFaceIsNeverLooking() { Assert.IsFalse(GazeMath.Looking(false, false, 0f, 20f)); }
}
