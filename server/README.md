# Oracle proxy

The iOS client sends chat turns to `POST /oracle`. Python 3.10–3.12 is sufficient for local development.

## Unity Editor

1. Set `GEMINI_API_KEY` in the server process environment and run `python -m server.main` from the repository root. The default Gemini model is `gemini-3.5-flash-lite`. Set `GEMINI_MODEL` on the server to change it.
2. Launch Unity with `ARDYSSEY_ORACLE_URL=http://127.0.0.1:8787` in its environment. The client appends `/oracle`.
3. Enter Play Mode and ask “How do I defeat the Cyclops?” followed by “When should I raise it?” Ask “Where is Ithaca?” to verify the in-character refusal.

## iPhone

Serve `server.main:app` with a production WSGI server behind an HTTPS reverse proxy or HTTPS hosting service. Keep the server key in the hosting secret store. Apply shared edge request and concurrency limits if running multiple workers or instances. The built-in development server listens only on loopback.

Set the public HTTPS base URL in the active `GameConfig.serverUrl` or create `iOS_MapStoryEngine/Assets/Game/UI/Resources/Odyssey/OracleEndpoint.txt` containing the URL alone. The device build accepts HTTPS and sends chat only to this proxy. The URL is public client configuration. The Gemini key belongs exclusively on the server.

The backend recognizes gameplay questions locally and sends Gemini only the corresponding rule set, a fixed intent, and an indication of whether the question continues the previous topic. Gemini selects the details and phrasing components for each reply. The proxy renders only validated sentence IDs. It has no connection to the Unity scene, coordinates, or clue journal, and it never logs chat text.

To use PF Hellenica Regular, import the app-licensed font asset as `Assets/Game/UI/Resources/Odyssey/PF Hellenica Regular.otf`. The project currently bundles Caesar Dressing Regular as its fallback.

Run the proxy regression tests with `python -m unittest discover -s server -p "test_*.py"`. They use a simulated provider and require no API key.
