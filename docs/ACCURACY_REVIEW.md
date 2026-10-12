# Accuracy review (X7) + X4 scope notes

_main @ 4685247, Oct 12 2026. Each number below comes from a test that runs. Nothing is rounded in our favour._

## 1. Validated models

| Model | Reference (public domain / facts) | Measured worst | Gate (ε) | Why this ε | Test |
|---|---|---|---|---|---|
| Sun apparent RA/Dec | JPL Horizons, geocentric 500@399, apparent of date, **TT** epochs, 50 epochs 2000–2100 | **29.8″** (rms 14.3″) | 36″ (audit); 0.1° (gameplay) | Meeus ch. 25 published accuracy is 0.01°. | `Validation/AstroAccuracyAuditTests.cs` (PR A), `AstroGoldenTests.cs` |
| Moon apparent RA/Dec | same | **9.5″** (rms 5.1″) | 11″ (audit); 0.1° | Meeus ch. 47 main terms: about 10″ in longitude and 4″ in latitude, so √116 = 10.8″. | same |
| Moon distance | same | — | 50 km | ch. 47 distance series | `AstroGoldenTests.cs` |
| Time scales | Ephemeris takes JD(TT). ΔT (Espenak–Meeus) is only for UTC input. | — | ΔT(2026) ∈ [60, 80] s | Sanity range only. ΔT is predicted, not measured, after 2026. | `AstroGoldenTests.cs` |
| Atmosphere 0–79 km | USSA76 Table I (5 s.f.) | 6.7e-5 rel | 1e-4 rel (T, p, ρ) | Table rounding is ≤ 5e-5, plus the R_air constant. | `tests/validation/atmosphere-j2-decay.test.cjs` (PR C) |
| Atmosphere 80–86 km | same | T 4.07e-4 | T 5e-4, p/ρ 1e-4 | The table is kinetic T. The code uses molecular-scale T_M, and M/M0 = 0.9996 at 86 km. This difference is by design. | same |
| J2 reference formula | WGS-84/EGM96 J2 | ISS node −4.95°/day; SSO at 800 km is 98.60° | 0.1°/day; 0.1° | First-order J2 only (J2², J4 ≈ 1 %) plus rounding of the published figures. | same |

## 2. Known inaccuracies (not hidden, not gated)

- **Above 86 km**, the isothermal tail has H = 5.8 km. Density is **+11 %** at 100 km, and the error is orders of magnitude above about 120 km. There is no thermosphere and no F10.7 input. → S2.
- **No J2 in the sim** (point-mass gravity). The reference formula is tested; the propagator is not. → S3.
- **No orbital decay.** Orbits are immortal. → S2 + S3. Blocked on the decisions about decay strength and F10.7.
- **No weather model.** It is a wind-hold policy only. Climatology checks are `todo` (PR B). → R5.
- **Sun at 29.8″** is about 0.5 solar radii. That is invisible at gameplay scale, but it is not good enough for eclipse-contact timing to the second. A tighter solar theory (VSOP87 truncated) would be needed if eclipse timing is ever scored.

## 3. Test runner note
`npm test` runs `./tests/*.test.cjs` only, so `tests/validation/` needs `node --test tests/validation/`. Wiring it into `package.json` is a one-line change outside this lane. Kestrel Arcade should decide. C# validation runs under `unity/dotnet` (`dotnet test`), because the csproj globs `Kestrel.Core.Tests/**`.

---

## 4. X4 notes, tiered

### Weather (R5 is X4's weather item)
| Tier | Scope |
|---|---|
| **1.0** | Seeded `Weather.Generate(site, dateSeed, difficulty)` covering surface wind, gusts, cloud ceiling, precipitation, temperature, and lightning distance. It is deterministic, with byte-identical JSON goldens. Visual only in KID/Relaxed. Climatology validation against NOAA NCEI 1991–2020 normals (PD) uses statistical ε (3σ/√N plus interannual spread). |
| **1.1** | Upper-level winds into the sim (shear profile from NOAA IGRA radiosonde climatology, PD). Diurnal cycle. Regenerating goldens requires a Physics ε review. |
| **Later** | Fronts or multi-day systems, anvil/field-mill electrification model, and per-site microclimates. |

### Launch commit criteria and windows
| Tier | Scope |
|---|---|
| **1.0** | A fictional rule set modelled on public range weather rules (facts only, original wording): surface wind/gust limit (default 12 m/s, kept), lightning within N km, flight through precipitation, ceiling, and temperature limits. Violations emit `held` / `delayed` / `scrub` through `MissionEvent` with no new schema (DECISIONS #30). The existing T-2 h / T-4 min built-in holds stay. Windows are **instantaneous or fixed-length per mission**. |
| **1.1** | Physically derived windows: a planar window for a target inclination from launch latitude (`cos i ≥ cos φ`), plus RAAN-targeted windows once S3 J2 exists. Recycle logic after a scrub. |
| **Later** | Rendezvous/phasing windows (Moon, stations), plus launch-collision avoidance (COLA)-style blackout periods. |

### Post-insertion scope
| Tier | Scope |
|---|---|
| **1.0** | Orbit verdict (Ap/Pe, insertion grade) on point-mass gravity. Time warp (basic). Earth orbit only. **The UI must not claim orbit lifetime** until decay exists. |
| **1.1** | S2 thermosphere + decay, S3 on-rails Kepler + J2 secular, node-drift visual, spent stages as tracked on-rails objects (cap 64), `orb.decay.reentry`. Needs goldens for both, validated against the reference formulas in PR C (propagator node rate within 1 % of the J2 formula over 10 days). |
| **Later** | Lunar flight and patched conics, third-body perturbations, and re-entry heating. |
