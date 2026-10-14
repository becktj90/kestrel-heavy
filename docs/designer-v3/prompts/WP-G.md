# Kestrel Heavy: Designer v3 WP-G: Missions, economy, campaign

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
§4, §8 GV11

## You own
new `data/campaign.json`, `data/mission-ramp.json`, `unity/Assets/Kestrel.Core/Flight/MissionRamp.cs`, new `Flight/MissionScore.cs`, new `Economy/Career.cs`, `Economy/CostModel.cs`, `Flight/FlightBatch.cs`, new `data/golden/campaign-viable.json`, new `Kestrel.Core.Tests/CampaignTests.cs`

## Task
Prerequisite: WP E is on main.

Build the following:
- **Campaign:** the 10-mission campaign in §4.
- **Scoring:** pass/fail and stars, with the exact score formula.
- **Career:** the bank, with the reuse refund (70% minus refurbishment).
- **Unlocks:** in mission order.
- **No dark patterns:** no timers, no energy, no ads.
- **Viability:** for EVERY mission, author at least 3 distinct designs that pass under budget in FlightBatch, with random failures over 5 seeds. Store them in campaign-viable.json and add a CI test.

If a mission cannot reach 3 viable designs, tune the budget or target and record why in the PR.

## Done when
- Unity CI and all required checks are green on your branch, rebased on main.
- There are new tests with the formulas from the spec, and the existing goldens are unchanged.
- A PR titled `designer-v3(WP-G): Missions, economy, campaign` is opened as DRAFT. The body lists the files touched, the test results, the spec traceability IDs covered (§10) and the follow-ups.
- Then squash-merge when those checks are green (enable auto-merge if they are still running) and delete the branch.
