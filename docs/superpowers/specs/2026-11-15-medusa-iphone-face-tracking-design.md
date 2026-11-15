# Medusa on iPhone: ARKit face tracking (design)

## Why
The Medusa MVP only works in the Editor, with the keyboard mock. On the phone nothing sets `Services.Face`, so nobody can ever be STONED, and nothing pauses the rear AR. This adds real on-phone detection through ARKit face tracking ("Option B", the owner's choice at CP1), with no laptop or server needed.

## Goals
- A real `IFaceSignals` on iPhone: `FaceFound`, `EyesClosed`, `LookingAtScreen` (false whenever the eyes are closed).
- A real `ICameraModeSwitcher`: pause the rear AR and track the face with the front TrueDepth camera, with the feed **never displayed**; then restore the rear AR exactly as it was.
- Testable on a phone on its own through a phone-friendly `P2.unity`, before Farhan wires it into `MapStory.unity`.
- The Editor sandbox keeps working unchanged with `KeyboardFaceSignals`.

## Non-goals
- Mirror detection (`MirrorSeen` stays false).
- The server `/frame` path (not built now; the contract stays as is).
- Face tracking while the rear AR keeps running (ARKit 3 "user face tracking"): A12+ only, no clear control in AR Foundation 3.1, and the plan says the rear AR pauses.
- Android.

## Anti-goals
- Showing the front-camera feed at any time.
- Leaving the rear AR broken after Medusa (planes or raycasts still off, or the camera background still hidden).
- Breaking the Editor build when the face-tracking package is missing.

## Constraints
- Unity 2019.4, C# 7.3, AR Foundation 3.1.3, ARKit XR Plugin 3.0.4. No AR Foundation 4+ APIs (`requestedFacingDirection` and similar).
- Verified in the package cache: `ARFaceManager` and `ARFace.leftEye` / `rightEye` / `fixationPoint` exist in 3.1.3.
- Blink values (ARKit blend shapes `eyeBlinkLeft` / `eyeBlinkRight`) need the separate **ARKit Face Tracking** package (`com.unity.xr.arkit-face-tracking`), which isn't installed. `Packages/` and `ProjectSettings/` belong to Farhan.
- Only P2 folders are edited: `Assets/Game/CameraMode/`, `Assets/Game/Encounters/Medusa/`, `Assets/Game/Sandbox/P2.unity`.

## Approaches
1. **Chosen: swap the one AR session to face tracking while Medusa runs.** Turn off the rear-AR managers and `ARCameraBackground`, then turn on an `ARFaceManager`, and reverse this on exit. It works on iPhone X and newer, matches the plan's "rear AR paused", and needs no laptop.
2. Simultaneous world and face tracking (ARKit 3): rejected for the reasons in Non-goals.
3. Server `/frame` with a hidden `WebCamTexture`: rejected by the owner. It needs the laptop, Local Network permission and an ATS exception, and it adds 0.2–0.4 s of delay.

## Design

**`CameraMode/ARCameraModeSwitcher.cs`**, a MonoBehaviour that implements `ICameraModeSwitcher`.
- It finds `ARSessionOrigin` and `ARSession` at runtime (`FindObjectOfType`), the same way `PlaceObject` and Cyclops already do. No edit to Farhan's scene.
- **Enter:**
  - Record which of these are enabled: every AR manager component on the origin (planes, point clouds, raycasts, anchors, images, …) and `ARCameraBackground` on the AR camera.
  - Disable them.
  - Get or add an `ARFaceManager` on the origin (`maximumFaceCount = 1`) and enable it.
- **Exit:** disable the face manager, and re-enable exactly the components that were enabled before.
- Calling Enter twice, or Exit without Enter, does nothing.
- `FrontCameraTexture` returns `null`, which the contract allows for the Face ID path.

**`CameraMode/ARKitFaceSignals.cs`**, a MonoBehaviour that implements `IFaceSignals`.
- `StartStreaming()` / `StopStreaming()` gate the readings. While not streaming, everything reads false.
- Each frame it takes the tracked face from the `ARFaceManager`:
  - `FaceFound`: a face with `trackingState == Tracking`.
  - `EyesClosed`: both blink values ≥ `closedThreshold` (0.5).
  - `LookingAtScreen`: `FaceFound && !EyesClosed` and the gaze angle < `gazeMaxDegrees` (20). The gaze angle is measured between (fixation point − midpoint of the eyes) and (AR camera position − midpoint of the eyes).
- **Dropout grace** (my call): `LookingAtScreen` stays true through tracking gaps shorter than 0.1 s, so one dropped frame doesn't reset Medusa's 0.5 s count. Gaps of 0.1 s or longer still read false.
- All thresholds are public fields.

**Blink reader** in its own small assembly, `CameraMode/ARKitFace/` (`Game.CameraMode.ARKitFace.asmdef`):
- It references the AR Foundation, ARSubsystems and ARKit Face Tracking assemblies.
- The asmdef's `versionDefines` set `ARKIT_FACE_TRACKING` only when the package is installed.
- With the package: `TryGetBlinks(ARFace face, out float left, out float right)` reads the ARKit blend shapes.
- Without the package: it returns false. `ARKitFaceSignals` then treats the eyes as open and logs one warning. The game still works, but closing your eyes won't protect you.

**`CameraMode/GazeMath.cs`**, in a small pure assembly `Game.CameraMode.Logic` with EditMode tests:
- The gaze-angle rule.
- The blink rule.
- The dropout-grace timer.

**`CameraMode/FaceTrackingServices.cs`** (bootstrap):
- On an iOS device where face tracking is supported, it adds the two components above and sets `Services.Face` and `Services.CameraMode`. It only sets them if they're still null.
- In the Editor it does nothing, so the keyboard mock stays in charge.
- Farhan adds it to `GameSystems.prefab` when integrating.

**Phone test in `P2.unity`:**
- Add `AR Session` and `AR Session Origin` with an AR camera, plus `FaceTrackingServices`.
- `MedusaSandbox` gains on-screen buttons: **Begin**, **Abort**, and toggles for clue answering and abort-on-kill. Its debug label shows `face`, `eyesClosed`, `looking` and the gaze angle.
- Farhan builds `P2.unity` as the only scene for the first phone test.

## Needs Farhan (blocking the phone test)
1. Add `com.unity.xr.arkit-face-tracking` at the version that matches ARKit XR Plugin 3.0.4. Pick it in Package Manager; it's likely 3.0.x.
2. Turn on face tracking in the ARKit XR settings, if 3.0.4 shows that option.
3. Make sure the camera usage text mentions face tracking.
4. Build `P2.unity` to an iPhone X or newer.
5. Later: put `FaceTrackingServices` on `GameSystems.prefab`.

## Open risks (resolved on the first phone build)
- **Camera swap:** that turning off the rear managers and turning on `ARFaceManager` really switches ARKit 3.0.4 to the front camera. If it doesn't, the fallback is to restart the session (`ARSession.Reset()`) after the swap.
- **Gaze direction:** how reliable `fixationPoint` is. If it's noisy, use head direction (face forward vs camera) together with the eye check.
- **Thresholds:** 0.5 for blinks, 20° for gaze, 0.1 s for grace. Tune them on the device.

## Left to the builder
How to find the AR camera, log formats, and the button layout in the sandbox.
