# Golden tolerances

Reference model: `PHYSICS.md` (atmosphere, aero, gravity, rocket equation, `simulate()`).
This file is the contract for `data/golden/catalog-flight.json`.

The vectors are engine-neutral. They call the plain JS physics modules only. They do not select a game runtime.

## Regenerate

```bash
npm run gen:golden
```

That is `node tools/export-golden.mjs`. It rewrites every file in `data/golden/` from the reference implementation. `tests/catalogs-json.test.cjs` fails if a committed file drifts from a fresh run. Do not type expected numbers by hand.

`tests/golden-harness.test.cjs` reads the `golden-tolerances` block below and compares a fresh `simulate()` / `dragForce()` / `orbitalElements()` run to the committed vectors.

## Comparison

For a numeric expected value `E` and a result `A`:

```text
|A - E| <= abs + rel * |E|
```

Non-finite values are stored as strings (`"Infinity"`, `"NaN"`) and must match exactly. `outcome`, event `type`, event `stage`, Mach-peak `gridAltitudeM`, and `mach` are exact: a missed separation is not a tolerance problem.

`maxQTime.abs` is the catalog-flight step `tools/golden-flight.mjs` `CATALOG_FLIGHT_DT` (0.2 s). The harness fails if those two diverge.

## What is covered

One case of each kind for every engine in `data/engines.json` (loaded by `web/js/data/engines.js`) and every preset in `PRESETS` (`data/presets.json` plus `web/js/data/templates/*.json`):

| Kind | Reference | Recorded result |
|---|---|---|
| ascent | `simulate()` | outcome, time, altitude, speed, expended and ideal Δv, mass, load factor, losses, state |
| staging | same run, event list | ignition, liftoff, burnout, separation, cutoff, orbit |
| drag | same run, plus `dragForce` / `dynamicPressure` / `cdMach` | max-Q and its time, drag loss, drag at the peak-q sample, drag at Mach `M_PEAK` on the ISA grid |
| orbit | `orbitalElements()` of the final state | classical elements; the state is also an input so the transform can be checked on its own |

Engine cases also evaluate Mach-peak drag on the catalog nozzle diameter, so two engines that share a stage diameter still get distinct nozzle drag when their exits differ.

Preset flights use `serialStages()` on the real design (strap-ons included as the existing combined phase). Engine flights are a two-stage fixture, not a copy of a preset:

- Propellant is the engine's catalog propellant. Both stages use that engine.
- Stage diameter is `max(defaultStage().diameter, engine.diameter)`, clamped to the design schema.
- Payload mass is 0. Fairing length equals that diameter. Avionics tier is `basic`.
- Engine count is the smallest cluster (1–40) whose largest propellant load at liftoff TWR 1.5 still has an ideal first-burn climb of at least 10 m. The climb uses vacuum mass flow, because that is what `simulate()` integrates, and net acceleration `(TWR − 1) · g0`.
- A vacuum bell (`thrustSL <= 0` or `ispSL <= 0`) is integrated with `vacuum: true`, because sea-level Isp is 0 and the vehicle would never leave the pad.

Those fixture choices are inputs. The numbers in `out` come from `simulate`, `dragForce`, `dynamicPressure`, `cdMach`, and `orbitalElements`.

## Gaps

- Vacuum bells (`gg-kerolox-vac`, `gg-kerolox-small-vac`, `ff-methalox-vac`, `exp-hydrolox`, `pf-udmh`) have integrated max-Q and drag loss of 0. The reference ascent turns the atmosphere off for them. Their drag coverage is the closed-form Mach-peak evaluation, including nozzle area.
- `tpl-sounding-solid` is a single stage. Its staging vector has burnout and no separation.
- Mach-peak dynamic pressure on the ISA grid is highest at 0 m for every vehicle, because density falls faster than the speed of sound rises. `gridAltitudeM` is therefore 0. Engines are distinguished by area (stage diameter and nozzle diameter), not by a different winning altitude.
- `gg-kerolox-small-vac` and `pf-hypergolic` share a 1 m nozzle, so their nozzle Mach-peak drag matches. The sea-level engine still has a non-zero trajectory max-Q; the vacuum engine does not.
- `simulate()` has no J2, no throttle, no ullage, and no parallel-burn integration beyond the `serialStages()` combined phase. This file does not add those.

## Bands

The absolute term is the floor (the quantity is allowed to be ~0). The relative term tracks scale. Integrated quantities use relative 1e-6, the existing `simulate.json` libm/RK4 contract. Closed form uses relative 1e-9, the existing `rocket-eq.json` / `orbit.json` / `aero-gravity-earth.json` contract, except `cdMach`, which is a polynomial and is held to 1e-12.

| quantity | absolute | relative | reasoning |
|---|---|---|---|
| time | 1e-6 | 1e-6 | Burnout and cutoff land on a shortened RK4 step that hits propellant depletion. Absolute 1e-6 s is the existing event-time contract and is far below the 0.2 s catalog step. Relative 1e-6 on a 1000 s flight is 1 ms. |
| maxQTime | 0.2 | 0 | The peak is updated once per step, so the time is quantized to dt. Absolute tolerance is one catalog step (0.2 s). Relative tolerance is 0 so a one-step shift is allowed and a multi-step shift is not. |
| altitude | 1e-2 | 1e-6 | Geometric and event altitudes. Absolute 1 cm covers the pad, where a relative band on a near-zero altitude collapses. Relative 1e-6 is about 6 m at Earth radius, the simulate.json accumulation bound. |
| position | 1e-2 | 1e-6 | Inertial x and y. Same floor and relative band as altitude: centimetres while downrange is still small, 1e-6 once the position is a sizable fraction of an Earth radius. |
| velocity | 1e-4 | 1e-6 | Absolute 0.1 mm/s covers the pad, where air-relative speed is about 0. Relative 1e-6 is about 8 mm/s at 7.8 km/s. |
| mass | 1e-3 | 1e-6 | Mass flow is constant on a stage, so mass error tracks time error times mdot. Absolute 1 g on a depleted stage. Relative 1e-6 matches the integrator. |
| loss | 1e-4 | 1e-6 | Gravity, drag, and steering integrals. Vacuum drag loss is exactly 0, so the floor matters: 0.1 mm/s. Relative 1e-6 on a 1 km/s gravity loss is 1 mm/s. |
| deltaVIntegrated | 1e-3 | 1e-6 | Expended Δv sums thrust acceleration over every step. Absolute 1 mm/s. Relative 1e-6 on a 9 km/s burn is 9 mm/s. |
| deltaVClosed | 1e-9 | 1e-9 | Ideal Δv is one logarithm per stage, the rocket-eq.json contract. |
| maxG | 1e-6 | 1e-6 | Load factor is O(1) to O(10). Absolute 1e-6 g is about 10 µm/s². Relative 1e-6 matches the acceleration bound. |
| maxQ | 1e-3 | 1e-6 | Running peak of dynamic pressure. Vacuum flights are exactly 0, so the floor is 1 mPa. Relative 1e-6 at 40 kPa is 0.04 Pa. |
| dragClosed | 1e-9 | 1e-9 | One cdMach evaluation and a few multiplies, the aero-gravity-earth.json contract. The absolute term covers a zero-speed sample. |
| qClosed | 1e-9 | 1e-9 | Dynamic pressure is two multiplies of density and speed, with no special functions and no accumulated steps. |
| cdClosed | 1e-12 | 1e-12 | cdMach is a polynomial and the result is O(1). 1e-12 catches the wrong transonic branch without demanding bit-identical smoothstep rounding. |
| semiMajor | 1e-2 | 1e-6 | Semi-major axis, apoapsis, and periapsis of a fresh integration. They are algebraic in the integrated state, so they use the position band, not the closed-form band. |
| eccentricity | 1e-6 | 1e-6 | A 1e-6 relative velocity error moves e by about that order. Absolute 1e-6 covers a near-circular orbit, where a relative band on a tiny e is meaningless. |
| inclination | 1e-6 | 1e-6 | These flights are planar, so i is about 0 and the absolute term binds. 1e-6 rad is 0.2 arcsec. |
| energy | 1e-2 | 1e-6 | Specific energy is O(1e7) J/kg. Relative 1e-6 matches a 1e-6 velocity error because energy goes as v². Absolute 1e-2 J/kg is the floor where energy crosses zero. |
| angularMomentum | 1e-2 | 1e-6 | Specific angular momentum is O(1e10) m²/s in LEO, so the relative term binds. The absolute term is only the near-zero floor. |
| period | 1e-4 | 1e-6 | Relative 1e-6 on a 90 minute orbit is about 5 ms. Absolute 0.1 ms is the floor. A hyperbolic period is the string Infinity and is exact. |
| closedForm | 1e-9 | 1e-9 | orbitalElements of the recorded state only, the orbit.json contract. Relative 1e-9 binds for a, energy, h, and period. Absolute 1e-9 binds for e and i near 0. This band is not applied to the integrated state. |

```golden-tolerances
{
  "rule": "|actual - expected| <= abs + rel * |expected|",
  "exactKeys": ["outcome", "type", "stage", "gridAltitudeM", "mach"],
  "quantities": {
    "time": { "abs": 1e-6, "rel": 1e-6, "keys": ["time", "t"] },
    "maxQTime": { "abs": 0.2, "rel": 0, "keys": ["maxQTime"] },
    "altitude": { "abs": 1e-2, "rel": 1e-6, "keys": ["finalAlt", "alt", "apoAlt", "periAlt"] },
    "position": { "abs": 1e-2, "rel": 1e-6, "keys": ["x", "y"] },
    "velocity": { "abs": 1e-4, "rel": 1e-6, "keys": ["finalV", "v", "vrel", "vx", "vy"] },
    "mass": { "abs": 1e-3, "rel": 1e-6, "keys": ["finalMass", "m"] },
    "loss": { "abs": 1e-4, "rel": 1e-6, "keys": ["gravity", "drag", "steering", "dragLoss"] },
    "deltaVIntegrated": { "abs": 1e-3, "rel": 1e-6, "keys": ["deltaVExpended"] },
    "deltaVClosed": { "abs": 1e-9, "rel": 1e-9, "keys": ["idealDeltaV"] },
    "maxG": { "abs": 1e-6, "rel": 1e-6, "keys": ["maxG"] },
    "maxQ": { "abs": 1e-3, "rel": 1e-6, "keys": ["maxQ"] },
    "dragClosed": { "abs": 1e-9, "rel": 1e-9, "keys": ["dragForce"] },
    "qClosed": { "abs": 1e-9, "rel": 1e-9, "keys": ["dynamicPressure"] },
    "cdClosed": { "abs": 1e-12, "rel": 1e-12, "keys": ["cd"] },
    "semiMajor": { "abs": 1e-2, "rel": 1e-6, "keys": ["a", "periapsis", "apoapsis"] },
    "eccentricity": { "abs": 1e-6, "rel": 1e-6, "keys": ["e"] },
    "inclination": { "abs": 1e-6, "rel": 1e-6, "keys": ["i"] },
    "energy": { "abs": 1e-2, "rel": 1e-6, "keys": ["energy"] },
    "angularMomentum": { "abs": 1e-2, "rel": 1e-6, "keys": ["h"] },
    "period": { "abs": 1e-4, "rel": 1e-6, "keys": ["period"] },
    "closedForm": { "abs": 1e-9, "rel": 1e-9, "keys": [] }
  },
  "overlays": {
    "orbitClosedForm": {
      "a": "closedForm",
      "periapsis": "closedForm",
      "apoapsis": "closedForm",
      "periAlt": "closedForm",
      "apoAlt": "closedForm",
      "e": "closedForm",
      "i": "closedForm",
      "energy": "closedForm",
      "h": "closedForm",
      "period": "closedForm"
    }
  }
}
```

## 3-DOF ascent (P7a)

`Ascent3Dof` integrates `[x, y, z, vx, vy, vz, m]` with the same guidance, atmosphere, and point-mass gravity as `Headless`. `SimulatePlanar` seeds `z = vz = 0` on that pad state and uses the same air rate, `Ω cos φ`. In that plane the steps match the 2-D flights: every `simulate.json` case and every catalog ascent. No J2, and no wind shear.

`LaunchAzimuth.InertialNortherlyDeg` is the non-rotating relation `sin Az = cos i / cos φ`, degrees from north toward east. For 51.6° from 28.5° N the northerly branch is 44.9751330989884° and the southerly branch is 135.0248669010116°. `|cos i| > |cos φ|` throws `ArgumentOutOfRangeException`. A pole throws as well. The result is never NaN.

`RotatingNortherlyDeg` and `RotatingSoutherlyDeg` are the ground headings of a horizontal inertial velocity `v = sqrt(μ/R)` at `r = R` after the eastward site speed `ω R cos φ` is removed. For the same inclination and latitude those headings are 42.801031904016824° and 137.1989680959832°. A WGS-84 site in ECI waits on the frames work. This type does not keep a second ellipsoid.

| quantity | absolute | relative | reasoning |
|---|---|---|---|
| planar3dof | 1e-9 | 1e-9 | Same RK4 steps as Headless while `z = vz = 0`. Relative 1e-9 is the regression bar on the integrated state. Absolute 1e-9 covers components that sit near zero. |
| launchAzimuthInertial | 0.01 deg | 0 | Non-rotating closed form, both branches, 51.6° from 28.5° N. The test also checks the arcsine identity to 1e-12 deg. |
| launchAzimuthRotating | 1e-6 deg | 0 | Velocity triangle at surface circular speed. 1e-6 deg is about 0.004 arcsec, above `atan2` roundoff and well inside a guidance step. |
