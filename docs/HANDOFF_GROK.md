# Handoff notes for the grok bot team

_Written 2026-10-25 by Claude Code. Main is at #357 (52e2ffd)._

These notes cover what a new agent needs to pick up work on Kestrel Heavy: where things stand, how to build and test, what is in flight, and the traps we already fell into. Read it alongside the long-lived docs below. Those win if they disagree with this page.

## 1. Read these first

| Doc | Why |
|---|---|
| `docs/STANDING_BRIEF.md` | Product goals, priorities (Vehicle Designer first), design rules. |
| `docs/PROJECT_STATE.md` | Living state, the "Pending with Trevor" list, house rules. |
| `docs/DECISIONS.md` | Numbered decisions. Log new design and tech calls here. |
| `docs/audits/DESIGNER_MISSION_DISCREPANCIES.md` | Designer × mission discrepancy list (10 Blocker, 30 Major, 31 Minor) with repro and fix per item. **It was audited at b8ad6b9 (Oct 19).** Re-run the harnesses before working an item (see §6). |
| `NAV_BUGS.md` | Known navigation bugs and the KnownBug tests that pin them. |
| `docs/PAD_DETAIL_GRAMMAR.md`, `docs/PAD_ENGINEERING_RULES.md`, `docs/UMBILICAL_CONTRACT.md` | Rules for pad and tower art. |
| `docs/PHYSICS.md`, `docs/PHYSICS_CORE_AUDIT.md`, `docs/FLIGHT_EVENTS.md`, `docs/MISSION_EVENTS.md` | Sim and event model. |
| `docs/XCODE_CLOUD_PLAN.md`, `docs/XCODE_CLOUD_FIRST_SETUP.md` | The release pipeline. |

## 2. Rules that are easy to break

- **Trevor approves:** spend, App Store, signing, Xcode project changes, privacy and legal changes. Never do these without his yes. The full list is in PROJECT_STATE.
- **Never touch the `xcode-export` branch by hand.** It is an orphan branch written only by the `xcode-export.yml` workflow (see §4).
- **Don't open the Unity editor GUI.** Change files only, and use CI as the Unity runner.
- **Assets:** free assets only, logged in the license files. No new third-party packages unless the PR notes them.
- **Data:** catalogs stay data-driven in `data/*.json`.
- **Tests:** add EditMode tests for new logic. Never skip, disable or quarantine a test to get green. A `KnownBug_*` test is retired by turning it into a regression test (see NAV-07 in #328 and NAV-09 in #356).
- **Branches:**
  - Keep PRs small.
  - Squash-merge into main.
  - On someone else's branch, add merge commits only: no rebase and no force-push.
- **PR body:** list what changed, test results, and what you couldn't verify.

## 3. Build and test locally (no Unity needed)

```sh
export PATH=/opt/dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_ROOT=/opt/dotnet
dotnet test unity/dotnet/Kestrel.Core.Tests/Kestrel.Core.Tests.csproj   # ~1050 tests, 10 skipped, ~12 min
python3 tools/cs-stubcheck/stubcheck.py --all                             # compiles every Unity asmdef against stubs
python3 tools/cs-stubcheck/stubcheck.py --base origin/main                # changed assemblies, INCLUDING Kestrel.Capture.Tests
npm test                                                                  # node / web tests (~1.5 min)
npm run gen:golden                                                        # regenerate goldens; never hand-edit data/golden/*
```

Gotchas:
- `stubcheck --all` does **not** compile `Kestrel.Capture.Tests`. Run `--base origin/main` as well when you touch PlayMode tests.
- `ReadoutV2Tests.Recompute_MeetsLatencyBudget` is a wall-clock test. It fails locally when the machine is busy, and CI excludes it. Ignore it locally.
- Unity EditMode and PlayMode tests run only in CI. Plan on one CI round-trip per Unity-only fix, and reason carefully from code first.

## 4. CI and release

**CI on every PR:**
- `ci.yml`: Node tests and the iOS Debug and Release builds.
- `core-dotnet.yml`: Kestrel.Core golden tests.
- `unity.yml`: Unity tests, in four jobs.

| Job | What it runs | Time |
|---|---|---|
| EditMode | All EditMode tests | ~20 min |
| PlayMode | Every PlayMode test except device captures (`-testCategory !DeviceCapture`) | ~13 min |
| PlayModeDevice | `[Category("DeviceCapture")]`: Countdown, Home and FlightPhase captures | **30–50 min**, see below |
| iOS export compile-check | Unity iOS export, no Xcode | ~5 min |

- The runner has no GPU and renders through llvmpipe, so the device captures are slow. Countdown alone takes about 19 minutes.
- **Watch item:** PlayModeDevice took 50 of its 60 minutes on #357, because the FlightPhase test now flies KH-3 to T+192. If it starts timing out, split the three fixtures into separate matrix entries or cut Countdown's settle frames. Don't just raise the timeout.
- Unity CI doesn't run on pushes to main, only on PRs. Treat the latest merged PR's run as main's status.
- Logs from `get_job_logs` run to about 400 KB. Parse the NUnit XML in them (`<test-case ... result="Failed">` plus `<message>`) instead of reading them.
- The Node job hung once inside a browser test, and a rerun passed.

**Release:**
1. Run `xcode-export.yml` (workflow_dispatch, inputs `build_number` and `ref`) to build the Unity iOS project.
2. `tools/ios-export/publish.sh` publishes it to the orphan `xcode-export` branch and tags it `export/2.0.0-<build>`.
3. Trevor starts the Xcode Cloud build by hand on his Mac.

The latest export is **2.0.0 (207)**, from main f5f69bd on Oct 22. Main has moved on since then (#294–#357), so the next export is 208. Exporting needs Trevor's go-ahead.

## 5. What landed Oct 19–25

| PR | What |
|---|---|
| #294 | Designer × mission discrepancy audit plus rerunnable harnesses (`tools/audit/designer-missions/`) |
| #354 | Flight report opens at orbit insertion. A booster still in the air finishes its landing on Auto-assist. |
| #350 | Narrator fact tags on every clip, plus 26 new takes (147 clips) |
| #348 | Countdown "Below" camera framing, sky hole fix, picker tap y-flip, Follow |
| #306 | Flight-sink event-order contract, first-peak max-Q, guarded sinks |
| #305 | Time of day: UTF-8 FNV-1a, JS port, shared golden, bundled table |
| #353 | Campaign card fits or scrolls, lit payload renders |
| #355 | PlayMode split into PlayMode and PlayModeDevice; Home captures wait for readiness |
| #356 | NAV-09 retired; CONTINUE is the Home primary CTA; no AppShell leak between test fixtures |
| #357 | Web KESTREL title row fits; trajectory-mark labels fit; FlightPhase capture flies KH-3; RecoveryShow particle fix |

After #357, every Unity test job on that PR's head is green.

## 6. Open work and the backlog

### In progress on Oct 25 (Claude Code agents; check whether a PR exists before starting)

Trevor's feedback after watching it on his iPhone:

1. **"The tower needs more detail like an actual tower on a launch pad."**
   - Branch `feat/pad-tower-detail`.
   - The flight scene draws a flat slab with one yellow strut, even though a rich procedural pad module exists in `unity/Assets/Kestrel.Game/Pad/` (PadGenerator, SwingArmTruss, TowerAnchors and others).
   - Goal: a lattice fixed-service structure with platforms, crew access arm, umbilical arms and a lightning mast. It must stay within mobile perf budgets (PadPerf) and keep the countdown Below framing (PadSightlines).
2. **"The camera buttons need to work better."**
   - Branch `fix/flight-camera-controls`.
   - Audit Following / Follow rocket, Cam: Auto, the picker and pinch/drag.
   - Labels must match the real state. A user's choice beats Auto. Hit targets must be at least 44 pt. Auto must not pick shots where the rocket is tiny; his recording shows a far chase at 60 km.
3. **"I still haven't been able to land the booster."**
   - Branch `fix/booster-landing-reserve`.
   - In his recording, stage 1 on a Haven-landing mission is still burning at 65 km and 2.8 km/s, then shows "Stage 1 is almost empty. Separate in the window?". The landing reserve is being burned for ascent.
   - Fix: hold the reserve so that, with the starter design and Auto-assist, the booster lands on KH-6, 7, 10, 13, 15 and 19. Add a Core test per mission.

### Backlog worth doing next

- **Discrepancy list Blockers B-01 to B-10.** Re-check each against current main first.
  - #337 (one shared flight director) and #343/#345 (starter tuning) landed after the audit, so B-01 and B-03 may be partly fixed.
  - Rerun `tools/audit/designer-missions/verdict/run.sh` and compare with the doc.
- **KH-1 count says "89 m/s short for First Hop" while the designer says Ready.** This is item M-10: a flat 1000 m/s apogee loss allowance.
- **Engineer mode (#354).** A hand-flown planned landing is finished by the autopilot at insertion and graded on the autopilot's result. Needs Trevor's sign-off, or let the player keep flying.
- **Kids tumble line (#350).** It was cut to "Round and round!", which dropped the "add fins / move weight up" hint. Product call.
- **`GuardedFlightSink` (#306).** It swallows every exception type, including OutOfMemory, and logs to stderr. Point `Log` at `Debug.LogWarning` in Unity.
- **`LaunchTime.PickTimeOfDay` (#305).** No game code calls it yet. Without the bundled table it throws on device. Wire `LaunchTimeBoot` before using it.
- **`PayloadThumbnails` (#353).** LRU eviction (64 entries) can leave a thumb pointing at a destroyed texture until its next Refresh. Low risk.
- **Rocket radius (#348).** The Below-hold `rocketRadius` uses `bounds.extents.x`, while `VehicleRadius(bounds)` exists. Switch to it so off-centre strap-ons are framed right.
- **FlightPhase capture.** The flight-phase screens at T+116 and T+192 were captured for the first time on #357. Check those PNGs (artifact `flight-phase-captures`) for HUD issues.
- **Codex review.** Codex review runs on ready-for-review but never finished on #305 or #306. Don't block on it.
- **Housekeeping for Trevor.** Delete the `cursor/*` branches, since agents can't delete branches (the proxy returns 403).

## 7. Traps we hit (save yourself a CI round)

- **Per-frame UI measuring stalls flights.** Calling `MeasureTextSize` or reading `resolvedStyle` in a per-frame or GeometryChanged path (the trajectory rail labels) stopped the live flight before MECO in PlayMode. Size labels from font size arithmetic instead (see `AscentHud.TrajectoryRailView`).
- **ParticleSystem duration.** `AddComponent<ParticleSystem>()` starts the system playing. Call `Stop(true, StopEmittingAndClear)` before setting `main.duration`, or Unity asserts and the test fails on an unhandled log.
- **`AppShell` survives scene loads.** A PlayMode fixture that boots Boot must destroy a leftover `AppShell.Instance` first, or it gets an empty Home root (the MenuPlayWalk NRE).
- **Home CTA.** Since #300 there is no `home-continue` on Home. CONTINUE is `home-primary` with `MainMenuOverlay.CtaIsContinue`. Use `NavIds.Continue(root)`.
- **The KH-1 starter is already a 1-engine rocket.** The "crashing rocket" test's `engineCount = 1` spoil changes nothing, so don't rely on it to force a failure.
- **KH-1 ends at T+25.7 by design (a 10 km hop).** Use KH-3 (orbit at about T+517) when a test needs a long flight. `FlightCapture.BootToCount(cleared: 2)` does this.
- **UiTreeChecks is strict.** Text taller than its content box fails, often by inherited theme padding. Use the `Unpad` helper (MissionBoardScreen, AscentHud) instead of loosening the checker.
- **Git worktrees.** Moving a branch from another checkout leaves the worktree's index stale, and the stale file shows as staged changes. Check `git diff HEAD` before committing.
- **Merging PRs through the GitHub API.** `merge_pull_request` needs the full 40-character `expectedHeadSha`, not a short one.

## 8. Useful harnesses

- `tools/audit/designer-missions/verdict/run.sh`: design × mission matrix, live-pad emulation, sensitivity and probes. Output goes to `out/results.tsv` and `summary.txt`.
- `tools/audit/designer-missions/missions` (modes `campaign-winners live`, `board`, `fit`, `credits`) and `.../options`: per-option probes.
- They compile Kestrel.Core straight from source, so a throwaway dotnet console app linking `../../unity/Assets/Kestrel.Core/**/*.cs` is the fastest way to answer a sim question without Unity.
