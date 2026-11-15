# Team Setup (HackUTA 2026)

Everyone should do this once before writing any code.

## 1. Install the exact Unity version
The team uses **Unity 2019.4.41f2 (LTS)**. Any other version will try to upgrade the project and break things for everyone else.

1. Install [Unity Hub](https://unity.com/download).
2. Install the editor from this link (it opens Unity Hub): `unityhub://2019.4.41f2/6b23d448b533`
   You can also find it in the [Unity download archive](https://unity.com/releases/editor/archive) under 2019.x.
3. Add the **iOS Build Support** module. It lets the Editor switch to the iOS platform, even on Windows. Only Farhan's Mac turns the export into a phone build.

## 2. Clone and open
Also read **`changes.md`** for the iOS build notes.
```bash
git clone https://github.com/AdibBinKadir/mapstoryengine.git
```
In Unity Hub, choose **Add** and pick **`iOS_MapStoryEngine`**.
Then open `Assets/StoryEngine/Scenes/MapStory.unity`.

> The repo has two copies of the project. **The team builds for iPhone, so only work in `iOS_MapStoryEngine`.** `Android_MapStoryEngine` is a backup. Changes do not sync between them.

## 3. Add your API keys (never commit them)
The project reads `GOOGLE_PLACES_API_KEY` and `MAPBOX_ACCESS_TOKEN` (details in `changes.md` and the README):
1. Copy `iOS_MapStoryEngine/.ardyssey.example.json` to `iOS_MapStoryEngine/.ardyssey.local.json` (gitignored) and fill in `GooglePlacesApiKey` and `MapboxAccessToken`. Environment variables take precedence if you set them.
2. Google key: enable **Places API (New)** and restrict the key to that API. Mapbox: use a **public** token.
3. **Don't** paste keys into the Inspector or the *Mapbox → Setup* window, because those get saved into tracked files.
4. Never commit Xcode exports or app bundles; they contain the keys.
5. Set up the [Oracle and Sphinx server](server/README.md) with `server/.env`. For iPhone testing, configure the same HTTPS endpoint for both features. The local `OracleEndpoint.txt` and its Unity-generated `.meta` are ignored by Git.

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
- **Only Farhan builds to the iPhone.** It needs his Mac, Xcode, and the export fixes in `changes.md`. Everyone else tests in the Editor with the mocks, and asks Farhan for a phone build.
- The app needs **location + camera + microphone** permissions, plus **Local Network** to reach the laptop server.
