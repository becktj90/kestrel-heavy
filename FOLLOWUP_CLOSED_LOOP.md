# Follow-up (draft, not filed): closed-loop insertion guidance for the upper stage

Status: draft for Kestrel Physics. Do not open from #313. Depends on #313 (energy cutoff, shared director,
parking-orbit departure check).

## Problem

#313 cuts the engines at the target's specific orbital energy, located inside the step. Energy fixes the
semi-major axis, not the shape. The flight-path angle (γ) at cutoff sets the eccentricity, and nothing in the
current guidance drives γ to 0 at that instant:

- Stage 1 flies an open-loop pitch program (kick, then `turnShape` or a gravity turn). Its loft is fixed before launch.
- Upper stages run PEG-lite (`GuidanceProfile.PegLitePitch`): vr_cmd = clamp((h_target − h)/40, −150, 250),
  sin p = (g + 0.03(vr_cmd − vr))/a_T clamped to [−0.6, 0.95]. It tracks altitude, not γ at a predicted cutoff time,
  so the energy cutoff often comes while vr is still up to +250 m/s (γ ≈ +1.8°, e.g. 411 × −11 km for a 200 km
  parking target) or after an overshoot (scout before retune: 443 × 357 km at γ −0.11°).
- At low upper-stage T/W, PEG-lite holds altitude by pointing up to sin p = 0.95 and loses 2–3 km/s in steering
  loss. The m7 winner lost 2,116 m/s to steering on the way to a 200 km parking orbit (dump in GOLDEN_DIFF.md).

What it costs today:

- Every ellipse and C3 design needed a much larger rocket to clear the honest departure check (Physics A), and the
  open-loop parking orbit only lands in the ±5% perigee band by retuning kick and turn per design.
- finch-leo400-k3 seed 2 and pantry-leo400-k3 seed 3 miss after a seeded engine-out: the open-loop loft no
  longer matches the slower vehicle.
- DesignerRepair can report FullyFixed on rockets that miss with no failure (see the Fix-it findings).
- Presets: scout and kid needed hand-tuned kick, pitch-kick altitude, g limit and throttle floor to land in the band.

## Proposal

Linear-tangent steering on the last burning stage, which drives γ → 0 exactly at the energy cutoff:

1. On upper-stage ignition (and every N s, e.g. 2 s), solve for the burn time t_go to the target energy from the
   rocket equation on the current stage (and later stages), with the actual throttle (max-Q cap, g limit, engine-out scale).
2. Steer tan p(t) = tan p0 − k·t (linear tangent), with p0 and k chosen so that at t_go the radial velocity is 0 and
   the altitude is the aim altitude (parking perigee, or the circular target). This is the standard two-point
   problem; a 2×2 Newton iteration on (p0, k) with the current RK4 propagator is enough at this fidelity.
3. Keep the energy cutoff as the engine-off event (ruling 1). Guidance only shapes the path so γ = 0 there.
4. Same director for batch, designer predict, timeline and live pad (option (b)): the steering law lives in
   GuidanceProfile and is called from Headless.Deriv, so batch/live parity tests keep holding.
5. Re-plan after an engine-out or fuel-imbalance throttle so the remaining burn still ends at γ = 0. This should
   recover the finch/pantry engine-out misses.

## Acceptance

- Every circular golden, viable, contract and starter cuts off with |γ| < 0.05° and |apo − peri| < 2 km on seeds 1–5.
- Parking orbits for ellipse and C3 missions within 1 km of the aim perigee; departure margins reported again in GOLDEN_DIFF.md.
- m7 steering loss under 400 m/s; the Physics A margins table regenerated.
- finch-leo400-k3 seed 2 and pantry-leo400-k3 seed 3 pass with the engine-out.
- Scout and kid presets reach their target on predict without per-preset pitch-kick altitude hacks.
- Batch (0.2 s) vs live (1/120 s) parity tolerances unchanged (MissionFlightSetup.Orbit*Tolerance).
- Deterministic: no new randomness; vectors regenerated with KESTREL_WRITE_PREDICT=1.

## Out of scope

- Plane changes, launch azimuth or inclination targeting.
- Finite-burn modelling of the departure burn itself (Physics A keeps the 1.5% loss, scaled up below T/W 0.5).
