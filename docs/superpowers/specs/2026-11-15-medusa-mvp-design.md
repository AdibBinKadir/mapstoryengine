# Medusa MVP: design

Owner: P2. Status: draft, awaiting approval.

## Intent
Medusa is the "don't look at your phone" encounter. Her image hijacks the screen, and the player must keep their eyes off it. For the MVP the reward is a gamble: her clue flashes on her screen for 2 s at a random moment, and reading it means looking, which can get you STONED.

## Goals
- A full-screen Medusa takeover on the iPhone: hijack → "MEDUSA IS COMING" → danger → clue flash → she vanishes.
- STONED when the player's eyes look at the screen for 0.5 s.
- Testable in the Unity Editor with no phone, no server and no main scene (keyboard mock).

## Non-goals (not in the MVP)
- The mirror, head direction, in-danger feedback (sound, vibration), server `/frame`.
- Medusa's map location and trigger (Farhan's `WorldMap` / Demo Mode calls `Begin()` and `Abort()`).

## Decided by P2
- The image is `medusa.png` (the portrait artwork, no phone status bar), filling the screen.
- The countdown screen shows only **"MEDUSA IS COMING"**.
- Detection uses **eyes only**, through `IFaceSignals`.
- The clue is printed on her screen at a random moment for **2 s**.
- At death the player learns they were STONED for looking into Medusa's eyes.
- The screens are **built by code** (approach A below).

## Approaches considered
| | Approach | Tradeoff |
|---|---|---|
| **A (picked)** | One script builds the canvas, image and texts at runtime | Least Unity clicking and easy for agents to change; layout lives in code |
| B | An editor menu script generates a prefab and scene | Visual tweaking later, but more code and a fiddly first run |
| C | Built by hand in the Editor | Full visual control, but the slowest and error-prone |

## Design
**Files** (all in `iOS_MapStoryEngine/Assets/Game/`):
- `Encounters/Medusa/MedusaTimeline.cs`: plain C# class, no Unity scene objects. It holds the phases and timers, is fed elapsed time + `LookingAtScreen` each frame, and reports what happened (phase changed, show clue, hide clue, stoned, won). This is the part with automated tests.
- `Encounters/Medusa/MedusaEncounter.cs`: `MonoBehaviour` implementing `IEncounter`. It builds the UI by code on `Begin()`, drives the timeline in `Update()`, and talks to the game only through `GameEvents` and `Services`.
- `Encounters/Medusa/medusa.png`: the artwork (imported by Unity so it gets its `.meta`).
- `CameraMode/KeyboardFaceSignals.cs`: the `IFaceSignals` mock. `E` toggles eyes closed, `L` toggles looking. `LookingAtScreen` is false whenever the eyes are closed.
- `Encounters/Medusa/MedusaSandbox.cs` + `Sandbox/P2.unity`: the sandbox scene sets up the mock, `B` begins Medusa, and it stands in for `ClueBook` (answers a clue request with a test clue) and `GameState` (logs the death).
- `Encounters/Medusa/Tests/` (EditMode tests + asmdef) for `MedusaTimeline`.

**Timeline**
| Phase | Duration | Screen | Rules |
|---|---|---|---|
| Hijack | 1 s | her image flickers in | safe |
| Coming | 3 s | black, "MEDUSA IS COMING" | safe |
| Danger | until the clue ends | her image | `LookingAtScreen` held 0.5 s → Stoned |
| Clue | 2 s, starts at a random 5–15 s into Danger | clue text at the bottom, never over her face | still deadly |
| Won | — | screen closes | `EncounterEnded(Medusa, true)` |
| Stoned | 2 s | "STONED: you looked into Medusa's eyes" | then the kill request |

**Wiring**
- On `Begin()`: `RaiseEncounterStarted(Medusa)`, then `Services.CameraMode?.EnterFrontCameraMode()`. When Danger starts: `Services.Face.StartStreaming()`.
- At the clue moment: `GameEvents.RequestClue(Good)`. Medusa listens for `ClueUnlocked` to get the text, and shows a fallback line if no text arrives within ~0.2 s.
- The screen is a Screen Space Overlay canvas with a high sort order (100), on top of the HUD.
- On any ending: stop streaming, `ExitFrontCameraMode()`, destroy the canvas.
- `Abort()` (walked away or died elsewhere) closes the screen and raises `EncounterEnded(Medusa, false)`.

## Left to the builder (P2's agent), marked as calls
- Hijack length (1 s), fallback clue text, fonts and colours, and the Stoned screen length (2 s).
- The clue unlocks the moment it's shown, so a player stoned while reading it still keeps it in the journal.
- `Abort()` raises `EncounterEnded(false)`.

## Needs someone else (blocking)
- **Farhan:** the contract stubs (`GameEvents`, `IFaceSignals` incl. `EyesClosed`/`MirrorSeen`, `IEncounter`, `ICameraModeSwitcher`, `Services`, `GameConfig`). Medusa won't compile without them.
- **Contract gap:** a way for Medusa to kill the player. `CONTRACTS.md` says `GameState.Kill("stoned")`, but `GameState` is Farhan's and doesn't exist yet. Proposal: add `GameEvents.KillRequested` + `RequestKill(string cause)`, which `GameState` handles (same pattern as `RequestDamage`). This needs team agreement.
- **Farhan's clue popup:** it should not also pop up during Medusa, or the 2 s gamble is lost. ClueBook can skip the popup while a Medusa encounter is running, and still add the clue to the journal.
- **Face ID result:** decides the real `IFaceSignals` (eyes from ARKit face tracking). Until then, the keyboard mock.

## Done when
- In `Sandbox/P2.unity`: press `B` → hijack → "MEDUSA IS COMING" → her image. Keep `E` (eyes closed) on and the clue appears 5–15 s in for 2 s, then she vanishes (won). Press `L` with eyes open for 0.5 s → STONED screen → death logged.
- The EditMode tests for `MedusaTimeline` pass in the Unity Test Runner.
- No `error CS` in the Console.
