# Agent Instructions (shared by Claude Code and Codex, so keep one copy)

Read `docs/PLAN.md` (roles, timeline, mechanic specs), `docs/CONTRACTS.md` (APIs and interfaces) and `changes.md` (iOS build notes and credentials) before doing anything. `UNITY.md` covers the Android backup project only.

## Project
Odyssey AR: an open-world, closed-map AR game built on mapstoryengine. Start at Troy and find the hidden Ithaca using clues. Hackathon, about 18 hours, 4 people.
- **We build for iPhone. Only work in `iOS_MapStoryEngine/`.** Ignore `Android_MapStoryEngine/`; it's a backup and nobody edits it.
- Game code goes in `iOS_MapStoryEngine/Assets/Game/`. Every `Assets/...` path in these docs means `iOS_MapStoryEngine/Assets/...`.
- The vision/LLM server is in `server/` (Python 3.10–3.12, FastAPI, MediaPipe, Gemini, Groq Whisper).

## Hard technical constraints (agents get these wrong)
- **Unity 2019.4.41f2 (LTS) only, which means C# 7.3.** No records, `init`, switch expressions, nullable reference types, `using` declarations, or default interface methods. Use `UnityWebRequest`, not `HttpClient`.
- **AR Foundation 3.1.3 / ARKit XR Plugin 3.0.4.**
  - Use `ARSessionOrigin` (NOT `XROrigin`) and `ARSession`.
  - Do NOT use `ARCameraManager.requestedFacingDirection`, `ARCameraManager.TryAcquireLatestCpuImage`, or any other AR Foundation 4+/5+ API.
  - If you're not sure an API exists in 3.1, say so; don't guess.
- **iOS builds need Farhan's Mac.** Unity exports an Xcode project, and the fixes listed in `changes.md` must be reapplied after each export until they're in the build pipeline. Nobody else can build to the phone.
- **iOS permissions:** camera, microphone and location need usage descriptions in Player Settings (Info.plist). Ask for the microphone at runtime with `Application.RequestUserAuthorization(UserAuthorization.Microphone)`, not the Android `Permission` API.
- **Phone → laptop server:**
  - iOS 14+ shows a **Local Network** prompt; the player must allow it.
  - iOS may block plain `http://` (App Transport Security). If requests to the laptop fail, Farhan adds an ATS exception to the exported Info.plist.
- **Camera use per encounter (do not deviate):**
  - Default screen, Sphinx, Cyclops: **rear-camera AR**. Hydra has no AR character; her fight is a separate minigame screen.
  - Medusa:
    - Rear AR is paused, and she hijacks the screen with an on-screen animation, then a 3-2-1 countdown.
    - The **front camera runs hidden** (its feed is never displayed). It only detects looking and eyes closed.
    - Looking with eyes open for ~0.5 s = STONED death.
    - Detection is either the iPhone's Face ID tracking (being checked) or the server's `/frame`; this is decided at CP1.
  - The front camera is used ONLY by Medusa. No pushups or pose tracking anywhere.
- Cyclops: tap = attack. The shield gesture is TBD (see PLAN); the current proposal is the AR camera raised ≥15 cm above its position at fight start.
- **The Sphinx is back-camera AR** (reuse mapstoryengine's AR object placement). The conversation is **voice only**: Unity `Microphone` → WAV → `POST /sphinx/talk`.
- Clues: the Sphinx and Medusa (mirror) give Good clues, and the Cyclops gives a Specific one. Medusa's and the Cyclops' clues show **as text on screen**. **The Hydra never gives a clue**; fighting her only buys a 30 s poison break.
- Mapbox SDK is in `Assets/Mapbox/`. Never edit it.
- Use Newtonsoft Json (`jillejr.newtonsoft.json-for-unity`) or `JsonUtility` for JSON. SimpleJSON is also in the project.

## Ownership: only edit your own human's folders
| Owner | Paths (under `iOS_MapStoryEngine/` unless noted) |
|---|---|
| P1 Farhan | `Assets/StoryEngine/Scenes/MapStory.unity`, `ProjectSettings/`, `Packages/`, `Assets/Game/Core/` (except Contracts and Clues.json), `Assets/Game/UI/`, `Assets/Game/GameSystems.prefab` |
| P2 | `Assets/Game/CameraMode/`, `Assets/Game/Encounters/Medusa/`, `Assets/Game/Encounters/Sphinx/`, `Assets/Game/Net/`, `server/` (repo root) |
| P3 | `Assets/Game/Encounters/Cyclops/`, `Assets/Game/Encounters/Hydra/` |
| P4 | `Assets/Game/Art/`, `Assets/Game/Core/Clues.json` |
| Shared (team-agreed changes only) | `Assets/Game/Core/Contracts/`, `docs/CONTRACTS.md`, `docs/PLAN.md`, `AGENTS.md` |

- If a change is needed outside your owner's paths, **stop and tell your human** which file needs changing and why. Don't make the edit.
- **Never change contract files on your own.** Propose the change instead.
- Only Farhan saves `MapStory.unity`. Everyone else builds **prefabs** and tests in `Assets/Game/Sandbox/<Name>.unity`.

## Unity + git rules
- **Every new asset (including `.cs`) must be committed together with its `.meta` file, and the `.meta` must come from Unity.** After creating files, the human must focus the Unity Editor so it imports them, then commit the `.cs` and `.meta` together. Never hand-write or regenerate `.meta` files or GUIDs.
- Don't commit `Library/`, `Temp/`, `Logs/`, Xcode exports, app bundles, or API keys.
  - Unity keys come from `ArdysseyCredentials` (env vars or the gitignored `iOS_MapStoryEngine/.ardyssey.local.json`; see `changes.md`). Never put keys in the Inspector, scenes or `MapboxConfiguration.txt`.
  - The server's Gemini and Groq keys go in `server/.env` (gitignored).
- Each feature must be testable **without the main scene, the phone and the server**. Use the mocks (`KeyboardFaceSignals`, server `--fake`, debug-panel event buttons).
- **MCP for Unity is only installed in the Android project**, not the iOS one. Don't assume the MCP endpoint works for `iOS_MapStoryEngine`.
- **Compile check without opening the editor:** with the Editor closed, run
  `"<UnityPath>/Editor/Unity.exe" -batchmode -nographics -quit -projectPath iOS_MapStoryEngine -logFile -` and look for `error CS`.
- Server: `python -m pytest server` must pass before you open a PR.

## Workflow
- Branch: `p<N>/<short-task>`. Keep PRs small. Rebase on `main` first. The PR description must include a **"How to test"** section.
- GitHub Issues are turned off on this repo, so tasks are tracked in the team chat.
- Build exactly what the spec in `docs/PLAN.md` says. Don't add extra features, frameworks, or refactors of the original mapstoryengine code. Items marked **TBD** are decided by their owner, not by an agent.

## Lessons learned (add one line each, in a PR)
- (empty)
