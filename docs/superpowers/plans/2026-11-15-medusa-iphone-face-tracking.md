# Medusa on iPhone (ARKit Face Tracking) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Real on-phone Medusa detection. `ARKitFaceSignals` (an `IFaceSignals`) and `ARCameraModeSwitcher` (an `ICameraModeSwitcher`) swap the one AR session to front-camera face tracking while Medusa runs. `P2.unity` becomes a phone test scene.

**Architecture:**
- The pure rules (gaze angle, blink, dropout grace) live in `Game.CameraMode.Logic` with EditMode tests.
- The blink read from ARKit blend shapes sits in its own asmdef. It compiles to a stub unless the ARKit Face Tracking package is installed (`versionDefines`).
- MonoBehaviours in `Assets/Game/CameraMode/` (Assembly-CSharp) glue AR Foundation 3.1.3 to the contracts.
- A bootstrap registers both services on the device only.

**Tech Stack:** Unity 2019.4.41f2 (C# 7.3), AR Foundation 3.1.3, ARKit XR Plugin 3.0.4, optional `com.unity.xr.arkit-face-tracking`, Unity Test Framework (EditMode), Unity MCP for refresh/tests/scene edits.

**Spec:** `docs/superpowers/specs/2026-11-15-medusa-iphone-face-tracking-design.md`

## Global Constraints

- Edit only `iOS_MapStoryEngine/Assets/Game/CameraMode/`, `iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/` and `iOS_MapStoryEngine/Assets/Game/Sandbox/P2.unity`. Never touch `Packages/`, `ProjectSettings/`, `Core/Contracts/`, `MapStory.unity` or `Assets/Mapbox/`.
- C# 7.3, no namespaces for game types. AR Foundation **3.1** APIs only; no `requestedFacingDirection`, `XROrigin` or other 4+ APIs.
- `.meta` files come from Unity only: refresh Unity via MCP, wait for the `.meta` files, then commit each file together with its `.meta`. Use `git add` with explicit paths only.
- If the Unity MCP stops answering for more than 30 s, ask the human to dismiss any Editor dialog or focus the Editor.
- The project **must compile without the face-tracking package**, in the Editor and on the device.
- Exact values:
  - closed threshold **0.5** (both eyes ≥ 0.5 = closed);
  - gaze max **20°**;
  - dropout grace **0.1 s**;
  - `ARFaceManager.maximumFaceCount = 1`;
  - `FrontCameraTexture` returns `null`.
- The front-camera feed is never displayed: `ARCameraBackground` is off while in front-camera mode.
- Branch: `p2/medusa-iphone`, created from `p2/medusa-mvp`.

## Review Focus

1. **Exit restores only what was on.** Plane detection was off (PlaceObject turns it off after placing), so Exit must not turn it back on. Pinned by Task 3's manual check 4, which turns the plane manager off before Begin.
2. **Enter/Exit called twice, or Exit without Enter** → no-op, with no exception. Pinned by Task 3's manual check 5.
3. **Face package missing** → still compiles; blinks read as open, with one warning logged. Pinned by Task 2 Step 3 (the compile check without the package) and its log check.
4. **Tracking flickers for a single frame** → the looking count isn't reset. Pinned by `DropoutGraceTests` (Task 1).
5. **Bootstrap order:** `FaceTrackingServices` and `MedusaSandbox` both run at startup → the device must end up with the AR face source, never the keyboard mock. Pinned by Task 4 (the sandbox only adds the keyboard in `Start`, in the Editor, when `Services.Face` is still null).

---

### Task 1: `GazeMath` + `DropoutGrace` (pure, tested)

**Files:**
- Create: `Assets/Game/CameraMode/Logic/Game.CameraMode.Logic.asmdef` → `{ "name": "Game.CameraMode.Logic", "references": [], "autoReferenced": true }`
- Create: `Assets/Game/CameraMode/Logic/GazeMath.cs`, `Assets/Game/CameraMode/Logic/DropoutGrace.cs`
- Create: `Assets/Game/CameraMode/Tests/Game.CameraMode.Tests.asmdef`. Same shape as `Encounters/Medusa/Tests/Game.Medusa.Tests.asmdef`, but referencing `Game.CameraMode.Logic`.
- Test: `Assets/Game/CameraMode/Tests/GazeMathTests.cs`, `Assets/Game/CameraMode/Tests/DropoutGraceTests.cs`

**Interfaces (produces):**
```csharp
public static class GazeMath
{
    public const float ClosedThreshold = 0.5f, GazeMaxDegrees = 20f, DropoutGraceSeconds = 0.1f;
    // Angle between (fixation - eyeMid) and (camera - eyeMid); eyeMid = midpoint of the eyes. 180 if either vector is ~zero.
    public static float GazeAngleDegrees(Vector3 leftEye, Vector3 rightEye, Vector3 fixationPoint, Vector3 cameraPosition);
    public static bool EyesClosed(float blinkLeft, float blinkRight, float threshold);   // both >= threshold
    public static bool Looking(bool faceFound, bool eyesClosed, float gazeAngleDegrees, float maxDegrees); // found && !closed && angle < max
}
public sealed class DropoutGrace
{
    public DropoutGrace(float graceSeconds);
    // raw true -> true and the gap resets; raw false -> true only while it was true before and the gap < grace.
    public bool Step(bool raw, float deltaSeconds);
}
```

- [ ] **Step 1: Write the failing tests** (NUnit `[Test]`s):
```csharp
// GazeMathTests (eyes at (-0.03,0,0) and (0.03,0,0); camera at (0,0,0.4))
LookingStraightAtCameraIsZero:  GazeAngleDegrees(L, R, new Vector3(0,0,0.3f), cam) < 0.01
LookingSidewaysIsNinety:        Assert.AreEqual(90f, GazeAngleDegrees(L, R, new Vector3(1,0,0), cam), 0.01f)
FifteenDegreesOffIsLooking:     fixation = (sin15°,0,cos15°)*0.3 → Looking(true,false,angle,20f) is true
TwentyFiveDegreesOffIsNot:      fixation at 25° → Looking(...) is false
ZeroGazeVectorIsNotLooking:     fixation == eye midpoint → angle == 180
EyesClosedNeedsBoth:            EyesClosed(0.6f,0.6f,0.5f) true; EyesClosed(0.6f,0.2f,0.5f) false; EyesClosed(0.5f,0.5f,0.5f) true
ClosedEyesAreNeverLooking:      Looking(true, true, 0f, 20f) false
NoFaceIsNeverLooking:           Looking(false, false, 0f, 20f) false
// DropoutGraceTests (grace 0.1)
FalseStartStaysFalse:           new DropoutGrace(0.1f).Step(false, 0.016f) false
ShortGapIsBridged:              Step(true,.016) ; Step(false,.05) true ; Step(false,.04) true
LongGapEnds:                    Step(true,.016) ; Step(false,.06) ; Step(false,.05) false
TrueResetsTheGap:               Step(true,.016); Step(false,.08); Step(true,.016); Step(false,.08) true
```
- [ ] **Step 2: Run to verify they fail.** Refresh Unity (MCP `refresh_unity`, `mode: force`), then read the console. Expected: `error CS0246` for `GazeMath` / `DropoutGrace`.
- [ ] **Step 3: Implement both** in their files. Use `Vector3.Angle` and treat `sqrMagnitude < 1e-8` as zero.
- [ ] **Step 4: Run the tests.** MCP `run_tests` with `mode: EditMode` and `test_names: ["GazeMathTests","DropoutGraceTests","MedusaTimelineTests"]`, then poll `get_test_job`. Expected: all pass, 12 new + 19 existing.
- [ ] **Step 5: Commit** the `CameraMode/Logic` and `CameraMode/Tests` folders with their `.meta` files: `feat(camera): gaze/blink rules and dropout grace with EditMode tests`

---

### Task 2: Blink reader asmdef (compiles with or without the package)

**Files:**
- Create: `Assets/Game/CameraMode/ARKitFace/Game.CameraMode.ARKitFace.asmdef`:
```json
{
  "name": "Game.CameraMode.ARKitFace",
  "references": ["Unity.XR.ARFoundation", "Unity.XR.ARSubsystems", "Unity.XR.ARKit.FaceTracking"],
  "autoReferenced": true,
  "versionDefines": [
    { "name": "com.unity.xr.arkit-face-tracking", "expression": "1.0.0", "define": "ARKIT_FACE_TRACKING" }
  ]
}
```
  (A missing assembly reference by name is ignored by Unity; check this in Step 3.)
- Create: `Assets/Game/CameraMode/ARKitFace/ARKitBlinks.cs`

**Interfaces (produces):**
```csharp
public static class ARKitBlinks
{
    public static bool Available { get; }   // true only with ARKIT_FACE_TRACKING && UNITY_IOS && !UNITY_EDITOR
    public static bool TryGetBlinks(ARFaceManager manager, ARFace face, out float left, out float right);
}
```
- [ ] **Step 1: Implement.**
  - Under `#if ARKIT_FACE_TRACKING && UNITY_IOS && !UNITY_EDITOR`:
    - cast `manager.subsystem` to `UnityEngine.XR.ARKit.ARKitFaceSubsystem`;
    - call `GetBlendShapeCoefficients(face.trackableId, Allocator.Temp)` inside `using (...)`, the C# 7.3 block form;
    - pick `ARKitBlendShapeLocation.EyeBlinkLeft` / `EyeBlinkRight`.
  - Check those names against the installed package source before relying on them.
  - Otherwise: `left = right = 0`, return false.
- [ ] **Step 2: Refresh Unity.** Expected: no `error CS` in the console. The package is absent, so this exercises the stub path.
- [ ] **Step 3: Confirm** that the console has no "missing assembly reference" error for `Unity.XR.ARKit.FaceTracking`.
  - **If it does:** remove that reference and the `#if` body, then ledger a ruling. Farhan restores both once the package is installed; record that as a PR note.
- [ ] **Step 4: Commit** `CameraMode/ARKitFace` with its `.meta` files: `feat(camera): optional ARKit blink reader`

---

### Task 3: `ARCameraModeSwitcher` + `ARKitFaceSignals`

**Files:**
- Create: `Assets/Game/CameraMode/ARCameraModeSwitcher.cs`, `Assets/Game/CameraMode/ARKitFaceSignals.cs`

**Interfaces:**
- Consumes: `GazeMath`, `DropoutGrace` (Task 1); `ARKitBlinks` (Task 2); contracts `ICameraModeSwitcher`, `IFaceSignals`.
- Produces:
  - `public class ARCameraModeSwitcher : MonoBehaviour, ICameraModeSwitcher`, with `public bool InFrontMode { get; }` and `public ARFaceManager FaceManager { get; }` (null until the first Enter).
  - `public class ARKitFaceSignals : MonoBehaviour, IFaceSignals`, with public fields `ARCameraModeSwitcher switcher`, `float closedThreshold = GazeMath.ClosedThreshold`, `float gazeMaxDegrees = GazeMath.GazeMaxDegrees`, `float dropoutGraceSeconds = GazeMath.DropoutGraceSeconds`, plus a read-only `public float GazeAngle { get; }` for debug.

- [ ] **Step 1: Implement `ARCameraModeSwitcher`.**
  - **EnterFrontCameraMode:**
    - If already in front mode, return.
    - Find `ARSessionOrigin` (`FindObjectOfType`); if there is none, log a warning and return.
    - Collect every enabled `Behaviour` on the origin whose type namespace is `UnityEngine.XR.ARFoundation`, except `ARSessionOrigin` and `ARFaceManager`.
    - Also collect the `ARCameraBackground` on `origin.camera`, if enabled.
    - Store that list and disable each item.
    - Get or add an `ARFaceManager` on the origin, set `maximumFaceCount = 1`, enable it, and set `InFrontMode = true`.
  - **ExitFrontCameraMode:**
    - If not in front mode, return.
    - Disable `FaceManager`, re-enable exactly the stored list (skipping any that are now null), clear the list, set `InFrontMode = false`.
  - **`FrontCameraTexture`** returns null.
  - **`OnDisable`** calls `ExitFrontCameraMode()`.
  - Log one line per Enter/Exit with the names of the components it toggled.
- [ ] **Step 2: Implement `ARKitFaceSignals`.**
  - `StartStreaming` / `StopStreaming` set `streaming`; StopStreaming also resets the `DropoutGrace`.
  - `Update`, when `streaming && switcher.FaceManager != null`:
    - Take the first `ARFace` in `FaceManager.trackables` whose `trackingState == TrackingState.Tracking`.
    - `FaceFound` = one exists.
    - Blinks come from `ARKitBlinks.TryGetBlinks`. If that returns false, the eyes are open, with one warning per session ("ARKit Face Tracking package missing: eyes-closed detection off").
    - `GazeAngle` comes from `face.leftEye/rightEye/fixationPoint` world positions and `Camera.main` (fall back to the origin camera). If any eye transform is null, `GazeAngle = 180`.
    - `LookingAtScreen = grace.Step(GazeMath.Looking(...), Time.unscaledDeltaTime)`.
  - When not streaming, everything is false.
  - `MirrorSeen` is always false.
- [ ] **Step 3: Refresh Unity, then check the console.** Expected: no `error CS`.
- [ ] **Step 4: Commit** both files with their `.meta`s: `feat(camera): ARKit front-camera switcher and face signals`

Manual device checks for this task are listed in Task 5 (they need the phone).

---

### Task 4: `FaceTrackingServices` bootstrap + phone-friendly sandbox

**Files:**
- Create: `Assets/Game/CameraMode/FaceTrackingServices.cs`
- Modify: `Assets/Game/Encounters/Medusa/MedusaSandbox.cs`
- Modify: `Assets/Game/Sandbox/P2.unity` (via the Unity MCP)

**Interfaces:**
- Consumes: Task 3 types.
- Produces: `public class FaceTrackingServices : MonoBehaviour`, with `[DefaultExecutionOrder(-100)]`.

- [ ] **Step 1: Implement `FaceTrackingServices.Awake`.**
  - Only `#if UNITY_IOS && !UNITY_EDITOR`; in the Editor it logs `[FaceTrackingServices] editor: leaving mocks in place` and returns.
  - Add `ARCameraModeSwitcher` and `ARKitFaceSignals` to this GameObject, and set `signals.switcher`.
  - Set `Services.CameraMode` / `Services.Face` only if each is null.
  - `OnDestroy` clears the ones it set (a `ReferenceEquals` check).
- [ ] **Step 2: Change `MedusaSandbox`.**
  - Move the keyboard fallback from `Awake` to `Start`, and only when `Application.isEditor && Services.Face == null`.
  - In `OnGUI`, add `GUI.Button`s, about 220×110 px each in a row at the bottom-left: **Begin**, **Abort**, **Clues: on/off**, **Abort on kill: on/off**. Scale the IMGUI font with `GUI.skin.label.fontSize = 32` / `button.fontSize = 32` on device.
  - Show `GazeAngle` when `Services.Face is ARKitFaceSignals`.
  - Keep the keys.
- [ ] **Step 3: Build out `P2.unity` via the MCP.**
  - `AR Session` GO: `ARSession` + `ARInputManager`.
  - `AR Session Origin` GO: `ARSessionOrigin`, plus a child `AR Camera` with `Camera` (tag MainCamera, clear flags Solid Color black), `ARCameraManager`, `ARCameraBackground` and `TrackedPoseDriver`. Set the origin's `camera` field to it.
  - Delete the old `Main Camera`.
  - `Systems` GO with `FaceTrackingServices`.
  - Save.
- [ ] **Step 4: Editor regression.**
  - Refresh, console clean.
  - Rerun all EditMode tests (expected: 31 pass).
  - Ask the human to press Play in `P2.unity` and run the MVP checklist (B/E/L/C/K/A) plus a click on the on-screen **Begin**. Expected: same behaviour as before; the AR components only log warnings in the Editor.
- [ ] **Step 5: Commit** `FaceTrackingServices.cs` + `.meta`, `MedusaSandbox.cs` and `P2.unity`: `feat(camera): device bootstrap and phone-testable P2 sandbox`

---

### Task 5: Handoff for the phone build + PR

- [ ] **Step 1: Write `Assets/Game/CameraMode/README.md`.** It holds Farhan's checklist and the device test.
  - **Farhan's checklist:**
    - Add `com.unity.xr.arkit-face-tracking` at the version Package Manager offers for ARKit 3.0.4.
    - Enable face tracking in *Project Settings → XR → ARKit*, if listed.
    - The camera usage text mentions face tracking.
    - If Task 2 Step 3 removed it, restore the `Unity.XR.ARKit.FaceTracking` reference and the `#if` body.
    - Build `P2.unity` as the only scene to an iPhone X or newer.
  - **Device checks** (each must hold):
    1. **Begin**: hijack, then countdown, then her image, and the debug label shows `face=True` within about 1 s.
    2. Look at the screen with eyes open: STONED in about 0.5 s.
    3. Close your eyes: `eyesClosed=True`, and the clue comes and goes (won).
    4. Turn plane detection off in some test (or note what was on), then **Begin**/**Abort**: after Exit, the rear AR comes back with exactly the same managers on.
    5. Press **Begin** twice, then **Abort** twice: no errors in the Xcode console.
    6. Try looking away 30° or more: never stoned.
  - **If the camera doesn't swap to the front:** call `ARSession.Reset()` after Enter and after Exit, then retest.
- [ ] **Step 2: Commit** the README + `.meta`. Push `p2/medusa-iphone` **after the human confirms**, then open a PR to `main` (stacked on `p2/medusa-mvp`). It must include "How to test" (Editor + device), the Farhan checklist, and the open risks from the spec.
