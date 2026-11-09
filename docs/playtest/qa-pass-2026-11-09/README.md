# Kestrel Heavy QA pass — 2026-11-09

**Head.** `main` `b2bd87b31b8a` (#468).  
**Caveat: not device-verified.** Evidence is headless Core.Tests / campaign-search, #468's green Unity CI (EditMode + PlayMode + PlayModeDevice), and a WebGL bot play-through in software-rendered Chrome. No Unity CLI PlayMode on this box (Unity auth not signed in). No touch, no sound, no phone.

## A) Shipped builds vs main

| Item | Value |
|---|---|
| Current main | `b2bd87b31b8a` |
| Prior export | `xcode-export` `fb8200d` — 2.0.0 **(214)** from main `2f85c027` (34 commits behind) |
| New export | Actions run [42624041284](https://github.com/becktj90/kestrel-heavy/actions/runs/42624041284) — 2.0.0 **(215)** |
| Export branch | `xcode-export` `bb4ead95789b` — from main `b2bd87b31b8a` |
| Xcode Cloud | **#51** auto-started (`GIT_REF_CHANGE`), **SUCCEEDED**, source `bb4ead95789b` |
| TestFlight | Build **51** `VALID`, `IN_BETA_TESTING` (uploaded 2026-11-09 ~08:33 ET) |
| Match? | **Yes** — TF 51 = Cloud 51 = export 215 = current main |

Prior TF tip was build 50 (= export 214 / Cloud 50 from Nov 6).

## B) Campaign KH-1…KH-13 (headless)

### Core.Tests (`CampaignFlightLoop` + `NavFlow` + `CampaignTests`)
- **42 passed, 1 skipped** (was KnownBug NAV-08; fixed in #472).
- Board starters (seed 0): KH-1…3,5–7,9–15,17–20 **pass**; KH-4 / KH-8 / KH-16 are intentional near-miss starters that need Fix it / strength (covered by `BoardStarter_FixItOrOneStrengthEdit_*`).
- Log: `docs/playtest/qa-pass-2026-11-09/campaign-dotnet.log`.

### campaign-search `report` (board / fix / strength / fly)
Every KH-1…KH-13 row **pass** on the board path:

```
KH-1 board pass … KH-3 fly pass … KH-4 fix pass … KH-5 fly pass …
KH-8 strength pass … KH-12 fly pass … KH-13 fly pass
```

Full log: `docs/playtest/qa-pass-2026-11-09/campaign-search-report.log`.

### Unity PlayMode / NavFlow
- Local Unity CLI: **not signed in** on this box → PlayMode not run here.
- CI on the PR that became this main (#468): EditMode, PlayMode, PlayModeDevice all **SUCCESS**.

## C) WebGL bot play-through

| | |
|---|---|
| Build | WebGL CI artifact run `42541585757` @ `b2bd87b3` |
| Host | Local static server with brotli `.unityweb` |
| Viewport | 390×844, mouse, SwiftShader |
| Persona | Kid-style: Home → briefing → Build it → LAUNCH → prompts → results |

### What worked
- Home → KH-1 briefing → Starter → countdown → flight → **MISSION COMPLETE** (1★, apogee 10 km, +2.0M Kc, unlocks fins/nose).
- Results exits present: Next mission, One more launch, Back to designer, Missions, Home.
- **No NullReferenceException** and **no Fix-it soft-lock** on this pass path (P0s from the Nov 3 fun review stay closed).
- No CapsuleCollider / magenta console spam on this KH-1 pass.
- Shots: `docs/playtest/qa-pass-2026-11-09/shots/`.

### Real bug filed
| Issue | Title | Evidence |
|---|---|---|
| [#473](https://github.com/becktj90/kestrel-heavy/issues/473) | KH-1 starter: designer Ready vs countdown "89 m/s short" | `shots/03-designer-ready.png` vs `shots/04-countdown-short.png`; flight still passes |

### Clear fix landed as PR
| PR | Fix | Notes |
|---|---|---|
| [#472](https://github.com/becktj90/kestrel-heavy/pull/472) | NAV-08 Escape opens pause in flight | Wires `MenuBack.FlightEscape`; awaiting CI green then squash-merge |

### Harness / environment noise (not filed)
- `favicon.ico` / `TemplateData/style.css` 404 from the local static host.
- `screen.orientation.lock()` NotSupportedError in desktop Chrome.
- FS.syncfs “operations in flight” warnings from the Unity loader.
- Bot heuristic missed the green **Next mission** button for a long stretch (dark navy results card); not a game bug.

## D) Still open / not claimed

- Full interactive WebGL run of KH-2…KH-13 in one session (bot time); headless campaign already covers board viability.
- Forced-breakup → Fix it soft-lock regression on WebGL (pass path only exercised here).
- Local Unity PlayMode (needs CLI sign-in).
- **No device claims.**

## Scorecard (headless / WebGL only)

| Area | Result |
|---|---|
| Campaign board KH-1…13 | Pass (headless) |
| NavFlow Core routes | Pass |
| Soft-lock / NRE on KH-1 pass | Not seen |
| Readiness copy consistency | **Fail** → #473 |
| Escape → pause | Fix in #472 |
| Export / TestFlight = main | **Pass** (build 215 / Cloud 51 / TF 51) |
