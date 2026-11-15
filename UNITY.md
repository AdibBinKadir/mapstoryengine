# ARdyssey project notes

## Android project

- Open `Android_MapStoryEngine` in Unity **2019.4.41f2**. The project includes **MCP for Unity 2019 v0.3.1**. Its local Streamable HTTP endpoint defaults to `http://127.0.0.1:6500/mcp`.
- The Android build includes `Assets/StoryEngine/Scenes/MapStory.unity` and has ARCore enabled in XR Plug-in Management. The demo currently uses five stops within a 250 m search radius. Keep the child counts under `SiteAudio_17` and `ObjectsToPlace_16` aligned with the stop count.

## Changes made

- Migrated Android's `SearchLocationsScript.cs` from Google Nearby Search (Legacy) to Nearby Search (New). The script requests display names, coordinates, and place types, then converts the response into the format `StoryManager` expects. The Unity Console distinguishes request failures from too few results.
- Updated `Title.cs` to wait for the search to complete before enabling `StoryManager`.
- Added `ArdysseyCredentials.cs` to read `GOOGLE_PLACES_API_KEY` and `MAPBOX_ACCESS_TOKEN`. Environment variables take precedence over the ignored `Android_MapStoryEngine/.ardyssey.local.json`. Use `Android_MapStoryEngine/.ardyssey.example.json` as the template for a fresh clone.
- Removed the saved Places key and Mapbox token values from the tracked Android scene and Mapbox configuration asset. The Mapbox setup window saves its settings with an empty token field. Use a key restricted to **Places API (New)** and a public Mapbox token.
- Added `ArdysseyAndroidCredentialsBuild.cs` to write the client credentials into the generated Android Gradle assets. Keep the local profile, generated credential file, and credential-bearing APKs out of Git and public artifacts. For public distribution, keep the Google key server-side and route Places requests through a backend.

## Verification

- Unity compiled the updated search and credential code. Sample responses verified conversion of valid, incomplete, and empty results. The Android player credential reader also compiled with the Android build define.
- The earlier quick-start APK was built before the Places migration and credential changes. The shared debug keystore failed during signing, so that APK was signed with a project-local test key. A fresh APK and live Google Places and ARCore behavior still need on-device verification.
