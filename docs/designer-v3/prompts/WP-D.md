# Kestrel Heavy: Designer v3 WP-D: Aero, guidance, recovery

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
§2.4 (separation), §2.5, §2.7, §2.8

## You own
new `unity/Assets/Kestrel.Core/Physics/AeroModel.cs`, new `Flight/GuidanceProfile.cs`, new `Flight/RecoveryModel.cs`, `Vehicle/Stack.cs`, `Vehicle/InterstageAero.cs`, `Designer/Balance.cs`, `data/fins.json`, `data/fairings.json`, `data/interstages.json`, `data/landing.json`, `data/steer.json`, new `Kestrel.Core.Tests/AeroGuidanceRecoveryTests.cs`

## Task
Implement the following:
- **AeroModel:** the Cd0 build-up from catalogs and the nose table, plus Barrowman fin CN_α.
- **Balance:** stability S(t) as the CG follows propellant drain, and CAR at α = 4°.
- **GuidanceProfile:** a steering law (pitch kick, turnShape, max-Q throttle bucket, g-limit, PEG-lite insertion), exposed as a pure function `Command(state) → (pitchRad, throttle)`.
- **RecoveryModel:** landing Δv need, RTLS/barge boostback Δv, v_leg, and the separation v_rel check.
- **Stack.BaseCd:** uses AeroModel only when v3 aero fields are present.

Tests must cover GV9 stability sign, the guidance determinism, and v2 identity.

## Done when
- Unity CI and all required checks are green on your branch, rebased on main.
- There are new tests with the formulas from the spec, and the existing goldens are unchanged.
- A PR titled `designer-v3(WP-D): Aero, guidance, recovery` is opened as DRAFT. The body lists the files touched, the test results, the spec traceability IDs covered (§10) and the follow-ups.
- Then STOP. Do not merge and do not enable auto-merge.
