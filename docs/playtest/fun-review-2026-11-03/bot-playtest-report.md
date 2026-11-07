# Kestrel Heavy: fun review, bot playtest (2026-11-03)

Two scripted personas played the first ~15 minutes of the game from a fresh save on current `main`.

## What was played

| | |
|---|---|
| Build | **Unity WebGL** CI artifact `webgl-build`, Actions run 41556949101, headSha `30262fe9` (= `origin/main`, "Boost liftoff/max-Q screen shake… (#412)"), product version 2.0.0 |
| Not played | Phaser `web/` demo; Unity PlayMode/editor (Unity CLI is not signed in on the box, so the sanctioned `unity` CLI path is blocked) |
| Host | Served locally with brotli/gzip headers; headless Chrome (Playwright) on SwiftShader, about 5–20 fps |
| Network | Zero external requests. The only `netreq` entries are local `blob:` URLs (see `*/actions.jsonl`) |
| Not verified | **Not device-verified.** Frame rate, touch, haptics and juice all need checking on iPhone. Taps are mouse events, and touch was not exercised |

| Persona | Viewport | Start (EST) | Length | Log |
|---|---|---|---|---|
| Kid: taps the obvious button, skips text, accepts defaults, reflex-taps glowing prompts | 390×844 | 20:29:45 | ~17.7 min | `kid/actions.jsonl` |
| Engineer: full designer, sandbox, edge cases | 440×956 | 20:47:46 | ~15 min | `engineer/actions.jsonl` |

`t` in the logs is seconds since page load. Every tap, note, console error and screenshot is logged. The PNGs here are a curated subset of the ~1,190 captured frames. The harness lives in `harness/` (`ctl.cjs` controller, `watch.py` frame watcher with kid reflex taps).

## Kid timeline

| t (s) | Screen / action | Note |
|---|---|---|
| 4.4 | TAP TO PLAY | Loader ready at 4.25 s |
| 22.1 | Home → **LAUNCH KH-1** | Opens a text-heavy briefing, not a launch (`kid/003`) |
| 46.6 | Build it | Simple designer shows defaults as "Ready" (`kid/007`) |
| 74.8 | LAUNCH | ~18 s countdown. The countdown says "89 m/s short for First Hop", which contradicts "Ready" (`kid/013`) |
| ~93 | Liftoff | HUD shows Eng/Tnk/Hull/Guide/Fair, Q kPa, G (`kid/020`) |
| ~126 | Hard cut to results at ~T+33 | Kid missed the THROTTLE prompt and got 1★ bronze (`kid/024`, `026`→`027`) |
| 190.6 / 225.4 | Taps **Fix it** (the big red primary button) twice | The first tap re-renders the report as "Stars saved. Kc was settled…" with empty charts. Nothing else visible happens (`kid/040`→`041`, `044`) |
| 230.4 | Next mission → map → Fly KH-2 → briefing → Build it → LAUNCH | 4 taps to fly again (`kid/047`, `048`, `052`) |
| 343–376 | Reflex taps on NUDGE PITCH / THROTTLE | 2★ silver |
| 536.8–557.0 | KH-3: Next → Fly → Build it → LAUNCH (defaults "Ready… fuel to spare") | `kid/300`, `302` |
| 625–690 | Follows both prompts | "Short on speed" shows from about T+60 |
| ~803 | **Structural breakup at T+165** (20 km, 2.76 km/s, 82 kPa) | 11 console errors, "Can't add component… CapsuleCollider/SphereCollider doesn't exist". No breakup visual: the screen cuts straight to text (`kid/465`, `611`, `640`→`641`) |
| 976.18 | Taps **Fix it** | **NullReferenceException 0.1 s later**. The UI disappears and only sky plus the web EXIT button remain (`kid/839`) |
| 976–1057 | Taps the centre, the bottom, and Escape | **Soft-lock for more than 60 s** (`kid/851`) |
| 1060 | Page reload | The save survives ("CONTINUE KH-3, 2 of 20 flown") (`kid/852`, `854`) |

**Kid dead time:** about 18 s countdown per flight, 30–165 s of passive "Auto-assist on" ascent, and ~20 s of static "Heating hard". In the first 15 minutes the kid made about 12 meaningful taps and spent most of the session watching.

## Engineer timeline

| t (s) | Action | Result |
|---|---|---|
| 16.7 | Settings gear (half hidden under the web EXIT button) → Show full designer On | `engineer/002` |
| 52–103 | Full designer on default KH-1 | "**4 things to fix**", although the simple designer says Ready. Cost shows as **$516.71M** (Kc everywhere else). The stats row clips ("Ma:" / "49.0 kP") after a tab switch. Mouse-drag doesn't scroll. Parts opens an exploded view with the camera at or under pad level (`engineer/008`, `009`, `013`) |
| 118.5 | Launch with 4 warnings | The countdown says "539 m/s to spare". **Skip count** at 133.66 does not skip (still T-5.1 / T-2.9) (`015`, `017`, `023`). Flew fine for 1★ bronze, so the warnings are noise (`104`) |
| 238.3 / 255.9 | **Back to designer** ×2 | The first tap re-renders the report. The second does nothing (`105`, `109`) |
| 304.9 | Missions → Sandbox → Design first | Sandbox card says "No mission and no score" (`245`, `114`) |
| 362–376 | **Edge: minimum fuel** (Fuel load − ×20) | Floors at 2.9 t, Speed 96% shown red, LAUNCH still allowed. The panel jumps ~23 px when an error line appears (`119`) |
| 376 → 454 | Launch | **Structural breakup at T+6.98**, 35,052.5 Pa vs a 35,000 Pa limit. The screen says "**Silver for timing**" and "No stars" on a sandbox flight (`160`, `180`) |
| 468–481 | Leave and re-enter sandbox | **The sandbox design resets** to the default Hopper |
| 489–533 | **Edge: extreme mass.** Tap Heavy gas-generator / Gas-generator chips (4 attempts) | **No effect.** Undo stays disabled and no console warning appears (`248`) |
| 551–563 | Engines + | 3 → 4 → 10. At 4, a breakup warning pushed the panel up ~57 px, so the next 7 taps missed (`252`). At 10, LAUNCH greys out with "Too many engines for this width", which is a good guard (`253`) |
| 580 | **7 engines, 3.94× push** (designer predicts "bent in half at T+9") → LAUNCH | The countdown says "**2,396 m/s to spare**" and gives no breakup warning (`254`, `296`). **Breakup at T+9.45**, so the designer's prediction was accurate (`310`) |
| 643.52 | **Fix it** on the breakup result | **Soft-lock reproduced:** NRE at 643.71, UI gone, a **giant magenta sphere** on screen (stripped shader) (`325`, `326`) |
| 803 | Reload (local server restart was a harness issue, not the game) | |
| 841–907 | **Edge: odd staging.** Stack tab → "+ Stage on top" ×3, Side boosters "4" | **No effect** (`330`, `333`). Odd staging could not be tested |

## Ratings (1–5)

| Axis | Kid | Engineer | Why |
|---|---|---|---|
| Core loop | 2 | 3 | Kid: build → watch → read numbers, with almost no doing. Engineer: the designer predictions are honest (it said T+9, the rocket broke at T+9.45), but too many edits don't apply |
| First-minute hook | 3 | 3 | Great Home art and a big LAUNCH, but the first liftoff is ~70 s away behind a text briefing and an 18 s countdown |
| Control feel | 2 | 2 | 1–2 timed prompts per flight. The first tap on results is swallowed. Engineer: dead chips and buttons, layout jumps under the finger, no drag-scroll (SwiftShader fps lowers this score) |
| Difficulty curve | 1 | 3 | The KH-3 default "Ready" build breaks up after 2:45 of watching, and following the prompts doesn't save it. Engineer: clear failure physics, but the readiness signals contradict each other |
| Reward / progression | 3 | 2 | Kid: patches, stars, a new part per mission, the save persists. Engineer: the sandbox doesn't keep designs and the medal copy contradicts "no score" |
| Variety | 2 | 2 | KH-1/2/3 play the same way. The engineer could not change engine type, stages or boosters |
| Juice | 2 | 2 | Liftoff plume and shake are OK. Breakups have no visual (colliders stripped, magenta sphere), and the flight cuts at burnout with no apogee or coast. Needs device check |
| Friction / dead time | 1 | 1 | Soft-lock, countdown, passive ascent, 4 taps to relaunch, jargon-heavy results with 6 buttons |

## Top 15 candidate problems (ranked)

| # | Problem | Evidence |
|---|---|---|
| 1 | **Fix it after a Structural breakup throws an NRE and soft-locks the game**: the UI disappears and only a reload recovers. Reproduced 2 of 2 | kid t=976.18 → NRE 976.28, `kid/839`, `kid/851`; engineer t=643.52 → NRE 643.71, `engineer/325`, `326` |
| 2 | **Breakup visuals are broken in WebGL**: "Can't add component because class CapsuleCollider/SphereCollider doesn't exist" (physics module stripped), a magenta sphere appears (missing shader), and there is no breakup animation | console kid t≈803.7, engineer t≈614.68; `engineer/325`; `kid/640`→`641` |
| 3 | **KH-3's default "Ready… fuel to spare" build breaks up at T+165** after the kid followed both prompts. 2:45 of watching earns 0 stars | `kid/302`, `465`, `611`, `640`, `641` (compare open PRs #410/#413/#414 on KH-3) |
| 4 | **Sandbox Hangar chips and buttons never apply** (engine type, + Stage on top, side boosters), while +/- steppers do. Undo stays disabled and nothing is logged. *Could be harness-specific (mouse at low fps), so verify on device* | engineer t=489–533, 859–891; `engineer/248`, `330`, `333`; code `Kestrel.Slice/Runtime/Hangar/HangarPanel.cs` `BuildEngined`/`BuildStack` |
| 5 | **The first tap on the results screen is swallowed**: the report re-renders as "Stars saved. Kc was settled…" with empty charts and the buttons shift about 25 px. **Back to designer** did nothing on its second tap | `kid/040`→`041`; `engineer/104`→`105`→`109`; text at `Kestrel.Core/Flight/FlightDebrief.cs:541` |
| 6 | **Readiness signals contradict each other**: the designer says "Ready" while the countdown says "89 m/s short"; the full designer shows "4 things to fix" and the rocket flies fine; the designer predicts a T+9 breakup while the countdown says "2,396 m/s to spare" | `kid/007`, `013`; `engineer/008`, `015`, `104`, `254`, `296` |
| 7 | **"Fix it" is the primary red button even on a pass**, and tapping it looks like a no-op | `kid/041`, `044` (t=190.6, 225.4) |
| 8 | **The flight cuts to results around burnout** (T+33 on KH-1, T+40 on KH-2), so the player never sees apogee, coast or descent even though the copy promises "…and back" | `kid/026`→`027` |
| 9 | **Dead time**: ~18 s countdown, **Skip count doesn't skip**, and 30–165 s of passive "Auto-assist on" ascent | `engineer/017`, `023` (Skip tap t=133.66); kid timeline t=74.8→~93 |
| 10 | **Agency is just 1–2 timed prompts per flight** (~2.5 s window), and missing one costs a medal tier | `kid/024` (bronze vs silver on KH-2 with reflex taps) |
| 11 | **The results screen is jargon- and number-heavy**: "Reward 2000000 Kc… Bank 3995160.96 Kc", "Air load 83637.2 Pa past 83600.0 Pa", "Air pushed 35 kPa. The walls held 35 kPa"; chart labels clip; six buttons | `kid/027`, `641`; `engineer/180`, `310` (`Kc()` uses `0.##`) |
| 12 | **Sandbox contradictions**: the card says "No mission and no score" but results show "Silver for timing… Medals still count"; the sandbox design resets when you leave | `engineer/245`, `180`; engineer t=488.9 |
| 13 | **The designer layout jumps under the finger** when warning text appears (23–57 px), causing mis-taps; scroll position is inconsistent; mouse drag doesn't scroll the lists | `engineer/119`, `252`; engineer t=551–560 |
| 14 | **Navigation labels mislead**: "LAUNCH KH-1" / "Fly KH-2" open a briefing and then "Build it", so it takes 3–4 taps to fly | `kid/003`, `048`, `052` |
| 15 | **Wrapper and UI polish**: the web EXIT button overlaps the settings gear; desktop-grey UI Toolkit scrollbars; clipped titles ("KH-3 Tiny Orbi"); HUD jargon (Eng/Tnk/Fair, Q kPa); cost shows "$516.71M" vs Kc; exploded Parts view camera sits under the pad | `kid/001`, `003`, `300`, `013`; `engineer/002`, `008`, `013` |

## What worked

- The Home art and the big obvious LAUNCH.
- The campaign map with patches and stars.
- The save survives a reload.
- One new part unlocks per mission.
- The designer's breakup prediction was accurate.
- The "Too many engines for this width" guard.
- No external network calls.

## Caveats

- SwiftShader software rendering at ~5–20 fps. Game time tracked wall time, so the dead-time numbers hold, but control feel and juice scores are pessimistic.
- Input was mouse events, not touch.
- Not device-verified.
- Unity PlayMode was not used (CLI not signed in).
- The engineer persona's 15 minutes include ~2 min lost to a local static-server restart (harness only).
