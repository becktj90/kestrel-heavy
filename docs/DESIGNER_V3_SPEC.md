# Designer v3: every knob flies

Status: Spec on main; WP-A–G merged. WP-UI in progress (portrait designer v3). WP-H next. Owner: Trevor Beck. Target: Kestrel Heavy 2.x, Unity 6.3 LTS, portrait only.
Base documents:
- Trevor's **Rocket Designer Overhaul** prompt (2026-10-11), ported from Phaser/WKWebView to Unity C# (`Kestrel.Core`).
- The Economy/Contracts prompt.
- The Starter Templates + Hangar Reveal prompt.
- `/workspace/kh-audit/AUDIT.md` (main @ ac5eed2).
- `docs/DESIGNER_SPEC.md` (Hangar G3) and `docs/HANGAR_REVEAL_UNITY_PLAN.md`.

The designer is the most important part of the game. A kid can bolt on a big engine and watch it fly or blow up. An engineer finds that chamber pressure, expansion ratio and tank walls behave as they expect.

**Pillars**
1. **Every option changes the physics.** Anything cosmetic sits in the "Look" tab.
2. **Bad designs fail in flight.** Each failure has a clear cause, a readable visual and a post-flight explainer. The readout warns before launch.
3. **The payload drives the mission.** It sets mass, volume and fairing fit, and the target orbit or Δv. A campaign ramps up the difficulty.
4. **Honest physics:**
   - fixed-step RK4
   - ISA 1976 atmosphere
   - Cd(Mach)
   - max-Q
   - inverse-square gravity
   - rotating Earth
   - orbit determination
   - landing and reuse

## 0. Audit (main, Oct 12 2026)

### 0.1 Overhaul-prompt port status (JS → Unity C#)
| Overhaul item | Unity C# today | State |
|---|---|---|
| RK4 fixed step + accumulator | `Physics/Integrator.cs` | done (M1) |
| ISA 1976 to 86 km + upper table | `Physics/Atmosphere.cs`, `UpperAtmosphere.cs` | done |
| Cd(Mach) | `Physics/Aero.cs` (one curve, cd0 from `Stack.BaseCd`) | done (v3 extends it) |
| Inverse-square gravity, rotating Earth | `Physics/Gravity.cs`, `Frames.cs`, `Geopotential.cs`, `LaunchAzimuth.cs` | done |
| Orbit elements | `Physics/Orbit.cs`, `Kepler.cs`, `OnRails.cs`, `Handoff.cs` | done |
| Rocket equation, ṁ = F/(Isp·g0) | `Physics/RocketEq.cs` | done |
| Headless sim + CLI | `Physics/Headless.cs` (2-D), `Ascent3Dof.cs`, `Cli/` | done |
| Design schema, mass, stack, crossfeed | `Vehicle/Design.cs`, `Mass.cs`, `Stack.cs`, `InterstageAero.cs` | done (v1/v2) |
| Catalogs | `data/*.json` via `Data/Catalog.cs`: 20 engines + nozzle variants, 7 propellants, 6 materials, 7 tanks, 5 fins, 6 fairings, 5 interstages, 6 payloads | done |
| Readout | `Designer/Readout.cs`: Δv vac/SL, TWR, margin, cost, reliability, CG/CP/stability, cached max-Q | done |
| Validation + "Make it fly" | `Designer/Validate.cs`, `DesignRules.cs`, `DesignerLimits.cs`, `Experience.cs` (simple/standard/engineer) | done |
| Presets / templates | `data/presets.json` (4), `data/designer-templates.json` (17), T1 gallery | done |
| Hangar model, undo, library | `Hangar/*` (HangarEditor, EditHistory, DesignLibrary, Advisories, DataSheet) | done |
| Customizer UI (G3) | **#176**, **#180** (drafts) | in-PR |
| Flight: autopilot/gravity turn, staging, telemetry HUD | partial (Slice flight, Timeline, Sensors, LaunchPredictor) | M3 partial |
| Structure loads, failures, post-flight report | **missing**: no FailureModel; max-G is only a design rule | planned (v3) |
| Economy: costs | `Economy/CostModel.cs` | done |
| Contracts, tech tree, career, reuse economy | `Flight/MissionRamp.cs` + `mission-ramp.json` only | planned |
| Save | `Save/*` (merged #5) | done |
| Hangar reveal | `docs/HANGAR_REVEAL_UNITY_PLAN.md`, HangarCamera/Orbit/Explode | partial; #179 in-PR |
| Rocket visual detail | #186 merged; #187–#192 drafts | in-PR |

### 0.2 Options that reach physics today
- **Feed physics:** thrust/Isp/mass (EnginePoint + nozzle variant), engineCount, propMass, diameter (tank length, drag area), material.structuralFraction, safetyFactor, interstage, legs/gridFins/rcs/avionics (mass), nose/fins/strap-on drag, payload mass/orbit, tune%.
- **Loaded but no performance effect:**
  - Engine fields: `pc`, `expansionRatio` (cost/visual), `gimbal`, `throttle` (validation/visual).
  - Material fields: `yieldMPa`, `maxTempK`, `cryoOK`, `density`.
  - Other fields: `boiloffPerHour` (cost), fairing `cd`, interstage `dragDeltaCd` (limits only), payload `volume`, `maxHeatFlux`.
- **No failure modes.** Headless never ends a flight for loads, instability, thermal, separation or landing.
- **No guidance-profile options** for the player.

### 0.3 Open PRs (do not duplicate)
- #176/#180: Hangar G3 UI (`Kestrel.Slice/Runtime/Hangar/*`, DesignerValidation.cs, SliceBootstrap.cs).
- #179: HangarCamera/HangarOrbit/SliceApp/KH_Sky.
- #187–#192: vehicle art (`Kestrel.Game/Vehicle/*`, RocketMeshBuilder.cs, SliceApp.cs).

v3 packages must not edit these files until those PRs merge. WP-UI and WP-FX are gated on them.

## 1. Milestones
| M | Overhaul scope | Status | v3 WPs |
|---|---|---|---|
| M1 | Physics core + catalogs | **done (merged)** | WP-A extends the catalogs |
| M2 | Designer (VAB): readout, validation, presets | **done (merged)**; G3 UI in #176/#180 | WP-A..D deepen, WP-UI |
| M3 | Flight: autopilot/gravity turn, staging, orbit HUD, telemetry, arcade adapter | partial | WP-D (guidance), WP-F (telemetry) |
| M4 | Failures, loads, post-flight report, loss accounting | planned | WP-E, WP-F |
| M5 | Economy, contracts, tech tree, reuse | partial (cost only) | WP-G |
| M6 | Polish, tutorial, perf, save | partial (save done) | WP-UI (tutorial), WP-H (templates + reveal + perf) |

## 2. Option categories and physics
g0 = 9.80665. Every new field is optional. With no v3 fields, results must match v2 bit-for-bit: all existing goldens stay unchanged.

### 2.1 Propellants
Catalog fields: bulkDensity, mixtureRatio, Isp ceiling, storability, boiloffPerHour.

Player option: **mixture-ratio offset ±10%**.
- Isp penalty: −0.6% per 1% off nominal.
- Beyond ±6%: combustion-instability hazard.

Cryogenic propellant + a material with `cryoOK=false` → **hard fail at T+0**.

### 2.2 Engines (per stage)
- **cycle:** sets the pc range, mass factor k_cycle (pf 0.8, gg 1.0, exp 1.05, sc 1.15, ff 1.25), base reliability and restarts.
- **chamber pressure pc** (pcMin..pcRated×1.15):
  - Isp_vac = Isp_ref·(1+0.08·ln(ε/ε_ref))·(pc/pc_ref)^0.02
  - F ∝ pc
  - mass ∝ k_cycle·(pc/pc_ref)^0.5
  - overpressure hazard λ = 0.02·max(0, pc/pc_rated−1)² per second
- **expansion ratio ε** (8..120; vacuum engines to 250):
  - Isp_SL = Isp_vac − p_a·A_e/(ṁ·g0), with A_e = ε·A_t
  - exit diameter ∝ √ε, which feeds the PACK fit check
  - flow separation when p_e < 0.4·p_a: Isp_SL −15% plus a side-load hazard
- **throttle min/max:** used by guidance for the max-Q bucket and the g-limit. Deep throttle below 40% costs +1% engine mass per 10%.
- **gimbal δ** (0..catalog max): control moment M_ctrl = N·F·sin δ·L_arm. Mass +0.4% per degree.
- **count + cluster pattern** (1–40):
  - engine-out tolerance: N ≥ 7 with redundant avionics survives one out if TWR_after > 1.05
  - N > 9 without the base heat shield: base-heating hazard
- **tune thrust% / Isp%** (existing): reliability −0.5% per +1% over nominal.

### 2.3 Tanks
- **Material:** σ_y, ρ, maxTempK, cryoOK.
- **Pressurization p_t** (1.5–6 bar):
  - wall thickness t = SF·p_t·r/σ_y (hoop), minimum gauge 1 mm
  - mass = ρ·2πrL·t + 2 domes
  - adds axial stiffening of p_t·πr²
- **Insulation** none / foam / MLI:
  - boil-off multiplier ×1 / ×0.4 / ×0.15
  - mass 0 / 1.2 / 2.5 kg/m²
  - coast loss Δm = m·(1−(1−r_eff)^h)
- **Common bulkhead:** −8% tank mass. If Δp > 2 bar, +structural hazard.
- **Fineness:** L/D > 18 → buckling warning; > 22 → red.

### 2.4 Stages and boosters
- 1–5 core stages, plus strap-on count {0, 2, 3, 4, 6, 8}, solid or liquid.
- Crossfeed / asparagus: +2% booster dry mass for plumbing.
- **Separation mode:** cold / hot / springs, with sepImpulse.
  - Collision if v_rel < 0.5 + 0.02·q_kPa (m/s) within 2 s.
  - Hot staging without a vented interstage → overpressure fail.
- **Booster sep:** at burnout, or at a manual % of the booster burn.

### 2.5 Aero
- **Drag build-up:** Cd0 = Cd_nose + Cd_fairing + ΣCd_fin + Cd_grid + 0.03·N_strap + ΣdragDeltaCd_interstage, then Cd(Mach) via `Aero.CdMach`.
  - v3 replaces the hard-coded terms in `Stack.BaseCd` with catalog values only when v3 fields are present.
- **Nose:** ogive / conic / blunt / von-karman / biconic / hammerhead. Each sets Cd0, the CP offset and a heating factor.
- **Fins:** planform, count 3–8, span, root chord.
  - Barrowman CN_α feeds `Balance` CP.
  - mass = areal density × area.
- **Grid fins:** stowed Cd 0.01; deployed in descent for control.
- **Stability:** S(t) = (x_cg(t) − x_cp)/D, with the CG following the drain.
  - S < 0 and control-authority ratio CAR < 1 → tumble.

### 2.6 Structure
- **Allowable axial load:** F_allow = σ_y·2πr·t/SF + p_t·πr².
- **Load:** F = m_above·a_axial + q·S·Cd + bending from M_b = q·S·CN_α·α·|x_cp − x_cg|.
- **Margin of safety:** MS = F_allow/F − 1. MS < 0 → structural failure.
- **Max-Q limit:** q_lim = 35 kPa·(SF/1.4)·(σ_y/500). The fairing uses its own material.
- **Over-g:** a > payload.maxG damages the payload. a > 8 g breaks up the vehicle.

### 2.7 Avionics and guidance
- **Avionics tier** basic / standard / redundant:
  - mass 60 / 150 / 300 kg
  - reliability ×0.97 / 1 / 1.02
  - engine-out logic only on redundant
- **Guidance profile:**
  - pitchKickAlt, pitchKickDeg
  - turnShape 0..1
  - maxQThrottle %, gLimit
  - target orbit and closed-loop insertion (PEG-lite)
- **Control authority:** CAR = M_ctrl/M_aero at α = 4°. RCS adds authority above 60 km.
- **Losses** are reported as gravity, drag and steering, from integrator accounting.

### 2.8 Recovery
- **Legs:** +6% S1 dry. Light legs: v_leg 2 m/s; heavy legs: 4 m/s and +9%. Touchdown v_z > v_leg → leg collapse.
- **Landing reserve:** 0–20% of S1 propellant.
  - Need: Δv_land ≈ v_term + g·t_burn·(1 − 1/TWR_land).
  - Short reserve → hard landing (`LandingGrade`).
- **Recovery mode:**
  - RTLS boostback Δv ≈ 2·v_h(MECO)
  - barge "Haven" ≈ 0.3·v_h(MECO)
  - expendable

### 2.9 Fairing and payload
- **Fit:**
  - payload volume ≤ π(0.45·D_f)²·0.8·L_f
  - payload diameter + 0.2 m ≤ D_f
  - otherwise launch is blocked
- **Jettison altitude:** after jettison, ½ρv³ < payload.maxHeatFlux, else the payload is damaged.
- Payload mass, maxG and orbit come from the mission (§4).

## 3. Failure model (pillar 2)
New file `Kestrel.Core/Flight/FailureModel.cs`.
- Pure, deterministic and allocation-free.
- Random draws are seeded by (design hash, mission seed).
- It is evaluated at every Headless/Ascent3Dof step. It emits the first `FailureEvent {t, kind, stage, engineIndex, cause, value, limit, fixHint}`.
- Debris then continues ballistically.

| Kind | Trigger | Random? | Visual | Fix hint |
|---|---|---|---|---|
| structural-maxq | MS < 0 or q > q_lim | no | stack snaps at the weakest joint (interstage/fairing) | raise SF, use a stronger material, lower maxQThrottle |
| overg | a > min(maxG, 8 g) | no | payload crumples / break-up | throttle down, add gLimit |
| tumble | S < 0 and CAR < 1 for 0.5 s, or α > 15° at q > 20 kPa | no | real attitude spin, then break-up | bigger fins, more gimbal, move the mass up |
| engine-overpressure | pc > 1.15·rated; hazard λ | yes | bell flash, that plume cuts out | lower pc |
| bad-mixture | MR offset > 6% | yes | sputtering green-tinted plume | return to nominal MR |
| thermal | base heating index > 1, or nose T > maxTempK | no | glowing skin, burn-through | heat shield, different nose |
| cryo-material | cryo propellant in a !cryoOK tank | no (T+0) | frost cracks on the pad | change material |
| prop-depletion | stage empty below target | no | flame-out, "out of fuel" | more propellant / stage |
| separation-collision | v_rel < v_min within 2 s | no | stages bump, then tumble | more sepImpulse, springs |
| leg-collapse | v_z > v_leg | no | leg folds, booster tips over | heavier legs, more reserve |
| engine-out | per-engine reliability per burn | yes | one plume off; survive if tolerant | more engines + redundant avionics |

- **Gentle physics:** default on in campaign tiers 1–3 and on the kid layer. Random failures are off and deterministic ones stay on.
- **Pre-launch risk list:** the debounced ReferenceAscent runs FailureModel in predict mode.
  - Each risk shows green/amber/red with its margin.
  - A red risk shows a "Launch anyway" confirm.
- **Post-flight report:**
  - an explainer card with an icon, a kid sentence and an engineer line with numbers
  - a "Fix it" deep link to the field
  - telemetry plots (altitude, velocity, q, g, Δv losses) with event markers, failure point highlighted

Example:
> Your rocket bent in half at T+62 s. The air pushed 41 kPa, but your tanks could only take 33 kPa. Try safety factor 1.6 or max-Q throttle 70%.

## 4. Missions, economy and campaign (pillar 3)
New file `data/campaign.json`. It extends `mission-ramp.json` and `Flight/MissionRamp.cs`.

A mission is `{payloadId, mass, volume, orbit (alt/inc) | Δv | apogee, requirements[], budget, reward, unlocks[]}`.

| # | Mission | Payload kg / m³ | Target | Requirements | Budget | Unlocks |
|---|---|---|---|---|---|---|
| 1 | First Hop | kid-box 50 / 0.2 | 10 km apogee | — | 2 M | fins, nose |
| 2 | Edge of Space | kid-box 100 / 0.2 | 100 km apogee | — | 5 M | stage 2 |
| 3 | Tiny Orbit | cubesat 20 / 0.05 | LEO 200 km | margin ≥ 0 | 15 M | nozzle ε |
| 4 | Weather Eye | leo-sat 2,500 / 12 | 500 km, 51.6° | g ≤ 6 | 40 M | pc, throttle |
| 5 | Heavy Lifter | leo-sat 12,000 / 40 | 400 km | strap-ons OK | 90 M | crossfeed |
| 6 | Come Back | leo-sat 8,000 / 30 | 400 km | S1 lands on the pad, grade ≥ good | 70 M | legs, grid fins |
| 7 | Haven at Sea | gto-comsat 5,500 / 60 | GTO | S1 lands on the barge Haven | 110 M | insulation |
| 8 | Crew Day | crew-capsule 12,000 / 25 | 400 km | g ≤ 4, reliability ≥ 0.98, no red risks, random failures on | 160 M | redundant avionics |
| 9 | Moon Shot | lunar-probe 3,000 / 15 | TLI (C3 −2) | 3 h coast with boil-off | 180 M | hydrolox upper |
| 10 | Big Reuse | gto-comsat 7,000 / 70 | GTO | land S1 + boosters | 120 M | — |

**Pass:** the payload reaches the target within tolerance (alt ±5%, inc ±0.5°, or Δv reached), every requirement is met, and the payload is undamaged.

**Stars:**
- ★ pass
- ★★ margin 2–10% and cost ≤ budget
- ★★★ also a perfect landing when landing is required, else peak g ≤ 80% of the limit

**Score:**
- 1000·pass
- + 300·max(0, 1 − |margin − 0.05|/0.05)
- + 200·max(0, (budget − cost)/budget)
- + landing bonus (perfect 200, good 100)

**Economy:**
- Reward minus cost goes to the bank.
- Recovered stages refund 70% of hardware minus refurbishment (10% + 5% × flights).
- The tech tree unlocks in mission order.
- No timers, no energy, no ads (ENGAGEMENT_SPEC no-dark-patterns).

**Acceptance (sim-verified):** for every mission, **at least 3 distinct viable designs** must pass under budget in `FlightBatch` with Gentle physics off and random failures seeded at 5 seeds. They are stored in `data/golden/campaign-viable.json`, and a CI test runs them.

## 5. Readout v3
Per stage:
- Δv vac/SL
- TWR at start and end
- burn time
- mass ratio
- MS min
- boil-off loss

Vehicle:
- liftoff TWR
- total Δv vs mission need (margin %)
- max-Q vs q_lim
- peak g vs limit
- min stability over the burn
- min CAR
- landing reserve vs need
- fairing fit
- cost vs budget
- reliability
- risk list

Each control shows a live delta chip, for example "+42 m/s Δv · −0.03 TWR".

Budget: instant part p95 < 0.5 ms (ReadoutBench). The predict ascent stays debounced (300 ms) and cached.

## 6. Portrait UI (extends G3 #176/#180, does not replace it)
- Top ~45%: rocket in the per-part/exploded view. Tapping a part opens its tab.
- Sticky strip:
  - Δv bar vs target
  - TWR
  - max-Q gauge
  - cost/budget
  - risk chips
- Bottom tab bar: **Propellant · Engines · Tanks · Stages · Aero · Structure · Guidance · Recovery · Payload · Look**.
  - Simple view: 3–4 kid controls with plain words.
  - Engineer view: every field with units.
  - Layers follow `Experience.cs`.
- Supported devices: iPhone SE (375 pt) through Pro Max and iPad portrait. Safe areas, VoiceOver, Reduce Motion, 44 pt targets.
- Tutorial (M6): a guided 200 km LEO build.

## 7. Starter templates and hangar reveal
- Keep the 17 `designer-templates.json` templates and the T1 gallery.
- Add v3 templates showing each pillar, all sim-verified:
  - "Wobbly Pencil" (tumbles)
  - "Glass Cannon" (max-Q break)
  - "Thirsty" (runs dry)
  - "Lander"
  - "Haven Hopper"
  - "Crew Safe"
- Hangar reveal per `HANGAR_REVEAL_UNITY_PLAN.md`:
  - ≤ 6 s, skippable camera beats
  - cutaway/inspect hooks reuse #187–#192 layouts
  - perf budget from §5 of that plan

## 8. Golden vectors (`data/golden/designer-v3.json`)
| ID | Input | Expected |
|---|---|---|
| GV1 | Tsiolkovsky m0 550 t, mf 150 t, Isp 311 | 3962.6 m/s |
| GV2a–d | Isp(ε, pc) ref 350 s/ε34/300 bar at (34,300),(50,300),(80,300),(34,200) | 350.00, 360.80, 373.96, 347.17 s |
| GV3 | q at ρ 0.4135, v 300 | 18.608 kPa |
| GV4 | boil-off 100 t, r 0.002/h, foam ×0.4, 3 h | 99,760.2 kg |
| GV5 | hoop wall SF 1.4, 3 bar, r 1.85, σ 500 MPa | 1.554 mm |
| GV6 | gimbal moment 9 × 2.3 MN, 5°, arm 30 m | 54.12 MN·m |
| GV7 | every preset/template without v3 fields | identical to designer-readout.json, simulate.json, catalog-flight.json |
| GV8 | hydrolox in a !cryoOK material | cryo-material at t = 0 |
| GV9 | finless, top-heavy preset | tumble predicted red; FailureEvent kind tumble |
| GV10 | SF 1.1 al-li, maxQThrottle 100% | structural-maxq; q > q_lim |
| GV11 | each campaign mission | ≥ 3 viable designs pass (campaign-viable.json) |

## 9. Work packages (all cloud-agent work; disjoint file ownership)
Paths are relative to `unity/Assets/Kestrel.Core` unless stated otherwise. Interfaces are fixed in §2–§5. Stub other WPs' interfaces in your own tests only.

| WP | Scope | Owns (only these) | Gate |
|---|---|---|---|
| A Schema, propellant, payload | v3 fields + v2→v3 migration, payload fit, MR offset | `Vehicle/Design.cs`, `Save/DesignMigration.cs`, new `Vehicle/PayloadFit.cs`, `data/schemas/*`, `data/designer-limits.json`, `data/payloads.json`, `data/propellants.json`, new tests `Kestrel.Core.Tests/DesignV3SchemaTests.cs` | none (first) |
| B Propulsion | pc/ε/throttle/gimbal/cycle/tune models | new `Vehicle/EngineModel.cs`, `Vehicle/EnginePoint.cs`, `data/engines.json`, new tests `EngineModelTests.cs` | after A |
| C Tanks & structure | walls, pressurization, insulation, boil-off, allowables, q_lim | new `Vehicle/TankModel.cs`, new `Vehicle/StructureModel.cs`, `Vehicle/Mass.cs`, `data/materials.json`, `data/tanks.json`, new tests | after A |
| D Aero, guidance, recovery | Cd build-up, stability along the burn, CAR, guidance profile, landing need | new `Physics/AeroModel.cs`, new `Flight/GuidanceProfile.cs`, new `Flight/RecoveryModel.cs`, `Vehicle/Stack.cs`, `Vehicle/InterstageAero.cs`, `Designer/Balance.cs`, `data/fins.json`, `data/fairings.json`, `data/interstages.json`, `data/landing.json`, `data/steer.json` | after A |
| E Failure model + readout v3 + goldens | FailureModel, sim hooks, loss accounting, risk predict, readout | new `Flight/FailureModel.cs`, `Physics/Headless.cs`, `Physics/Ascent3Dof.cs`, `Physics/Integrator.cs`, `Designer/Readout.cs`, `Designer/ReferenceAscent.cs`, `Designer/Validate.cs`, `Designer/DesignRules.cs`, new `data/golden/designer-v3.json`, new `Kestrel.Core.Tests/DesignerV3GoldenTests.cs` | after B, C, D |
| F Failure visuals, telemetry, post-flight report | break-up/engine/tumble/leg FX, telemetry recorder, report scene | new `unity/Assets/Kestrel.Game/Failure/**`, new `Kestrel.Slice/Runtime/Report/**`, new `Flight/TelemetryLog.cs`; one registration line in SliceApp after #187–#192 merge | after E and #192 |
| G Missions, economy, campaign | campaign.json, scoring, rewards, reuse refund, tech unlocks, viability batch | new `data/campaign.json`, `data/mission-ramp.json`, `Flight/MissionRamp.cs`, new `Flight/MissionScore.cs`, new `Economy/Career.cs`, `Economy/CostModel.cs`, `Flight/FlightBatch.cs`, new `data/golden/campaign-viable.json` | after E |
| UI Portrait designer v3 + tutorial | tabs, Simple/Engineer views, delta chips, risk chips, budget | new `Kestrel.Slice/Runtime/DesignerV3/**`, new `unity/Assets/UI Toolkit/DesignerV3/**`, new Game.Tests `DesignerV3UiTests.cs` | after #176/#180 and E |
| H Templates, hangar reveal, perf | v3 templates, reveal beats, perf pass | `data/designer-templates.json`, `data/presets.json`, new `Kestrel.Slice/Runtime/Reveal/**`, `Designer/ReadoutBench.cs` | after G and #179 |

Waves:
1. A
2. B ∥ C ∥ D
3. E
4. F ∥ G ∥ UI
5. H

Prompts: `docs/designer-v3/prompts/WP-*.md`.

## 10. Requirements traceability
Legend: **done** = on main; **in-PR** = open PR; **planned** = WP.

### 10.1 Rocket Designer Overhaul prompt
| # | Requirement | Status | Where / WP |
|---|---|---|---|
| O1 | Full VAB | done (M2) + in-PR G3 #176/#180 | UI deepens |
| O2 | Propellant choice (7 families) | done; MR offset planned | A |
| O3 | 1–5 stages + strap-ons, crossfeed | done; sep modes planned | A, D |
| O4 | Engine catalog with tuning | done (tune %, nozzles); pc/ε/throttle/gimbal physics planned | B |
| O5 | Fairing (size, material, jettison) | done; fit + heat planned | A, D |
| O6 | Aero (nose, fins, Cd) | done (partial); build-up/stability planned | D |
| O7 | Structure (material, SF) | done (mass only); allowables planned | C |
| O8 | Live Δv, TWR, margins | done | E extends |
| O9 | Cost and reliability | done | E, G |
| O10 | Validation + Make it fly | done | E adds risk codes |
| O11 | Presets | done | H |
| O12 | Fixed-step RK4 | done | E adds loss accounting |
| O13 | ISA atmosphere | done | — |
| O14 | Cd(Mach) | done | D |
| O15 | Max-Q | done (cached) | C limits, E |
| O16 | Inverse-square gravity | done | — |
| O17 | Rotating Earth | done | — |
| O18 | Orbits | done | — |
| O19 | Landing / reuse | partial (LandingGrade, legs mass) | D, G |
| O20 | Failure modes with causes | planned | E, F |
| O21 | Telemetry | partial | F |
| O22 | Post-flight report | planned | F |
| O23 | Autopilot / gravity turn / PEG-lite | partial | D |
| O24 | Arcade adapter / KID forgiveness | done (Gentle physics planned) | E |
| O25 | Tutorial | planned | UI |
| O26 | Save designs | done | — |
| O27 | Perf (sim < 0.1 ms/step, readout 0.5 ms) | done (bench) | H |
| O28 | Portrait only | done | all |
| O29 | M1 | done | — |
| O30 | M2 | done | — |
| O31 | M3 | partial | D, F |
| O32 | M4 | planned | E, F |
| O33 | M5 | planned | G |
| O34 | M6 | partial | UI, H |

### 10.2 Economy / Contracts prompt
| # | Requirement | Status | WP |
|---|---|---|---|
| E1 | Part costs, recurring price | done (CostModel) | — |
| E2 | Contracts with payload/orbit/budget/reward | planned | G |
| E3 | Tech tree / unlocks | planned | G |
| E4 | Career bank, reuse refund | planned | G |
| E5 | ≥ 3 viable affordable designs per level, sim-verified | planned | G (campaign-viable.json + CI) |
| E6 | No dark patterns | done (policy) | G honours it |
| E7 | Game Center per contract | needs Trevor/ASC approval | not in scope |

### 10.3 Starter templates + hangar reveal prompt
| # | Requirement | Status | WP |
|---|---|---|---|
| T1 | Starter templates + gallery | done (17 + T1) | H adds v3 |
| T2 | Template validation (all fly) | done | H re-verifies under v3 |
| T3 | Hangar reveal camera beats | partial (#179 in-PR) | H |
| T4 | Inspect/cutaway hooks | in-PR #187–#192 | H |
| T5 | Exploded view | in-PR #176/#179 | — |

### 10.4 Trevor pillars (Oct 12)
| # | Requirement | Status | WP |
|---|---|---|---|
| P1 | Many more options, all affecting performance | planned | A–D |
| P2 | Structural / over-g failure | planned | C, E, F |
| P3 | Engine-out / explosion (overpressure, mixture, thermal) | planned | B, E, F |
| P4 | Tumble (CP ahead of CG) | planned | D, E, F |
| P5 | Out of propellant | planned | E, F |
| P6 | Separation collision | planned | D, E, F |
| P7 | Leg collapse | planned | D, E, F |
| P8 | Readable failure visual | planned | F |
| P9 | "Why it failed" explainer | planned | F |
| P10 | Pre-launch risk warnings | planned | E, UI |
| P11 | Payload-driven goals (mass, volume, fit, orbit/Δv) | planned | A, G |
| P12 | Campaign ramp (heavier, higher, tighter, reuse) | planned | G |
| P13 | Mission table + pass/fail scoring | planned | G |
| P14 | Fun for kids + engineers (Simple/Engineer) | planned | UI |
| P15 | Golden vectors | planned | E |
