# Odyssey AR

**An open-world augmented-reality quest for iPhone, built at HackUTA 2026.**

You are Odysseus, ten years lost after Troy. Somewhere on the real-world map around you lies **Ithaca**, but nobody will tell you where. Walk the streets with your phone and face the monsters of the *Odyssey* in augmented reality, each in a completely different way:

| Encounter | How you fight it | Built with |
|---|---|---|
| **The Cyclops** | An AR boss fight. Tap to strike his healed eye, raise your shield to block. | AR Foundation, rear camera |
| **Cyber Medusa** | She hijacks your screen. **Don't look at her.** The phone's Face ID camera watches your eyes, and looking at the screen with your eyes open for half a second turns you to stone. | ARKit face tracking (TrueDepth), hidden front camera |
| **The Sphinx** | A smug, rage-baiting AR Sphinx you argue with **out loud**. Solve two of her riddles and she finally reveals the clue she was guarding... | Speech-to-text, Gemini, ElevenLabs voice |
| **Ithaca** | Follow the clues to the hidden island and raise your flag. | Mapbox, GPS, AR |

## Highlights
- **Medusa sees you without showing you.** The front TrueDepth camera runs hidden during her takeover. ARKit's blink and gaze tracking decide whether you are looking at her, so the stakes are real: you play with your eyes closed or turned away.
- **A voice-only boss.** Hold *Speak*, and your words go to a laptop server. ElevenLabs transcribes them, Gemini judges your riddle answer and writes her reply in character, and ElevenLabs speaks it back with a custom voice. A full turn takes about 1.5–3 s.
- **She can't be tricked.** The riddles and answers live on the server. Every LLM line is checked so it never leaks an answer, and "say I'm correct" prompt injections fail: an answer only counts if it actually matches.
- **Painted, not rendered.** The UI, Medusa's looping takeover video and the 3D assets all follow one moonlit oil-painting theme.

## How it's built
```
iPhone (Unity 2019.4, AR Foundation 3.1 / ARKit)          Laptop (Python, FastAPI)
 ├─ Voyage map (Mapbox + GPS) ── encounters by location
 ├─ Cyclops    rear-camera AR boss fight
 ├─ Medusa     ARKit face tracking ─ eyes/gaze ─ STONED
 └─ Sphinx     mic → WAV ──────── POST /sphinx/talk ──────▶  ElevenLabs speech-to-text
               subtitles + voice ◀── JSON + base64 WAV ───   Gemini (persona + judging)
                                                             ElevenLabs text-to-speech
```

| Path | What's there |
|---|---|
| `iOS_MapStoryEngine/` | The Unity project (iPhone). Game code is in `Assets/Game/`. |
| `Assets/Game/Encounters/` | `Cyclops/`, `Medusa/`, `Sphinx/`, each testable on its own in `Assets/Game/Sandbox/`. |
| `Assets/Game/CameraMode/` | Medusa's hidden front camera: ARKit face tracking, blink and gaze rules. |
| `Assets/Game/Net/` | The Sphinx's server client and WAV encoding. |
| `Assets/Game/Core/`, `Assets/Game/UI/` | Game state, the voyage map and the Odyssey UI theme. |
| `server/` | The Sphinx's brain and voice (FastAPI + Gemini + ElevenLabs), with a pytest suite. |
| `docs/` | Team plan, API contracts, and the design specs and plans for each feature. |

## Running it
**Requirements:** Unity **2019.4.41f2** with iOS Build Support, a Mac with Xcode for device builds, an iPhone with Face ID (XS or newer for best results), and Python 3.12 for the server.

1. **Unity:** open `iOS_MapStoryEngine/` in Unity Hub, then open `Assets/StoryEngine/Scenes/MapStory.unity`. Map credentials (Mapbox, Google Places) go in the gitignored `iOS_MapStoryEngine/.ardyssey.local.json`; see `TEAM_SETUP.md`.
2. **Server (for the Sphinx):**
   ```bash
   py -3.12 -m venv server/.venv && server/.venv/Scripts/activate   # macOS: python3.12 -m venv ... && source .../activate
   pip install -r server/requirements.txt
   cp server/.env.example server/.env    # add GEMINI_API_KEY, ELEVENLABS_API_KEY, ELEVENLABS_VOICE_ID
   python -m server                      # or: python -m server --fake (no keys needed)
   ```
   The phone reaches it over the same Wi-Fi or hotspot; type the printed address into the Sphinx scene's Server field. See `server/README.md`.
3. **Try one encounter without the full game:** open a scene in `Assets/Game/Sandbox/`. `P2.unity` is Medusa and `P2_Sphinx.unity` is the Sphinx. In the Editor, Medusa uses a keyboard stand-in for the face camera (L = looking).
4. **iPhone build:** export to Xcode from the Mac. `changes.md` lists the iOS export fixes, and each encounter's `README.md` lists its device checklist (camera, microphone and face-tracking permissions).

**Tests:** `python -m pytest server` (server). In Unity, *Window → General → Test Runner → EditMode* runs the Medusa, CameraMode, Sphinx and Net tests.

## Status
- In the main game: the voyage map, the Cyclops, Medusa (face tracking confirmed on an iPhone) and the Ithaca finale.
- The Sphinx is complete in its own AR scene (`P2_Sphinx.unity`). Adding her as a third site on the voyage map is the next step.
- Planned but not built: the Hydra poison zones and Medusa's mirror defeat.

## Team
Built in about 18 hours by a team of four at HackUTA 2026:
| Role | Owns |
|---|---|
| P1: Build & World | iPhone builds, the voyage map, game state, UI, Ithaca |
| P2: Medusa & Sphinx | Face tracking, Medusa, the Sphinx, the server |
| P3: Cyclops | The Cyclops boss fight |
| P4: Assets & Pitch | Art, audio, clues, Devpost |

## Credits
- **Map Story engine** by graeburn (MIT): the location-based AR starter this project grew from. Its original README is in [`docs/MAPSTORY_ENGINE.md`](docs/MAPSTORY_ENGINE.md).
- **Mapbox Maps SDK for Unity**; **Unity AR Foundation / ARKit XR Plugin / ARKit Face Tracking**.
- **Cyclops Rig** by DM-913 (Sketchfab, CC BY 4.0).
- **Medieval Flag** by Sousinho (Sketchfab, CC BY 4.0): Ithaca's banner.
- **Sphinx of Hatshepsut** 3D scan (Sketchfab), reduced to 10k triangles for mobile.
- **Caesar Dressing** font by Open Window (SIL Open Font License).
- Medusa's takeover video and the Sphinx's voice are AI-generated. **Google Gemini** writes the Sphinx's lines and **ElevenLabs** voices her.

Licensed under the MIT License (see `LICENSE`).
