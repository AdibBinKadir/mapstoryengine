# Odyssey AR

*Ten years after Troy, you still haven't made it home.*

You are Odysseus. Somewhere in the streets around you lies **Ithaca**, and the gods have hidden it well. Open your phone and the old monsters are waiting, standing in the real world through your camera.

**The Cyclops** blocks the road, his one eye healed and furious. Tap to strike, raise your shield to survive.

**Medusa** doesn't wait to be found. She seizes your screen, counts down from three, and watches you through the front camera. Look at her with your eyes open for half a second and you are stone. Close your eyes. Look away. Listen to her hiss.

**The Sphinx** lies across your path and will only talk, out loud. Hold *Speak* and answer her riddles while she mocks your deaths, your wounds and your sense of direction. Solve two and she leans in to share the secret she's been guarding all along:

> *"Fine. A deal is a deal. Lean closer, sailor... Ithaca is... Not here."*

Keep walking. Ithaca is out there, and you'll know it when you see the flag.

---

## How it works
- **iPhone app** in Unity 2019.4 with AR Foundation / ARKit and Mapbox: GPS places each encounter in the real world around you.
- **Medusa** uses ARKit face tracking on the TrueDepth camera, with the feed never shown, to read your blinks and gaze.
- **The Sphinx** sends your voice to a small Python server. ElevenLabs hears you, Gemini judges your answer and writes her reply in character, and ElevenLabs speaks it back, in about 2 seconds. She can't be talked into a wrong answer.

## Run it
- **Game:** open `iOS_MapStoryEngine/` in Unity 2019.4.41f2, scene `Assets/StoryEngine/Scenes/MapStory.unity`. Each encounter can also be played alone from `Assets/Game/Sandbox/`. Setup is in `TEAM_SETUP.md`; iOS build notes are in `changes.md`.
- **Sphinx server:** `pip install -r server/requirements.txt`, add your Gemini and ElevenLabs keys to `server/.env`, then run `python -m server`. Use `--fake` to run with no keys. More in `server/README.md`.

## Credits
Built in 18 hours at **HackUTA 2026**, on top of graeburn's [Map Story engine](docs/MAPSTORY_ENGINE.md) (MIT).
- **3D models (Sketchfab):** Cyclops Rig by DM-913 (CC BY 4.0), Medieval Flag by Sousinho (CC BY 4.0), and the Sphinx of Hatshepsut scan.
- **Font:** Caesar Dressing by Open Window (OFL).
- **Medusa's video and the Sphinx's voice** are AI-generated, the voice with ElevenLabs; her words are from Google Gemini.
