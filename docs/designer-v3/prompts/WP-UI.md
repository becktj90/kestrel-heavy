# Kestrel Heavy: Designer v3 WP-UI: Portrait designer v3 UI + tutorial

You are a Cursor cloud agent on becktj90/kestrel-heavy (Unity 6.3 LTS, C#, portrait-only iOS game).

## Hard rules (read first)
- Squash-merge the PR as soon as required checks are green, then delete the branch. If checks are still running, enable auto-merge (squash, delete branch). Do not leave a green PR open. Decision #66.
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
§5, §6, Experience layers

## You own
new `unity/Assets/Kestrel.Slice/Runtime/DesignerV3/**`, new `unity/Assets/UI Toolkit/DesignerV3/**`, new `Kestrel.Game.Tests/DesignerV3UiTests.cs`. Extend the G3 HangarPanel by composition. Do NOT edit `Kestrel.Slice/Runtime/Hangar/*`.

## Task
Prerequisites: #176/#180 and WP E are on main.

Build the following:
- **Tab bar:** Propellant · Engines · Tanks · Stages · Aero · Structure · Guidance · Recovery · Payload · Look.
- **Views:** a Simple view and an Engineer view, gated by Experience.cs.
- **Delta chips:** live chips on every control, computed through Readout.Quick on a 300 ms debounce.
- **Sticky strip:** Δv/target bar, TWR, max-Q gauge, cost/budget and risk chips (tapping one jumps to its field).
- **Red-risk launch:** "Launch anyway" confirm.
- **Tutorial:** the guided 200 km LEO tutorial.
- **Layout:** fit iPhone SE through iPad portrait with safe areas, 44 pt targets, VoiceOver labels and Reduce Motion.

Put PlayMode capture shots (SE, Pro Max, iPad) in the PR body.

## Done when
- Unity CI and all required checks are green on your branch, rebased on main.
- There are new tests with the formulas from the spec, and the existing goldens are unchanged.
- A PR titled `designer-v3(WP-UI): Portrait designer v3 UI + tutorial` is opened as DRAFT. The body lists the files touched, the test results, the spec traceability IDs covered (§10) and the follow-ups.
- Then squash-merge when those checks are green (enable auto-merge if they are still running) and delete the branch.
