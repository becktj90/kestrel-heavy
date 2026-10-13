# Kestrel Heavy: Designer v3 WP-F: Failure visuals, telemetry, post-flight report

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
§3 (visual, report), §5

## You own
new `unity/Assets/Kestrel.Game/Failure/**`, new `unity/Assets/Kestrel.Slice/Runtime/Report/**`, new `unity/Assets/Kestrel.Core/Flight/TelemetryLog.cs`, new Game.Tests `FailureFxTests.cs`/`ReportTests.cs`; ONE registration call in `SliceApp.cs` (only after #187–#192 are merged; say so in the PR)

## Task
Prerequisites: WP E and the #187–#192 art chain are on main.
- `TelemetryLog`: a ring buffer of t/alt/v/q/g/mass/losses plus events.
- Failure FX per §3: break at the weakest joint using the existing AirframeLayout/FairingLayout/LandingLayout, per-engine bell flash and plume cut (BellCluster index), a real tumble, a leg fold.
  - Pooled particles.
  - Use only the existing shaders. If you add a keyword, add it to the always-included/variant list (shader stripping broke build 101).
- Post-flight report: the explainer card (icon, kid line, engineer line, Fix-it deep link), plus plots with event markers.
- Portrait layout from SE to iPad, VoiceOver, Reduce Motion (no flash/shake).
- PlayMode capture shots of each failure go in the PR body.

## Done when
- Unity CI and all required checks are green on your branch, rebased on main.
- There are new tests with the formulas from the spec, and the existing goldens are unchanged.
- A PR titled `designer-v3(WP-F): Failure visuals, telemetry, post-flight report` is opened as DRAFT. The body lists the files touched, the test results, the spec traceability IDs covered (§10) and the follow-ups.
- Then STOP. Do not merge and do not enable auto-merge.
