# Release readiness (living checklist)

Shipping engine: **Unity** (iOS, **portrait only**). The Phaser build in `web/` is the beckify.com demo and is not release-gating.
Owner of this file: Kestrel QA. Merge sequencing: Kestrel Arcade. Merges to `main` and every App Store Connect, signing, privacy or spend action: Trevor's yes.

_Snapshot: 2026-10-28 9:30 PM EDT, `main` at `39cb8490` (#367). Update it in the same PR that changes an item, or when a PR in the table below lands._

## How to read this

| Status | Meaning |
|---|---|
| **verified** | Backed by a test that ran green in CI, or by captures / a device run. The Proof column says which. |
| **in PR** | The change exists only in an open PR. Not on `main`. The PR's CI state is in the open-PR table. |
| **unverified** | No test, capture or device run backs it. Includes "I read the code and it looks right". |
| **gap** | Known missing, wrong or open bug. Needs an owner decision. |

Two caveats apply to the whole file:

1. **Nothing here has been verified on a real iPhone or iPad.** CI renders on llvmpipe (no GPU), so "CI capture" means the layout and pixels are right in a software render, not that frame rate, heat, touch feel, audio routing or safe-area insets are right on a phone. Every row has a Device step in the script below; a row only becomes device-verified when Trevor records a pass there.
2. Unity CI runs on pull requests, not on pushes to `main` ([HANDOFF_GROK.md §4](HANDOFF_GROK.md)). "Verified (CI)" means green on the PR that merged it, plus the Core/Node jobs on `main`.

Owners are roles: **Arcade** (Kestrel Arcade, lead and merge owner), **Gameplay** (Gameplay Dev), **Graphics** (Graphics Dev), **Physics**, **Narrator** (Narrator Voice), **UX** (Kestrel UX), **QA** (Kestrel QA), **Trevor** (decisions, device, App Store Connect).

## Open PRs accounted for

CI state read at the snapshot time. "pending" = still running, not failed.

| PR | What | Draft | Base | CI at snapshot | Touches |
|---|---|---|---|---|---|
| [#371](https://github.com/becktj90/kestrel-heavy/pull/371) | Explosion never magenta, bounded size, visible on every failure path | no | main | Node, iOS Debug/Release, export compile-check pass; Core, EditMode, PlayMode, PlayModeDevice pending | Visual polish, Flight |
| [#372](https://github.com/becktj90/kestrel-heavy/pull/372) | Docs: KH-13 is the one starter with the guidance computer on | no | main | iOS Debug/Release pass; rest pending (docs only) | Flight and landing |
| [#373](https://github.com/becktj90/kestrel-heavy/pull/373) | Docs: fairing joint = stage diameter, flare allowed (spec) | no | #372's branch (stacked) | all pass | Visual polish, Designer |
| [#374](https://github.com/becktj90/kestrel-heavy/pull/374) | Corner character off by default, switch in Settings | **yes** | main | all pass; author did not run Unity locally | Navigation, Narration |
| [#375](https://github.com/becktj90/kestrel-heavy/pull/375) | Fairing joint = stage diameter, 15 degree shoulder (render only) | no | main | Node, iOS Debug/Release pass; Core, Unity jobs pending | Visual polish, Designer |
| [#376](https://github.com/becktj90/kestrel-heavy/pull/376) | Narrator accuracy follow-up: fact-gated takes, 20 true clips, verifier | no | main | all pass | Narration |
| [#377](https://github.com/becktj90/kestrel-heavy/pull/377) | Two-host color commentary, 25 clips, **inert until wired** | no | #376's branch (stacked) | all pass | Narration |

Merge-order constraints from the PR bodies: #373 needs #372 first (DECISIONS rows 70 and 71); #377 needs #376 first; #372 describes a test pin that already landed with #367.

## Navigation

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| N1 | Every route from a fresh start, a campaign session and a free build lands on a named screen and never throws; every screen and overlay has an exit | verified (CI) | Arcade | Core [`NavFlowTests`](../unity/Assets/Kestrel.Core.Tests/NavFlowTests.cs); PlayMode [`NavFlowPlayTests`](../unity/Assets/Kestrel.Capture.Tests/NavFlowPlayTests.cs) |
| N2 | Home to Missions to briefing to designer and back, no dead end; CONTINUE rides `home-primary` | verified (CI) | Arcade | `NavFlowPlayTests`, [`MenuPlayWalkTests`](../unity/Assets/Kestrel.Capture.Tests/MenuPlayWalkTests.cs), [NAV_BUGS.md](../NAV_BUGS.md) (NAV-03/04/05/07/09 fixed) |
| N3 | Every flight ends in a report with Retry, Back to designer, Missions and Home, including when no debrief was built | verified (CI) | Arcade | `NavFlowTests` (crash routes), [`ReportTests`](../unity/Assets/Kestrel.Game.Tests/ReportTests.cs), NAV-04/05 |
| N4 | Portrait only: Unity player allows `Portrait` only (no upside-down, no landscape) | verified (config + CI layout) | UX | [`ProjectSettings.asset`](../unity/ProjectSettings/ProjectSettings.asset) `allowedAutorotateToPortrait: 1`, others 0; [`PortraitLayoutTests`](../unity/Assets/Kestrel.Game.Tests/PortraitLayoutTests.cs), [`IosPortraitHomeDeviceTests`](../unity/Assets/Kestrel.Game.Tests/IosPortraitHomeDeviceTests.cs). Rotation on a real device: unverified (D1) |
| N5 | Text fits and nothing is cut off at 335, 390 and 430 pt wide (portrait) | verified (CI render) | UX | [`TextFitDeviceSweepTests`](../unity/Assets/Kestrel.Game.Tests/TextFitDeviceSweepTests.cs), [`HomeDeviceLayoutTests`](../unity/Assets/Kestrel.Game.Tests/HomeDeviceLayoutTests.cs), [`FlightHudReportLayoutDeviceTests`](../unity/Assets/Kestrel.Game.Tests/FlightHudReportLayoutDeviceTests.cs). Real safe areas (notch, Dynamic Island, home bar): unverified (D1) |
| N6 | Flight camera: the player's pick wins, labels match state, HUD touches never orbit | verified (CI) | Graphics | #364 (merged); [`FlightCameraPickerDeviceTests`](../unity/Assets/Kestrel.Game.Tests/FlightCameraPickerDeviceTests.cs). Pinch/drag feel: unverified (D5) |
| N7 | Settings: corner character switch, off by default | in PR | UX | [#374](https://github.com/becktj90/kestrel-heavy/pull/374) (draft, CI green, Unity not run by author). See gap G7 |
| N8 | **Known bug NAV-06:** report "Next mission" lands on the Missions list, not the next briefing | gap | Arcade | Pinned as Ignored `KnownBug_NAV06...` in `NavFlowTests`. Not a dead end |
| N9 | **Known bug NAV-08:** Escape/back in flight does not open Pause | gap (low on iPhone) | Arcade | Pinned as `KnownBug_NAV08...` in both Nav test files. On iPhone there is no Escape key, so the on-screen Pause is the only route. Device check D5 |
| N10 | **NAV-10:** CONTINUE shows when cleared > 0 with no draft and opens an auto-built starter | gap (label only) | Arcade | NAV_BUGS.md. Documented, not a dead end |
| N11 | Back-gesture / app-switch / lock-screen resume mid-flight does not strand the player | unverified | Arcade | No test. Device step D6 |

## Designer

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| D-1 | One designer screen with Kid / Builder / Engineer switch, Kid "Why?" cards, speed bar, X-ray | verified (CI) | UX | #366 (merged); [`DesignerV3UiTests`](../unity/Assets/Kestrel.Game.Tests/DesignerV3UiTests.cs), [`StarterTiersDeviceTests`](../unity/Assets/Kestrel.Game.Tests/StarterTiersDeviceTests.cs), [`DesignerV3CaptureTests`](../unity/Assets/Kestrel.Capture.Tests/DesignerV3CaptureTests.cs) |
| D-2 | All 20 campaign starters show Ready and are launchable (no gate-blocked mission) | verified (CI) | Gameplay | NAV_BUGS.md F1 "now 20/20 Ready" (#309); `NavFlowTests`; [`DesignerValidationTests`](../unity/Assets/Kestrel.Game.Tests/DesignerValidationTests.cs). Re-run after #367 changed the KH-13 starter: unverified |
| D-3 | Starter screen is honest: rebuilt rocket shows and is framed big; Ready means flyable | verified (CI) | UX | #352, #365; [`StarterDesignerDeviceTests`](../unity/Assets/Kestrel.Game.Tests/StarterDesignerDeviceTests.cs), [`StarterPreviewDeviceTests`](../unity/Assets/Kestrel.Game.Tests/StarterPreviewDeviceTests.cs) |
| D-4 | KH-1 designer frames the tower crown, base plates and pad circle beside the vehicle | verified (CI capture) | Graphics | #368 (merged); [`StarterKh1PadCaptureTests`](../unity/Assets/Kestrel.Game.Tests/StarterKh1PadCaptureTests.cs) |
| D-5 | Designer Launch hides the sheet, flies the design in the headless sim, jettisons the fairing | verified (CI) | Gameplay | #360 (merged); `DesignerDeviceTests`, [`DesignerSimFlight`](../tests/designer-simflight.test.cjs) (web port) |
| D-6 | Fairing joint equals top-stage diameter, 15 degree shoulder, no ledge (render only) | in PR | Graphics | [#375](https://github.com/becktj90/kestrel-heavy/pull/375): `FairingShoulderTests`, `FairingJointMeshTests`, captures `fairing-joint-*.png` (CI pending at snapshot). Spec: [#373](https://github.com/becktj90/kestrel-heavy/pull/373) (CI green) |
| D-7 | **Discrepancy audit:** 10 Blocker, 30 Major, 31 Minor designer x mission items, audited at `b8ad6b9` (Oct 19) | gap | Gameplay | [DESIGNER_MISSION_DISCREPANCIES.md](audits/DESIGNER_MISSION_DISCREPANCIES.md). #337, #343, #345, #309 and #367 landed after it. Not re-run since. Re-run `tools/audit/designer-missions/verdict/run.sh` and mark each Blocker |
| D-8 | **M-10 / [#369](https://github.com/becktj90/kestrel-heavy/issues/369):** starter predictor says landing needs about 0.7 km/s, flown return needs about 2.3 km/s; KH-1 count says "short" while the designer says Ready | gap | Gameplay | Open issue #369; HANDOFF_GROK.md backlog |
| D-9 | Designer feel on a touchscreen: drag, snap, X-ray, tier switch, 44 pt targets | unverified | UX | CI checks sizes in a software render only. Device step D3 |

## Flight and landing

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| F-1 | Boosters land on every landing mission with Auto-assist (KH-6, 7, 10, 13, 15, 19); a Core test per mission; batch grades the flown landing | verified (CI) | Gameplay | #367 (merged 9:21 PM EDT Oct 28); [`BoosterLandingMissionTests`](../unity/Assets/Kestrel.Core.Tests/BoosterLandingMissionTests.cs), [`HavenLandingGoldenTests`](../unity/Assets/Kestrel.Core.Tests/HavenLandingGoldenTests.cs). PlayModeDevice on #367 was still pending when it merged. Real-hand landing: unverified (D4) |
| F-2 | KID landing: booster stands up in the final approach even with no lit burn | verified (CI) | Gameplay | #362 (merged); `AeroGuidanceRecoveryTests` |
| F-3 | KH-13 is the one starter with the guidance computer on (everything else off) | in PR | Physics | [#372](https://github.com/becktj90/kestrel-heavy/pull/372) (docs, DECISIONS #70); code pin `GuidanceComputerTests` already on `main` via #367 |
| F-4 | Explosion never shows magenta, size bounded, fire + smoke + debris + ring on every failure path, flash <= 3 per second | in PR | Graphics | [#371](https://github.com/becktj90/kestrel-heavy/pull/371): `ExplosionBoundsTests` (fails on old main), `ExplosionFailurePathTests`, `ExplosionCaptureTests` (fails on magenta pixels). CI pending. Cause was read from code, **never reproduced on a device**; the fix is unverified on one (D4) |
| F-5 | Flight events in order, first-peak max-Q, guarded sinks | verified (CI) | Gameplay | #306 (merged); [FLIGHT_EVENTS.md](FLIGHT_EVENTS.md) |
| F-6 | Flight report opens at orbit insertion; booster still in the air finishes landing on Auto-assist | verified (CI) | Gameplay | #354 (merged) |
| F-7 | Fairing jettison and stage separation visible and in order | verified (CI capture) | Graphics | [`FairingSeparationDeviceTests`](../unity/Assets/Kestrel.Game.Tests/FairingSeparationDeviceTests.cs), `FairingSeparationCaptureTests`, [`FlightPhaseCaptureTests`](../unity/Assets/Kestrel.Capture.Tests/FlightPhaseCaptureTests.cs) (KH-3 to T+192) |
| F-8 | First-flight fairness: a new player's first flight (KH-1, a 10 km hop, ends T+25.7 s) is winnable | unverified | Gameplay | Web bots only (Phaser demo). No Unity test of an unaided first flight. Device step D2 |
| F-9 | Hand-flown (Engineer) landing is graded on the player's flight, not the autopilot's | gap | Gameplay | HANDOFF_GROK.md backlog: needs Trevor's sign-off |
| F-10 | Open sim issues: [#342](https://github.com/becktj90/kestrel-heavy/issues/342) booster misses Haven (still open; #367 should close it, confirm), [#333](https://github.com/becktj90/kestrel-heavy/issues/333), [#335](https://github.com/becktj90/kestrel-heavy/issues/335), [#326](https://github.com/becktj90/kestrel-heavy/issues/326) | gap | Gameplay | Triage which block release; none is a crash |

## Narration and audio

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| A-1 | Webcast clips carry fact tags; lint checks every clip | verified (CI) | Narrator | #350 (merged); [`narration-facts-lint.test.cjs`](../tests/narration-facts-lint.test.cjs), [`NarrationTests`](../unity/Assets/Kestrel.Game/Audio/Tests/NarrationTests.cs) |
| A-2 | Narrator only states facts true for the design flying (pad, recovery mode, stage positions); 20 new true takes; RTLS P1 from #350 fixed | in PR | Narrator | [#376](https://github.com/becktj90/kestrel-heavy/pull/376) (CI green): independent claim-vs-design verifier across all starters, templates, winners, presets on 4 pads. Known gaps listed in its commit notes (`cohost/entry.1` "nose first", `fairing.halves`) |
| A-3 | Two-host color commentary (25 clips) | in PR (inert) | Narrator | [#377](https://github.com/becktj90/kestrel-heavy/pull/377) (stacked on #376). Nothing plays these yet. Needs C# priority classes, a `kh.audio.commentary` setting and safe-window triggers (`docs/narration-webcast.md`). Sources S4 and S5 are marked CHECK. **Do not count as shipped** |
| A-4 | Narration license and voice terms | unverified | Trevor | [NARRATION_LICENSE.md](NARRATION_LICENSE.md): ElevenLabs paid Creator plan. Trevor to confirm the plan is active at release |
| A-5 | Subtitles, music, sound and voice switches exist in Settings | verified (CI) | UX | `UiIds.SettingsControls`, `NavFlowTests`, `TextFitDeviceSweepTests`. Whether subtitles cover every narrated line: unverified (listed under Accessibility in [APPSTORE_CONNECT.md §7](APPSTORE_CONNECT.md)) |
| A-6 | Audio on a phone: silent switch, headphones, call/Siri interruption, volume, music/voice ducking, no clipping | unverified | Narrator | CI has no audio device. Device step D7 |
| A-7 | Built Unity player contains the guide clips | verified (CI) | Narrator | [`tools/check-built-guide-clips.sh`](../tools/check-built-guide-clips.sh), `AudioImportTests` |
| A-8 | When the corner character is off (#374), Thrum's tips and spoken lines do not appear at all | gap | UX | #374 body ("the portrait is where they are drawn"). Product call: is onboarding still clear without them? |

## Visual polish

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| V-1 | Launch pad: lattice service tower, platforms, stairs and elevator, yellow crown, base plates, apron | verified (CI) | Graphics | #361, #363; `PadTowerTests`, `PadTowerCaptureTests`, `PadTowerDetailTests` |
| V-2 | Countdown "Below" framing: deck-standing hold, no sky hole | verified (CI capture) | Graphics | #348; `CountdownBelowDeviceTests`, `PadBelowFramingTests` |
| V-3 | Flight HUD: dock vs Cam/Pause, notes, health strip; HUD hidden on result | verified (CI) | Graphics | #346; `FlightHudPolishDeviceTests`, `FlightHudReportLayoutDeviceTests`, `HudLayoutTests` |
| V-4 | Explosion: no magenta error material on iOS (stripped shader variant) | in PR | Graphics | See F-4. [#371](https://github.com/becktj90/kestrel-heavy/pull/371) pins a `Resources` material. CI cannot show iOS shader stripping; **only a device build proves it** (D4) |
| V-5 | Fairing shape (no flat ledge on m1, m7, m10, stock, m20) | in PR | Graphics | See D-6 |
| V-6 | Plume, trench, pad ground, chase camera horizon | verified (CI) | Graphics | `PlumeShapeTests`, `TrenchPlumeTests`, `PadGroundTests`, `ChaseCameraHorizonTests` |
| V-7 | Photosensitivity: flashes bounded in area and rate; Safe-flash on by default | verified (CI) | Graphics | `FlashLimiter` via `ExplosionPlanTests`, `StagingFlashPlanTests`, `CameraFxTests`; DECISIONS #6. On a device with Reduce Motion on: unverified (D8) |
| V-8 | Visual review of the CI capture artifacts (`ui-captures`, `flight-phase-captures`, `fairing-joint-*`, `explosion-skinny-*`) by a person | unverified | QA | The PNGs exist per PR run; HANDOFF_GROK.md notes the T+116 and T+192 frames were never reviewed. **QA to open and sign off** |

## Performance

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| P-1 | Pad and effect budgets as numbers (particles, lights, draw counts) | verified (CI, unit level) | Graphics | [`PadPerf`](../unity/Assets/Kestrel.Game/Pad/Core/PadPerf.cs) checks in `PadTowerDetailTests`; `ExplosionBoundsFxTests` in #371 |
| P-2 | Adaptive quality governor steps down on sustained slow frames and stops at the floor | verified (CI) | Graphics | [`QualityGovernorTests`](../unity/Assets/Kestrel.Game.Tests/QualityGovernorTests.cs) |
| P-3 | Loop speed: results to next launch 0.45 s (web); 3 taps to relaunch after a crash; fixed 3 to 4 s wait before results | verified (web demo only) | QA | [LOOP_SPEED_AUDIT.md](LOOP_SPEED_AUDIT.md), [`qa/loop-speed.mjs`](../qa/loop-speed.mjs). **Not measured on Unity** |
| P-4 | **Frame rate, thermals and memory on a real iPhone** (older and newer model) over a full KH-3 flight and a landing | gap | Trevor + QA | Nothing recorded. Device step D9. The TestFlight text already asks testers for frame drops and heat |
| P-5 | Launch / cold-start time, build size, load stalls between Home, designer and flight | unverified | Arcade | No measurement. Device step D9 |
| P-6 | CI itself: PlayModeDevice takes 30 to 50 min of a 60 min limit | gap | Arcade | HANDOFF_GROK.md watch item. A timeout would block merges |

## Device test script for Trevor

Do this on a **real iPhone** from the latest TestFlight build that includes the PRs you want checked. Portrait only. About 15 minutes. Say "pass" or "fail + what you saw" per step; a screen recording of D4 and D9 helps most. Leave Low Power Mode off, volume up, silent switch off unless the step says otherwise.

Before you start, write down: build number, iPhone model, iOS version, and which PRs were merged into that build (the table above).

| Step | Do this | You should see | Covers |
|---|---|---|---|
| D1 | Install, open the app. Hold the phone upright, then turn it sideways and upside down. | The game stays upright. Title, buttons and text are fully on screen, nothing under the notch or the home bar. No spinning. | N4, N5 |
| D2 | From Home tap the rocket or **LAUNCH** to fly mission KH-1 with no tutorial help from you. | A briefing, then a launch. The 10 km hop finishes in about 26 seconds and a report card shows. First try should feel winnable. Note anything confusing. | F-8, N2 |
| D3 | From Home tap **DESIGN FIRST**. Drag one part, then switch Kid, Builder and Engineer. Tap a "Why?" card and X-ray. Tap **Launch**. | Parts move under your finger and snap. Tier switch changes detail on the same screen. Buttons are easy to hit. **Launch** is enabled (Ready) and starts a flight. | D-1 to D-4, D-9 |
| D4 | Fly KH-1 again with a tiny or broken rocket on purpose (remove an engine or most fuel), then **Launch** and let it fail. | A bounded fireball with smoke and debris. **No pink or magenta shape anywhere**, no full-screen flash. Then a report card with **Retry** and **Back to designer**. | F-4, V-4 |
| D5 | In any flight tap **Cam** and cycle shots; pinch and drag; tap **Pause**, then **Resume**. | The camera label matches what you see. Pinch and drag move the view. Pause stops the flight and Resume continues. HUD buttons do not rotate the camera. | N6, N9 |
| D6 | Mid-flight press the side button to lock, wait 10 s, unlock. Then swipe to another app and back. | The flight is paused or still running and the game is usable. No black screen, no frozen controls. | N11 |
| D7 | Turn the silent switch on, then off. Plug in headphones. Start a flight and let narration play. Take a call or ask Siri, then end it. | Music and voice play at a sane level and do not clip. Narration lines match what the rocket is really doing (right pad, right stage). Audio returns after the interruption. | A-2, A-6 |
| D8 | Settings (gear on Home): toggle **Music**, **Sound**, **Voice**, **Subtitles**, **Reduce motion**, **Show corner character** (if #374 is in the build). Then fly. | Every toggle works and survives closing the app. With Reduce motion on, no shake or strong flashes. With the corner character off, nothing pops in the corner. | N7, V-7, A-5, A-8 |
| D9 | Fly KH-3 (orbit, about 8.5 minutes of flight) with the screen recording on. Then fly a landing mission (KH-6) with Auto-assist. Feel the phone after. | Smooth motion with no long stutters, no overheating warning, no crash or restart. The booster lands on Haven. Note the model, how warm it got, and any stutter. | F-1, P-4, P-5 |
| D10 | Turn on **Airplane mode**, fully close the app, open it again, fly one mission and view the report. | Everything works. No error pop-ups, no "no connection" messages. | Privacy |
| D11 | Open **Game Center** (Settings app, Game Center) and check whether Kestrel Heavy has leaderboards. Fly and finish KH-1, then look in Game Center. | Today the Unity build is expected to **fail this**: see gap G1. Record what you see. | Game Center |

## App Store text and screenshots

Source of truth: [APPSTORE_CONNECT.md](APPSTORE_CONNECT.md) (written Oct 11). Limits are checked by `node tools/asc/check_limits.mjs` and `bundle exec fastlane dry_run`.

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| S-1 | Field lengths within Apple limits (name, subtitle, promo, keywords, description, review notes) | verified | QA | `node tools/asc/check_limits.mjs`; [`tests/asc-limits.test.cjs`](../tests/asc-limits.test.cjs) (Node CI) |
| S-2 | Description claims match the Unity build | gap | Arcade + Trevor | Doc says "Five KH flights" and "Simple / Standard / Engineer" layers. The game now has 20 campaign missions and Kid / Builder / Engineer (DECISIONS #69). Bullets marked [GATED] (narration, adaptive score, replay/daily, Game Center, debrief) must each be checked or removed. **Rewrite before submission** |
| S-3 | Screenshots: 6 portrait frames per size (iPhone 1320x2868 and 1206x2622, iPad 2064x2752), no alpha | gap | Graphics + QA | Not started. No `ScreenshotDirector`, no `tools/asc/check_screenshots`, no `fastlane/screenshots/`. The doc says screenshots are "redone at the end" (DECISIONS #14). Needs the Unity capture pipeline and a size/alpha checker |
| S-4 | Version and build number | unverified | Trevor | Doc says marketing 2.0.0, build >= 100, bundle `com.beckify.kestrelheavy`. Latest export is 2.0.0 (207); next is 208. The Unity project still has `com.beckify.kestrelheavy.unityproto` in `ProjectSettings.asset` (swap needs Trevor). Confirm which App Store Connect version record the build goes to |
| S-5 | Age rating 4+, not Made for Kids; accessibility labels declare only Dark Interface and Reduced Motion | unverified | Trevor | [AGE_RATING_RECOMMENDATION.md](AGE_RATING_RECOMMENDATION.md), DECISIONS #32 and #34. Reduced Motion is [GATED: QA]; run D8 first. Violence answer: re-check against the final explosion VFX (#371) |
| S-6 | TestFlight "what to test" text | verified (text) | QA | APPSTORE_CONNECT.md §11. Update to name the PRs in the build |
| S-7 | Trevor-only [TO CONFIRM] items (SKU, DSA trader status, review contact, agreements, URLs, China/Vietnam, feedback email) | unverified | Trevor | APPSTORE_CONNECT.md §13. QA cannot check these |

## Privacy and Game Center

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| G-1 | **Game Center in the Unity build:** the GameKit plug-in is not in `Packages/manifest.json`, so `GameCenterBridge` uses `NullGameCenter` (logs, returns false). The only board wired is the sandbox `kh.unityproto.meco_alt_m`, not `kh1` to `kh5` | gap (release blocker unless the claim is removed) | Arcade | [`GameCenterBridge.cs`](../unity/Assets/Kestrel.Slice/Runtime/GameCenterBridge.cs), [`AppleGameCenter.cs`](../unity/Assets/Kestrel.Slice/GameCenter/AppleGameCenter.cs) ("not compiled here. Verify on first open"), [unity/README.md §6](../unity/README.md). Either wire the plug-in and the real boards (and prove score parity with web) or drop the Game Center bullet and tick |
| G-2 | Game Center failures are harmless offline: auth and submit catch exceptions and return false | unverified (in Unity) | Arcade | Code reads right; no Unity test. The offline check exists only for the Phaser demo ([`qa/`](../qa/README.md)). Device step D10, D11 |
| G-3 | Unity Analytics, Ads, Purchasing, Cloud Diagnostics, Performance Reporting all off | verified (config) | Arcade | [`UnityConnectSettings.asset`](../unity/ProjectSettings/UnityConnectSettings.asset): all `m_Enabled: 0`. Packages: none for analytics, ads or crash reporting in `manifest.json`. Config only; not checked on a built player |
| G-4 | `submitAnalytics: 1` in `ProjectSettings.asset` | gap (needs an answer) | Arcade | Confirm what this flag sends on a shipped iOS build, or turn it off. It sits against the "Data Not Collected" label |
| G-5 | Privacy label "Data Not Collected" backed by a built-player check: inspect the exported Xcode project for analytics/ad frameworks and **run a proxy capture** of a full session | unverified | QA + Trevor | Required by APPSTORE_CONNECT.md §5; not run. Needs an `xcode-export` build to inspect. Do not claim the label until it passes |
| G-6 | Privacy manifest (`PrivacyInfo.xcprivacy`) for the **Unity** export: NSPrivacyTracking false, no collected types, required-reason APIs | unverified | Arcade | Only the legacy Phaser wrapper has one in-repo (`ios/KestrelHeavy/PrivacyInfo.xcprivacy`). Unity generates one at export; nothing in CI inspects it |
| G-7 | Export compliance `ITSAppUsesNonExemptEncryption = NO` and Game Center capability set by the Unity post-process | verified (code) | Arcade | [`IosXcodePostprocess.cs`](../unity/Assets/Kestrel.Slice/Editor/IosXcodePostprocess.cs). Result in the exported plist: unverified |
| G-8 | No network at runtime other than Game Center (web demo: no off-box requests) | unverified (Unity) | QA | Web demo harness: [`qa/`](../qa/README.md) checks off-box requests. Unity: needs G-5 and D10. Known web item: Google Fonts request fails the no-network check ([#73](https://github.com/becktj90/kestrel-heavy/issues/73)) |
| G-9 | Privacy policy page lists Kestrel Heavy and says no data is collected | unverified | Trevor | https://beckify.com/privacy is [TO CONFIRM] in APPSTORE_CONNECT.md. Legal text is Trevor's call |

## Gaps that need a decision before release

Highest first. "Blocker" = would make a store claim false, or cannot be tested without a person.

1. **G-1 Game Center is a stub on Unity main** (blocker for the Game Center claim): wire it or remove the claim.
2. **P-4, D-series: no real-device run exists.** Trevor runs the script above, or names another tester.
3. **V-4 / F-4: the magenta explosion fix** (#371) is the only open fix for something Trevor actually saw on a device. CI was pending at the snapshot; after merge, a TestFlight build must be checked at D4.
4. **S-3 screenshots not started** and **S-2 listing text stale.**
5. **G-5 / G-4 / G-6 privacy proof** not run; the label must not be claimed until it is.
6. **F-8 first-flight fairness** has no Unity-side evidence.
7. **A-8 / N7** (#374): decide whether onboarding works with the corner character off.
8. **D-7 / D-8**: designer discrepancy audit and the starter-vs-flown landing disagreement (#369) need a triage pass.
9. **A-3** (#377) is inert; keep it out of the release notes until it is wired.

## Out of scope

- Cube Squad: separate repository and checklist.
- Phaser web demo (`web/`): its QA findings are issues [#66](https://github.com/becktj90/kestrel-heavy/issues/66) to [#74](https://github.com/becktj90/kestrel-heavy/issues/74). They do not gate the Unity release.
- Landscape: not supported on iPhone or iPad. The harness keeps landscape presets for the web demo only.

## Update log

| Date (EDT) | Change | By |
|---|---|---|
| 2026-10-28 | First version. Read #371 to #377 and `main` at `39cb8490`. | QA |
