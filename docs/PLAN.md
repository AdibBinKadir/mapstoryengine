# Odyssey AR: Team Plan (HackUTA 2026)

**Pitch:** You are Odysseus, starting at **Troy**. Somewhere on a closed real-world map lies **Ithaca**, but nobody tells you where. Vague clues trickle in over time. Better ones must be earned:
- by out-talking the rage-baiting **Sphinx**, by voice,
- by surviving **Cyber Medusa** hijacking your phone (look at the screen and you're STONED),
- and by beating a **Cyclops whose eye has been healed** in an AR boss fight.

Meanwhile, invisible **Hydra** poison zones drain your HP every second, and the only smart move is to run.

**Platform:** **iPhone**, using the `iOS_MapStoryEngine` project (ARKit). Builds need Farhan's Mac (see `changes.md`). **Everyone uses Unity 2019.4.41f2 (LTS).** No other version. `Android_MapStoryEngine` stays as an untouched backup.
**Demo:** indoors at a table, using **Demo Mode** (fake GPS teleport buttons). Also record a real outdoor run on video as a backup.

> ⚠️ Items marked **TBD** are still being decided. Agents: build the *proposal* only if your human confirms it.

---

## 1. Game design

### The world
- **Open world, closed map.** All locations are **fixed coordinates** in `GameConfig`:
  - Troy (start), Ithaca (the hidden goal);
  - the Sphinx, Medusa's lair and the Cyclops' cave;
  - Hydra zones.

  Google Places is optional: keep it only if we want real place names in clues or in the Sphinx's lines.
- **Default screen = back-camera AR view** with a HUD: HP bar, clue journal button, and map button (the mapstoryengine map, opened as a toggle).
- **Win:** walk within 20 m of Ithaca. Clues are how you figure out where that is.
- **Death:** from STONED, poison or the Cyclops. You see a death screen, then you must **walk to a respawn point** to restore full HP. *TBD: which respawn point. Proposal: the nearest of 2–3 "ships"; Troy is always one.*

### Clues (the progression system)
| Source | Tier | How the player gets it |
|---|---|---|
| Timer | **Vague** | Unlocks automatically every ~5 min (every 30 s in Demo Mode) |
| **Sphinx** | **Good** | Answer her riddle **by voice** |
| **Medusa** | **Good** | Defeat her with the mirror; the clue appears **as text on screen** |
| **Cyclops** | **Specific** | Defeat him; the clue appears **as text on screen** |
| **Hydra** | none | She is never worth fighting |

Every unlocked clue also goes into the clue journal so the player can reread it.
Clues are **written by the team** in `Assets/Game/Core/Clues.json`, because they need local knowledge of where Ithaca is. The LLM only words the Sphinx's lines and riddles; it never invents clues.

### Encounters
**Sphinx** (back AR, voice only, rage-bait):
- She's placed at her fixed spot in the real world, Pokémon Go style.
- **The conversation is spoken both ways.** The player **holds "Speak" and talks**. Unity records up to ~8 s (16 kHz mono) and sends it to `POST /sphinx/talk`.
- The server transcribes it, and Gemini answers in character. She's deliberately frustrating and rage-baiting. Gemini also judges riddle answers, forgiving speech-to-text misspellings.
- A correct answer gives a **Good** clue.
- *TBD (P2): how her voice gets to the phone (text-to-speech on the server or on the phone), and whether her lines also show as subtitles.*
- If the server takes over 8 s, play or show one built-in line.

**Cyber Medusa** (front camera only; the back camera is never used for her). She has a **fixed lair** on the map. Walking into her radius triggers her:
1. **She hijacks the screen** with a scary, glitchy full-screen video or animation. Rear AR is paused.
2. **A 3-2-1 countdown**, then the danger starts.
3. **The front camera runs hidden**: its feed is never shown, and it only detects the player.
   - The player is safe while **looking away** or **with eyes closed**.
   - **Looking at the screen** with eyes open for ~0.5 s = **STONED** (death).
   - Tracking uses eyes if possible, with head direction as the fallback.
4. Audio (left/right hiss) tells you she's still there.

| How it ends | Result |
|---|---|
| **Show her a mirror** | She's defeated; a **Good** clue appears **as text on screen** |
| **Leave her lair** (walk out of her radius) | You escape; no clue |
| **Look at her** | STONED (death) |

- *TBD (P2): what "showing her a mirror" is for the player (a real mirror held up to the phone, or an in-game action), and how the phone detects it.*
- *Being checked: whether the iPhone's Face ID camera (ARKit face tracking) can detect eyes and head direction on the phone. If it can, Medusa doesn't need the server's `/frame`. Decided at CP1.*

**Cyclops** (back AR, full boss fight): a 3D figure **standing still** at his cave when you arrive.
- **Tap = attack.** **Shield = a physical gesture.** *TBD (P3): which gesture. Current proposal: raise the phone, meaning the AR camera ends up ≥15 cm above where it was when the fight started (AR Foundation gives the camera's position). The threshold is tunable.*
- *TBD (proposal for sleeping/awake):*
  - He starts **asleep**. Sneak attacks deal 2× damage, and after 3 hits (or 10 s) he wakes up.
  - **Awake:** his healed eye glows (~1.5 s warning), then he slams his club or throws a boulder. **Shield up at impact = blocked**; otherwise you take damage. Tap to hit him between attacks.
  - Sometimes he yawns and **dozes off** for ~5 s, giving another 2× damage window.
- **Win:** he collapses, and a **Specific** clue appears **as text on screen**. *TBD bonus: full HP (he feasts on his sheep).* **Lose:** death.

**Hydra** (survival; **run away**):
1. Her zones are invisible: a 30 m radius around fixed points, using smoothed GPS. There's no character on the main screen.
2. Inside one: a **green screen tint** plus a notification, and **poison drains HP every second**.
3. A **"Fight Hydra"** button opens a **separate minigame**. Winning it only buys a **30 s break from the poison**. She gives **no clue**.
4. Leaving the zone stops the poison. If she kills you, you die.
5. *TBD (P3): what the minigame is, and whether heads still multiply (heads × 2, stronger poison) after each break.*

---

## 2. Roles
| Role | Who | Owns (folders) | Builds |
|---|---|---|---|
| **P1: Build & World** | **Farhan** (has the Mac) | `iOS_MapStoryEngine/Assets/StoryEngine/Scenes/MapStory.unity`, `iOS_MapStoryEngine/ProjectSettings/`, `iOS_MapStoryEngine/Packages/`, `Assets/Game/Core/` (except Contracts and Clues.json), `Assets/Game/UI/`, `Assets/Game/GameSystems.prefab` | 1) Every iPhone build (Unity → Xcode → phone, reapplying the `changes.md` fixes). 2) Contract stubs in hour 1. 3) `GameState` (HP, poison, death, walking to respawn, win at Ithaca), `ClueBook` (tiers, periodic vague clues, journal UI, on-screen clue popup), `WorldMap` (fixed locations and zone checks), **Demo Mode** (teleport buttons and an encounter trigger panel), HUD. 4) Adding `GameSystems` to the scene once. |
| **P2: Medusa & Sphinx** | *(name)* | `Assets/Game/CameraMode/`, `Assets/Game/Encounters/Medusa/`, `Assets/Game/Encounters/Sphinx/`, `Assets/Game/Net/`, `server/` | Both encounters end to end. **Medusa:** takeover screen, countdown, front-camera detection (Face ID path or server `/frame`), mirror, leave, STONED. **Sphinx:** AR placement, Speak button + mic + WAV, her voice, the server's `/sphinx` and `/sphinx/talk` (speech-to-text + Gemini). |
| **P3: Cyclops & Hydra** | *(name)* | `Assets/Game/Encounters/Cyclops/`, `Assets/Game/Encounters/Hydra/` | **Cyclops boss fight** (AR placement, sleep/awake state machine, tap attacks, shield gesture, telegraphs, win/lose). **Hydra** (tint, poison, "Fight Hydra" button, minigame, 30 s break). |
| **P4: Assets & Pitch** | *(name)* | `Assets/Game/Art/` (models, animations, audio, Medusa video), `Assets/Game/Core/Clues.json` | Models and animations for the Sphinx, Medusa, Cyclops and Hydra; Medusa's takeover video; sounds. Writes the **clue list** with the team. Then the Devpost write-up, the video and the pitch. |

Also: someone is checking whether Face ID / ARKit face tracking works for Medusa (see §1). The result goes to P2 and Farhan at CP1.

---

## 3. How the team stays integrated

### Rule 1: Contracts before code (first hour)
`docs/CONTRACTS.md` defines every connection between people's code (HTTP API, C# events and interfaces, encounter IDs).
- **P1** creates the C# stubs from the contracts *inside Unity* (so `.meta` files exist) in the first hour and merges them.
- Every role has a **mock**, so nobody waits:
  - **Gaze:** the keyboard or debug buttons (`L` = looking, `E` = eyes closed, `M` = mirror).
  - **Server:** a `--fake` mode returns canned replies.
  - **Encounters and events:** triggered from the Demo Mode panel.
- Contract changes: post in the team chat, then a small PR to `CONTRACTS.md`. **Agents never change contracts on their own.**

### Rule 2: One `GameSystems` prefab, so integration is nearly free
- Every gameplay system lives inside `Assets/Game/GameSystems.prefab` or gets spawned by code at runtime from prefab references in `GameConfig`.
- **Farhan adds `GameSystems` to `MapStory.unity` once.** After that, nobody needs to touch the scene again.
- Everyone builds and tests in a personal sandbox scene: `Assets/Game/Sandbox/<Name>.unity`.

### Rule 3: Folder ownership
You and your agent only edit **your own folders** (table above). Need a change elsewhere? Ask its owner. Never modify `Assets/Mapbox/`.

### Rule 4: One shared agent file
`AGENTS.md` (Codex) is the single source of truth, and `CLAUDE.md` imports it. When an agent learns a team-wide lesson, add one line to "Lessons learned" in a PR.

### Rule 5: Small branches, frequent merges
- Branches look like `p3/cyclops-shield`. Merge into `main` **at least every ~2 hours**, and rebase first.
- Every PR has a **"How to test"** section, for example: "Open Sandbox/Ana.unity, press C, the Cyclops spawns asleep."
- Farhan skims PRs that touch `Assets/`. P2 owns `server/` reviews.
- **Only Farhan builds for the iPhone** (it needs his Mac). Send him a heads-up before you need a phone test.

### Rule 6: Unity help
Stuck on Unity? Try your agent for 15 minutes first. Save non-blocking questions for the checkpoint standup.

### Rule 7: Track tasks
GitHub Issues are turned off on the repo. Track tasks in the team chat, or turn Issues on and use one issue per task, labeled `P1`–`P4`.

---

## 4. Scope tiers
| Tier | Features |
|---|---|
| **Must** (needed for the demo) | Demo Mode; AR home screen + HUD; HP/death/respawn; clue journal + on-screen clue popup + Ithaca win; Sphinx voice conversation + riddle → clue; Medusa takeover + countdown + STONED + leave-to-escape; Cyclops tap attack + shield + win/lose → clue |
| **Should** | Medusa mirror defeat → clue; Medusa eyes-closed detection; Hydra (tint, poison, minigame, 30 s break); Cyclops sleep/doze phases; periodic vague clues; Medusa directional audio |
| **Stretch** | Map boundary warning; real outdoor run; polished models and animations |

---

## 5. Timeline (H0 = start of plan)

| Time | Checkpoint / everyone | P1 Farhan | P2 | P3 | P4 |
|---|---|---|---|---|---|
| **H0–1** | Assign names. Read CONTRACTS together. **Pick the real coordinates** for Troy, Ithaca, the Sphinx, Medusa, the Cyclops and the Hydra zones. Get keys (Mapbox, Gemini, Groq). | Contract stubs + sandbox scenes | Server skeleton + `--fake` mode | Install Unity, read the AR placement code | Asset list |
| **H1–3** | | The original app on the iPhone, then `GameState`, HUD, Demo Mode panel | Face ID check result → pick Medusa's detection path; Sphinx mic recording | Cyclops placed in AR (sandbox), tap to damage | Placeholder models for all four |
| **✅ CP1 @ H3** | The app runs on the iPhone. **Medusa detection path: Face ID or server.** | | | | |
| **H3–8** | | `ClueBook` + journal + clue popup, `WorldMap`, Ithaca win, respawn | Medusa takeover + countdown + STONED; `/sphinx/talk` (speech-to-text + Gemini) | Cyclops shield + attack loop | Clue list; Medusa video |
| **✅ CP2 @ H8** | Each encounter works **on its own** in the editor (with mocks). The phone reaches the server over the hotspot. | | | | |
| **H8–12** | | Add `GameSystems` to the scene, phone builds | Medusa mirror + leave; Sphinx voice connected to the real server | Hydra; Cyclops sleep phases | Final models and sounds |
| **✅ CP3 @ H12** | **A full demo on the phone:** Troy → Sphinx clue → Hydra scare → Cyclops → Medusa → Ithaca. **Feature freeze at H13.** | | | | |
| **H12–15** | | Fixes, final build | Polish | Polish | **Devpost + video script** |
| **H15–17** | Record the backup video. Rehearse the pitch twice. | | | | |
| **H17–18** | Buffer. **Submit an hour early.** | | | | |

Sleep in shifts of no more than 2 people, never during a checkpoint. Farhan sleeps after CP3.

---

## 6. Risks & fallbacks
| Risk | Check by | Fallback |
|---|---|---|
| The iPhone build breaks (Xcode export fixes lost) | Farhan, every build | Reapply the fixes listed in `changes.md`; nobody else can build, so tell Farhan early. |
| Face ID tracking isn't usable from Unity 2019.4 / AR Foundation 3.1 | Face ID checker, CP1 | Pause AR, use the front camera through `WebCamTexture`, and send frames to the server's `/frame`. |
| The front-camera switch crashes | P2, CP1 | Medusa runs in the demo with the debug-panel mock for looking. The takeover visuals still work. |
| The phone can't reach the laptop | P2, CP2 | Use a phone hotspot, allow the iOS **Local Network** prompt, and make sure plain HTTP is allowed (see `AGENTS.md`). Medusa falls back to the mock, and the Sphinx to the typed debug box. |
| Gaze or eyes-closed detection is noisy | P2, H8–12 | Widen the thresholds, keep the ~0.5 s hold, add a calibration step. |
| Mirror detection doesn't work | P2, H8–12 | A debug button counts as "mirror shown"; leaving her lair still ends the encounter. |
| Speech-to-text fails in a loud venue | P2 | A hidden debug text box sends typed text to the same endpoint. |
| The shield gesture isn't detected reliably | P3 | Fall back to the phone's tilt (gyro), or a hold-to-shield button. |
| AR placement is jittery | P3/P2 | Place the Cyclops and Sphinx a fixed distance in front of the camera when the encounter starts. |
| Scene merge conflict | Everyone | Only Farhan saves the scene. Discard accidental changes with `git checkout -- <scene>`. |
