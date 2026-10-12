# Kestrel Heavy: Unity prototype → App Store ship plan
_Draft, Oct 11 2026. Not committed. Based on `unity-prototype` @ e908d00 (draft PR #13), ENGINE_PHASE0.md, C_AUDIT, GFX_AUDIT, docs/VISUAL_SPEC.md (PR #16)._

## 0. Decisions locked (Trevor, 10:11 PM Oct 11)
- **Unity 6.3 LTS (6000.3.26f1) is the shipping engine.** The first App Store release is the Unity build (version 2.0).
- `release/1.0.1` is **archive only**. Don't release it, don't delete it.
- Phaser = beckify.com web demo. Bug fixes only, **no new G/C/D work.**
- **Portrait-only**, including iPad. Big screens get themed fill, never letterboxing (VISUAL_SPEC §portrait).
- Ship on **`com.beckify.kestrelheavy`** and the existing App Store record, so reviews and leaderboard IDs carry over. Until then, every build goes to TestFlight on **`com.beckify.kestrelheavy.unityproto`**.
- Moderate failure penalty (economy). Hangar is **3D** (T reveal in Unity, not three.js).
- **Editor automation runs only through the official Unity CLI** (no raw `Unity -batchmode` from agents). iOS builds use Unity Build Automation's free tier; GameCI on CI is held until Trevor clears it under ToS §17.2 (see gate G6). $0 budget (§5).
- **One merge queue**, owned by Kestrel Arcade: rebase, CI green, squash, delete branch. **One lane per hot file** (the C# equivalents of mission.js are `FlightDirector.cs` and `Events.cs`).

## 1. Foundations (already in PR #13)
- `Kestrel.Core` (noEngineReferences) is shared C#: atmosphere, aero, gravity, orbit, rocket-eq, RK4, AscentSim, design/mass/stack, cost + reliability. dotnet: 17/17 tests, 129/129 golden vectors, max rel err 3.3e-13.
- Catalogs: `data/*.json` + schemas (from `web/js/data`), plus `economy-data/constants.json`, loaded by `Kestrel > Sync Data`. **JSON is the single source.** JS and C# both read it, and nobody hand-copies it.
- Golden vectors: `data/golden/*.json` via `npm run gen:golden`. **Rule: every Core PR adds or extends vectors. CI fails on drift > stated ε.**
- Slice: SliceApp, FlightDriver (1/120 s, floating origin), RocketMeshBuilder, PlumeController, GameCenterBridge, SliceBootstrap, BuildIOS.

## 2. Milestones U1–U9
| U | Scope | Golden vectors / exit | Owner | Specialists |
|---|---|---|---|---|
| **U1 Core parity** | Port the rest of the headless logic: designer validation + readouts, save schema/migration (native file store, which retires SAVE_BRIDGE), params registry (C1a), engineModel, D0 replay KH-1..5 | Vectors for designer-validate, save-store, vehicle v2 migrate, D0 replay. dotnet + Unity Test Runner green via CLI | Gameplay Dev | Physics (ε review), QA |
| **U2 Shell + portrait** | App bootstrap, scene flow, safe-area, portrait lock, themed big-screen fill, quality presets + governor (G1), UI Toolkit theme/tokens | VISUAL_SPEC scaling cases on iPhone SE / Pro Max / iPad 13" screenshots | Graphics Dev | UX, Art |
| **U3 Designer (M1–M2 + C1a)** | DESIGN screen in UI Toolkit: stages, engines, propellant, structure, payload, recovery, cosmetics. Live TWR/Δv/burn readouts, validation, "Make it fly", presets/saves, T1 templates, undo/symmetry | Readouts equal Core vectors. Template set validates | Gameplay Dev | UX, Physics, QA |
| **U4 Flight (M3 + C2 + G4/G5 core)** | Flight scene: autopilot, staging, orbit determination, telemetry channel registry + HUD/drawer, procedural vehicle renderer, physics-driven plume + diamonds, sky→space fade, Earth curvature, camera rig | Flight replay matches `simulate.json`. p95 ≤16.7 ms on floor device | Gameplay Dev (sim/HUD logic); Graphics Dev (render/plume/sky/camera) | Art, Physics, QA |
| **U5 Failures + report (M4)** | Failure modes, structural loads/Max-Q, flex visuals, post-flight report + plot, recorder | Loads/failure vectors. Report numbers match Core | Gameplay Dev | Physics, Graphics Dev (failure FX), Codex (explanations) |
| **U6 Launch complex + recovery (G2/G3/G6, C4, C6-core)** | Pad requirements/compat, seeded pad generator, countdown + ignition substates, expendable/droneship (Haven strake-glide)/RTLS/parachute fairing | Pad-sizing + descent/landing-burn vectors. Golden-image gallery in CI | Graphics Dev (pad/FX/countdown); Gameplay Dev (recovery physics) | Art, Physics, QA |
| **U7 Audio + narrator (S1, S3-core, N1–N6)** | Unity Audio Mixer port of S1 (buses, ducking, lifecycle), physics-driven engine/atmo/structural SFX, narration event bus + queue + director, subtitles, webcast overlay (mission clock, telemetry). **Bundled ElevenLabs clip library, pre-generated once on Trevor's paid Creator plan (non-beta models, commercial license kept), + on-device iOS speech (AVSpeechSynthesizer) for player-specific strings. No live server, no Fly.io.** | Event→callout mapping test. Mixer levels per S1 spec | Gameplay Dev (bus/queue) | Sound, Narrator Voice, UX (subtitles) |
| **U8 Campaign + hangar 3D (C/E economy 1.1-lite, M6, edu)** | Budget, contracts, costs from Core, reserves/loans, **moderate failure penalty**, tech tree, hangar 3D reveal + inventory, tutorial/first 5 min, Codex L1 + moment cards | `economy.json` exact. Campaign sim vectors (N flights → balance) | Gameplay Dev (economy); Graphics Dev (hangar 3D) | Codex, UX, Art, QA |
| **U9 Ship** | Game Center (Apple unityplugins, same leaderboard IDs), medals, achievements, VoiceOver/Dynamic Type gap closure, perf/thermal/IPA ≤40 MB, App Store assets, **bundle swap .unityproto → com.beckify.kestrelheavy** | RESULTS.md pass criteria all met. Full QA regression. Approved build in ASC | Kestrel Arcade (release/queue) | QA, Marketing, UX, Art, Sound |

**Release cut.** 2.0 = U1–U7 + U9, with U8 limited to tutorial, Codex L1 and a basic budget. Full economy/tech tree, C3 console, C7 wear, G7 photo mode, Ultra quality and the rest of C5 (vapor, glow, bloom) go to 1.1.
**Parallelism.** U1 ∥ U2 → U3 → U4 → (U5 ∥ U6 ∥ U7) → U8 → U9. Gameplay Dev and Graphics Dev run on separate folders (`Kestrel.Core`/`Kestrel.Game.Sim` vs `Kestrel.Game.Render`). Only one lane edits `FlightDirector.cs` at a time.

## 3. Porting map (C / G / M / S1 / narrator)
- **M1–M6** → U1 (M1 core), U3 (M2), U4 (M3), U5 (M4), U8 (M5 economy/tech, M6 tutorial/polish/save).
- **C1a** → U1/U3. **C2** → U4. **C4/C6-core** → U6. **C5** → U4 (core). C1b/C1c/C3/C7 → 1.1.
- **G1** → U2. **G2/G3/G6** → U6. **G4/G5** → U4. **G7** gallery → CI in U6, photo mode in 1.1. VISUAL_SPEC.md is the contract.
- **S1** → U7 (Unity Audio Mixer replaces the JS mixer; the S1 bus names and levels are the spec).
- **Narrator** → U7. The manifest, subtitles and already-generated clips are reused as is. Drop the OGG duplicates (−4.3 MB). **ElevenLabs stays as pre-generated clips** on Trevor's paid Creator plan (DECISIONS #16); no live generation, Fly.io backend cut. Player-specific lines use on-device TTS.
- **Arcade mission.js logic** (hazards, corridor, medals) is **rewritten, not translated**, inside U4/U5. Its scope is cut to what the designer game needs.

## 4. Specialist standing duties
QA: test pass per milestone + device matrix · UX: flows/portrait/a11y review · Art: style, palette, icons, hangar · Sound: S1/S3 specs + mixes · Narrator Voice: script + clip library · Physics: ε review on every Core PR · Codex: edu content + report text · Marketing: screenshots, preview video, listing copy (U9).

## 5. $0 zero-Mac pipeline (replaces the Mac checklist)
**Hard constraint: $0 budget, no card on file anywhere, no overage.** Trevor does nothing on a Mac; one-time steps are browser-only (Chief of Staff can drive them).

**Lanes**
1. **Every PR, GitHub Ubuntu (free private-repo minutes, 1× multiplier):** `dotnet test` for `Kestrel.Core` (17 tests) + golden-vector drift check (`npm run gen:golden` + compare), JSON schema validation. No Unity involved, so it doesn't touch ToS §17.2(ff).
2. **iOS → TestFlight: Unity Build Automation free tier.** 100 Mac Standard minutes/month, renews monthly, **no card**. With no card, hitting the limit blocks builds (an effective $0 cap). Budget ≈ 25–35 min per Unity 6 iOS build (Library import + IL2CPP + Xcode archive; the first uncached build may take 40+) → **about 3 builds/month**. Build only on demand (tag `tf-*` or manual), never per push. Upload to TestFlight is free (covered by the existing Apple Developer membership).
3. **Overflow (only if approved under gate G6):** GitHub macOS runner with GameCI + fastlane, using the ASC API key secrets. A private repo's 2,000 free minutes count macOS at 10×, so that's about 200 Mac minutes (~6 builds). The repo must have a $0 Actions budget with no payment method so overage is blocked, not billed.
4. **No Unity on Linux CI by default.** A free headless/WebGL Unity check on Ubuntu (GameCI) is technically possible inside the free minutes, but it needs a Personal `.ulf` in secrets and carries the same §17.2(ff) risk as lane 3, so it's gated by G6.

**Code-first, no editor clicks:** scenes, PlayerSettings (portrait, bundle ID, team 9TR6R5LV8M), URP assets and build profiles are generated by `Kestrel.Editor.BuildPipeline.*` methods. Build Automation calls them as **Pre-export / Custom build method**; GameCI calls them via `buildMethod` (`-executeMethod`).

**One-time browser steps (no Mac)**
1. cloud.unity.com (Unity ID already exists): create the org/project. **Don't add a payment method.** Turn on usage notifications.
2. Build Automation → connect GitHub `becktj90/kestrel-heavy`, project subfolder `unity/`, Unity 6000.3.26f1, iOS target, set the custom/pre-export method, auto-build off.
3. developer.apple.com: create an Apple Distribution certificate from a CSR (the CSR and .p12 are made on the box with openssl, not a Mac), then an App Store provisioning profile for `com.beckify.kestrelheavy.unityproto` (later `com.beckify.kestrelheavy`). Upload the .p12 and profile to Build Automation iOS credentials.
4. App Store Connect: create the "Kestrel Heavy Proto" record (`.unityproto`) and the test leaderboard, and add Trevor to TestFlight Internal (gate G2). The API key already exists (ASC_KEY_ID/ISSUER/P8). Connect it to Build Automation's App Store Connect delivery; fallback is a free post-build step that uploads the IPA with the API key.
5. GitHub → Settings → Billing: confirm the Actions budget is $0 with no payment method.
6. Email Unity support asking for written confirmation that "AI-authored commits → Unity Build Automation builds" is permitted under ToS §17.2(ff), or is Authorized Agentic Access (gate G6).

**Device checks** come from TestFlight on Trevor's phone (fps, cold start, IPA size via ASC), not Xcode.
**U9:** submit for review from App Store Connect in the browser (gate G2).

## 6. Approval gates (ask Trevor first)
| # | Gate | When |
|---|---|---|
| G1 | **Xcode project / signing** changes, ASC API key role change, archive/upload of any new bundle | U2 first TestFlight, U9 |
| G2 | **App Store record** changes: new proto record, leaderboards, metadata, screenshots, pricing, the bundle swap to `com.beckify.kestrelheavy`, submit for review | U2, U9 |
| G3 | **Free assets only.** CC0/OFL/self-made (or Unity free packages) with licenses logged in `ASSETS_LICENSES.md`. Nothing paid, ever (Asset Store paid, fonts, audio, Unity Pro/Plus) | any |
| G4 | **$0 budget**: no card on Unity Cloud or GitHub. Free tiers only; no ElevenLabs, no Fly.io, no paid build minutes | always |
| G5 | **iPad `UIRequiresFullScreen`** (portrait-only iPad means giving up Split View) | U2 |
| G6 | **Unity automation surface**: anything outside the Unity CLI, e.g. GameCI/`-batchmode` in GitHub Actions, or a Unity AI/MCP connector | before enabling CI Unity jobs |
| G7 | Retiring the Phaser/WKWebView `ios/` target from main (kept on `release/1.0.1`) | U9 |

## 7. Risks
- The ToS §17.2(ff) reading of CLI vs CI → G6. Mitigation: Unity Build Automation (Unity-operated) plus a written OK from Unity support.
- 100 free Mac minutes ≈ 3 TestFlight builds/month. Batch changes; an over-limit block also risks Unity Cloud data deletion after 30 days locked out.
- The mission.js arcade rewrite (~2.5k C#) is the largest scope. Cut it to what the designer game needs.
- Apple GameKit plug-in signature drift (`AppleGameCenter.cs` has never been compiled).
- IPA ≤40 MB with URP + audio: audit at U4 and U7.
- VoiceOver/Dynamic Type in UI Toolkit is a known gap, budgeted in U9.

> Update 2026-10-11 10:17 PM: Trevor wants ElevenLabs narration kept: pre-generate clips on the free tier (pending terms check) and bundle them offline; no paid live server.

## 8. Engagement H1–H6 (docs/ENGAGEMENT_SPEC.md)
| H | Scope | Lands in | Owner |
|---|-------|----------|-------|
| H1 | Audit + spec + age rating draft (docs) | done | Kestrel Arcade |
| H2 | Loop speed: RELAUNCH <2 s, quick-tweak w/ highlighted fix, shortening cinematics | U3 (tweak), U4 (relaunch, cinematics) | Gameplay Dev, UX |
| H3 | Gap-to-goal debrief, near-miss, ghost + A/B flip, predict + success probability | U4 (ghost, predict), U5 (debrief) | Gameplay Dev, Physics |
| H4 | Goals at 3 timescales, Next-up, mastery ladder, PBs, modes, offline advisor | U8 (1.0 modes), 1.1 rest | Gameplay Dev, UX, Codex |
| H5 | Forgiving streaks, welcome-back, calm mode, opt-in notifications, Game Center social | U8, U9 | Gameplay Dev, UX |
| H6 | Balance sim on headless core (`pacing.json`) + playtest kit (kids, casual adults, engineers) | U8–U9 | QA, Physics |
Acceptance additions: U4 gate includes results→relaunch <2 s; U5 gate includes debrief numbers = Core; U9 gate includes ethics checklist pass.

## 9. Launch Timeline K1–K7 (docs/MISSION_EVENTS.md)
K2 schema + K3 generator in the shared core with U1–U3 (golden timelines for all templates, <5 ms). K4 designer preview in U3. K5 live tracker in U4 (anomalies U5, holds/recycle/scrub with the generated pad in U6). K6 narration/SFX/music/Codex/economy subscribers in U7–U8. K7 replay scrub, achievements, a11y/loc, calibration review in U5 and U9. The fixed `SPINE`/`PACE` tables are not ported.
