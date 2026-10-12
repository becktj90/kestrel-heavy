# Environment Realism audit (R1–R7)

_Audit only, no feature code. Oct 12 2026, main @ 7117114. Brief: Trevor via Chief of Staff, adapted to Unity 6.3 LTS / URP 17.3, portrait only (iPhone + iPad), 120 Hz targets, **$0** (CC0 or procedural only, ledger in `LICENSES.md`), no real trademarks, reduced-flash safety. Phaser / WKWebView / landscape parts of the brief don't apply._

## 1. Current state

| Area | What exists | Where | Gap vs brief |
|---|---|---|---|
| **Sky** | Camera clear = flat solid colour `(0.45,0.62,0.85)`. U2 `ThemedFillCamera` colours big-screen fill from `Spec.ThemedFill(TodId, SpaceT)`. `data/tod.json` exports the web TOD palettes (dawn/noon/golden/dusk… gradient stops); VISUAL_SPEC §4 defines sky/stars/clouds/haze alphas vs altitude `t`. | `SliceApp.cs:35`, `Shell/ThemedFillCamera.cs`, `data/tod.json`, `data/visual-spec.json` | No skybox, no scattering, no sun disc, no stars/Moon/planets, no sky→space fade in the flight scene (U4 item). Gradient stops are artistic, not physical. |
| **Astronomy** | None. Sun is a fixed directional light, intensity 1.3. | `SliceApp.cs:37` | No sun position from site lat/long + date/time, no Moon phase, no star field. |
| **Terrain / site** | 4 km × 4 km Unity `Plane` primitive tinted green, parented to the floating-origin `World` root. Floating origin re-centres every 2 km (`FlightDriver.cs:97-102`); #76 fixed the chase camera against it. | `SliceApp.cs:41-44`, `FlightDriver.cs` | No heightfield, no coastline/sea, no Earth curvature (U4), no pad complex (U6/G2-G3). Ground vanishes beyond 2 km; at altitude the camera sees void. |
| **Materials** | Two generated materials: `KH_Body` (URP/Lit, smoothness 0.55, livery tint per instance) and `KH_Plume` (URP Particles/Unlit). `data/materials.json` holds structural material data (sim, not rendering). | `SliceBootstrap.cs:94-95` | No PBR texture sets, no per-part materials (steel/composite/tile/frost), no weathering, no ground/concrete/water shaders. |
| **Clouds / atmosphere FX** | None (VISUAL_SPEC has a clouds alpha curve only). | — | No cloud layers, haze, fog, condensation. |
| **Weather** | `Kestrel.Core.Flight.Weather.Hold()` = wind > 12 m/s → hold, KID/Relaxed never hold. Its own comment: "This is not a weather model." Timeline has built-in holds at T-2 h / T-4 min (`TimelineClock`), `holdable` on `MissionEvent`. | `Kestrel.Core/Flight/Weather.cs`, `Timeline/*` | No weather state, no seeded generator, no commit criteria beyond one wind limit, no upper-level winds into the sim, no visuals, no link to narration/telemetry. |
| **Quality / perf** | G1 presets low/medium/high (+ultra 1.1) in `data/quality.json`; all `targetFps: 60`, fallback 30; p95 governor (step down at 20 ms). `Application.targetFrameRate = 60` in both `SliceApp` and `AppShell`. Physics fixed at 1/120 s. | `data/quality.json`, `QualitySetup.cs`, `AppShell.cs:32` | 120 Hz isn't wired: no ProMotion target, no 8.3 ms budgets, governor thresholds assume 60. |
| **Licensing** | `NARRATION_LICENSE.md`, `MUSIC_LICENSES.md` (X2). | docs/ | No `LICENSES.md` asset ledger yet. |

## 2. Frame and memory budgets per device tier

120 Hz is a target on ProMotion devices only; everything else is 60. The sim stays 1/120 s regardless (`FixedStepper`). Budgets are for the **environment share** of the frame (sky + terrain + clouds + weather FX + post); the rest goes to vehicle, plume, UI and sim.

| Tier (G1 preset) | Example devices | Display target | Frame | Env GPU share | Env CPU share | Env texture + mesh memory | Total app memory ceiling |
|---|---|---|---|---|---|---|---|
| **Low** | iPhone SE 2/3, iPhone 11, base iPads (A13–A14, 3–4 GB) | 60 fps, fallback 30 | 16.7 ms | ≤ 4.0 ms | ≤ 1.0 ms | ≤ 48 MB | ≤ 900 MB |
| **Medium** | iPhone 12–15 non-Pro, iPad Air (4–6 GB) | 60 fps | 16.7 ms | ≤ 5.0 ms | ≤ 1.2 ms | ≤ 96 MB | ≤ 1.3 GB |
| **High @120** | iPhone 13 Pro+ / iPad Pro (ProMotion, ≥ 6 GB) | 120 fps when cool, drop to 60 under thermal | 8.3 ms (16.7 ms at 60) | ≤ 2.5 ms @120 / ≤ 6.0 ms @60 | ≤ 0.8 ms | ≤ 160 MB | ≤ 2.0 GB |

Rules: p95, not mean; measured in flight at Max-Q with full cloud deck (worst case). Governor needs a 120 Hz rung: step 120 → 60 on p95 > 9.5 ms for 3 s, recover after 30 s ≤ 8.0 ms. IPA cap (U9, ≤ 40 MB) means environment assets ≤ **8 MB compressed** in the bundle; everything else is procedural.

## 3. Technique options per tier

| Feature | Low | Medium | High (@120 must fit 2.5 ms) |
|---|---|---|---|
| **Sky** | Gradient skybox driven by `tod.json` stops + altitude `t` (one fullscreen pass, ~0.2 ms) | Precomputed Bruneton/Hillaire-style transmittance + sky-view LUT, baked offline per sun angle into a small 2D LUT set (CC0 code we write), sampled in a custom URP skybox shader | Same LUTs, refreshed at runtime for continuous sun angle (sky-view LUT 192×108, updated every N frames); aerial perspective on terrain |
| **Sky → space** | Lerp to black + star cubemap alpha by `t` (VISUAL_SPEC §4) | Same, LUT altitude axis | Same + limb glow from LUT |
| **Astronomy** | Sun direction from date/lat/long (NOAA algorithm, pure C# in Core, golden vectors); fixed Moon | + Moon position/phase, bright-star catalogue points (≤ 2,000 stars from public-domain Yale BSC) | + planets (low-precision VSOP-lite), Milky Way band texture |
| **Terrain / site** | Procedural heightfield, 3 LODs, vertex colour + 1 tiling detail texture; flat sea plane | 2 CC0 PBR sets (sand/scrub, concrete) via triplanar on slopes; simple water normal scroll | 4 CC0 PBR sets with splat mask, foam line, distance-based quadtree LOD to the horizon + Earth curvature (U4) |
| **Materials (vehicle/pad)** | URP Lit, solid colour + roughness per part type | + CC0 detail normal/roughness for steel, composite, tile, frost (procedural frost mask by tank temp) | + soot/scorch accumulation per flight, clearcoat-like spec on livery |
| **Clouds** | 2 billboard/cloud-card layers, alpha by VISUAL_SPEC curve | 2D cloud layer: raymarch-free noise sheet with fake lighting, cloud deck height from weather state | Low-step (≤ 16) raymarched volumetric layer at reduced res (¼) + temporal reprojection; fly-through on ascent |
| **Weather visuals** | Tint + fog density + rain streak overlay (UI layer) | + wind-driven cloud motion, ground wetness (roughness), particle rain | + lightning (reduced-flash rules apply), anvil clouds |
| **Post** | none | tonemapping (Neutral/ACES) | + bloom (High only, existing G1 flag), exposure adaptation across sky→space |

All shaders/LUT bakers written in-repo; no Asset Store packages (avoids licence and cost questions).

## 4. Weather sim (deterministic, ties into commit criteria)
- `Kestrel.Core.Weather` (no Unity types): `Generate(site, dateSeed, difficulty) → WeatherTimeline` over the count window. Seeded, byte-identical JSON, golden vectors like every Core PR.
- State per step: surface wind (speed/gust/dir), upper-level wind shear profile (feeds `AscentSim` drag/steer as a disturbance, golden-checked), ceiling/cloud cover, precipitation, lightning distance, temperature (affects LOX load events), visibility.
- **Launch commit criteria**: fictional rule set modelled on public range rules (surface wind, upper shear, lightning within N km, cumulus/anvil distance, temperature, flight-through-precip). Each violated rule emits `MissionEvent` statuses (`held`, `delayed`, `scrub`) through the existing bus — **no new event schema** (DECISIONS #30).
- KID/Relaxed: weather is visual only, never holds (keeps current `Weather.Hold` contract).
- Subscribers: timeline holds/recycle (K5/K6), narrator lines via cue ids (U7), wind/rain/thunder SFX (U7), telemetry channels `wx.*` (U4 registry), economy scrub refund hooks (U8).

## 5. Reduced-flash safety
- Lightning, ignition flash, sun glints and staging flashes obey: ≤ 3 flashes per second, flashing area ≤ 25% of screen, red-saturated flashes avoided (WCAG 2.3.1 / Harding-style thresholds).
- Honour iOS **Reduce Motion** and an in-game "Reduce flashing" toggle (default on when Reduce Motion is on): lightning becomes a soft sky brighten over ≥ 300 ms; no strobing of exposure adaptation.
- QA check: automated luminance-delta test on captured frames in the U6 golden-image gallery.

## 6. CC0 / $0 source list
Ledger: every imported file gets a row in `LICENSES.md` (file, source URL, author, licence, date, SHA-256). Only CC0 / public domain; no CC-BY unless Chief of Staff approves attribution (decision 5).

| Need | Source | Licence |
|---|---|---|
| PBR ground/concrete/metal/rock texture sets | ambientCG (ambientcg.com) | CC0 |
| PBR textures + HDRIs (reference/lookdev only) | Poly Haven (polyhaven.com) | CC0 |
| Simple props (fences, tanks, towers) | Kenney (kenney.nl), Quaternius | CC0 |
| Elevation for generic coastlines (shape inspiration, not real sites) | NASA SRTM / USGS 3DEP | Public domain (US Gov) |
| Star catalogue | Yale Bright Star Catalogue (BSC5) | Public domain |
| Earth textures / night lights | NASA Visible Earth / Blue Marble, Black Marble | Public domain (no NASA insignia/logos) |
| Sky model maths | Hillaire 2020, Bruneton 2008 papers; our own implementation | Algorithms, no code copied |
| Noise (clouds/terrain) | Own procedural (Perlin/Worley/curl) | Ours |
| Weather/commit-criteria reference | Public range launch-weather rules (e.g. 45th Weather Squadron documentation) | Facts only, wording original |

No real trademarks: sites are fictional ("Haven", existing world bible names); no real pad names, logos, range insignia, NASA meatball, company liveries.

## 7. Milestones R1–R7 and sequencing

| R | Scope | Lands in / depends on | Owner | Collision notes |
|---|---|---|---|---|
| **R1** Audit + `LICENSES.md` ledger + perf budgets + 120 Hz rung spec | Now (this doc); `quality.json` 120 Hz fields land with U2 follow-up | Kestrel Arcade, Graphics Dev | `quality.json` and `AppShell.cs` are U2 files → land through Graphics Dev's U2 lane only. |
| **R2** Astronomy core: sun/Moon/stars position (pure C#, golden vectors) | Parallel to U1 (Core); no render dependency | Gameplay Dev, Physics | New folder `Kestrel.Core/Astro/`; doesn't touch `FlightDirector.cs`/`Events.cs`. |
| **R3** Physically based sky + sky→space + tonemapping, tier ladder | Inside **U4** render (sky→space fade already a U4 item) after U2 | Graphics Dev | Replaces `SliceApp` solid clear + `ThemedFillCamera` colour source → coordinate so themed fill samples the same sky (VISUAL_SPEC §5). |
| **R4** Procedural sites + PBR terrain/pad materials + Earth curvature | With **U6** (G2/G3 seeded pad generator) and U4 curvature | Graphics Dev, Art | Pad generator owns layout; R4 owns surfaces/terrain. One lane on `SliceBootstrap`/materials at a time. |
| **R5** Weather core: seeded generator + commit criteria → `MissionEvent` holds/scrubs + upper winds into sim | Core after **K3**, before U6 holds/recycle; X4 already lists weather/commit criteria — R5 *is* X4's weather item | Gameplay Dev, Physics, QA | Must use K2 `MissionEvent`; no parallel schema (R14). Upper-wind sim changes regenerate goldens → Physics ε review. |
| **R6** Clouds + weather visuals + reduced-flash | U4/U6 render, after R3 + R5 | Graphics Dev, UX (a11y) | Plume (G4/G5) and cloud passes share particle/transparent budget; joint perf test. |
| **R7** Hookups: narrator lines, SFX, music stingers, telemetry `wx.*`, Codex entries, golden-image gallery + perf gate on devices | **U7** (audio/narrator), K6 subscribers, U6 gallery, U9 perf | Sound, Narrator Voice, Codex, QA | Narration clips are pre-generated ElevenLabs (paid Creator plan) → new weather lines need a batch; no new spend. |

Order: **R1 → (R2 ∥ U1/K3) → R5 → R3 (U4) → R4 (U6) → R6 → R7 (U7) → U9 perf gate.** No R milestone adds to the U1→U3 critical path; R3/R4/R6 add render scope to U4/U6 and are the schedule risk.

**2.0 vs 1.1 proposal:** 2.0 = R1, R2 (sun + Moon + stars), R3 Low/Medium, R4 one site, R5 full (it's gameplay), R6 Low/Medium clouds, R7 core lines. 1.1 = High-tier volumetric clouds, runtime LUT refresh, planets, extra sites, lightning visuals beyond the safe minimum.

## 8. Scope decisions for Chief of Staff
1. **120 Hz:** target 120 fps only on High (ProMotion, cool device) and keep Low/Medium at 60, with a governor rung 120 → 60? Or 60 everywhere for 2.0 and 120 in 1.1?
2. **Sites for 2.0:** one fictional coastal site (Haven) only, or Haven + a second inland/desert site?
3. **Sky model:** baked LUT physical sky (Medium/High) with the existing `tod.json` palettes kept as the Low fallback and art-direction target — or physical-only and retire the palettes?
4. **Volumetric clouds:** ship raymarched volumetrics on High in 2.0, or defer to 1.1 and ship 2D cloud layers on all tiers?
5. **Licences:** CC0/public domain only, or also allow CC-BY with an in-app credits screen?
6. **Real-world data:** may sites be *inspired by* public-domain elevation (SRTM) while staying fictional and unnamed, or fully procedural only?
7. **Weather in difficulty modes:** commit-criteria holds in Standard and Engineer, visual-only in KID/Relaxed (current rule) — confirm, and should Standard get a "launch anyway" override with a reliability penalty?
8. **Upper-level winds in the sim:** feed shear into `AscentSim` for 2.0 (changes golden vectors, needs Physics ε review), or visual/commit-only in 2.0?
9. **Real date/time:** sun/Moon from the player's real clock and date, from a mission-set date, or a player-chosen time of day?
10. **Reduced flashing:** default the "Reduce flashing" toggle ON for everyone, or only when iOS Reduce Motion is on?
11. **Asset budget:** accept ≤ 8 MB compressed environment assets inside the 40 MB IPA cap, or tighter?
12. **Narration:** OK to add a batch of weather/hold lines to the pre-generated ElevenLabs library (existing Creator plan, no new spend), or cover weather with on-device TTS only?
