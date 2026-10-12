# Kestrel Heavy: Unity prototype → App Store ship plan
_Draft, Oct 11 2026. Not committed. Based on `unity-prototype` @ e908d00 (draft PR #13), ENGINE_PHASE0.md, C_AUDIT, GFX_AUDIT, docs/VISUAL_SPEC.md (PR #16)._

## 0. Decisions locked (Trevor, 10:11 PM Oct 11)
- **Unity 6.3 LTS (6000.3.26f1) is the shipping engine.** The first App Store release is the Unity build (version 2.0).
- `release/1.0.1` is **archive only**. Don't release it, don't delete it.
- Phaser = beckify.com web demo. Bug fixes only, **no new G/C/D work.**
- **Portrait-only**, including iPad. Big screens get themed fill, never letterboxing (VISUAL_SPEC §portrait).
- Ship on **`com.beckify.kestrelheavy`** and the existing App Store record, so reviews and leaderboard IDs carry over. Until then, every build goes to TestFlight on **`com.beckify.kestrelheavy.unityproto`**.
- Moderate failure penalty (economy). Hangar is **3D** (T reveal in Unity, not three.js).
- **Editor automation runs only through the official Unity CLI** (no raw `Unity -batchmode` from agents). GameCI on CI is held until Trevor clears it under ToS §17.2 (see gate G6).
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
| **U7 Audio + narrator (S1, S3-core, N1–N6)** | Unity Audio Mixer port of S1 (buses, ducking, lifecycle), physics-driven engine/atmo/structural SFX, narration event bus + queue + director, subtitles, webcast overlay (mission clock, telemetry). **Bundled offline clip library**; Fly.io dynamic strings behind a flag (off) | Event→callout mapping test. Mixer levels per S1 spec | Gameplay Dev (bus/queue) | Sound, Narrator Voice, UX (subtitles) |
| **U8 Campaign + hangar 3D (C/E economy 1.1-lite, M6, edu)** | Budget, contracts, costs from Core, reserves/loans, **moderate failure penalty**, tech tree, hangar 3D reveal + inventory, tutorial/first 5 min, Codex L1 + moment cards | `economy.json` exact. Campaign sim vectors (N flights → balance) | Gameplay Dev (economy); Graphics Dev (hangar 3D) | Codex, UX, Art, QA |
| **U9 Ship** | Game Center (Apple unityplugins, same leaderboard IDs), medals, achievements, VoiceOver/Dynamic Type gap closure, perf/thermal/IPA ≤40 MB, App Store assets, **bundle swap .unityproto → com.beckify.kestrelheavy** | RESULTS.md pass criteria all met. Full QA regression. Approved build in ASC | Kestrel Arcade (release/queue) | QA, Marketing, UX, Art, Sound |

**Release cut.** 2.0 = U1–U7 + U9, with U8 limited to tutorial, Codex L1 and a basic budget. Full economy/tech tree, C3 console, C7 wear, G7 photo mode, Ultra quality and the rest of C5 (vapor, glow, bloom) go to 1.1.
**Parallelism.** U1 ∥ U2 → U3 → U4 → (U5 ∥ U6 ∥ U7) → U8 → U9. Gameplay Dev and Graphics Dev run on separate folders (`Kestrel.Core`/`Kestrel.Game.Sim` vs `Kestrel.Game.Render`). Only one lane edits `FlightDirector.cs` at a time.

## 3. Porting map (C / G / M / S1 / narrator)
- **M1–M6** → U1 (M1 core), U3 (M2), U4 (M3), U5 (M4), U8 (M5 economy/tech, M6 tutorial/polish/save).
- **C1a** → U1/U3. **C2** → U4. **C4/C6-core** → U6. **C5** → U4 (core). C1b/C1c/C3/C7 → 1.1.
- **G1** → U2. **G2/G3/G6** → U6. **G4/G5** → U4. **G7** gallery → CI in U6, photo mode in 1.1. VISUAL_SPEC.md is the contract.
- **S1** → U7 (Unity Audio Mixer replaces the JS mixer; the S1 bus names and levels are the spec).
- **Narrator** → U7. The manifest, subtitles and clips are reused as is. Drop the OGG duplicates (−4.3 MB).
- **Arcade mission.js logic** (hazards, corridor, medals) is **rewritten, not translated**, inside U4/U5. Its scope is cut to what the designer game needs.

## 4. Specialist standing duties
QA: test pass per milestone + device matrix · UX: flows/portrait/a11y review · Art: style, palette, icons, hangar · Sound: S1/S3 specs + mixes · Narrator Voice: script + clip library · Physics: ε review on every Core PR · Codex: edu content + report text · Marketing: screenshots, preview video, listing copy (U9).

## 5. Trevor's Mac checklist
1. **Unity Hub → Installs → 6000.3.26f1** with the **iOS Build Support** module. Sign in, Personal license.
2. Install **Xcode 26+** and sign in with the Apple ID on team **9TR6R5LV8M** (Xcode → Settings → Accounts).
3. `git clone becktj90/kestrel-heavy`, check out the current branch, then **Unity Hub → Add project from disk → `kestrel-heavy/unity`**. Let the bootstrap run (or **Kestrel → Setup Project**).
4. Window → General → Test Runner → EditMode → **Run All**. Expect 17/17 green, then fill in RESULTS.md.
5. Build Profiles → iOS → Switch Platform → Build to `unity/build/iOS`. Open `Unity-iPhone.xcodeproj`, check signing (auto, team 9TR6R5LV8M, `.unityproto`), and Run on a device.
6. **Apple GameKit plug-in:** clone `apple/unityplugins` → `python3 build.py -p Core GameKit` → add the Core tgz, then the GameKit tgz, via Package Manager.
7. **ASC (one time):** create the "Kestrel Heavy Proto" app record (`.unityproto`, SKU `kh-unityproto`), add the test leaderboard `kh.unityproto.meco_alt_m`, and add yourself to TestFlight Internal.
8. **CI secrets** (only if the CI path is approved): UNITY_LICENSE/EMAIL/PASSWORD. Possibly regenerate the ASC key as Admin if App Manager signing fails.
9. Fill in the RESULTS.md device rows: fps, IPA size, cold start, hand edits.
10. **At U9:** in Xcode/ASC, archive on `com.beckify.kestrelheavy` and submit to review (gated).

## 6. Approval gates (ask Trevor first)
| # | Gate | When |
|---|---|---|
| G1 | **Xcode project / signing** changes, ASC API key role change, archive/upload of any new bundle | U2 first TestFlight, U9 |
| G2 | **App Store record** changes: new proto record, leaderboards, metadata, screenshots, pricing, the bundle swap to `com.beckify.kestrelheavy`, submit for review | U2, U9 |
| G3 | **Paid assets** (Asset Store, fonts, audio, Unity Pro/Plus) | any |
| G4 | **Ongoing costs**: Fly.io narrator backend, ElevenLabs generation/plan, macOS CI minutes beyond the free tier | U7, CI |
| G5 | **iPad `UIRequiresFullScreen`** (portrait-only iPad means giving up Split View) | U2 |
| G6 | **Unity automation surface**: anything outside the Unity CLI, e.g. GameCI/`-batchmode` in GitHub Actions, or a Unity AI/MCP connector | before enabling CI Unity jobs |
| G7 | Retiring the Phaser/WKWebView `ios/` target from main (kept on `release/1.0.1`) | U9 |

## 7. Risks
- The ToS §17.2 reading of CLI vs CI → G6. Fallback: Trevor builds locally (§5).
- The mission.js arcade rewrite (~2.5k C#) is the largest scope. Cut it to what the designer game needs.
- Apple GameKit plug-in signature drift (`AppleGameCenter.cs` has never been compiled).
- IPA ≤40 MB with URP + audio: audit at U4 and U7.
- VoiceOver/Dynamic Type in UI Toolkit is a known gap, budgeted in U9.
