# Kestrel Heavy: Designer v3 WP-B: Propulsion physics

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
§2.2, §8 GV2/GV6

## You own
new `unity/Assets/Kestrel.Core/Vehicle/EngineModel.cs`, `Vehicle/EnginePoint.cs`, `data/engines.json`, `data/schemas/engines.schema.json` (only the engine keys), new `Kestrel.Core.Tests/EngineModelTests.cs`

## Task
Implement `EngineModel.Resolve(engine, nozzle, pc, eps, throttleMin, gimbalDeg, mixtureOffsetPct, tune)`, returning thrustSL/Vac, ispSL/Vac, mass, exitDiameter, throttle range, gimbal, hazard rate and a flowSeparation flag. Use the §2.2 formulas exactly.
- Add `pcMin`/`pcRated`/`epsMin`/`epsMax`/`cycleMassFactor` to every engine in engines.json.
- Make `EnginePoint.Of` route through EngineModel only when the v3 fields are set.
- Tests: GV2a–d to 0.01 s, GV6, and the no-v3-field identity across all 20 engines.

## Done when
- Unity CI and all required checks are green on your branch, rebased on main.
- There are new tests with the formulas from the spec, and the existing goldens are unchanged.
- A PR titled `designer-v3(WP-B): Propulsion physics` is opened as DRAFT. The body lists the files touched, the test results, the spec traceability IDs covered (§10) and the follow-ups.
- Then STOP. Do not merge and do not enable auto-merge.
