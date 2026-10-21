# #313: one shared flight director for batch, designer, timeline and live pad (option b + Physics rulings 1-4)

> **DRAFT. Not ready to merge.** Core has 21 failures, all listed below, and none was loosened. Three need decisions: a Physics ruling on prop-depletion for ellipse and C3 targets, an Arcade decision on three board starters that now win unedited, and a Physics call on the legacy-guidance FPA misses (presets scout and kid). The golden diff (GOLDEN_DIFF.md) goes to Kestrel Physics first.

Closes #315

## Root cause

Only the live pad flew the `AscentDirector`. Batch, designer practice flight and timeline flew the bare sim. The director also carried its own max-Q bucket (70% default), separate from the design's GuidanceProfile max-Q law. Two further sources of batch-vs-live disagreement:
- The perigee cutoff was grazing: perigee creeps toward the target while apoapsis grows fast, so m12 was 602 vs 905 km.
- The gravity-turn and kick logic was step-quantized: m5 staged at 146 vs 141 km.

## Fix

- **One director** (`DirectedFlight`, from a9ec49c): FlightBatch, ReferenceAscent (designer and timeline), LaunchPredictor and FlightDriver all step `MissionFlightSetup.Director` → `AscentSim`. Option (b): the autopilot takes the max-Q call only on a design max-Q throttle and never takes the pitch nudge. Player taps are unchanged (stubcheck clean).
- **Ruling 2, energy cutoff:** `SimOptions.energyCutoff` and `cutoffEnergy`, with `SimOptions.EnergyFor(apo, peri)` = −μ/(2(R + (h_apo + h_peri)/2)); c3 uses C3/2. `MissionFlightSetup.TargetEnergy`. Bisected inside the step (40 iterations, deterministic), in the same code for batch and live. Kestrel flights have no perigee cutoff. It stays only on the JS-parity path (raw `SimOptions` / `Stack.SimInputs`), and no Kestrel flight uses it.
- **Ruling 1:** the cutoff is at the target energy itself, with no 10 km offset and no half-band. Bands unchanged. FPA misses are retuned in the design (see GOLDEN_DIFF.md) or listed as real misses with γ.
- **Ruling 3:** the fuel-imbalance penalty throttles only its stage and ends when that stage separates or shuts down (`fuelStage`). **Extension for Physics to confirm:** the stage-1 engine-out throttle ((n−1)/n) had the same leak into the upper stage, and I scoped it the same way (`engineStage`).
- **Ruling 4:** the design's GuidanceProfile max-Q throttle is the only max-Q law. `AscentCommand.SuppressDesignThrottle` is removed. A taken max-Q call on a design without one sets `EngageMaxQCap`, and `DirectedFlight` engages the same GuidanceProfile law at that cap. Bare sim, batch, designer and pad are identical for event-free flights (tested on every seed for both hops).
- **Step-independence** (`SimOptions.continuousTurn`, on for every Kestrel flight):
  - The custom gravity turn follows velocity continuously.
  - The step lands exactly on the pitch-kick trigger (`LandOnKick`).
  - PEG-lite is evaluated at each RK4 substage, not held from the step start.
  - KH-6 at 0.2 s vs live: 0.20% apo, 0.41 km peri, 0.0000% SMA, so no 0.05 s step is needed.
- **Hops (bands unchanged):**
  - KH-1 starter stays at **515 kg**: 9.786 km batch on every seed, 9.787 live.
  - **m1b is kept at 705 kg:** it flies the band under (b) at 10.000 km. Arcade's 645 kg peaks at 9.07 km and fails.
  - m2b 2540 → 2585 kg: 101.02 km.
  - m2f 1560 → 1580 kg: 100.10 km.
- **#316:** the KH-2 starter pulls 6.90 g batch / 6.92 g live (limit 6 g). Not changed here.
- **Assist note:** under (b) the default player flies the design's own settings. No open-loop bias is left. The prediction flies the same director with the same defaults (auto-assist on, Simple depth).

## Golden changes (details and per-seed numbers in GOLDEN_DIFF.md)

- m3 h-n5 24000 → 23500 kg, renamed h-n5-k8-23500-2500: winner, viable, found, board and the flock-leo200-k0 contract.
- m3-eight kick 8 → 7.
- m5 n9 kick 7 → 8, renamed n9-400000-k8-u40000.
- m12 kh12-win kick 8 → 6. It was a real FPA miss: γ 0.627° / 0.625°, 676.3/523.7 km.
- Contract retunes: tern-leo600-k3 ×2 kick 8 → 6, tern-sso-k3 n5 kick 6 → 4, tern-sso-k9 t3-t3 kick 8 → 5.
- The prediction vectors were regenerated.

## Real misses (γ at cutoff or burnout, batch/live)

- **m10 b9** (winner, starter, board, found): prop-depletion at 199.72 km, 489.8/199.7 km, γ 0.008°/0.007°.
- **m17 kh17-win** (winner, starter, board): prop-depletion at 199.77 km, apo 57324 km, γ 0.020°/0.020°.
- **m19 kh19-win** (winner, starter, board, found): prop-depletion at 199.76 km, 975.0/199.7 km, γ 0.023°/0.023°.
- Viables: m7 n9 (γ −0.037°) and m10 b8/b9/b10 (−0.015°/0.008°/0.019°). Contracts: comsat-gto-k3 ×3, comsat-gto-k6 ×3, lark-tli-k6 ×2 (γ 0.017-0.027°).
- **Why they can't be retuned cleanly:** these won on main through the "delta-v" criterion (200 km perigee plus on-paper Δv). Under ruling 2 they burn toward GTO or TLI energy, which their real performance can't reach. They run dry just under the 200 km aim, and `OnEmpty` calls prop-depletion. Kick, kick altitude, shape, upper-propellant and engine sweeps found no pass, and the campaign search finds no m10 or m19 design. The m17 found.json variant passes the batch, but its liftoff TWR of 0.82 is schema-blocked, so it was reverted. **Physics ruling needed** (options in GOLDEN_DIFF.md).
- **FPA misses:**
  - contracts finch-leo400-k3 c9-u38 (seed 2, γ 1.830°) and pantry-leo400-k3 c8-u35 (seed 3, γ 1.842°): a seeded stage-1 engine-out leaves the upper stage low; no kick fixes it.
  - presets: scout 443.1/356.8 km (cuts at 441 km, γ −0.108°) and kid 656.7/−256.8 km (cuts at 657 km, γ 0.069°). Legacy guidance overshoots the target altitude before reaching target energy.

## Tests

New in `SharedDirectorTests`: the energy cutoff (exact ε and SMA at 0.2 s and 1/120 s; ellipse and C3 energies; the parity path is untouched), the design max-Q law as the only law on every hop seed, the per-stage fuel penalty, batch-vs-live on every orbital winner and board starter, determinism, and the designer = bare sim check.

Expectations changed, because they were wrong under the rulings (full list in GOLDEN_DIFF.md):
- Director commands: ThrottleScale → `EngageMaxQCap` for max-Q, in AscentDecisionTests and Kh1FirstHopTests.
- KH-1 starter 464 → 515 kg.
- Designer vs bare sim: not-equal → equal.
- ReferenceMaxQ compares against the directed designer flight. The stock readout is 70.71 kPa under the predicted stage-1 fuel imbalance; the bare stock vehicle still checks at 73.01 kPa.
- The m3 board starter name.
- Prediction vectors.

| Suite | Passed | Skipped | Failed |
|---|---:|---:|---:|
| Core | 730 | 14 | **21** |
| Site | 28 | 0 | 0 |
| Pad.Core | 84 | 0 | 0 |
| Vehicle.Core | 41 | 0 | 0 |
| Build | 13 | 0 | 0 |
| Guide.Core | 41 | 0 | 0 |
| Livery.Core / Livery.Branding (extra) | 15 / 39 | 0 | 0 |

node: 179 tests, 172 pass, 0 fail, 1 skipped, 6 todo. `stubcheck --all`: exit 0.

Core failures, by cause:
- **Prop-depletion class, 12 tests:** CampaignWinners_FlyPassOnEverySeed, CampaignViableDesigns_WinM1ThroughM10, EveryMission_HasThreeViableDesigns, EveryCell_AndEveryOffer_IsViable (also the finch/pantry FPA misses), DesignFirstRocketsAreReadyAfterThePredictRun, GoldenWinnersHaveNoCertainFailureTheyDoNotHave, EveryOrbitalWinner_LiveStep_ReachesOrbit, NAV01, NAV02, BatchAndLivePad_Agree_Orbits… (m10, m17 and m19 only), DefaultPlayer_EveryGoldenWinnerAndStarter… (m10, m17 and m19 only), CampaignWinners_HideEachReleasedBody… (15 of 18).
- **Board premise, 1 test:** BoardStarter_FixItOrOneStrengthEdit_WinsEveryMission. The raw m5, m12 and m14 starters now win without their edit. **Arcade:** switch those rows to mode "fly", or pick new near-miss starters.
- **Preset scout FPA miss, 4 tests:** UnchangedDesign_HasAnEmptyDiff, PropellantCut…, BrokenPrecondition…, UpperEngineSwap….
- **Fix it knife-edge, 4 tests:** FixIt_WeakAirframe…(14) and (16), FixIt_ThatCannotFixTheFlight…, ReportFixIt_RanOutOfFuel…. Loads and cutoff changed under rulings 2 and 4. Not yet investigated.

Not run here: Unity EditMode and PlayMode (GameCI only).

## Kestrel Physics sign-off

- Energy cutoff at the target energy, located in the step (rulings 1-2): TBD
- Prop-depletion on ellipse and C3 delta-v designs (m10, m17, m19 and the GTO/TLI contracts): TBD
- Fuel penalty per stage (ruling 3), and the same scoping applied to the engine-out throttle: TBD
- Design max-Q law as the only law (ruling 4): TBD
- Legacy-guidance FPA misses (presets scout and kid): TBD
- Orbital tolerances: apoapsis 0.5% / perigee 1 km / SMA 0.1%: TBD
- Signed off by:
