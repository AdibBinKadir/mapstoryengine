# Return to Ithaca iOS build and verification

## What is in the final voyage

- **Play** opens the rear-camera AR voyage. Cyclops, Medusa, and the Sphinx are placed within about one meter of the start in different directions. Cyclops and the Sphinx appear in AR and can be approached in any order. Medusa stays hidden until the player is within 55 cm of her site.
- Ithaca is hidden about 1.5 m from the start. Its flag and arrival music play within 60 cm, and reaching it finishes the voyage.
- Each new voyage starts with an empty journal. Boss victories unlock their clues in the order earned. The Oracle of Delphi answers gameplay questions with its own chat panel.
- The home screen includes **Test Cyclops**, **Test Medusa**, **Test Sphinx**, and **Reached Ithaca**. The Ithaca test previews the arrival and returns to the home screen. The iOS app icon is the helmet-and-wave artwork.

## Build from Unity

1. Open `iOS_MapStoryEngine` in Unity **2019.4.41f2** with **iOS Build Support** installed. The project uses AR Foundation 3.1.3 and ARKit XR Plugin 3.0.4.
2. Follow [team setup](TEAM_SETUP.md) for the local Google Places and Mapbox credentials. For the Oracle and Sphinx, follow the [server setup](server/README.md) and configure a shared HTTPS URL before exporting.
3. Open `Assets/StoryEngine/Scenes/MapStory.unity`. In **File → Build Settings**, select **iOS**, keep `MapStory` first in the scene list, and choose **Build**. Choose **Append** when updating your existing Xcode export.
4. Open the generated `Unity-iPhone.xcodeproj`. In the `Unity-iPhone` target's **Signing & Capabilities**, select your development team. Connect and select a Face ID iPhone, then press **⌘R**.

The local `.ardyssey.local.json` and venue endpoint files are ignored by Git. The export includes the client configuration in `Data/Raw/ardyssey-client-config.json`. Keep credential-bearing exports and app bundles private.

## Xcode export fixes

Unity's postprocessor qualifies public `UnityFramework` header includes, links ARKit to `UnityFramework`, adds the Mapbox Mobile Events header search path, and removes `-mno-thumb` from the app target. The Unity project sets the iOS deployment target to 15.0.

For a fresh export with the iOS 27 SDK, reproduce the launch fixes from the working Xcode export if needed. Its `Info.plist` and `Classes/UnityAppController.mm` use `OdysseySceneDelegate` to attach Unity's window to a `UIWindowScene` and forward lifecycle events. Its `UnityFramework` target uses `GCC_STRICT_ALIASING = NO` for Unity 2019's generated IL2CPP code. Check these settings after appending if Xcode regenerated the affected files.

## How to test

1. Open the Unity project and check its Console for compiler errors.
2. Run `python -m pytest -q server` from the repository root after installing `server/requirements.txt`.
3. On iPhone, try all four home test buttons. Ask the Oracle a gameplay question and answer a Sphinx riddle by typing or speaking with voice configured.
4. Tap **Play**, open the clue journal, and confirm it starts empty. Walk to the bosses in any order and confirm each victory unlocks only its own clue. Walk to Ithaca to check the reveal and arrival audio.
