# Team Setup (HackUTA 2026)

Everyone should do this once before writing any code.

## 1. Install the exact Unity version
The team uses **Unity 2019.4.41f2 (LTS)**. Any other version will try to upgrade the project and break things for everyone else.

1. Install [Unity Hub](https://unity.com/download).
2. Install the editor from this link (it opens Unity Hub): `unityhub://2019.4.41f2/6b23d448b533`
   You can also find it in the [Unity download archive](https://unity.com/releases/editor/archive) under 2019.x.
3. Add these modules:
   - **Android Build Support**, including the Android SDK & NDK Tools and OpenJDK
   - **iOS Build Support** (only needed on a Mac building for iPhone)

## 2. Clone and open
```bash
git clone https://github.com/AdibBinKadir/mapstoryengine.git
```
In Unity Hub, choose **Add** and pick **`Android_MapStoryEngine`** (or `iOS_MapStoryEngine` if your team builds for iPhone).
Then open `Assets/StoryEngine/Scenes/MapStory.unity`.

> The repo has two copies of the project. **Pick one as a team and only work in that one.** Changes do not sync between them.

## 3. Add your API keys (never commit them)
Both key files are gitignored.

- **Mapbox token:** in Unity, go to *Mapbox → Setup* and paste your token. It is saved to `Assets/Resources/Mapbox/MapboxConfiguration.txt`, which is ignored.
- **Google Places key:** create `Assets/Resources/Secrets/GooglePlacesKey.txt` with only the key inside it.
  Leave the `API_KEY` field on **SearchLocalSites_12** *empty* in the Inspector. If you type it there, it gets saved into the scene file and pushed to GitHub.

Before every commit, run `git diff --cached` and make sure no `AIza...` or `pk.ey...` strings are in it.

## 4. Git workflow
Unity scene files merge badly, so follow these rules:

- **Use a branch per feature:** `git checkout -b yourname/feature`. Open a PR or merge into `main` often, about every 1–2 hours.
- **Only one person edits `MapStory.unity` at a time.** Say so in the group chat before you touch it. Build your work as **prefabs** or in your own test scene, and drop it into the main scene at the end.
- **Pull before you start** and before you merge: `git pull --rebase origin main`.
- **Always commit `.meta` files** together with their assets.
- **Never commit** `Library/`, `Temp/`, `Logs/`, or builds. These are already ignored.

### Optional: Unity Smart Merge
This makes scene and prefab conflicts much easier to resolve. Run it once per machine and adjust the path to your install:

Windows:
```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/2019.4.41f2/Editor/Data/Tools/UnityYAMLMerge.exe' merge -h -p --force %O %B %A %A"
```
macOS:
```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'/Applications/Unity/Hub/Editor/2019.4.41f2/Unity.app/Contents/Tools/UnityYAMLMerge' merge -h -p --force %O %B %A %A"
```

## 5. Testing on a phone
- **Android:** turn on Developer Options and USB debugging, plug the phone in, then use *File → Build Settings → Android → Build And Run*. The phone needs ARCore support.
- **iOS:** this requires a Mac with Xcode and an Apple ID for signing.
- The app needs **location + camera** permissions, and you have to be outdoors near real places for Google Places to return sites.
