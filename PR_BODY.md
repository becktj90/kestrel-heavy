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
  - KH-14 is fixed at strength 1.3. KH-16 now honestly reports "not fixed": its first passing strength is 1.5, past the 8-step budget.
  - KH-6 is asserted as an `fpa-miss` by name.
- **Contracts:** finch-leo400-k3 is now **c10-462-k7-u38 (cost 27495, +1260)** and viable on 5/5. pantry-leo400-k3 has no design within 2 cost steps and stays failing. ISSUE_ENGINEOUT_CONTRACTS.md is drafted for the engine-out balance PR (not filed).
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

## Arcade board rows
- m5 `fix` → `fly` and m14 `strength` → `fly`: the raw starters pass 5/5.
- m12 `strength` → `fix` with a new near-miss: kh12-win with stage-1 engines 7 → 6.
  - Raw it is an honest miss on all 5 seeds at 856.5 × 343.5 km, with no failure.
  - One Fix it (weak-upper: S2 engines 2 → 3) passes 5/5 at 606 × 594 km.
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
  - `FixIt_WhenTheMaxQThrottleLoftsIntoAMiss_NeverClaimsFixed`.

| Suite | Passed | Skipped | Failed |
|---|---:|---:|---:|
| Core | 880 | 12 | **4** |
| Site | 28 | 0 | 0 |
| Pad.Core | 84 | 0 | 0 |
| Vehicle.Core | 41 | 0 | 0 |
| Build | 13 | 0 | 0 |
| Guide.Core | 41 | 0 | 0 |
| Livery.Core / Livery.Branding | 15 / 39 | 0 | 0 |

node: 179 tests, 172 pass, 0 fail, 1 skipped, 6 todo. `stubcheck --all`: exit 0. Unity EditMode and PlayMode were not run here (GameCI only).

The 4 Core failures:
- **pantry-leo400-k3:** `EveryCell_AndEveryOffer_IsViable`. It is left failing and visible, per Arcade.
- **tpl-hydrogen-upper (#314):** `BrokenPrecondition…` and `UpperEngineSwap…`. Their vectors are left for #314.
- **`MissReasonTests.MoreTopStageFuelBringsTheHighPointIn("m12")`:** from #311 on main. The m12 board starter (#313's 6-engine near-miss) is an fpa miss (857 × 343 km). MissReason's "add top-stage fuel" advice does not bring it in: 1.10 still flies 866 × 334. Needs a decision.

## Decisions needed
1. **MissReason advice for a flight-path-angle miss:** either MissReason recognises the miss (energy on target, bad γ) and gives pitch or upper-engine advice, or m12's board starter changes. This is player-facing copy, so I left it.
2. **Fix it strength budget:** KH-16 at strength 1.0 needs 1.5 (10 steps) and StrengthSteps is 8, so Fix it reports not fixed. Keep the budget or raise it.
3. **Engine-out contracts (pantry):** see ISSUE_ENGINEOUT_CONTRACTS.md.

## Kestrel Physics sign-off
- Parking cutoff and the departure Δv check, with the margins table (A): TBD
- Engine-out scoping (B): TBD
- Heron batch-vs-live ≤ 0.5% apo: TBD
- Preset retune, the predictor band and the Fix it rule (C): TBD
- Signed off by:
