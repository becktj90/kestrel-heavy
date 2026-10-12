# Pad engineering rules

_Status: describes main @ 4685247 plus planned rules. Owner: validation lane. A rule is **Live** only when code and a test enforce it._

Source of truth for live rules: `Kestrel.Core/Timeline/PadMission.cs` (`Pad.For`). Pad hardware is the only hardware the timeline generator may mention (docs/MISSION_EVENTS.md §2). Sites are fictional, there is no real range or agency insignia (LICENSES.md), and the app is portrait-only.

## Live rules (deterministic, from the design)

| # | Rule | Why (physics / ops) |
|---|---|---|
| P1 | First-stage diameter ≥ 3 m ⇒ **tower pad** (`pad.tower`), else **rail pad** (`pad.rail`). | Small vehicles launch off a rail; larger ones need a tower for access and umbilicals. |
| P2 | **Strongback** only when the vehicle is liquid **and** on a tower. | A strongback carries propellant and electrical lines to a liquid stack. A solid has nothing to load. |
| P3 | **Umbilical** iff any stage or strap-on is liquid (storability ≠ solid). | Liquids need loading and top-off up to the final minutes of the count. |
| P4 | **Deluge** iff liquid **or** tower. | Sound suppression and flame-trench cooling scale with pad structure and engine type. A small solid on a rail gets none. |
| P5 | **Crew arms** iff crewed payload **or** tower. | Access for crew or close-out work. |
| P6 | **Internal power** is always available. | Every vehicle switches to internal power before liftoff. |
| P7 | Weather holds: wind > limit (default 12 m/s) ⇒ hold, Standard only. KID and Relaxed **never** hold (DECISIONS #5). | Gameplay contract, enforced by `tests/validation/weather-climatology.test.cjs` (PR B). |

## Planned (not on main, needs a PR with goldens)

| # | Rule | Gate |
|---|---|---|
| P8 | Seeded pad generator (G2) may vary cosmetic pad detail but **must not** break P1–P6. | Golden vectors. G-series build work is paused. |
| P9 | Launch commit criteria (R5/X4) emit `held` / `delayed` / `scrub` through the existing `MissionEvent` bus, with no new schema (DECISIONS #30). | See ACCURACY_REVIEW.md §X4. |
| P10 | Launch latitude comes from `Mission.latitude`, then `Registry.LaunchLatitude`, then `Stack.LAUNCH_LATITUDE`. Azimuth and inclination limits follow `cos i ≥ cos φ` (inclination ≥ latitude without a dogleg). | Physics review. |

## Honesty rules
- A pad item appears in narration or UI only if `Pad.For` produced it.
- No real pad names or launch-complex numbers.
- Any rule change that alters timeline output regenerates `tests/vectors/timeline` and needs QA review.
