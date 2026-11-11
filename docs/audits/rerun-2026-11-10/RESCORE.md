# Designer × mission audit re-score — 2026-11-10

**Head.** `main` `1a69b7c1` (#491).  
**Not device-verified.** This closes readiness **D-7** (the audit re-run), not every remaining product discrepancy.

Harnesses: `tools/audit/designer-missions/verdict/run.sh`, `missions/ … fit`, `missions/ … board`. Logs in this folder.

## Harness vs shipping flight

The verdict `full` profile and the missions-board `full` / `lat28-calm-t600` profiles still cut many orbital flights at **t=600**, so `BATCH_PASS_LIVE_FAIL` / `LIVE_TIMEOUT` stay high on that profile (t600 counts are **identical** to the 2026-11-09 rerun: 131 / 155). Lifting the harness cap to 2500 s drops live timeouts 155 → 3 and batch-pass-live-fail 131 → 82.

That leftover is the **harness**, not the shipping game. Product live flight uses `MissionFlightSetup` / `tMax ≥ 2500` (B-01 / B-03, already Fixed). Do not read harness `full` timeouts as a campaign softlock.

`SIMPLE_LAYER_READY_BUT_FAILS` is still 228. That flag is `Experience.Present(report, "simple")` returning `Experience.Ready` on designs whose FlightBatch score is 0/5. The shipping **Starter** badge is honest (`StarterHonestReadyTests`; KnownGaps is empty). B-10 stays Open for the Experience simple layer only.

## Blockers re-scored

| ID | Status | Evidence |
|---|---|---|
| B-01 | Fixed (unchanged) | `MissionFlightSetup` / `Stack.SimInputs` `tMax ≥ 2500`; FlightDriver |
| B-02 | **Fixed** | `designer-limits.json` `payloadMassKg.min` is **10** (was 50). KH-3 is still 20 kg cubesat. `missions board`: m3 `launchEnabled=True`, `blocks=[]`. `CampaignFlightLoopTests` asserts LaunchEnabled on every board starter |
| B-03 | Fixed (unchanged) | Shared `MissionFlightSetup` with FlightBatch |
| B-04 | **Fixed** | `PayloadFitRuleTests` — Validate, Readout, stepper, LaunchGate and the FlightBatch judge agree; winners/board starters fit; the old audit losers are rejected everywhere. `missions fit`: no `designer-OK-but-mission-fails` or `designer-blocks-but-mission-fits` rows on KH-1…KH-20 |
| B-05 | Open | Not re-verified on this pass |
| B-06 | **Fixed** | Cryo-on-composite is no longer unwarned: `StarterHonestReadyTests.CryoOnComposite_IsNamed_ByTheSameFlightAsThePad` — badge cause `Cryo`, kid line "The cold fuel would crack its tanks." The flight still fails, as it should |
| B-07 | Open | Not re-verified on this pass (base-heating / heat-shield rule) |
| B-08 | Open | `DesignV2Tests.MaxQ_IsWarnOnly_AndSkipsUnguidedSolids` still documents the designer exemption |
| B-09 | **Fixed** | Hangar has a "Landing fuel" stepper (`HangarPanel`); stepper `landingReserve` checks; `StarterHonestReadyTests` names a short reserve; landing missions have CI (`BoosterLandingMissionTests`) |
| B-10 | Open | Starter / LaunchGate honest Ready is already verified (D-3, `StarterHonestReadyTests`). Experience **simple** layer still reports Ready on 228 harness rows that fail 0/5 |

## Majors sampled

| ID | Status | Evidence |
|---|---|---|
| M-30 | **Fixed** | Fairing jettison altitude is passed into the sim (`MissionFlightSetup`, `AscentDecisions`, `Stack.SimInputs`). `FlightSimSinkContractTests.MissionSetup_WiresTheDesignJettisonAltitude_AndTheFairingHookFires`. Readiness D-5 already verified |

Other majors and all minors stay **Open** unless a later pass re-scores them. They are not silently closed.

## What this does not claim

- No device play-through.
- No claim that every Major/Minor is gone.
- No change to physics tests.
