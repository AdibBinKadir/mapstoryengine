# Medusa MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A full-screen Medusa takeover (hijack → "MEDUSA IS COMING" → danger → 2 s clue flash → won, or STONED), testable in the Unity Editor with a keyboard mock.

**Architecture:** The phase/timer rules live in a pure C# class, `MedusaTimeline`, in its own assembly so EditMode tests can reach it. `MedusaScreen` builds and updates the full-screen UI by code. `MedusaEncounter` (implements `IEncounter`) drives the timeline each frame from `Services.Face` and talks to the game only through `GameEvents`/`Services`. A sandbox script stands in for Farhan's ClueBook and GameState.

**Tech Stack:** Unity 2019.4.41f2 (C# 7.3), uGUI (`UnityEngine.UI`, built-in Arial font), Unity Test Framework 1.1.31 (NUnit, EditMode).

**Spec:** `docs/superpowers/specs/2026-11-15-medusa-mvp-design.md`

## Global Constraints

- Work only in `iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/`, `iOS_MapStoryEngine/Assets/Game/CameraMode/` and `iOS_MapStoryEngine/Assets/Game/Sandbox/`. Never edit `Assets/Game/Core/Contracts/`, `Packages/`, `ProjectSettings/`, `Assets/Mapbox/`, or `MapStory.unity`.
- C# 7.3 only. No switch expressions, records, nullable reference types, `using` declarations, or default interface methods. No namespaces (the contract types have none).
- **`.meta` files come from Unity only.** After creating files, refresh Unity (via the Unity MCP if it offers an asset refresh, otherwise ask the human to focus the Editor). Commit each file together with its `.meta`. Never hand-write a `.meta`.
- **Never commit** `iOS_MapStoryEngine/Packages/manifest.json`, `iOS_MapStoryEngine/Packages/packages-lock.json` (they hold a local-only MCP install) or the deleted `iOS_MapStoryEngine/Assets/Mapbox/Unity/Editor/Build.meta`. Always `git add` explicit paths, never `git add -A` or `git add .`.
- Exact values from the spec:
  - Hijack **1 s**, "MEDUSA IS COMING" **3 s**.
  - Clue appears at a random **5–15 s** into Danger and stays **2 s**.
  - STONED after `LookingAtScreen` is held **0.5 s** continuously, in Danger or Clue only.
  - STONED screen **2 s**. Canvas sort order **100**.
- Exact copy:
  - `MEDUSA IS COMING`
  - `STONED` / `You looked into Medusa's eyes.`
  - Fallback clue (when no ClueBook answers within **0.2 s**): `Ithaca hides from those who dare to look.`
- Detection is **eyes only** through `IFaceSignals.LookingAtScreen` (false whenever `EyesClosed`). No head, mirror, sound or vibration in the MVP.
- Branch: `p2/medusa-mvp`.

## Review Focus

1. **Frame hitch** (app backgrounded, phone call, editor pause) gives one huge `deltaSeconds` → the timeline must carry time across phases, and only count looking during the part of the step spent in Danger/Clue. Pinned by `BigStepCrossesPhasesAndOnlyCountsDangerTime` (Task 2).
2. **Flickering detection** (eyes detected open for a few frames, then not) → STONED only after 0.5 s of *continuous* looking. Pinned by `LookingMustBeContinuous` (Task 2).
3. **Re-entrancy on death:** `RequestKill("stoned")` may make GameState call `Abort()` on this encounter during the same call → exactly one `EncounterEnded`, no exceptions. Pinned by the sandbox's `K` mode in Task 5's manual check.
4. **Nobody answers the clue request** (ClueBook missing or no clues left) → fallback text after 0.2 s, never a blank clue box. Pinned by Task 5's manual check with `C` (clue answering off).
5. **`Begin()` while already running, or `Services.Face` not set** → the second `Begin()` is ignored; a null face source counts as "not looking", with one warning logged. Pinned by Task 5's manual check.

---

### Task 1: Preconditions (contract stubs present)

**Files:** none created.

- [ ] **Step 1: Update and check out the branch**

Run: `git fetch origin && git checkout p2/medusa-mvp && git rebase origin/main`
Expected: rebase succeeds. `git status` shows no staged changes.

- [ ] **Step 2: Verify the contract members Medusa needs exist**

Run: `grep -rn "RequestKill\|KillRequested\|EyesClosed\|LookingAtScreen\|RequestClue\|ClueUnlocked\|RaiseEncounterEnded\|interface IEncounter\|ICameraModeSwitcher CameraMode\|IFaceSignals Face" iOS_MapStoryEngine/Assets/Game/Core/Contracts/`
Expected: every name appears at least once, and `EncounterType` contains `Medusa`, `ClueTier` contains `Good`.
**If anything is missing: STOP.** Tell the human which member is missing. Farhan's stubs (and the proposed `GameEvents.KillRequested` / `RequestKill(string cause)`) must land first. Do not create or edit contract files.

- [ ] **Step 3: Commit the spec and this plan**

```bash
git add docs/superpowers/specs/2026-11-15-medusa-mvp-design.md docs/superpowers/plans/2026-11-15-medusa-mvp.md
git commit -m "docs: Medusa MVP design and implementation plan"
```

---

### Task 2: `MedusaTimeline` (pure logic + EditMode tests)

**Files:**
- Create: `iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/Logic/Game.Medusa.Logic.asmdef`
- Create: `iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/Logic/MedusaTimeline.cs`
- Create: `iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/Tests/Game.Medusa.Tests.asmdef`
- Test: `iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/Tests/MedusaTimelineTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public enum MedusaPhase { Hijack, Coming, Danger, Clue, Won, Stoned, Aborted }

  public sealed class MedusaTimeline
  {
      public const float HijackSeconds = 1f, ComingSeconds = 3f, ClueSeconds = 2f;
      public const float StoneAfterLookingSeconds = 0.5f, StonedScreenSeconds = 2f;
      public const float ClueEarliestSeconds = 5f, ClueLatestSeconds = 15f;

      public MedusaTimeline(float clueDelaySeconds);   // seconds into Danger; clamped to [5, 15]
      public MedusaPhase Phase { get; }                // starts at Hijack
      public bool IsOver { get; }                      // Won, Aborted, or Stoned after its 2 s screen
      public MedusaPhase Tick(float deltaSeconds, bool lookingAtScreen); // returns Phase after the step
      public void Abort();                             // -> Aborted, IsOver = true
  }
  ```
  Once `IsOver` is true, `Tick` changes nothing.

- [ ] **Step 1: Create the two assembly definitions**

`Logic/Game.Medusa.Logic.asmdef`:
```json
{ "name": "Game.Medusa.Logic", "references": [], "autoReferenced": true }
```
`Tests/Game.Medusa.Tests.asmdef`:
```json
{
  "name": "Game.Medusa.Tests",
  "references": ["Game.Medusa.Logic", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": ["Editor"],
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

- [ ] **Step 2: Write the failing tests** in `MedusaTimelineTests.cs` (NUnit `[Test]`s; helper `static MedusaTimeline AtDanger(float clueDelay)` = `new MedusaTimeline(clueDelay)` then `Tick(4f, false)`):

```csharp
[Test] public void StartsInHijack() { Assert.AreEqual(MedusaPhase.Hijack, new MedusaTimeline(10f).Phase); }
[Test] public void HijackLastsOneSecond() { var t = new MedusaTimeline(10f); Assert.AreEqual(MedusaPhase.Hijack, t.Tick(0.99f, false)); Assert.AreEqual(MedusaPhase.Coming, t.Tick(0.02f, false)); }
[Test] public void ComingLastsThreeSeconds() { var t = new MedusaTimeline(10f); t.Tick(1f, false); Assert.AreEqual(MedusaPhase.Coming, t.Tick(2.99f, false)); Assert.AreEqual(MedusaPhase.Danger, t.Tick(0.02f, false)); }
[Test] public void LookingIsSafeBeforeDanger() { var t = new MedusaTimeline(10f); t.Tick(0.9f, true); Assert.AreEqual(MedusaPhase.Coming, t.Tick(3f, true)); }
[Test] public void StonedAfterHalfSecondOfLooking() { var t = AtDanger(10f); Assert.AreEqual(MedusaPhase.Danger, t.Tick(0.3f, true)); Assert.AreEqual(MedusaPhase.Stoned, t.Tick(0.25f, true)); }
[Test] public void LookingMustBeContinuous() { var t = AtDanger(10f); t.Tick(0.3f, true); t.Tick(0.1f, false); Assert.AreEqual(MedusaPhase.Danger, t.Tick(0.3f, true)); }
[Test] public void EyesClosedNeverStones() { var t = AtDanger(15f); Assert.AreEqual(MedusaPhase.Danger, t.Tick(14f, false)); }
[Test] public void ClueStartsAtTheDelay() { var t = AtDanger(5f); Assert.AreEqual(MedusaPhase.Danger, t.Tick(4.99f, false)); Assert.AreEqual(MedusaPhase.Clue, t.Tick(0.02f, false)); }
[Test] public void WonAfterClueLastsTwoSeconds() { var t = AtDanger(5f); t.Tick(5f, false); Assert.AreEqual(MedusaPhase.Clue, t.Tick(1.99f, false)); Assert.AreEqual(MedusaPhase.Won, t.Tick(0.02f, false)); Assert.IsTrue(t.IsOver); }
[Test] public void LookingDuringClueStones() { var t = AtDanger(5f); t.Tick(5f, false); Assert.AreEqual(MedusaPhase.Stoned, t.Tick(0.5f, true)); }
[Test] public void LookingCarriesFromDangerIntoClue() { var t = AtDanger(5f); t.Tick(4.8f, false); t.Tick(0.3f, true); Assert.AreEqual(MedusaPhase.Stoned, t.Tick(0.25f, true)); }
[Test] public void StonedScreenLastsTwoSeconds() { var t = AtDanger(10f); t.Tick(0.5f, true); Assert.IsFalse(t.IsOver); t.Tick(1.9f, false); Assert.IsFalse(t.IsOver); t.Tick(0.2f, false); Assert.IsTrue(t.IsOver); Assert.AreEqual(MedusaPhase.Stoned, t.Phase); }
[Test] public void ClueDelayIsClamped() { var early = AtDanger(1f); Assert.AreEqual(MedusaPhase.Danger, early.Tick(4.9f, false)); Assert.AreEqual(MedusaPhase.Clue, early.Tick(0.2f, false)); var late = AtDanger(30f); Assert.AreEqual(MedusaPhase.Danger, late.Tick(14.9f, false)); Assert.AreEqual(MedusaPhase.Clue, late.Tick(0.2f, false)); }
[Test] public void BigStepCrossesPhasesAndOnlyCountsDangerTime() { Assert.AreEqual(MedusaPhase.Danger, new MedusaTimeline(10f).Tick(4f, true)); Assert.AreEqual(MedusaPhase.Stoned, new MedusaTimeline(10f).Tick(4.6f, true)); }
[Test] public void AbortEndsEverything() { var t = AtDanger(10f); t.Abort(); Assert.IsTrue(t.IsOver); Assert.AreEqual(MedusaPhase.Aborted, t.Tick(10f, true)); }
[Test] public void NothingChangesAfterWon() { var t = AtDanger(5f); t.Tick(7.1f, false); Assert.AreEqual(MedusaPhase.Won, t.Tick(5f, true)); }
```

- [ ] **Step 3: Run the tests to verify they fail**

Refresh Unity, then run EditMode tests. Either: *Window → General → Test Runner → EditMode → Run All* (ask the human, or use the Unity MCP if it has a test tool), or, with the Editor **closed**:
`"C:/Program Files/Unity/Hub/Editor/2019.4.41f2/Editor/Unity.exe" -batchmode -nographics -projectPath iOS_MapStoryEngine -runTests -testPlatform EditMode -testResults medusa-tests.xml -logFile -`
Expected: compile error `MedusaTimeline` / `MedusaPhase` not found.

- [ ] **Step 4: Implement `MedusaTimeline` in `Logic/MedusaTimeline.cs`**

Plain C#, no `UnityEngine` usage. `Tick` consumes `deltaSeconds` in a loop: for the current phase, take `min(remaining delta, time left in the phase)`; in Danger/Clue add it to the looking accumulator when `lookingAtScreen`, else reset the accumulator to 0. Become Stoned as soon as the accumulator reaches 0.5. Advance phases until the delta is used up or `IsOver`. The accumulator is **not** reset on the Danger → Clue transition. In Stoned, time counts toward the 2 s screen, then `IsOver = true`.

- [ ] **Step 5: Run the tests to verify they pass**

Same command as Step 3. Expected: all 16 `MedusaTimelineTests` pass. Delete `medusa-tests.xml` if it was created; it's not committed.

- [ ] **Step 6: Commit** (each file with its Unity-generated `.meta`, plus the `Logic/` and `Tests/` folder `.meta`s)

```bash
git add iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/Logic iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/Logic.meta iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/Tests iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/Tests.meta
git commit -m "feat(medusa): timeline rules with EditMode tests"
```
(Include the parent folder `.meta`s `Assets/Game.meta`, `Assets/Game/Encounters.meta` and `Assets/Game/Encounters/Medusa.meta` if this is the first commit that creates those folders and they aren't on `main` yet.)

---

### Task 3: `KeyboardFaceSignals` mock

**Files:**
- Create: `iOS_MapStoryEngine/Assets/Game/CameraMode/KeyboardFaceSignals.cs`

**Interfaces:**
- Consumes: `IFaceSignals` from the contracts (`FaceFound`, `EyesClosed`, `LookingAtScreen`, `MirrorSeen` if the stub has it, `StartStreaming()`, `StopStreaming()`).
- Produces: `public class KeyboardFaceSignals : MonoBehaviour, IFaceSignals`, with public bools `face = true`, `eyesClosed = false`, `looking = false`, `mirror = false`.
  - Keys `F`, `E`, `L` and `M` toggle them in `Update()` (legacy `Input.GetKeyDown`).
  - `LookingAtScreen` returns `face && !eyesClosed && looking`.
  - `StartStreaming()` / `StopStreaming()` only log `[KeyboardFaceSignals] streaming on|off`.

- [ ] **Step 1: Implement it**, matching exactly the members the contract stub declares.
- [ ] **Step 2: Compile check.** Refresh Unity; read the Console via the Unity MCP (or ask the human). Expected: no `error CS`.
- [ ] **Step 3: Commit** `KeyboardFaceSignals.cs` + its `.meta` (+ `Assets/Game/CameraMode.meta` if new): `git commit -m "feat(medusa): keyboard face-signal mock (F/E/L/M)"`

---

### Task 4: `MedusaScreen` (UI built by code) and the image

**Files:**
- Create: `iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/MedusaScreen.cs`
- Create: `iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/medusa.png`. Copy it from `C:/Users/Yolo/AppData/Local/Temp/claude/c--Users-Yolo-Desktop-hackuta2026-mapstoryengine/a06a2171-75a0-4d53-ba40-3391e1e49423/images/3.png` (474×1024 portrait). If that file is gone, ask the human for the Medusa artwork.

**Interfaces:**
- Consumes: `MedusaPhase` (Task 2).
- Produces:
  ```csharp
  public sealed class MedusaScreen
  {
      public static MedusaScreen Create(Texture medusaImage);  // builds the canvas at runtime
      public void Show(MedusaPhase phase);
      public void SetClue(string text);
      public void UpdateEffects(float time);                   // hijack flicker; call every frame
      public void Destroy();
  }
  ```

- [ ] **Step 1: Implement `MedusaScreen.Create`.**
  - A root `GameObject "MedusaScreen"` with a `Canvas`:
    - `RenderMode.ScreenSpaceOverlay`, `sortingOrder = 100`.
    - A `CanvasScaler`: `ScaleWithScreenSize`, reference `1179×2556`, match `0.5`.
  - Children, all full-stretch `RectTransform`s unless noted:
    - a black `Image` background;
    - a `RawImage` with the texture, plus an `AspectRatioFitter` set to `EnvelopeParent` and ratio `width/height`, so it fills with no stretching;
    - `Text` elements using `Resources.GetBuiltinResource<Font>("Arial.ttf")`, white, centred:
      - **coming:** `MEDUSA IS COMING`, size 90, bold;
      - **clue:** anchored to the bottom 20 % of the screen, size 54, on a semi-transparent black panel, so it never covers her face;
      - **stoned:** `STONED` size 140 + newline + `You looked into Medusa's eyes.` size 60, on a grey overlay (alpha 0.6).
- [ ] **Step 2: Implement `Show(phase)`.**

  | Phase | What's visible |
  |---|---|
  | Hijack | image (flickering) |
  | Coming | black background + coming text only |
  | Danger | image |
  | Clue | image + clue panel |
  | Stoned | image + stoned overlay |
  | Won / Aborted | nothing (the caller destroys the screen) |

  `UpdateEffects(time)` flickers the image alpha during Hijack only, e.g. `0.4 + 0.6 * |sin(time * 40)|`, and sets it to 1 otherwise.
- [ ] **Step 3: Compile check** (Console clean), then **commit** `MedusaScreen.cs`, `medusa.png` and their `.meta`s: `git commit -m "feat(medusa): full-screen takeover UI built by code"`

---

### Task 5: `MedusaEncounter`, sandbox scene, manual end-to-end check

**Files:**
- Create: `iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/MedusaEncounter.cs`
- Create: `iOS_MapStoryEngine/Assets/Game/Encounters/Medusa/MedusaSandbox.cs`
- Create: `iOS_MapStoryEngine/Assets/Game/Sandbox/P2.unity` (the human creates it in the Editor, or via the Unity MCP if it can create and save scenes)

**Interfaces:**
- Consumes: `MedusaTimeline`/`MedusaPhase` (Task 2), `MedusaScreen` (Task 4), `KeyboardFaceSignals` (Task 3); contracts `IEncounter`, `EncounterType.Medusa`, `ClueTier.Good`, `GameEvents.{RaiseEncounterStarted, RaiseEncounterEnded, RequestClue, ClueUnlocked, RequestKill, KillRequested}`, `Services.{Face, CameraMode}`.
- Produces: `public class MedusaEncounter : MonoBehaviour, IEncounter`, with `public Texture medusaImage` (assigned in the Inspector), `Type => EncounterType.Medusa`, `Begin()`, `Abort()`, and `public bool IsRunning { get; }`.

- [ ] **Step 1: Implement `MedusaEncounter`.**
  - **`Begin()`:**
    - If already running, log a warning and return.
    - Otherwise: `new MedusaTimeline(UnityEngine.Random.Range(5f, 15f))`, `MedusaScreen.Create(medusaImage)`, `Show(Hijack)`, `GameEvents.RaiseEncounterStarted(EncounterType.Medusa)`, `Services.CameraMode?.EnterFrontCameraMode()`.
  - **`Update()`:**
    - `looking = Services.Face != null && Services.Face.LookingAtScreen` (a null face source logs one warning per encounter).
    - Call `Tick(Time.unscaledDeltaTime, looking)` and `UpdateEffects(Time.unscaledTime)`.
    - On a phase change, `Show(phase)`, and:
      - entering **Danger** → `Services.Face?.StartStreaming()`;
      - entering **Clue** → subscribe to `GameEvents.ClueUnlocked`, call `GameEvents.RequestClue(ClueTier.Good)`, and if no text arrives within 0.2 s (unscaled), `SetClue("Ithaca hides from those who dare to look.")`;
      - **Won** → `Finish(true)`;
      - **Stoned** and `IsOver` → `Finish(false)` **then** `GameEvents.RequestKill("stoned")`.
  - **`Abort()`:** if running, `timeline.Abort()` then `Finish(false)`.
  - **`Finish(bool won)`** (private):
    - Set `IsRunning = false` **first**, so a re-entrant `Abort()` is ignored.
    - Unsubscribe `ClueUnlocked`, `Services.Face?.StopStreaming()`, `Services.CameraMode?.ExitFrontCameraMode()`, `screen.Destroy()`.
    - `GameEvents.RaiseEncounterEnded(EncounterType.Medusa, won)`.
  - `OnDisable()` calls `Abort()`.
- [ ] **Step 2: Implement `MedusaSandbox`** (`[RequireComponent(typeof(MedusaEncounter))]`).
  - **Awake:** if `Services.Face == null`, add `KeyboardFaceSignals` to this GameObject and assign it.
  - **Subscriptions:**
    - `ClueRequested` → if `answerClues`, `GameEvents.RaiseClueUnlocked("TEST CLUE: Ithaca lies past the fountain.")`;
    - `KillRequested` → log `[Sandbox] killed: <cause>`, and if `abortOnKill`, call `encounter.Abort()` (re-entrancy check);
    - `EncounterStarted` / `EncounterEnded` → log.
  - **Keys:** `B` = `Begin()`, `A` = `Abort()`, `C` = toggle `answerClues` (default true), `K` = toggle `abortOnKill` (default true).
  - **`OnGUI`:** a small label at the top-left showing face / eyes / looking / answerClues / abortOnKill and the encounter's running state. IMGUI draws above the overlay canvas.
- [ ] **Step 3: Compile check.** Console clean.
- [ ] **Step 4: Create the sandbox scene.**
  - *File → New Scene*, save it as `Assets/Game/Sandbox/P2.unity`.
  - Add an empty GameObject `Medusa` with `MedusaEncounter` + `MedusaSandbox`, and drag `medusa.png` into **Medusa Image**. Save.
- [ ] **Step 5: Manual end-to-end check in Play mode** (human, or the Unity MCP for the Console). All must hold:
  1. `B` → flicker for 1 s → black `MEDUSA IS COMING` for 3 s → her image. The Console logs `EncounterStarted` and `streaming on`.
  2. Press `E` (eyes closed) and wait: 5–15 s into Danger, `TEST CLUE: …` appears at the bottom for 2 s. Then the screen closes, and the Console logs `EncounterEnded Medusa won=True` and `streaming off`.
  3. `B` again, then after the countdown press `L` with eyes open → within ~0.5 s the STONED screen appears for 2 s. The Console logs `EncounterEnded … won=False` exactly once, then `killed: stoned`, with no exceptions (with `K` on).
  4. `C` (clue answering off), `B`, `E`: the clue panel shows `Ithaca hides from those who dare to look.`
  5. `B` twice quickly → a warning, and only one screen. `A` mid-encounter → the screen closes, and `EncounterEnded … won=False` is logged once.
- [ ] **Step 6: Commit** `MedusaEncounter.cs`, `MedusaSandbox.cs`, `P2.unity` and their `.meta`s (+ `Assets/Game/Sandbox.meta` if new): `git commit -m "feat(medusa): encounter wiring and P2 sandbox scene"`

---

### Task 6: PR

- [ ] **Step 1:** `git status`. Nothing from `Packages/` or `Assets/Mapbox/` is staged, and every new `.cs`/`.png`/`.unity`/`.asmdef` is committed with its `.meta`.
- [ ] **Step 2:** Push `p2/medusa-mvp` and open a PR to `main`. The description includes:
  - a **How to test** section (the Task 5 Step 5 checklist, condensed);
  - a note that it depends on Farhan's contract stubs, including `RequestKill`;
  - a note that ClueBook should not pop up the clue while Medusa is running.
