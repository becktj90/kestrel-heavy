# Kestrel Heavy: Designer v3 WP-E: Failure model, readout v3, goldens

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
§3, §5, §8 (all GV), loss accounting

## You own
new `unity/Assets/Kestrel.Core/Flight/FailureModel.cs`, `Physics/Headless.cs`, `Physics/Ascent3Dof.cs`, `Physics/Integrator.cs`, `Designer/Readout.cs`, `Designer/ReferenceAscent.cs`, `Designer/Validate.cs`, `Designer/DesignRules.cs`, new `data/golden/designer-v3.json`, new `Kestrel.Core.Tests/DesignerV3GoldenTests.cs`

## Task
Prerequisite: WPs A–D are on main.

FailureModel:
- Implement all 11 failure kinds in §3: deterministic, seeded, allocation-free.
- `FailureEvent` has a kid sentence and an engineer line, both built from template strings with numbers.
- Gentle-physics flag.

Headless/Ascent3Dof:
- Call FailureModel every step.
- Use GuidanceProfile when present.
- Add gravity/drag/steering loss accounting.

Readout v3 fields per §5. Risk prediction runs in ReferenceAscent. Validate/DesignRules turn red risks into issues with a tab and a field for deep links.

Write designer-v3.json with GV1–GV10. Keep ReadoutBench p95 < 0.5 ms for the instant path.

## Done when
- Unity CI and all required checks are green on your branch, rebased on main.
- There are new tests with the formulas from the spec, and the existing goldens are unchanged.
- A PR titled `designer-v3(WP-E): Failure model, readout v3, goldens` is opened as DRAFT. The body lists the files touched, the test results, the spec traceability IDs covered (§10) and the follow-ups.
- Then STOP. Do not merge and do not enable auto-merge.
