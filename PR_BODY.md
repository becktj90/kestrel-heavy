# #313: one shared throttle director for batch, designer, timeline and live pad (option b)

> **DRAFT. Not ready to merge.** The Core suite has 25 failures (listed below). The golden diff goes to Kestrel Physics first. Four rulings are needed; see "Findings Physics needs to rule on" in GOLDEN_DIFF.md.

Closes #315

## Root cause

Only the live pad (`FlightDriver`) flew the `AscentDirector`. With auto-assist on, the autopilot auto-accepted every call. It took the max-Q throttle-down at a hard-coded 70% even when the design set no max-Q throttle, and it took a default nose-down pitch nudge. `FlightBatch`, `ReferenceAscent` (designer practice flight) and `LaunchPredictor` flew the bare `Headless` sim with no director. So prediction and play were two flight models: KH-1 9.787 km batch vs 10.262 km live, and KH-2 97.56 km vs 100.5–102.2 km.

## Fix

- `Kestrel.Core/Flight/DirectedFlight.cs` (new): one stepper (apply command → `AscentSim.Tick` → `AscentDirector.Advance` → apply). `MissionFlightSetup.Director`/`Start`/`Fly` build it. `FlightBatch`, `ReferenceAscent` (timeline and practice flight), `LaunchPredictor` and `FlightDriver`/`AscentFlight` all use it.
- Option (b), `AscentDirector.AutopilotAccept`: the autopilot takes the max-Q call only when the design set a max-Q throttle, and at the design's own value. It never takes the pitch nudge. Player taps in live play are unchanged.
- Event location (`SimOptions.locateCutoff`, on for every mission flight in batch and live): when a step crosses the target-energy cutoff, `AscentSim` bisects the step length from the saved step-start state. That is 40 fixed iterations, so it's deterministic, and it takes the partial step. Prop depletion already landed exactly on burnout.
- Hop retunes (bands unchanged): KH-1 starter 515 → 464 kg, m2b 2540 → 2585 kg, m2f 1560 → 1580 kg. m1b stays at 705 kg, because 645 kg flies 9.07 km and fails under (b).
- Assist note (Physics item 6): under (b) the default player just flies the design's settings. There is no open-loop bias left. The only remaining assist action is the design's own max-Q value. Prediction flies the same director with the same default settings (auto-assist on, Simple depth).

## Tables

See GOLDEN_DIFF.md: hop peaks per seed, KH-6 and worst-case gaps before and after location, the per-step orbital gap table with runtimes, every verdict flip, and every flight old → new.

Headline: KH-1 starter 10.028 km on every seed (live 10.046 km). KH-2 starter 99.360 km at 7.66 g batch / 7.69 g live (#316). KH-6 after location: Δapo 0.199%, Δperi 6 m, ΔSMA 0.0031% at 0.2 s.

## Tests

New: `SharedDirectorTests` covers the default player on every golden winner and starter on seeds 1–5, batch-vs-live agreement (hops 0.5% on peak; orbits 0.5% apoapsis, 1 km perigee, 0.1% SMA as named constants) and determinism. Plus `AscentDecisionTests.Autopilot_KeepsTheDesignSettings_ButAPlayerTapStillTakesEitherCall`.

Expectation changed (it was wrong under (b)): `AscentDecisionTests` auto-assist max-Q expectations 0.70 → 0.60 (the test vehicle now sets a 60% max-Q throttle).

| Suite | Passed | Skipped | Failed |
|---|---:|---:|---:|
| Core | 724 | 14 | **25** |
| Site | 28 | 0 | 0 |
| Pad.Core | 84 | 0 | 0 |
| Vehicle.Core | 41 | 0 | 0 |
| Build | 13 | 0 | 0 |
| Guide.Core | 41 | 0 | 0 |

node: 179 tests, 172 pass, 0 fail, 1 skipped, 6 todo. stubcheck --all: exit 0.

The Core failures fall into four groups. None was loosened or edited:
- **Golden verdict flips** (Physics findings 1 and 3): CampaignWinners_FlyPassOnEverySeed, CampaignViableDesigns_WinM1ThroughM10, EveryMission_HasThreeViableDesigns, EveryCell_AndEveryOffer_IsViable, BoardStarter_FixItOrOneStrengthEdit_WinsEveryMission, DefaultPlayer_EveryGoldenWinnerAndStarter_PassesOnEverySeed (new), GoldenWinnersHaveNoCertainFailureTheyDoNotHave, DesignFirstRocketsAreReadyAfterThePredictRun, NAV01, NAV02.
- **Orbital agreement** (finding 2): BatchAndLivePad_Agree_OrbitsOnApoapsisPeriapsisAndSemiMajorAxis (new). m3, m5, m7, m12 and m19 are out.
- **Prediction vectors and text at the cutoff margin** (finding 1, perigee lands on target − 10 km): GoldenPredictions_MatchCommittedVectors, BrokenPrecondition_FlagsCausality, FairingRemoval_RemovesTheEvent, PropellantCut_FlagsAFailedPrediction, UnchangedDesign_HasAnEmptyDiff.
- **Tests that model the pad or the timeline with the bare sim** (finding 4): TheLivePadResultPeaksAtTheBatchValue(m1, m2), TheTimelineShowsTheSameFlownApogee, TheOldKH1StarterReadAPassOnKeplerAndFlewShort, DefaultDesignClearsTheFirstHop, PadAimMatchesTheBatch, ReferenceMaxQ_MatchesFullHeadlessPeak, StabilityBand_IsFinnedOrUnguided_AndMaxQHighDoesNotBlock, GateFixIt_RepairsAWeakAirframe. These need their pad and timeline models moved onto `DirectedFlight` once finding 4 is decided.

Not run here: Unity EditMode and PlayMode (GameCI only).

## Kestrel Physics sign-off

- Orbital tolerances: apoapsis 0.5% / perigee 1 km / SMA 0.1%. TBD
- Cutoff margin and criterion (findings 1–2): TBD
- FuelBalance penalty scope (finding 3): TBD
- Director max-Q vs guidance max-Q (finding 4): TBD
- Signed off by: 
