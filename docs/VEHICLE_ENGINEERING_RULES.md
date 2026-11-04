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

## Fairing joint and flare (spec, DECISIONS #71)
_Status: **spec, not Live.** The render and the advisory need code plus a golden in the same PR (JS and C# together). Nothing below changes a shipped design's mass, Cd, payload fit or flight._

**Geometry**
- The fairing's **joint diameter is the top stage's diameter** (D_s). It is never clamped to the stage: the fairing may flare **larger** than D_s above the joint, and a fairing narrower than the stage tapers in. The barrel keeps today's meaning: `fairing.diameter` (D_f) is the widest part.
- **Cargo is ≤ 0.9 × D_f** and the payload volume formula is unchanged (`PayloadFit`: radius 0.45 D_f, length factor 0.8).
- A **conical shoulder** joins the joint to the barrel. Its length is `L_sh = min(ΔR / tan 15°, 0.5 × cylinder length)`, with `ΔR = |D_f − D_s| / 2` and cylinder length `= fairing.length × (1 − nose fraction)`. The shoulder is **carved out of the fairing's own cylinder**, so total fairing length, stack height, mass, CG and payload volume do not change. The half-angle is `θ = atan(ΔR / L_sh)`, which is 15° unless the 0.5 limit bites.
- The shoulder belongs to the fairing. It jettisons with the halves, which hinge at radius D_s/2 at the joint, not at the barrel radius.
- Why 15°: low-speed conical-boattail tests found attached flow at 10° and the lowest total afterbody drag at 14°, with a separation bubble at 14° and 20° (Tran et al., Trans. JSASS 62(4), 2019). Real fairings flare too: the Atlas V 5-m fairing is 5.4 m on a 3.81 m booster and has a fixed composite boattail down to the Centaur interstage adapter (Atlas V Launch Services User's Guide, 2010). A Falcon 9 fairing is 5.2 m on a 3.66 m stage. Both ratios are about 1.42.

**Cd shoulder term (flare, D_f > D_s).** `ΔCd = 0.08 × (min(r, 1.5) − 1) × f(θ)`, with `r = D_f / D_s`, `f = 1` for θ ≤ 20°, then `f = 1 + (θ − 20°) / 20°`, capped at 1.5 (θ ≥ 30°).
- The 0.08 slope is the term already in `Stack.BaseCd` and `stack.js`, so a flare at 20° or less is **byte-identical to today**.
- Cap on size: `r` stops counting at 1.5, so ΔCd ≤ 0.04 at θ ≤ 20°. The cap on angle gives an absolute maximum of 0.06.
- Cross-check, not a fit: Tran's afterbody drag coefficients are 0.024 to 0.037 for 10° to 20° (referenced to the body's area). Our flat 0.08 slope gives 0.020 at r = 1.25 and 0.032 at r = 1.4. A flat aft-facing ledge is the limit as θ grows. At r = 1.5 the annulus is 56 % of the body area, so a blunt-base Cd of about 0.12 on it gives about 0.07, just above the 0.06 cap. (The 0.12 is from memory of Hoerner's *Fluid-Dynamic Drag*; check it before it is cited elsewhere.)
- Limitation: the term does not depend on Mach. Transonic boattail drag peaks and its best angle is much smaller than 15°, so treat it as a subsonic and low-supersonic estimate. ESDU 00026 is the reference for supersonic conical boat-tails (L/D ≤ 2.5, M 1.5 to 4).

**Cd taper-in term (D_f < D_s), phase 2.** `ΔCd = 2 sin²θ × (1 − (D_f / D_s)²)`, the Newtonian pressure on a forward-facing cone frustum, referenced to the stage area (the largest area). Today this case adds 0. Only the m20 starter and winner have D_f < D_s (3.6 m on 4.1 m, θ = 16.4°, ΔCd ≈ +0.037). Because it moves flights, it ships with the #335 re-fly, not before.

**Advisory (warning, never a block).** New code `fairing-flare` fires when `D_f / D_s > 1.5` **or** `θ > 30°`. Real flares sit at about 1.42, so 1.5 leaves 5 % margin. Above 30° the shoulder is a ledge, not a cone. Copy, plain and non-blaming: "This fairing flares a lot above the stage. Try a wider stage or a smaller fairing." `fairing-narrow` (D_f < 0.5 D_s) stays a block.

**Honest gaps**
- Barrowman CP still treats the shoulder as **zero length**. Giving it the real length L_sh moves the shoulder's lift point up by about 0.47 L_sh (for r ≈ 1.4), which I estimate shifts the stack's CP by up to about 0.2 cal on the stock rocket and `tpl-heavy`. That is an estimate from the formula, not a measured number. Land it in the same golden regen as #335 and quantify it first.
- Payload volume does not subtract the shoulder. The 0.8 length factor already allows for non-cylindrical sections, so it is unchanged until someone shows a design where it matters.
- Mass is slightly conservative: the barrel's areal density is applied to the whole length, including the shoulder, whose frustum area is a little smaller.

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
