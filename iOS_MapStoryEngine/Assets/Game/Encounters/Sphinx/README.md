# The Sphinx (P2)

A rear-camera AR boss who asks two riddles. The first correct answer earns a second riddle. The second correct answer wins the encounter and adds the Sphinx's clue to the voyage journal.

## How it works
- **Phone.** The Sphinx is visible in rear-camera AR while exploring. During the encounter, type an answer or hold **Speak** to record up to eight seconds when voice input is configured. The screen shows subtitles and plays back any generated speech. `SphinxProgress` counts correct answers.
- **Server.** The Sphinx and Oracle share the same Python process and Gemini key. ElevenLabs can transcribe answers and generate the Sphinx's voice. See [venue server setup](../../../../../server/README.md).
- **Connection issues.** The encounter shows a built-in line and allows another try.

## Run it in the Editor
1. Start the server from the repository root with `python -m server`. Use `--fake` for canned Sphinx replies.
2. Open `Assets/Game/Sandbox/P2_Sphinx.unity` and press Play. The server field uses the project endpoint if configured, with `http://127.0.0.1:8787` as its Editor fallback. Click **Save** after editing the sandbox URL.
3. Press **Begin**, then hold **Hold to speak** and talk, or type in the box and press **Send**.

## Run it on the phone

1. Configure the shared HTTPS URL and leave `Assets/StoryEngine/Scenes/MapStory.unity` first in the iOS build scene list. The microphone permission text and iOS recording settings are already configured.
2. Start the voyage to find the Sphinx in rear-camera AR within one meter of the start. Approach her site until **Face Sphinx** appears. Select **Test Sphinx** on the home screen to practice the encounter directly.
3. Tap **Face Sphinx** and answer the riddles. If voice input is configured, allow microphone access when prompted.

## The model
Put the FBX, its textures and an `Attribution.txt` in `Sphinx/Model/`. Then make a prefab at `Sphinx/Resources/SphinxModel.prefab`, about 1.5 m tall, facing +Z. The prefab's scale is kept, and she pulses gently while she talks.
