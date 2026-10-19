# Kestrel Heavy decisions log

Trevor (2026-10-11, 10:20 PM) delegated design and technical calls to Chief of Staff and Kestrel Arcade. The goal is a breathtaking game that's fun for kids and engineers. Trevor still approves spending (budget $0), App Store submission, signing and certs, Xcode or bundle changes to the live record, and anything affecting the privacy label.

| # | Date | Decision |
|---|------|----------|
| 1 | 10-11 | Unity 6.3 LTS is the shipping engine. 2.0 is the first Unity release, under the same bundle ID and App Store record. release/1.0.1 is archived. Phaser is web demo only. |
| 2 | 10-11 | Editor automation goes through the official Unity CLI only. Unity Build Automation free tier, no card. No builds until Trevor says ready. Daily CI is dotnet plus golden vectors on Ubuntu. |
| 3 | 10-11 | The 2.0 cut for C, G and M is ported into the U1–U9 plan (docs/UNITY_SHIP_PLAN.md). |
| 4 | 10-11 | Designer layers: Simple by default, Standard unlocks after the first orbit, Engineer is a Settings toggle. The kid path gets big tactile parts, instant launch, and spectacle every flight. |
| 5 | 10-11 | Sensors: exact values in KID and Sandbox, light noise in Standard, no weather holds in KID or Relaxed. |
| 6 | 10-11 | Safe-flash on by default. Reduced motion follows the OS setting. |
| 7 | 10-11 | Portrait-only with themed fill on big screens. iPad is full-screen portrait. UIRequiresFullScreen only if needed, and it changes the listing, so Trevor confirms first. |
| 8 | 10-11 | 120 Hz on ProMotion, with a thermal governor, quality Auto, and battery saver off. |
| 9 | 10-11 | Generic fictional pad. Photo and CSV export through the share sheet, with no Photos permission. |
| 10 | 10-11 | Menu: a living title pad with the player's rocket, tap-to-ignite, one CONTINUE button, and 5 root sections. Diegetic first, with a flat list in Accessibility settings. |
| 11 | 10-11 | Stack: URP, VFX Graph, Shader Graph, Cinemachine, Input System, UI Toolkit, Burst/Jobs, Addressables. Free assets only, logged in ASSETS_LICENSES.md. |
| 12 | 10-11 | Moderate failure penalty. 3D hangar. Physics stays honest, and difficulty comes from assists and presets. |
| 13 | 10-11 | Narration: no free-tier ElevenLabs clips in the paid app. Use the existing paid account if Trevor confirms it's his, otherwise on-device speech. |
| 14 | 10-11 | App Store screenshots get redone at the end. |
| 15 | 10-11 | Player-first. Rivals are flavor plus a light market (contract price drift); never block progress. Deep studio/market sim is later. |
| 16 | 10-11 | Narration (supersedes #13 and ship plan U7 text): ElevenLabs clips pre-generated once on Trevor's paid Creator plan, non-beta models, bundled; on-device TTS only for player-specific strings; no live server. |
| 17 | 10-11 | Music: original Kestrel motif (4–7 notes, Lydian, 72–96 BPM); generative + MIDI-rendered stems with free instruments for 1.0; adaptive engine data-driven so stems swap as data. AI music only if terms clearly allow paid apps. |
| 18 | 10-11 | Offline-first, $0, no backend in 1.0. Any future online leaderboard uses deterministic re-sim validation. Real-time multiplayer deferred. |
| 19 | 10-11 | Own repo (becktj90/kestrel-heavy), not the beckify monorepo. |
| 20 | 10-11 | Scope tiers per MASTER_ROADMAP.md; X1–X7 specs feed U1–U9. PROJECT_STATE.md is the living context file. |
| 21 | 10-11 | Legal/compliance docs are "draft for counsel"; audience/age rating needs Trevor's explicit decision. |
| 22 | 10-11 | Engagement: loop Design→Predict→Launch→Learn→Improve→Relaunch. RELAUNCH <2 s from results, 10–30 s failure→fixed relaunch; cinematics shorten on repeat. |
| 23 | 10-11 | Results screen primary button is RELAUNCH; TWEAK opens the designer on the failing part with one suggested fix. |
| 24 | 10-11 | Ban list (ENGAGEMENT_SPEC §7) is non-negotiable; ethics checklist required on engagement PRs. |
| 25 | 10-11 | Monetization: $0.99 premium, no IAP, no ads. Metrics are local on-device only, including negative signals. |
| 26 | 10-11 | Streaks forgiving (freezes, 48 h grace, hideable); notifications opt-in, ≤1/week, none in KID; calm mode; social opt-in via Game Center only. |
| 27 | 10-11 | Modes 1.0: campaign, sandbox, daily, weekly, challenge lab (12). 1.1: contract rush, failure investigation. Authored surprises only. |
| 28 | 10-11 | Age rating draft: 4+, all ages, Data Not Collected, not Kids Category — **Trevor decides**. |
| 29 | 10-11 | Name review (docs/NAME_REVIEW.md, not legal advice): keep "Kestrel Heavy" provisionally (amber-green, counsel check before 1.0). Replace "Northwind Aerospace" (real aerospace firm North Wind). Fallback title: Updraft Heavy. |
| 30 | 10-11 | First Unity release is version 2.0.0 (rename the unreleased 1.0.1 ASC version to 2.0 at G2); build numbers start at 100; bundle com.beckify.kestrelheavy. |
| 31 | 10-11 | Recommend Games → Simulation primary, Arcade secondary for the Unity build (ASC change is Trevor's G2). |
| 32 | 10-11 | Store listing claims are gated per milestone (APPSTORE_CONNECT.md); accessibility labels declare only Dark Interface and Reduced Motion until gaps close. Age rating recommendation 4+, not Made for Kids (Trevor decides). |
| 33 | 10-11 | fastlane: default dry_run lane is local-only; ASC lanes require TREVOR_APPROVED=yes; no submit lane. Secrets only in CI/UBA. |
| 34 | 10-11 | Trevor: age rating 4+, all ages, Data Not Collected, not in Kids category. Unity analytics and crash reporting stay off. |
| 35 | 10-11 | Trevor: Unity build ships as version 2.0.0 (rename the unreleased 1.0.1 record). |
| 36 | 10-11 | Trevor: primary category Simulation, secondary Arcade. |
| 37 | 10-11 | Trevor: remove China mainland and Vietnam from availability. |
| 29 | 10-11 | Launch Timeline: every rocket gets a generated mission event sequence, shown in the designer and live in flight. |
| 30 | 10-11 | One `MissionEvent` schema for narration, SFX, music, telemetry, pad, economy and Codex; N1 bus types become event ids. No parallel schemas. |
| 31 | 10-11 | Generator is pure, deterministic C# in the shared core; golden timelines for all templates, property tests, <5 ms worst case. Fixed SPINE/PACE not ported. |
| 32 | 10-11 | Events exist only if the hardware does (SES-n only for restartable engines, strongback only if the pad has one); Max-Q etc. are sim conditions, not scripted times. |
| 33 | 10-11 | Calibrate ordering/spacing against public timelines; all names and wording original and fictional; times compressed per mode. |
| 34 | 10-11 | Portrait bottom-sheet tracker: status by shape, tabular T±, `--:--:--` until an event happens, Δ vs nominal in Standard/Engineer. |
| 35 | 10-12 | Commit Unity `.meta` files for every tracked asset (standard Unity practice) so GUIDs stay stable across checkouts; generated scenes/settings assets stay code-first and untracked. |
| 38 | 10-12 | Environment Realism (Chief of Staff, under Trevor's delegation): all 12 audit recommendations approved (ENVIRONMENT_REALISM_AUDIT §8). 120 Hz wherever thermals allow: Auto picks High on ProMotion, governor steps down to 60. Weather "launch anyway" override shows its reliability penalty honestly, no nagging. |
| 39 | 10-12 | Space (Chief of Staff): SPACE_AUDIT §7 #1–4 and #6–9 approved as recommended. #5: 2.0 ships **no** on-demand textures; space assets ≤ 8 MB inside the IPA (procedural detail + compressed textures). ODR vs GitHub Releases is a later question for Trevor; no build-config change. |
| 40 | 10-12 | 2.0 space scope: Earth orbit only (true sky, Moon, eclipses, J2, orbital decay). Lunar landing, interplanetary, solar-system view post-launch, architected now. |
| 41 | 10-12 | Extras in scope: Kármán-line milestone `MissionEvent` + narrator line; photographic exposure model with dark-adapted-eye mode and a labelled cinematic assist; near-silence in vacuum; spent stages stay in orbit (and decay); Photo Mode space shots; Codex Labs: Orbit, Moon Phase, Sun-Sync, Exposure, Wind Shear. |
| 42 | 10-12 | `LICENSES.md` asset/data ledger exists from now on; only public-domain or CC0, each licence verified and logged before the file lands. |
| 43 | 10-12 | R2/S1 is one shared astronomy milestone in `Kestrel.Core.Astro`: deterministic sun/Moon/star positions, golden vectors vs JPL Horizons ≤ 0.1° (2000–2100). |
| 44 | 10-12 | Pad Detail (D1–D7) + Launch Vehicle Detail (V1–V7, replaces the rocket-detail pass) are one P0 track for 2.0.0, ahead of lower-priority R/S items; shared kit, materials, LOD and one pad–vehicle umbilical contract. Audit: PAD_VEHICLE_DETAIL_AUDIT.md; references in REFERENCE_LEDGER.md. |
| 45 | 10-12 | Parallel split by path: Graphics Dev D1–D7, R visuals, kit, umbilical contract (`Pad/**`, `Kit/**`, `Environment/**`); Art V1–V6 (`Vehicle/**`, `Art/Vehicle/**`, `Livery/**`); Gameplay Dev S2/S3, R3 weather/commit, Data Sheet numbers, V7 Fleet (`Kestrel.Core/**` except Astro, `data/**`); Physics validation + PAD_/VEHICLE_ENGINEERING_RULES.md; Sound anchors + vacuum silence (`Audio/**`); Codex Labs (`Codex/**`); UX Data Sheet/Fleet/inspect/Photo Mode screens (`UI/Screens/**`); QA CI/golden images/regression (`.github/workflows/**`, `tests/golden-images/**`); Kestrel Arcade lead/integrator, merge queue, sole Unity CLI driver. |
| 46 | 10-12 | Pad+Vehicle (Chief of Staff, under Trevor's delegation): all 19 PAD_VEHICLE_DETAIL_AUDIT §10 recommendations approved (Q2 resolved: #81 PadTower merged; D1 rebuilds behind it). |
| 47 | 10-12 | Umbilical contract: Graphics Dev owns the contract doc and visual side; Gameplay Dev owns the Core type. Schema changes need Graphics Dev + Gameplay Dev + Art review and a DECISIONS entry. |
| 48 | 10-12 | Cutaway, inspect mode and the Fleet/Reuse tracker are release-blocking in 2.0. Exploded/load-path views, Fleet history charts, free-form livery and free-roam Pad Walk are 1.1 (audit §11 lists every deferral). |
| 49 | 10-12 | Budget rule: if pad+vehicle exceed budget, cut far-field props before close-up hero detail on the pad or rocket. |
| 50 | 10-12 | Chief of Staff: basic Photo Mode ships in 2.0: pause, orbit or rail camera, exposure and time-of-day sliders, hide HUD, a few space-shot presets, PNG export via the share sheet only (no Photos permission). 1.1: full lens and filter suite, free-roam camera, Orbital Photographer achievements. Supersedes the Photo Mode row in #48/audit §11. |
| 51 | 10-12 | Art owns the Hangar Reveal (U8) scene, camera and vehicle staging; Graphics Dev owns the hangar environment; stage indices are zero-based in code and 1-based in the UI. |
| 52 | 10-12 | Xcode Cloud planned as primary iOS build path (Unity CLI export on the box → `ios-export` branch → Xcode Cloud), manual-start only, no builds until Trevor says ready; UBA fallback. First Xcode Cloud onboarding needs Xcode on a Mac (Apple); needs Trevor's OK. See docs/XCODE_CLOUD_PLAN.md. |
| 53 | 10-12 | Trevor (6:59 AM): no Mac, so iOS builds go through Unity Build Automation on the free tier; Xcode Cloud is shelved (supersedes #52; docs/XCODE_CLOUD_PLAN.md kept for reference only). Still no builds until Trevor says ready, $0 spend, and ask before any signing or App Store Connect change. |
| 54 | 10-10 | Trevor: Crew Talk. Crew lines are written in Spanish and voiced/subtitled in English. The spoken line and the main English line keep the full meaning but are rendered in each character's own dialect and style (Bodie, Junie, Pearl, Sloane). Never a canned stock line, never a literal translation. Owners: Narrator Voice (voices), Codex (writing), Sound (mix). |
| 55 | 10-11 | Narration cast: mission control plus two webcast hosts (currently Eric, Sarah, Chris), pre-generated on Trevor's existing ElevenLabs Creator plan. No new spend (see #16, NARRATION_LICENSE.md). |
| 56 | 10-12 | Realism: honest physics stays, and difficulty uses the three-step Simple / Standard / Engineer assist ladder (#4, #5, #12). No realism slider; Trevor didn't ask for one. |
| 57 | 10-12 | Unity ProjectSettings, URP/Quality assets and packages-lock are committed (lead decision; generated by Kestrel > Setup Quality with the URP depth texture on for Medium/High/Ultra). Library/, Temp/, Logs/, UserSettings/ and editor-generated scenes stay ignored. |
| 58 | 10-12 | Name stays Kestrel Heavy. Backup titles are shortlisted in NAME_FALLBACKS.md (a quick public screen, not legal clearance). |
| 59 | 10-12 | #53 reversed 2026-10-12 12:26 PM: Trevor does the one-time Xcode Cloud setup on his Mac (docs/XCODE_CLOUD_FIRST_SETUP.md); UBA iOS shelved; export on `xcode-export` (orphan branch, LFS, never merged to main; replaces the `ios-export` name in #52). Trevor approved (12:27 PM) stamping com.beckify.kestrelheavy, 2.0.0, build 100, team 9TR6R5LV8M, automatic signing, Game Center only. Workflow Manual Start only; no builds until Trevor says ready; no ASC changes; $0. Supersedes #53. |
| 60 | 10-12 | Kestrel Arcade (lead): overrides #48 for the exploded view, which now ships in 2.0. A cloud agent owns R1 (`HangarCamera`) then R2 (exploded view); Graphics stays on the pad. |
| 61 | 10-12 | Kestrel Arcade (lead): template branding (12 glyphs, 6 frames, 16 colours) ships in 2.0. Free-form livery stays 1.1. |
| 62 | 10-12 | Kestrel Arcade (lead): iPad designer uses a 400 pt right side panel. |
| 63 | 10-12 | Kestrel Arcade (lead): designer undo goes 50 steps back. |
| 64 | 10-12 | Kestrel Arcade (lead): 8 local design save slots (no cloud, no account). |
| 65 | 10-12 | Kestrel Arcade (lead): liveries are earned only (never sold, never random, never time-limited). |
| 66 | 10-14 | Trevor: always squash-merge a pull request the moment required checks are green, and delete the branch. Enable auto-merge while checks are still running. Do not leave a green PR open. Supersedes the designer-v3 prompt rule that told agents to open a PR and stop. |
| 67 | 10-18 | Flight HUD stays in the Design scene. Designer Launch hides the sheet, the designer, and the web readout column with inline display, then brings the HUD forward. The Flight scene stays the placeholder stub. Vehicle health is five short marks under the clock, painted from the live sim. Flight notes scroll above the dock. |
