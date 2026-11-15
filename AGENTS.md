# Agent Instructions (shared by Claude Code and Codex, so keep one copy)

Read `docs/PLAN.md` (roles, timeline, mechanic specs) and `docs/CONTRACTS.md` (APIs and interfaces) before doing anything.

## Project
Odyssey AR: an open-world, closed-map AR game built on mapstoryengine. Start at Troy and find the hidden Ithaca using clues. Hackathon, about 18 hours, 4 people.
- **Only work in `Android_MapStoryEngine/`.** Ignore `iOS_MapStoryEngine/`.
- Game code goes in `Android_MapStoryEngine/Assets/Game/`. The vision/LLM server is in `server/` (Python 3.10+, FastAPI, MediaPipe).

## Hard technical constraints (agents get these wrong)
- **Unity 2019.4.41f2 (LTS) only, which means C# 7.3.** No records, `init`, switch expressions, nullable reference types, `using` declarations, or default interface methods. Use `UnityWebRequest`, not `HttpClient`.
- **AR Foundation 3.1.3 / ARCore XR Plugin 3.1.4.** Use `ARSessionOrigin` (NOT `XROrigin`) and `ARSession`. Do NOT use `ARCameraManager.requestedFacingDirection`, `ARCameraManager.TryAcquireLatestCpuImage`, or any other AR Foundation 4+/5+ API. If you're not sure an API exists in 3.1, say so; don't guess.
- **Camera use per encounter (do not deviate):**
  - Default screen, Sphinx, Cyclops, Hydra: **rear-camera AR**.
  - Medusa: rear AR paused; she hijacks the screen with an on-screen animation; the **front camera runs hidden** (feed never displayed) and only detects looking. Looking for ~0.5 s = STONED death.
  - The front camera is used ONLY by Medusa. No pushups or pose tracking anywhere.
- Cyclops shield = AR camera raised ≥15 cm above its position at fight start; tap = attack.
- **The Sphinx is back-camera AR** (reuse mapstoryengine's AR object placement). The player talks to her by voice: Unity `Microphone` → WAV → `POST /sphinx/talk`. Request `UnityEngine.Android.Permission.Microphone` at runtime.
- ARCore can't use both cameras at once. The front camera is only available after pausing AR (`ARSession.enabled = false`), through `WebCamTexture`. See `ICameraModeSwitcher`.
- Mapbox SDK is in `Assets/Mapbox/`. Never edit it.
- Use Newtonsoft Json (`jillejr.newtonsoft.json-for-unity`) or `JsonUtility` for JSON. SimpleJSON is also in the project.

## Ownership: only edit your own human's folders
| Owner | Paths |
|---|---|
| P1 | `Assets/StoryEngine/Scenes/MapStory.unity`, `Assets/Game/CameraMode/`, `Assets/Game/Encounters/Medusa/`, `Assets/Game/Net/CvClient*`, `ProjectSettings/`, `Packages/` |
| P2 | `Assets/Game/Core/` (except Contracts and Clues.json), `Assets/Game/Encounters/Hydra/`, `Assets/Game/UI/`, `Assets/Game/GameSystems.prefab` |
| P3 | `server/` (all of it), `Assets/Game/Core/Clues.json` |
| P4 | `Assets/Game/Encounters/Cyclops/`, `Assets/Game/Encounters/Sphinx/`, `Assets/Game/Net/SphinxClient*` |
| Shared (team-agreed changes only) | `Assets/Game/Core/Contracts/`, `docs/CONTRACTS.md`, `AGENTS.md` |

- If a change is needed outside your owner's paths, **stop and tell your human** which file needs changing and why. Don't make the edit.
- **Never change contract files on your own.** Propose the change instead.
- Only P1 saves `MapStory.unity`. Everyone else builds **prefabs** and tests in `Assets/Game/Sandbox/<Name>.unity`.

## Unity + git rules
- **Every new asset (including `.cs`) must be committed together with its `.meta` file, and the `.meta` must come from Unity.** After creating files, the human must focus the Unity Editor so it imports them, then commit the `.cs` and `.meta` together. Never hand-write or regenerate `.meta` files or GUIDs.
- Don't commit `Library/`, `Temp/`, `Logs/`, builds, or API keys. The Google Places key goes in `Assets/Resources/Secrets/GooglePlacesKey.txt` (gitignored). The Mapbox token goes in through *Mapbox → Setup*. The LLM key goes in `server/.env` (gitignored).
- Each feature must be testable **without the main scene and without the server**. Use the mocks (`KeyboardFaceSignals`, server `--fake`, debug-panel event buttons).
- **Compile check without opening the editor:** with the Editor closed, run
  `"<UnityPath>/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Android_MapStoryEngine -logFile -` and look for `error CS`.
- Server: `python -m pytest server` must pass before you open a PR.

## Workflow
- Branch: `p<N>/<short-task>`. Keep PRs small. Rebase on `main` first. The PR description must include a **"How to test"** section.
- One GitHub Issue per task. Use `gh issue view <n>` for the spec and put `Closes #<n>` in the PR.
- Build exactly what the spec in `docs/PLAN.md` §4 says. Don't add extra features, frameworks, or refactors of the original mapstoryengine code.

## Lessons learned (add one line each, in a PR)
- (empty)
