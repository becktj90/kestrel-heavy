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

## Physics C (approved 12:14 PM), plus the predictor band and Fix it
- **Presets:**
  - scout uses **al-li tanks** (rated for cryogenic propellant; no T+0 cryo failure, no exemption) and pitch settings. Predict and seeds 1-5 fly **410.1 × 389.9 km, g 6.48, q 21.4 kPa**.
  - kid uses pitch settings with throttleMin 0.4. It flies **205.8 × 194.2 km, g 7.62 (limit 8), q 18.0 kPa (limit 40)**.
  - The old kid and scout are now named fixtures: tests/fixtures/designs/maxq-high-kid.json (90.07 kPa) and weak-scout.json. GV7 follows the new rule, "presets may carry pitch settings". No assertion was loosened.
- **One acceptance rule:** `MissionScore.TargetMet`. FlightBatch, FlightDebrief and LaunchPredictor all call it. The new `PredictorBatchVerdictTests` checks that predictor and batch agree on all 6 templates and 4 presets.
- **Fix it:** a repair counts only if the mission passes on all 5 seeds. Clean misses are named `fpa-miss`, `miss` or `gate-miss`. A 95% max-Q throttle that lofts into a miss is never FullyFixed; Fix it falls back to strength on the original rocket.
  - KH-14 is fixed at strength 1.3.
  - KH-16: the budget holds at 8 steps (Arcade). Fix it looks further on a copy, never applies it, and says plainly: "Fix it can't fix this rocket: it still fails 5 of 5 test flights. It needs a redesign with a stronger airframe: strength 1.5. Fix it only goes up to 1.4." The board's KH-16 row is `strength` (1.8 by hand), so nothing relies on Fix it for KH-16.
  - KH-6 is asserted as an `fpa-miss` by name. Both can't-fix fixtures have their note and report text pinned, and their advice is flown.
- **Contracts:** finch-leo400-k3 is now **c10-462-k7-u38 (cost 27495, +1260)** and viable on 5/5.
  - pantry-leo400-k3 is out of the viable set and in a new `tracked` list for **#326** (engine-out balance). It is not dealt.
  - Recorded: c8-360-k7-u35 misses on seed 3 at 637.7 × 162.3 km. The best variant, S2 propellant +10% (cost 24708), passes 4/5.
  - `TrackedCells_AreExactlyPantryLeo400K3_ForIssue326_AndStillMiss` pins that one row. `EveryCell_AndEveryOffer_IsViable` stays strict for the other 20 cells.
  - The engine-out write-up was filed as #326 and removed from the branch.
- FOLLOWUP_CLOSED_LOOP.md is drafted. No ticket was opened.

## Thin Δv margins: do not let future tuning push these negative
Each is the minimum over seeds 1-5, available − required departure Δv:

| Row | Design | Margin |
|---|---|---:|
| comsat-gto-k0 (contract) | n12-p494-m80-u28 | **+88 m/s** |
| comsat-gto-k3 (contract) | n12-p416-m100-u28 | +90 m/s |
| comsat-gto-k6 (contract) | n12-p416-m150-u42 | +94 m/s |
| m20 winner | kh20-win | **+104 m/s** |
| m7 viable | n14-p416-m200-u42 | +108 m/s |
| m10 winner | b16-p544-m165-u33-s22 | **+112 m/s** |
| m10 viable | b16-p544-m165-u33-s22 | +118 m/s |

## MissReason: flight-path-angle misses (Arcade, kid words)
A clean cutoff with |γ| ≥ 0.5°, the high point above the band and the low point below it is now a flight-path-angle miss. It is checked first (orbit first), before apo-high. The new `cutoffGammaDeg` is a readout only; physics is unchanged.

| Code | Advice | Flown on |
|---|---|---|
| `fpa-steep` (climbing) | "Your rocket flew too steep. Give the upper stage more push (one more engine, with the max-Q throttle at 95%), or tip it over less at launch (a smaller pitch kick in Guidance)." | m12: Fix it's edit 5/5, kick 6° → 4° 5/5 |
| `fpa-steep-suborbital` (climbing, fell back) | "Your rocket flew too steep and fell back down. Give stage N much more fuel (it can take double or more), so it burns long enough to level out." | KH-6 fixtures: ×2 (4/5 plus a seeded engine-out), ×3.06 (5/5) |
| `fpa-flat` (diving) | "Your rocket flew too flat. Add fuel to stage N, about 2% at a time (up to 10%), until the orbit rounds off." | m3 (1.06) and m4 (1.02) |

- Fix it's m12 edit is a multi-move: +1 upper engine, max-Q throttle 95%, fins and diameter. One more engine alone still misses (903 × 297 km). So the advice names the throttle too, and the board text now says it.
- "Turn over sooner" (a bigger kick) makes m12 worse; a smaller kick lands it. So the steep advice says "tip it over less".
- m6 (γ +0.10°) keeps the fuel advice and its test. `MoreTopStageFuel…("m12")` was replaced by the two KH12 tests.

## Arcade board rows
- m5 `fix` → `fly` and m14 `strength` → `fly`: the raw starters pass 5/5.
- m12 `strength` → `fix` with a new near-miss: kh12-win with stage-1 engines 7 → 6.
  - Raw it is an honest miss on all 5 seeds at 856.5 × 343.5 km, with no failure.
  - One Fix it (weak-upper: S2 engines 2 → 3 with the max-Q throttle at 95%) passes 5/5 at 606 × 594 km.
- The suggested SF 1.3 starter breaks up at max-Q, which violates NAV01, and no strength-only near-miss exists for m12 (SF 1.35-2.2 all pass).
- `BoardStarter_FixItOrOneStrengthEdit` stays strict and passes.

## Heron (tpl-hydrogen-upper) batch vs live, ≤ 0.5% apo
#314 is not on main yet (rebased onto e18218b), so the 9-engine case stays. The file is untouched. On the shared director with the in-step energy cutoff:

| Heron | Batch, km | Live, km | Apo Δ | Peri Δ |
|---|---|---|---:|---:|
| 7 engines (#314) | 35786.099 × 199.901 | 35786.095 × 199.905 | 0.00001% | 4 m |
| 9 engines (main) | 6982.202 × 879.603 | 6979.026 × 879.659 | 0.045% | 56 m |

- The 271 vs 230 km gap was the JS-parity `Stack.SimInputs` path, which has a perigee cutoff on the step. Kestrel flights no longer fly it.
- With the failure monitor on, both Herons break up at max-Q (T+34.5 s and T+66.6 s). That is #314's to fix.

## Verdict flips
- vs origin/main: 11 of 530, all fail → pass (the m5 and m14 starters, and found m18 seed 2). After the rebase the batch is identical to 8eb886c (0 flips).
- vs 961edad: 111 flips (106 fail → pass, 5 pass → fail). The 5 are the intended m12 near-miss.

## Tests (rebased on e18218b)
- **Expectations changed** (re-pinned to real values; full old → new list in GOLDEN_DIFF.md):
  - the ellipse/C3 target energy;
  - the KH-10 strap and landing hold;
  - GV7 (new preset rule);
  - Version1 liftoff masses (al-li);
  - LaunchPredictorTests (now uses the shared rule);
  - the Fix it tests (new rule, real values; the RanOutOfFuel fixture goes 50% → 16% upper load, the real depletion point);
  - #311's MissReasonTests and #301's Starter tests (#313 numbers).
- **New:**
  - `DepartureCheckTests`;
  - the Physics B test;
  - the Heron batch-vs-live test;
  - `PredictorBatchVerdictTests`;
  - `FixIt_WeakAirframe_ThatOnlyFliesIntoAMiss_IsNotFixed`;
  - `FixIt_WhenTheMaxQThrottleLoftsIntoAMiss_NeverClaimsFixed`;
  - `KH12_SteepAdvice_IsFixItsEdit_AndFollowingItPassesEverySeed`, `KH12_SmallerPitchKickAlsoLands_AndOneEngineAloneIsNotEnough` and `FlightPathAngleMiss_NeedsACleanTiltedCutoff`;
  - `TrackedCells_AreExactlyPantryLeo400K3_ForIssue326_AndStillMiss`.

| Suite | Passed | Skipped | Failed |
|---|---:|---:|---:|
| Core | 885 | 12 | **2** |
| Site | 28 | 0 | 0 |
| Pad.Core | 84 | 0 | 0 |
| Vehicle.Core | 41 | 0 | 0 |
| Build | 13 | 0 | 0 |
| Guide.Core | 41 | 0 | 0 |
| Livery.Core / Livery.Branding | 15 / 39 | 0 | 0 |

node: 179 tests, 172 pass, 0 fail, 1 skipped, 6 todo. `stubcheck --all`: exit 0. Unity EditMode and PlayMode were not run here (GameCI only).

The 2 Core failures are both **tpl-hydrogen-upper (#314)**: `BrokenPrecondition_FlagsCausality_AndKeepsTheRemovedEvent` and `UpperEngineSwap_DropsRestart_AndExplainsTheLostBurns`. Their vectors are left for #314, and they clear when #325 lands.

## Decisions
1. **Flight-path-angle advice:** done (above). The steep wording differs from the suggested "turn over sooner" because that lever makes m12 worse.
2. **Fix it strength budget:** held at 8 steps. KH-16 gets the redesign note (strength 1.5).
3. **Engine-out contracts:** pantry-leo400-k3 is tracked for **#326**. For #326: S2 at exactly ×1.10 (38.5 t, not rounded to the tonne) passes 5/5.

## Kestrel Physics sign-off
- Parking cutoff and the departure Δv check, with the margins table (A): TBD
- Engine-out scoping (B): TBD
- Heron batch-vs-live ≤ 0.5% apo: TBD
- Preset retune, the predictor band and the Fix it rule (C): TBD
- Signed off by:
