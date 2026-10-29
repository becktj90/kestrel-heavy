# Kestrel Heavy: App Store listing text and screenshot plan (Unity 2.0.0)

> **Draft for Trevor. Nothing is entered in App Store Connect. Price is not set here ($0.99 is blocked on the Paid Applications bank agreement). Trevor alone submits to App Review.**

**Last verified against main at commit 39cb8490c45d61fc87e5ad3479a80a95e81b18ed (39cb8490) on 2026-10-28 21:45 ET (UTC-4).** Latest main commit: "fix(recovery): boosters land on every landing mission; batch grades the flown landing (#367)". Previous check: 39054b8da731889f24d015f4affdb67611cf9441 (2026-10-28 19:20 ET).

Docs only. Character counts below come from a script (`len()` for text fields, UTF-8 bytes for keywords and review notes, as Apple counts them). Facts used: portrait only, bundle `com.beckify.kestrelheavy`, version 2.0.0, first Unity release. The Phaser build is a web demo only and is not described as the product.

---

## 1. Verified-merged evidence

Source of truth: the default branch `main` at the commit above, read with `gh` (read-only). Merged PRs are those with `merged_at` set. Open or draft PRs are not counted. Re-check on the latest main found one PR merged since the previous check (#367, 2026-10-28 21:21 ET); every claim below was re-read on this commit, and #367 touched only recovery guidance, the KH-13 starter data, and tests, so no claim changed. `docs/LAUNCH_PLAN` does not exist on main; the docs read were README, DECISIONS (#1–#69), PROJECT_STATE, STANDING_BRIEF, HANDOFF_GROK, UNITY_SHIP_PLAN, APPSTORE_CONNECT, APPSTORE_REQUIREMENTS, DESIGNER_TIERS_SPEC, PHASED_DESIGNER_PLAN, MUSIC_LICENSES, NARRATION_LICENSE, plus `unity/` code and `data/*.json`.

| Claimable feature (Unity app) | Proof on main |
|---|---|
| Unity is the shipping app, Phaser is web demo only | `README.md`; DECISIONS #1, #30, #35; `unity/ProjectSettings/ProjectSettings.asset` (`bundleVersion: 2.0.0`) |
| Portrait only; iPhone + iPad; full screen on iPad; iOS 16+ | ProjectSettings: only `allowedAutorotateToPortrait: 1`, `targetDevice: 2` (iPhone and iPad), `uIRequiresFullScreen: 1`, `iOSTargetOSVersionString: 16.0`; PRs #139, #164 |
| Designer is the main screen: pick a mission, start from a rocket built for it, change engine fuel (kerosene/methane), stages, fuel (5 notches), boosters where allowed | PR #301 (Starter screen); `Kestrel.Core/Designer/StarterScreen.cs`; `docs/plans/PHASED_DESIGNER_PLAN.md`; `data/features.json` (`"tier": "starter"`) |
| Readiness check that flies the predictor and names the cause (max-Q break-up, over-g, tumble, short fuel, cargo does not fit) | PR #365; `Kestrel.Core/Designer/StarterVerdict.cs`; `data/starter-readiness.json` |
| Speed-to-orbit bar, "?" Why? cards, X-ray cutaway | PR #366; `Kestrel.Core/Designer/DesignerTiers.cs`, `StarterXray.cs`; `data/designer-tiers.json` |
| "Show full designer" switch in Settings opens the full workshop (hangar, v3 panel, stepper, templates) | `Kestrel.Game/Shell/MainMenu/MainMenuOverlay.cs` (Settings row, `FullDesignerNote`); `Kestrel.Core/Features/FeatureFlags.cs` (`ShowFullDesigner` turns every `designer.*` flag on); `StarterScreen.UseStarter`; PRs #176, #213, #214 |
| Part catalogs: 24 engines, 7 propellants, 7 tanks, 6 fairings, 5 fins, 6 materials, 5 interstages, 16 liveries, 12 finishes | `data/engines.json` … `data/finishes.json` (counted at this commit) |
| 20 missions KH-1 to KH-20, each with a fictional payload, each with a verified winning rocket | `data/campaign.json` (20), `data/golden/campaign-winners.json`; PRs #258, #270 |
| Embroidered mission patch per KH mission | PRs #260, #276; `Resources/MissionBadges/kh-1…kh-20.png` (20 files); `Kestrel.Game/Campaign/PatchCollectionScreen.cs` |
| Mission briefing, campaign map, post-flight report (cause, stars), Back to designer reopens your rocket | PRs #235, #236, #237, #261, #311 |
| Full launch sequence: countdown, liftoff, max-Q, staging, fairing separation, cutoff | PRs #181, #229, #246, #307, #334 |
| In-flight camera picker (player's choice wins) | PRs #304, #364; `Kestrel.Core/Flight/FlightCameraControl.cs` |
| Day, sunset and night launches | PRs #271, #272, #275; `Kestrel.Game/Environment/Runtime/TimeOfDayRig.cs` |
| Detailed procedural pad: lattice service tower, stairs, planked decks, yellow crown, base plates, pad circle, flame trench, umbilicals | PRs #105, #135, #152, #154, #361, #363, #368 |
| First stage turns around and tries a landing on Haven on recovery missions | PRs #249, #290, #331; `Kestrel.Core/Flight/BoosterLanding.cs`, `Kestrel.Slice/Runtime/RecoveryShow.cs` (see caution C3: only "tries") |
| Mission control and webcast narration, AI-generated voices | PRs #185, #218, #350; `Resources/Narration` (116 mission-control, 19 host, 12 co-host clips, 147 mp3 files); `docs/NARRATION_LICENSE.md` (ElevenLabs, Trevor's paid Creator plan) |
| Music in the hangar, title and flight | PRs #211, #224, #238; `MUSIC_LICENSES.md` |
| Codex: 8 short lessons (delta-v, thrust-to-weight, Isp, staging, max-Q, center of mass/pressure, orbits, booster landing) | `data/codex.json`; `Kestrel.Game/Shell/CodexScreen.cs`; `ScreenStub.cs` wiring; Settings Codex row (`UiIds.SettingsCodex`) |
| Progress saves on device; CONTINUE on Home; credits wallet and draft autosave | PR #292; `Kestrel.Game/Shell/HomeResumeBar.cs`; `docs/ECONOMY.md` |
| Settings: music, sound, voice, subtitles, Reduce Motion | `Kestrel.Core/Ui/UiIds.cs` (`SettingsMusic … SettingsReduceMotion`) |
| No ads, no IAP, no accounts, no analytics | DECISIONS #25, #34; `ProjectSettings/UnityConnectSettings.asset` (every `m_Enabled: 0`); `Packages/manifest.json` has no ads/analytics/IAP package; only `UnityWebRequest` use is reading bundled audio clips |
| Unity CI on the PRs behind the claims | #368 head: EditMode, PlayMode, PlayModeDevice, iOS export compile-check, Debug and Release iOS builds all pass. #367 (latest merge): EditMode, PlayMode, compile-check and both iOS builds pass, but its PlayModeDevice job still showed pending when checked, so #367 does not count as green (Unity CI does not run on pushes to main, only on PRs) |

### Not claimed (and why)

- **Game Center and leaderboards.** `AppleGameCenter.cs` compiles only if Apple's plug-in is installed (`KH_GAMEKIT`), the plug-in is not in `Packages/manifest.json`, the file "has never been compiled" (UNITY_SHIP_PLAN §risks), and the only board it references is a prototype id (`kh.unityproto.meco_alt_m`). The kh1–kh5 boards are from the old Phaser build.
- **"KID mode" and KID landing (PR #362).** Merged, but it changes `web/js/*` only. Its own PR text says "the Unity shipping app has no KID mode". Unity's "Kid" is the designer tier name, not a difficulty.
- **Builder and Engineer designer tiers, the tier switch, "Standard unlocks after the first orbit".** Flags `designer.tierBuilder` and `designer.tierEngineer` are false in `data/features.json`; PR #366 says only Kid ships and players see no switch.
- **Daily, weekly or challenge-lab modes, replays, Photo Mode, sandbox as a mode.** Described in older docs; no Unity screen or code found (a Sandbox settings toggle exists, but what it does is unclear).
- **"Original score", "adaptive score".** The flight bed `kestrel-flight.ogg` is AI-generated (Gemini, Trevor's paid plan); title and designer beds are original. Beds crossfade; nothing adaptive was verified.
- **"Honest physics", "real numbers", accuracy, framerate, 120 Hz, ProMotion, battery or heat claims.** The repo has an accuracy review with known gaps (point-mass gravity, planar flights, no thermosphere above 86 km) and no device performance measurement. I claim only that the flight model includes thrust, drag, gravity and staging, and I do not even use those words in the final text.
- **"Land the booster" as a hero claim.** The attempt exists, but on Oct 25 Trevor reported he could not land one. The follow-up fix, PR #367, merged to main on 2026-10-28 21:21 ET, but its Unity PlayModeDevice check was still pending, and its own PR text lists the on-device look of the new entry and tail-first phases as not verified. It also ships the KH-13 starter with the guidance computer on, which the PR leaves for Trevor to decide. So the text stays at "tries to land" until a green PlayModeDevice run and a device check.
- **Contracts after KH-20, credits shop, corner guide character, safe-flash default, VoiceOver.** Data or partial code exists, but I could not confirm a shipped, default-on player path. The corner guide character is off by default on main (its flag in `data/features.json` is false; DECISIONS #72 and PR #374 "corner character off by default", still a draft, cover the Settings switch), and the listing text does not mention it.
- **Only in open or draft PRs (checked on this commit):** #371 (explosions never magenta), #372/#373/#375 (KH-13 guidance, fairing shape), #374 (draft), #376/#377 (narrator two-host commentary). All were still open.
- **Merged but held back:** #367 (boosters land on every landing mission), for the reason in the booster bullet above.
- **Web-only:** anything in `web/` (Phaser): KID difficulty, the old five-flight Pier 7 game, `docs/KESTREL_APP_STORE.md` (marked ARCHIVED).

---

## 2. Final text

Counts (script-checked after the last edit): Name 13/30 · Subtitle 28/30 · Promotional text 135/170 · Description 2115/4000 · Keywords 94/100 bytes · What's New 475/4000 · What's New template 293/4000 · Review notes 802/4000 bytes.
Checks run: no "free", no price or pricing words, no award/rating/press claims, no real agency or vehicle names, no Game Center, no performance or device claims, keywords have no spaces, no repeated words, and none repeat a word from the name or subtitle (rocket/launch/design/land are left out on purpose).

### Name (13/30)
```text
Kestrel Heavy
```

### Subtitle (28/30)
```text
Design, launch, land rockets
```

### Promotional text (135/170)
```text
Pick a mission, tune your rocket, then light it up. 20 KH missions, a built-in Codex and an X-ray cutaway. No ads, no in-app purchases.
```

### Keywords (94/100 bytes)
```text
orbit,space,booster,simulator,engineering,stem,science,builder,spaceflight,kids,missions,max-q
```

### Description (2115/4000)
```text
Kestrel Heavy is a rocket design and launch game for curious kids and working engineers.

Every flight starts in the designer. Pick a mission and you begin with a rocket built to try it. Then make it yours.

THE DESIGNER
• Choose the engine fuel (kerosene or methane), the number of stages, how much fuel to carry, and side boosters on missions that allow them
• A readiness check flies your rocket in the game's predictor and tells you in plain words when it expects trouble, like shaking apart in max-Q or running short of fuel
• See the speed you need to reach orbit next to the speed you have
• Tap the ? on any row for a short "Why?" card
• Open the X-ray cutaway to see the tanks, engines, fairing and cargo
• Engineers: switch on Show full designer in Settings for the full parts workshop, with engines, propellants, tanks, fairings, fins, materials, interstages and liveries

THE LAUNCH
• Watch the whole flight: countdown, liftoff, max-Q, staging, fairing separation and engine cutoff
• Mission control and webcast-style commentary call the flight. The voices are AI-generated
• Pick your own camera angle while you fly
• Day, sunset and night launches from a detailed pad with a lattice service tower, stairs and planked decks
• On recovery missions the first stage turns around and tries to land on Haven, our recovery barge
• When a flight falls short, the post-flight report shows the real cause and your stars, and takes you back to your rocket in the designer

THE MISSIONS
• 20 missions, KH-1 to KH-20, each with its own fictional payload and an embroidered mission patch
• A Codex of short lessons on delta-v, thrust-to-weight, specific impulse, staging, max-Q, center of mass, orbits and booster landing
• Your progress saves on your device, and Continue picks up where you left off
• Settings for music, sound, voice, subtitles and Reduce Motion

THE FINE PRINT
• No ads. No in-app purchases. No accounts. No internet connection needed
• Plays on iPhone and iPad, in portrait

Kestrel Heavy, Haven and the KH missions are original fiction. Not affiliated with any launch provider or space agency.
```

### What's New, first Unity release 2.0.0 (475/4000)
Note: ASC does not show a What's New field for an app's first version (APPSTORE_CONNECT §3). This text is ready for whichever release first shows the field, or can be dropped.
```text
Welcome aboard. Kestrel Heavy 2.0 is the first release of the rebuilt game.

• Design in the new Starter designer: pick a mission, change engine fuel, stages, fuel and boosters, and see the readiness check
• Tap ? for "Why?" cards and open the X-ray cutaway
• 20 KH missions with mission patches, a Codex, and a post-flight report
• Full launch sequence with mission control and webcast-style commentary, day, sunset and night launches, and a detailed pad

Thanks for flying.
```

### What's New template for later updates (293/4000)
```text
What's new in 2.0.x   (4000 chars max; one line per change; only changes that are merged AND in this build)

• New: <one player-visible feature, in the words a player would use>
• Better: <one thing that now works or looks better>
• Fixed: <one bug a player could have hit>

Thanks for flying.
```

### Suggested App Review notes (802/4000 bytes)
```text
Kestrel Heavy needs no account or sign-in and works offline. To test: on Home tap CONTINUE or LAUNCH, read the mission briefing, and on the Starter designer screen change engine fuel, stages, fuel or boosters, then tap LAUNCH. The X-ray button on the rocket preview opens a cutaway, and the ? buttons open short "Why?" cards. Settings (gear on Home) has music, sound, voice, subtitles and Reduce Motion toggles, a Codex row, and a Show full designer switch that opens the full parts designer. No in-app purchases, no ads, no analytics, no accounts. Mission control and webcast narration are AI-generated voice clips bundled in the app (no network). Music is bundled. The game is fiction: Kestrel Heavy, Haven and the KH missions are original and not affiliated with any launch provider or space agency.
```

### App Review and privacy cautions (for Trevor)

- **C1. Game Center entitlement without the feature.** `IosXcodePostprocess.cs` adds the Game Center capability and the export-compliance comment cites Game Center HTTPS, but no working Game Center code ships (see Not claimed). Remove the capability or ship the feature, then match the ASC Game Center tick and the leaderboard sheets in `docs/APPSTORE_CONNECT.md` §10. The draft review notes in that doc mention leaderboards kh1–kh5 and should not be used.
- **C2. The existing ASC draft (`docs/APPSTORE_CONNECT.md`) over-claims for the current build.** It lists three design layers, Standard unlocking after first orbit, daily challenge, replays, an adaptive score, Game Center, "safe-flash on by default" and "honest physics you can trust". Each is gated or unverified; use the text above instead. Its subtitle "Design, launch, land rockets" and its core pitch still fit.
- **C3. Booster landing.** Keep "tries to land". If the final build lets the player land reliably, strengthen the line after a device check. PR #367 merged after the last check but is not yet green on PlayModeDevice or checked on device (see Not claimed).
- **C4. AI-generated audio.** Narration is ElevenLabs on Trevor's paid plan; the flight music bed is Gemini output. I wrote "The voices are AI-generated" in the description. Trevor should decide whether to keep it and how to answer ASC's content-rights question; the ledgers (`NARRATION_LICENSE.md`, `MUSIC_LICENSES.md`) say the licences allow commercial use in an app. No "original music" claim is made.
- **C5. Privacy policy page is out of date.** https://beckify.com/privacy (fetched 2026-10-28) describes Kestrel Heavy as a "Phaser 4 Pier 7 arcade in a native SwiftUI shell", and it is a Beckify Toolbox policy. It must describe the Unity game (no collection, local saves, no analytics) before submission. That is Trevor's legal and site decision.
- **C6. Age rating.** Draft is 4+ (DECISIONS #34). The flight has layered explosions, debris and breakup VFX (#274, #277, #279), and KH-8 "Crew Day" carries a crew capsule. The questionnaire note says "confirm against final failure VFX"; confirm no crew harm is shown, or answer "infrequent cartoon/fantasy violence".
- **C7. iPad.** `uIRequiresFullScreen` is on and `targetDevice` is iPhone + iPad, so iPad 13-inch screenshots are mandatory and "Plays on iPhone and iPad" appears in the description. DECISIONS #7 says Trevor confirms this change to the listing. If iPad is not wanted, drop that line and the iPad shots.
- **C8. Name.** DECISIONS #29/#58 keep "Kestrel Heavy" provisionally pending a counsel check (not legal advice); fallback title "Updraft Heavy". The description names no real vehicle or agency.
- **C9. Marketing URL** `https://beckify.com/games/kestrel-heavy` is the Phaser web demo, which is a different game from the Unity app (older flights, KID mode, Game Center). Consider pointing the Marketing URL elsewhere or labeling it a demo.
- **C10. Accessibility labels.** Declare only what works end to end. Reduce Motion and the subtitles toggle exist; VoiceOver, Larger Text and Captions are not verified and are not claimed.
- **C11. Open [TO CONFIRM] items** from `APPSTORE_CONNECT.md` §13 (SKU, DSA trader status, support URL contact info, China mainland and Vietnam availability) are outside this task and unverified.
- **C12. Fonts.** PR #223 added SIL OFL fonts and noted the OFL exception to the "PD/CC0 only" rule needs Trevor's approval; confirm it was approved.

---

## 3. Screenshot plan

**Specs checked 2026-10-28 (ET)** at https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications. 1 to 10 screenshots per size, `.png/.jpg/.jpeg`, **no alpha or transparency**. If the UI is consistent across sizes, only the highest required resolution is needed and ASC scales down.

| Slot | Portrait pixel sizes Apple lists | Required? |
|---|---|---|
| iPhone 6.9" (Dynamic Island, large: 16/17 Pro Max, Air, 18 Pro Max) | **1320 × 2868** (also 1290 × 2796, 1260 × 2736) | Optional if the medium size below is provided (fallback chain: large uses scaled 6.5" Face ID shots if absent) |
| iPhone 6.5" (Face ID, large: 11 Pro Max, XS Max, XR, 12/13 Pro Max, 14 Plus) | **1284 × 2778** or **1242 × 2688** | Required only if no Dynamic Island large shots are given |
| iPhone 6.3" (Dynamic Island, medium: 14 Pro, 15, 16, 17, 18 Pro) | **1206 × 2622** or 1179 × 2556 | **Required: at least one** |
| iPad 13" (iPad Pro M4/M5, Air M2–M4) | **2064 × 2752** or 2048 × 2732 | **Required** because the app runs on iPad |

Recommendation: produce one set at **1320 × 2868** (6.9") and one at **1206 × 2622** (6.3"), plus **2064 × 2752** for iPad 13". Add 1284 × 2778 (6.5") only if you want to avoid scaling. The repo's own `docs/APPSTORE_REQUIREMENTS.md` §2 reached the same conclusion on 2026-10-11, and Apple's page still says that.

**Dates and project rules.** Screenshots are redone at the end of the project (DECISIONS #14), so everything below is a plan against main at 39cb8490. Real game captures only: no mockups, no invented UI. Captions are a short text band added above the untouched capture; they only repeat claims in section 1. No real logos or agency names in any frame (the pad and hangar are generic and fictional).

**Capture routes that work without a Mac or Xcode**
1. **TestFlight build on Trevor's iPhone** (and an iPad if he has one): press side button + volume up, then move the images off the phone via Photos → Files, iCloud.com or email. Real device, real shaders. The native pixel size depends on the phone model, which I do not know: it must match a size in the table (for example, iPhone 17 Pro Max = 1320 × 2868, iPhone 16/17 = 1206 × 2622). Needs a build newer than export 2.0.0 (207), because main has moved a lot since Oct 22 (including the booster-landing rework in #367).
2. **Unity CI capture job on GitHub Actions** (GameCI on Ubuntu, software rendering): real Unity renders written as PNG artifacts at an exact pixel size. The repo already has this: `Kestrel.Capture.Tests/ScreenCaptureTests.cs` (1179 × 2556), `DesignerV3CaptureTests.cs` (1320 × 2868 and 2064 × 2752), plus `CountdownCaptureTests`, `HomeCaptureTests`, `FlightPhaseCaptureTests`, and the Starter and X-ray device captures. Caveats: the Starter, X-ray, countdown and flight-phase captures write at phone **point** sizes (375 × 667, 430 × 932, 820 × 1180), not ASC pixel sizes, so a new capture job (or a changed viewport) at 1320 × 2868, 1206 × 2622 and 2064 × 2752 is needed. The `ScreenshotDirector` in `APPSTORE_CONNECT.md` §9 does not exist on main. Software rendering can differ from device (llvmpipe, no real GPU); compare one frame against a phone shot before trusting it. Countdown capture alone takes about 19 minutes in CI.
3. **Unity editor Game view at exact resolution** needs a machine with the Unity editor. Trevor has no Mac, so this works only on a Windows or Linux box with the editor, or in CI (route 2).
4. **WebGL build:** not recommended. It is the same game on a different layout and quality path (`WebPanelScale`, web layout); a store screenshot must show the iOS app.

Then composite captions with ImageMagick (Linux), check size and no-alpha, and upload only through Trevor's approval. Nothing here uploads.

### Shot list (portrait; upload order = order below)

| # | Capture | Set-up | Caption (short, factual) | Source | Status |
|---|---|---|---|---|---|
| 1 | The designer (Starter screen) on KH-3 "Tiny Orbit": rocket preview, Engine fuel / Stages / Fuel / Boosters rows, readiness badge, speed-to-orbit bar, LAUNCH | New save, clear KH-1 and KH-2 so KH-3 is open; open KH-3 briefing then designer; leave the default winner rocket; badge reads Ready | Pick a mission. Shape the rocket. | Device (route 1) or CI capture (route 2) | OK to capture (PR #301, #365, #366) |
| 2 | Same screen with one "?" Why? card open (for example Stages) | As #1, tap ? on the Stages row | Tap ? to learn why. | Device or CI | OK (#366). Note the preview shrinks while a card is open (PR text), so check framing |
| 3 | X-ray cutaway: tanks, engines, fairing and cargo, flight-plan strip | As #1, tap X-ray on the preview | See inside your rocket. | Device or CI | OK (#366); Painter2D pictograms were "unchecked in Unity" at merge, so look closely |
| 4 | Countdown on the pad at dusk or night, Below camera, tower with yellow crown in frame | KH-3, wait for the held count; time of day is picked per mission, so check which one KH-3 gets or choose a mission that gets sunset or night | Light it up. | Device or CI (`CountdownCaptureTests`) | OK, but pad art is recent (#361, #363, #368); capture from a build that includes them |
| 5 | Ascent HUD around max-Q or staging with the rocket in view | KH-3, Auto-assist, capture near T+116 (CI flight-phase test flies KH-3 to T+192) | Watch max-Q, staging and cutoff. | Device or CI (`FlightPhaseCaptureTests`) | OK |
| 6 | Booster descending to Haven, deck and rocket in view | A recovery mission such as KH-6 (return to pad) or KH-7 (Haven barge) with Auto-assist | Bring the booster home. | Device only for honesty | **Cannot be captured honestly yet.** Trevor could not land a booster on Oct 25. The fix (#367) is merged but not green on PlayModeDevice and not checked on a device. Capture only a frame that really happened, and only after a device check on a build that includes #367 |
| 7 | Post-flight report after a miss or a clear: cause, stars | Fly KH-3 once, with or without a clear | See what happened. Try again. | Device or CI | OK (#237, #311); use a real result, not edited text |
| 8 | Mission map with patches | Several missions cleared so patches show | 20 missions, a patch for each. | Device or CI (`HomeCaptureTests` / map) | OK; confirm the map layout on a phone, since PR #353 fixed card fit recently |
| 9 | Full designer (Settings → Show full designer) with a part picker or stage panel | Turn the switch on, open the designer | Engineers: every part and number. | CI (`DesignerV3CaptureTests` is the only capture at 1320 × 2868 and 2064 × 2752) or device | **Check first.** The code path is verified; I did not find a device walk-through of it. Hold until a device run confirms it opens and fits |

iPad: repeat 1 to 5, 7 to 9 at 2064 × 2752. The repo gives iPad 13" a 400 pt right-side designer panel (DECISIONS #62), so the designer layout differs from iPhone; capture it separately, do not scale up a phone image. Needs an iPad for route 1, or route 2.

Not shown on purpose: Home with credits or stats chips, anything with the corner guide character, Game Center, and any frame with a debug label.

---

## 4. Keep current

When the game updates, re-verify the listing before anyone touches ASC: list merged PRs since the sha in the last-verified line (`gh pr list --state merged --search "merged:>=<date>"`), then diff `data/features.json` (flags such as `designer.tierBuilder`, `designer.tierEngineer` and the corner-character flag), `data/campaign.json`, `data/codex.json`, `docs/DECISIONS.md`, `docs/PROJECT_STATE.md`, `unity/ProjectSettings/ProjectSettings.asset` (version, orientation, `targetDevice`, `uIRequiresFullScreen`), `Packages/manifest.json`, `MUSIC_LICENSES.md` and `docs/NARRATION_LICENSE.md` against the previous sha. For each changed claim, update this file's evidence table first: a feature only goes in the text if its PR is merged to main, Unity CI was green on it, and it is in the build being submitted (check the export tag `export/2.0.0-<build>`), and anything draft, open, or web-only moves to Not claimed. Rewrite only the lines the evidence changed, add one line to What's New from the template, rerun the character-count script (Apple limits: subtitle 30, promo 170, description 4000, keywords 100 bytes), and redo any screenshot whose screen changed (re-take from the new build, never edit a capture). The author of the update (Chief of Staff or the Grok Bot lane) proposes the diff; Trevor approves every ASC text, screenshot, privacy and submission step (PROJECT_STATE "Pending with Trevor"), and nothing is uploaded without his yes. Re-read Apple's screenshot page at each refresh because sizes change.

**Last verified against main at commit 39cb8490c45d61fc87e5ad3479a80a95e81b18ed on 2026-10-28 21:45 ET (UTC-4).**
