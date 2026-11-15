# Odyssey AR: Team Plan (HackUTA 2026)

**Pitch:** You are Odysseus, starting at **Troy**. Somewhere on a closed real-world map lies **Ithaca**, but nobody tells you where. Vague clues trickle in over time. Better ones must be earned: by out-talking the **Sphinx** (by voice), by surviving **Cyber Medusa** hijacking your phone (don't look at the screen, or you're STONED), and by beating a **Cyclops whose eye has been healed** in an AR boss fight. Meanwhile, invisible **Hydra** poison zones drain you, and killing her only makes more heads.

**Platform:** Android, using the `Android_MapStoryEngine` project. **Everyone uses the same Unity version** (proposed: 2019.4.41f2; confirm as a team).
**Demo:** indoors at a table, using **Demo Mode** (fake GPS teleport buttons). Also record a real outdoor run on video as a backup.

> ⚠️ Items marked **TBD** are still being decided. Agents: build the *proposal* only if your human confirms it.

---

## 1. Game design

### The world
- **Open world, closed map.** All locations are **fixed coordinates** in `GameConfig`: Troy (start), Ithaca (hidden goal), the Sphinx, the Cyclops' cave, and Hydra zones. Medusa isn't tied to a location. This means we **don't need Google Places** for POIs anymore.
- **Default screen = back-camera AR view** with a HUD: HP bar, clue journal button, and map button (the mapstoryengine map, opened as a toggle).
- **Win:** walk within 20 m of Ithaca. Clues are how you figure out where that is.
- **Death:** from STONED, poison or the Cyclops. You see a death screen, then you must **walk to a respawn point** to restore full HP. *TBD: which respawn point. Proposal: the nearest of 2–3 "ships"; Troy is always one.*

### Clues (the progression system)
| Tier | Source | Example |
|---|---|---|
| **Vague** | Unlocks automatically every ~5 min (every 30 s in Demo Mode) | "Ithaca lies where the sun sets on Troy." |
| **Good** | Sphinx (answer her riddle), Medusa (survive her hijack) | "Ithaca stands beside water, north of the great hall." |
| **Specific** | Cyclops (defeat him) | "Ithaca is the bench under the oak by the library." |

Clues are **written by the team** in `Assets/Game/Core/Clues.json`, because they need local knowledge of where Ithaca is. The LLM only rewords the Sphinx's lines; it never invents clues.

### Encounters
**Sphinx** (back AR, words): placed at her fixed spot in the real world, Pokémon Go style. The player **holds "Speak" and talks**. Unity records up to ~8 s (16 kHz mono) and sends it to `POST /sphinx/talk`. The server transcribes it, the LLM answers in character (rage-bait), and it judges riddle answers, forgiving speech-to-text misspellings. A correct answer gives a **Good** clue. Always show `heard` so the player sees what she understood. If the server takes over 8 s, show one built-in line.

**Cyber Medusa** (front camera, never the back camera): she **hijacks the screen** with a glitchy, full-screen on-screen animation (video or model). Rear AR is paused, and the **front camera runs hidden** purely as a detector; its feed is never shown. If `LookingAtScreen` holds for ~0.5 s, the player is **STONED** (death). Audio (left/right hiss) tells you she's still there.
- *TBD (proposal): she triggers randomly while you explore (every ~8–12 min; a button in Demo Mode). If you survive ~20 s without looking, she glitches out and drops a **Good** clue.*

**Cyclops** (back AR, full boss fight): a 3D figure **standing still** at his cave when you arrive.
- **Tap = attack. Raise the phone = shield.** "Raised" means the AR camera is ≥15 cm above where it was when the fight started (AR Foundation gives the camera's position). The threshold is tunable.
- *TBD (proposal for sleeping/awake):*
  - He starts **asleep**. Sneak attacks deal 2× damage, and after 3 hits (or 10 s) he wakes up.
  - **Awake:** his healed eye glows (~1.5 s warning), then he slams his club or throws a boulder. **Shield up at impact = blocked**; otherwise you take damage. Tap to hit him between attacks.
  - Sometimes he yawns and **dozes off** for ~5 s, giving another 2× damage window.
- **Win:** he collapses. *TBD reward: a **Specific** clue + **full HP** (he feasts on his sheep).* **Lose:** death.

**Hydra** (survival; never worth fighting):
1. Her zones are invisible: a 30 m radius around fixed points, using smoothed GPS. Inside one: a **green screen tint** plus a notification, and **poison drains 1 HP/s × number of heads**.
2. A **"Fight Hydra"** button appears, and tapping it **reveals her** in back AR. Heads are weak; a few taps kill one.
3. Killing a head gives a **30 s break from the poison**, then she **multiplies** (heads × 2) and the poison gets stronger.
4. Leaving the zone stops the poison. Each zone remembers its head count. If she kills you, you die.

---

## 2. Roles
P1 is the only experienced Unity dev, so P1 **does not build big features**. P1 handles the two risky technical pieces (Android build, front-camera switch) plus Medusa, which depends on them. Integration is designed to take minutes, not hours (see Rule 2).

| Role | Unity? | Owns (folders) | Builds |
|---|---|---|---|
| **P1: Unity Lead** | Yes (experienced) | `MapStory.unity`, `Assets/Game/CameraMode/`, `Assets/Game/Encounters/Medusa/`, `Assets/Game/Net/CvClient*`, `ProjectSettings/`, `Packages/` | 1) The original app building to an Android phone. 2) **Camera switch** (pause rear AR → hidden front camera → resume). 3) `CvClient` (frames → server). 4) **Medusa's** hijack screen and STONED logic. 5) Adding the `GameSystems` prefab to the scene once. 6) The APK at every checkpoint. |
| **P2: World & Hydra** | Yes (learning, agent-driven) | `Assets/Game/Core/`, `Assets/Game/Encounters/Hydra/`, `Assets/Game/UI/` | `GameState` (HP, poison, death, walking to respawn, win at Ithaca), `ClueBook` (tiers, periodic vague clues, journal UI), `WorldMap` (fixed locations and zone checks), **Demo Mode** (teleport buttons and an encounter trigger panel), HUD, the full **Hydra**. Also creates the contract stubs in hour 1. |
| **P3: Server & Content** | **No** | `server/`, `Assets/Game/Core/Clues.json` (content only) | The Python server: `/frame` (MediaPipe head pose → `looking_at_screen`) plus a webcam test tool; `/sphinx` and `/sphinx/talk` (speech-to-text + LLM). Writes the **clue list** with the team. Then the Medusa/Sphinx media assets, the Devpost write-up, the video and the pitch. |
| **P4: Cyclops & Sphinx** | Yes (learning, agent-driven) | `Assets/Game/Encounters/Cyclops/`, `Assets/Game/Encounters/Sphinx/`, `Assets/Game/Net/SphinxClient*` | **The Cyclops boss fight** (AR placement, sleep/awake state machine, tap attacks, shield via phone height, telegraphs, win/lose). **The Sphinx in Unity** (AR placement, Speak button + mic recording + WAV, showing replies). |

Assign names to P1–P4 now.

---

## 3. How the team stays integrated

### Rule 1: Contracts before code (first hour)
`docs/CONTRACTS.md` defines every connection between people's code (HTTP API, C# events and interfaces, encounter IDs).
- **P2** creates the C# stubs from the contracts *inside Unity* (so `.meta` files exist) in the first hour and merges them.
- Every role has a **mock**, so nobody waits:
  - **Gaze:** the keyboard or debug buttons (`L` = looking).
  - **Server:** a `--fake` mode returns canned replies.
  - **Encounters and events:** triggered from the Demo Mode panel.
- Contract changes: post in the team chat, then a small PR to `CONTRACTS.md`. **Agents never change contracts on their own.**

### Rule 2: One `GameSystems` prefab, so integration is nearly free
- Every gameplay system lives inside `Assets/Game/GameSystems.prefab` or gets spawned by code at runtime from prefab references in `GameConfig`.
- **P1 adds `GameSystems` to `MapStory.unity` once.** After that, nobody needs to touch the scene again.
- Everyone builds and tests in a personal sandbox scene: `Assets/Game/Sandbox/<Name>.unity`.

### Rule 3: Folder ownership
You and your agent only edit **your own folders** (table above). Need a change elsewhere? Ask its owner. Never modify `Assets/Mapbox/`.

### Rule 4: One shared agent file
`AGENTS.md` (Codex) is the single source of truth, and `CLAUDE.md` imports it. When an agent learns a team-wide lesson, add one line to "Lessons learned" in a PR.

### Rule 5: Small branches, frequent merges
- Branches look like `p4/cyclops-shield`. Merge into `main` **at least every ~2 hours**, and rebase first.
- Every PR has a **"How to test"** section, for example: "Open Sandbox/Ana.unity, press C, the Cyclops spawns asleep."
- P1 skims PRs that touch `Assets/`. P3 or P4 reviews `server/`.
- **Only P1 builds the demo APK.** After the first successful build, P1 shows P4 how to build, so there are two people who can.

### Rule 6: Unity help
Stuck on Unity? Try your agent for 15 minutes first. Save non-blocking questions for the checkpoint standup.

### Rule 7: Track tasks as GitHub Issues
One issue per task, labeled `P1`–`P4`. Agents can use `gh issue view <n>` as their prompt, and PRs say `Closes #<n>`.

---

## 4. Scope tiers
| Tier | Features |
|---|---|
| **Must** (needed for the demo) | Demo Mode; AR home screen + HUD; HP/death/respawn; clue journal + Ithaca win; Sphinx voice + riddle → clue; Medusa hijack + STONED; Cyclops tap attack + shield block + win/lose |
| **Should** | Hydra (tint, poison, reveal, multiplying); Cyclops sleep/doze phases; periodic vague clues; Medusa directional audio |
| **Stretch** | Sphinx speaks out loud (TTS); map boundary warning; real outdoor run; polished models and animations |

---

## 5. Timeline (H0 = start of plan)

| Time | Checkpoint / everyone | P1 | P2 | P3 | P4 |
|---|---|---|---|---|---|
| **H0–1** | Assign names. Read CONTRACTS together. Install Unity (P1, P2, P4). Get keys (Mapbox, LLM, speech-to-text). **Pick the real coordinates** for Troy, Ithaca, the Sphinx, the Cyclops and the Hydra zones. | Start the Android build | Contract stubs + sandbox scenes | Server skeleton + `--fake` mode | Install Unity, read the AR placement code in mapstoryengine |
| **H1–3** | | **The original app on a phone**, then the camera-switch test | `GameState`, HUD, Demo Mode panel | Webcam → `looking_at_screen` | Cyclops placed in AR (sandbox), tap to damage |
| **✅ CP1 @ H3** | The app runs on a phone. **Camera switch: go or no-go** (no-go means Medusa uses the mock in the demo). The server detects looking from the webcam. | | | | |
| **H3–8** | | `CvClient`, Medusa hijack + STONED | `ClueBook` + journal, `WorldMap`, Ithaca win, respawn | `/sphinx/talk` (speech-to-text + LLM), clue list | Cyclops shield + attack loop; Sphinx placement + mic recording |
| **✅ CP2 @ H8** | Each encounter works **on its own** in the editor (with mocks). The phone reaches the server over the hotspot. | | | | |
| **H8–12** | | Add `GameSystems` to the scene, APK | Hydra | Tune gaze on real phone frames; media assets | Cyclops sleep phases; Sphinx connected to the real server |
| **✅ CP3 @ H12** | **A full demo on the phone:** Troy → Sphinx clue → Hydra scare → Cyclops → Medusa → Ithaca. **Feature freeze at H13.** | | | | |
| **H12–15** | | Fixes, final APK | Polish | **Devpost + video script** | Polish |
| **H15–17** | Record the backup video. Rehearse the pitch twice. | | | | |
| **H17–18** | Buffer. **Submit an hour early.** | | | | |

Sleep in shifts of no more than 2 people, never during a checkpoint. P1 sleeps after CP3.

---

## 6. Risks & fallbacks
| Risk | Check by | Fallback |
|---|---|---|
| The app won't build for Android | P1, H0–3 | Fix that first; everyone helps if P1 is stuck. |
| The front-camera switch crashes | P1, CP1 | Medusa runs in the demo with the debug-panel mock for looking. The hijack visuals still work. |
| The phone can't reach the laptop | P3, CP2 | Use a phone hotspot. Medusa falls back to the mock, and the Sphinx to the typed debug box. |
| Gaze detection is noisy | P3, H8–12 | Widen the thresholds, keep the ~0.5 s hold, add a calibration step. |
| Speech-to-text fails in a loud venue | P3/P4 | A hidden debug text box sends typed text to the same endpoint. |
| Raising the phone isn't detected reliably | P4 | Fall back to the phone's tilt (gyro), or a hold-to-shield button. |
| AR placement is jittery | P4 | Place the Cyclops and Sphinx a fixed distance in front of the camera when the encounter starts. |
| Scene merge conflict | Everyone | Only P1 saves the scene. Discard accidental changes with `git checkout -- <scene>`. |
