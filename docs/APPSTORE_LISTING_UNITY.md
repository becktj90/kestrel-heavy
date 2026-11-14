# Kestrel Heavy: App Store listing text and screenshot plan (Unity 2.0.0)

> **Field text and Trevor open items now live under [`docs/appstore/`](appstore/).**  
> Use [`appstore/LISTING.md`](appstore/LISTING.md) for copy, [`appstore/SCREENSHOTS.md`](appstore/SCREENSHOTS.md) for the CI shot plan, and [`appstore/TREVOR_OPEN_ITEMS.md`](appstore/TREVOR_OPEN_ITEMS.md) for decisions only Trevor can close.  
> This file keeps the longer **evidence** table. Refresh when claims change.

> **Draft for Trevor. Nothing is entered in App Store Connect. Price is not set here ($0.99 is blocked on the Paid Applications bank agreement). Trevor alone submits to App Review.**

**Last verified against main at commit b2bd87b31b8a966ce444ad9595a2dc63f0b5d0c1 (b2bd87b3) on 2026-11-09 (America/New_York).** Latest merge: #468 (KH-12 board starter fitted with closed-loop guidance computer). Latest export tag: **`export/2.0.0-215`** from main `b2bd87b31b8a` — **up to date with current main**. Previous listing check: 39cb8490 (2026-10-28).

Docs only. Character counts for the live draft are maintained in `docs/appstore/LISTING.md`. Facts used: portrait only, bundle `com.beckify.kestrelheavy` (iPhone), version 2.0.0, first Unity release. The Phaser build is a web demo only and is not described as the product.

---
## 1. Verified-merged evidence

Source of truth: the default branch `main` at b2bd87b3, read with `gh` (read-only). Merged PRs are those with `merged_at` set. Open or draft PRs are not counted. Since the 2026-10-28 listing check, the fun-review queue (#419–#448 and follow-ups), narration (#437/#450/#442), landing burn (#452), coast-warp (#436), and physics closes (#463–#468) all merged. Re-read claims against that tree; **device verification is still zero** unless Trevor fills `docs/DEVICE_TEST_SCRIPT.md`. `docs/LAUNCH_PLAN` does not exist on main; the docs read were README, DECISIONS (#1–#69), PROJECT_STATE, STANDING_BRIEF, HANDOFF_GROK, UNITY_SHIP_PLAN, APPSTORE_CONNECT, APPSTORE_REQUIREMENTS, DESIGNER_TIERS_SPEC, PHASED_DESIGNER_PLAN, MUSIC_LICENSES, NARRATION_LICENSE, plus `unity/` code and `data/*.json`.

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
| Mission control and webcast narration, AI-generated voices | PRs #185, #218, #350, #493, #494; `Resources/Narration` (140 mission-control, 123 host, 95 co-host clips, 358 mp3 files) plus 78 guide clips; `docs/NARRATION_LICENSE.md` (ElevenLabs, Trevor's paid Creator plan) |
| Music in the hangar, title and flight | PRs #211, #224, #238; `MUSIC_LICENSES.md` |
| Codex: 8 short lessons (delta-v, thrust-to-weight, Isp, staging, max-Q, center of mass/pressure, orbits, booster landing) | `data/codex.json`; `Kestrel.Game/Shell/CodexScreen.cs`; `ScreenStub.cs` wiring; Settings Codex row (`UiIds.SettingsCodex`) |
| Progress saves on device; CONTINUE on Home; credits wallet and draft autosave | PR #292; `Kestrel.Game/Shell/HomeResumeBar.cs`; `docs/ECONOMY.md` |
| Settings: music, sound, voice, subtitles, Reduce Motion, Reduce flashing, Haptics | `Kestrel.Core/Ui/UiIds.cs` (`SettingsMusic … SettingsHaptics`) |
| No ads, no IAP, no accounts, no analytics | DECISIONS #25, #34; `ProjectSettings/UnityConnectSettings.asset` (every `m_Enabled: 0`); `Packages/manifest.json` has no ads/analytics/IAP package; only `UnityWebRequest` use is reading bundled audio clips |
| Unity CI on the PRs behind the claims | #368 head: EditMode, PlayMode, PlayModeDevice, iOS export compile-check, Debug and Release iOS builds all pass. #367 (latest merge): EditMode, PlayMode, compile-check and both iOS builds pass, but its PlayModeDevice job still showed pending when checked, so #367 does not count as green (Unity CI does not run on pushes to main, only on PRs) |

### Not claimed (and why)

- **Game Center and leaderboards.** `AppleGameCenter.cs` compiles only if Apple's plug-in is installed (`KH_GAMEKIT`), the plug-in is not in `Packages/manifest.json`, the file "has never been compiled" (UNITY_SHIP_PLAN §risks), and the only board it references is a prototype id (`kh.unityproto.meco_alt_m`). The kh1–kh5 boards are from the old Phaser build.
- **"KID mode" and KID landing (PR #362).** Merged, but it changes `web/js/*` only. Its own PR text says "the Unity shipping app has no KID mode". Unity's "Kid" is the designer tier name, not a difficulty.
- **Builder and Engineer designer tiers, the tier switch, "Standard unlocks after the first orbit".** Flags `designer.tierBuilder` and `designer.tierEngineer` are false in `data/features.json`; PR #366 says only Kid ships and players see no switch.
- **Daily, weekly or challenge-lab modes, replays, Photo Mode, sandbox as a mode.** Described in older docs; no Unity screen or code found (a Sandbox settings toggle exists, but what it does is unclear).
- **"Original score", "adaptive score".** The flight bed `kestrel-flight.ogg` is AI-generated (Gemini, Trevor's paid plan); title and designer beds are original. Beds crossfade; nothing adaptive was verified.
- **"Honest physics", "real numbers", accuracy, framerate, 120 Hz, ProMotion, battery or heat claims.** The repo has an accuracy review with known gaps (point-mass gravity, planar flights, no thermosphere above 86 km) and no device performance measurement. I claim only that the flight model includes thrust, drag, gravity and staging, and I do not even use those words in the final text.
- **"Land the booster" as a hero claim.** #367 and later recovery work (#405/#435/#452) are on main with CI evidence; player-flown final burn is DECISIONS #79. **Still no Trevor device pass** on Haven. Listing copy stays at "tries to land" until `docs/DEVICE_TEST_SCRIPT.md` step 6 is Pass.
- **Contracts after KH-20, credits shop, corner guide character, safe-flash default, VoiceOver.** Data or partial code exists, but I could not confirm a shipped, default-on player path. The corner guide character is off by default on main (its flag in `data/features.json` is false; DECISIONS #72 and PR #374 "corner character off by default", still a draft, cover the Settings switch), and the listing text does not mention it.
- **Formerly open, now merged (do not treat as gated):** #371 explosions, #372/#375 fairing + KH-13 guidance docs/mesh, #374 corner character off by default, #376/#437 two-host commentary, #367 recovery landings, fun-review P0s #419/#425/#432/#427.
- **Guidance computer:** KH-13 campaign winner and board starters for KH-12 (#468 / DECISIONS #97) and KH-13 ship with `avionics.guidanceComputer` on; not a store-copy claim.
- **Web-only:** anything in `web/` (Phaser): KID difficulty, the old five-flight Pier 7 game, `docs/KESTREL_APP_STORE.md` (marked ARCHIVED).

---

## 2. Final text

**Moved.** Canonical ASC field drafts: [`docs/appstore/LISTING.md`](appstore/LISTING.md).

## 3. Screenshot plan

**Moved.** [`docs/appstore/SCREENSHOTS.md`](appstore/SCREENSHOTS.md) — uses the existing `appstore-screenshots` workflow / `AppStoreScreenshotTests` path; regenerate only that way.

## 4. Keep current

When the game updates, re-verify before anyone touches ASC: list merged PRs since the sha in the last-verified line, diff `data/features.json`, `data/campaign.json`, `docs/DECISIONS.md`, `unity/ProjectSettings/ProjectSettings.asset`, and `Packages/manifest.json`. Update `docs/appstore/LISTING.md` first; keep this evidence table honest. Trevor approves every ASC text, screenshot, privacy and submission step.

**Last verified against main at commit b2bd87b31b8a966ce444ad9595a2dc63f0b5d0c1 on 2026-11-09 (America/New_York).**
