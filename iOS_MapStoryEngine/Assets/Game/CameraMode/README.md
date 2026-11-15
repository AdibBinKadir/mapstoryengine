# CameraMode: Medusa on iPhone (ARKit face tracking)

While Medusa runs, `ARCameraModeSwitcher` pauses the rear-AR features and hides the camera feed. It then turns on front-camera (TrueDepth) face tracking on the same AR session. `ARKitFaceSignals` turns the tracked face into `IFaceSignals`:
- **Looking** = a face is found, both eye-blink values are < 0.5, and the gaze is within 20° of the phone.
- Tracking gaps under 0.1 s are bridged.

`FaceTrackingServices` registers both services, on the iOS device only. In the Editor, `KeyboardFaceSignals` (F/E/L/M keys) is used instead.

Spec: `docs/superpowers/specs/2026-11-15-medusa-iphone-face-tracking-design.md`

## Farhan: before the first phone build
1. **Package (required):** add `"com.unity.xr.arkit-face-tracking": "3.0.1"` to `Packages/manifest.json`. This is the release built for ARKit XR Plugin 3.0.x; 3.1.x would pull ARKit 3.1. Without it there is **no face tracking at all**: `face` stays False, so nobody can ever be STONED. Xcode logs `No face subsystem: install com.unity.xr.arkit-face-tracking 3.0.1 …`.
2. **Face tracking setting:** in *Project Settings → XR Plug-in Management → ARKit*, enable face tracking if the option is listed.
3. **Camera usage text** (Player Settings → iOS): mention face tracking, e.g. *"Uses the camera for AR and to detect when you look at Medusa."*
4. **Build `Assets/Game/Sandbox/P2.unity` as the only scene** to an iPhone with Face ID (X or newer). Reapply the `changes.md` Xcode fixes.
5. **Later:** add `FaceTrackingServices` to `GameSystems.prefab` for the main game.

## Device checks (P2.unity)
Use the on-screen buttons; the top-left label shows `face / eyesClosed / looking / gaze`.
1. Tap **Begin**. You should see a flicker, then MEDUSA IS COMING, then her image. Within about 1 s of her image, the label shows `face=True`.
2. Look at the screen with your eyes open → STONED in about 0.5 s.
3. Keep your eyes closed → `eyesClosed=True`. The clue appears 5–15 s in, then she's gone (won).
4. Look 30° or more away from the phone → never STONED.
5. After the encounter ends or after **Abort**, the camera background and the rear AR come back. Xcode logs `front mode off; restored: ...` listing the same components that `front mode on; paused: ...` listed.
6. Tap **Begin** twice, then **Abort** twice → no errors in the Xcode console.
7. **Rear AR survives Medusa:** in `MapStory.unity`, place an AR object (e.g. the Sphinx), run Medusa, then check the object is still where it was. Switching ARKit to face tracking and back may restart world tracking; if objects jump, report it.

**If `face` stays False:**
- First check Xcode for `No face subsystem`. If it's there, the package from step 1 is missing or the phone has no Face ID.
- Otherwise the camera didn't swap to the front.
- Retest after adding `FindObjectOfType<ARSession>().Reset()` at the end of `EnterFrontCameraMode` and `ExitFrontCameraMode`.
- Then report back. This is the main open risk.

## Tuning
On `ARKitFaceSignals`: `closedThreshold` (0.5), `gazeMaxDegrees` (20) and `dropoutGraceSeconds` (0.1).
