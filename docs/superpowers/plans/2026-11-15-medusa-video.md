# Medusa Video Loop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Medusa plays a looping, sound-on video of herself during Danger and Clue, freezes on Stoned, and never shows black.

**Architecture:** A pure phase rule (`MedusaVideoRules`) decides when the video plays. `MedusaScreen` owns a `VideoPlayer` that renders into a RenderTexture shown by the existing portrait `RawImage`. `MedusaEncounter` passes the clip, loading it from `Resources` when no clip is assigned.

**Tech Stack:** Unity 2019.4 (C# 7.3), `UnityEngine.Video`, NUnit EditMode tests, ffmpeg (asset prep only).

**Spec:** `docs/superpowers/specs/2026-11-15-medusa-video-design.md`

## Global Constraints
- Unity 2019.4.41f2, C# 7.3 only.
- Only edit `Assets/Game/Encounters/Medusa/` (P2). No edits to Cyclops, scenes owned by others, or contracts.
- Every new asset is committed with its Unity-generated `.meta`; never hand-write `.meta`.
- `git add` explicit paths only.
- Source video: `C:\Users\Yolo\Downloads\Generated Video November 15, 2026 - 5_51AM.mp4` (H.264 360×640, 24 fps, 10 s, AAC audio).

## Review Focus
- Abort or Won during Hijack/Coming while the clip is still preparing: no sound afterwards, no leaked RenderTexture.
- Clip missing or failing to load: still image stays, one log line, no exceptions.
- Begin → Abort → Begin quickly: the second run plays normally.
- Stoned: frame freezes and sound stops (pause, not stop, so it's not black).
- The portrait never goes black between "still" and "first video frame".

---

### Task 1: When the video plays

**Files:**
- Create: `Assets/Game/Encounters/Medusa/Logic/MedusaVideoRules.cs`
- Test: `Assets/Game/Encounters/Medusa/Tests/MedusaVideoRulesTests.cs`

**Interfaces:**
- Produces: `public static class MedusaVideoRules { public static bool Plays(MedusaPhase phase); }`

- [ ] **Step 1: Write the failing test** — `[TestCase]` per phase: Danger → true, Clue → true; Hijack, Coming, Won, Stoned, Aborted → false.
- [ ] **Step 2: Run EditMode tests (Unity MCP `run_tests`, filter `MedusaVideoRulesTests`)** — Expected: compile error / FAIL (type missing).
- [ ] **Step 3: Implement `Plays`** in `MedusaVideoRules.cs` (no UnityEngine).
- [ ] **Step 4: Run tests** — Expected: all `MedusaVideoRulesTests` and `MedusaTimelineTests` pass.
- [ ] **Step 5: Commit** `.cs` + Unity `.meta` files: `feat(medusa): phase rule for the video loop`.

### Task 2: Assets

**Files:**
- Create: `Assets/Game/Encounters/Medusa/Resources/MedusaLoop.mp4` (copy of the source video, unchanged)
- Modify: `Assets/Game/Encounters/Medusa/medusa.png` (overwritten in place with the video's first frame, 360×640 PNG; keep the existing `.meta`)

- [ ] **Step 1:** Copy the video; extract frame 0 with `ffmpeg -i <src> -frames:v 1 medusa.png`.
- [ ] **Step 2:** Refresh Unity; Expected: `Resources.meta` and `MedusaLoop.mp4.meta` exist, `medusa.png.meta` GUID unchanged (`42c3d78716ee5bc4aa321fea3d7df8a1`), console has no import errors.
- [ ] **Step 3: Commit** the mp4, both new `.meta`, and `medusa.png`: `feat(medusa): themed video loop and matching still`.

### Task 3: Play it on the Medusa screen

**Files:**
- Modify: `Assets/Game/Encounters/Medusa/MedusaScreen.cs`
- Modify: `Assets/Game/Encounters/Medusa/MedusaEncounter.cs`

**Interfaces:**
- Consumes: `MedusaVideoRules.Plays(MedusaPhase)`.
- Produces: `MedusaScreen.Create(Texture medusaImage, VideoClip video)`; `MedusaEncounter.medusaVideo` (`public VideoClip`), resource name `"MedusaLoop"`.

- [ ] **Step 1: `MedusaScreen`**:
  - With a non-null clip, add a `VideoPlayer` on the root: `playOnAwake=false`, `isLooping=true`, `renderMode=RenderTexture` into a new `RenderTexture(clip.width, clip.height, 0)`, `audioOutputMode=Direct`; call `Prepare()` in `Create`.
  - `Show(phase)`: `Plays(phase)` → `Play()`, else `Pause()` if playing.
  - In `UpdateEffects`, once the player is playing with `frame > 0`, switch `portrait.texture` to the RenderTexture (one-way flag) and set the fitter's aspect to the clip's.
  - `errorReceived` → log once with `[Medusa]` prefix; the still stays.
  - `Destroy()`: `Stop()`, release and destroy the RenderTexture, then destroy the root.
- [ ] **Step 2: `MedusaEncounter`**: add `public VideoClip medusaVideo;`; in `Begin`, use it or `Resources.Load<VideoClip>("MedusaLoop")`, pass to `MedusaScreen.Create`.
- [ ] **Step 3: Compile + run all EditMode tests** — Expected: no `error CS`; Medusa and CameraMode tests all pass.
- [ ] **Step 4: Editor check (P2 sandbox, Play mode)** — Begin: flicker, 3-2-1, then the moving video with sound; mock "looking" → frame freezes, sound stops, STONED; Abort during Coming → silence, no errors. Expected: console has no exceptions.
- [ ] **Step 5: Commit** both files: `feat(medusa): play the video loop with sound during danger`.
