# Launch pad graphics — Codex fixes handoff (for the grok bot team)

Status: reviewable PR work, NOT merged. The PR still says "Do not merge without
approval" — that stands; the merge decision is Trevor's.

## Branch and commits

- PR: https://github.com/becktj90/kestrel-heavy/pull/361
- Branch: `launch-pad-graphics`, base `main` @ `52e2ffd`
- Original change: `48f0f4d` — yellow tower crown, base plates, pad circle, apron
  terrace (visuals only; see the PR body).
- Codex fixes: `c1db2cd5` — "Fix Codex P2s: flat base-plate footings, sealed
  apron/plinth boundary (visuals only)". This note was added on top of that
  commit; see the PR page for the current head.

## What the fix commit changed (2 files)

### 1. Base plates render flat — `unity/Assets/Kestrel.Game/Pad/Runtime/PadRenderer.cs`

`MemberMatrix` (unit +Y beam stretched A→B) used the minimal rotation
(`Quaternion.FromToRotation(up, dir)`) for every member. For the X-running
`tower.base.plate.*` members that maps the beam's 0.9 m width onto world Y, so
the plates rendered as 0.9 m tall fins, 0.08 m across, sticking 0.55 m up and
0.35 m below the deck.

Fix: for `MemberKind.Deck` members whose run is axis-aligned and horizontal
(`|dir.y| < 1e-4` and `|dir.x| > 0.9999` or `|dir.z| > 0.9999`), build the basis
flat instead — length along the run, width horizontal across it, thickness
(vertical, 0.08 m) up. The plate member endpoints are UNCHANGED (2.2 m along X
at y = 0.10, `base-plate` tag, outside the vehicle keep-out cylinder), so the
layout, hashes, and all non-rendering data are identical; only the rendered
box changes, to 2.2 (X) x 0.08 (Y) x 0.9 (Z), sitting at y 0.06–0.14 — embedded
in the deck plates (which top out at 0.09 m), not floating.

Deliberate scope limits, all verified by harness checks:

- Z-running decks (level decks, crew walk) render IDENTICALLY under both bases
  (the box is symmetric), so they are unaffected.
- Diagonal Deck members (lightning-mast plats, corner-to-corner) keep the
  legacy matrix — the guard is axis-aligned only. Flattening those is a
  follow-up, not part of this fix.
- Every non-Deck kind is untouched (columns, girts, braces, arms, rails).
- No new draw call: same meshes, same batches. `PadPerf` counts are unchanged.

### 2. Apron/plinth boundary sealed — `unity/Assets/Kestrel.Game/Pad/Core/TrenchMeshBuilder.cs`

Codex found the apron inner edge (48-sided) diverging from the plinth wall
(28-sided), leaving an exposed-ground gap up to 52 cm on a 100 m pad.

Fix: `Apron()` now uses `n = 28`, the same angle sequence Slab and Plinth use
(`2*pi*i/28`), so the apron inner edge and the plinth wall share the same
chords exactly (gap 0 by construction). Triangle count drops (28 top + 28 wall
quads max, ~112 tris, vs 48 + 48, ~192).

Strengthening found during re-verification (same commit): the old crossing
test checked only segment midpoints/endpoints, which missed narrow corner
lobes of the grade-opening polygon — for canonical seed 11 the old rule kept
ALL 48 segments, bridging all four trench-exit mouths with concrete. The test
now samples both chords (16 steps) and both radial sides (8 steps) of every
segment; a segment is dropped if any sample falls inside the grade opening,
plus the existing one-neighbour buffer each side. This drops a superset of
what the old rule dropped. Trench exits stay open, as designed.

## Verification (done here, dotnet harness — no Unity in this sandbox)

- Harness: 61 checks pass (the prior 56, plus 5 new). The prior 56 are the
  repo's Pad Core assertions replicated outside vstest (see below), plus the
  original 7 new-behavior checks.
- New checks: (a) every base plate is flat under the new matrix
  (2.2 x 0.08 x 0.9) and a fin under the old one (2.2 x 0.9 x 0.08), across
  seeds 1–12 x Tower/TowerStrongback; (b) guarded non-plate Deck members are
  all Z-running; (c) non-plate decks render identically under both matrices;
  (d) Rail layouts gain no plates; (e) apron/plinth seal — from the REAL built
  mesh, every apron-top inner vertex sits on the 28-gon (angle a multiple of
  2*pi/28, radius exactly deck), kept apron chords never enter the channel
  (32 samples each), and the plinth/apron chord gap is 0.000 over all
  kept-both segments.
- Before/after dumps, seeds 11/42/1/9/3 (TowerStrongback/Tower/Rail): ZERO
  non-apron differences — layout hash, member counts, graph counts, deck and
  surface radii, tower params, trench spec, swing-arm specs, light plan, draw
  calls, and pad-circle markings are all identical. Concrete changes are
  exactly 2x the apron-top change in every case, i.e. only apron top+wall
  quads changed. Worst case (seed 9, 120 m TowerStrongback): 35,544 / 40,000
  triangles, 14 draw calls (unchanged).
- Schematic renders (PIL drawings of dumped geometry, NOT in-game
  screenshots): plan before/after shows the apron ring going from unbroken
  (bridged exits) to open at the four trench exits; a footing cross-section
  shows old fin vs new flat footing. Regeneration: dotnet dump, then the
  render scripts (see "Reproducing" below).

## What was NOT verified here (read this before merging)

- NO Unity run exists in this environment (no Unity installed), and the PR's
  CI check status could not be queried from here (the GitHub status tool is
  not advertised to this agent, and the checks page does not render as text).
  Do NOT treat this note or the PR body as claiming CI green.
- `PadRenderer.cs` could NOT be compiled here: it is Unity-dependent and the
  repo's dotnet test project (`unity/Assets/Kestrel.Game/Pad/Tests/Dotnet~/`)
  covers Core only. The `MemberMatrix` fix was verified by replicating its
  math exactly in the harness (old shortest-arc rotation vs new flat basis),
  not by executing Unity code. Unity CI's iOS compile check is the compile
  verdict for this file.
- Renders are schematic (same dumped geometry, same viewpoints), not Unity,
  browser, or device screenshots.

## Reproducing the verification

The harness is NOT in the repo (it references repo sources by absolute path):

- Harness project: `~/kh-dump/KhDump.csproj` (dotnet SDK 8.0.426 at
  `~/dotnet`); checks in `~/kh-dump/Check.cs`, dump writer in
  `~/kh-dump/Program.cs`. Copies of the fix-era harness files are kept with
  the workstream notes (see below).
- Run checks: `~/dotnet/dotnet run --project ~/kh-dump/KhDump.csproj check`
- Dump before/after JSON: `~/dotnet/dotnet run --project ~/kh-dump/KhDump.csproj
  <out.json>` (cases: seeds 11/42/1/9/3; canonical seed 11 includes full
  member + marking + apron geometry).
- Renders: the workstream folder's `render.py` (plan/elevation from dump JSON)
  and `footing_detail.py` (plate cross-section from the canonical member).
- Workstream folder (dumps, renders, harness snapshots, prior notes):
  `~/workspace/goals/kestrel-heavy-1-launch/hidden_files/launch-pad/`
  (`dump-prefix.json` = pre-fix, `dump-postfix.json` = post-fix,
  `plan-before-fix.png` / `plan-after-fix.png`,
  `footing-detail-fix.png`, `Check-fix.cs`).
- Repo's own NUnit Pad tests: run `dotnet test
  unity/Assets/Kestrel.Game/Pad/Tests/Dotnet~/Kestrel.Pad.Core.Tests.csproj`
  where the vstest testhost works. In this sandbox the testhost starts but
  its channel never connects (even for single-test discovery), which is why
  the assertions were replicated in the harness instead. That is a sandbox
  limitation, not a repo failure — the project itself builds clean.

## Open items for the grok bot team (Unity continuation)

1. Confirm Unity CI status on PR #361. Workflow: `.github/workflows/unity.yml`
   (GameCI, Unity 6000.3.26f1; triggers on `pull_request` and
   `workflow_dispatch`; jobs skip cleanly if the `UNITY_LICENSE` secret is
   absent — if jobs skipped, that secret is the first thing to check).
   Jobs to be green: Unity tests (EditMode, PlayMode, PlayModeDevice) and the
   iOS export compile check. Capture artifacts land as GitHub Actions
   artifacts on the PR's workflow run: `screenshots` (PlayMode real scenes),
   `countdown-captures` (held count in every camera, INCLUDING the pad
   camera), `flight-phase-captures`, `ui-captures`, `home-captures`,
   `device-captures`, plus `test-results-*` and `xcode-ios-compile`.
2. Eyeball a real Unity/device build of the pad at liftoff and confirm:
   yellow tower crown; flat base plates under the legs (not fins); pad circle
   + 8 ticks on the deck, broken at the trench slot; apron terrace ring with
   OPEN trench exits (no concrete bridging the channel); no visible gap
   between apron inner edge and plinth wall.
3. Run the repo's own NUnit Pad tests where the testhost works (or confirm
   CI ran them), since the harness here is a replication, not the repo suite.
4. Merge decision: still Trevor's call ("Do not merge without approval").
   This PR touches only the two Pad files above plus this note. It does NOT
   touch PR #362's files (`web/js/haven-control.js`, `web/js/mission.js`) or
   PR #360's headless-sim files; if any overlap appears, rebase order matters
   and this PR's changes (members added, mesh quads added, one renderer
   matrix branch) move no existing geometry.
