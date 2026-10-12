# Loop-speed audit: Phaser web demo (`web/`)

Measured 2026-10-12 on `main` @ d67e31d with `qa/loop-speed.mjs`. Setup: Playwright, real touch taps, 430×932 portrait,
Canvas renderer, desktop-class CPU, and taps fired the moment the target is visible. Three runs; spread was under 10%.
These are **system latency only**. A real player adds roughly 0.5–1.5 s of reading/reaction per decision screen.

Targets (Trevor): **< 2 s from results to relaunch**, **10–30 s from a failure back to the point of failure**.

| Loop | Taps | System time | Verdict |
|---|---|---|---|
| First open → menu ready | 0 | 1.24–1.45 s | OK on desktop. Still needs an iPhone 12-class check (G7: cold start < 3 s) |
| Menu → liftoff (LAUNCH, then tap playfield) | 2 | 0.70–0.78 s | OK. Launch confirm → liftoff is 0.32 s by design |
| **First open → first liftoff** | **2** | **~2.0–2.1 s** | Good |
| Ascent RUD → summary visible | 0 | 50–65 ms | Instant |
| **Failure summary → liftoff** | **3** (CONTINUE → LAUNCH → playfield) | 0.70–0.72 s | Meets 10–30 s only because early failures are near the pad. Every retry goes through the menu |
| Haven contact → summary | 0 | salvage/splash ≈ 3.1 s (1.3–1.4 + 0.9 + 0.7 s); recovered ≈ 4.0 s (2.4 + 0.9 + 0.7 s) | Longest fixed wait in the loop, not skippable |
| **Results → relaunch (NEXT · KH-n+1, then tap playfield)** | **2** | 0.44–0.46 s | Meets < 2 s (~1.5 s with a human tap) |

## Friction points (by impact)

1. **No "Retry flight" on the failure summary.** After an ascent RUD the summary shows `Retry landing`, `Retry from sep`
   and `CONTINUE`. The two retry buttons go to stages the player never reached. A same-flight restart costs
   CONTINUE → menu → LAUNCH → tap (3 taps plus a menu screen). *Fix:* for `rud` before MECO, make the primary button
   `RETRY KH-n` (`restartMission()`), and show retry-sep/retry-landing only once that stage was reached
   (`session.fired.meco` / Haven entered).
2. **Haven contact → summary is a 3.1–4.0 s dead wait.** The `finishUpper` chain plays SECO/deploy beats. *Fix:* allow a tap
   to skip after 0.8 s, or shorten the 900/700 ms steps after the first few flights.
3. **The summary is a text wall on phone.** About 10 lines (headline, score, WHY, medal, SINK/OFF/TILT, ascent, Haven, PB)
   sit before the buttons. Pin NEXT/RETRY in the thumb zone.
4. **SOUND/PAUSE pills overlap the `SUMMARY` phase chip** on phone and 6.9" (found by the QA harness). It is visible on every results screen.
5. **Mid-flight restart is PAUSE → RESTART (2 taps).** Acceptable as is.
6. **"Tap continue or press Space" shows on touch devices.** Drop "press Space" when the pointer is coarse.

## Proposed G7 loop targets

- Results → liftoff: ≤ 2 taps, ≤ 0.6 s system (met).
- Failure → liftoff: ≤ 2 taps (now 3), ≤ 0.6 s system.
- Contact → summary: ≤ 1.5 s and skippable (now 3.1–4.0 s).
- `qa/loop-speed.mjs` is the regression probe. It needs `playwright` (`cd qa && npm i playwright`).
