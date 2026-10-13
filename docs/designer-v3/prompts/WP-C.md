# Kestrel Heavy: Designer v3 WP-C: Tanks and structure

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
§2.3, §2.6, §8 GV4/GV5/GV10

## You own
new `unity/Assets/Kestrel.Core/Vehicle/TankModel.cs`, new `Vehicle/StructureModel.cs`, `Vehicle/Mass.cs`, `data/materials.json`, `data/tanks.json`, new `Kestrel.Core.Tests/TankStructureTests.cs`

## Task
Implement the following:
- `TankModel`: wall thickness, tank mass with domes, insulation mass, boil-off over hours, common bulkhead, fineness.
- `StructureModel.Allowables(stage)`: F_allow, q_lim, and `Margin(state)` → MS.
- Mass.cs uses TankModel only when the v3 tank fields are present.
- Add `elasticGPa` and `cryoOK` to every material, where they are missing.
- Tests: GV4, GV5, the q_lim formula, and v2 identity on all presets.

## Done when
- Unity CI and all required checks are green on your branch, rebased on main.
- There are new tests with the formulas from the spec, and the existing goldens are unchanged.
- A PR titled `designer-v3(WP-C): Tanks and structure` is opened as DRAFT. The body lists the files touched, the test results, the spec traceability IDs covered (§10) and the follow-ups.
- Then STOP. Do not merge and do not enable auto-merge.
