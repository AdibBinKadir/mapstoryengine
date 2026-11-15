# Contracts

These are the only points where one person's code talks to another's. **Don't change them without telling the team** (see PLAN.md, Rule 1). P1 (Farhan) creates the C# stubs *inside Unity* (in `iOS_MapStoryEngine`) so `.meta` files get generated, then commits them.

## A. HTTP API (phone ↔ laptop server, owner: P2)
Base URL is configurable in the app (Demo Mode panel and `GameConfig.serverUrl`), e.g. `http://192.168.43.10:8000`.
Run with `uvicorn server.main:app --host 0.0.0.0 --port 8000`. Set `FAKE=1` to return canned data with no models or API keys.

### `GET /health`
→ `{ "ok": true, "fake": false }`

### `POST /frame`  (Medusa only, and only if the Face ID path is a no-go; decided at CP1)
Body: raw JPEG bytes (`Content-Type: image/jpeg`), about 320×240, ~5 per second, from the hidden front camera.
```json
{ "face_found": true, "eyes_closed": false, "looking_at_screen": false, "yaw": 34.2, "pitch": -5.1, "ts": 1731600000.12 }
```
- `eyes_closed`: both eyes closed (tunable), smoothed over ~3 frames.
- `looking_at_screen`: a face is found AND eyes are open AND |yaw| < 20° AND |pitch| < 20° (tunable), smoothed over ~3 frames. Eye direction may be added if it proves reliable.
- The ~0.5 s "must keep looking" rule before STONED lives in **Unity** (Medusa), not the server.
- *TBD (P2): mirror detection. If the server detects it, it adds a field here.*

### `POST /sphinx`  (intro and unprompted taunts, no audio)
```json
{
  "event": "intro | taunt",
  "hp": 40, "max_hp": 100, "deaths": 2,
  "last_event": "hydra_poisoned | stoned | cyclops_won | ...",
  "riddle_index": 1,
  "riddle_answer": "Central Library"
}
```
→ `{ "line": "Back again, little sailor? The Hydra's youngest head pities you.", "riddle": "I hold a thousand voices yet never speak..." }`

### `POST /sphinx/talk`  (the player spoke)
`multipart/form-data` with:
- `audio`: WAV, 16 kHz, mono, 16-bit, ≤ ~8 s
- `state`: the same JSON as `/sphinx`, sent as a string

→
```json
{
  "heard": "is it the central library",
  "line": "Hmph. Even a blind sailor stumbles onto the truth eventually.",
  "riddle": null,
  "answer_correct": true
}
```
- `heard` is the speech-to-text transcript. Always show it on screen.
- `answer_correct`: `true` or `false` if the player answered the current riddle, `null` if they were just talking.
- `riddle_answer` comes from the **game**. The LLM only words the riddle, must never reveal the answer, and must never invent clues.
- The LLM call times out after 8 s. Unity plays or shows one built-in line if the request fails.
- The LLM is **Gemini**. Speech-to-text is **Groq-hosted Whisper**, with OpenAI Whisper as the fallback.
- *TBD (P2): how the Sphinx's voice reaches the phone (server text-to-speech in the response, or text-to-speech on the phone).*
- *TBD (P2): the form field for the typed debug-box fallback (PLAN §6), sent instead of `audio`.*

## B. C# (Unity 2019.4 means C# 7.3: no records, no `switch` expressions, no nullable reference types)
All game code goes under `Assets/Game/`. The contract files are in `Assets/Game/Core/Contracts/`.

```csharp
public enum EncounterType { Sphinx, Medusa, Cyclops, Hydra }
public enum ClueTier { Vague, Good, Specific }

// GameEvents.cs (owner: P1). Every encounter talks to the rest of the game only through these.
// Clue rewards: Sphinx -> RequestClue(Good); Medusa (mirror) -> RequestClue(Good); Cyclops -> RequestClue(Specific);
// Hydra -> none. ClueBook shows every ClueUnlocked as on-screen text and adds it to the journal.
using System;
public static class GameEvents
{
    public static event Action<EncounterType> EncounterStarted;
    public static event Action<EncounterType, bool> EncounterEnded;  // bool = playerWon
    public static event Action<int> DamageRequested;                 // encounters ask; GameState applies
    public static event Action FullHealRequested;                    // e.g. Cyclops' sheep
    public static event Action<string> PlayerDied;                   // cause: "stoned" | "poison" | "cyclops"
    public static event Action PlayerRespawned;
    public static event Action<ClueTier> ClueRequested;              // encounters ask; ClueBook unlocks the next clue
    public static event Action<string> ClueUnlocked;                 // clue text, for UI and toasts
    public static event Action GameWon;

    public static void RequestDamage(int amount) { if (DamageRequested != null) DamageRequested(amount); }
    public static void RequestFullHeal() { if (FullHealRequested != null) FullHealRequested(); }
    public static void RequestClue(ClueTier tier) { if (ClueRequested != null) ClueRequested(tier); }
    public static void RaiseEncounterStarted(EncounterType t) { if (EncounterStarted != null) EncounterStarted(t); }
    public static void RaiseEncounterEnded(EncounterType t, bool won) { if (EncounterEnded != null) EncounterEnded(t, won); }
    public static void RaisePlayerDied(string cause) { if (PlayerDied != null) PlayerDied(cause); }
    public static void RaisePlayerRespawned() { if (PlayerRespawned != null) PlayerRespawned(); }
    public static void RaiseClueUnlocked(string text) { if (ClueUnlocked != null) ClueUnlocked(text); }
    public static void RaiseGameWon() { if (GameWon != null) GameWon(); }
}
// Rule: only GameState changes HP and only GameState raises PlayerDied, except Medusa, which kills
// instantly with a "stoned" cause through GameState.Kill("stoned").

// IFaceSignals.cs (owner: P2; real = Face ID tracking or CvClient, mock = KeyboardFaceSignals with
// L = toggle looking, F = toggle face, E = toggle eyes closed, M = toggle MirrorSeen)
public interface IFaceSignals
{
    bool FaceFound { get; }
    bool EyesClosed { get; }
    bool LookingAtScreen { get; }   // false whenever EyesClosed
    bool MirrorSeen { get; }   // Medusa: her face was recognised in a mirror held up to the phone
    void StartStreaming();
    void StopStreaming();
}

// ICameraModeSwitcher.cs (owner: P2). Used ONLY by Medusa. With the Face ID path, FrontCameraTexture may be null.
using UnityEngine;
public interface ICameraModeSwitcher
{
    void EnterFrontCameraMode();   // pause rear AR, start the front camera HIDDEN (feed never displayed)
    void ExitFrontCameraMode();    // stop the front camera, resume rear AR
    WebCamTexture FrontCameraTexture { get; }
}

// IEncounter.cs: each encounter prefab's root script implements this.
public interface IEncounter
{
    EncounterType Type { get; }
    void Begin();   // raise GameEvents.EncounterEnded when done
    void Abort();   // the player died or walked away
}

// Services.cs (owner: P1): the simplest possible service locator, filled in at startup.
public static class Services
{
    public static IFaceSignals Face;
    public static ICameraModeSwitcher CameraMode;
    public static GameConfig Config;
}
```

## C. Config and content (owner: P1; values filled in by the team)
`GameConfig` is a ScriptableObject at `Assets/Game/Core/GameConfig.asset`:
- `serverUrl`
- `troy`, `ithaca`, `sphinx`, `medusa`, `cyclops`: lat/lon each (Medusa's lair; leaving its `encounterRadiusM` ends her encounter)
- `hydraZones[]`: lat/lon list
- `respawnPoints[]`: lat/lon list (Troy is always included)
- `maxHp = 100`, `winRadiusM = 20`, `respawnRadiusM = 20`, `hydraRadiusM = 30`, `encounterRadiusM = 25`
- `vagueClueIntervalSec = 300` (Demo Mode: 30), `demoMode = true`
- Prefab references for each encounter

`Assets/Game/Core/Clues.json` (content written by P4 and the team):
```json
{ "vague": ["..."], "good": ["..."], "specific": ["..."],
  "riddles": [ { "answer": "Central Library", "hint": "..." } ] }
```
