# Rocket designer × missions: discrepancy list

Audit of `main` at b8ad6b9 (2026-10-19). Scope: every designer option (data catalogs, Hangar and v3 panels, guided stepper, edit ops), every campaign mission KH-1 to KH-20, the contract generator, and the path from the designer's verdict to the flight result.

**Headline.** The campaign cannot be finished as built:
- KH-3 can never launch (B-02).
- Live flights to orbit stop at 600 s, before any of the 18 orbital golden winners reaches orbit (B-01).

The designer's verdict also does not predict the mission result:
- All 9 templates and presets fail all 20 missions. The designer allows launch for most of them, and the Simple layer says "ready to fly".
- The designer blocks 10 golden winners that pass 5/5.

The test suite is green (607 passed, 8 skipped, 0 failed), because the tests only use `FlightBatch` and never `Validate` or the live flight setup.

**Totals:** 10 Blockers, 30 Major, 31 Minor. They are merged from three independent reviews; source IDs are kept in each item.

## How to work this list

- Take one item at a time and open one PR per item, titled `fix(<ID>): …`.
- Blockers come first, in order: B-01 and B-03 share one fix, and B-04 unblocks most of the rest.
- Every item has a reproduction. After a fix, rerun the harness (`tools/audit/designer-missions/README.md`) and add a regression test in `unity/Assets/Kestrel.Core.Tests` or `Kestrel.Game.Tests`.
- Severity:
  - **Blocker:** a mission can't be launched or won, the game crashes, or the player is told "ready" when the flight cannot succeed.
  - **Major:** a wrong or misleading number, or an option that does nothing.
  - **Minor:** a data or text inconsistency.
- Confidence:
  - **run:** reproduced by running code.
  - **emulated:** reproduced by copying the live flight setup in Core. It has not been checked in Unity.
  - **read:** found by reading code.

Paths are relative to the repo root. `Core/` = `unity/Assets/Kestrel.Core/`, `Slice/` = `unity/Assets/Kestrel.Slice/Runtime/`.

## Tracker

| ID | Sev | Area | Missions | Title | Status |
|---|---|---|---|---|---|
| B-01 | Blocker | Flight | KH-3–20 | Live flight cut off at T+600 s, before orbit | Open |
| B-02 | Blocker | Mission data | KH-3, Flock contracts | KH-3 payload 20 kg is below the designer's 50 kg minimum, so KH-3 can never launch (campaign softlock) | Open |
| B-03 | Blocker | Flight | KH-3–20 | Live flight setup differs from FlightBatch (cutoff on the ±5% edge, random pad and wind, boil-off, strap-on landing fuel) | Open |
| B-04 | Blocker | Fit | KH-4–20 | Three different payload-fit rules: designer blocks 10 winners, passes 13 losers | Open |
| B-05 | Blocker | Failure model | All | Failure model switched on by unrelated fields; prediction ≠ judge; red risks only warn; Simple says "ready" | Open |
| B-06 | Blocker | Failure model | All | Cryo rule mismatch: any LOX propellant on composite or glass-phenolic fails at T+0, unwarned | Open |
| B-07 | Blocker | Failure model | All | More than 9 engines with no heat shield fail on base heating; no rule and no heat-shield control | Open |
| B-08 | Blocker | Failure model | All | Unguided solids exempt from max-Q in the designer, not in flight | Open |
| B-09 | Blocker | Designer | KH-10, KH-19 | No control for booster landing fuel; boosters-must-land missions unwinnable from the designer | Open |
| B-10 | Blocker | Designer | All | Simple layer says "This design is ready to fly." on 217 designs that fail 0/5 | Open |
| M-01 | Major | Scoring | KH-7, 9, 10, 15, 17–20 | Ellipse and C3 missions pass on on-paper Δv, not flown Δv | Open |
| M-02 | Major | Scoring | All | Hidden pass conditions (exact payload mass, fit, budget, gentle tier); debrief gives the wrong reason | Open |
| M-03 | Major | Economy | All, contracts | Mission budgets and rewards about 1000× the real costs | Open |
| M-04 | Major | Progression | KH-1–9 | Unlocks gate nothing, and their order contradicts the requirements | Open |
| M-05 | Major | Progression | All | Earned liveries, finishes and decals can never unlock | Open |
| M-06 | Major | Scoring | All | Live stars and score use different rules from FlightBatch | Open |
| M-07 | Major | Scoring | Most | Most briefing bonuses are never scored; "window" graded as clean staging | Open |
| M-08 | Major | Mission UX | KH-1–3 | "Gentle on." shown but never applied | Open |
| M-09 | Major | Contracts | Post-KH-20 | Contracts can't be played or paid | Open |
| M-10 | Major | Readout | KH-1, KH-2 | 1000 m/s apogee loss pad: false "short" warning and a pad "Short" breakup on rockets that pass | Open |
| M-11 | Major | Repair | All | Make it fly and DvMarginFix break mission rockets (payload, engines, overshoot) | Open |
| M-12 | Major | Readout | Circular/apogee | Δv margin doesn't predict a pass (batch burns the upper stage out, so ±2% propellant flips it) | Open |
| M-13 | Major | Readout | All | Liftoff TWR uses catalog sea-level thrust; flight derives thrust from Isp (up to 5.6% apart) | Open |
| M-14 | Major | Readout | KH-10, KH-19 | Landing "need" read at booster burnout instead of core burnout | Open |
| M-15 | Major | Readout | KH-9, 10, 17–20 | Designer counts strap-on landing fuel and boil-off as ascent propellant | Open |
| M-16 | Major | UI | All | v3 strip and Hangar show different Speed %, cost (×100,000) and budget | Open |
| M-17 | Major | Designer | All | Legs light/heavy chip writes `landing.legsKind`; physics reads `legs.kind` | Open |
| M-18 | Major | Designer | All | Tank Pressure knob cuts stage dry mass about 13× on first touch | Open |
| M-19 | Major | Designer | All | Guidance steppers show defaults the sim doesn't use; clamps differ from limits; no turnShape control | Open |
| M-20 | Major | Economy | Landing missions | Landing propellant priced twice | Open |
| M-21 | Major | Economy | All | Cost ignores chamber pressure, nozzle, tune, fins, insulation | Open |
| M-22 | Major | Physics | KH-5, 10, 14, 19 | Crossfeed has no performance effect | Open |
| M-23 | Major | Physics | All | Fins weigh nothing, cost nothing, aren't drawn; count and size don't change drag | Open |
| M-24 | Major | Designer | All | Separation chip writes stage 0 (ignored); "hot" impossible; no sepImpulse | Open |
| M-25 | Major | Designer | All | One "−" on tank length empties the tank (1 kg) | Open |
| M-26 | Major | Stepper | All | Picking a mission in the guided stepper doesn't set the mission orbit | Open |
| M-27 | Major | Designer | All | Min-throttle lets solids and pressure-fed engines throttle | Open |
| M-28 | Major | Validate | All | Gimbal 0° on stage 1 not treated as unguided | Open |
| M-29 | Major | Designer | All | Add stage copies engine overrides, interstage mass and mixture onto the new vacuum stage | Open |
| M-30 | Major | Physics | All | Fairing never jettisons in flight (release altitude not passed to the sim) | Open |
| N-01 | Minor | Debrief | KH-9, 17, 18, 20 | Lunar debrief says "Goal 0 km" | Open |
| N-02 | Minor | Data | Several | Catalog payloads ≠ mission payloads; catalog names "KH-1".."KH-5" clash with mission codes | Open |
| N-03 | Minor | Data | All | campaign.json `score`/`tolerances`/`reuse` unread (hard-coded copies); sun-sync Δv subtracts Earth rotation | Open |
| N-04 | Minor | Data | — | mission-ramp.json and MissionRamp unused and contradict the campaign | Open |
| N-05 | Minor | Economy | KH-10, KH-19 | Reuse refund ignores boosters; one shared refurb counter | Open |
| N-06 | Minor | Tests | All | Goldens fail the schema; no test covers Validate or the live flight setup on missions | Open |
| N-07 | Minor | Economy | KH-3–10 | Credits can't cover a from-scratch winner (suspected) | Open |
| N-08 | Minor | Validate | All | Validate hard-codes limits instead of reading designer-limits.json | Open |
| N-09 | Minor | Limits | All | Two parallel limit sets (strap-on counts, landing reserve max) | Open |
| N-10 | Minor | Schema | All | Templates, starters and editor output fail design.schema.json | Open |
| N-11 | Minor | Designer | All | Nose shapes inconsistent across Hangar, v3, fairings.json, mesh, Balance | Open |
| N-12 | Minor | Designer | All | Interstage "truss"/"hot-staging" unselectable; flare drag differs by aero path | Open |
| N-13 | Minor | Look | All | Engine, fins, legs, gridfins, rcs finishes unreachable | Open |
| N-14 | Minor | Look | All | Livery patterns, rocket name and decals not drawn | Open |
| N-15 | Minor | Templates | — | heavy/moonshot livery id colours ≠ their primary/accent | Open |
| N-16 | Minor | Templates | KH-2 | Hopper fairing release 110 km above its 100 km apogee | Open |
| N-17 | Minor | Data | All | engines.json values outside the v3 limits | Open |
| N-18 | Minor | Data | All | Unused or inconsistent catalog data (tanks.json, catalog mass and cost fields, material cost ratios, hybrid density) | Open |
| N-19 | Minor | Budget | KH-12, missions | designer-budget.json has no leo600 or kh-m* rows | Open |
| N-20 | Minor | Designer | All | gridFinDeployAltM has no consumer or UI | Open |
| N-21 | Minor | Undo | All | v3 panel edits record undo steps that change nothing | Open |
| N-22 | Minor | Data | All | maxQThrottle units ambiguous (fraction vs percent) | Open |
| N-23 | Minor | Designer | All | Expansion-ratio override changes exit diameter in physics only | Open |
| N-24 | Minor | Data | All | Grid-fin mass 120 kg/m² ≠ fins.json 360–440 kg | Open |
| N-25 | Minor | Editor | — | Built-in fallback limits and editor data sync incomplete | Open |
| N-26 | Minor | Designer | All | v3 knobs only reach stage 1; stepper recovery has no grid-fin/RCS toggle | Open |
| N-27 | Minor | Validate | All | TWR thresholds disagree (Validate 1.0/1.25, strip 1.05/1.2, stepper 1.3) | Open |
| N-28 | Minor | UI | All | "100%" shown red at 99.6%; "1.62x x its weight" | Open |
| N-29 | Minor | UI | All | Part card ignores v3 engine params | Open |
| N-30 | Minor | Physics | — | Engine envelope table loaded from the repo checkout, not thread-safe (suspected) | Open |
| N-31 | Minor | UI | KH-11, KH-13 | Home says "Enough for this orbit." on launch-blocked picks; HUD THR reads about 90% at full throttle; v1 design with 0 usable Δv passes | Open |

## Blockers

### B-01 · Live flight cut off at T+600 s, before orbit
- **Missions:** KH-3 to KH-20.
- **Evidence:**
  - `Slice/FlightDriver.cs:128` sets `opts.tMax = 600`.
  - FlightBatch uses 2500 s (`Core/Vehicle/Stack.cs:192`).
  - Orbital winners reach orbit at t ≈ 525–900 s.
- **Repro:** `tools/audit/designer-missions/verdict/run.sh`.
  - Pad emulation: all 18 orbital winners in `data/golden/campaign-winners.json` end "timeout@600s" with periapsis between −2,500 and −5,400 km. That is 144 rows. FlightBatch passes them all 5/5.
  - Example, KH-9 winner at t=600 s: apogee 190 km, periapsis −4,507 km.
- **Fix:** take `tMax` from `Stack.SimInputs` (or at least 2500 s). Better, one shared mission-flight setup (see B-03).
- **Sources:** D-MIS-02, D-VER-01.
- **Confidence:** emulated.

### B-02 · KH-3 can never launch (campaign softlock)
- **Missions:** KH-3, and the 20 kg "flock" contract cell.
- **Evidence:**
  - `data/campaign.json` sets KH-3 `massKg: 20`.
  - `data/designer-limits.json:13` sets `payloadMassKg.min 50`. `Core/Designer/DesignRules.cs:60-63` turns that into a schema block, and `HangarSession.LaunchEnabled` and `SliceApp.cs:1024` refuse to launch.
  - `HangarEditor.SetPayloadMass` (l.486) clamps to 50, but then `HomeAutoBuild.ApplyMission` writes 20 back.
  - A 50 kg payload fails the exact-mass `payloadMatch` check (`Core/Flight/FlightDebrief.cs:189`, `FlightBatch.cs:105`).
  - Career only opens mission Cleared+1 (`Career.cs:58`), so KH-4 to KH-20 stay locked.
- **Repro:** `HomeAutoBuild.Choose` for m3 returns `LaunchEnabled=false` with "payload.mass: 50–100000 kg". All 4 KH-3 golden designs are blocked the same way.
- **Fix:**
  - Lower the designer minimum to 20 kg or less, or exempt mission payloads, or set KH-3 (and the flock archetype) to at least 50 kg.
  - Test that every mission-stamped board starter has no schema block.
- **Sources:** D-MIS-01, D-OPT-02, D-VER-04.
- **Confidence:** run.

### B-03 · Live flight setup differs from FlightBatch
- **Missions:** KH-3 to KH-20.
- **Evidence:**
  - **(a) Cutoff and aim.** `MissionAim.Apply` only handles apogee hops.
    - The live flight keeps the design's catalog target with `cutoff=true`. Headless cuts off at periapsis ≥ target − 10 km (`Core/Physics/Headless.cs:434`), which is exactly the ±5% pass edge.
    - FlightBatch instead disables the cutoff or aims far out and burns the upper stage out (`FlightBatch.cs:132-162`).
  - **(b) Site and wind.**
    - The live flight uses the briefing's random pad (Salt Flat 41°, North Reach 57.5°, Coastal Pad B 31.4°) and seeded wind.
    - FlightBatch, `Readout` and `RequiredDeltaV` assume 28.5° and calm air.
    - The campaign doesn't filter pads by target inclination, though contracts.json says contracts do.
  - **(c) Coast boil-off.** FlightBatch removes it (`FlightBatch.cs:118-124`). The live flight does not; `PostFlightSource.cs:148-152` only checks ≥ 0.
  - **(d) Strap-on landing fuel.** FlightBatch holds it out (`FlightBatch.cs:126-130`). The live flight burns it, but `PredictLandingGrade` still counts it.
- **Repro:** run `missions/` with `campaign-winners live`, or `verdict/out2500/`.
  - With tMax 2500, calm air and 28.5°, these still fail 0/5:

    | Mission | Live result |
    |---|---|
    | KH-3 | apogee 252 km |
    | KH-10 | periapsis 189.8 km |
    | KH-12 | apogee 878 km |
    | KH-19 | periapsis 189.9 km |

  - With the real site and wind added, winner pass rates drop:

    | Mission | Live passes (batch 5/5) |
    |---|---|
    | KH-5, KH-12, KH-19 | 0/5 |
    | KH-8 | 2/5 |
    | KH-4, KH-10, KH-14, KH-16 | 3/5 |
    | KH-6 | 4/5 |

- **Fix:**
  - Build the live `SimOptions` and vehicle from the same Core helper FlightBatch uses.
  - Put the cutoff inside the pass band.
  - Either fly the briefing site and wind in FlightBatch, or only offer pads that can reach the target inclination.
- **Sources:** D-MIS-03, D-VER-02.
- **Confidence:** emulated.

### B-04 · Three different payload-fit rules
- **Missions:** KH-4 to KH-20.
- **Evidence:**
  - **Mission check** (`Core/Vehicle/PayloadFit.cs:38-57`, used by FlightBatch and PostFlightSource): π(0.45D)²·0.8·L against the mission `volumeM3`, and D ≥ `diameterM` + 0.2 m.
  - **Designer block** (`Core/Designer/Validate.cs:33-39, 366-372`): 0.6·π(D/2)²·L against catalog volume × min(1.5, (m/m_cat)^(2/3)). It never checks diameter.
  - **Readout and stepper** (`Readout.cs:280-288`, `StepperChecks.cs:150-157`): catalog volume, with diameter passed as 0 because `Catalog.cs:37-43` never parses `payloads.json` `diameter`.
- **Repro (run):**
  - **The designer blocks winners.** Golden winners or "fly"-mode board starters for KH-4, 7, 8, 9, 10, 14, 16, 18, 19 and 20 are blocked. Example, KH-8: "needs 40.0 m³; holds 26.5", but the mission needs 25 m³ and the mission check has 3.6 m³ to spare.
  - **The designer passes losers.** These fairings (D × L, m) pass the designer but fail the mission check:

    | Mission | Fairing |
    |---|---|
    | KH-4 | 1.6×15 |
    | KH-7 | 3×15 |
    | KH-8 | 2.4×15 |
    | KH-9 | 2×12 |
    | KH-10 | 3.2×15 |
    | KH-11 | 1.6×8 |
    | KH-12 | 1.6×10 |
    | KH-14 | 2.4×15 |
    | KH-15 | 3×12 |
    | KH-16 | 2.4×15 |
    | KH-18 | 2×12 |
    | KH-19 | 3.6×15 |
    | KH-20 | 2×15 |

  - **The diameter is never checked.** A 3.4 m gto-comsat in a 1.5 m fairing shows +1.30 m of diameter margin.
- **Fix:** one `PayloadFit` call everywhere (Validate, Readout, stepper, FlightBatch, debrief). Use the mission spec when a mission is attached, and parse the payload diameter.
- **Sources:** D-MIS-04, D-OPT-02, D-OPT-08, D-OPT-09, D-VER-03.
- **Confidence:** run.

### B-05 · Failure model switches on for unrelated fields, and the designer doesn't predict it
- **Missions:** all.
- **Evidence:**
  - **What switches it on.** `FailureMonitor.Engages` (`Core/Flight/FailureModel.cs:429-447`) turns on for:
    - version 3, or any v3 field (mixtureOffsetPct, engineParams, tank, sepMode, guidance, legsKind, recoveryMode, heatShield, landingReserve, gridFinDeployAlt, finPlanform);
    - any `missionId`.
  - **Where it runs:**
    - The live flight attaches it when it engages, otherwise uses arcade breakups at 2.4× q_lim (`Slice/FlightDriver.cs:104, 144-147`).
    - FlightBatch always attaches it, with Gentle off (`FlightBatch.cs:163-165`).
    - The designer only predicts with it when it engages, and treats red predicted failures as warnings.
  - **Max-Q mismatch.** The structural max-Q limit is 35 kPa × (SF/1.4) × (yield/500) (`StructureModel.cs:145`), but the designer warns only above 45 kPa (`designer-limits.json` `maxQKPa.warnAbove`, `Validate.cs:31`).
- **Repro (run).** Fork each template, set `DesignerKnobs.SetMixtureOffset(d,0,0)` (no physical change), then `ReferenceAscent.Run`:

  | Template | Max-Q | Result |
  |---|---|---|
  | hopper | 50.3 kPa | structural break-up at T+27 s |
  | workhorse | 48.6 kPa | structural break-up at T+33 s |
  | heavy | 71.9 kPa | thermal failure at T+7 s |
  | lander | 53.7 kPa | structural break-up at T+29 s |
  | moonshot | 63.3 kPa | structural break-up at T+23 s |

  - Untouched, all five fly clean.
  - Validate stays ok with only `risk-*` warnings.
  - In the verdict harness, 192 designer-OK rows fail: 95 structural max-Q, 55 thermal, 23 cryo and 12 over-g.
  - tpl:heavy (own GTO) and tpl:moonshot (own TLI) fail the batch 0/5 but pass on the pad.
- **Fix:**
  - Run the failure model the same way in prediction, batch and live: always, or keyed to one explicit setting.
  - Align the max-Q warning with q_lim.
  - Block launch on deterministic red failures.
  - Re-tune the templates so they pass.
- **Sources:** D-OPT-01, D-VER-05.
- **Confidence:** run.

### B-06 · Cryo rule mismatch
- **Missions:** all.
- **Evidence:**
  - The designer warns only on composite + hydrolox, and only for v2+ designs (`DesignRules.cs:191-204`, `Hangar/Advisories.cs:160`).
  - The flight fails any storability containing "cryo" (kerolox, methalox, hydrolox) on a material with `cryoOK=false` (composite, glass-phenolic) at T+0 (`FailureModel.cs:565-571, 690`).
- **Repro (run):**
  - preset `scout` (composite + methalox) validates ok and fails 0/5 at T+0.
  - 23 designer-OK rows fail this way, including Make it fly outputs.
  - Glass-phenolic + hydrolox gets no warning at all.
- **Fix:** one rule driven by `materials.cryoOK` × propellant storability, used as a block by DesignRules and by FailureModel. Fix the scout preset.
- **Sources:** D-OPT-03, D-VER-06.
- **Confidence:** run.

### B-07 · Base heating above 9 engines; no heat-shield control
- **Missions:** all.
- **Evidence:**
  - More than 9 engines (strap-ons multiply the count) without `heatShield` gives a thermal failure once q > 5 kPa (`FailureModel.cs:556, 693, 166`).
  - No designer rule warns about it, and no UI sets `heatShield`.
- **Repro (run):** presets `stock` and `super-heavy` fail on heat at 0.7–1.2 km on all 20 missions. The designer only shows "unstable" and "maxq-high".
- **Fix:** add a heat-shield control (with mass and cost), and a designer rule that blocks or warns, using the same threshold.
- **Sources:** D-VER-07, D-OPT-01.
- **Confidence:** run.

### B-08 · Unguided solids exempt from max-Q only in the designer
- **Missions:** all.
- **Evidence:** `Validate.cs:119` and `DesignRules.cs:253` exempt them; the failure monitor does not.
- **Repro (run):** the designer-validate golden case 62 (Pipit solid) is READY with no warnings in Standard, yet fails 0/5 with a structural max-Q break-up at 6 km. This is the only Standard-layer READY-but-fails case.
- **Fix:** remove the exemption, or apply it in the failure monitor too.
- **Sources:** D-VER-08.
- **Confidence:** run.

### B-09 · No booster landing-fuel control
- **Missions:** KH-10, KH-19 (boosters must land).
- **Evidence:**
  - `HangarEditor.SetLandingReserve` (l.327) only takes `PartRef.Stage(i)`.
  - The recovery steppers (`HangarPanel.cs:400-408`, `StepperControls.cs:527`) only cover stage 0.
  - `DefaultBoosters()` has no `landingPropMass`.
  - With no booster landing fuel, `FlightBatch.GradeLanding` returns "short" and the mission fails.
  - The only working route is the board starter (2,800 kg), which is itself blocked by B-04.
- **Fix:** add a booster landing-fuel control (edit op, Hangar row, stepper step) and a test.
- **Sources:** D-MIS-08.
- **Confidence:** read.

### B-10 · Simple layer says "ready to fly" on designs that cannot pass
- **Missions:** all.
- **Evidence:** Simple hides every warning (`Core/Designer/Experience.cs:50`), and red predicted failures are warnings (B-05).
- **Repro (run):** in `verdict/out/results.tsv`, 217 rows show "This design is ready to fly." in Simple yet score 0/5. All 9 templates and presets fail all 20 missions.
- **Fix:** Simple must show a single plain-language stop when a red predicted failure, a fit failure or a mission requirement will fail the flight.
- **Sources:** D-VER-05.
- **Confidence:** run.

## Major

### M-01 · Ellipse and C3 missions pass on on-paper Δv
- **Missions:** KH-7, 9, 10, 15, 17–20.
- **Evidence:**
  - `Core/Physics/Headless.cs:590` sets `idealDeltaV = StackDeltaV(...)`, the whole rocket's capacity.
  - `GoalMet` passes ellipse and C3 missions on periapsis within 5% plus `idealDv ≥ required`.
  - So a rocket parked at about 200 km passes GTO and lunar missions, and the card reads "Apogee 200 km. Goal 35786 km."
- **Fix:** use `deltaVExpended`, or check the apogee or C3 directly.
- **Sources:** D-MIS-12.
- **Confidence:** read and emulated.

### M-02 · Hidden pass conditions
- **Missions:** all.
- **Evidence:**
  - `FlightDebrief.GoalMet` (l.189) fails the flight on any of: a payload mass that isn't exactly the mission's, a fit failure, going over budget, or a gentle-tier design.
  - The payload stepper (`HangarPanel.cs:451`) and Make it fly ("Lightened the payload", `Validate.cs:204`) both change the mass.
  - `designer-budget.json` says over budget "is a warning, never a block".
  - On a miss, `NumberLine` (`FlightDebrief.cs:349-350`) prints "Reached X km. The goal was Y km." even when the altitude was hit.
- **Fix:** lock payload type and mass on mission designs, list these conditions as requirements in the designer, and give the real reason in the debrief.
- **Sources:** D-MIS-05.
- **Confidence:** read, and run for the fit case.

### M-03 · Budgets and rewards about 1000× too large
- **Missions:** all, plus contracts.
- **Evidence:**
  - Winner costs are 4,838 Kc (KH-1) to 34,687 Kc (KH-19); mission budgets are 2,000,000–240,000,000 Kc, so costs use 0.009–0.24% of the budget.
  - `designer-budget.json` `byOrbit` (leo400 40,000 Kc) is on the real scale.
  - As a result the cost term in the score is always about +200, budget warnings never fire, and the bank gains about 2.35 billion Kc over the campaign.
  - Contract budgets (15M–288M Kc) have the same problem.
- **Fix:** rescale campaign and contract budgets and rewards by about 1/1000, or reprice.
- **Sources:** D-MIS-06.
- **Confidence:** run.

### M-04 · Unlocks gate nothing, and the order contradicts the requirements
- **Missions:** KH-1 to KH-9.
- **Evidence:**
  - `Career.Unlocks` only feeds `MissionMap.PartsLine` and the debrief line. No designer code calls `HasUnlock` for parts; the only real gate is credits.
  - The order contradicts the requirements:
    - KH-6 needs legs, but legs are KH-6's own reward.
    - `redundant-avionics` arrives after KH-8, which already needs 0.98 reliability.
    - Strap-ons, needed by KH-5, are never unlocked.
    - KH-10 to KH-20 unlock nothing.
- **Fix:** either gate designer options by unlock and reorder (legs before KH-6, strap-ons/crossfeed before KH-5), or remove the unlock copy.
- **Sources:** D-MIS-07.
- **Confidence:** read.

### M-05 · Earned looks never unlock
- **Missions:** all.
- **Evidence:**
  - `HangarSession.progress` stays `HangarProgress.None` (`Core/Hangar/HangarSession.cs:19`).
  - The Look tab and `ApplyLivery`, `SetFinish` and `AddDecal` gate on it (`HangarPanel.cs:482-533`, `HangarEditor.cs:592,615,632`).
  - The earn rules use "KH-1".."KH-6", but mission ids are "m1".."m20".
  - `Kestrel.Livery.PlayProgress` is never used.
- **Fix:** feed career counters and cleared missions into `session.progress`, with an id map.
- **Sources:** D-OPT-14.
- **Confidence:** read.

### M-06 · Live stars and score use different rules from FlightBatch
- **Missions:** all.
- **Evidence:**
  - **Stars.** `PostFlightSource.cs:95` sets `GLimit` to the mission's maxG or 0, where FlightBatch uses min(8, payload maxG, mission maxG) (`FlightBatch.cs:264-271`). With a limit of 0, three stars are impossible on KH-1, 2, 3, 5, 9, 11, 14, 17, 18 and 20. Example: m2a-2500 earns 3 stars in the batch, 2 live.
  - **Score.** The live score adds a landing bonus for any landing (`FlightDebrief.cs:256`); FlightBatch only when a landing is required (`FlightBatch.cs:202`).
- **Fix:** score through one Core function for both.
- **Sources:** D-MIS-09.
- **Confidence:** read.

### M-07 · Most briefing bonuses are never scored
- **Missions:** most.
- **Evidence:** `AscentDecisions.FromDesign` (l.336-359) only recognises a few id fragments.
  - **Never evaluated:**

    | Bonus id | Missions |
    |---|---|
    | margin | KH-3, KH-14 |
    | land-booster | KH-4 |
    | strap-ons | KH-5, KH-14 |
    | perfect-pad | KH-6 |
    | barge | KH-7, KH-13, KH-15 |
    | quiet | KH-8, KH-16 |
    | coast | KH-9, KH-17, KH-20 |
    | boosters | KH-10, KH-19 |
    | same-hour | KH-11 |

  - `window` is graded as clean staging.
  - KH-6, 11, 13, 15, 16 and 19 have no scored bonus at all.
  - Bonuses that are scored only appear in the HUD; they give no stars, score or credits.
- **Fix:** implement each bonus id from data, and feed bonuses into the score.
- **Sources:** D-MIS-10.
- **Confidence:** read.

### M-08 · "Gentle on." shown but never applied
- **Missions:** KH-1 to KH-3.
- **Evidence:**
  - `gentleDefault: true` makes `MissionMap.cs:454` and `BriefingBoard.cs:186` say "Gentle on.".
  - FlightBatch forces gentle off (`FlightBatch.cs:164`), and a gentle-tier design fails `GoalMet`.
- **Fix:** apply gentle physics where the data says so, or drop the text.
- **Sources:** D-MIS-11.
- **Confidence:** read.

### M-09 · Contracts can't be played or paid
- **Missions:** post-KH-20 contracts.
- **Evidence:**
  - `ContractGen` has no callers in Kestrel.Game or Kestrel.Slice. The board says "No contract is posted yet." (`MissionMap.cs:81`).
  - `Career.Settle` rejects any index above 20.
  - The flock cell hits B-02.
- **Fix:** wire `ContractGen` into the board and Career, or hide contracts for 2.0.
- **Sources:** D-MIS-18.
- **Confidence:** read.

### M-10 · 1000 m/s apogee loss pad
- **Missions:** KH-1, KH-2.
- **Evidence:**
  - The required Δv for apogee targets adds 1000 m/s (`Stack.cs:179`, `MissionScore.cs:85-90`).
  - The KH-1 winner (1020 vs 1443 m/s) shows "About 423 m/s short".
  - The pad destroys it at burnout (`PlayedBreakup.Short`, `FlightDriver.cs:213`), yet FlightBatch passes it 5/5 at 9.6 km.
  - `DvMarginFix` "repairs" it to 0/5 (overshoot).
- **Fix:** base apogee Δv on a reference ascent, not a flat pad, and drop the pad "Short" breakup for apogee hops.
- **Sources:** D-MIS-21, D-VER-09.
- **Confidence:** run.

### M-11 · Make it fly and DvMarginFix break mission rockets
- **Missions:** all.
- **Evidence:**
  - "Lightened the payload" (`Validate.cs:203-204`) causes a payload-mismatch failure.
  - Added engines skip the cluster-pattern and geometry checks (`Validate.cs:177-196`), which creates "cluster-count" or "engines-dont-fit" blocks.
  - Added propellant overshoots the ±5% band.
- **Repro (run):** in the verdict harness, 18 flying designs go to 0/5 after Make it fly. 172 Make it fly outputs are designer-OK but fail, and 16 `DvMarginFix` results are OK but fail. Hopper with 30 t of propellant becomes "Pattern row3 takes 3 engines; stage has 5".
- **Fix:** never change mission payloads, use `HangarEditor.SetEngineCount`, and aim the margin at the mission band.
- **Sources:** D-OPT-15, D-VER-10.
- **Confidence:** run.

### M-12 · Δv margin doesn't predict a pass on circular or apogee missions
- **Missions:** circular and apogee missions.
- **Evidence:** FlightBatch burns the upper stage out (`FlightBatch.cs:142-161`), so passing needs a near-exact Δv.
- **Repro (run):** `verdict/out/sensitivity.txt`:
  - ±2% upper-stage propellant fails KH-6, even at a 21–22% designer margin.
  - KH-12 fails at −5%, −2% and +2%.
  - The designer's 5% minimum margin and "Enough for this orbit." push players to add Δv.
- **Fix:** once B-03 makes the batch cut off inside the band, check the margin against the band and say so in the readout.
- **Sources:** D-VER-11.
- **Confidence:** run.

### M-13 · Liftoff TWR source mismatch
- **Missions:** all.
- **Evidence:**
  - The designer uses catalog sea-level thrust (`Readout.cs:121`, `Stack.cs:62,71`); the sim derives thrust from Isp (`Headless.cs:68,214`).
  - They disagree by: gg-kerolox-heavy −3.0%, srm-small −2.0%, pf-kerolox +2.2%, sc-methalox +2.1%, hybrid-small +5.6%.
- **Repro (run):** a design with designer TWR 1.020 has sim TWR 0.990 and sits on the pad for 7.4 s.
- **Fix:** compute TWR from the normalized sim stage, or fix the catalog data.
- **Sources:** D-VER-12.
- **Confidence:** run.

### M-14 · Landing "need" read at the wrong burnout
- **Missions:** KH-10, KH-19.
- **Evidence:** `ReferenceAscent.cs:103-106` caches the first burnout, which is booster burnout; the judge uses core burnout (`FlightBatch.cs:292,302`).
- **Repro (run):** KH-10 2,585 vs 2,717 m/s; KH-19 2,797 vs 2,978 m/s.
- **Fix:** read the core-stage burnout.
- **Sources:** D-VER-13.
- **Confidence:** run.

### M-15 · Designer counts strap-on landing fuel and boil-off as ascent propellant
- **Missions:** KH-9, 10, 17–20.
- **Evidence:** `Mass.cs:272` counts both; FlightBatch removes them (`FlightBatch.cs:118-130`).
- **Repro (run):**
  - KH-10: 11,363 → 11,333 m/s (margin +0.3% → +0.03%, right at the pass line).
  - KH-19: 11,387 → 11,359 m/s.
  - preset kid on KH-20: −0.5%.
- **Fix:** subtract them in the readout, the same way FlightBatch does.
- **Sources:** D-VER-14.
- **Confidence:** run.

### M-16 · v3 strip and Hangar disagree
- **Missions:** all.
- **Evidence:**
  - **Speed.** The v3 strip shows the margin % (`DesignerV3Strip.cs:37-39`); the Hangar shows Δv/required % (`HangarFormat.cs:82`), so the same design reads 15% in one and 115% in the other.
  - **Cost.** The strip (`DesignerV3Strip.cs:79-89`) and `StepperStats.cs:63` show "$" on minor units, which is 100,000× the Hangar's Kc.
  - **Budget.** `QuickReadout.budget` is never set (`Readout.cs:92-93`), so cost never turns red, while the judge fails over-budget flights.
- **Fix:** both views read one formatter.
- **Sources:** D-VER-15.
- **Confidence:** read.

### M-17 · Legs kind chip writes the wrong key
- **Missions:** all.
- **Evidence:**
  - `DesignerV3Edits.SetLegsKind` writes `landing.legsKind` (`Core/Designer/DesignerV3Edits.cs:79-87`), but `Design.FromJson` reads `legs.kind` (`Core/Vehicle/Design.cs:278`).
  - `Mass` always uses 6% for legs (`Mass.cs:18,63`), while `Balance` uses the kind fraction (`Balance.cs:129`).
- **Repro (run):** after "heavy" is picked, `Design.legsKind` stays null, landing speed stays 2 m/s and mass is unchanged.
- **Fix:** write `legs.kind`, and make Mass use `RecoveryModel.LegDryFraction`.
- **Sources:** D-OPT-05.
- **Confidence:** run.

### M-18 · Tank pressure knob changes the mass model
- **Missions:** all.
- **Evidence:**
  - When unset, the UI shows 3.0 bar (`DesignerKnobs.cs:21,274`), but the mass model uses the structural fraction.
  - Any set value switches to the hoop-stress shell (`TankModel.cs:194-200`).
- **Repro (run):** workhorse stage-1 shell is 11,700 kg unset but 1,360 kg at the displayed 3 bar. Stage-1 dry mass goes 15,040 → 4,700 kg, and Δv goes 11,080 → 11,341 m/s.
- **Fix:** calibrate the two models to agree at the default, or show "auto" when unset.
- **Sources:** D-OPT-06.
- **Confidence:** run.

### M-19 · Guidance steppers
- **Missions:** all.
- **Evidence:**
  - When unset, the UI shows kick 2°, kick altitude 100 m and g-limit 4 g (`DesignerKnobs.cs:32-40`). The sim actually uses a 4° kick at 60 m/s with no g-limit (`GuidanceProfile.cs:19-20`).
  - One "+" stores 2.5°, which lowers the real kick from 4°.
  - The clamps are 0–20° and 1.5–8 g; the limits file says 0–30° and 1–12 g.
  - `turnShape` exists in the schema, the limits file and the physics, but has no control.
- **Fix:** show the real defaults (or "auto"), read the clamps from data, and add a turnShape control.
- **Sources:** D-OPT-07.
- **Confidence:** run.

### M-20 · Landing propellant priced twice
- **Missions:** landing missions.
- **Evidence:** `CostModel.PriceDesign` prices `propMass + landingPropMass` (`Core/Economy/CostModel.cs:110`, and `web/js/economy/costModel.js:108`), but landing propellant is already inside `propMass` (`Mass.cs:67-68`).
- **Repro (run):** workhorse at 30% reserve goes from 10,018,800 to 12,386,880 with the same 260,000 kg of propellant.
- **Fix:** price `propMass` only.
- **Sources:** D-OPT-10.
- **Confidence:** run.

### M-21 · Cost ignores performance options
- **Missions:** all.
- **Evidence:** engine cost uses the catalog pc and thrust (`CostModel.cs:37-50`).
- **Repro (run):**
  - Engine cost is identical at 661,544,547 for the base engine, 115% pc (vacuum thrust 1,074 vs 934 kN), the minimum pc, the extended nozzle and tune +15%.
  - Fins and insulation add nothing.
- **Fix:** price the resolved `EnginePoint`, fins and insulation.
- **Sources:** D-OPT-11.
- **Confidence:** run.

### M-22 · Crossfeed does nothing to performance
- **Missions:** KH-5, 10, 14, 19.
- **Evidence:**
  - `Stack.SerialStages` never reads `crossfeed` (`Stack.cs:153-165`); only an imbalance-event chance does (`AscentDecisions.cs:213-218,287`).
  - The UI hint says "Boosters feed the core" (`HangarPanel.cs:383`).
- **Repro (run):** heavy with liquid boosters gets 12,890.7 m/s with crossfeed off and on.
- **Fix:** model crossfeed in the parallel phase, or reword the hint.
- **Sources:** D-OPT-12.
- **Confidence:** run.

### M-23 · Fins are weightless, free and invisible
- **Missions:** all.
- **Evidence:**
  - Liftoff mass is identical with fins off, 3 × 0.2 m and 8 × 6 m.
  - Drag is a flat +0.04 for any fins (`Stack.cs:34`). `AeroModel.FinMassKg` (`AeroModel.cs:148`) is never called.
  - No fin mesh exists (`FinCluster` is only used for grid fins, `SliceApp.cs:944-953`).
  - `fins.planform` and `rootChord` have no UI.
- **Fix:** add fin mass, cost, drag scaled by area, and visuals. Expose planform or drop it.
- **Sources:** D-OPT-13.
- **Confidence:** run.

### M-24 · Separation chip does nothing useful
- **Missions:** all.
- **Evidence:**
  - `DesignerV3Panel.cs:838` writes `stages[0].sepMode`; the failure model only reads `sepMode` on upper stages (`FailureModel.cs:458-464, 552-554, 720-735`).
  - "hot" needs the hot-staging interstage, which `SetInterstageShape` rejects (`HangarEditor.cs:31,341-344`).
  - No UI sets `sepImpulse`.
  - The chip's only real effect is turning on the failure model (B-05).
- **Fix:** write upper-stage sepMode, add sepImpulse and a hot-staging option, or remove the chip.
- **Sources:** D-OPT-04.
- **Confidence:** run.

### M-25 · One "−" on tank length empties the tank
- **Missions:** all.
- **Evidence:** `PropMassForLength` returns 0 at or below the dome allowance (0.33·D), which then clamps to 1 kg (`HangarEditor.cs:302-323`).
- **Repro (run):** workhorse stage 2 (3.7 m, 3 t): 1.5 m → 1.2 m sets `propMass` to 1 kg.
- **Fix:** clamp the length at the dome length plus one step.
- **Sources:** D-OPT-16.
- **Confidence:** run.

### M-26 · Guided-stepper mission pick doesn't set the orbit
- **Missions:** all.
- **Evidence:**
  - `StepperControls.SetMission` (l.166-193) sets the payload, not the orbit. The payload's home orbit is used instead (kid-box → leo200, not kh-m1).
  - `HomeAutoBuild.ApplyMission` does set `kh-mN`.
- **Fix:** reuse `ApplyMission`.
- **Sources:** D-OPT-17.
- **Confidence:** read.

### M-27 · Min-throttle lets solids throttle
- **Missions:** all.
- **Evidence:** the clamp is 0.05–1 regardless of the catalog range (`DesignerKnobs.cs:22,218-236`), and the sim honours it (`AscentDecisions.cs:379-380`, `Headless.cs:523`).
- **Repro (run):** srm-small (catalog [1,1]) with a 1.5 g limit: peak 36 g with the catalog throttle, 1.50 g with throttleMin 0.05.
- **Fix:** clamp to the catalog range, and disable the control when the range is [1,1].
- **Sources:** D-OPT-18.
- **Confidence:** run.

### M-28 · Gimbal 0° not treated as unguided
- **Missions:** all.
- **Evidence:** `PadSteered` and `Gimballed` read the catalog `e.gimbal` (`DesignRules.cs:286-297`), not `engineParams.gimbalDeg`.
- **Repro (run):** workhorse with gimbal 0 has stability −7.79 cal and Validate ok, and the predicted flight tumbles at T+0.6 s.
- **Fix:** use the resolved `EnginePoint.gimbal`.
- **Sources:** D-OPT-19.
- **Confidence:** run.

### M-29 · Add stage copies overrides
- **Missions:** all.
- **Evidence:** `AddStage` clones the stage below (`HangarEditor.cs:58-69`) without `ReconcileEngineParams`.
- **Repro (run):** the new gg-kerolox-vac stage keeps expansion ratio 8 (catalog 165), pc 40, a duplicated 5,000 kg interstage override and a 9% mixture.
- **Fix:** drop or reconcile `engineParams`, `interstageMass`, `mixtureOffsetPct`, `tank` and `sepMode` on the copy.
- **Sources:** D-OPT-20.
- **Confidence:** run.

### M-30 · Fairing never jettisons in flight
- **Missions:** all orbital missions.
- **Evidence:**
  - `Stack.cs:80` reads the release altitude, but `SimInputs` (`Stack.cs:192`) never passes it to the sim. The fairing rides to orbit.
  - `TimelineGenerator.cs:727-750` still narrates a separation.
- **Fix:** pass the jettison altitude to the sim and drop the fairing mass there.
- **Sources:** D-VER-19.
- **Confidence:** read.

## Minor

| ID | Title | Evidence | Fix | Source | Conf. |
|---|---|---|---|---|---|
| N-01 | Lunar debrief "Goal 0 km" | `PostFlightSource.cs:93` sets `GoalKm = ApoKm`, which is 0 for C3 targets; `FlightDebrief.cs:349-350` prints it | Show C3 or the target text | D-MIS-13 | read |
| N-02 | Catalog payloads ≠ mission payloads | kid-box 300 kg/2 m³ vs KH-1 50 kg/0.2 m³; leo-sat, crew-capsule, lunar-probe and gto-comsat differ too; `displayName` "KH-1".."KH-5" clashes with mission codes | Use mission values when a mission is attached; rename payload display names | D-MIS-14 | read |
| N-03 | campaign.json blocks unread | `score`/`tolerances`/`reuse` are hard-coded in MissionScore and CostModel; `InclinationWithin` unused; sun-sync `RequiredDeltaV` subtracts Earth rotation (8,676 < 8,760 m/s) | Read the data; no rotation credit for retrograde | D-MIS-15 | read |
| N-04 | mission-ramp.json unused | `MissionRamp` has no callers outside tests; its barge assists and star rule contradict the campaign | Delete it or wire it in | D-MIS-16 | read |
| N-05 | Reuse refund | `PostFlightSource.Quote` calls `RecoveredHardwareMinor(price,true,false)`, so boosters are never refunded; `ArticleKey` is always "core" | Pass the booster flag; key by design | D-MIS-17 | read |
| N-06 | Goldens and tests | `campaign-viable.json` covers KH-1–10 only; 27/30 viable and 54 contract designs fail the schema; no test runs Validate or the live setup on missions | Add both tests | D-MIS-19, D-VER-23 | run |
| N-07 | Credits vs winners | Winner credit quotes are 146, 408 and 594 at KH-3, 6 and 10, against 46, 130 and 270 earned | Rebalance credits | D-MIS-20 | suspected |
| N-08 | Validate hard-codes limits | Strap counts 0/2/4 (`Validate.cs:330`), 1.6 top-heavy ratio (l.355), max-Q 45 (l.31); error texts; stepper TWR 1.3 | Read designer-limits.json | D-OPT-21 | read |
| N-09 | Two limit sets | `strapOnCounts` is [0,2,4] vs [0,2,3,4,6,8]; landing reserve max 0.3 vs 0.2 | One set | D-OPT-22 | run |
| N-10 | Schema failures | All templates fail on `look`; board starters on `strapOns.diameter 0`; the editor writes keys the schema forbids | Update the schema or the writers | D-OPT-23 | run |
| N-11 | Nose shapes | Hangar has 4, v3 has 6; "capsule-tower" is rejected; the mesh draws biconic/hammerhead as ogive; von-karman is 0.45 in Balance vs 0.5 in the mesh | One nose table | D-OPT-24 | read |
| N-12 | Interstage shapes | "truss"/"hot-staging" unselectable; flare drag is 0.02 vs 0.03/0.008 by aero path (`InterstageAero.cs:52-82`) | Expose them or drop them; one drag value | D-OPT-25 | run |
| N-13 | Unreachable finish slots | Only tank, booster, interstage and fairing are offered (`HangarRocketView.cs:41`) | Offer or remove the rest | D-OPT-26 | read |
| N-14 | Look not drawn | solid, checker_roll and gradient_aft patterns, `livery.name` and `look.decals` aren't rendered (`HangarRocketView.cs:116-130`, `SliceApp.cs:959-978`) | Render or hide | D-OPT-27 | read |
| N-15 | Template look data | heavy and moonshot livery ids' colours ≠ their primary/accent | Align | D-OPT-28 | run |
| N-16 | Hopper fairing | Release at 110 km, above the 100 km apogee (minimum allowed 80) | Set it to 80 km or less | D-OPT-29 | run |
| N-17 | engines.json out of limits | exp-hydrolox ε max 400 (cap 250); srm-large ε min 7.7 (limit 8); sc-hydrolox max throttle 1.09 | Fix the data or the limits | D-OPT-30 | run |
| N-18 | Unused or inconsistent catalogs | tanks.json not loaded; fairing, interstage and fin mass and cost unused; `engines[].cost` unused; material `costPerKg` ratios vary; hybrid density 1100 vs 1174 | Remove or use | D-OPT-31 | read |
| N-19 | Budget rows | No leo600 or kh-m* rows in designer-budget.json, so they fall back to 40,000 | Add the rows | D-OPT-32 | read |
| N-20 | gridFinDeployAltM | No consumer and no UI; it only switches the drag model (`AeroModel.cs:54`) | Remove or wire up | D-OPT-33 | read |
| N-21 | Empty undo steps | `SetInsulation`, `SetCommonBulkhead`, `SetSepMode` and `SetLegsKind` always return an Edit | Return `Edit.None` when unchanged | D-OPT-34 | read |
| N-22 | maxQThrottle units | A value ≤ 1.5 is read as a fraction (`GuidanceProfile.cs:139,150`); the schema says percent | One unit | D-OPT-35 | read |
| N-23 | Expansion-ratio exit diameter | Fit, overhang and mesh use the catalog exit (`Validate.cs:349`, `DesignRules.cs:112`, `RocketMeshBuilder.cs:92`) | Use the resolved exit | D-OPT-36 | read |
| N-24 | Grid-fin mass | 120 kg/m² gives about 1,290 kg on 3.7 m (`Mass.cs:18`), vs 360–440 kg in fins.json | Use catalog mass | D-OPT-37 | run |
| N-25 | Editor fallbacks | Fallback limits lack al-cu, titanium and glass-phenolic areal densities (`DesignerLimits.cs:43`); `SliceBootstrap.cs:29` data sync omits fairings, interstages, fins, designer-stepper and campaign | Complete them | D-OPT-38 | read |
| N-26 | v3 knobs stage 1 only | Mixture, pc, ε, gimbal, throttle, pressure, insulation and bulkhead are stage 1 only; the stepper recovery step has no grid-fin/RCS toggle | Per-stage controls | D-OPT-39 | read |
| N-27 | TWR thresholds | Validate 1.0/1.25, strip 1.05/1.2 (`DesignerV3Strip.cs:45`), stepper 1.3 (`StepperChecks.cs:16-17,47`) | Read twr.* from the limits file | D-VER-16 | read |
| N-28 | Rounding and text | 99.6% shows "100%" in red; "1.62x x its weight" | Floor before display; drop the extra x | D-VER-17 | read |
| N-29 | Part card | `DataSheet.cs:137-142` ignores v3 engine params and propellant scaling | Use `EnginePoint` | D-VER-18 | read |
| N-30 | Engine envelope table | `EngineModel.cs:285-322` loads from the repo checkout and sets "loaded" before filling; the device fallback differs (pcMin 0.5 vs 0.45, epsMax 285 vs 400) | Ship the table in Resources; lock it | D-VER-20 | suspected |
| N-31 | UI text and HUD | Home shows "Enough for this orbit." on blocked picks (`HomeAutoBuild.cs:362-366`); HUD THR reads about 90% at full throttle (`FlightDriver.cs:273-274`); a v1 design with 0 usable Δv passes | Fix each | D-VER-21, 22, 24 | run |

## Missions at a glance

Budget is in Kc. Requirements and unlocks marked "–" mean the mission has none.

| Mission | Payload | Target | Requirements | Unlocks | Budget | Issues |
|---|---|---|---|---|---|---|
| KH-1 | kid-box 50 kg | apogee 10 km | – | fins, nose | 2M | M-02, M-03, M-04, M-06, M-08, M-10, N-02 |
| KH-2 | kid-box 100 kg | apogee 100 km | – | stage-2 | 5M | M-02, M-03, M-04, M-06, M-08, M-10, N-16 |
| KH-3 | cubesat 20 kg | circular 200 km / 28.5° | margin ≥ 0 | nozzle-expansion | 15M | **B-02**, B-01, B-03, M-07, M-08 |
| KH-4 | leo-sat 2,500 kg | circular 500 km / 51.6° | maxG 6 | pc, throttle | 40M | **B-01, B-03, B-04**, M-07 |
| KH-5 | leo-sat 12,000 kg | circular 400 km / 51.6° | strap-ons | crossfeed | 90M | **B-01, B-03, B-04**, M-04, M-22 |
| KH-6 | leo-sat 8,000 kg | circular 200 km / 51.6° | RTLS landing | legs, grid-fins | 70M | **B-01, B-03, B-04**, M-04, M-12 |
| KH-7 | gto-comsat 5,500 kg | GTO ellipse | barge landing | insulation | 110M | **B-01, B-03, B-04**, M-01, M-07 |
| KH-8 | crew-capsule 12,000 kg | circular 400 km / 51.6° | maxG 4.5, reliability 0.98 | redundant-avionics | 160M | **B-01, B-03, B-04**, M-04 |
| KH-9 | lunar-probe 3,000 kg | C3 −2 | coast 3 h | hydrolox-upper | 180M | **B-01, B-03, B-04**, M-01, M-15, N-01 |
| KH-10 | gto-comsat 7,000 kg | GTO ellipse | RTLS + boosters land | – | 120M | **B-01, B-03, B-04, B-09**, M-01, M-14, M-15 |
| KH-11 | weather-sat 2,200 kg | circular 550 km / 97.6° | – | – | 80M | **B-01, B-03**, N-03 |
| KH-12 | space-telescope 1,800 kg | circular 600 km / 51.6° | maxG 5 | – | 100M | **B-01, B-03** (fails even at 28.5° in calm air), M-12, N-19 |
| KH-13 | cubesat-dispenser 480 kg | circular 550 km / 97.6° | barge landing | – | 90M | **B-01, B-03**, N-03 |
| KH-14 | cargo-capsule 10,000 kg | circular 400 km / 51.6° | margin ≥ 0, strap-ons | – | 130M | **B-01, B-03, B-04** |
| KH-15 | geo-relay 6,200 kg | GTO ellipse | barge landing | – | 140M | **B-01, B-03**, M-01 |
| KH-16 | crew-capsule 12,000 kg | circular 500 km / 51.6° | crew set | – | 180M | **B-01, B-03, B-04** |
| KH-17 | lunar-lander 4,000 kg | C3 −2 | coast 3 h | – | 190M | **B-01, B-03**, M-01, N-01 |
| KH-18 | lunar-probe 2,500 kg | C3 −0.4 | coast 3 h | – | 200M | **B-01, B-03, B-04**, M-01, N-01 |
| KH-19 | gto-comsat 7,500 kg | GTO ellipse | RTLS + boosters land | – | 210M | **B-01, B-03** (fails even at 28.5° in calm air), **B-04, B-09**, M-14, M-15 |
| KH-20 | lunar-probe 3,200 kg | C3 −0.25 | coast 4 h | – | 240M | **B-01, B-03, B-04**, M-01, N-01 |
| Contracts | 6 archetypes | LEO / SSO / GTO / TLI | margin, maxG, reliability ladder | – | 15M–288M | **M-09**, B-02 (flock), M-03 |

Issues that touch every mission (B-05 to B-08, B-10, M-02, M-03, M-11, M-13, M-16 to M-30) are not repeated per row.

**What checks out:**
- Briefing Δv and mass numbers match the code.
- Reward equals budget everywhere.
- Every payload id and orbit id exists.
- Every requirement type is parsed.
- `credits.json` `missionClear` lines up with the 20 missions.
- The briefing budget and hangar budget agree once `ApplyMission` stamps `budgetKc`.
- No NaN, exceptions or nondeterminism were found across 467 design × mission runs, re-flown serially and in parallel.

## How to verify

The harnesses are in `tools/audit/designer-missions/` (see its README). They compile `Kestrel.Core` directly and change nothing in the repo.

```sh
export PATH=/opt/dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1
tools/audit/designer-missions/verdict/run.sh          # design × mission matrix, pad emulation, sensitivity, probes (~1 min)
dotnet run -c Release --project tools/audit/designer-missions/missions/Missions.csproj -- campaign-winners live   # also: board | fit | credits
dotnet run -c Release --project tools/audit/designer-missions/options/Options.csproj      # per-option probes (B-05, M-17–M-29)
dotnet test unity/dotnet/Kestrel.Core.Tests/Kestrel.Core.Tests.csproj                     # existing suite: 607 passed, 8 skipped
```

`verdict/out/results.tsv` has one row per design × mission. It holds the designer verdict (Standard and Simple), Δv and requirement, TWR, the FlightBatch result over 5 seeds, apogee and periapsis, the pad emulation result, and the Make it fly and DvMarginFix outcomes. `summary.txt` groups the rows by mismatch type. The pad emulation copies `FlightDriver`'s setup (dt 1/120, tMax 600, `MissionAim`, `StopForBreakup`, `PostFlightSource` grading), but leaves out the ascent director, wind and pad latitude. Confirm B-01 and B-03 in Unity once they're fixed.
