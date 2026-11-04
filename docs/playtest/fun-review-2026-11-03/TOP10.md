# Kestrel Heavy fun review: top 10 problems (2026-11-03)

**Summary.** Kestrel Heavy isn't fun yet. A new player can't get past KH-5, a failed flight can freeze the game, and pressing the buttons makes flights worse than leaving them alone. We combined three reviews of main `30262fe9`: Gameplay Dev's headless sim runs (20 missions × 10 play styles, 702 Starter combinations, landing and nudge-timing sweeps), UX's flow review of the WebGL build, and QA's 15-minute bot play-throughs as a new kid and as an engineer. **Caveat: all of this is headless sim or bot play of the WebGL build in software-rendered Chrome with a mouse. Nothing was checked on a device, with touch, or with sound.**

Sources:
- **Sim** = Gameplay Dev, `FUN-REVIEW.md` and `out/probe1–4.txt` (box only, `/workspace/kh-funreview/`, not in the repo). Live step 1/120 s, briefing wind seed 1.
- **UX** = draft PR #416, `ux-flow-review.md` and `ux-shots/`.
- **QA** = draft PR #417, `bot-playtest-report.md`, `kid/`, `engineer/`, `*/actions.jsonl`. `t=` means seconds into the session.

---

## 1. You can't finish the campaign (P0)
**Problem.** KH-5's Starter default misses its orbit whatever the player does, and missions unlock strictly in order, so a player who keeps the defaults never sees KH-6 to KH-20. In QA's run, KH-3's "Ready" default also broke up at T+165 even though the kid followed both prompts.
**Evidence.**
- Sim: KH-5 default flies 427.3 × 372.7 km against a 400 ± 20 km target. It's one of only two misses shared by every play style (probe1/P1). A +1° nudge at T+45 fixes it (400.7 × 399.3, P4). The gate is `MissionOpen index == Cleared+1`.
- QA: KH-3 breakup after 2:45 of watching, 0 stars: `kid/302`, `kid/465`, `kid/640`, `kid/641`.
**Fix.** Retune KH-5's kick in the Starter data. Add a test that flies every mission's default in that mission's own wind, not only calm air like #399. Check whether KH-3's breakup still happens after #410/#413/#414.
**Owner.** Gameplay Dev (Physics reviews the before/after numbers). **Status.** In progress. #386's earlier kick fix was closed without merging on Oct 29. KH-3 work is in #410/#413/#414.

## 2. Freezes and lost taps after a flight (P0)
**Problem.** After a failed flight, Fix it leaves a blank sky and only a page reload gets out. The first tap on the results screen gets swallowed, and Back to designer can miss a second tap.
**Evidence.**
- QA, 2 of 2: kid t=976.18 → NullReferenceException at 976.28 (`kid/839`, `kid/851`). Engineer t=643.52 → NRE at 643.71 with a giant magenta sphere (`engineer/325`, `engineer/326`). Both logged as `NullReferenceException` in `kid/actions.jsonl` and `engineer/actions.jsonl`.
- UX, 1 of 1: empty sky for 35+ s after a failed KH-3, console `[Count] done->boot` then an NRE (`ux-shots/14-fixit-softlock-empty-sky.png`).
- Swallowed taps: `kid/040`→`041`, `engineer/104`→`105`→`109`.
**Fix.** Fix it always lands in the designer with the failing control highlighted. If no screen is showing 1 s after a route change, fall back to the designer. Results buttons shouldn't move after the first render, and should act on the first tap.
**Owner.** Graphics Dev (soft-lock), UX (taps). **Status.** Local branch `fix/fixit-softlock-webgl`. No PR yet.

## 3. Pressing buttons only makes it worse
**Problem.** Leaving the rocket alone beats playing. Accepting the pitch call holds −1° for the rest of the flight. An early steering nudge breaks the rocket up. There are only 1–2 prompts per flight anyway.
**Evidence.**
- Sim (P1): no input 18/20 passes, auto-assist 18/20, decline-all 18/20. Pressing the green big button on every call: 3/20. Tapping early 2/20, late 3/20. Accepting pitch misses orbit on 15 of 18 orbital missions. On KH-20 the big button is "SHUT DOWN ENGINE (will not hold)" and it loses the vehicle.
- Sim (P4): a ±1° nudge at T+0.5 breaks up 18/20 at max-Q, 14/20 at T+3, 11/20 at T+8. KH-1 breaks up at 0 km.
- QA: 1–2 timed prompts per flight with a ~2.5 s window (`kid/024`).
**Fix.** Make accept a one-shot correction toward the plan. Make "Ride it out" the big button when the design can't survive an engine-out. Hide nudges until about T+10 s or 100 m/s, and have Physics look at the low-speed loads.
**Owner.** Gameplay Dev. **Status.** Queued after #1.

## 4. Too much waiting
**Problem.** Most of a flight is watching. The countdown is long and Skip doesn't skip. You also have to sit through a flight that is clearly doomed.
**Evidence.**
- Sim: flights take 9–15 min real time (about 234 min for all 20), and calls are open only 2–13% of the time. First call at T+12 to T+47 s. `FlightDriver` runs 1:1 with only slow motion, capped at 64 steps per frame.
- QA: ~18 s countdown, Skip count tap at t=133.66 doesn't skip (`engineer/017`, `engineer/023`), then a 30–165 s hands-off climb.
- UX: about 2 min watching a doomed flight, and the coach text wraps to 3 lines (`ux-shots/06-coach-text-wrap.png`).
**Fix.** Run coasts at 4–10× when no call is open and the camera isn't on the booster. Make Skip work. Add ⏩x4 and Back to design once the outcome is certain.
**Owner.** Gameplay Dev and UX. **Status.** Not started.

## 5. No big moment at the end
**Problem.** The flight cuts to results at engine cutoff, so you never see apogee or the trip back down. On the web build a breakup shows nothing, or a magenta sphere.
**Evidence.**
- UX: on KH-2 the card appeared at T+57 s, 26 km, still climbing (`ux-shots/07-report-cuts-in-at-cutoff.png`). From code: `Headless.cs:751`, `FailureHost.cs:143-154`.
- QA: `kid/026`→`027`. Console: "CapsuleCollider/SphereCollider doesn't exist" (kid t≈803.7, engineer t≈614.68), magenta sphere (`engineer/325`, `kid/640`→`641`). UX saw the CapsuleCollider error 8 times.
**Fix.** A 3–5 s warped coast to apogee or orbit insertion with a big callout, skippable with a tap, then the card. Stop WebGL from stripping physics (link.xml) and fix the missing shader.
**Owner.** Graphics Dev. **Status.** Local branch `fix/explosion-magenta`. No PR yet.

## 6. The game contradicts itself
**Problem.** Different screens disagree about whether the rocket will make it.
**Evidence.**
- Sim (P2): "Ready" shows on 270 Starter combinations, but only 52 of 702 pass. One-tap neighbours of a default pass 7/87. On 10 of 20 missions only the default works.
- QA and UX: designer Ready, then the countdown says "89 m/s short" (`kid/013`, `ux-shots/04-ready-then-count-says-short.png`). "4 things to fix" sits next to "539 m/s to spare", or the rocket flies fine anyway (`engineer/008`, `engineer/015`). Designer predicts breakup at T+9 while the countdown says "2,396 m/s to spare" (`engineer/254`, `engineer/296`; it broke at T+9.45). Silver on a doomed flight (`ux-shots/12-doomed-flight-silver-medal.png`).
**Fix.** One verdict source used everywhere. Show Ready only when the predictor's flight actually passes.
**Owner.** Gameplay Dev. **Status.** Not started. #410 touches the "89 m/s short" case.

## 7. A win looks like a loss
**Problem.** The results screen after a pass leads with a red Fix it, never says MISSION COMPLETE, and is full of raw numbers and jargon.
**Evidence.** UX: red Fix it is always added first (`RewardsCard.cs:64-75`), 6 equal buttons, "Bank 3995160.96 Kc", clipped plot labels (`ux-shots/08-pass-report-red-fixit.png`). QA: "Air load 83637.2 Pa past 83600.0 Pa", Fix it on a pass does nothing (`kid/027`, `kid/041`, `kid/044`, `engineer/180`).
**Fix.** A celebratory pass card with stars, "+2.0M Kc", and one green Next mission. No Fix it on a pass.
**Owner.** UX (with #413). **Status.** UX ready to start.

## 8. Medals don't reward skill
**Problem.** Doing nothing earns a medal, failed flights earn Gold, and heat damage costs nothing.
**Evidence.** Sim (P1): no input earns Bronze 20/20. Auto-assist is stuck at Bronze on 8/20 because the max-Q green zone is never reached. Gold on 9 failed flights. Fairing heat damage still passes 18/20.
**Fix.** Set the max-Q window from each rocket's real peak q. No medal on a failure. Heat damage costs grade or a bonus.
**Owner.** Gameplay Dev. **Status.** Queued (third of Gameplay Dev's four).

## 9. Booster landing isn't a challenge
**Problem.** In Simple mode it lands itself perfectly. In Engineer mode the controls are fiddly single taps from throttle 0, and even following the plan fails.
**Evidence.** Sim (P3): Simple lands Perfect 6/6 (0.8 m/s, 0 m miss). Engineer with no input crashes 6/6. Following the plan at 4 taps/s lands 0/6. A burn 0.5 s late gives Salvage 6/6; 0.5 s early gives Perfect 6/6.
**Fix.** One "LIGHT LANDING BURN" timing call that forgives early presses. Engineer buttons repeat while held.
**Owner.** Gameplay Dev and UX. **Status.** Not started. Booster HUD is in #389.

## 10. Same thing every time, and slow to start
**Problem.** The missions all feel alike, the first three need no decisions, and getting into the air takes too long. Some Engineer designer controls seemed to do nothing.
**Evidence.**
- Sim: 20 missions use 4 profiles (2 hops, 9 low orbits, 4 identical transfers, 4 escapes) and the same calls. KH-10 to KH-20 unlock nothing. About 940 credits in total.
- UX: KH-1 to KH-3 open already Ready ✓ (`ux-shots/03-designer-presolved-kh1-kh2-kh3.png`). 4 taps and 20–60 s to first liftoff. Duplicate tab rows in the full designer (`ux-shots/15-full-designer-landscape.png`).
- QA: engine type, + Stage on top and side boosters had no effect, and Undo stayed grey (`engineer/248`, `engineer/330`, `engineer/333`). This may be the bot's mouse input at low fps and needs a device check. "LAUNCH KH-1" opens a briefing first (`kid/003`, `kid/048`).
**Fix.** One twist call per mission (wind shear, crew abort, lunar departure) and unlocks for later missions. One honest gap to fix in each early mission. Fold the brief into the designer. Fix or check the designer controls.
**Owner.** Gameplay Dev and UX. **Status.** Not started.

---

## Order of work
1. **#1 KH-5 retune + live-wind default gate** (Gameplay Dev). Unblocks the campaign.
2. **#2 Fix it soft-lock + WebGL physics stripping** (Graphics Dev), and the first-tap fix (UX).
3. **Cheap big wins:** #3 one-shot accept and nudge gate, #8 medal fixes (Gameplay Dev). #7 pass card (UX).
4. **#6 single verdict source**, then **#4 time warp and working Skip**.
5. **#5 apogee moment**, then **#9 landing burn**.
6. **#10 variety and faster start**: the largest job, last.

Each lands as its own PR, merged when CI is green. None of it counts as device-verified until someone plays it on an iPhone or iPad.
