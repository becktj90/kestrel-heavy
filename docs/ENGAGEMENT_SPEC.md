# Engagement spec (H1): "one more launch", no dark patterns

Goal: players relaunch because they **want to know if the fix works**, never because they're pushed. Premium $0.99, **no IAP**, no ads, no data collected.

## 1. Core loop
**Design → Predict → Launch → Learn → Improve → Relaunch.**
| Budget | Target |
|--------|--------|
| Results → relaunch (same rocket) | **< 2 s**, 1 tap (RELAUNCH is the primary button) |
| Failure → relaunch with a fix | **10–30 s** (tap TWEAK → fix highlighted → LAUNCH) |
| Cinematics on repeat | Full first time; 50% second; skippable/condensed after (per-cue `repeatScale` data) |

- **Quick-tweak:** TWEAK opens the designer on the failing part, highlighted, with one suggested fix ("+1 tank: +410 m/s Δv") as a one-tap apply. Undo always.
- **Predict:** before launch show predicted apoapsis/Δv margin + success probability (headless core, N seeded runs on-device; exact in KID shows a simple ✓/✗ meter).
- **Gap-to-goal debrief:** "Short by 312 m/s Δv ≈ 1,900 kg propellant → add a tank or swap to vacuum engine". Always numbers + one fix.
- **Near-miss clarity:** show how close (e.g. "orbit 94% — periapsis 6 km below atmosphere"), honest, never inflated.
- **Ghost:** translucent last attempt trajectory + telemetry overlay. **A/B flip:** change one parameter, relaunch, side-by-side result.

## 2. Motivation model
Self-determination theory (autonomy: many valid solutions; competence: clear feedback; relatedness: opt-in sharing) + flow (difficulty via assists/layers, never via physics lies).
- **Adaptive advisor** (offline rule-based): offers, never forces; one-tap dismiss; remembers dismissals per topic; silent in Engineer unless asked.
- **Goals at 3 timescales:** session (this rocket), week (2–3 chosen goals), long (mastery ladder).
- **Next-up panel** on results and home: max 3 suggestions, player-dismissable.
- **Mastery ladder:** Hop → Suborbital → Orbit → Recovery → Precision landing → Heavy → Engineer challenges.
- **Personal bests:** per mission and per metric (cost, mass, Δv margin, landing offset, time).
- **Open loops on home:** "Your orbit attempt was 94% — try again?" One line, no badges/counters, no notifications.

## 3. Modes
| Mode | Tier |
|------|------|
| Campaign | 1.0 |
| Sandbox | 1.0 |
| Daily (local seed, same for everyone) | 1.0 |
| Weekly (constraint challenge) | 1.0 |
| Challenge lab (authored puzzles) | 1.0 (12), more 1.1 |
| Contract rush (timed contract string, opt-in) | 1.1 |
| Failure investigation (diagnose a pre-built failed flight) | 1.1 |
Surprises are **authored only** (rare weather, special payloads); no random reward drops.

## 4. Streaks, wellbeing, notifications
- Streaks forgiving: 2 freezes/week auto-applied, 48 h grace, never reset with a loss message; can be **hidden**. Daily can be hidden too.
- Welcome back: "Welcome back — here's where you were." No guilt, no missed-reward list.
- Notifications: **opt-in only**, max 1/week, off and unavailable in KID.
- Healthy play: gentle break suggestion after 45 min (configurable/off); never in a flight.
- **Calm mode:** no streaks, no timers, no daily, softer audio.

## 5. Social (opt-in, Game Center only, no own server)
Multi-metric boards (cost, mass, margin, landing), friends filter, solution histograms ("your mass vs everyone's"), share cards via share sheet. No public chat; design codes sanitized (X5). Off by default in KID.

## 6. Pacing curve (data: `pacing.json`)
| Horizon | Player experience |
|---------|------------------|
| 60 s | Instant launch of a starter rocket; spectacle; first success |
| 10 min | First own design reaches space; first fix-and-relaunch |
| 1 hr | First orbit; Standard layer unlocks; first daily |
| 1 week | Recovery, weekly challenge, 3–5 personal bests |
| Long term | Mastery ladder, Engineer layer, challenge lab, sandbox creativity |

## 7. Ban list (non-negotiable)
Energy/lives/timers to play · loot boxes or random paid rewards · FOMO (expiring rewards, limited-time shops) · streak punishment or loss-framing · confirmshaming · social pressure (public shaming, "friend beat you" pushes) · manipulating children (no notifications, social, or reward-pressure in KID) · IAP · ads · dark-pattern defaults · fake scarcity · inflated near-miss.

### Ethics PR checklist (paste into any engagement PR)
- [ ] Nothing on the ban list. [ ] Opt-in, off in KID where social/notify. [ ] Can be hidden/disabled. [ ] No network, no data leaves device. [ ] Copy has no guilt/shame. [ ] Numbers are honest (from core sim). [ ] Negative-signal metric defined.

## 8. Metrics (local on-device only, never transmitted)
Positive: results→relaunch time, relaunch rate after failure, fix success rate, PB count. **Negative:** rage-quit (exit < 5 s after failure), repeat-fail streak ≥5 on same issue, advisor dismiss rate, sessions > 60 min, streak-hide rate. Viewable in a debug/Settings "Your stats" page; used by playtest kit (H6) via manual export only.
