# Physics Core audit: P1–P8 vs current Kestrel.Core

Status: audit, 2026-10-12, Physics (Grok Bot for Trevor). Baseline `origin/main` 104cc2b.
Scope: `unity/Assets/Kestrel.Core/**` (C#, the 2.0 core) and its JS reference `web/js/physics/**`,
`web/js/flight/**` (golden-vector source, `tools/export-golden.mjs`). Brief: Physics Core P1–P8 (CoS, via Kestrel Arcade).
Owners: Physics owns validation + `PHYSICS.md`; Gameplay Dev implements `Kestrel.Core/**` (MASTER_ROADMAP Owners).

**Numbering.** The brief lists ten topics under "P1–P8". This doc uses P1–P7 for the physics topics and P8 for the
three cross-cutting items (8a Monte Carlo, 8b determinism, 8c validation suite). Rename if CoS meant another split.

**CoS rulings applied.** Architecture: pure C# core, golden vectors, `dotnet` headless CLI (no C++/Rust, no paid libs).
2.0: J2+J3/J4, drag/decay + lifetime, weather + LCC, 3-DOF default / 6-DOF Engineer, reduced-fidelity MC,
eclipses/beta/SRP, third-body only for GTO/GEO secular drift. 1.1+: tesseral geopotential, SGP4/TLE, CW/rendezvous,
lunar patched-conic, elastic modes, detailed radiation. Determinism: bit-exact per platform, tolerance-based across.

## 0. What exists today (one paragraph)
The flight core is a **planar (2-D) point-mass ascent**: state `[x, y, vx, vy, m]`, fixed-step RK4 (`Physics/Integrator.cs:73-89`,
`Headless.cs:97,122`), point-mass gravity (`Gravity.cs:6-15`), USSA76 to 86 km (`Atmosphere.cs`), Mach-dependent Cd
(`Aero.cs:134-145`), Isp vs back-pressure (`RocketEq.cs`), heuristic guidance (vertical → kick → gravity turn → a
closed-loop vertical-rate law on upper stages, `Headless.cs:170-193`), Earth rotation as a site velocity plus a
co-rotating air term (`Headless.cs:121-123`, `Gravity.cs:17-23`). Orbit elements are two-body osculating only
(`Orbit.cs:37-56`). Astronomy (R2/S1, #83) is good: Meeus Sun/Moon, nutation, ΔT, validated vs Horizons
(`Astro/Ephemeris.cs`; Sun 29.8″, Moon 9.5″ worst). Weather is a single wind threshold (`Flight/Weather.cs:114-123`).
There is no coast propagator, no 3-D attitude, no Monte Carlo (`LaunchPredictor.cs:1-3` says so), no CLI.

## 1. Item table

Legend: **present / partial / absent**. Tolerances are |a−b| ≤ abs + rel·|b| unless stated. "Golden" = vector in
`data/golden/*.json` checked by both `npm test` (JS) and `dotnet test` (C#); "reference" = external truth in
`tests/validation/**`.

### P1 Frames, time, WGS-84 — **partial**
- Evidence: WGS-84 a = 6378137 m and μ = 3.986004418e14 (`Gravity.cs:8`, `web/js/physics/gravity.js:2-3`); ω⊕ = 7.2921159e-5
  (`Gravity.cs:19`; WGS-84 is 7.292115e-5, the extra digit is harmless). JD, ΔT, UTC→TT (`Ephemeris.cs:17-41`).
  Apparent RA/Dec of date (`Ephemeris.cs:1-6`).
- Gap: the sim is a 2-D plane with a spherical Earth (`Headless.cs:122`: pad at (0, R⊕)); no ECI/ECEF, no geodetic
  lat/lon/height, no flattening f = 1/298.257223563, no GMST/ERA (so no ground track, no launch azimuth, no
  site→inertial transform), no UT1 (ΔT is a polynomial; fine for a game). Inclination from `Orbit.Elements` is always 0
  in the planar sim. `MinInclination = |lat|` (`Gravity.cs:22`) is the only 3-D notion.
- Golden vectors: (1) geodetic↔ECEF round trip at 0/28.5/45/90° lat, h 0 and 400 km, abs 1e-6 m; published WGS-84
  ECEF of a fixed point (NGA TR8350.2 example), abs 1e-3 m. (2) GMST/ERA (IAU 2000 ERA formula) vs USNO/Horizons
  sidereal time at 10 epochs, abs 0.01 s. (3) ECI↔ECEF of a state vector at 3 epochs vs Vallado example 3-15 (published
  numbers), abs 1 m / 1 mm/s (we drop precession-nutation of the pole: record as "TOD-like, no polar motion").
- Tier: **2.0** (every other 3-D item depends on it). Polar motion, full IAU-2006 CIO: later.

### P2 Gravity J2 + J3/J4, SSO, critical inclination, mean vs osculating — **absent**
- Evidence: point mass only (`Gravity.cs:1,9-14`; `gravity.js:1` "J2 not modeled in M1"). Validation C (#90) checks
  only the *reference* formula, and pins "still point-mass" as a guard.
- Gap: zonal J2/J3/J4 acceleration; secular rates (Ω̇, ω̇, Ṁ) for on-rails coast; SSO inclination solver; critical
  inclination 63.43°/116.57° (ω̇ = 0); Brouwer–Lyddane or Kozai first-order **mean ↔ osculating** conversion so the HUD
  shows stable elements. No `bodies.json` yet (S3).
- Golden vectors: (1) acceleration at 6 points vs closed-form zonal formula with EGM2008 C20/C30/C40
  (J2 1.08262668e-3, J3 ≈ −2.5327e-6, J4 ≈ −1.6196e-6; take exact values from the NGA EGM2008 C̄n0 file and log it), rel 1e-12 (same formula, two languages). (2) Ω̇ ISS (420 km, 51.64°)
  −5.0°/day ±0.1 (exists, #90); SSO at 800 km 98.6° ±0.1 (exists); critical inclination ω̇ = 0 at 63.435° ±0.01°.
  (3) Numerical propagation 10 days with J2: Ω drift matches the secular formula within 1 % (todo exists in #90).
  (4) Mean→osc→mean round trip, abs 1 m in a, 1e-7 in e. (5) J3 frozen-orbit e ≈ 0.001 at 800 km SSO, ±10 %.
- Tier: **2.0** (J2+J3/J4 zonal). Tesseral/EGM coefficients beyond J4: **1.1+**.

### P3 ISA + NRLMSIS-class upper atmosphere, F10.7/Ap, space-weather generator — **partial**
- Evidence: USSA76 0–86 km validated to 1e-4 (#90). Above 86 km an isothermal 5.8 km tail (`Atmosphere.cs:33` on
  main): **+11 % ρ at 100 km, orders of magnitude low above 120 km**. Ascent ignores the atmosphere above 300 km
  (`Headless.cs:136,216`). Draft #96 adds a separate `UpperAtmosphere` (USSA76 knots, no F10.7). PR
  `fix/atmo-above-86km` (this audit's companion) replaces the tail in `Atmosphere.At` with 5 km USSA76 knots.
- Gap: no solar/geomagnetic input; no space-weather generator; one mean condition (T∞ = 1000 K). **#96 and the fix
  overlap**: #96's knots are 10–100 km apart and log-linear, which is up to ~25 % off in ρ between 120 and 150 km
  (measured against the full USSA76 equations), against ≤2.5 % (≤0.3 % above 150 km) for 5 km knots. The 5 km
  USSA76 tabulation is the single source in
  the fix. Recommend #96 call `Atmosphere.At` above 86 km instead of its own table (one model, no duplication).
- **Licence finding (NRLMSIS 2.0/2.1): not usable.** NRL-SOF-014-1 grants academic, non-commercial use only,
  forbids modifications, and covers the methods by US Patent 10,641,925. A "simplified reimplementation" would be a
  modification of patented methods for a non-academic product. Do not port its code, its coefficient file, or run it to
  make shipped data. See LICENSES.md "Not allowed". Plan: a **Jacchia-71-class** model (exospheric temperature from
  F10.7, F10.7-bar, Ap; diffusion profiles), built from the NASA/SAO government reports (Jacchia 1971, SAO SR-332; NASA
  MSFC MET, NASA TM-4675/TM-2004-213283), which are US Government or published-equation sources. Verify each report's
  licence before use and log it. USSA76 stays the F10.7 = 150-ish, Ap = 4 "mean" check.
- Space-weather generator: seeded F10.7 (11-year cycle sinusoid 70–250 sfu + lognormal 27-day noise) and Ap (mostly
  4–15, rare storms), drawn from NOAA SWPC/NGDC historical stats (public domain).
- Golden vectors: (1) USSA76 0–1000 km at 5 km knots, rel 1e-4 (fix PR). (2) Jacchia-71 published table values
  (SAO SR-332 tables for T∞ = 700/1000/1500 K at 150/300/500/800 km), rel 2 %. (3) Density ratio solar max/min at
  400 km ≈ 5–10× (published), as a band. (4) Generator: seed 1 → fixed 30-day F10.7/Ap series, rel 1e-12; 10 000-day
  statistics: mean F10.7 within ±5 sfu of the target cycle mean, Ap ≥ 50 in 1–3 % of days.
- Tier: **2.0** (density for decay is on the 2.0 list). Detailed composition/radiation: 1.1+.

### P4 Weather generator, ensembles, LCC — **landed** (see #113 / `Kestrel.Core.Weather`)
- Evidence: `Kestrel.Core.Weather` seeded generator + LCC + ensemble; `Flight.Weather.Hold` policy stub unchanged
  (KID/Relaxed never hold). Haven calibrated to NOAA NCEI 1991–2020 Melbourne `USW00012838` (LICENSES.md).
  Lightning = Hold + 30 min (site distance); shear limit 0.012 /s with generator tail that can exceed it.
  Upper winds are visual/LCC only (not in AscentSim) — known simplification until 6-DOF.
- Golden vectors: see `docs/PHYSICS.md` § Weather (P4). Seed→day rel 1e-12; climatology vs hard-coded NOAA
  literals with 3σ/√N + interannual bands; LCC 30 hand cases exact; Rayleigh wind exceedance at N=10 000.
- Tier: **2.0** (weather + LCC). Ensembles: **2.0 reduced**. Upper winds into ascent drag: **1.1 / 6-DOF**.

### P5 Propagators/handoff, drag/decay, lifetime, re-entry heating — **partial**
- Evidence: one RK4 at `dt` 0.05 s for ascent (`Headless.cs:22,97,197`), sim ends at cutoff (`Headless.cs:253-262`);
  "orbit" = perigee > 120 km (`Headless.cs:269`). `FixedStepper` 1/120 s (`Integrator.cs:91-109`). Drag only below
  300 km during ascent (`Headless.cs:136`). No coast phase, no decay, no lifetime, no heating.
- Gap: coast propagator (Kepler + J2 secular "on rails" for time warp; RK4/RK45 with drag when perigee < ~600 km);
  defined **handoff** rules between ascent RK4 ↔ coast numeric ↔ on-rails, with state continuity; drag decay and
  lifetime estimate (King-Hele style for 1.0 claims, numeric for 2.0); re-entry: Sutton–Graves stagnation heating
  q̇ = k√(ρ/rₙ)v³ (k = 1.7415e-4 SI, Earth), peak-g and break-up altitude as gameplay events.
- Golden vectors: (1) Kepler propagation 1 period, two-body, closes to abs 1e-3 m (RK4 at dt 1 s) and exact for the
  analytic path (rel 1e-12). (2) Handoff: ascent end state → on-rails → back, position abs 1 mm. (3) Lifetime: a 400 km
  circular, B* equivalent ballistic coefficient 100 kg/m², USSA76 mean atmosphere → reference value from a numeric run
  of the same equations in an independent script (rel 1 %), plus a published-sanity band (ISS-like decay ~2 km/month
  at moderate solar activity, ±50 %; it is a band, not a claim). (4) Sutton–Graves vs a published capsule case (Apollo
  peak heating order 10⁶ W/m², ±30 %).
- Tier: **2.0** (drag/decay + lifetime). SGP4/TLE propagation: **1.1+**. Detailed ablation/radiation heating: 1.1+.

### P6 SRP, third-body, eclipses, beta angle — **partial** (inputs exist, models absent)
- Evidence: Sun and Moon geocentric directions exist (`Ephemeris.cs:84,150`, `Direction` at `:176`). Nothing uses them in
  the flight core.
- Gap: cylindrical/conical Earth shadow (umbra/penumbra) for eclipse entry/exit; beta angle β = asin(ŝ·ĥ); cannonball SRP
  a = P☉·Cr·A/m (P☉ = 4.56e-6 N/m²) scaled by shadow; Sun/Moon third-body point-mass (only as secular drift for GTO/GEO
  per CoS).
- Golden vectors: (1) β for ISS-like orbit at 4 dates vs the formula with Horizons Sun (abs 0.01°). (2) Eclipse fraction
  for 400 km circular at β = 0 → 39.0 % ±0.2 % (cylindrical shadow: 2·asin(R⊕/r)/360°), and the zero-eclipse threshold β* = asin(R⊕/r) ≈ 70.2° ±0.1°. (3) Conical shadow entry time
  vs an independent geometric root-find, abs 0.1 s. (4) GEO third-body inclination drift ≈ 0.75–0.95°/yr (published),
  band. (5) SRP accel for A/m = 0.01 m²/kg, Cr 1.3 → 5.9e-8 m/s², rel 1e-6.
- Tier: **2.0** (eclipses/β/SRP; third-body GTO/GEO secular only). Lunar patched-conic: **1.1+**.

### P7 3-DOF default + 6-DOF Engineer (slosh, aero, loads, GNC/PEG) — **partial** (2-DOF + mass)
- Evidence: planar translational point mass with mass flow (`Headless.cs:127-157`); no attitude state; "pitch" is a
  commanded thrust direction (`Headless.cs:101,146-150`); Cd(M) only, no lift/normal force (`Aero.cs:134-145`); max-g
  and max-q diagnostics (`Headless.cs:214-236`); guidance is heuristic (`Headless.cs:170-193`), not PEG.
- Gap: 3-DOF means 3-D translational (needs P1) with launch azimuth/inclination targeting. 6-DOF Engineer layer:
  attitude quaternion + body rates, inertia from `Vehicle/Mass.cs`, TVC gimbal limits, CN/CA vs α and Mach, aero
  centre vs CG (static margin), first-mode pendulum slosh per tank, axial/lateral loads (q·α, bending moment proxy at
  interstage), PEG (powered explicit guidance, Jaggers/Langston public NASA papers) for upper stages.
- Golden vectors: (1) 3-DOF in the orbit plane reduces exactly to today's 2-D results: all `simulate.json` cases, rel
  1e-9 (proves no regression). (2) Launch azimuth for i = 51.6° from 28.5° N, non-rotating formula, abs 0.01°.
  (3) 6-DOF: torque-free spin conserves H and T over 1000 s, rel 1e-9; pendulum slosh frequency ω = √(g·ξ/R) vs
  textbook (NASA SP-106), rel 1 %. (4) PEG: hits a 200 km circular target with |Δa| < 1 km, |e| < 1e-3 across 20
  catalogue vehicles. (5) q·α loads: a fixed wind-shear case reproduces a hand-computed bending moment, rel 1e-6.
- Tier: **2.0** (3-DOF default; 6-DOF Engineer with slosh/aero/loads/PEG). Elastic/bending modes: **1.1+**.

### P8a Monte Carlo — **absent**
- Evidence: `LaunchPredictor.cs:1-3` "There is no noise model, so a second seed repeats the same flight and the
  probability is 0 or 1"; `Runs = 1` (`:84`). Seeded RNG exists: mulberry32 (`web/js/flight/sensors.js:11-20`,
  `Flight/Sensors.cs:18`). `FlightBatch` already runs sims in parallel (`Flight/FlightBatch.cs:16-17`).
- Gap: dispersion set (thrust ±1 %, Isp ±0.5 %, mass ±1 %, Cd ±10 %, winds from P4, F10.7 from P3), per-run seed
  derivation (seed ⊕ run index through a splittable hash, not sequential draws, so runs are order-independent under
  `Parallel.For`), reduced-fidelity mode (dt 0.2 s like `CatalogDrag.CatalogFlightDt`, 3-DOF, N = 64–256).
- Golden vectors: (1) seed 1, N = 64 → exact success count and per-run outcome list (bit-exact per platform).
  (2) statistics: mean apogee within ±0.5 % of the nominal run, σ within ±20 % of a linear-sensitivity estimate.
  (3) Convergence: success probability standard error ≤ √(p(1−p)/N) + 0.01.
- Tier: **2.0 reduced-fidelity**. Full-fidelity 6-DOF MC: 1.1+.

### P8b Determinism — **partial**
- Evidence: golden vectors are cross-language with rel/abs 1e-9 (`data/golden/atmosphere.json` tolerance block;
  `docs/PHYSICS.md` "Comparison"). `JsMath` reproduces V8 `hypot`/`round` (`Physics/JsMath.cs:7-30`). CI runs dotnet goldens
  on every PR (#84). Replay D0 pinned clock (#63).
- Gap: no written determinism contract; `Parallel.For` paths (`Atmosphere.cs` `SampleDensity`, `FlightBatch`) are safe
  (independent per index) but nothing tests it; transcendental functions (`Math.Exp/Pow/Sin/Log`) are libm-dependent, so
  bit-exactness across iOS/ARM vs x64 is not guaranteed and must not be promised.
- Contract to adopt: **bit-exact per platform** (same binary, same seed → identical bytes; test: run twice, hash
  results); **tolerance across platforms** (goldens rel 1e-9 closed form, documented bands for integrated flights).
- Golden vectors: (1) same-process double run of 20 catalogue flights + MC N = 64, SHA-256 of canonical output equal.
  (2) `Parallel.For` vs sequential batch, bit-equal. (3) Cross-platform: existing goldens at their tolerances.
- Tier: **2.0**.

### P8c Ten-category validation suite — **partial** (4 of 10 started)
| # | Category | Today | Reference data (PD) |
|---|---|---|---|
| 1 | Astronomy | **present** (#83, #88) | JPL Horizons |
| 2 | Atmosphere | **present 0–86 km** (#90), 86–1000 km in fix PR | USSA76 |
| 3 | Gravity/J2 | reference formulas only (#90) | EGM2008 C20–C40, published Ω̇ |
| 4 | Frames/time | absent | NGA TR8350.2, Vallado examples, USNO |
| 5 | Propagation/decay | todo (#90) | own reference script + published bands |
| 6 | Weather/LCC | todo (#89) | NOAA NCEI normals |
| 7 | Eclipse/β/SRP | absent | Horizons Sun + geometry |
| 8 | Ascent performance | goldens are self-consistency only (`simulate.json`, `catalog-flight.json`) | rocket equation, published Δv-to-LEO budgets (bands) |
| 9 | 6-DOF/slosh/loads | absent | NASA SP-106, textbook cases |
| 10 | MC + determinism | absent | statistics, hashes |
- Home: `tests/validation/*.test.cjs` (JS) and `unity/Assets/Kestrel.Core.Tests/Validation/**` (C#, via `dotnet test`).
- Tier: **2.0** (all ten categories exist, even if some only as bands).

### Architecture — **partial**
- Pure C# core: **present** (`unity/dotnet/Kestrel.Core` builds without Unity; `AtmosphereSampleJob` is the only Burst
  file). Golden vectors: **present** (`data/golden`, 17 files, 727 vectors). **Headless dotnet CLI: absent** — only the
  library and test projects exist in `unity/dotnet/`. Gap: `unity/dotnet/Kestrel.Cli` (`simulate`, `predict`, `mc`,
  `atmo`, `propagate`; JSON in/out) so validation and balance (H6) run without Unity or Node.
- Tier: **2.0**.

## 2. Top gaps (ranked by what blocks the most)
1. **No 3-D frame** (P1). J2, inclination, β, eclipses, ground track, PEG and 3-DOF all need ECI/ECEF + GMST first.
2. **No coast propagator or handoff** (P5). Decay, lifetime, time warp, spent stages (DECISIONS #39) all wait on it.
3. **Point-mass gravity** (P2). J2 is cheap once P1 exists.
4. **Upper atmosphere** (P3): tail was +11 % at 100 km and wrong above 120 km (fixed for the mean condition in
   `fix/atmo-above-86km`); no F10.7/Ap; NRLMSIS is licence-blocked, so Jacchia-71-class is the plan.
5. **No Monte Carlo** (P8a): the predictor's "probability" is 0 or 1.
6. **Weather is one threshold** (P4).
7. **No headless CLI** (architecture).
8. Attitude dynamics absent (P7 6-DOF), largest single build.

## 3. Implementation order for Gameplay Dev (proposed; one Core PR per effect, R19)
1. **P1 frames/time** (ECI/ECEF, geodetic, GMST/ERA) + `bodies.json` (Earth). Also land `Kestrel.Cli` skeleton.
2. **P7a 3-DOF** translational ascent in ECI, launch azimuth; must reproduce today's planar goldens (rel 1e-9).
3. **P2 J2/J3/J4** acceleration + secular rates + mean/osc (absorbs S3's J2 half).
4. **P5 coast propagator + handoff** (on-rails Kepler+J2, numeric with drag) (absorbs S3's propagator half).
5. **P3 upper atmosphere**: fold #96 onto `Atmosphere.At` (fix PR), then Jacchia-71-class with F10.7/Ap + space-weather
   generator (absorbs S2).
6. **P5 drag decay, lifetime, re-entry heating** + `orb.decay.reentry` events.
7. **P6 eclipses/β/SRP**, GTO/GEO third-body secular.
8. **P8a/P8b** MC runner (reduced fidelity) + determinism hash tests; switch `LaunchPredictor` to N runs.
9. **P4 weather generator + LCC + ensembles** (R5/X4 weather item; can run in parallel with 3–7, it only needs K3).
10. **P7b 6-DOF Engineer** (attitude, aero CN/CA, slosh, loads, PEG).
Physics lands each item's validation file first (todo tests), then flips them to real tolerances as code lands.

## 4. Proposed MASTER_ROADMAP mapping (no duplication)
| P item | Absorbs | Stays where it is |
|---|---|---|
| P1 frames/time | S4 "Frames" (core half) | S4 scaled-space camera (Graphics) |
| P2 gravity | S3 J2 | — |
| P3 atmosphere | **S2** density + F10.7; R3 sky LUT reads the same `Atmosphere.At` profile | R3 rendering (Graphics) |
| P4 weather | **R5** (= X4 weather/commit criteria) | R6 clouds/FX |
| P5 propagators | S3 propagator, S2 decay, spent-stage decay (DECISIONS #39) | S6 event hooks |
| P6 eclipses/β/SRP | S5 eclipse *geometry* | S5 visuals |
| P8c validation | **Astro #83** (category 1), validation A–C (#88–#90) | — |
