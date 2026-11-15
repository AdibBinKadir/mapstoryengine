# Medusa video loop (short design)

## Why
Medusa is a still picture during the danger phase. The owner wants her alive: frantic, eyes darting, snakes thrashing, screaming. The old picture (glossy CGI, orange fire) also clashed with the Odyssey theme (moonlit teal oil painting, `IthacaVoyage.png`). The owner generated a themed, looping 10 s video of her, with sound.

## Goals
- During **Danger** and **Clue**, the portrait slot plays `medusa_loop.mp4` full screen, looped, **with its sound**.
- On **Stoned**, the video freezes on its current frame and the sound stops; the STONED panel shows as today.
- Hijack (flicker), Coming (3-2-1) and everything else stay as on `main`.
- `medusa.png` becomes the video's first frame, so the still image (Hijack flicker, and the fallback) matches the video.
- Nobody outside P2 has to change anything.

## Non-goals
- The glare / stone-flash stills (later, once the owner makes them).
- A separate hiss or directional audio.

## Anti-goals
- A black screen at any point: if the video isn't ready or fails, the still image stays.
- Medusa's sound continuing after the encounter ends or after Abort.

## Constraints
- Unity 2019.4, C# 7.3. `UnityEngine.Video.VideoPlayer` (H.264 MP4 plays on iOS).
- In the main game `CyclopsMapEvent` (P3) adds `MedusaEncounter` at runtime and only passes `medusaImage`. So the clip is loaded with `Resources.Load<VideoClip>("MedusaLoop")` from `Assets/Game/Encounters/Medusa/Resources/MedusaLoop.mp4`, unless the public `medusaVideo` field is set.
- `medusa.png` is overwritten in place (same file, same `.meta` and GUID), so the references in `P2.unity` and `MapStory.unity` stay valid.

## Approaches
1. **Chosen: VideoPlayer → RenderTexture → the existing portrait `RawImage`.** Reuses Farhan's layout, crop and frame; one component on the screen's root.
2. VideoPlayer drawing straight to the camera (`CameraNearPlane`): rejected, it ignores the UI layering and the bronze frame.
3. Still frames swapped in code: dropped by the owner in favour of the video (more natural motion).

## Design
- **`MedusaVideoRules.Plays(MedusaPhase)`** (pure, in `Game.Medusa.Logic`, tested): true for Danger and Clue only.
- **`MedusaScreen.Create(Texture image, VideoClip video)`**: when `video` isn't null, adds a `VideoPlayer` to the root: not playing on awake, looping, `RenderTexture` mode into a texture the clip's size, audio `Direct`. It calls `Prepare()` at once (Hijack + Coming give it ~4 s).
- **`Show(phase)`**: if `Plays(phase)`, play; else pause (Stoned freezes the frame).
- The portrait shows the still image until the first video frame arrives (`frameReady` / `isPlaying` with a frame), then switches to the RenderTexture. On `errorReceived`, it logs once and stays on the still image.
- **`Destroy()`**: stops the player and releases the RenderTexture before destroying the root.
- **`MedusaEncounter`** gains `public VideoClip medusaVideo;`, falling back to `Resources.Load`.

## Testing
- EditMode: `MedusaVideoRules` for every phase.
- Editor: P2 sandbox, Begin → video and sound in Danger, freeze on STONED, silence after Abort / Won.
- Phone: Farhan's next build. Note: the iPhone's silent switch may mute Unity's sound.

## Left to the builder
Log wording; whether to keep the clip's aspect in the `AspectRatioFitter`.
