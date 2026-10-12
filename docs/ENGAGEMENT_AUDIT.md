# Engagement audit (H1): Phaser web build + Unity plan

Refs are to `main` at H1. The Phaser build is web demo only; findings feed the Unity U-plan.

## Restart flow and timings (Phaser)
| Step | Where | Time / taps |
|------|-------|-------------|
| Upper-stage end → endMission | `web/js/mission.js:1824-1833` (finishUpper: 900 ms + 700 ms delayed calls) | ~1.6 s fixed |
| Abort/tip/salvage → finishUpper | `web/js/mission.js:1785`, `:1804`, `:1821` (2400 / 1400 / 1300 ms) | 1.3–2.4 s + 1.6 s |
| Summary sheet CONTINUE → **menu**, not relaunch | `web/js/mission.js:406` → `onPrimary` `web/js/mission.js:916-918` → `returnToMenu`; button `web/index.html:1429` | 1 tap |
| Menu → launch | `onPrimary` `web/js/mission.js:912-913` → `startMission` | 1 tap |
| Retry only from Pause | `web/js/hud.js:244` → `restartMission` `web/js/mission.js:2860` | hidden |
| Next flight | `flyNextMission` `web/js/mission.js:2874` (not on summary) | via menu |
**Result:** summary → relaunch = 2 taps + menu hop, ≈3–5 s with reading. Failure → relaunch ≈6–10 s. Open PR #28 (one-tap retry) addresses part of this in Phaser only.

## Reward moments
- Medals bronze/silver/gold per flight, best kept, `improved` flag: `web/js/medals.js:48-55`; shelf on hangar `web/js/medals.js:64`, `web/js/hud.js:526`.
- Summary medal + breakdown + "why": `web/js/mission.js:2306-2318` (setSummaryWhy/Medal/Copy/Breakdown).
- Loadout unlocks on recovery: `web/js/loadout.js:170-182`.
- Liftoff / MECO / SEP callouts: `web/js/mission.js:966`, `:1020`, `:1605`.
- Pickups (shield, LOX, core kick): `web/js/mission.js:2016-2039` — arcade, not designer-loop rewards.

## Friction points
1. No direct RELAUNCH or TWEAK on summary (`web/js/mission.js:916-918`).
2. Fixed cinematic delays never shorten on repeat (`web/js/mission.js:973-989`, `:1098-1120`).
3. Failure copy is generic: `web/js/config.js:225` ("Read the fail banner, then retry"). No gap-to-goal numbers.
4. No prediction before launch; no ghost of the last attempt.
5. Best tracking is medal-only (`settings.missionBests`, `web/js/config.js:249` `bestFlight: null`); no per-metric personal bests.
6. One mode (linear KH-n flights); no daily, sandbox or challenge.
7. Good: storage is local only (`web/js/storage.js:95-112`), no analytics, no IAP, no nagging.

## Unity plan (docs/UNITY_SHIP_PLAN.md)
- U5 has a post-flight report + plot, but no debrief deltas, ghost or predict.
- U8 has campaign/tutorial; no daily/weekly, goals or Next-up.
- No restart-time budget in any U acceptance gate. **Fix:** H2–H6 below, mapped into U3–U9.
