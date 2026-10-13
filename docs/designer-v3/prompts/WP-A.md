# Kestrel Heavy: Designer v3 WP-A: Schema, propellant, payload fit

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
§2.1, §2.4 (fields), §2.9, §4 (payload fields), §8 GV7

## You own
`unity/Assets/Kestrel.Core/Vehicle/Design.cs`, `Save/DesignMigration.cs`, new `Vehicle/PayloadFit.cs`, `data/schemas/*`, `data/designer-limits.json`, `data/payloads.json`, `data/propellants.json`, new `Kestrel.Core.Tests/DesignV3SchemaTests.cs`

## Task
Add design version 3. All new fields are optional, with defaults equal to v2 behaviour:
- per stage: `engine.pc`, `engine.expansionRatio`, `engine.throttleMin`, `engine.gimbalDeg`, `mixtureOffsetPct`, `tank.pressureBar`, `tank.insulation`, `tank.commonBulkhead`, `sepMode`, `sepImpulse`
- vehicle level: `boosterSepPct`, `fins.planform/rootChord`, `gridFinDeployAltM`, `legs.kind`, `recovery.mode`, `landingReserveFraction`, `guidance{pitchKickAlt,pitchKickDeg,turnShape,maxQThrottle,gLimit}`, `heatShield`, `missionId`

Then:
- Write the v2→v3 migration (an identity on numbers), add the schema and limits, and add `volume`/`diameter` to payloads.
- Implement `PayloadFit.Check(design, payload)`, which returns fit/volume/diameter margins.
- Tests: round-trip, migration, and payload-fit cases.

## Done when
- Unity CI and all required checks are green on your branch, rebased on main.
- There are new tests with the formulas from the spec, and the existing goldens are unchanged.
- A PR titled `designer-v3(WP-A): Schema, propellant, payload fit` is opened as DRAFT. The body lists the files touched, the test results, the spec traceability IDs covered (§10) and the follow-ups.
- Then STOP. Do not merge and do not enable auto-merge.
