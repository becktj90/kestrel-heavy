# Vehicle engineering rules

_Status: describes main @ 4685247. Source of truth: `Kestrel.Core/Designer/Validate.cs` (port of `web/js/designer/validate.js`), with goldens in `data/golden/designer-validate.json`. A rule here is **Live** only when code and a golden enforce it._

## Blocks (design cannot fly)

| Code | Rule | Physics |
|---|---|---|
| `zero-engines` | Every stage (and strap-on set) needs ≥ 1 engine. | No thrust means no Δv. |
| `zero-propellant` | Every stage needs propellant mass > 0. | Rocket equation: Δv = Isp·g0·ln(m0/mf) = 0. |
| `schema` | Design JSON must be well formed. | n/a |
| `math` | Readout must compute. | n/a |
| `twr-low` | Liftoff TWR must be **> 1.0** (`MIN_TWR`). | Thrust must exceed weight to leave the pad. |
| `engines-dont-fit` | Engine cluster must fit under the stage diameter. | Geometry (packing factor `PACK` = 1.1). |
| `top-heavy` | An upper stage may not be ≥ 1.6× the diameter of the stage below. | Structural and aero load-path heuristic. |
| `fairing-narrow` | Fairing ≥ half the top-stage diameter, and wide enough for the cargo (cargo ≤ 0.9 × the fairing diameter, `payload-doesnt-fit`). | Geometry. |
| `payload-doesnt-fit` | Payload volume ≤ fairing volume. The volume scales as (m/m_ref)^(2/3), capped at 1.5×. | Constant-density scaling. |

## Warnings (flies, but badly)

| Code | Rule |
|---|---|
| `twr-marginal` | 1.0 < liftoff TWR < 1.25 (`GOOD_TWR`): large gravity losses. |
| `weak-upper` | Upper-stage vacuum TWR < 0.5. |
| `weak-upper-dv` | Top stage contributes little Δv. |
| `dv-margin` | Δv margin below `MIN_MARGIN` = 5 % (or negative). |
| `landing-hardware` | Landing reserve without legs or grid fins. |
| `heavy-fairing` | Fairing mass > payload mass. |
| `fairing-flare` | Barrel diameter / top-stage diameter > 1.5, or the shoulder half angle > 30° (`data/designer-limits.json` `fairingFlare`). Advice only, never blocks. | Render and advice; the sim is untouched. |

## Physics constraints that are NOT rules yet (honest gaps)
- **Gravity is point-mass.** There is no J2 (S3). Orbit predictions ignore nodal regression, which is about −5°/day for a 420 km, 51.6° orbit. Reference checks live in `tests/validation/atmosphere-j2-decay.test.cjs`.
- **Atmosphere** follows USSA76 within 1e-4 from 0 to 86 km (validated). Above 86 km it uses an approximate tail: +11 % density at 100 km, and no thermosphere. There is **no orbital decay**, so a "stable orbit" verdict has no lifetime yet.
- No max-Q, structural-load, or heating limits gate a design today. `maxQ` is reported only (docs/PHYSICS.md).

## Guidance computer exception (#341, KH-13)
- The guidance computer (#341) is an optional upper-stage part: 25 kg, 30 credits, unlocked after mission 10, flag `designer.guidanceComputer`. A design without it flies exactly as before.
- **KH-13 (starter m13) is the one starter that ships with it on.** Reason, as decided by Trevor and relayed by Kestrel Arcade (DECISIONS #70): KH-13 must land its booster and still hold a 550 x 550 km orbit. The computer is what plans the engine cutoff for a near-circular orbit, where basic guidance only chases the right height (`data/guidance.json`, `basic.label`). Landing reserve, stage-2 propellant and the 3 degree pitch kick were retuned in #367 so the booster return closes with spare propellant and max-Q stays under its limit.
- **Why it is an exception and not the rule:** a starter that needs an optional paid part teaches nothing about choosing that part. No other starter may turn it on without a new DECISIONS entry, and GuidanceComputerTests fails if one does.
- **Physics conditions that stay true.** The computer's mass is counted in the stack, so Δv and TargetMet already include it. The game has no navigation or sensor error, so its insertion is better than a real rocket's. Real upper stages land within a few km of target (data/guidance.json, `closedLoop.realWorld`). Do not tighten the TargetMet band (±5 %) because of it.
- **Changing it:** moving the exception to another starter, or removing it, changes goldens and needs a DECISIONS entry plus a Physics old to new list.

## Change rules
- Any threshold change regenerates `data/golden/designer-validate.json` (`npm run gen:golden`) and needs Physics ε review (docs/PHYSICS.md).
- The JS and C# implementations change in the same PR, or the golden harness fails.
- Player-facing messages stay plain and non-blaming ("Try …"), per WRITING_BIBLE.md.
