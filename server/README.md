# Odyssey venue server

The Oracle (`POST /oracle`) and the Sphinx (`POST /sphinx` and `POST /sphinx/talk`) use one server. For first-time setup, copy [`.env.example`](.env.example) to `server/.env` and set `GEMINI_API_KEY` for both features. Git ignores the local credentials file.

## Unity Editor

1. From the repository root, install `server/requirements.txt` in a Python virtual environment and run `python -m server`. It listens on `127.0.0.1:8787` by default.
2. Launch Unity with `ARDYSSEY_ORACLE_URL=http://127.0.0.1:8787` in its environment to test both features via loopback.
3. In `MapStory.unity`, ask the Oracle how to defeat the Cyclops. Start the voyage and approach the Sphinx site to answer her riddles. For an isolated Sphinx test, use `Assets/Game/Sandbox/P2_Sphinx.unity`. `python -m server --fake` uses canned Sphinx replies.

## iPhone

For a venue demo, keep the server and `cloudflared tunnel --url http://127.0.0.1:8787` running on the Mac. The phone reaches the public HTTPS tunnel over Wi-Fi or cellular data. Restarting a Cloudflare Quick Tunnel generates a new URL. For a stable hosted deployment, serve the ASGI app `server.venue:app` behind HTTPS and keep the API keys in the hosting secret store. Apply shared edge request and concurrency limits if running multiple workers or instances.

Set the public HTTPS base URL in the active `GameConfig.serverUrl` or in `iOS_MapStoryEngine/Assets/Game/UI/Resources/Odyssey/OracleEndpoint.txt` as a single line. Both clients read that configuration. Git ignores the local endpoint asset and its `.meta`. Refresh Unity and export again after changing the bundled URL.

The Sphinx accepts typed answers and displays subtitles with only the Gemini key. Set `ELEVENLABS_API_KEY` and `ELEVENLABS_VOICE_ID` to enable spoken replies. Microphone answers use ElevenLabs, Groq, or OpenAI, depending on the credentials in `server/.env`. Restart the Python server after updating its environment.

The iOS Medusa encounter uses on-device tracking. The prototype `/frame` endpoint is disabled by default. For local prototype testing, install MediaPipe in the Python environment and set `ODYSSEY_ENABLE_FRAME=1` before starting the server.

The Oracle recognizes gameplay questions locally and sends Gemini the corresponding rules, a fixed intent, and whether the question continues the previous topic. Gemini selects validated components for each reply. The Oracle keeps chat text out of its logs and has no access to the Unity scene, coordinates, or clue journal.

To use PF Hellenica Regular, import the app-licensed font asset as `Assets/Game/UI/Resources/Odyssey/PF Hellenica Regular.otf`. The project currently bundles Caesar Dressing Regular as its fallback.

Run both server test suites with `python -m pytest -q server`. The tests run without provider credentials.
