# Upper Atmosphere and Outer Space audit (S1–S7)

_Audit only, no feature code. Oct 12 2026. Brief: Trevor via Chief of Staff. Planned **together with** ENVIRONMENT_REALISM_AUDIT.md (R1–R7): one astronomy core, one atmosphere model, one camera/floating-origin system, nothing duplicated. Same adaptations: Unity 6.3/URP, portrait, 120 Hz on High, $0, public-domain or CC0 data only, every licence verified and logged in `LICENSES.md`, fictional names, reduced-flash safe._

**Scope (Chief of Staff call):** 2.0 ships **Earth orbit only**: true sky, the Moon, eclipses, J2 and orbital decay. Lunar landing, interplanetary flight and the solar-system view are post-launch, but the architecture below is built for them now.

## 1. Current state
| Area | Today | Where | Gap |
|---|---|---|---|
| Atmosphere | US Standard Atmosphere 1976, 0–86 km + isothermal exponential tail | `Kestrel.Core/Physics/Atmosphere.cs` | No thermosphere density (needed for decay), no solar-activity input |
| Gravity / orbit | Point-mass `MU_EARTH`, `Orbit.Elements` / `Planar` / `CircularVelocity`, RK4 at 1/120 s | `Physics/Gravity.cs`, `Orbit.cs`, `Integrator.cs` | No J2, no third bodies, no on-rails propagation, no time warp beyond X4 basic, single central body hard-coded |
| Frames | Floating origin re-centres every 2 km, float render, double sim | `FlightDriver.cs:97-102`, `ChaseCamera.cs` | No inertial (ECI) vs Earth-fixed split; no scaled-space layer for Earth/Moon at orbital distances |
| Sky / bodies | Flat clear colour, fixed sun light | `SliceApp.cs` | No stars, Moon, Earth-from-orbit, eclipses (see Env audit §1) |

## 2. Shared architecture (R + S, no duplication)
| System | Single owner | Used by R | Used by S |
|---|---|---|---|
| `Kestrel.Core.Astro` (time scales UTC/TT, sun/Moon ephemeris, star catalogue, body registry) | R2 = S1 (one milestone) | sun angle for sky, Moon in sky, time of day | eclipses, Moon gravity (post-launch), solar-system view (post-launch) |
| `Kestrel.Core.Atmo` (USSA76 ≤ 86 km + thermosphere density model above, optional F10.7 input) | S2, extends existing `Atmosphere.cs` | sky LUT density profile (R3), weather layer heights (R5) | drag + orbital decay, re-entry heating |
| `Kestrel.Core.Bodies` (`CelestialBody` record: μ, radius, J2, rotation, atmosphere ref, SOI) loaded from `data/bodies.json` | S3 | Earth radius/curvature for terrain (R4) | J2, patched conics later (Moon, planets as data, not code) |
| Propagator: RK4 under thrust/atmosphere, analytic Kepler + J2 secular + decay "on rails" when coasting | S3 | — | time warp, orbit prediction, ground track |
| Frames + camera: double ECI sim → Earth-fixed → floating origin → float render, plus **scaled-space** camera layer (1:1000) for Earth/Moon/limb | S4 (extends U4 camera + #76) | horizon, curvature, sky→space (R3) | orbit view, Moon, future planets |
| Physical sky renderer (one shader family, LUT-based) | R3 | ground view | limb/airglow from orbit, Earth shadow, eclipse dimming |

Rules: no second sun direction, no second atmosphere table, no second camera rig. Body data is JSON (single source, like other catalogs); adding the Moon as a landable body or Mars later is data + SOI switching, not a new engine.

## 3. Techniques per tier (space side)
| Feature | Low | Medium | High |
|---|---|---|---|
| Stars | ≤ 2,000 point stars (BSC5) | ≤ 5,000 + magnitude-correct size | ≤ 9,000 + Milky Way band (PD) |
| Earth from orbit | 1k equirect day texture on sphere, flat ocean spec | 2k day + night lights blend at terminator, cloud layer | 4k (on-demand download) + atmosphere limb from R3 LUTs, airglow |
| Moon | 512 albedo texture, phase from ephemeris | 1k + normal | 2k on-demand |
| Eclipse | Shadow cone test, light dim | + penumbra fraction | + limb-darkened sun disc, Earth-shadow on Moon colour (no flash; reduced-flash ramp ≥ 300 ms) |
| Orbit view | line orbit + Ap/Pe markers | + ground track | + J2 node regression visual |

**Bundle rule:** ≤ 1k planet/Moon textures in the IPA (inside the ≤ 8 MB environment budget). 2k/4k are on-demand downloads (Apple On-Demand Resources hosted by Apple = $0, or GitHub Releases); the game must be fully playable offline without them.

## 4. Data sources (public domain / CC0 only; each verified and logged)
`LICENSES.md` row per file: file, source URL, author/agency, licence text or PD statement link, date verified, SHA-256.
| Data | Source | Licence status to verify |
|---|---|---|
| Earth day / night | NASA Visible Earth Blue Marble, Black Marble | US Gov work, PD; strip NASA insignia |
| Moon albedo / elevation | NASA SVS CGI Moon Kit (LRO) | PD (NASA SVS) |
| Planets (post-launch) | NASA/JPL/USGS Astrogeology mosaics | PD for NASA/USGS; exclude ESA/third-party credited items |
| Stars | Yale Bright Star Catalogue BSC5; Hipparcos subset | PD / verify CDS terms; BSC5 preferred |
| Ephemeris | Meeus-style analytic series (our implementation), checked against JPL Horizons values in tests | algorithms; Horizons output used only as test vectors, logged |
| Thermosphere | NRLMSISE-00 (US Naval Research Lab) or simpler Jacchia-style exponential table | NRL code is US Gov; prefer own table from published coefficients |
| Earth J2, μ, radii | WGS-84 / EGM constants | Facts |

## 5. S1–S7
| S | Scope | 2.0? | Lands in | Owner |
|---|---|---|---|---|
| **S1** Astronomy core = R2 (time, sun, Moon, stars, body registry, golden vectors vs Horizons) | yes | parallel to U1 | Gameplay Dev, Physics |
| **S2** Upper atmosphere: thermosphere density, Kármán / space transition events, drag + **orbital decay** | yes | Core after U1; feeds U4 | Gameplay Dev, Physics |
| **S3** Bodies + propagator: J2, on-rails Kepler+J2+decay, time warp integration (X4), `bodies.json` with Earth+Moon | yes (Moon as sky/eclipse body only) | U4 | Gameplay Dev, Physics |
| **S4** Frames + scaled-space camera + orbit view (extends U4 camera, R3 sky) | yes | U4 | Graphics Dev |
| **S5** Space visuals: stars, Earth from orbit, Moon, eclipses, limb/airglow, tiered textures, on-demand pack | yes (Low/Medium; High textures on demand) | U4/U6, after R3 | Graphics Dev, Art |
| **S6** Hooks: `MissionEvent`s (`orb.insert`, `orb.eclipse.enter/exit`, `orb.decay.reentry`), telemetry `orb.*`, narrator/Codex/SFX | yes | U7 / K6 | Sound, Narrator Voice, Codex |
| **S7** Beyond Earth: lunar SOI + landing, interplanetary transfers, solar-system view | **post-launch** (architecture only in 2.0) | 1.1+ | Gameplay Dev, Graphics Dev |

## 6. Sequencing (combined with R)
R1 audit (done) → **R2/S1 astronomy** ∥ U1 → S2 atmosphere ∥ K3 → R5 weather + S3 propagator → U4: R3 sky + S4 camera/scaled space + S5 space visuals → U6: R4 sites, R6 clouds → U7: R7 + S6 hooks → U9 perf gate. S7 after 2.0.
Collision rules: Core folders `Astro/`, `Atmo/` (extends `Physics/Atmosphere.cs` via Physics review), `Bodies/` are new; `FlightDirector.cs` / `Events.cs` single lane; J2/decay change golden vectors → one Core PR each with Physics ε review.

## 7. Scope decisions for Chief of Staff (space)
1. **Orbital decay strength:** realistic decay (LEO below ~300 km decays in days-to-weeks of game time), or exaggerated so players see it within a session?
2. **Solar activity:** fixed mean F10.7 for 2.0, or a seeded solar-cycle value per mission (affects decay)?
3. **Moon in 2.0:** sky body + eclipses only (no Moon gravity), confirmed? Or include lunar third-body perturbation in the propagator now?
4. **Time warp limits:** max warp in Earth orbit (e.g. 1,000× or 10,000×), and is warp disabled under thrust and in atmosphere?
5. **On-demand texture host:** Apple On-Demand Resources (needs App Store build config, Trevor gate) or GitHub Releases download?
6. **Star count per tier:** accept 2,000 / 5,000 / 9,000, or one 5,000-star set everywhere?
7. **Accuracy target:** golden vectors for sun/Moon positions against JPL Horizons within 0.1° for 2000–2100, enough?
8. **Re-entry of decayed stages:** visual re-entry + debris event narration in 2.0, or just a "decayed" status in the tracker?
9. **Clock:** real current date by default for sky/eclipse, or mission-set date (shared with Env decision 9)?

## 8. Approved extras (DECISIONS #39, #41)
- **Kármán line:** `asc.karman` `MissionEvent` (sim condition: altitude ≥ 100 km, first crossing), narrator line batch item, Codex link. S2/S6.
- **Exposure model:** shared with R3 (ENV §9): dark-adapted-eye mode + labelled cinematic assist; one exposure system for ground and orbit.
- **Near-silence in vacuum:** U7 mixer snapshot driven by `Atmo` dynamic pressure/density — external air SFX fade to zero above ~80 km; only structure-borne engine rumble, pumps, mission audio and music remain.
- **Spent stages stay in orbit:** stages and fairings that reach orbit become on-rails objects in the S3 propagator, decay under S2 drag, emit `orb.decay.reentry`; cap of tracked objects per save (proposed 64).
- **Photo Mode space shots:** G7 photo mode (1.1 plan) gets orbit/limb/eclipse presets; 2.0 keeps the capture path (PortraitGameView helpers) for marketing only unless pulled in.
- **Codex Labs:** Orbit (Ap/Pe, J2 node drift), Moon Phase (S1 ephemeris), Sun-Sync (J2 precession), Exposure (EV/eye adaptation), Wind Shear (R5). Each is a small interactive card driven by Core functions with golden vectors.
- **Textures (#5 override):** 2.0 ships no on-demand downloads; all space assets ≤ 8 MB inside the IPA with procedural detail and ASTC compression. ODR vs GitHub Releases is a later question for Trevor; no build-config change. (Supersedes the "on-demand" lines in §3.)
