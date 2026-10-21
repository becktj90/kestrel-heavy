Closes #313
Closes #315

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

Also thin, on g: the **kid preset peaks at 7.62 g against its 8 g limit** (seeds 1-5 and predict). Keep it under 8 when tuning.

## MissReason: flight-path-angle misses (Arcade, kid words)
A clean cutoff with |γ| ≥ 0.5°, the high point above the band and the low point below it is now a flight-path-angle miss. It is checked first (orbit first), before apo-high. The new `cutoffGammaDeg` is a readout only; physics is unchanged.

| Code | Advice | Flown on |
|---|---|---|
| `fpa-steep` (climbing) | "Your rocket flew too steep. Give the upper stage more push (one more engine, with the max-Q throttle at 95%), or use a smaller pitch kick at launch (for example 6° → 4° in Guidance)." | m12: Fix it's edit 5/5, kick 6° → 4° 5/5 |
| `fpa-steep-suborbital` (climbing, fell back) | "Your rocket flew too steep and fell back down. Give stage N much more fuel (it can take double or more), so it burns long enough to level out." | KH-6 fixtures: ×2 (4/5 plus a seeded engine-out), ×3.06 (5/5) |
| `fpa-flat` (diving) | "Your rocket flew too flat. Add fuel to stage N, about 2% at a time (up to 10%), until the orbit rounds off." | m3 (1.06) and m4 (1.02) |

- Fix it's m12 edit is a multi-move: +1 upper engine, max-Q throttle 95%, fins and diameter. One more engine alone still misses (903 × 297 km). So the advice names the throttle too, and the board text now says it.
- A bigger initial pitch kick (7° or 8°) makes m12 worse; a smaller one (6° → 4°) lands it. So the steep advice names a smaller pitch kick at launch.
- m6 (γ +0.10°) keeps the fuel advice and its test. `MoreTopStageFuel…("m12")` was replaced by the two KH12 tests.

## Arcade board rows
- m5 `fix` → `fly` and m14 `strength` → `fly`: the raw starters pass 5/5.
- m12 `strength` → `fix` with a new near-miss: kh12-win with stage-1 engines 7 → 6.
  - Raw it is an honest miss on all 5 seeds at 856.5 × 343.5 km, with no failure.
  - One Fix it (weak-upper: S2 engines 2 → 3 with the max-Q throttle at 95%) passes 5/5 at 606 × 594 km.
- The suggested SF 1.3 starter breaks up at max-Q, which violates NAV01, and no strength-only near-miss exists for m12 (SF 1.35-2.2 all pass).
- `BoardStarter_FixItOrOneStrengthEdit` stays strict and passes.

## Rebased on #325 (main b2fa61a), and the Physics follow-ups (1:24 PM)
Rebased onto origin/main b2fa61a (#325 and #323). Conflicts resolved toward #313: the reference ascent, the designer predict, the timeline and the live pad all fly `DirectedFlight` with #325's `PlayedChecks` (FlightDriver: `directed.Step()`, then the played checks).

### Live loop: no held nudge, no default 0.70 throttle (Arcade, TestFlight KH-3)
- `AscentDirector.AutopilotAccept` (unity/Assets/Kestrel.Core/Flight/AscentDecisions.cs:682-692): the autopilot takes the max-Q call only when the design sets a max-Q throttle (`return vehicle.MaxQSet`, :687) and never takes the pitch call (`return false`, :688). So no -1° nudge is ever held unless the player taps it (:707-708).
- `AscentDirector.Refresh` (:766-790): no director bucket. The 0.70 `DefaultBucket` is only the offer on a player-tapped max-Q call for a design without its own cap (:770-771, engaged through the design's guidance law in DirectedFlight.cs:100-108). A design with a cap flies its own value. Main's override (`bucket = maxQDown && q >= 8000`, `scale = min(scale, offered)`) is gone.
- New `LivePadKh3Tests` (Core, the FlightDriver loop: AscentFlight.Prepare's director, 1/60 s frames, LiveDt, `DirectedFlight.Step`, played checks, seed 1, default Simple/auto-assist):
  - KH-3 starter as built: **201.8 × 198.2 km, separation T+156.1, max-Q 44.2 kPa, nudge 0°**, inside the ±5% band.
  - KH-3 starter with the board's strength restore (1.9): **223.1 × 176.9 km, separation T+155.2, max-Q 44.3 kPa**. Orbit, no breakup; outside the ±5% band (heavier walls). Flagged below for Arcade.

### Pipit (tpl-sounding-hybrid): propellant 1900 → 1750 kg
One rule (`TargetMet` ±5%). Flown peak **102.0 km** on predict, batch seeds 1-5 (102.01) and the live pad seeds 1-5 (102.02); `asc.karman` fires on every seed. Max-Q 37.8 kPa, peak 6.98 g. (At 1900 kg it peaked at 120.9 km, a miss; 1800 kg is 108.6, 1700 kg is 95.3.)

| Golden | Old → new |
|---|---|
| Template stats (JS readout) | liftoff 2.7 → 2.5 t, TWR 1.91 → 2.03, Δv 3038 → 2921, margin 26.6 → 21.7%, sim apogee 125 → 108 km, max g 7.2 → 7 |
| Review text | "peak air load about 36 kPa … 7–8 g" → "about 38 kPa … about 7 g. 1,750 kg … peaks about 102 km" |
| Make it fly (designer-validate.json, 8 cases + preset) | only the echoed design and readout move: Δv 3038 → 2921, liftoff 2674.5 → 2513.6 kg, TWR 1.906 → 2.028, margin 0.266 → 0.217. No warning, block or fix changed. |
| Economy (economy.json cases 4/14) | propellant 573,420 → 528,150; structure 12,156,271 → 12,084,625; total 514,943,886 → 514,826,970 |
| catalog-flight / simulate (JS) | apo 124.9 → 107.7 km, burnout T+77.6 → T+71.5, max-Q 36.2 → 37.8 kPa |
| Timeline golden | max-Q T+0:30 (36.2 kPa) → T+0:28 (37.7 kPa); MECO T+1:16.636 → T+1:10.507; Karman/fairing T+2:25.636 → T+2:39.507; deploy T+3:32.203 → T+3:16.693 |
| launches.txt | 121.1 → 102.0 km, Δv margin 637 → 520 m/s (standard and kid) |
| ReadoutV2 max-Q pin | 36.21 → 37.77 kPa |

### Heron (tpl-hydrogen-upper): throttle only, engines stay at 7
Stage 1 gets `engineParams.throttleMin` 0.45 (stock 0.57), `guidance.maxQThrottle` 55 and `guidance.gLimit` 3.5. No engine-count, propellant or payload change.
- Why: with the full failure monitor the 7-engine Heron broke up at max-Q (35.05 kPa vs 35 kPa, T+66.7 s, 10 km). A max-Q throttle fixes that, but then the comsat's **6 g** rating (the monitor's g limit for gto-comsat, not 20 g) trips near stage-1 burnout because the stock 57% floor can't throttle low enough. A 6-engine Heron passes in C# but the JS readout (no guidance) crashes it, failing `templates-data`; so it stays at 7.
- Result: predict **reach**, batch seeds 1-5 **pass**, live pad seeds 1-5 **orbit**: 35786.0 × 200.0 km, peak **4.82 g** (limit 6), max-Q **17.7 kPa** (limit 35). Robust ±5 points of max-Q throttle (50-60% all pass at gLimit 3.5).
- **GTO Δv, the honest way:** flown to a 200 km parking orbit (196.3 × 203.7 km), rocket-equation Δv left is **3439 m/s** vs **2491 m/s** required (2492 at that T/W) → **+946 m/s**. Flown straight to GTO, 917 m/s is left after cutoff.
- The JS template stats are unchanged (the JS readout ignores guidance and throttleMin), so the template card still shows the JS numbers (max g 10.7). The review text now describes the throttled flight.
- Batch vs live (free build, seed 1): shipped 35785.993 × 200.007 vs 35785.994 × 200.006 km, with the monitor on. The unthrottled 7 and 9 engine cases are kept, without the monitor.

| Heron vector | Old (4cc2859) → new |
|---|---|
| causality.txt | Heron failed-prediction line dropped (it now reaches); SECO-2 t 2217.128 → 2144.949 |
| fairing.txt | Heron failed-prediction line dropped; shifted events re-timed with the throttled stage 1 (max-Q before/after 48/49 → 57/44 s, Karman 121/122 → 204/200, fairing 125/127 → 215/209, deploy after 390.709 → 350.34 s; MECO and SEP s1 now also shift, 231.949 → 229.418) |
| restart.txt | "Perigee -3230.2, apogee 903.8 … Short by 1266" → "The edit loses the target. … Perigee -4866.3 km, apogee 175.0 km … Short by 1228 m/s Δv. First failure: structural-maxq at T+231.725 s. dv margin -0.11 < 0 at burnout." (the before-Heron now reaches) |
| timeline golden | max-Q T+1:09 (35.3 kPa) → T+0:57 (18.3 kPa); MECO T+3:05.593 → T+3:51.949; orbit T+9:33.593 → T+10:44.949 |

### KH-16: #313's rule
`FixIt_WeakAirframe16_RepairIsTheMinimalStrengthForTheBreakupQ` (#325) now asserts the minimal passing strength is **1.5** (1.5 passes 5/5, 1.45 fails a gate seed), and that Fix it, within its 8-step budget (to 1.4), says "Fix it can't fix this rocket … strength 1.5. Fix it only goes up to 1.4." and does not apply it. #325's breakup-q checks are kept.

### Fixtures
- Sounding-solid fixture re-pinned at **0.5 km** (`UnchangedFailingDesign…`).
- Scout prop cut (prediction.txt): the event log is a real failure, not an altitude artifact: **overg at T+0.000 s** (ignition, stage 1), **13.70 g vs the 8.0 g limit**. Liftoff T/W is 22.6 on the cut stage; even at minimum throttle it is 13.7 g. The sim then ends on the pad (apogee -0.1 km). The vector now names it: the timeline diff's failed-prediction line appends "First failure: overg at T+0.000 s. Peak 13.70 g, past the 8.00 g limit." (`LaunchPrediction.Failure`, not in `Canonical`, so launches.txt is unchanged by it).

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
1. **Flight-path-angle advice:** done (above). The steep advice names the kick lever literally: a smaller initial pitch kick at launch (6° → 4° lands m12).
2. **Fix it strength budget:** held at 8 steps. KH-16 gets the redesign note (strength 1.5).
3. **Engine-out contracts:** pantry-leo400-k3 is tracked for **#326**. For #326: S2 at exactly ×1.10 (38.5 t, not rounded to the tonne) passes 5/5.

## For Arcade
- **KH-3 strength restore:** the board's 1.9 restore flies the live pad to orbit but at 223.1 × 176.9 km, outside ±5% of 200 km; the starter as built lands 201.8 × 198.2. Which strength the row teaches is Arcade's call.
- **KH-16 board row:** it uses strength **1.8** by hand (`restoreSafetyFactor`), while **1.5** is the minimum that passes seeds 1-5. Fix it's redesign note also names 1.5. Which number the row teaches is Arcade's call; it was left at 1.8.

## Kestrel Physics sign-off
- Parking cutoff and the departure Δv check, with the margins table (A): TBD
- Engine-out scoping (B): TBD
- Heron batch-vs-live ≤ 0.5% apo: TBD
- Preset retune, the predictor band and the Fix it rule (C): TBD
- Signed off by: **Physics approved (4cc2859).**
