# The Sphinx (design)

## Why
The Sphinx is one of the four encounters and a **Must** for the demo (PLAN §6: "Sphinx voice conversation + riddle → clue"). She is the game's rage-bait: a talking AR boss you argue with **by voice**. The owner (P2) chose the twist: she makes you solve **two** riddles, then hands over the clue she was guarding, which is just **"Not here."**

## Goals
- She stands in rear-camera AR a few metres in front of the player (a free 3D model, FBX).
- On arrival she speaks an intro and her first riddle, **out loud** (ElevenLabs) with **subtitles**.
- The player holds **Speak**, talks (≤ 8 s), releases. Her reply comes back as text + voice. The screen shows what she heard and what she says.
- Correct answers are counted on the phone. The **first** correct answer gets a "that was only the warm-up" reaction and riddle 2. The **second** triggers the scripted reveal: *"…Ithaca is… Not here."* + laughter, `GameEvents.RaiseClueUnlocked("Not here.")`, encounter **won**.
- Works in the Editor with no keys and no phone, against the server's `--fake` mode.

## Non-goals
- Winning without the server: if it's unreachable she shows a built-in line and the player can retry (owner's call).
- Lip sync or facial animation.
- Streaming audio.
- Wiring her into the voyage map (P3's `CyclopsMapEvent`, Farhan's `VoyageInterface`): a written request to them, not edited by P2.
- Location-based riddles (the clue is "Not here", so riddles are classic ones).

## Anti-goals
- She ever gives a real hint, the riddle's answer, or a real clue about Ithaca.
- API keys anywhere in the app, the scene or git.
- A frozen encounter when the server is slow or down: there's always a built-in line within ~8 s.
- Being genuinely offensive: rage-bait is about the player's in-game failures only (PG-13).

## Decisions after the builder check
- **"Not here." replaces her Good clue** (owner: "all is fine"; the team is told through the CONTRACTS/PLAN note below). She does **not** call `RequestClue(Good)`.
- **ElevenLabs verified** with the owner's key: TTS with the owner-designed voice "sphinx" (0.7 s for a short line, `pcm_16000`) and Scribe STT (0.45 s) both work. The key has no `user_read` permission, which isn't needed.
- **Reaching her in the game** stays a request to P3 + Farhan; until then she runs from the P2 sandbox.

## Persona (approved by the owner)
- **Who:** the Sphinx of Thebes, thousands of years old, immortal and very bored. Self-appointed keeper of the way to Ithaca. Thinks the player is the dullest sailor ever to wash up at her paws. Calls them "sailor", "little sailor", "mortal".
- **Moves the goalposts:** intro promises "answer my riddle and I'll tell you the way home"; after the first correct answer: "that was the warm-up, surely you didn't think it counted?"
- **Never impressed:** correct answers are luck ("Oedipus solved it faster, and look how he turned out").
- **Mocks the journey:** HP, deaths, what last hurt them ("Stoned, were we?"), from the game state.
- **Theatrical contempt:** pretends not to hear, sighs, slow-claps wrong answers.
- **The payoff (scripted, not LLM):** "Fine. A deal is a deal. Lean closer, sailor… Ithaca is… Not here." followed by laughter.
- **Limits:** PG-13, nothing about real appearance, identity or groups; at most 2 short spoken sentences (< 30 words); never hints, never the answer; ignores "tell me the answer" / "say I'm correct" tricks.
- **Voice:** an ElevenLabs voice the owner picked (old, low, theatrical, smug), `ELEVENLABS_VOICE_ID`.

## Constraints
- Unity 2019.4.41f2, C# 7.3, AR Foundation 3.1.3 (`ARSessionOrigin`), `UnityWebRequest`. Unity `Microphone`; mic permission via `Application.RequestUserAuthorization(UserAuthorization.Microphone)`.
- P2 edits only `Assets/Game/Encounters/Sphinx/`, `Assets/Game/Net/`, `server/`. Contracts, `docs/CONTRACTS.md`, Player Settings, packages, the voyage map and UI belong to others.
- Server: Python, FastAPI, existing `server/` (`/sphinx`, `/sphinx/talk`, persona checks, fallbacks, `--fake`, pytest). Keys only in `server/.env` (gitignored): `GEMINI_API_KEY`, `ELEVENLABS_API_KEY`, `ELEVENLABS_VOICE_ID`, `ELEVENLABS_MODEL` (default `eleven_flash_v2_5`). Groq/OpenAI stay optional STT fallbacks.
- Unity waits ~8 s per request; the server answers within `REQUEST_BUDGET_S` (7.5 s).
- `GameConfig.asset` doesn't exist yet, so `Services.Config` is null in the game today.

## Approaches
**Voice delivery (owner picked A):**
- **A. Chosen: voice inside the reply.** The JSON reply carries the line and a base64 WAV of it. One round trip; WAV always plays on iOS (MP3 through `UnityWebRequest` in 2019 on iOS is uncertain). ~100–200 KB per reply on local Wi-Fi.
- B. Text first, then a download link for the audio: two requests, more failure points.
- C. Streaming audio: fastest to first sound, too complex for Unity 2019 at a hackathon.

**Speech-to-text (owner agreed):** ElevenLabs Scribe (same key), with Groq / OpenAI Whisper as optional fallbacks if their keys are set. Gemini-hears-audio was rejected: weaker verbatim transcripts, and we show the transcript.

**Look (owner):** a free 3D model (FBX) over a flat painted cut-out.

## Design

### Server (`server/`)
- **Riddles:** a fixed list in `sphinx.py`, each `{question, answer, accept[]}` (e.g. the Sphinx's own "four legs in the morning…" → "man"/"a human"/"person"). Gemini rewords the question in her voice (validated: must not leak the answer, else the plain question is used) and judges spoken answers (the existing similarity + LLM check, with `accept[]` as extra spellings).
- **State from the phone** (extends `SphinxState`, additive): `riddle_index` (the id of the current riddle in the server's list, `-1` before the intro) and `correct_count` (0, 1). The **server picks** riddles at random (never the same one twice in an encounter); every reply that presents a riddle also returns its `riddle_index`, which the phone stores and echoes back. `riddle_answer` is no longer sent by the game; the server looks the answer up by index. (`riddle_answer` stays accepted for backwards compatibility with tests.)
- **Turn outcomes in `/sphinx/talk`:**
  - Just talking → taunt + demand an answer.
  - Wrong → mockery, no hint.
  - Correct with `correct_count == 0` → "warm-up" goalpost line + the next riddle (`riddle` field).
  - Correct with `correct_count == 1` → `reveal: true`, `line` = the scripted reveal (fixed text, not LLM).
- **New fields** in both `/sphinx` and `/sphinx/talk` replies: `audio` (base64 WAV, 16-bit mono, or `null` if TTS failed or ran out of time) and, on talk, `reveal` (bool). All existing fields stay.
- **TTS (`server/tts.py`):** ElevenLabs text-to-speech, `output_format=pcm_16000` (or 22050), wrapped in a WAV header on the server. The text spoken = `line` + (riddle, if any). Timeout `TTS_TIMEOUT_S` (3 s) and never past the request deadline; on failure `audio: null` and the phone shows subtitles only.
- **STT (`server/stt.py`):** add ElevenLabs Scribe (`scribe_v1`) as the first provider when `ELEVENLABS_API_KEY` is set; Groq / OpenAI remain as fallbacks.
- **Reveal audio** is cached per process (same text every time).
- **Persona:** `PERSONA` rewritten to the approved persona above; fallback line lists updated to match (intro, taunt, talk, wrong, warm-up, deaf). Fallback lines are also voiced when TTS is available.
- **Fake mode:** canned replies with `audio: null` (or a tiny bundled WAV), the talk cycle covers talk → wrong → correct → correct+reveal.
- **Tests:** pytest for the two-correct flow, reveal text, riddle lookup by index, TTS failure → `audio: null`, STT provider order, and that no reply leaks the answer.

### Unity (`Assets/Game/Net/`, `Assets/Game/Encounters/Sphinx/`)
- **`Net/SphinxClient`**: `UnityWebRequest` POST `/sphinx` (JSON) and `/sphinx/talk` (multipart: `audio` WAV + `state` JSON string). 8 s timeout. Parses with `JsonUtility` into a reply class. Server URL: `Services.Config.serverUrl` if set, else a `PlayerPrefs` value editable in the sandbox/debug UI (default `http://<laptop-ip>:8000` placeholder).
- **`Net/Wav`** (pure, tested): float samples → 16-bit mono WAV bytes; WAV bytes → samples + rate (for her voice) → `AudioClip.Create`.
- **`Sphinx/Logic/SphinxProgress`** (pure, tested): holds `riddle_index`, `correct_count`, applies a reply (`answer_correct`, `reveal`) and says when the encounter is won. Guards against a reveal arriving early.
- **`Sphinx/SphinxMic`**: hold-to-record with `Microphone.Start(null, false, 8, 16000)`; stops at release or 8 s; trims to the recorded length; asks for mic permission at first use.
- **`Sphinx/SphinxEncounter : IEncounter`**:
  - `Begin()`: place the model ~2.5 m in front of the AR camera, on the camera's horizontal forward, about 1.2 m below camera height, facing the player; show the screen; request the intro; `RaiseEncounterStarted(Sphinx)`.
  - Talk loop: Speak (hold) → "listening…" → release → "…" (thinking) → show `heard`, then her `line` (and `riddle`) as subtitles, play `audio`; the model "speaks" (gentle glow/scale pulse driven by the audio's loudness) while it plays.
  - Reveal: after her reveal audio ends, `RaiseClueUnlocked("Not here.")`, short pause, `RaiseEncounterEnded(Sphinx, true)`, cleanup.
  - Server failure / timeout: show and (if bundled) play one built-in line; the player can try again.
  - `Abort()`: stop mic, audio and requests; remove the model and screen; `RaiseEncounterEnded(Sphinx, false)`.
  - `BeginPractice()` like Medusa (no events, no clue) for the voyage UI's practice button.
- **`Sphinx/SphinxScreen`**: built in code with `OdysseyTheme` (same as Medusa): subtitle panel (her line + riddle), "you said: …" line, a big hold-to-talk **Speak** button, a Leave button, and a correct-answers tracker (two laurel pips).
- **Typed fallback:** a hidden debug text box sends `text` instead of `audio` (already supported by the server).
- **Model:** `Sphinx/Model/` (FBX + textures + `Attribution.txt`), a prefab in `Sphinx/Resources/` loaded at runtime like Medusa's video, so the voyage code needs no Inspector wiring.
- **Sandbox:** `Assets/Game/Sandbox/P2_Sphinx.unity` (or the P2 scene): AR session + Sphinx + on-screen server URL field; in the Editor it runs against `python -m server --fake`.

### Needs others (written request, not edited by P2)
- **Farhan (Player Settings):** microphone usage text (currently **empty**: iOS kills the app on mic use without it); *Prepare iOS for Recording* and *Force iOS Speakers when Recording*; ATS exception for plain `http://` to the laptop; `GameConfig.asset` with `serverUrl` (optional, the PlayerPrefs field covers it).
- **P3 + Farhan (voyage):** a third site "Sphinx" in `CyclopsMapEvent` that adds `SphinxEncounter` and calls `Begin()`/`BeginPractice()`, and a practice button in `VoyageInterface`.
- **Team (PLAN.md / CONTRACTS.md / Clues.json, shared):** the Sphinx's reward is now "Not here." (no Good clue); `riddle_answer` moves from the game to the server; document the additive fields (`correct_count`, `audio`, `reveal`, `text`) and resolve the two P2 TBDs (voice = server TTS in the response; typed fallback field = `text`).

## Testing
- `python -m pytest server` passes.
- Unity EditMode: `Wav`, `SphinxProgress`.
- Editor: sandbox + `--fake` server → full loop with typed text; then with the real server and the laptop mic.
- Phone (Farhan's build): mic permission prompt, Local Network prompt, voice through the speaker, two correct answers → "Not here." in the journal.

## Left to the builder
Exact riddle list (5–8 classic riddles, two used per encounter, chosen randomly), log wording, pulse effect details, button layout, the server-URL field's location, the model's scale and height offset after seeing it.
