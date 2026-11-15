# Odyssey AR build changes

## Unity projects

- The Android starter uses Unity 2019.4.41f2 and ARCore. Its setup includes Unity MCP, Google Places Nearby Search (New), and local credential loading. See [UNITY.md](UNITY.md) for the Android changes and their verification status.
- The `ios` branch upgraded `iOS_MapStoryEngine` to Unity 2019.4.41f2. The build opens `Assets/StoryEngine/Scenes/MapStory.unity` and uses AR Foundation 3.1.3 with ARKit XR Plugin 3.0.4.
- The iOS starter now uses Google Places Nearby Search (New). The title flow waits for the search to finish before continuing. The iOS AR path uses AR Foundation, and the branch includes native compatibility fixes for the legacy Mapbox AR plugin.
- The iOS credential loader accepts the `GOOGLE_PLACES_API_KEY` and `MAPBOX_ACCESS_TOKEN` environment variables or the ignored `iOS_MapStoryEngine/.ardyssey.local.json` file. Copy the [example file](iOS_MapStoryEngine/.ardyssey.example.json) and fill in `GooglePlacesApiKey` and `MapboxAccessToken` with a Places API (New) key and a public Mapbox token. The export step writes them to `Data/Raw/ardyssey-client-config.json`. Keep the local profile and credential-bearing exports and app bundles private.

## Local Xcode export

The current `Odyssey iOS Xcode` export was generated from the iOS project and built with Xcode 27.2. These fixes were applied to that generated export. Reapply them when regenerating the export until they are incorporated into the Unity build pipeline.

- Set the minimum deployment target to iOS 15.0 for the installed Xcode SDK.
- Added the bundled Mapbox Mobile Events header directory to the `UnityFramework` header search paths and removed the unsupported `-mno-thumb` flag from the arm64 app target. The local Unity working tree also has an uncommitted postprocessor update for Xcode target selection, the header path, and the compiler flag.
- Added a UIKit scene manifest in `Info.plist` and a scene delegate in `Classes/UnityAppController.mm`. The scene delegate attaches Unity's window to a `UIWindowScene` and forwards foreground, active, inactive, and background transitions to Unity's existing handlers. This fixes the startup trap on iOS 27 with the iOS 27 SDK.
- Disabled strict aliasing for `UnityFramework` to fix startup crashes in Unity 2019's generated IL2CPP delegate code with the current compiler.

## Verification

- Xcode built and signed `iOSMapStoryEngine`. The signed build contained both credentials and was installed on an iPhone 15 Pro running iOS 27.0.1. The app launched and its process stayed running after startup.
- Xcode Run had failed during device symbol extraction on a nearly full Mac. Installation and launch were verified using Xcode's `devicectl`.
- Live Google Places results, Mapbox tile loading, and AR gameplay still need an on-device test.
