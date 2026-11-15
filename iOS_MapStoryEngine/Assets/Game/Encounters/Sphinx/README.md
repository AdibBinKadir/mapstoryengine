# The Sphinx (P2)

A rear-camera AR boss you argue with **by voice**. She asks a riddle; the first correct answer is "only the warm-up" and earns a second riddle; the second correct answer gets her scripted reveal, *"…Ithaca is… Not here."*, which goes into the clue journal as `Not here.` and wins the encounter.

Spec: `docs/superpowers/specs/2026-11-15-sphinx-design.md` · Plan: `docs/superpowers/plans/2026-11-15-sphinx.md`

## How it works
- **Phone (Unity):** `SphinxEncounter` places her ~2.5 m ahead, records while **Speak** is held (≤ 8 s, `SphinxMic`), and posts the WAV to the laptop server (`Net/SphinxClient`). It shows the subtitles and "You said…", and plays her voice. `SphinxProgress` counts correct answers.
- **Laptop (`server/`):** ElevenLabs hears the player, Gemini (`gemini-flash-lite-latest`) judges and writes her reply, and ElevenLabs speaks it. The voice comes back inside the JSON as a base64 WAV. Keys are only in `server/.env`.
- **If the server fails:** she shows a built-in line, and the player can try again.

## Run it in the Editor
1. Start the server from the repo root: `python -m server`. Use `--fake` for canned replies with no keys.
2. Open `Assets/Game/Sandbox/P2_Sphinx.unity` and press Play. The server field defaults to `http://127.0.0.1:8000`.
3. Press **Begin**, then hold **Hold to speak** and talk, or type in the box and press **Send**.

## Run it on the phone (Farhan)
Before the build, in Player Settings → iOS:
1. **Microphone Usage Description**: it is currently **empty**, and iOS kills the app the first time the mic is used without it. Example: *"The Sphinx listens to your answers."*
2. **Prepare iOS for Recording** and **Force iOS Speakers when Recording**: on. Without them her voice plays through the earpiece after the mic is used.
3. **Plain `http://` to the laptop**: add an ATS exception in the exported Info.plist (see `AGENTS.md`).
4. Build `Assets/Game/Sandbox/P2_Sphinx.unity` as the only scene.

On the phone:
- Allow the **microphone** and **Local Network** prompts.
- Type the laptop's address in the sandbox's Server field (for example `http://192.168.x.x:8000`) and press **Save**.
- The laptop runs `python -m server`, which listens on `0.0.0.0:8000`.

## Putting her in the game (P3 + Farhan)
She is not on the voyage map yet. `CyclopsMapEvent` (P3) and `VoyageInterface` (Farhan) need a third site, "Sphinx". It should work like Medusa:
- `gameObject.AddComponent<SphinxEncounter>()`;
- `Begin()` when the player reaches her site, and `BeginPractice()` for a practice button;
- listen for `GameEvents.EncounterEnded(EncounterType.Sphinx, won)`.

No Inspector wiring is needed. The model is loaded from `Resources/SphinxModel` if it exists, otherwise a gold placeholder is shown.

## For the team (shared docs, not edited by P2)
- **Reward:** the Sphinx's reward is now the text **"Not here."**; she no longer calls `RequestClue(Good)`. `PLAN.md`, `CONTRACTS.md` and possibly `Clues.json` need a line.
- **`riddle_answer`:** now chosen by the server, not sent by the game.
- **New API fields:** `correct_count`, `riddle_index` echo, `verdict`, `reveal`, `audio`, and the typed fallback field `text`.

## The model
Put the FBX, its textures and an `Attribution.txt` in `Sphinx/Model/`. Then make a prefab at `Sphinx/Resources/SphinxModel.prefab`, about 1.5 m tall, facing +Z. The prefab's scale is kept, and she pulses gently while she talks.
