# Kestrel Heavy physics core (M1)

Plain ES modules in `web/js/physics/`, no dependencies, SI units throughout
(m, s, kg, N, Pa, K, rad; Isp in seconds). Nothing here is wired into the game yet.

## integrator.js
- `createRK4(n)` → `rk4Step(deriv, t, y, dt)`: classic 4th-order Runge–Kutta, y ← y + dt/6·(k1+2k2+2k3+k4).
  `deriv(t, y, out)` writes dy/dt into `out`. Scratch arrays are allocated once. Global error O(dt⁴).
- `createFixedStepper(step, {dt=1/120, maxSteps=16})` → `{advance(frameDt), alpha, time, reset()}`:
  the "fix your timestep" accumulator. Physics runs at a fixed dt regardless of render rate; `alpha` is the
  interpolation fraction. Backlog beyond `maxSteps` is dropped (spiral-of-death guard).

## atmosphere.js — U.S. Standard Atmosphere 1976 (NOAA/NASA/USAF, NOAA-S/T 76-1562)
- Geopotential height h = R₀·z/(R₀+z), R₀ = 6 356 766 m.
- Layers (base h km / lapse K·m⁻¹): 0/−0.0065, 11/0, 20/+0.001, 32/+0.0028, 47/0, 51/−0.0028, 71/−0.002, 84.852 (=86 km geometric).
- T = T_b + L·(h−h_b). p = p_b·(T/T_b)^(−g₀/(R·L)) if L≠0, else p_b·exp(−g₀(h−h_b)/(R·T_b)).
- ρ = p/(R·T), a = √(γRT); R = 287.053 J/(kg·K), γ = 1.4, g₀ = 9.80665 m/s², T₀ = 288.15 K, p₀ = 101 325 Pa.
- 86–1000 km: USSA76 Table I knots every 5 km (kinetic T, p, ρ, tabulated sound speed; 183 knots from 90 km),
  ln p and ln ρ linear between knots, T and a linear; 86–90 km starts from the layer solution at 86 km so the
  regimes meet. Exact to the table's 5 significant figures at knots; between knots ≤2.5 % (ρ, worst at 112.5 km
  where T bends 240→300 K) and ≤0.3 % above 150 km vs the full USSA76 equations. Above 1000 km the 995–1000 km
  scale height continues (**assumption**). One mean solar condition (1976, T∞ = 1000 K); no F10.7/Ap. Above
  ~150 km a is the tabulated sound speed, not √(γRT) (mean molecular weight falls). Replaced the isothermal
  5.8 km tail, which was +11 % in ρ at 100 km and orders of magnitude low above 120 km.
- `atmosphere(z, out?)` → `{T, p, rho, a}`; `pressureAt(z)`, `densityAt(z)`, `geopotential(z)`.

## aero.js
- q = ½ρv² (Pa). D = q·C_d(M)·A, A = πd²/4 (m²), M = v_rel/a. v_rel is relative to the co-rotating air.
- `cdMach(M, Cd0)`: **engineering fit, not CFD**. Cd0 for M ≤ 0.8; smoothstep rise to 2·Cd0 at M = 1.1
  (transonic drag spike); supersonic Cd0·(1 + (1.1/M)²). Shape follows the usual slender-body trend
  (e.g. Sutton & Biblarz, *Rocket Propulsion Elements*; Fleeman, *Tactical Missile Design*).
- Exports `cdMach`, `refArea(d)`, `dynamicPressure(rho, v)`, `dragForce(rho, v, a, Cd0, A)`.

## gravity.js
- g = μ/r², vector −μ·r/|r|³. μ = 3.986004418×10¹⁴ m³/s², R_E = 6 378 137 m (WGS-84).
- Spherical point mass; **J2 not modeled**. Exports `MU_EARTH, R_EARTH, gravityAccel(r), gravityVec(x,y,z,out)`.

## earth.js
- ω = 7.2921159×10⁻⁵ rad/s (sidereal). Eastward site speed v = ω·R_E·cos φ (spherical Earth; 465.1 m/s at equator).
- `siteRotationSpeed(lat)`, `siteVelocity(lat, out)`, `airVelocity2D(x, y, out)` (ω × r), `minInclination(lat)` = |φ|.

## orbit.js
- Specific energy ε = v²/2 − μ/r; a = −μ/(2ε); h = |r × v|; e = √(1 + 2εh²/μ²); i = acos(h_z/|h|).
- r_p = h²/(μ(1+e)), r_a = a(1+e) (∞ if e ≥ 1); altitudes subtract R_E; T = 2π√(a³/μ). v_circ = √(μ/(R_E+h)).
- `orbitalElements(r[3], v[3], mu?, out?)`, `orbit2D(x, y, vx, vy, mu?, out?)`, `circularVelocity(alt)`.
  Source: Vallado, *Fundamentals of Astrodynamics and Applications*, ch. 2.

## rocket-eq.js
- Tsiolkovsky Δv = Isp·g₀·ln(m₀/m_f). Mass flow ṁ = F/(Isp·g₀). Burn time = m_prop/ṁ.
- Isp(p) = Isp_vac − (Isp_vac − Isp_SL)·p/p₀. Linear in ambient pressure is exact for a fixed nozzle at constant
  ṁ, since F = F_vac − A_e·p (Sutton & Biblarz ch. 3). Clamped ≥ 0; no flow-separation model for over-expanded nozzles.
- `deltaV, massFlow, ispAt, thrustAt(Fvac, IspSL, IspVac, p), burnTime, G0`.

## headless.js
- Planar, Earth-centred inertial model in the launch plane, state `[x, y, vx, vy, m]`, RK4, dt = 0.05 s by
  default. Step size is shortened to land exactly on each burnout.
- Forces: point-mass gravity, thrust ṁ·g₀·Isp(p) with ṁ from the **vacuum** pair, drag with the **active
  stage's** diameter and Cd0. `thrustSL` is only checked for consistency (warning if >5% off ṁ·g₀·Isp_SL).
- Launch is due east from latitude φ, with site speed ω·R_E·cos φ (**assumption**: planar model, so the
  plane is treated as equatorial; inclination is not simulated).
- Guidance: vertical rise → pitch kick (`kickAngle`, default 4°) at `kickSpeed` (60 m/s) → gravity turn on
  air-relative velocity for stage 1 → closed-loop upper-stage steering (pitch chosen so vertical accel tracks
  a commanded climb rate toward `targetAlt`). Cutoff when periapsis ≥ min(target, current alt) − 10 km.
  This is a simple steering law, **not optimal (no PEG)**, so steering losses are high (~0.9 km/s).
- Staging is instant, with no coast, ullage, or throttle. Full thrust throughout, so max-g is unlimited (~6 g with the archetype).
- Loss accounting: gravity ∫g·sinγ dt, drag ∫D/m dt, steering ∫(F/m)(1−cos α) dt.
- `simulate(stages, opts)`, `stackDeltaV(stages, payload)`, `normalizeStages(stages)`, `MEDIUM_LIFTER`.
  Stage object: `{thrustSL, thrustVac, ispSL, ispVac, propMass, dryMass, diameter, cd}`.

## CLI
`node tools/sim.mjs [--payload kg] [--target m] [--lat deg] [--dt s] [--samples s] [--stages file.json] [--json]`

## Tests (`npm test`)
`tests/physics-core.test.cjs` (ISA table, Cd(M), rocket-eq, v_circ at 200 km = 7784 m/s, site speed, stepper),
`tests/physics-orbit.test.cjs` (energy drift < 1e-8 over 3 orbits, circular after 10 periods),
`tests/physics-ascent.test.cjs` (integrated Δv vs Tsiolkovsky < 1%, medium lifter + 10 t reaches LEO, 60 t does not).

`data/golden/catalog-flight.json` is an ascent, staging, drag/max-Q, and orbit-determination vector for every catalog engine and every preset. Per-quantity absolute and relative tolerances are in `docs/PHYSICS.md`. Regenerate with `npm run gen:golden`.
