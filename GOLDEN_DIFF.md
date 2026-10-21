# GOLDEN_DIFF: #313 shared throttle director, Physics rulings A/B/C, Arcade board rows, and the Physics-approved follow-up

# Arcade follow-up (12:54 / 12:55 PM): flight-path-angle advice, KH-16 redesign note, pantry tracked for #326

ISSUE_ENGINEOUT_CONTRACTS.md was removed from the branch in 4117802 (filed as **#326**). No history was rewritten.

## 1. MissReason: flight-path-angle misses (orbit first, same style)
New `cutoffGammaDeg`: the flight-path angle at a clean "cutoff" event (NaN on a burnout or failure). It is set on SimResult (DirectedFlight.Finish), CampaignFlight and MissInput (FromFlight, PostFlightSource). This is a readout only and physics is unchanged: no flight golden moved.

A circular miss counts as a flight-path-angle miss when all of these hold: the cutoff was clean, |γ| ≥ 0.5°, the high point is above the band and the low point is below it. It comes before apo-high. Code and advice:

| Code | When | Advice |
|---|---|---|
| `fpa-steep` | γ > 0, low point above ground | "Your rocket flew too steep. Give the upper stage more push (one more engine, with the max-Q throttle at 95%), or use a smaller pitch kick at launch (for example 6° → 4° in Guidance)." |
| `fpa-steep-suborbital` | γ > 0, low point below ground | "Your rocket flew too steep and fell back down. Give stage N much more fuel (it can take double or more), so it burns long enough to level out." |
| `fpa-flat` | γ < 0 | "Your rocket flew too flat. Add fuel to stage N, about 2% at a time (up to 10%), until the orbit rounds off." |

The reason sentence keeps the band text and adds "The engines cut off while it was still climbing, 1.9° up." (or "already diving, 1.1° down.").

**Starters, old → new (MissReasonTests, seed 1):**
- m3 72.3 × 328 km, γ −1.1°: `apo-high` → **`fpa-flat`**. Fuel 1.06 lands it (unchanged test).
- m4 382 × 618 km, γ −1.0°: `apo-high` → **`fpa-flat`**. Fuel 1.02 lands it (unchanged test). The debrief card test's MissCode changes the same way.
- m6 188 × 212 km, γ +0.10°: stays **`apo-high`** (under 0.5°, a real fuel miss); fuel 1.02.
- m12 343 × 857 km, γ +1.9°: `apo-high` → **`fpa-steep`**. `MoreTopStageFuelBringsTheHighPointIn("m12")` was removed; fuel up to 1.10 still flies 866 × 334.
- m8 and m16: still `reliability`.

**What Fix it actually does for m12:** the board's "Upper engines 2 → 3" edit is a StepperFix multi-move: fins 1.10 → 3.10 m, max-Q throttle 100% → 95%, and upper stage 2 at 3.20 m → 3 at 3.50 m. I flew each part (seeds 1-5):

| m12 edit | Result |
|---|---|
| none | 856.5 × 343.5, γ +1.86 |
| +1 upper engine | 903.3 × 296.7, γ +1.84 (miss) |
| 95% max-Q only | 739.0 × 461.0, γ −1.14 (miss) |
| +1 upper engine **and** 95% max-Q | **606.5 × 593.5, pass 5/5** |
| Fix it's full edit | 606.0 × 594.0, pass 5/5 |
| pitch kick 6° → 5° | 832.3 × 367.7 (miss) |
| pitch kick 6° → **4°** | **602.4 × 597.6, pass 5/5** |
| pitch kick 7° or 8° | 898 × 302 or 943 × 257 (worse) |

The fins and diameter changes do nothing. So the advice names the engine *and* the throttle, and a smaller initial pitch kick at launch (a bigger one makes it worse). The board row's text now reads "Upper engines 2 → 3, with the max-Q throttle at 95%."

**New tests:**
- `KH12_SteepAdvice_IsFixItsEdit_AndFollowingItPassesEverySeed`: runs the board's Fix it, asserts +1 upper engine and 95% max-Q, and checks 5/5. The same edit by hand on the starter is also 5/5.
- `KH12_SmallerPitchKickAlsoLands_AndOneEngineAloneIsNotEnough`: kick 4° gives 5/5; +1 engine alone is still `fpa-steep`.
- `FlightPathAngleMiss_NeedsACleanTiltedCutoff`: steep, flat, suborbital, near-level (0.1°), NaN and low-point-in-band cases.
- The fuel test also rejects the new fpa codes as "landed", so it is not loosened.

## 2. KH-6 can't-fix fixtures: report text pinned
- **0.5 upper load, after Fix it:** the note is "Fix it could not fully fix this rocket. It still fails 5 of 5 test flights. Lighten the cargo or change the rocket."
  - Report: "Missed the 200 km circle: the orbit flew -12 km × 412 km, and the high point needs 190 km to 210 km. The low point, -12 km, needs 190 km to 210 km. The engines cut off while it was still climbing, 1.8° up. Your rocket flew too steep and fell back down. Give stage 3 much more fuel (it can take double or more), so it burns long enough to level out."
  - Following it, ×2 top fuel passes seeds 1, 3, 4 and 5; seed 2 is its seeded engine-out.
- **0.16 load, after the fuel Fix it:** same note. Report: "... -13.9 km × 414 km ... 1.8° up ..." with the same advice. ×3.06 passes 5/5.
- The steep levers do not move these: +1 or +2 engines, 95% max-Q, and kicks 0-3° all stay around 412 × −12. That is why they get their own suborbital advice.

## 3. KH-16: Fix it says plainly it can't fix it
- The strength budget holds at **8 steps** (asserted).
- When strength was the lever and the budget runs out, Fix it looks further on a copy, using the same steps and seeds, and never applies it. The note becomes `DesignerRepair.NeedsRedesign`:
  - old: "Fix it could not fully fix this rocket. It still fails 5 of 5 test flights. Lighten the cargo or change the rocket."
  - new: "**Fix it can't fix this rocket: it still fails 5 of 5 test flights. It needs a redesign with a stronger airframe: strength 1.5. Fix it only goes up to 1.4.**"
- The board's KH-16 row is mode `strength` (set strength 1.8 by hand) and does not rely on Fix it. No KH-16 tutorial card mentions Fix it; the generic `fail.retry` card applies to every mission.

## 4. pantry-leo400-k3: out of the viable set, tracked for #326
- data/golden/contracts-viable.json: cells **21 → 20**. A new `tracked` list holds the one row `pantry-leo400-k3`, with issue 326 and its full cell (3 designs).
  - failingDesign c8-360-k7-u35: seed 3, 637.7 × 162.3 km.
  - bestVariant: S2 propellant +10% (35 t → 38 t), cost 24708, passes 4/5 (seed 3: 652.6 × 147.4).
- ContractGen parses `tracked`, never deals it, and throws if an id is both viable and tracked.
- New test `TrackedCells_AreExactlyPantryLeo400K3_ForIssue326_AndStillMiss`: exactly one row; re-flies the miss and the 4/5 variant. It fails when #326 makes it fly.
- `EveryCell_AndEveryOffer_IsViable` is unchanged and strict for the other 20 cells.
- Side finding, not acted on: S2 at exactly ×1.10 (38.5 t, not rounded to the tonne) passes 5/5. It is a candidate for #326.

# Physics approval follow-up (12:14 PM): presets, predictor band, Fix it, finch/pantry, rebase

Rebased onto origin/main **e18218b**. These 9 commits landed on main since 079d19b: #300, #301, #304, #311, #312, #319, #320, #321 and #322. **#314 has not landed**, so the 9-engine Heron batch-vs-live case stays. tpl-hydrogen-upper and its timeline-diff vectors (causality.txt, restart.txt) are untouched; those belong to #314.

## 1. Presets (data/presets.json, from keep/presets_retune.patch, adjusted)

| Preset | Change | Before (8eb886c) | After, predict and seeds 1-5 |
|---|---|---|---|
| scout | both stages composite → **al-li** (cryoOK); guidance pitchKickAlt 500, pitchKickDeg 6, maxQThrottle 40, gLimit 5 | predict 443.1 × 356.8 km (fail under the strict rule), g 11.29, q 53.7 kPa; with the patch's composite tanks: cryo-material at T+0 | **410.1 × 389.9 km**, peak g **6.48**, max q **21.4 kPa** (limit 35.0), no failure; predict 410.0 × 389.9, pass |
| kid | guidance pitchKickAlt 34000, pitchKickDeg 3, turnShape 0.5, maxQThrottle 40, gLimit 6; stage 1 engineParams throttleMin 0.4 | predict 656.7 × −256.8 km (suborbital), g 18.87, q 90.1 kPa; seeds broke up at max-Q | **205.8 × 194.2 km**, peak g **7.62** (limit 8), max q **18.0 kPa** (limit 40), no failure; predict 205.7 × 194.2, pass |

**Deviations from the patch, both for real-performance reasons:**
- scout: the real cryo fix (al-li, rated for cryogenic propellant) made the stack heavier. With the patch's guidance (pitchKickAlt 1000, kick 8, gLimit 6) it flew 657 × 143 km, so I retuned it with `pgrid`. No exemption is used.
- kid: the patch's throttleMin 0.3 brought in EngineModel's deep-throttle mass penalty (below 0.4), which the JS mass model does not have. That broke JS mass parity (stage-0 dry 10423.5 vs 10400). At throttleMin 0.4 there is no penalty, and I retuned the kick and turn shape to match.

**Fixtures (new, tests/fixtures/designs/, like #314's sounding-solid.json):**
- `maxq-high-kid.json`: the pre-#313 kid. It still peaks at **90.07 kPa**, the maxq-high warn-not-block case (`ReadoutV2Tests.StabilityBand…`).
- `weak-scout.json`: the pre-#313 scout. Both breakup tests now load it (`FlightSimSinkTests.FlyWeakScout`, `PlayedBreakupTests.AVersion3Breakup`).
- Both are loaded with `Golden.FixtureDesign(name)`. **No assertion was changed.**

**Test updated to the new rule:** `DesignerV3GoldenTests.GV7`.
- A bench design that carries pitch settings may only use {pitchKickAlt, pitchKickDeg, turnShape, maxQThrottle, gLimit}. It must engage the monitor, get a guidance profile and fly its own kick.
- Designs without V3 fields keep the original strict asserts.
- Exactly {scout, kid} carry pitch settings.

**Golden values, old → new:**
- `DesignV2Tests.Version1_DoesNotDeriveInterstageMass`: 44129.78 → **44723.54** and 44215.28 → **44845.68** (al-li). docs/PHYSICS.md is updated to match.
- JS goldens (`npm run gen:golden`). At clean HEAD this produces no diff, so every change below comes from the presets:
  - scout liftoff 44329.78 → **44923.54** kg;
  - S1/S2 dry 2487.66/523.89 → **2994.53/610.78** kg;
  - ideal Δv 11823.0 → **11389.5** m/s;
  - recurring cost 906,949,027 → **796,263,665** (al-li structure 198.4M → 87.8M);
  - JS sim (no guidance) 467.2 × 434.7 → 419.9 × 267.7 km, max q 53.58 → 52.58 kPa;
  - kid gains guidance and engineParams in designer-validate, economy, vehicle and vehicle-migrate.
- Save-store golden: the scout design gains `guidance` and al-li, so the save-key count is **12 → 13**. `Ids_Keys_AndSession_MatchJs` passes against JS.
- `tests/vectors/prediction/launches.txt` (KESTREL_WRITE_PREDICT=1):
  - scout success false → **true**, 443.1 × 356.8 → **410.0 × 389.9**, Δv margin 3063 → 2629;
  - kid outcome suborbital → **orbit**, 656.7 × −256.8 → **205.7 × 194.2**, success false → **true**, and its timeline gains `orbit`.
- `tests/vectors/timeline/scout.json` and `kid.json` (KESTREL_WRITE_TIMELINES=1). TimelineGenerator now flies the design's own pitch settings, as Stack.SimInputs and the predictor do; without them its bare sim had scout suborbital. Changes:
  - scout: MECO 124.9 → 153.3 s, Karman/fairing 111 → 135 s, orbit insert 318.9 → 361.9 s, max-Q 43 → 44 s;
  - kid: MECO 90.4 → 146.0 s, max-Q 32 → 10 s, Karman/fairing 83 → 135 s, orbit insert 332.4 → 331.0 s.
- `tests/vectors/timeline-diff/prediction.txt`: the scout timelines above, and the cut scout misses at 7.6 km apogee (was 5.7 km).
- `tests/vectors/timeline-diff/fairing.txt`: **+1 line**, a failed-prediction for Heron without a fairing (924.7 × 22458.9 km against GTO). Under the old strict rule a 9-engine Heron with perigee ≥ 200 km "reached" GTO; under the batch band it does not. #314 changes Heron, so this vector will move again there.
- The predictor's miss text "the target is the perigee" now reads "the orbit is outside the target band (5%)".

## 2. Predictor band: one acceptance function

- `MissionScore.TargetMet(kind, apoTarget, periTarget, c3Target, apo, peri, c3, departureCovered, out criterion)` is the **only** ±5% target rule.
- `FlightBatch.TargetReached`, `FlightDebrief.GoalMet` and `LaunchPredictor.Accepts` all call it. There is no second copy of the band.
- The predictor judges a campaign design on its mission row (`ReferenceAscent.MissionFor`), with the same departure check. A free build is judged on its catalog orbit (`MissionScore.FreeOrbitKind`: hop, circle or ellipse). A flight with a failure is a miss.
- `CampaignFlight.reached` records the acceptance verdict before the mission gates.
- New test `PredictorBatchVerdictTests.Predictor_AndBatch_GiveTheSameVerdict_OnEveryTemplateAndPreset`: 6 templates and 4 presets, all agree. Scout and kid reach. Every template, stock and super-heavy miss on both sides; on the batch side they break up under the full monitor:
  - Heron, medium-kerolox and medium-kerolox-reusable: structural-maxq;
  - methane-reusable: overg / engine-out;
  - small-kerolox: cryo-material at T+0;
  - sounding-solid: overg;
  - stock: thermal;
  - super-heavy: engine-out / thermal.
  
  These template failures are not #313 changes and were not compared against main.
- `LaunchPredictorTests.Prediction_IsTheSim_AndTheTimeline` now recomputes success with `LaunchPredictor.Accepts`, not the old strict formula.
- The batch-vs-live checks (apo ≤ 0.5%, SMA ≤ 0.1%) are unchanged and pass, including Heron.
- `PropellantCut_FlagsAFailedPrediction…` and `UnchangedDesign_HasAnEmptyDiff` now pass honestly: scout reaches in band before the cut.

## 3. Fix it: a repair counts only when the mission passes on all 5 seeds

`DesignerRepair`:
- `FirstFailure` and `FailedSeeds` now use `MissKind`: the failure kind, else `fpa-miss`, `miss` or `gate-miss`, else null.
  - `fpa-miss`: an intact flight whose mean altitude (semi-major axis) is in the target band but whose apo or peri is out of it. It cut off at the target energy at a bad flight-path angle.
  - `miss`: any other clean miss, such as short or lofted.
  - `gate-miss`: target reached, another mission gate failed.
- `FullyFixed` requires every seed to pass.
- Strength keeps stepping through a clean miss once it has stopped a breakup, inside the same `StrengthSteps` budget. When the stepper's 95% max-Q throttle lofts into a miss, Fix it falls back to strength on the rocket as it was. Otherwise it reports not fixed, and never FullyFixed.
- No Fix it exists in web/js (grepped), so there was no JS change.

| Test | Old | New (real values) |
|---|---|---|
| FixIt_WeakAirframe…(14) | FullyFixed on a lofted miss; the knife-edge vanished | The stepper's 95% throttle lofts (739.6 × −2284.7 km), so Fix it takes strength on the rocket as it was: **Strength 1 → 1.3, 416.2 × 383.8 km, 5/5 pass**. 1.25 passes (409.6 × 390.4); 1.20 is an fpa-miss (516.6 × 283.4), so two steps under fails |
| FixIt_WeakAirframe…(16) | FullyFixed on a miss | Split out as **FixIt_WeakAirframe_ThatOnlyFliesIntoAMiss_IsNotFixed**: **not fixed, 5 of 5 fail**. Within 8 steps (1.0 → 1.4), strength trades the breakup for misses: 1.10-1.25 fpa-miss (635.9 × 364.1 at 1.20); 1.30-1.45 reach 500 km but miss the 0.98 reliability gate (gate-miss). The first strength that passes is **1.50** (500.6 × 499.4), two steps past the budget |
| new: FixIt_WhenTheMaxQThrottleLoftsIntoAMiss_NeverClaimsFixed | — | KH-14 at SF 1.0 with a 95% max-Q throttle: clean `miss` on all seeds (876.0 × −2079.2 km; 794.8 × −2728.9 at 1.4). Fix it: not fixed, 5 of 5 |
| FixIt_ThatCannotFixTheFlight… | expected a failure count; got 0 failures | KH-6 at half upper load: **fpa-miss** on 4 seeds (412.4 × −12.4 km; seed 2 engine-out). After Fix it, **fpa-miss on all 5** (412.0 × −12.0), note "still fails 5 of 5" |
| ReportFixIt_RanOutOfFuel… → **…_AddsFuelUntilItNoLongerRunsDry_ThenSaysTheMissRemains** | expected prop-depletion at 50% load; got none | At 50% KH-6 no longer runs dry. It runs dry at **16%** on all 5 seeds (depletion starts at ≤ 18%), so the fixture factor is 0.5 → 0.16. Fuel Fix it adds propellant (DeltaVAdded; the ascent is no longer short; no seed runs dry). With the mission, every seed is then an **fpa-miss** (413.9 × −13.9 km), so the result is not fixed, 5 of 5 |

KH-6's starter itself is an fpa-miss at 212.2 × 187.8 km (band 190-210). That is why board row m6 is "strength 1.8", which flies 203.5 × 196.5 km on 5/5.

## 4. Rebase interactions (tests that landed on main with pre-#313 numbers)

- **StarterRocketTests.Bases_…ShippedCopyMatchesTheGolden:** data/campaign-winners.json (new on main) now matches the #313 golden.
- **StarterScreenTests.Readiness_…:** #313 regrew KH-7's GTO winner, so "kerosene-3-f0-b0" now reads **Tight (+9.5%)**. The short example moved to "kerosene-2-f2-b0" (**about 6% short**). The assertions are unchanged.
- **MissReasonTests** (from #311), using the real #313 numbers:
  - m3: 192 × 221 → **72.3 × 328** km (38 m/s);
  - m4: 502 × 2,637 → **382 × 618** (32 m/s);
  - m6: 193 × 216 → **188 × 212** (4 m/s);
  - m12: 592 × 649 → **343 × 857** (69 m/s). These four now also name the low point, because both points are out of band (fpa misses).
  - m8: apo-high → **reliability** ("Reliability was 95.1%; this mission needs 98%").
  - m5 and m14 now pass (board rows "fly"), so they moved to `PassingStarterHasNoReason`.
  - `MoreTopStageFuel…`: m3 1.02 → **1.06**, m4 1.04 → **1.02**, m14 1.04 → **1.02**.
  - **m12 stays failing**: fuel does not bring an fpa miss in (1.10 flies 866 × 334). See the decisions below.

## 5. finch / pantry (Arcade re-search, ≤ 2 cost steps)

- **finch-leo400-k3: fixed.** c9-420-k7-u38 (26235) → **c10-462-k7-u38, cost 27495** (+1260): S1 engines 9 → 10 and S1 propellant 420000 → 462000. It passes 5/5 at 400.0 × 400.0 km.
- **pantry-leo400-k3: nothing within 2 steps.** c8-360-k7-u35 (24559) stays failing at seed 3, 637.7 × 162.3 km. The closest variant, S2 propellant +10% (24708), passes 4/5; seed 3 flies 652.6 × 147.4.
- The engine-out write-up (all 14 variants per row) was filed as **#326**; ISSUE_ENGINEOUT_CONTRACTS.md was removed in 4117802. pantry-leo400-k3 is now tracked for #326 (see the Arcade follow-up section at the top).

## 6. Re-run (seeds 1-5)

- dump2 m1-m20 (530 flights) is **identical** to 8eb886c: 0 flips vs e7. vs origin/main (079d19b baseline), 11 flips, all fail → pass (the m5 and m14 starters, and found m18 seed 2).
- Contracts: 63 designs, 62 viable; pantry is the 1 failure (finch is now viable).
- Presets: scout and kid pass 5/5 (above). Templates: all fail under the full monitor (above).

---

(The sections below describe 8eb886c. Its "Decisions needed" 1 and 2 are resolved above, and 3 is section 5.)


Local only (not pushed). This section is for this commit. Everything below "Earlier: Physics rulings 1-4" is the 961edad diff and is unchanged.
Runs: FlightBatch 0.2 s and the pad at 1/120 s, seeds 1-5. Baselines are **old** = origin/main before #313 (old2), **961edad** = the last commit, and **Physics A on old data** = this code with the 961edad goldens (e6). **new** = this commit (e7, contracts5).

## What changed

- **Physics A (i): parking cutoff and an honest departure check.** Ellipse and C3 targets now cut off at the circular parking orbit at the aim perigee, a = R + aim (`MissionFlightSetup.TargetEnergy`). Free builds are unchanged. A flight passes when the parking perigee is in band and **available ≥ required**:
  - available = Σ Isp·g0·ln(m_now/m_dry) on the propellant left after boil-off (`SimResult.departureDv`, staged in order);
  - required = the impulsive burn from parking × (1 + loss). GTO is 2455 m/s, so 2491 required. TLI (C3 −2) is 3133 m/s, so 3180 required;
  - loss is 1.5% at T/W ≥ 0.5. Below that it is 1.5%·(0.5/TW)², capped at 10%. With no thrust or no T/W it is 10% (`MissionScore.DepartureLoss`).
  
  The criterion string is `departure`. FlightBatch, FlightDebrief.GoalMet and the post-flight report share `FlightDebrief.DepartureMet`.
- **Physics B:** a stage-1 engine-out throttles only stage 1 (8/9). Stage 2 is untouched. A test is added.
- **Physics C:** finch-leo400-k3 and pantry-leo400-k3 stay real misses (below). The scout/kid preset retune is **not committed**: see "Decisions needed".
- **Arcade:** board rows m5 and m14 become `fly`. m12 gets a new near-miss starter (below). `BoardStarter_FixItOrOneStrengthEdit` is unchanged and strict.
- **Heron batch-vs-live acceptance (Physics):** a new test, `BatchAndLivePad_Agree_OnTheHeronTemplate_FreeBuild` (below).

## Δv margins (min over seeds 1-5; avail / need are the seed with the smallest margin)

"Old" is the 961edad design flown under Physics A. Every ellipse and C3 design was a real miss under the honest check, so every one was re-searched (option iii): S1 engines ×1-2.5 and prop ×1-1.9, S2 +0-2 engines and prop ×1-2.5, S3 +0-3 engines and prop ×1-2, kick 6°, turnShape 0.5/0.8.

A candidate was accepted only if it met all of these:
- seed 1 margin ≥ 60 m/s;
- parking perigee 192-208 km;
- all 5 seeds pass with margin ≥ 40 m/s;
- the 1/120 s live pad on seed 1 passes;
- it adds no new validation block.

The cheapest accepted design per row is shown. Ids are renamed from the real stage masses, except kh*-win, which keep their id.

| Row | Mission | Old design | Old avail / need (min margin), m/s | New design | New avail / need, m/s | New min margin (5 seeds) | T/W at cutoff | Parking perigee min, km | Seeds |
|---|---|---|---|---|---|---:|---:|---:|---:|
| winner | m7 | n5-p260-m80-u28 | 800 / 2491 (-1691) | n12-p416-m160-u42 | 2787 / 2491 | **+296** | 1.40 | 193.7 | 5/5 |
| viable | m7 | n5-p260-m80-u28 | 803 / 2491 (-1688) | n12-p416-m160-u42 | 2789 / 2491 | **+298** | 1.40 | 193.7 | 5/5 |
| viable | m7 | n7-p260-m80-u28 | 1185 / 2491 (-1306) | n10-p338-m200-u56 | 2637 / 2491 | **+146** | 1.66 | 197.8 | 5/5 |
| viable | m7 | n9-p260-m80-u28 | 1240 / 2491 (-1251) | n14-p416-m200-u42 | 2599 / 2491 | **+108** | 1.44 | 196.5 | 5/5 |
| winner | m9 | n4-p260-m100-u28 | 3265 / 3180 (+85) | n6-p260-m100-u42 | 3611 / 3180 | **+431** | 1.61 | 196.7 | 5/5 |
| viable | m9 | n4-p260-m100-u28 | 1885 / 3180 (-1295) | n6-p260-m100-u42 | 3611 / 3180 | **+431** | 1.61 | 196.7 | 5/5 |
| viable | m9 | n6-p260-m100-u28 | 3265 / 3180 (+85) | n9-p260-m100-u28 | 3512 / 3180 | **+332** | 2.50 | 195.5 | 5/5 |
| viable | m9 | n8-p260-m100-u28 | 3428 / 3180 (+248) | n8-p494-m100-u28 | 4462 / 3180 | **+1282** | 1.57 | 193.7 | 5/5 |
| winner | m10 | b9-p340-m83-u33-s14 | 83 / 2491 (-2408) | b16-p544-m165-u33-s22 | 2603 / 2491 | **+112** | 1.36 | 196.4 | 5/5 |
| viable | m10 | b8-p340-m82-u33-s14 | 2 / 2491 (-2489) | b16-p544-m165-u33-s22 | 2609 / 2491 | **+118** | 1.36 | 196.1 | 5/5 |
| viable | m10 | b9-p340-m83-u33-s14 | 92 / 2491 (-2399) | b22-p646-m208-u50-s27 | 2797 / 2491 | **+306** | 1.15 | 198.5 | 5/5 |
| viable | m10 | b10-p340-m83-u33-s14 | 145 / 2491 (-2346) | b20-p646-m208-u50-s27 | 2803 / 2491 | **+312** | 1.14 | 197.4 | 5/5 |
| winner | m15 | kh15-win | 887 / 2491 (-1604) | kh15-win | 2878 / 2491 | **+387** | 1.26 | 193.8 | 5/5 |
| winner | m17 | kh17-win | 2718 / 3180 (-462) | kh17-win | 4043 / 3180 | **+863** | 1.32 | 195.5 | 5/5 |
| winner | m18 | kh18-win | 3507 / 3254 (+253) | kh18-win | 3875 / 3254 | **+621** | 1.94 | 193.7 | 5/5 |
| winner | m19 | kh19-win | 210 / 2491 (-2281) | kh19-win | 2720 / 2491 | **+229** | 1.13 | 194.5 | 5/5 |
| winner | m20 | kh20-win | 3342 / 3261 (+81) | kh20-win | 3365 / 3261 | **+104** | 2.17 | 199.3 | 5/5 |

| Contract cell | Old design | Old seed-1 avail / need | New design | New seed-1 avail / need | New min margin (5 seeds) | Viable |
|---|---|---|---|---|---:|---|
| comsat-gto-k0 | t6-p300-m76-u26 | 449 / 2491 | t15-p570-m190-u39 | 2974 / 2491 | **+483** | True |
| comsat-gto-k0 | n5-p260-m80-u28 | 803 / 2491 | n12-p494-m80-u28 | 2580 / 2491 | **+88** | True |
| comsat-gto-k0 | n7-p260-m80-u28 | 1185 / 2491 | n14-p338-m160-u42 | 2647 / 2491 | **+155** | True |
| comsat-gto-k3 | n6-p260-m100-u28 | 764 / 2491 | n12-p416-m100-u28 | 2582 / 2491 | **+90** | True |
| comsat-gto-k3 | n6-p260-m100-u28-t0 | 989 / 2491 | n14-p416-m100-u28-t0 | 2624 / 2491 | **+133** | True |
| comsat-gto-k3 | n6-p260-m100-u28-t1 | 907 / 2491 | n12-p379-m150-u42-t1 | 2882 / 2491 | **+390** | True |
| comsat-gto-k6 | n8-p260-m100-u28 | 468 / 2491 | n12-p416-m150-u42 | 2585 / 2491 | **+94** | True |
| comsat-gto-k6 | n8-p260-m100-u28-t0 | 549 / 2491 | n9-p416-m200-u42-t0 | 2691 / 2491 | **+199** | True |
| comsat-gto-k6 | n8-p260-m100-u28-t1 | 740 / 2491 | n12-p553-m100-u28-t1 | 2671 / 2491 | **+179** | True |
| lark-gto-k0 | t9-p320-m64-u26 | 2457 / 2491 | t9-p320-m96-u26 | 2647 / 2491 | **+156** | True |
| lark-gto-k0 | n5-p260-m80-u28 | 1965 / 2491 | n10-p260-m80-u28 | 2623 / 2491 | **+132** | True |
| lark-gto-k0 | n7-p260-m80-u28 | 2283 / 2491 | n10-p416-m80-u28 | 3175 / 2491 | **+684** | True |
| lark-gto-k3 | t6-p300-m76-u26 | 1887 / 2491 | t9-p300-m152-u39 | 2873 / 2491 | **+382** | True |
| lark-gto-k3 | t7-p300-m70-u26 | 1940 / 2491 | t10-p390-m105-u39 | 2780 / 2491 | **+289** | True |
| lark-gto-k3 | t9-p320-m64-u26 | 2006 / 2491 | t9-p320-m128-u39 | 2711 / 2491 | **+220** | True |
| lark-gto-k6 | t7-p300-m70-u26 | 1522 / 2491 | t10-p480-m105-u39 | 2645 / 2491 | **+153** | True |
| lark-gto-k6 | t9-p320-m64-u26 | 1595 / 2491 | t14-p512-m128-u39 | 3246 / 2491 | **+755** | True |
| lark-gto-k6 | n5-p260-m80-u28 | 1107 / 2491 | n12-p494-m80-u28 | 2849 / 2491 | **+358** | True |
| lark-tli-k0 | t9-p320-m64-u26 | 2461 / 3180 | t9-p416-m160-u39 | 3517 / 3180 | **+337** | True |
| lark-tli-k0 | n5-p260-m80-u28 | 1973 / 3180 | n8-p416-m120-u42 | 3394 / 3180 | **+214** | True |
| lark-tli-k0 | n7-p260-m80-u28 | 2288 / 3180 | n10-p338-m200-u42 | 3317 / 3180 | **+137** | True |
| lark-tli-k3 | t6-p300-m76-u26 | 1891 / 3180 | t12-p480-m152-u39 | 3689 / 3180 | **+509** | True |
| lark-tli-k3 | t7-p300-m70-u26 | 1944 / 3180 | t10-p480-m140-u39 | 3313 / 3180 | **+133** | True |
| lark-tli-k3 | t9-p320-m64-u26 | 2008 / 3180 | t14-p416-m128-u39 | 3313 / 3180 | **+133** | True |
| lark-tli-k6 | n4-p260-m100-u28 | 871 / 3180 | n8-p494-m100-u28 | 3661 / 3180 | **+481** | True |
| lark-tli-k6 | n6-p260-m100-u28 | 2317 / 3180 | n9-p494-m100-u28 | 4051 / 3180 | **+871** | True |
| lark-tli-k6 | n8-p260-m100-u28 | 2547 / 3180 | n8-p338-m200-u42 | 3351 / 3180 | **+171** | True |

**No design was found for:** none. Every row has a design. The first pass found nothing for m10, m19, comsat-gto-k6 and lark-tli-k6; that was a scratch-tool schema false positive on strap-on `crossfeed`. A rerun with the fix found designs for all of them.

**m10 note:** the cheapest m10 winner from the b9 base (b22-p646-m208-u50-s27) passes the batch but fails `HavenLandingGoldenTests.Assisted_Lands…`: the core lands at 524 m/s, 6 km off the deck. I used the b8-based b16-p544-m165-u33-s22 for the winner, found and board rows. It passes Haven and is cheaper (43,013 vs 49,995). The b22 and b20 designs stay as m10 viables, which Haven does not fly.

## Golden file changes (old → new)

The full original rows are in the scratch log `physA_log.json`.

| File | Row | Design (old → new) | Change |
|---|---|---|---|
| campaign-winners.json | m7 | n5-p260-m80-u28 → **n12-p416-m160-u42** | S1 engines 5→12, prop 260→416 t, landing 31.2→49.92 t; S2 engines 1→2, prop 80→160 t, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| campaign-winners.json | m9 | n4-p260-m100-u28 → **n6-p260-m100-u42** | S2 engines 1→2, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| campaign-winners.json | m10 | b9-p340-m83-u33-s14 → **b16-p544-m165-u33-s22** | S1 engines 9→16, prop 340→544 t, landing 68→108.8 t; S2 engines 1→2, prop 83→165 t, ⌀ 3.6→4.5 m; straps prop 14→22.4 t; kick 4→6°, turnShape 0.2→0.5 |
| campaign-winners.json | m15 | kh15-win → **kh15-win** | S1 engines 7→10, prop 260→416 t, landing 31.2→49.92 t; S2 engines 1→3, prop 80→200 t, ⌀ 3.6→5.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| campaign-winners.json | m17 | kh17-win → **kh17-win** | S1 engines 6→9, prop 260→338 t; S2 engines 1→2, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| campaign-winners.json | m18 | kh18-win → **kh18-win** | S1 prop 250→325 t; kick 4→6°, turnShape 0.2→0.8 |
| campaign-winners.json | m19 | kh19-win → **kh19-win** | S1 engines 10→15, prop 374→711 t, landing 68→129.272 t; S2 engines 1→3, prop 83→208 t, ⌀ 3.6→5.5 m; S3 prop 33→50 t; straps prop 14→26.6 t; kick 4→6°, turnShape 0.2→0.5 |
| campaign-winners.json | m20 | kh20-win → **kh20-win** | S1 engines 7→10, prop 260→338 t; S3 engines 3→4, prop 28→42 t, ⌀ 3.6→4.1 m; kick 4→6°, turnShape 0.2→0.5 |
| campaign-viable.json | m7 | n5-p260-m80-u28 → **n12-p416-m160-u42** | S1 engines 5→12, prop 260→416 t, landing 31.2→49.92 t; S2 engines 1→2, prop 80→160 t, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| campaign-viable.json | m7 | n7-p260-m80-u28 → **n10-p338-m200-u56** | S1 engines 7→10, prop 260→338 t, landing 31.2→40.56 t; S2 engines 1→3, prop 80→200 t, ⌀ 3.6→5.5 m; S3 engines 3→4, prop 28→56 t, ⌀ 3.6→4.1 m; kick 4→6°, turnShape 0.2→0.8 |
| campaign-viable.json | m7 | n9-p260-m80-u28 → **n14-p416-m200-u42** | S1 engines 9→14, prop 260→416 t, landing 31.2→49.92 t; S2 engines 1→2, prop 80→200 t, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| campaign-viable.json | m9 | n4-p260-m100-u28 → **n6-p260-m100-u42** | S1 engines 4→6; S2 engines 1→2, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| campaign-viable.json | m9 | n6-p260-m100-u28 → **n9-p260-m100-u28** | S1 engines 6→9; S3 engines 3→4, ⌀ 3.6→4.1 m; kick 4→6°, turnShape 0.2→0.5 |
| campaign-viable.json | m9 | n8-p260-m100-u28 → **n8-p494-m100-u28** | S1 prop 260→494 t; kick 4→6°, turnShape 0.2→0.5 |
| campaign-viable.json | m10 | b8-p340-m82-u33-s14 → **b16-p544-m165-u33-s22** | S1 engines 8→16, prop 340→544 t, landing 68→108.8 t; S2 engines 1→2, prop 82.4→165 t, ⌀ 3.6→4.5 m; straps prop 14→22.4 t; kick 4→6°, turnShape 0.2→0.5 |
| campaign-viable.json | m10 | b9-p340-m83-u33-s14 → **b22-p646-m208-u50-s27** | S1 engines 9→22, prop 340→646 t, landing 68→129.2 t, ⌀ 3.6→4.2 m; S2 engines 1→3, prop 83→208 t, ⌀ 3.6→5.5 m; S3 prop 33→50 t; straps prop 14→26.6 t; kick 4→6°, turnShape 0.2→0.5 |
| campaign-viable.json | m10 | b10-p340-m83-u33-s14 → **b20-p646-m208-u50-s27** | S1 engines 10→20, prop 340→646 t, landing 68→129.2 t, ⌀ 3.6→4.0 m; S2 engines 1→3, prop 83→208 t, ⌀ 3.6→5.5 m; S3 prop 33→50 t; straps prop 14→26.6 t; kick 4→6°, turnShape 0.2→0.5 |
| found.json | m7 | n5-p260-m80-u28 → **n12-p416-m160-u42** | S1 engines 5→12, prop 260→416 t, landing 31.2→49.92 t; S2 engines 1→2, prop 80→160 t, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| found.json | m9 | n4-p260-m100-u28 → **n6-p260-m100-u42** | S1 engines 4→6; S2 engines 1→2, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| found.json | m10 | b9-p340-m83-u33-s14 → **b16-p544-m165-u33-s22** | S1 engines 9→16, prop 340→544 t, landing 68→108.8 t; S2 engines 1→2, prop 83→165 t, ⌀ 3.6→4.5 m; straps prop 14→22.4 t; kick 4→6°, turnShape 0.2→0.5 |
| found.json | m15 | kh15-win → **kh15-win** | S1 engines 7→10, prop 260→416 t, landing 31.2→49.92 t; S2 engines 1→3, prop 80→200 t, ⌀ 3.6→5.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| found.json | m17 | kh17-win → **kh17-win** | S1 engines 4→8; S2 engines 1→2, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.8 |
| found.json | m18 | kh18-win → **kh18-win** | S1 engines 4→6, prop 260→338 t; kick 4→6°, turnShape 0.2→0.8 |
| found.json | m19 | kh19-win → **kh19-win** | S1 engines 10→15, prop 374→711 t, landing 68→129.272 t; S2 engines 1→3, prop 83→208 t, ⌀ 3.6→5.5 m; S3 prop 33→50 t; straps prop 14→26.6 t; kick 4→6°, turnShape 0.2→0.5 |
| found.json | m20 | kh20-win → **kh20-win** | S1 engines 4→6; S2 engines 1→2, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| board-starters.json | m7 | n5-p260-m80-u28 → **n12-p416-m160-u42** | S1 engines 5→12, prop 260→416 t, landing 31.2→49.92 t; S2 engines 1→2, prop 80→160 t, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| board-starters.json | m9 | n4-p260-m100-u28 → **n6-p260-m100-u42** | S2 engines 1→2, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| board-starters.json | m10 | b9-p340-m83-u33-s14 → **b16-p544-m165-u33-s22** | S1 engines 9→16, prop 340→544 t, landing 68→108.8 t; S2 engines 1→2, prop 83→165 t, ⌀ 3.6→4.5 m; straps prop 14→22.4 t; kick 4→6°, turnShape 0.2→0.5 |
| board-starters.json | m15 | kh15-win → **kh15-win** | S1 engines 7→10, prop 260→416 t, landing 31.2→49.92 t; S2 engines 1→3, prop 80→200 t, ⌀ 3.6→5.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| board-starters.json | m17 | kh17-win → **kh17-win** | S1 engines 6→9, prop 260→338 t; S2 engines 1→2, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| board-starters.json | m18 | kh18-win → **kh18-win** | S1 prop 250→325 t; kick 4→6°, turnShape 0.2→0.8 |
| board-starters.json | m19 | kh19-win → **kh19-win** | S1 engines 10→15, prop 374→711 t, landing 68→129.272 t; S2 engines 1→3, prop 83→208 t, ⌀ 3.6→5.5 m; S3 prop 33→50 t; straps prop 14→26.6 t; kick 4→6°, turnShape 0.2→0.5 |
| board-starters.json | m20 | kh20-win → **kh20-win** | S1 engines 7→10, prop 260→338 t; S3 engines 3→4, prop 28→42 t, ⌀ 3.6→4.1 m; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | comsat-gto-k0 | comsat-gto-k0-t6-p300-m76-u26 → **comsat-gto-k0-t15-p570-m190-u39** | S1 engines 6→15, prop 300→570 t, landing 60→114 t; S2 engines 1→2, prop 95→190 t, ⌀ 3.6→4.5 m; S3 engines 3→4, prop 26→39 t, ⌀ 3.6→4.1 m; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | comsat-gto-k0 | comsat-gto-k0-n5-p260-m80-u28 → **comsat-gto-k0-n12-p494-m80-u28** | S1 engines 5→12, prop 260→494 t, landing 31.2→59.28 t; kick 4→6°, turnShape 0.2→0.8 |
| contracts-viable.json | comsat-gto-k0 | comsat-gto-k0-n7-p260-m80-u28 → **comsat-gto-k0-n14-p338-m160-u42** | S1 engines 7→14, prop 260→338 t, landing 31.2→40.56 t; S2 engines 1→2, prop 80→160 t, ⌀ 3.6→4.5 m; S3 engines 3→4, prop 28→42 t, ⌀ 3.6→4.1 m; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | lark-tli-k0 | lark-tli-k0-t9-p320-m64-u26 → **lark-tli-k0-t9-p416-m160-u39** | S1 prop 320→416 t, landing 64→83.2 t; S2 engines 1→2, prop 64→160 t, ⌀ 3.6→4.5 m; S3 prop 26→39 t; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | lark-tli-k0 | lark-tli-k0-n5-p260-m80-u28 → **lark-tli-k0-n8-p416-m120-u42** | S1 engines 5→8, prop 260→416 t, landing 31.2→49.92 t; S2 engines 1→2, prop 80→120 t, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.8 |
| contracts-viable.json | lark-tli-k0 | lark-tli-k0-n7-p260-m80-u28 → **lark-tli-k0-n10-p338-m200-u42** | S1 engines 7→10, prop 260→338 t, landing 31.2→40.56 t; S2 engines 1→2, prop 80→200 t, ⌀ 3.6→4.5 m; S3 engines 3→4, prop 28→42 t, ⌀ 3.6→4.1 m; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | lark-gto-k0 | lark-gto-k0-t9-p320-m64-u26 → **lark-gto-k0-t9-p320-m96-u26** | S2 prop 64→96 t; kick 4→6°, turnShape 0.2→0.8 |
| contracts-viable.json | lark-gto-k0 | lark-gto-k0-n5-p260-m80-u28 → **lark-gto-k0-n10-p260-m80-u28** | S1 engines 5→10; kick 4→6°, turnShape 0.2→0.8 |
| contracts-viable.json | lark-gto-k0 | lark-gto-k0-n7-p260-m80-u28 → **lark-gto-k0-n10-p416-m80-u28** | S1 engines 7→10, prop 260→416 t, landing 31.2→49.92 t; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | comsat-gto-k3 | comsat-gto-k3-n6-p260-m100-u28 → **comsat-gto-k3-n12-p416-m100-u28** | S1 engines 6→12, prop 260→416 t; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | comsat-gto-k3 | comsat-gto-k3-n6-p260-m100-u28-t0 → **comsat-gto-k3-n14-p416-m100-u28-t0** | S1 engines 7→14, prop 260→416 t; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | comsat-gto-k3 | comsat-gto-k3-n6-p260-m100-u28-t1 → **comsat-gto-k3-n12-p379-m150-u42-t1** | S1 engines 6→12, prop 291.2→379 t; S2 engines 1→2, prop 100→150 t, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | lark-gto-k3 | lark-gto-k3-t6-p300-m76-u26 → **lark-gto-k3-t9-p300-m152-u39** | S1 engines 6→9; S2 engines 1→2, prop 76→152 t, ⌀ 3.6→4.5 m; S3 prop 26→39 t; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | lark-gto-k3 | lark-gto-k3-t7-p300-m70-u26 → **lark-gto-k3-t10-p390-m105-u39** | S1 engines 7→10, prop 300→390 t, landing 60→78 t; S2 engines 1→2, prop 70→105 t, ⌀ 3.6→4.5 m; S3 prop 26→39 t; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | lark-gto-k3 | lark-gto-k3-t9-p320-m64-u26 → **lark-gto-k3-t9-p320-m128-u39** | S2 engines 1→2, prop 64→128 t, ⌀ 3.6→4.5 m; S3 prop 26→39 t; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | lark-tli-k3 | lark-tli-k3-t6-p300-m76-u26 → **lark-tli-k3-t12-p480-m152-u39** | S1 engines 6→12, prop 300→480 t, landing 60→96 t; S2 engines 1→2, prop 76→152 t, ⌀ 3.6→4.5 m; S3 prop 26→39 t; kick 4→6°, turnShape 0.2→0.8 |
| contracts-viable.json | lark-tli-k3 | lark-tli-k3-t7-p300-m70-u26 → **lark-tli-k3-t10-p480-m140-u39** | S1 engines 7→10, prop 300→480 t, landing 60→96 t; S2 engines 1→2, prop 70→140 t, ⌀ 3.6→4.5 m; S3 prop 26→39 t; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | lark-tli-k3 | lark-tli-k3-t9-p320-m64-u26 → **lark-tli-k3-t14-p416-m128-u39** | S1 engines 9→14, prop 320→416 t, landing 64→83.2 t; S2 engines 1→2, prop 64→128 t, ⌀ 3.6→4.5 m; S3 prop 26→39 t; kick 4→6°, turnShape 0.2→0.8 |
| contracts-viable.json | comsat-gto-k6 | comsat-gto-k6-n8-p260-m100-u28 → **comsat-gto-k6-n12-p416-m150-u42** | S1 engines 8→12, prop 260→416 t; S2 engines 1→2, prop 100→150 t, ⌀ 3.6→4.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | comsat-gto-k6 | comsat-gto-k6-n8-p260-m100-u28-t0 → **comsat-gto-k6-n9-p416-m200-u42-t0** | S1 prop 260→416 t; S2 engines 1→3, prop 100→200 t, ⌀ 3.6→5.5 m; S3 prop 28→42 t; kick 4→6°, turnShape 0.2→0.8 |
| contracts-viable.json | comsat-gto-k6 | comsat-gto-k6-n8-p260-m100-u28-t1 → **comsat-gto-k6-n12-p553-m100-u28-t1** | S1 engines 8→12, prop 291.2→553 t; S3 engines 3→4, ⌀ 3.6→4.1 m; kick 4→6°, turnShape 0.2→0.8 |
| contracts-viable.json | lark-tli-k6 | lark-tli-k6-n4-p260-m100-u28 → **lark-tli-k6-n8-p494-m100-u28** | S1 engines 4→8, prop 260→494 t; kick 4→6°, turnShape 0.2→0.8 |
| contracts-viable.json | lark-tli-k6 | lark-tli-k6-n6-p260-m100-u28 → **lark-tli-k6-n9-p494-m100-u28** | S1 engines 6→9, prop 260→494 t; kick 4→6°, turnShape 0.2→0.8 |
| contracts-viable.json | lark-tli-k6 | lark-tli-k6-n8-p260-m100-u28 → **lark-tli-k6-n8-p338-m200-u42** | S1 prop 260→338 t; S2 engines 1→2, prop 100→200 t, ⌀ 3.6→4.5 m; S3 engines 3→4, prop 28→42 t, ⌀ 3.6→4.1 m; kick 4→6°, turnShape 0.2→0.8 |
| contracts-viable.json | lark-gto-k6 | lark-gto-k6-t7-p300-m70-u26 → **lark-gto-k6-t10-p480-m105-u39** | S1 engines 7→10, prop 300→480 t, landing 60→96 t; S2 engines 1→2, prop 70→105 t, ⌀ 3.6→4.5 m; S3 prop 26→39 t; kick 4→6°, turnShape 0.2→0.5 |
| contracts-viable.json | lark-gto-k6 | lark-gto-k6-t9-p320-m64-u26 → **lark-gto-k6-t14-p512-m128-u39** | S1 engines 9→14, prop 320→512 t, landing 64→102.4 t; S2 engines 1→2, prop 64→128 t, ⌀ 3.6→4.5 m; S3 prop 26→39 t; kick 4→6°, turnShape 0.2→0.8 |
| contracts-viable.json | lark-gto-k6 | lark-gto-k6-n5-p260-m80-u28 → **lark-gto-k6-n12-p494-m80-u28** | S1 engines 5→12, prop 260→494 t, landing 31.2→59.28 t; kick 4→6°, turnShape 0.2→0.8 |

- `data/board-starters.json`:
  - **m5** `fix` → `fly`: "Fly this rocket. It already clears every seed." The raw starter passes 5/5.
  - **m14** `strength` → `fly`: same text. The raw SF 1.35 passes 5/5.
  - **m12** `strength` → `fix`, with a new near-miss. The board design is kh12-win with stage-1 engines 7 → 6 (SF stays 1.55). Edit text: "Auto Fix it. Upper engines 2 → 3. With six first-stage engines the upper stage pushes at only 0.48 g, and the orbit comes out 856 × 344 km instead of the 600 km circle on every seed."
    - Before: an honest miss on all 5 seeds, 856.5 × 343.5 km, criterion altitude, **no failure**. That matters because NAV01 and DesignFirstRocketsAreReady require our own starters never to break.
    - One Fix it (check weak-upper) passes 5/5 at 606.0 × 594.0 km. The Fix-it diff: S2 engines 2→3, S2 ⌀ 3.2→3.5 m, fin span 1.1→3.1 m, maxQThrottle 95, nozzle and interstage defaults.
    - Arcade's suggested SF 1.3 starter was dropped. It broke up at max-Q on every seed, which fails NAV01 ("our own starters always fly"), and SF 1.35-2.2 all pass raw. So no strength-only near-miss exists for m12.
- `data/golden/README.md`: the comsat-gto-k0-t6 id note now points at its regrown id.

## Verdict flips

- **vs origin/main (old2): 11 of 530, all fail → pass.** The m5 starter (5 seeds), the m14 starter (5) and found m18 seed 2 (an old engine-out).
- vs 961edad (e5): 111 flips (106 fail → pass, 5 pass → fail). The 5 are the new m12 near-miss starter, which is intended.
- vs Physics A on old data (e6): 210 flips (205 fail → pass, 5 pass → fail, the same m12 starter).

Every winner, viable, found design, board starter (after its edit) and contract design passes on all 5 seeds, except the two contract misses below.

## Remaining misses

- **Contracts, kept failing (Physics C), flagged to Arcade for contract viability:**
  - pantry-leo400-k3-c8-360-k7-u35, seed 3: 637.7 × 162.3 km, γ 0.002° at cutoff;
  - finch-leo400-k3-c9-420-k7-u38, seed 2: 657.8 × 142.2 km, γ 0.003°.
  
  A seeded stage-1 engine-out leaves the upper stage low, and no kick fixes it. Both keep `EveryCell_AndEveryOffer_IsViable` red.
- **Pre-existing k6 fairing blocks (not new, not changed):** the k6 contract cells carry payload-doesnt-fit. lark-tli-k6 needs 30 m³ in 19.8. comsat-gto-k6 needs 82.1 in 75.6. The search only rejected new block codes.

## Heron (tpl-hydrogen-upper) batch vs live

#314 has **not** landed on main: origin/main is 079d19b, and Heron still has 9 stage-1 engines. The template file is not edited. The test checks both the 9-engine template and #314's 7-engine Heron in memory. It uses a free build, seed 1, the shared director with the in-step energy cutoff, and no failure monitor.

| Heron | Batch 0.2 s apo × peri, km | Live 1/120 s apo × peri, km | Apo Δ | Peri Δ | Result |
|---|---|---|---:|---:|---|
| 7 engines (#314) | 35786.099 × 199.901 | 35786.095 × 199.905 | 0.00001% | 4 m | PASS ≤ 0.5% |
| 9 engines (main) | 6982.202 × 879.603 | 6979.026 × 879.659 | 0.045% | 56 m | PASS ≤ 0.5% |

The 271 vs 230 km Physics measured is the 7-engine Heron on the **JS-parity** `Stack.SimInputs` path. That path keeps the perigee cutoff on the step and measures 271.162 × 190.774 batch vs 230.117 × 190.081 live. Kestrel flights no longer use it, and it is left for JS parity.

With the failure monitor on, both Herons break up at max-Q (9 engines at T+34.5 s, 7 engines at T+66.6 s), so the live pad never reaches orbit. That is #314's to fix.

## Test expectations changed (none loosened)

- `SharedDirectorTests.TheEnginesCutOffAtTheTargetEnergy…`: the ellipse and C3 target energy is now EnergyFor(aim perigee, aim perigee). It was the ellipse mean / C3/2. The old expectation was superseded by Physics A.
- `MissionFlightSetupTests.Setup_HoldsOutCoastBoilOffAndStrapOnLandingFuel`: the KH-10 strap prop is 14,000 → 22,400 kg and the landing hold 2,800 → 4,480 kg (free build 14,000 → 22,400). These values are pinned from the regrown m10 winner.
- New tests:
  - `DepartureCheckTests`: the parking cutoff for m7, m9, m10 and m17 (circular m8 unchanged); GTO 2455 ± 15 and TLI 3133 ± 15 m/s; the loss law; the pass/fail edge (exact need passes, need − 1 fails, peri out of band fails, empty check fails); the old ideal-stack Δv no longer passes.
  - `SharedDirectorTests`: AStageOneEngineOut_ThrottlesOnlyStageOne_NotTheUpperStage and BatchAndLivePad_Agree_OnTheHeronTemplate_FreeBuild.

## Fix-it investigation (4 tests): not fixture knife-edges, needs a product decision

None of the four is a fixture value that moved with real performance. All four are DesignerRepair behaviour under the new physics. No fixture or assertion was changed.

- **FixIt_WeakAirframe…(14) and (16)** ("two steps under 1.4 / 1.3 is the knife-edge"): Fix it adds a 95% max-Q throttle. Under ruling 4 HoldKickThroughMaxQ then lofts the rocket, so the flight misses without a failure, and `FullyFixed` counts a miss as fixed. The knife-edge (still failing two steps under) disappears.
- **FixIt_ThatCannotFixTheFlight_SaysSo…** ("still fails a gate seed", 0) and **ReportFixIt_RanOutOfFuel…** (expected prop-depletion, got none): the KH-6 fixture premise moved. The rocket now reaches the target energy at a bad flight-path angle and does not run dry. Depletion appears only when propellant is cut to about 16-19%.
- **Decision:** either DesignerRepair counts a miss as unfixed (a product change), or closed-loop upper-stage guidance lands first (FOLLOWUP_CLOSED_LOOP.md). I stopped there.

## Decisions needed

1. **Scout/kid preset retune: blocked by existing invariants, not committed.** The tuned values are below, and the patch is saved on the box.
   - kid: guidance pitchKickAlt 34000, pitchKickDeg 2, turnShape 0.2, maxQThrottle 40, gLimit 5, and stage-1 throttleMin 0.3. Predict and seeds 1-5 all give 203.0 × 197.0 km, **peak g 5.71** (limit 8) and **max q 18.0 kPa** (limit 40), with no failure. The 32.6 g breakup is gone. Before: predict 656.7 × −256.8 km, g 18.87, q 90.1 kPa; seeds broke at max-Q at T+16.1 s (67 kPa).
   - scout: guidance pitchKickAlt 1000, pitchKickDeg 8, maxQThrottle 40, gLimit 6. Predict gives 408.4 × 391.5 km, g 6.13 (limit 10), q 22.0 kPa (limit 49). Before: 443.1 × 356.8 km, g 11.29, q 53.7 kPa. Its predict monitor and every seed still report cryo-material at T+0 (composite tanks with methalox). That is a pre-existing material issue, and the material was not changed.
   - Committing it turns 6 passing tests red, and fixing them would mean changing their premises:
     - GV7_NoV3Fields_DoNotEngage (presets must carry no V3 guidance);
     - StabilityBand…MaxQHighDoesNotBlock (kid is the > 45 kPa max-Q fixture);
     - FlightSimSink.FailureEngineAndImpactFireOnce and PlayedBreakup.AVersion3BreakupStopsThePlayedFlight (the weak scout must break up);
     - Ids_Keys_AndSession_MatchJs (save key count vs JS);
     - GoldenTimelines.
   - In addition, with the exact energy cutoff a circular orbit always has perigee ≤ target. LaunchPredictor's strict `peri ≥ target` test therefore cannot pass for any preset, so "reaches target on predict" is unreachable as written. Proposal: predict success = the same ±5% apo/peri band as FlightBatch. Not implemented.
2. **Fix it:** see above.
3. **Contract viability for finch/pantry leo400-k3:** for Arcade.


# Earlier: Physics rulings 1-4 (961edad)

Branch `fix/shared-throttle-director`, local only (not pushed). Baseline "old" is `origin/main` before #313 (old2 run); "new" is this commit.
Batch is FlightBatch at 0.2 s. Live is the pad setup at 1/120 s with the batch monitor and the shared director. γ is the flight-path angle at engine cutoff (or at the last thrusting state on a burnout or failure).

Rulings applied:
1. The engines cut off at the target energy itself, with no 10 km offset and no half-band. The scoring bands are unchanged. An FPA miss is a real miss.
2. Energy cutoff for batch and live in the same code: ε = −μ/(2a), with a = R + (h_apo + h_peri)/2 (c3: ε = C3/2). It is located by bisection inside the step. Kestrel flights have no perigee cutoff (it stays only on the JS-parity `Stack.SimInputs`/raw `SimOptions` path).
3. The fuel-imbalance penalty throttles only its stage and ends when that stage separates or shuts down. I applied the same scoping to the stage-1 engine-out throttle (8/9 on a 9-engine stage), which had been carrying into the upper stage. **Physics: please confirm this extension.**
4. The design's GuidanceProfile max-Q throttle is the only max-Q law. A taken max-Q call on a design without one engages that same law (`EngageMaxQCap`). So bare sim = batch = designer = pad for every design with no events. KH-1 is back at 515 kg.

## Acceptance checks

| Check | Result |
|---|---|
| KH-1 starter ≥ 9.7 km on every seed | **PASS**: 9.786, 9.786, 9.786, 9.786, 9.786 km batch; live 9.787 km |
| m1b ≥ 9.7 km | **PASS**: 705 kg, 10.000 km on every seed (live 10.004) |
| m2b ≥ 97 km | **PASS**: 2585 kg, 101.017 km on every seed (live 101.027) |
| m2f ≥ 97 km | **PASS**: 1580 kg, 100.098 km on every seed (live 100.156) |
| Batch vs live: apo 0.5%, peri 1 km, SMA 0.1%, every winner and starter | **PASS for every flight that reaches orbit** (worst: m7 n5 0.40% apo and 0.09% SMA; m4 raw starter 0.88 km peri). **FAIL** only on the prop-depletion real misses m10 (1.76% apo), m17 (0.33% apo, 0.27% SMA) and m19 (0.89% apo). These burn to depletion and never reach the energy cutoff, so the cutoff never pins the SMA. |
| Every golden winner passes seeds 1-5 | **FAIL: m10, m17, m19** (real misses below). The other 17 pass. |
| Real-miss list with γ | below |

## Golden file changes (old → new)

All retunes use real performance. No band moved. An id changes only where it encodes the changed value, and the hash is re-verified on the renamed id.

| File | Mission | Design | Change | Why | Result (seeds 1-5, batch) |
|---|---|---|---|---|---|
| campaign-viable.json | m2 | m2b-2600 | propMass 2540 → 2585 kg (from a9ec49c) | #315 headroom ≥ 97 km | 101.017 km |
| campaign-viable.json | m2 | m2f-1600 | propMass 1560 → 1580 kg (from a9ec49c) | headroom ≥ 97 km (1560 kg flies 96.79) | 100.098 km |
| campaign-winners.json, campaign-viable.json, found.json, board-starters.json | m3 | h-n5-k8-24000-2500 → **h-n5-k8-23500-2500** | stage-1 propMass 24000 → 23500 | real FPA miss: γ −0.212°, 224.4/175.6 km | 205.20/194.80 km, γ 0.045°, 5/5 |
| campaign-viable.json | m3 | m3-eight | pitchKickDeg 8 → 7 | real FPA miss | 200.04/199.96 km, γ 0.000° |
| campaign-viable.json | m5 | n9-400000-k7-u40000 → **n9-400000-k8-u40000** | pitchKickDeg 7 → 8 | prop-depletion at 399.9 km | 400.30/399.70 km, γ 0.003° |
| campaign-winners.json, found.json, board-starters.json | m12 | kh12-win | pitchKickDeg 8 → 6 (id has no kick) | real FPA miss: γ 0.627° batch / 0.625° live, 676.3/523.7 km against 570-630 | 600.28/599.72 km, γ −0.002°, 5/5 |
| contracts-viable.json | flock-leo200-k0 | flock-leo200-k0-h-n5-k8-24000-2500 → **…-23500-2500** | stage-1 propMass 24000 → 23500 | same as m3 h-n5 (224.4/175.6) | passes 5/5 |
| contracts-viable.json | tern-leo600-k3 | tern-leo600-k3-n7-270000-k8-u40000 | pitchKickDeg 8 → 6 | real FPA miss: γ 1.246°, 751.8/448.2 | 600.4/599.6, γ −0.004° |
| contracts-viable.json | tern-leo600-k3 | …-n7-270000-k8-u40000-t3 | pitchKickDeg 8 → 6 | γ 1.564°, 790.5/409.5 | 600.5/599.5, γ −0.004° |
| contracts-viable.json | tern-sso-k3 | tern-sso-k3-n5-200000-k6 | pitchKickDeg 6 → 4 | γ 0.421°, 600.9/499.1 | 552.1/547.9, γ 0.017° |
| contracts-viable.json | tern-sso-k9 | …-n7-270000-k8-u40000-t3-t3 | pitchKickDeg 8 → 5 | γ 1.811° (seed 1 4/5) | 556.2/543.8, γ 0.052°, 5/5 |
| tests/vectors/prediction/launches.txt | — | presets | regenerated (`KESTREL_WRITE_PREDICT=1`) | energy cutoff | scout pass → fail (443.1/356.8); kid orbit 657.0/190.7 → suborbital 656.7/−256.8; rest ±0.1 km |

Kept: m1b at **705 kg**. It flies 10.000 km under (b); 645 kg peaks at 9.07 km and fails. KH-1 starter at **515 kg** (9.786 km).
Contract ids keep their old kick label, so the seeded schedules are unchanged. m12 kh12-win has no kick in its id.
Tried and reverted: m17 → the found.json 4-engine kh17-win. It passes the batch, but its liftoff TWR is 0.82, so the designer schema blocks it (NAV02).

## Hop peaks per seed (km; batch 0.2 s / live 1/120 s)

| Flight | s1 | s2 | s3 | s4 | s5 | live s1 | g batch/live |
|---|---|---|---|---|---|---|---|
| KH-1 starter (515 kg) | 9.786 | 9.786 | 9.786 | 9.786 | 9.786 | 9.787 | 6.00/6.00 |
| KH-2 starter | 97.549 | 97.549 | 97.549 | 97.549 | 97.549 | 97.669 | 6.90/6.92 |
| m1 m1a-520 | 10.044 | 10.044 | 10.044 | 10.044 | 10.044 | 10.044 | 3.27/3.27 |
| m1 m1b-705 | 10.000 | 10.000 | 10.000 | 10.000 | 10.000 | 10.004 | 4.92/4.92 |
| m1 m1p-360 | 9.926 | 9.926 | 9.926 | 9.926 | 9.926 | 9.926 | 2.18/2.18 |
| m2 m2a-2500 | 99.827 | 99.827 | 99.827 | 99.827 | 99.827 | 99.852 | 5.32/5.34 |
| m2 m2b-2600 | 101.017 | 101.017 | 101.017 | 101.017 | 101.017 | 101.027 | 5.41/5.41 |
| m2 m2f-1600 | 100.098 | 100.098 | 100.098 | 100.098 | 100.098 | 100.156 | 5.27/5.28 |

#316: the KH-2 starter pulls **6.90 g batch / 6.92 g live** (limit 6 g). Not changed here.

## Verdict flips vs origin/main: 90 flights (75 pass→fail, 15 fail→pass), out of 530

| Source | Flip | Flights | Designs |
|---|---|---|---|
| board-fly | pass→fail | 15 | m10 b9-p340-m83-u33-s14, m17 kh17-win, m19 kh19-win |
| campaign-viable | pass→fail | 20 | m7 n9-p260-m80-u28, m10 b8-p340-m82-u33-s14, m10 b10-p340-m83-u33-s14, m10 b9-p340-m83-u33-s14 |
| campaign-winners | pass→fail | 15 | m10 b9-p340-m83-u33-s14, m17 kh17-win, m19 kh19-win |
| found | pass→fail | 10 | m10 b9-p340-m83-u33-s14, m19 kh19-win |
| starter | fail→pass | 15 | m5 n8-360000-k8-u30000, m12 kh12-win, m14 kh14-win |
| starter | pass→fail | 15 | m10 b9-p340-m83-u33-s14, m17 kh17-win, m19 kh19-win |

Every pass→fail is the prop-depletion class below. The fail→pass flips are raw board starters m5, m12 and m14, which now fly the band without their board edit. On main they overshot under the perigee cutoff (1898, 649 and 1059 km apoapsis).

## Real misses (γ at burnout or cutoff; batch / live)

### A. Prop-depletion on ellipse and C3 targets: no clean retune; needs a Physics ruling
These designs won on main through the "delta-v" criterion: a 200 km perigee plus an on-paper Δv budget. Under ruling 2 they keep burning toward GTO or TLI energy and run dry a few hundred metres under the 200 km aim. PEG-lite approaches the aim from below. `FailureMonitor.OnEmpty` then calls prop-depletion, because the last stage empties below the aim altitude. None of them can actually reach the target energy, and that is real performance. Kick, kick altitude, turn shape, upper propellant and engine sweeps found no pass for m10 or m19, and the campaign search finds no m10 or m19 design. The delta-v winners that still pass (m7 n5, burnout 200.89 km; m15, 200.02 km) pass on the same knife-edge.

| Flight | Burnout alt km | Apo/peri km (batch) | Apo km live | SMA km | γ batch / live |
|---|---|---|---|---|---|
| **m10 winner** b9-p340-m83-u33-s14 (also starter, board, found) | 199.718 | 489.801/199.715 | 498.555 | 6722.895 | 0.008° / 0.007° |
| **m17 winner** kh17-win (also starter, board) | 199.772 | 57323.653/199.771 | 57515.737 | 35139.849 | 0.020° / 0.020° |
| **m19 winner** kh19-win (also starter, board, found) | 199.755 | 975.004/199.745 | 983.790 | 6965.511 | 0.023° / 0.023° |
| m7 n9-p260-m80-u28 (campaign-viable) | 199.769 | 7083.999/199.763 | 7087.531 | 10020.018 | -0.037° / -0.038° |
| m10 b8-p340-m82-u33-s14 (campaign-viable) | 199.723 | 207.477/199.361 | 217.430 | 6581.556 | -0.015° / -0.015° |
| m10 b9-p340-m83-u33-s14 (campaign-viable) | 199.718 | 519.335/199.715 | 527.938 | 6737.662 | 0.008° / 0.008° |
| m10 b10-p340-m83-u33-s14 (campaign-viable) | 199.737 | 715.023/199.727 | 721.315 | 6835.512 | 0.019° / 0.018° |

Contracts in the same class: comsat-gto-k3 (all 3 designs, 3606/5061/4498 × 199.7-199.8 km), comsat-gto-k6 (all 3, 2060/2447/3466 × 199.9 km), lark-tli-k6 n6 and n8 (28044/40449 × 199.8-199.9 km). γ at burnout is 0.017-0.027°. Those three cells drop below three viable designs.

Options for Physics, none applied: (i) for ellipse and C3 targets, cut off at the aim-perigee parking orbit and keep the delta-v criterion; (ii) don't call prop-depletion once the orbit's perigee is at the aim; (iii) accept these as misses and replace the m10, m17 and m19 goldens with new searched designs, which needs bigger upper stages.

### B. FPA misses that remain
| Flight | Seeds | Batch apo/peri km | γ at cutoff | Cause |
|---|---|---|---|---|
| contract finch-leo400-k3-c9-420-k7-u38 | 2 | 657.8/142.2 | 1.830° | seeded stage-1 engine-out: weak upper stage cuts at 260 km still climbing; kick 4-8 sweep: no 5/5 |
| contract pantry-leo400-k3-c8-360-k7-u35 | 3 | 637.7/162.3 | 1.842° | same; on the same seed it was prop-depletion before the engine-out fix |
| preset scout (free build, leo400) | predict | 443.1/356.8 | −0.108° (cutoff at 441 km) | legacy PEG-lite overshoots the 400 km altitude before target energy |
| preset kid (free build) | predict | 656.7/−256.8 | 0.069° (cutoff at 657 km) | legacy guidance lofts far above the 200 km target before target energy |

Raw (unedited) board starters that miss, as intended: m3 strength (γ −1.104°), m4 fix (−0.979°), m6 strength (0.104°), m8 strength (0.070°), m16 strength (0.016°). Their board edits pass.

## Orbital batch vs live, and step size (seed 1, every golden winner)

KH-6 (m6 t6-p300-m76-u26) at 0.2 s: 203.518/196.482 km, SMA 6578.137, against live 203.108/196.892/6578.137. That is 0.20% apo, 0.41 km peri and 0.0000% SMA, inside the named tolerances, so no 0.05 s step is needed.
| KH-6 (m6 t6-p300-m76-u26), 0.2 s | Batch apo | Live apo | Δapo | Batch peri | Live peri | Δperi | ΔSMA | In tolerance |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| main (perigee cutoff, snapped) | 207.154 | 202.697 | 2.199% | 200.303 | 192.770 | 7533 m | 0.0912% | no |
| a9ec49c (perigee cutoff, located) | 203.071 | 202.668 | 0.199% | 192.238 | 192.232 | 6 m | 0.0031% | yes |
| **this commit (energy cutoff)** | 203.518 | 203.108 | 0.202% | 196.482 | 196.892 | 410 m | 0.0000% | **yes** |

| Worst case m12 kh12-win, 0.2 s | Batch apo | Live apo | Δapo | Δperi | ΔSMA | γ batch/live |
|---|---:|---:|---:|---:|---:|---|
| a9ec49c (perigee cutoff, located; kick 8) | 602.240 | 905.397 | 33.48% | 689 m | 2.12% | — |
| energy cutoff, original kick 8 (real FPA miss) | 676.37 | 676.07 | 0.04% | — | 0.0000% | 0.627° / 0.625° |
| **this commit (kick 6)** | 600.279 | 600.281 | 0.000% | 2 m | 0.0000% | −0.002° / −0.002° |

Under the energy cutoff, the worst orbital winner that reaches orbit is m7 n5 (burnout-limited GTO): 0.40% apo and 0.09% SMA at 0.2 s, inside tolerance.

| Mission | Design | dt | outcome | apo km | peri km | SMA km | Δapo vs live | Δperi vs live | ΔSMA vs live | ms |
|---|---|---|---|---|---|---|---|---|---|---|
| m1 | m1a-520 | 0.2 | suborbital | 10.044 | -6367.523 | 3200.818 | 0.000% | — | — | 1 |
| m1 | m1a-520 | 0.1 | suborbital | 10.044 | -6367.499 | 3200.901 | 0.000% | — | — | 1 |
| m1 | m1a-520 | 0.05 | suborbital | 10.044 | -6367.486 | 3200.943 | 0.000% | — | — | 3 |
| m1 | m1a-520 | 0.00833 | suborbital | 10.044 | -6367.476 | 3200.978 | 0.000% | — | — | 21 |
| m2 | m2a-2500 | 0.2 | suborbital | 99.827 | -6364.713 | 3247.658 | 0.025% | — | — | 4 |
| m2 | m2a-2500 | 0.1 | suborbital | 99.841 | -6364.708 | 3247.704 | 0.011% | — | — | 8 |
| m2 | m2a-2500 | 0.05 | suborbital | 99.848 | -6364.708 | 3247.726 | 0.004% | — | — | 16 |
| m2 | m2a-2500 | 0.00833 | suborbital | 99.852 | -6364.707 | 3247.744 | 0.000% | — | — | 104 |
| m3 | h-n5-k8-23500-2500 | 0.2 | orbit | 205.199 | 194.801 | 6578.137 | 0.000% | 1 m | 0.0000% | 32 |
| m3 | h-n5-k8-23500-2500 | 0.1 | orbit | 205.198 | 194.802 | 6578.137 | 0.000% | 0 m | 0.0000% | 59 |
| m3 | h-n5-k8-23500-2500 | 0.05 | orbit | 205.198 | 194.802 | 6578.137 | 0.000% | 0 m | 0.0000% | 118 |
| m3 | h-n5-k8-23500-2500 | 0.00833 | orbit | 205.198 | 194.802 | 6578.137 | 0.000% | 0 m | 0.0000% | 731 |
| m4 | n5-200000-k6 | 0.2 | orbit | 504.304 | 495.696 | 6878.137 | 0.001% | 7 m | 0.0000% | 11 |
| m4 | n5-200000-k6 | 0.1 | orbit | 504.301 | 495.699 | 6878.137 | 0.001% | 4 m | 0.0000% | 23 |
| m4 | n5-200000-k6 | 0.05 | orbit | 504.299 | 495.701 | 6878.137 | 0.000% | 2 m | 0.0000% | 45 |
| m4 | n5-200000-k6 | 0.00833 | orbit | 504.297 | 495.703 | 6878.137 | 0.000% | 0 m | 0.0000% | 277 |
| m5 | n8-360000-k8-u30000 | 0.2 | orbit | 410.695 | 389.305 | 6778.137 | 0.022% | 89 m | 0.0000% | 15 |
| m5 | n8-360000-k8-u30000 | 0.1 | orbit | 410.667 | 389.333 | 6778.137 | 0.015% | 61 m | 0.0000% | 31 |
| m5 | n8-360000-k8-u30000 | 0.05 | orbit | 410.677 | 389.323 | 6778.137 | 0.017% | 71 m | 0.0000% | 61 |
| m5 | n8-360000-k8-u30000 | 0.00833 | orbit | 410.606 | 389.394 | 6778.137 | 0.000% | 0 m | 0.0000% | 373 |
| m6 | t6-p300-m76-u26 | 0.2 | orbit | 203.518 | 196.482 | 6578.137 | 0.202% | 410 m | 0.0000% | 41 |
| m6 | t6-p300-m76-u26 | 0.1 | orbit | 203.281 | 196.719 | 6578.137 | 0.085% | 173 m | 0.0000% | 79 |
| m6 | t6-p300-m76-u26 | 0.05 | orbit | 203.165 | 196.835 | 6578.137 | 0.028% | 57 m | 0.0000% | 161 |
| m6 | t6-p300-m76-u26 | 0.00833 | orbit | 203.108 | 196.892 | 6578.137 | 0.000% | 0 m | 0.0000% | 951 |
| m7 | n5-p260-m80-u28 | 0.2 | orbit | 3821.910 | 200.892 | 8389.538 | 0.395% | 5 m | 0.0904% | 42 |
| m7 | n5-p260-m80-u28 | 0.1 | orbit | 3831.826 | 200.892 | 8394.496 | 0.137% | 5 m | 0.0313% | 83 |
| m7 | n5-p260-m80-u28 | 0.05 | orbit | 3834.571 | 200.895 | 8395.870 | 0.065% | 2 m | 0.0150% | 167 |
| m7 | n5-p260-m80-u28 | 0.00833 | orbit | 3837.083 | 200.897 | 8397.127 | 0.000% | 0 m | 0.0000% | 1019 |
| m8 | c8-360-k7-u35 | 0.2 | orbit | 400.789 | 399.211 | 6778.137 | 0.002% | 8 m | 0.0000% | 16 |
| m8 | c8-360-k7-u35 | 0.1 | orbit | 400.785 | 399.215 | 6778.137 | 0.001% | 4 m | 0.0000% | 31 |
| m8 | c8-360-k7-u35 | 0.05 | orbit | 400.779 | 399.221 | 6778.137 | 0.000% | 2 m | 0.0000% | 63 |
| m8 | c8-360-k7-u35 | 0.00833 | orbit | 400.781 | 399.219 | 6778.137 | 0.000% | 0 m | 0.0000% | 379 |
| m9 | n4-p260-m100-u28 | 0.2 | orbit | 385644.400 | 199.768 | 199300.221 | 0.000% | 1 m | 0.0000% | 49 |
| m9 | n4-p260-m100-u28 | 0.1 | orbit | 385644.400 | 199.767 | 199300.221 | 0.000% | 0 m | 0.0000% | 103 |
| m9 | n4-p260-m100-u28 | 0.05 | orbit | 385644.401 | 199.767 | 199300.221 | 0.000% | 0 m | 0.0000% | 195 |
| m9 | n4-p260-m100-u28 | 0.00833 | orbit | 385644.401 | 199.767 | 199300.221 | 0.000% | 0 m | 0.0000% | 1164 |
| m10 | b9-p340-m83-u33-s14 | 0.2 | failure | 489.801 | 199.715 | 6722.895 | 1.756% | 0 m | 0.0651% | 108 |
| m10 | b9-p340-m83-u33-s14 | 0.1 | failure | 494.427 | 199.715 | 6725.208 | 0.828% | 0 m | 0.0307% | 212 |
| m10 | b9-p340-m83-u33-s14 | 0.05 | failure | 496.745 | 199.715 | 6726.367 | 0.363% | 0 m | 0.0135% | 424 |
| m10 | b9-p340-m83-u33-s14 | 0.00833 | failure | 498.555 | 199.715 | 6727.272 | 0.000% | 0 m | 0.0000% | 2566 |
| m11 | kh11-win | 0.2 | orbit | 550.412 | 549.588 | 6928.137 | 0.000% | 0 m | 0.0000% | 12 |
| m11 | kh11-win | 0.1 | orbit | 550.412 | 549.588 | 6928.137 | 0.000% | 0 m | 0.0000% | 25 |
| m11 | kh11-win | 0.05 | orbit | 550.411 | 549.589 | 6928.137 | 0.000% | 1 m | 0.0000% | 48 |
| m11 | kh11-win | 0.00833 | orbit | 550.412 | 549.588 | 6928.137 | 0.000% | 0 m | 0.0000% | 285 |
| m12 | kh12-win | 0.2 | orbit | 600.279 | 599.721 | 6978.137 | 0.000% | 2 m | 0.0000% | 12 |
| m12 | kh12-win | 0.1 | orbit | 600.281 | 599.719 | 6978.137 | 0.000% | 0 m | 0.0000% | 23 |
| m12 | kh12-win | 0.05 | orbit | 600.281 | 599.719 | 6978.137 | 0.000% | 0 m | 0.0000% | 47 |
| m12 | kh12-win | 0.00833 | orbit | 600.281 | 599.719 | 6978.137 | 0.000% | 0 m | 0.0000% | 274 |
| m13 | kh13-win | 0.2 | orbit | 550.063 | 549.937 | 6928.137 | 0.001% | 4 m | 0.0000% | 10 |
| m13 | kh13-win | 0.1 | orbit | 550.065 | 549.935 | 6928.137 | 0.000% | 2 m | 0.0000% | 21 |
| m13 | kh13-win | 0.05 | orbit | 550.067 | 549.933 | 6928.137 | 0.000% | 0 m | 0.0000% | 40 |
| m13 | kh13-win | 0.00833 | orbit | 550.067 | 549.933 | 6928.137 | 0.000% | 0 m | 0.0000% | 261 |
| m14 | kh14-win | 0.2 | orbit | 400.422 | 399.578 | 6778.137 | 0.000% | 2 m | 0.0000% | 16 |
| m14 | kh14-win | 0.1 | orbit | 400.423 | 399.577 | 6778.137 | 0.001% | 3 m | 0.0000% | 30 |
| m14 | kh14-win | 0.05 | orbit | 400.418 | 399.582 | 6778.137 | 0.000% | 2 m | 0.0000% | 65 |
| m14 | kh14-win | 0.00833 | orbit | 400.420 | 399.580 | 6778.137 | 0.000% | 0 m | 0.0000% | 365 |
| m15 | kh15-win | 0.2 | orbit | 4373.466 | 200.024 | 8664.882 | 0.291% | 4 m | 0.0736% | 45 |
| m15 | kh15-win | 0.1 | orbit | 4380.296 | 200.026 | 8668.298 | 0.135% | 2 m | 0.0343% | 87 |
| m15 | kh15-win | 0.05 | orbit | 4383.647 | 200.027 | 8669.974 | 0.059% | 1 m | 0.0149% | 175 |
| m15 | kh15-win | 0.00833 | orbit | 4386.233 | 200.028 | 8671.268 | 0.000% | 0 m | 0.0000% | 1075 |
| m16 | kh16-win | 0.2 | orbit | 503.668 | 496.332 | 6878.137 | 0.001% | 3 m | 0.0000% | 13 |
| m16 | kh16-win | 0.1 | orbit | 503.668 | 496.332 | 6878.137 | 0.001% | 3 m | 0.0000% | 28 |
| m16 | kh16-win | 0.05 | orbit | 503.672 | 496.328 | 6878.137 | 0.000% | 1 m | 0.0000% | 53 |
| m16 | kh16-win | 0.00833 | orbit | 503.671 | 496.329 | 6878.137 | 0.000% | 0 m | 0.0000% | 328 |
| m17 | kh17-win | 0.2 | failure | 57323.653 | 199.771 | 35139.849 | 0.334% | 1 m | 0.2726% | 64 |
| m17 | kh17-win | 0.1 | failure | 57441.079 | 199.771 | 35198.562 | 0.130% | 1 m | 0.1059% | 128 |
| m17 | kh17-win | 0.05 | failure | 57461.741 | 199.770 | 35208.892 | 0.094% | 0 m | 0.0766% | 247 |
| m17 | kh17-win | 0.00833 | failure | 57515.737 | 199.770 | 35235.890 | 0.000% | 0 m | 0.0000% | 1488 |
| m18 | kh18-win | 0.2 | orbit | 1980046.182 | 199.753 | 996501.105 | 0.000% | 1 m | 0.0000% | 50 |
| m18 | kh18-win | 0.1 | orbit | 1980046.183 | 199.752 | 996501.105 | 0.000% | 0 m | 0.0000% | 96 |
| m18 | kh18-win | 0.05 | orbit | 1980046.183 | 199.752 | 996501.105 | 0.000% | 0 m | 0.0000% | 192 |
| m18 | kh18-win | 0.00833 | orbit | 1980046.183 | 199.752 | 996501.105 | 0.000% | 0 m | 0.0000% | 1176 |
| m19 | kh19-win | 0.2 | failure | 975.004 | 199.745 | 6965.511 | 0.893% | 1 m | 0.0630% | 89 |
| m19 | kh19-win | 0.1 | failure | 979.287 | 199.744 | 6967.653 | 0.458% | 0 m | 0.0323% | 177 |
| m19 | kh19-win | 0.05 | failure | 981.855 | 199.744 | 6968.936 | 0.197% | 0 m | 0.0139% | 341 |
| m19 | kh19-win | 0.00833 | failure | 983.790 | 199.744 | 6969.904 | 0.000% | 0 m | 0.0000% | 2056 |
| m20 | kh20-win | 0.2 | orbit | 3175847.421 | 199.839 | 1594401.767 | 0.000% | 3 m | 0.0000% | 48 |
| m20 | kh20-win | 0.1 | orbit | 3175847.423 | 199.838 | 1594401.767 | 0.000% | 2 m | 0.0000% | 96 |
| m20 | kh20-win | 0.05 | orbit | 3175847.424 | 199.837 | 1594401.767 | 0.000% | 1 m | 0.0000% | 193 |
| m20 | kh20-win | 0.00833 | orbit | 3175847.424 | 199.836 | 1594401.767 | 0.000% | 0 m | 0.0000% | 1162 |

Total runtime, 20 winners, seed 1: 0.2 s = 678 ms, 0.1 s = 1341 ms, 0.05 s = 2654 ms, 0.00833 s = 16055 ms

## Test expectations changed (none loosened)

- `AscentDecisionTests.AutoAssist_ThrottlesInTheGreen…`: a design with a 60% max-Q throttle now flies that cap through its own guidance, so the director command is ThrottleScale 1 and EngageMaxQCap 0 (was ThrottleScale 0.60). Ruling 4.
- `AscentDecisionTests` (player tap): the tap engages the design law at the offered 70%. EngageMaxQCap is 0.70 and ThrottleScale 1 (was ThrottleScale 0.70). `SuppressDesignThrottle` is gone.
- `Kh1FirstHopTests`: the `SuppressDesignThrottle` assert becomes EngageMaxQCap 0 and ThrottleScale 1 (the design cap is the only law), and the hand-flown loop drops its suppression branch.
- `SharedDirectorTests`: the KH-1 starter is 515 kg (was 464) and m1b ≥ 9.7 km is added. Designer vs bare is now **equal** (was "not equal"), per ruling 4. The orbit batch-vs-live check covers every winner and every board starter (36 flights, was 18 winners). The perigee-cutoff location test is replaced by an energy-cutoff test (ε within 1e-9, SMA within 1 m at 0.2 s and 1/120 s; ellipse and C3 energies; raw SimOptions keep the parity path). New tests: TheDesignMaxQThrottle_IsTheOnlyMaxQLaw_ForEveryHop and TheFuelImbalancePenalty_ThrottlesOnlyItsStage_UntilItSeparates.
- `ReadoutV2Tests.ReferenceMaxQ_MatchesFullHeadlessPeak`: compares against the designer's directed flight, not a parity `Stack.SimInputs` run. `StabilityBand…`: the bare stock vehicle still checks at the documented 73.01 kPa. The readout now checks at 70.71 kPa, because the predict schedule's stage-1 fuel imbalance runs that stage at 97% until separation.
- `CampaignFlightLoopTests.BoardStarters_LoadFromThePack…`: the m3 board starter name is now h-n5-k8-23500-2500.
- `tests/vectors/prediction/launches.txt`: regenerated (see table above).

## Core tests still failing (21 of 765), by cause

- **Prop-depletion class (A), 12 tests:** CampaignWinners_FlyPassOnEverySeed, CampaignViableDesigns_WinM1ThroughM10, EveryMission_HasThreeViableDesigns, EveryCell_AndEveryOffer_IsViable (also class B), DesignFirstRocketsAreReadyAfterThePredictRun, GoldenWinnersHaveNoCertainFailureTheyDoNotHave, EveryOrbitalWinner_LiveStep_ReachesOrbit, NAV01_DesignFirstRocket…, NAV02_GoldenWinners…, BatchAndLivePad_Agree_Orbits… (m10, m17 and m19 only), DefaultPlayer_EveryGoldenWinnerAndStarter… (m10, m17 and m19 only), CampaignWinners_HideEachReleasedBody… (15 of 18).
- **Board premise, 1 test:** BoardStarter_FixItOrOneStrengthEdit_WinsEveryMission. The raw m5 (fix), m12 (strength) and m14 (strength) starters now win with no edit, so they are not near misses. **Arcade decision:** set those rows to mode "fly", or re-pick near-miss starters. Not changed here.
- **Preset scout misses under the energy cutoff (B), 4 tests:** UnchangedDesign_HasAnEmptyDiff, PropellantCut_FlagsAFailedPrediction…, BrokenPrecondition_FlagsCausality…, UpperEngineSwap_DropsRestart…. The fix is a legacy-guidance or preset retune, which Physics needs to decide.
- **Fix it knife-edge, 4 tests:** FixIt_WeakAirframe…(14) and (16) (two steps under the chosen strength now also fly), FixIt_ThatCannotFixTheFlight_SaysSo…, ReportFixIt_RanOutOfFuel…. The max-Q load and cutoff changed under rulings 2 and 4. Not investigated further.

## Appendix: every winner and starter, per seed (new; old verdict)

| Source | Mission | Design | Seed | old | new | batch apo/peri km | live apo/peri km | γ batch/live | criterion / failure |
|---|---|---|---|---|---|---|---|---|---|
| campaign-winners | m1 | m1a-520 | 1 | P | P | 10.044/-6367.523 | 10.044/-6367.476 | 37.695/37.695 | altitude |
| campaign-winners | m1 | m1a-520 | 2 | P | P | 10.044/-6367.523 | 10.044/-6367.476 | 37.695/37.695 | altitude |
| campaign-winners | m1 | m1a-520 | 3 | P | P | 10.044/-6367.523 | 10.044/-6367.476 | 37.695/37.695 | altitude |
| campaign-winners | m1 | m1a-520 | 4 | P | P | 10.044/-6367.523 | 10.044/-6367.476 | 37.695/37.695 | altitude |
| campaign-winners | m1 | m1a-520 | 5 | P | P | 10.044/-6367.523 | 10.044/-6367.476 | 37.695/37.695 | altitude |
| starter | m1 | armed hopper | 1 | P | P | 9.786/-6368.366 | 9.787/-6368.325 | 40.730/40.749 | altitude |
| starter | m1 | armed hopper | 2 | P | P | 9.786/-6368.366 | 9.787/-6368.325 | 40.730/40.749 | altitude |
| starter | m1 | armed hopper | 3 | P | P | 9.786/-6368.366 | 9.787/-6368.325 | 40.730/40.749 | altitude |
| starter | m1 | armed hopper | 4 | P | P | 9.786/-6368.366 | 9.787/-6368.325 | 40.730/40.749 | altitude |
| starter | m1 | armed hopper | 5 | P | P | 9.786/-6368.366 | 9.787/-6368.325 | 40.730/40.749 | altitude |
| campaign-winners | m2 | m2a-2500 | 1 | P | P | 99.827/-6364.713 | 99.852/-6364.707 | 66.994/66.993 | altitude |
| campaign-winners | m2 | m2a-2500 | 2 | P | P | 99.827/-6364.713 | 99.852/-6364.707 | 66.994/66.993 | altitude |
| campaign-winners | m2 | m2a-2500 | 3 | P | P | 99.827/-6364.713 | 99.852/-6364.707 | 66.994/66.993 | altitude |
| campaign-winners | m2 | m2a-2500 | 4 | P | P | 99.827/-6364.713 | 99.852/-6364.707 | 66.994/66.993 | altitude |
| campaign-winners | m2 | m2a-2500 | 5 | P | P | 99.827/-6364.713 | 99.852/-6364.707 | 66.994/66.993 | altitude |
| starter | m2 | armed hopper | 1 | P | P | 97.549/-6364.892 | 97.669/-6364.894 | 67.371/67.392 | altitude |
| starter | m2 | armed hopper | 2 | P | P | 97.549/-6364.892 | 97.669/-6364.894 | 67.371/67.392 | altitude |
| starter | m2 | armed hopper | 3 | P | P | 97.549/-6364.892 | 97.669/-6364.894 | 67.371/67.392 | altitude |
| starter | m2 | armed hopper | 4 | P | P | 97.549/-6364.892 | 97.669/-6364.894 | 67.371/67.392 | altitude |
| starter | m2 | armed hopper | 5 | P | P | 97.549/-6364.892 | 97.669/-6364.894 | 67.371/67.392 | altitude |
| board-strength | m3 | h-n5-k8-23500-2500 | 1 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| board-strength | m3 | h-n5-k8-23500-2500 | 2 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| board-strength | m3 | h-n5-k8-23500-2500 | 3 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| board-strength | m3 | h-n5-k8-23500-2500 | 4 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| board-strength | m3 | h-n5-k8-23500-2500 | 5 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| campaign-winners | m3 | h-n5-k8-23500-2500 | 1 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| campaign-winners | m3 | h-n5-k8-23500-2500 | 2 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| campaign-winners | m3 | h-n5-k8-23500-2500 | 3 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| campaign-winners | m3 | h-n5-k8-23500-2500 | 4 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| campaign-winners | m3 | h-n5-k8-23500-2500 | 5 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| starter | m3 | h-n5-k8-23500-2500 | 1 | F | F | 327.694/72.306 | 327.703/72.297 | -1.104/-1.104 | altitude |
| starter | m3 | h-n5-k8-23500-2500 | 2 | F | F | 327.694/72.306 | 327.703/72.297 | -1.104/-1.104 | altitude |
| starter | m3 | h-n5-k8-23500-2500 | 3 | F | F | 327.694/72.306 | 327.703/72.297 | -1.104/-1.104 | altitude |
| starter | m3 | h-n5-k8-23500-2500 | 4 | F | F | 327.694/72.306 | 327.703/72.297 | -1.104/-1.104 | altitude |
| starter | m3 | h-n5-k8-23500-2500 | 5 | F | F | 327.694/72.306 | 327.703/72.297 | -1.104/-1.104 | altitude |
| board-fix | m4 | n5-200000-k6 | 1 | P | P | 503.386/496.614 | 503.478/496.522 | 0.028/0.029 | altitude |
| board-fix | m4 | n5-200000-k6 | 2 | P | P | 503.386/496.614 | 503.478/496.522 | 0.028/0.029 | altitude |
| board-fix | m4 | n5-200000-k6 | 3 | P | P | 503.386/496.614 | 503.478/496.522 | 0.028/0.029 | altitude |
| board-fix | m4 | n5-200000-k6 | 4 | P | P | 503.386/496.614 | 503.478/496.522 | 0.028/0.029 | altitude |
| board-fix | m4 | n5-200000-k6 | 5 | P | P | 503.386/496.614 | 503.478/496.522 | 0.028/0.029 | altitude |
| campaign-winners | m4 | n5-200000-k6 | 1 | P | P | 504.304/495.696 | 504.297/495.703 | 0.036/0.036 | altitude |
| campaign-winners | m4 | n5-200000-k6 | 2 | P | P | 504.304/495.696 | 504.297/495.703 | 0.036/0.036 | altitude |
| campaign-winners | m4 | n5-200000-k6 | 3 | P | P | 504.304/495.696 | 504.297/495.703 | 0.036/0.036 | altitude |
| campaign-winners | m4 | n5-200000-k6 | 4 | P | P | 504.304/495.696 | 504.297/495.703 | 0.036/0.036 | altitude |
| campaign-winners | m4 | n5-200000-k6 | 5 | P | P | 504.304/495.696 | 504.297/495.703 | 0.036/0.036 | altitude |
| starter | m4 | n5-200000-k6 | 1 | F | F | 617.511/382.489 | 618.386/381.614 | -0.979/-0.986 | altitude |
| starter | m4 | n5-200000-k6 | 2 | F | F | 617.511/382.489 | 618.386/381.614 | -0.979/-0.986 | altitude |
| starter | m4 | n5-200000-k6 | 3 | F | F | 617.511/382.489 | 618.386/381.614 | -0.979/-0.986 | altitude |
| starter | m4 | n5-200000-k6 | 4 | F | F | 617.511/382.489 | 618.386/381.614 | -0.979/-0.986 | altitude |
| starter | m4 | n5-200000-k6 | 5 | F | F | 617.511/382.489 | 618.386/381.614 | -0.979/-0.986 | altitude |
| board-fix | m5 | n8-360000-k8-u30000 | 1 | P | P | 402.869/397.131 | 402.861/397.139 | 0.024/0.024 | altitude |
| board-fix | m5 | n8-360000-k8-u30000 | 2 | P | P | 402.869/397.131 | 402.861/397.139 | 0.024/0.024 | altitude |
| board-fix | m5 | n8-360000-k8-u30000 | 3 | P | P | 402.869/397.131 | 402.861/397.139 | 0.024/0.024 | altitude |
| board-fix | m5 | n8-360000-k8-u30000 | 4 | P | P | 402.869/397.131 | 402.861/397.139 | 0.024/0.024 | altitude |
| board-fix | m5 | n8-360000-k8-u30000 | 5 | P | P | 402.869/397.131 | 402.861/397.139 | 0.024/0.024 | altitude |
| campaign-winners | m5 | n8-360000-k8-u30000 | 1 | P | P | 410.695/389.305 | 410.606/389.394 | -0.090/-0.090 | altitude |
| campaign-winners | m5 | n8-360000-k8-u30000 | 2 | P | P | 410.695/389.305 | 410.606/389.394 | -0.090/-0.090 | altitude |
| campaign-winners | m5 | n8-360000-k8-u30000 | 3 | P | P | 410.695/389.305 | 410.606/389.394 | -0.090/-0.090 | altitude |
| campaign-winners | m5 | n8-360000-k8-u30000 | 4 | P | P | 410.695/389.305 | 410.606/389.394 | -0.090/-0.090 | altitude |
| campaign-winners | m5 | n8-360000-k8-u30000 | 5 | P | P | 410.695/389.305 | 410.606/389.394 | -0.090/-0.090 | altitude |
| starter | m5 | n8-360000-k8-u30000 | 1 | F | P | 401.511/398.489 | 401.565/398.435 | 0.013/0.013 | altitude |
| starter | m5 | n8-360000-k8-u30000 | 2 | F | P | 401.511/398.489 | 401.565/398.435 | 0.013/0.013 | altitude |
| starter | m5 | n8-360000-k8-u30000 | 3 | F | P | 401.511/398.489 | 401.565/398.435 | 0.013/0.013 | altitude |
| starter | m5 | n8-360000-k8-u30000 | 4 | F | P | 401.511/398.489 | 401.565/398.435 | 0.013/0.013 | altitude |
| starter | m5 | n8-360000-k8-u30000 | 5 | F | P | 401.511/398.489 | 401.565/398.435 | 0.013/0.013 | altitude |
| board-strength | m6 | t6-p300-m76-u26 | 1 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| board-strength | m6 | t6-p300-m76-u26 | 2 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| board-strength | m6 | t6-p300-m76-u26 | 3 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| board-strength | m6 | t6-p300-m76-u26 | 4 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| board-strength | m6 | t6-p300-m76-u26 | 5 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| campaign-winners | m6 | t6-p300-m76-u26 | 1 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| campaign-winners | m6 | t6-p300-m76-u26 | 2 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| campaign-winners | m6 | t6-p300-m76-u26 | 3 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| campaign-winners | m6 | t6-p300-m76-u26 | 4 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| campaign-winners | m6 | t6-p300-m76-u26 | 5 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| starter | m6 | t6-p300-m76-u26 | 1 | F | F | 212.180/187.820 | 212.656/187.344 | 0.104/0.109 | altitude |
| starter | m6 | t6-p300-m76-u26 | 2 | F | F | 212.180/187.820 | 212.656/187.344 | 0.104/0.109 | altitude |
| starter | m6 | t6-p300-m76-u26 | 3 | F | F | 212.180/187.820 | 212.656/187.344 | 0.104/0.109 | altitude |
| starter | m6 | t6-p300-m76-u26 | 4 | F | F | 212.180/187.820 | 212.656/187.344 | 0.104/0.109 | altitude |
| starter | m6 | t6-p300-m76-u26 | 5 | F | F | 212.180/187.820 | 212.656/187.344 | 0.104/0.109 | altitude |
| board-fly | m7 | n5-p260-m80-u28 | 1 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| board-fly | m7 | n5-p260-m80-u28 | 2 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| board-fly | m7 | n5-p260-m80-u28 | 3 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| board-fly | m7 | n5-p260-m80-u28 | 4 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| board-fly | m7 | n5-p260-m80-u28 | 5 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| campaign-winners | m7 | n5-p260-m80-u28 | 1 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| campaign-winners | m7 | n5-p260-m80-u28 | 2 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| campaign-winners | m7 | n5-p260-m80-u28 | 3 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| campaign-winners | m7 | n5-p260-m80-u28 | 4 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| campaign-winners | m7 | n5-p260-m80-u28 | 5 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| starter | m7 | n5-p260-m80-u28 | 1 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| starter | m7 | n5-p260-m80-u28 | 2 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| starter | m7 | n5-p260-m80-u28 | 3 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| starter | m7 | n5-p260-m80-u28 | 4 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| starter | m7 | n5-p260-m80-u28 | 5 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| board-strength | m8 | c8-360-k7-u35 | 1 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| board-strength | m8 | c8-360-k7-u35 | 2 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| board-strength | m8 | c8-360-k7-u35 | 3 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| board-strength | m8 | c8-360-k7-u35 | 4 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| board-strength | m8 | c8-360-k7-u35 | 5 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| campaign-winners | m8 | c8-360-k7-u35 | 1 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| campaign-winners | m8 | c8-360-k7-u35 | 2 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| campaign-winners | m8 | c8-360-k7-u35 | 3 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| campaign-winners | m8 | c8-360-k7-u35 | 4 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| campaign-winners | m8 | c8-360-k7-u35 | 5 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| starter | m8 | c8-360-k7-u35 | 1 | F | F | 408.332/391.668 | 408.319/391.681 | 0.070/0.070 | altitude |
| starter | m8 | c8-360-k7-u35 | 2 | F | F | 408.332/391.668 | 408.319/391.681 | 0.070/0.070 | altitude |
| starter | m8 | c8-360-k7-u35 | 3 | F | F | 408.332/391.668 | 408.319/391.681 | 0.070/0.070 | altitude |
| starter | m8 | c8-360-k7-u35 | 4 | F | F | 408.332/391.668 | 408.319/391.681 | 0.070/0.070 | altitude |
| starter | m8 | c8-360-k7-u35 | 5 | F | F | 408.332/391.668 | 408.319/391.681 | 0.070/0.070 | altitude |
| board-fly | m9 | n4-p260-m100-u28 | 1 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| board-fly | m9 | n4-p260-m100-u28 | 2 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| board-fly | m9 | n4-p260-m100-u28 | 3 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| board-fly | m9 | n4-p260-m100-u28 | 4 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| board-fly | m9 | n4-p260-m100-u28 | 5 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| campaign-winners | m9 | n4-p260-m100-u28 | 1 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| campaign-winners | m9 | n4-p260-m100-u28 | 2 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| campaign-winners | m9 | n4-p260-m100-u28 | 3 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| campaign-winners | m9 | n4-p260-m100-u28 | 4 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| campaign-winners | m9 | n4-p260-m100-u28 | 5 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| starter | m9 | n4-p260-m100-u28 | 1 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| starter | m9 | n4-p260-m100-u28 | 2 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| starter | m9 | n4-p260-m100-u28 | 3 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| starter | m9 | n4-p260-m100-u28 | 4 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| starter | m9 | n4-p260-m100-u28 | 5 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| board-fly | m10 | b9-p340-m83-u33-s14 | 1 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| board-fly | m10 | b9-p340-m83-u33-s14 | 2 | P | F | 502.464/199.715 | 511.520/199.715 | 0.005/0.005 | prop-depletion |
| board-fly | m10 | b9-p340-m83-u33-s14 | 3 | P | F | 502.464/199.715 | 511.520/199.715 | 0.005/0.005 | prop-depletion |
| board-fly | m10 | b9-p340-m83-u33-s14 | 4 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| board-fly | m10 | b9-p340-m83-u33-s14 | 5 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| campaign-winners | m10 | b9-p340-m83-u33-s14 | 1 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| campaign-winners | m10 | b9-p340-m83-u33-s14 | 2 | P | F | 502.464/199.715 | 511.520/199.715 | 0.005/0.005 | prop-depletion |
| campaign-winners | m10 | b9-p340-m83-u33-s14 | 3 | P | F | 502.464/199.715 | 511.520/199.715 | 0.005/0.005 | prop-depletion |
| campaign-winners | m10 | b9-p340-m83-u33-s14 | 4 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| campaign-winners | m10 | b9-p340-m83-u33-s14 | 5 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| starter | m10 | b9-p340-m83-u33-s14 | 1 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| starter | m10 | b9-p340-m83-u33-s14 | 2 | P | F | 502.464/199.715 | 511.520/199.715 | 0.005/0.005 | prop-depletion |
| starter | m10 | b9-p340-m83-u33-s14 | 3 | P | F | 502.464/199.715 | 511.520/199.715 | 0.005/0.005 | prop-depletion |
| starter | m10 | b9-p340-m83-u33-s14 | 4 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| starter | m10 | b9-p340-m83-u33-s14 | 5 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| board-fly | m11 | kh11-win | 1 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| board-fly | m11 | kh11-win | 2 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| board-fly | m11 | kh11-win | 3 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| board-fly | m11 | kh11-win | 4 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| board-fly | m11 | kh11-win | 5 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| campaign-winners | m11 | kh11-win | 1 | P | P | 550.412/549.588 | 550.412/549.588 | -0.003/-0.003 | altitude |
| campaign-winners | m11 | kh11-win | 2 | P | P | 550.412/549.588 | 550.412/549.588 | -0.003/-0.003 | altitude |
| campaign-winners | m11 | kh11-win | 3 | P | P | 550.412/549.588 | 550.412/549.588 | -0.003/-0.003 | altitude |
| campaign-winners | m11 | kh11-win | 4 | P | P | 550.412/549.588 | 550.412/549.588 | -0.003/-0.003 | altitude |
| campaign-winners | m11 | kh11-win | 5 | P | P | 550.412/549.588 | 550.412/549.588 | -0.003/-0.003 | altitude |
| starter | m11 | kh11-win | 1 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| starter | m11 | kh11-win | 2 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| starter | m11 | kh11-win | 3 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| starter | m11 | kh11-win | 4 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| starter | m11 | kh11-win | 5 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| board-strength | m12 | kh12-win | 1 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| board-strength | m12 | kh12-win | 2 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| board-strength | m12 | kh12-win | 3 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| board-strength | m12 | kh12-win | 4 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| board-strength | m12 | kh12-win | 5 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| campaign-winners | m12 | kh12-win | 1 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| campaign-winners | m12 | kh12-win | 2 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| campaign-winners | m12 | kh12-win | 3 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| campaign-winners | m12 | kh12-win | 4 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| campaign-winners | m12 | kh12-win | 5 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| starter | m12 | kh12-win | 1 | F | P | 600.933/599.067 | 600.919/599.081 | -0.008/-0.008 | altitude |
| starter | m12 | kh12-win | 2 | F | P | 600.933/599.067 | 600.919/599.081 | -0.008/-0.008 | altitude |
| starter | m12 | kh12-win | 3 | F | P | 600.933/599.067 | 600.919/599.081 | -0.008/-0.008 | altitude |
| starter | m12 | kh12-win | 4 | F | P | 600.933/599.067 | 600.919/599.081 | -0.008/-0.008 | altitude |
| starter | m12 | kh12-win | 5 | F | P | 600.933/599.067 | 600.919/599.081 | -0.008/-0.008 | altitude |
| board-fly | m13 | kh13-win | 1 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| board-fly | m13 | kh13-win | 2 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| board-fly | m13 | kh13-win | 3 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| board-fly | m13 | kh13-win | 4 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| board-fly | m13 | kh13-win | 5 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| campaign-winners | m13 | kh13-win | 1 | P | P | 550.063/549.937 | 550.067/549.933 | -0.000/-0.000 | altitude |
| campaign-winners | m13 | kh13-win | 2 | P | P | 550.063/549.937 | 550.067/549.933 | -0.000/-0.000 | altitude |
| campaign-winners | m13 | kh13-win | 3 | P | P | 550.063/549.937 | 550.067/549.933 | -0.000/-0.000 | altitude |
| campaign-winners | m13 | kh13-win | 4 | P | P | 550.063/549.937 | 550.067/549.933 | -0.000/-0.000 | altitude |
| campaign-winners | m13 | kh13-win | 5 | P | P | 550.063/549.937 | 550.067/549.933 | -0.000/-0.000 | altitude |
| starter | m13 | kh13-win | 1 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| starter | m13 | kh13-win | 2 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| starter | m13 | kh13-win | 3 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| starter | m13 | kh13-win | 4 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| starter | m13 | kh13-win | 5 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| board-strength | m14 | kh14-win | 1 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| board-strength | m14 | kh14-win | 2 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| board-strength | m14 | kh14-win | 3 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| board-strength | m14 | kh14-win | 4 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| board-strength | m14 | kh14-win | 5 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| campaign-winners | m14 | kh14-win | 1 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| campaign-winners | m14 | kh14-win | 2 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| campaign-winners | m14 | kh14-win | 3 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| campaign-winners | m14 | kh14-win | 4 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| campaign-winners | m14 | kh14-win | 5 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| starter | m14 | kh14-win | 1 | F | P | 402.748/397.252 | 402.739/397.261 | 0.023/0.023 | altitude |
| starter | m14 | kh14-win | 2 | F | P | 402.748/397.252 | 402.739/397.261 | 0.023/0.023 | altitude |
| starter | m14 | kh14-win | 3 | F | P | 402.748/397.252 | 402.739/397.261 | 0.023/0.023 | altitude |
| starter | m14 | kh14-win | 4 | F | P | 402.748/397.252 | 402.739/397.261 | 0.023/0.023 | altitude |
| starter | m14 | kh14-win | 5 | F | P | 402.748/397.252 | 402.739/397.261 | 0.023/0.023 | altitude |
| board-fly | m15 | kh15-win | 1 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| board-fly | m15 | kh15-win | 2 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| board-fly | m15 | kh15-win | 3 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| board-fly | m15 | kh15-win | 4 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| board-fly | m15 | kh15-win | 5 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| campaign-winners | m15 | kh15-win | 1 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| campaign-winners | m15 | kh15-win | 2 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| campaign-winners | m15 | kh15-win | 3 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| campaign-winners | m15 | kh15-win | 4 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| campaign-winners | m15 | kh15-win | 5 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| starter | m15 | kh15-win | 1 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| starter | m15 | kh15-win | 2 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| starter | m15 | kh15-win | 3 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| starter | m15 | kh15-win | 4 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| starter | m15 | kh15-win | 5 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| board-strength | m16 | kh16-win | 1 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| board-strength | m16 | kh16-win | 2 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| board-strength | m16 | kh16-win | 3 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| board-strength | m16 | kh16-win | 4 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| board-strength | m16 | kh16-win | 5 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| campaign-winners | m16 | kh16-win | 1 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| campaign-winners | m16 | kh16-win | 2 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| campaign-winners | m16 | kh16-win | 3 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| campaign-winners | m16 | kh16-win | 4 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| campaign-winners | m16 | kh16-win | 5 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| starter | m16 | kh16-win | 1 | F | F | 501.903/498.097 | 501.879/498.121 | 0.016/0.016 | altitude |
| starter | m16 | kh16-win | 2 | F | F | 501.903/498.097 | 501.879/498.121 | 0.016/0.016 | altitude |
| starter | m16 | kh16-win | 3 | F | F | 501.903/498.097 | 501.879/498.121 | 0.016/0.016 | altitude |
| starter | m16 | kh16-win | 4 | F | F | 501.903/498.097 | 501.879/498.121 | 0.016/0.016 | altitude |
| starter | m16 | kh16-win | 5 | F | F | 501.903/498.097 | 501.879/498.121 | 0.016/0.016 | altitude |
| board-fly | m17 | kh17-win | 1 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| board-fly | m17 | kh17-win | 2 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| board-fly | m17 | kh17-win | 3 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| board-fly | m17 | kh17-win | 4 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| board-fly | m17 | kh17-win | 5 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| campaign-winners | m17 | kh17-win | 1 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| campaign-winners | m17 | kh17-win | 2 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| campaign-winners | m17 | kh17-win | 3 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| campaign-winners | m17 | kh17-win | 4 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| campaign-winners | m17 | kh17-win | 5 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| starter | m17 | kh17-win | 1 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| starter | m17 | kh17-win | 2 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| starter | m17 | kh17-win | 3 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| starter | m17 | kh17-win | 4 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| starter | m17 | kh17-win | 5 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| board-fly | m18 | kh18-win | 1 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| board-fly | m18 | kh18-win | 2 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| board-fly | m18 | kh18-win | 3 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| board-fly | m18 | kh18-win | 4 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| board-fly | m18 | kh18-win | 5 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| campaign-winners | m18 | kh18-win | 1 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| campaign-winners | m18 | kh18-win | 2 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| campaign-winners | m18 | kh18-win | 3 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| campaign-winners | m18 | kh18-win | 4 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| campaign-winners | m18 | kh18-win | 5 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| starter | m18 | kh18-win | 1 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| starter | m18 | kh18-win | 2 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| starter | m18 | kh18-win | 3 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| starter | m18 | kh18-win | 4 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| starter | m18 | kh18-win | 5 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| board-fly | m19 | kh19-win | 1 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| board-fly | m19 | kh19-win | 2 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| board-fly | m19 | kh19-win | 3 | P | F | 960.960/199.750 | 969.254/199.749 | 0.024/0.024 | prop-depletion |
| board-fly | m19 | kh19-win | 4 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| board-fly | m19 | kh19-win | 5 | P | F | 960.960/199.750 | 969.254/199.749 | 0.024/0.024 | prop-depletion |
| campaign-winners | m19 | kh19-win | 1 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| campaign-winners | m19 | kh19-win | 2 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| campaign-winners | m19 | kh19-win | 3 | P | F | 960.960/199.750 | 969.254/199.749 | 0.024/0.024 | prop-depletion |
| campaign-winners | m19 | kh19-win | 4 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| campaign-winners | m19 | kh19-win | 5 | P | F | 960.960/199.750 | 969.254/199.749 | 0.024/0.024 | prop-depletion |
| starter | m19 | kh19-win | 1 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| starter | m19 | kh19-win | 2 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| starter | m19 | kh19-win | 3 | P | F | 960.960/199.750 | 969.254/199.749 | 0.024/0.024 | prop-depletion |
| starter | m19 | kh19-win | 4 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| starter | m19 | kh19-win | 5 | P | F | 960.960/199.750 | 969.254/199.749 | 0.024/0.024 | prop-depletion |
| board-fly | m20 | kh20-win | 1 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| board-fly | m20 | kh20-win | 2 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| board-fly | m20 | kh20-win | 3 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| board-fly | m20 | kh20-win | 4 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| board-fly | m20 | kh20-win | 5 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| campaign-winners | m20 | kh20-win | 1 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| campaign-winners | m20 | kh20-win | 2 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| campaign-winners | m20 | kh20-win | 3 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| campaign-winners | m20 | kh20-win | 4 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| campaign-winners | m20 | kh20-win | 5 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| starter | m20 | kh20-win | 1 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| starter | m20 | kh20-win | 2 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| starter | m20 | kh20-win | 3 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| starter | m20 | kh20-win | 4 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| starter | m20 | kh20-win | 5 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
