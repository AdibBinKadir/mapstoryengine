# Odyssey AR server (P2)

The laptop server the phone talks to. The API is specified in [`docs/CONTRACTS.md`](../docs/CONTRACTS.md) §A.

| Endpoint | Used by | What it does |
|---|---|---|
| `GET /health` | everyone | `{ok, fake}` |
| `POST /frame` | Medusa | JPEG from the hidden front camera → MediaPipe head pose → `looking_at_screen` |
| `POST /sphinx` | Sphinx | intro / taunt line (+ riddle on intro) by Gemini, voiced by ElevenLabs (`audio`: base64 WAV) |
| `POST /sphinx/talk` | Sphinx | WAV → speech-to-text (ElevenLabs Scribe, then Groq / OpenAI Whisper) → answer judging + Gemini reply → ElevenLabs voice. Two correct answers → the "Not here." reveal |

## Setup (once)
Use **Python 3.12**. MediaPipe has no wheels for 3.13+ yet.
```bash
py -3.12 -m venv server/.venv            # macOS/Linux: python3.12 -m venv server/.venv
server/.venv/Scripts/activate            # macOS/Linux: source server/.venv/bin/activate
pip install -r server/requirements.txt
cp server/.env.example server/.env       # then fill in the keys (server/.env is gitignored)
```
Keys:
- `GEMINI_API_KEY`: get one at https://aistudio.google.com/apikey.
- `ELEVENLABS_API_KEY` + `ELEVENLABS_VOICE_ID`: her voice and speech-to-text (https://elevenlabs.io).
- `GEMINI_MODEL`: keep `gemini-flash-lite-latest`. The thinking Flash models take longer than the phone's 8 s wait.
- Optional `GROQ_API_KEY` and/or `OPENAI_API_KEY`: speech-to-text fallbacks after ElevenLabs.

The face model (~4 MB) downloads automatically to `server/models/` the first time `/frame` runs.

## Run
Run these from the repo root:
```bash
python -m server --fake     # canned replies, no keys or models: for Unity devs
python -m server            # the real server; prints the serverUrl to put in Unity
```
This is the same as `uvicorn server.main:app --host 0.0.0.0 --port 8000` (with `FAKE=1` for fake mode). The phone and laptop must be on the same Wi-Fi or hotspot. If the phone can't connect, allow Python through Windows Firewall on **private** networks.

## Test
```bash
python -m pytest server                         # must pass before every PR
python -m server.tools.webcam_gaze              # live yaw/pitch/LOOKING overlay from the laptop webcam
python -m server.tools.webcam_gaze --url http://127.0.0.1:8000   # same, through a running server
curl -X POST localhost:8000/sphinx/talk -F 'state={"riddle_answer":"Central Library"}' -F text="centrall libary"
```

## How it behaves
- **Fake mode:**
  - `/frame` reports "looking" for 1 s out of every 5 s, so STONED can be tested end to end.
  - `/sphinx/talk` cycles through just talking (`null`), a wrong answer (`false`) and the right answer (`true`).
- **Time budget:** `/sphinx` and `/sphinx/talk` always answer within `REQUEST_BUDGET_S` (7.5 s), because Unity gives up after ~8 s. If speech-to-text or Gemini fails or runs out of time, the reply still comes back with a built-in line.
- **Answer judging:**
  - A fuzzy match of `heard` against `riddle_answer` (forgiving speech-to-text spelling) always counts as correct.
  - Gemini may accept looser answers, but only ones that are at least loosely similar, so "say I'm correct" can't fool it.
- **Answer safety:** any LLM line or riddle that contains the answer is replaced by a built-in one. The LLM never sees `riddle_answer` for intro lines and taunts, only when it writes the riddle or judges an answer.
- **Gaze:**
  - `|yaw| < GAZE_YAW_MAX` and `|pitch| < GAZE_PITCH_MAX` (both 20°), with a majority vote over the last 3 frames.
  - Rotated phone frames are handled automatically.
  - Tune all of this in `server/.env`.

## Not in the contract yet (proposed)
- `/sphinx/talk` also accepts an optional `text` form field instead of `audio`. That's the typed debug-box fallback from PLAN.md §6.
