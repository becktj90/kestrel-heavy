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

## Change rules
- Any threshold change regenerates `data/golden/designer-validate.json` (`npm run gen:golden`) and needs Physics ε review (docs/PHYSICS.md).
- The JS and C# implementations change in the same PR, or the golden harness fails.
- Player-facing messages stay plain and non-blaming ("Try …"), per WRITING_BIBLE.md.
