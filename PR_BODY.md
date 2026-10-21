# #313: one shared flight director for batch, designer, timeline and live pad (option b, Physics rulings 1-4 and A/B/C)

The shared director (option b) drives batch, designer predict, timeline and the live pad. This commit adds Physics A/B/C and Arcade's board rows on top of the 961edad rulings 1-4 work. Full per-row numbers are in GOLDEN_DIFF.md.

## Physics A (i): parking-orbit cutoff and an honest departure Δv check

- **Cutoff:** ellipse and C3 missions cut off at the circular parking orbit, a = R + aim perigee. Circular targets and free builds are unchanged.
- **Pass rule:** parking perigee in band **and** available ≥ required.
  - Available = Σ Isp·g0·ln(m_now/m_dry) on the propellant left after boil-off.
  - Required = the impulsive burn × (1 + loss). GTO: 2455 → 2491 m/s. TLI: 3133 → 3180 m/s.
  - Loss is 1.5% at T/W ≥ 0.5. Below that it is 1.5%·(0.5/TW)², capped at 10%.
- **Where it lives:** `MissionScore.Departure*` and `SimResult.departureDv/TW`. FlightBatch, FlightDebrief and PostFlightSource all use `FlightDebrief.DepartureMet`.
- **Re-search:** every ellipse and C3 design was a real miss (for example m7 had 800 of 2491 m/s), so all were re-searched (option iii). That covers 60 golden rows in winners, viable, found, board and the GTO/TLI contracts. Every row has a design with margin ≥ 88 m/s on all 5 seeds and passes the live pad on seed 1. **No row is left without a design.**

## Physics B
A stage-1 engine-out throttles only stage 1. New test: `AStageOneEngineOut_ThrottlesOnlyStageOne_NotTheUpperStage`.

## Physics C
- finch-leo400-k3 (seed 2, 657.8 × 142.2) and pantry-leo400-k3 (seed 3, 637.7 × 162.3) stay real misses. **Arcade: contract viability.**
- The scout and kid retune is **not committed**. It needs a decision (below).
- FOLLOWUP_CLOSED_LOOP.md is drafted. No ticket was opened.

## Arcade board rows
- m5 `fix` → `fly` and m14 `strength` → `fly`: the raw starters pass 5/5.
- m12 `strength` → `fix` with a new near-miss: kh12-win with stage-1 engines 7 → 6.
  - Raw it is an honest miss on all 5 seeds at 856.5 × 343.5 km, with no failure.
  - One Fix it (weak-upper: S2 engines 2 → 3) passes 5/5 at 606 × 594 km.
- The suggested SF 1.3 starter breaks up at max-Q, which violates NAV01, and no strength-only near-miss exists for m12 (SF 1.35-2.2 all pass).
- `BoardStarter_FixItOrOneStrengthEdit` stays strict and passes.

## Heron (tpl-hydrogen-upper) batch vs live, ≤ 0.5% apo
#314 is not on main yet (079d19b), and the file is untouched. On the shared director with the in-step energy cutoff:

| Heron | Batch, km | Live, km | Apo Δ | Peri Δ |
|---|---|---|---:|---:|
| 7 engines (#314) | 35786.099 × 199.901 | 35786.095 × 199.905 | 0.00001% | 4 m |
| 9 engines (main) | 6982.202 × 879.603 | 6979.026 × 879.659 | 0.045% | 56 m |

- The 271 vs 230 km gap was the JS-parity `Stack.SimInputs` path, which has a perigee cutoff on the step. Kestrel flights no longer fly it.
- With the failure monitor on, both Herons break up at max-Q (T+34.5 s and T+66.6 s). That is #314's to fix.

## Verdict flips
- vs origin/main: 11 of 530, all fail → pass (the m5 and m14 starters, and found m18 seed 2).
- vs 961edad: 111 flips (106 fail → pass, 5 pass → fail). The 5 are the intended m12 near-miss.

## Tests
- **Expectations changed** (superseded or re-pinned, none loosened):
  - the ellipse/C3 target energy in `TheEnginesCutOffAtTheTargetEnergy…`;
  - KH-10 strap prop 14,000 → 22,400 kg and landing hold 2,800 → 4,480 kg in `Setup_HoldsOutCoastBoilOff…`.
- **New:** `DepartureCheckTests`, the Physics B test, and the Heron batch-vs-live test.

| Suite | Passed | Skipped | Failed |
|---|---:|---:|---:|
| Core | 754 | 14 | **9** |
| Site | 28 | 0 | 0 |
| Pad.Core | 84 | 0 | 0 |
| Vehicle.Core | 41 | 0 | 0 |
| Build | 13 | 0 | 0 |
| Guide.Core | 41 | 0 | 0 |
| Livery.Core / Livery.Branding | 15 / 39 | 0 | 0 |

node: 179 tests, 172 pass, 0 fail, 1 skipped, 6 todo. `stubcheck --all`: exit 0. Unity EditMode and PlayMode were not run here (GameCI only).

The 9 Core failures, by cause (none newly introduced by this commit's data):
- **Fix it, 4 tests** (product decision): FixIt_WeakAirframe…(14) and (16), FixIt_ThatCannotFixTheFlight…, ReportFixIt_RanOutOfFuel….
  - In 14 and 16, Fix it's 95% max-Q throttle lofts the rocket into a miss that `FullyFixed` counts as fixed.
  - In the other two, the KH-6 rocket now misses at a bad γ instead of running dry.
  - Decide: count misses as unfixed, or land closed-loop guidance first.
- **finch/pantry contracts, 1 test:** EveryCell_AndEveryOffer_IsViable (a Physics C real miss).
- **Scout predict, 2 tests:** UnchangedDesign_HasAnEmptyDiff and PropellantCut…. These need the preset decision below.
- **tpl-hydrogen-upper predict, 2 tests:** BrokenPrecondition… and UpperEngineSwap…. Left for #314.

## Decisions needed
1. **The preset retune conflicts with existing invariants.**
   - The tuned values are in GOLDEN_DIFF.md. kid's peak g is 5.71 (limit 8) and its max q 18.0 kPa (limit 40) on predict and seeds 1-5, at 203 × 197 km.
   - It is not committed because it breaks GV7 (presets carry no V3 guidance), StabilityBand (kid is the > 45 kPa max-Q fixture), the weak-scout breakup fixtures, the JS save key count and GoldenTimelines.
   - Also, with the exact energy cutoff, LaunchPredictor's strict `peri ≥ target` can never pass a circular target. Proposal: predict success = the ±5% band.
2. **Fix it:** count misses as unfixed, or closed-loop guidance first.
3. **Arcade:** finch/pantry leo400-k3 contract viability.

## Kestrel Physics sign-off
- Parking cutoff and the departure Δv check, with the margins table (A): TBD
- Engine-out scoping (B): TBD
- Heron batch-vs-live ≤ 0.5% apo: TBD
- Preset retune vs GV7, StabilityBand and the predict criterion (C): TBD
- Signed off by:
