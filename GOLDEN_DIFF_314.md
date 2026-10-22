# #314 golden diff (Physics approved (9619132))

Branch `fix/314-one-flight-model`, rebased on main 079d19b (#301). Commit 1 is 7b879e2 (option 2: the prediction flies the live checks; q and g at the breakup moment). Commit 2 is a39f16d, Physics' rulings (1)–(3), approved by Physics as 9619132 before the rebase. Commit 3 is cb40c84, the KH-16 comment.

Seeds 1–5 are the campaign seeds. "Batch" is dt 0.2 s and "live" is dt 1/120 s. Both run `MissionFlightSetup` + `FailureMonitor` (when it engages) + `PlayedChecks`. Both templates are arcade (v1) designs, so the seeds don't change them: all 5 seeds give the same numbers.

## (1) Sounding template: `tpl-sounding-solid` → `tpl-sounding-hybrid`

The file moves from `web/js/data/templates/sounding-solid.json` to `sounding-hybrid.json`. The name stays Pipit. Tags, tagline, brief, benchmark class/text, things-to-try and the review note now describe the hybrid.

| | old: 1× srm-small, solid-apcp | new: 2× hybrid-small, hybrid |
|---|---|---|
| Stage | 2300 kg prop, 1.6 m, steel | 1900 kg prop, 1.3 m, steel |
| Fairing | 1.6 × 2.0 m | 1.3 × 2.4 m |
| Liftoff mass / TWR (JS stats) | 3.1 t / 19.58 | 2.7 t / 1.91 |
| Ideal Δv / margin (JS stats) | 3593 m/s / 49.7% | 3038 m/s / 26.6% |
| JS sim apogee / max g (stats) | 114 km / 20.6 g | 125 km / 7.2 g |
| C# prediction (PlayedChecks) | **breakup**: overg at T+0.6 s, apogee 0.6 km, fail | suborbital, apogee **121.1 km**, pass, Δv margin 637 m/s |
| Batch seeds 1–5: flown peak / max-Q / sim max g | breakup at T+0.6 s | 121.126 km / 36.21 kPa / 7.24 g |
| Live seeds 1–5: flown peak / max-Q / sim max g | breakup at T+0.5 s | 120.995 km / 36.21 kPa / 7.25 g |
| Played peak thrust/mass (20 g cap) | 19.7–19.9 g at T+0.5 s, broke on the next ticks | 7.89 g |
| Played q vs break line (2.4 × q_lim 49 kPa = 118 kPa) | n/a | 36.21 kPa |
| Uncut full headless max-Q | 659.79 kPa | 36.21 kPa (reference = full: 36.21 kPa at 5.2 km) |
| Timeline | asc.burnout T+8.4, asc.maxq T+8.4, no asc.karman | asc.maxq / throttle T+30, asc.meco T+76.6, asc.karman T+145.6, stg.fairing T+145.6, dep.payload T+212.2 |

The ios/ mirror (`ios/KestrelHeavy/Game/js/data/templates/`) is **not** touched. It still has sounding-solid.json and the old index.js. It needs a later sync, with Trevor's OK.

### Solid code paths: new named fixture
`tests/fixtures/designs/sounding-solid.json` (id `fixture-sounding-solid`, "Solid sounding fixture") is the old template's vehicle, unchanged except for id and name, with the template block dropped. `Golden.Fixture` / `Golden.FixtureJson` load it. These tests moved to it:
- `DesignV2Tests` FinlessSounding (`srm-large` finless variant)
- `LaunchSequenceTests.Solid_UsesBurnout_NotMeco`
- `TimelineTests.HardwareAndRestartRules` (solid part: no strongback, no MECO, solid arm)
- `UpperAtmosphereTests` (a sounding rocket under 100 km must explain the missing Karman event)
- `ReadoutV2Tests.StabilityBand…` (unguided-solid maxq-high exemption, fed its uncut 659.79 kPa peak)
- `TimelineDiffTests.UnchangedFailingDesign…` (the failing design: "Apogee 0.6 km, target 100.0 km")

## (3) `tpl-hydrogen-upper` (Heron) retune
On main with #314, it broke up at T+145.1 s (stage-1 burnout) at 21.8 g played thrust/mass, over the 20 g cap. Retune with real hardware: stage 1 gets **7 gg-kerolox instead of 9**. Everything else is unchanged (400 t / 30 t prop, 2× exp-hydrolox, 4 t GTO comsat).

| | old (9 engines) | new (7 engines) |
|---|---|---|
| Liftoff mass / TWR (JS stats) | 463.1 t / 1.67 | 462.2 t / 1.31 |
| Ideal Δv / margin vs GTO (JS stats) | 12641 m/s / 11.6% | 12680 m/s / 11.9% |
| JS sim | orbit 196 × 930 km, 13.5 g | orbit 191 × 271 km, 10.7 g |
| C# batch seeds 1–5 | **breakup** overg T+145.1 s | orbit, peri 190.774 / apo 271.162 km, max-Q 35.88 kPa, sim max g 10.65 |
| C# live seeds 1–5 | **breakup** overg T+145.1 s | orbit, peri 190.081 / apo 230.117 km, max-Q 36.17 kPa, sim max g 10.72 |
| Played peak thrust/mass (cap 20 g) | 21.80 g | 16.95 g |
| Played max-Q (warn 45 kPa, break 2.4 × 35 = 84 kPa) | 47.15 kPa (over warn) | 35.88 / 36.17 kPa |
| Upper-stage Δv left at cutoff (Isp·g0·ln(m/m_dry), payload in dry) | n/a | 3609 m/s batch / 3624 m/s live, against about 2.46 km/s (+1–2% finite burn) for GTO from the parking orbit, so about 1.1 km/s of margin |
| Timeline | maxq T+48, MECO/sep T+144.1, SECO-1 T+717.1 | maxq T+69, MECO/sep T+185.6, SECO-1 T+573.6 |

Note: the C# prediction still reads "does not reach the target" for this template (peri 190.7 km against 200 km). That is main's ascent cutoff (`periAlt >= min(target, alt) - 10 km`), and every orbital template has it today: medium-kerolox 190.6, medium-kerolox-reusable 158.3, methane-reusable 152.7, small-kerolox 308.1 against 400 km. #313 removes that 10 km offset (energy cutoff, parking orbit plus departure Δv check). This PR does not.

The kid preset is not touched (#313 owns it).

## (2) KH-16 Fix it: test asserts minimality
`FixIt_WeakAirframe_RepairsUntilEveryGateSeedFlies_GustsIncluded` keeps TestCase(14) unchanged, still strict: one step under flies all 5, two steps under breaks up. KH-16 moves to `FixIt_WeakAirframe16_RepairIsTheMinimalStrengthForTheBreakupQ`. The repair step is unchanged (DesignerRepair / StepperFix). Measured:
- At SF 1.0 the reference breaks up, and q is read at the breakup moment: **44.016 kPa** against q_lim(1.0) = 44.0 kPa. The uncut run peaks at 167.4 kPa. The test asserts q_lim ≤ q < 1.02 q_lim, and that the uncut peak is more than 2× q.
- The stepper's step: need = 1.0 × 44.016 / (0.85 × 44.0) = 1.177, which rounds up to the 0.05 grid as **SF 1.20**.
- Asserted: SF 1.20 flies FlightBatch seeds 1–5 with no failure, FullyFixed, gate Ready. 0.85·q_lim(1.20) = 44.88 ≥ 44.016 clears the line. **SF 1.15** gives 0.85·q_lim(1.15) = 43.01 < 44.016, so it does not clear the line. The repair is the minimal step.
- For reference only (not asserted): FlightBatch alone first flies all 5 seeds at SF 1.10 on KH-16 (1.05 breaks up). The old 1.15 / "two steps under is the knife-edge" assertion came from a q that was not read at the breakup moment.

## Make it fly: landing-reserve and low-thrust fix (JS + C#, kept identical)
The hybrid template exposed a stuck loop in Make it fly (`tests/designer-validate.test.cjs` breaker #7, and the C# port `Validation_Readout_MakeItFly_AndLayers_MatchJs`). With a 5000 kg landing reserve on a 1900 kg hybrid stage, it lightened the payload and then stopped at margin −1 (no usable propellant). JS and C# then diverged because their payload floors differ (50 vs 10 kg). Two narrow fixes, mirrored in `web/js/designer/validate.js` and `Validate.MakeItFly`:
- A landing reserve ≥ the stage's whole propellant is trimmed to the designer maximum (30%, `landingReserveFraction.max`).
- Single-stage only: when margin is short and liftoff TWR ≤ 1.45, add a first-stage engine if one fits, so the next step can add propellant again. It no longer shrinks the cargo.

Golden effect: only the template's own designer-validate cases changed. No other preset or template case moved (checked case by case against the pre-change export).

## Regenerated goldens and vectors
- `data/golden/{catalog-flight,designer-validate,economy,simulate,vehicle}.json` (`node tools/export-golden.mjs`): only the `tpl-sounding-solid→hybrid` and `tpl-hydrogen-upper` cases changed (4 catalog-flight cases each, plus their validate / makeItFly / economy / simulate / vehicle entries).
  - makeItFly hydrogen-upper #4 heavy-payload: 9→8 engines, 484 t→440 t stage 1, payload 7370→6265 kg.
  - makeItFly hydrogen-upper #7 landing-fuel: 9→8 engines.
  - makeItFly hybrid #7: reserve 5000→570 kg, 3 engines, 4073 kg prop.
  - makeItFly hybrid #4: 3 engines, 3060 kg prop, payload kept at 900 kg.
- `data/golden/design-v2.json` (hand-kept C# golden): `suborbitalExempt` tpl-sounding-solid → tpl-sounding-hybrid.
- `tests/vectors/prediction/launches.txt`: sounding row as above (fail 0.6 km → pass 121.1 km, standard and kid). The scout, kid and super-heavy rows are unchanged.
- `tests/vectors/timeline/tpl-sounding-solid.{json,md}` deleted; `tpl-sounding-hybrid.{json,md}` added; `tpl-hydrogen-upper.{json,md}` regenerated. No other timeline vector moved.
- `tests/vectors/timeline-diff/{causality,fairing,restart}.txt`: hydrogen-upper event times and failed-prediction text, for example causality's prediction: peri −5966.3/apo 1339.6 (breakup) → peri 190.7/apo 271.1 km.
- `web/js/data/templates/index.js` regenerated (`npm run templates:build`; `--check` clean).
- `docs/PHYSICS.md`: the single-stage staging note names tpl-sounding-hybrid.

## Tests (local)
Core 845 passed / 12 skipped / 0 failed (857 on main 079d19b). Site 28, Pad.Core 84, Vehicle.Core 41, Build 13, Guide.Core 41, all green. node: 179 tests, 172 pass, 1 skipped, 6 todo, 0 fail. stubcheck `--all` ok. templates `--check` ok. No test loosened. The 4 that failed at e01f2e9 now pass on real values.
