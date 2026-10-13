# Kestrel Heavy: Designer v3 WP-H: Templates, hangar reveal, perf

You are a Cursor cloud agent on becktj90/kestrel-heavy (Unity 6.3 LTS, C#, portrait-only iOS game).

## Hard rules (read first)
- NEVER merge. No auto-merge. Do not enable merge queues. Open a PR and STOP. The lead merges.
- Never touch the `xcode-export` branch, `ios/` signing, or App Store files.
- No Unity GUI. Use batchmode CLI/CI only. The GameCI Unity CI (EditMode, PlayMode capture, iOS export) and every required check must pass.
- Portrait-only. Never add landscape.
- No real company names, vehicle names or trademarks in code, data, text or art.
- Free or self-made assets only (CC0 / own work), credited in ASSETS_LICENSES.md.
- $0 budget: no paid services.
- Touch ONLY the files listed under "You own". If you need a change elsewhere, write it in the PR body as a follow-up.
- Any design with no v3 fields must reproduce the existing goldens exactly (data/golden/*.json, DesignerGoldenTests, catalog-flight).
- Spec: docs/DESIGNER_V3_SPEC.md (sections cited below). Repo: becktj90/kestrel-heavy. Branch from the latest main.
- Do not commit pad reference photos.

## Spec sections
§7, HANGAR_REVEAL_UNITY_PLAN.md

## You own
`data/designer-templates.json`, `data/presets.json`, new `unity/Assets/Kestrel.Slice/Runtime/Reveal/**`, `unity/Assets/Kestrel.Core/Designer/ReadoutBench.cs`, new `Kestrel.Game.Tests/RevealTests.cs`

## Task
Prerequisites: WP G and #179 are on main.
1. **Templates:** add the 6 v3 teaching templates from §7. Each failure template must fail the way its name says, verified in tests. The non-failing templates must fly.
2. **Re-verify:** check every existing template under v3.
3. **Reveal:** build the hangar reveal from the plan: ≤ 6 s, skippable, portrait beats, inspect/cutaway hooks reusing the existing layouts.
4. **Perf:** readout p95 < 0.5 ms and hangar frame budget as in the plan.
5. **Capture:** take capture shots.

## Done when
- Unity CI and all required checks are green on your branch, rebased on main.
- There are new tests with the formulas from the spec, and the existing goldens are unchanged.
- A PR titled `designer-v3(WP-H): Templates, hangar reveal, perf` is opened as DRAFT. The body lists the files touched, the test results, the spec traceability IDs covered (§10) and the follow-ups.
- Then STOP. Do not merge and do not enable auto-merge.
