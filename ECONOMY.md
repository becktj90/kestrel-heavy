# Kestrel Heavy economy (E1: cost model)

Status: **E1**. In 2.0 the economy is a **cost readout only** (Sandbox). Contracts, ledger, funds, risk and bailout arrive in E3–E5 (1.1).

## Currency
- **Kc** (kilocredits): fictional in-game money. Never sold, never linked to StoreKit or any real-money purchase. Never shown as "$".
- Stored as **integer minor units**: 1 Kc = 1,000 credits = 100,000 minor. All constants in `web/js/economy-data/constants.json` are minor units.
- Each line item is rounded once (`Math.round`), then only integers are added, so breakdown, totals and category sums agree exactly.
- Display (`formatKc`): `850 Kc`, `34.1k Kc`, `1.23M Kc`. Cost per kg: `x.xx Kc/kg`.

## Files
| File | Role |
|---|---|
| `web/js/economy/costModel.js` | `priceDesign(design, companyState)`, `engineDevCost`, `engineBatchCost`, `learningFactor`, `tankArea`, `marginalHints`, `formatKc` |
| `web/js/economy/reliability.js` | `reliabilityOf(design)`, `clusterReliability`, `engineOutTolerance` |
| `web/js/economy-data/constants.json` | Source of truth for constants (schema: `schemas/constants.schema.json`) |
| `web/js/economy-data/constants.js` | Generated mirror for the browser: `node tools/gen-economy-data.mjs` (test fails on drift) |
| `web/js/ui/finance/costPanel.js` | Funds bar, stacked bar, sortable breakdown, marginal hints in the DESIGN readout |

## Development vs per-launch
`priceDesign` returns `{ recurring, dev, total, perKg, lines[], devLines[], byCategory }`.
- **dev** (one-time): engine development per engine *type*, avionics qualification. Skipped for ids in `companyState.devDone`.
- **recurring** (per launch): everything below. **Cost per kg uses recurring only.**
- `companyState.unitsBuilt[engineId]` continues the learning curve across a campaign (E3+).

## Formulas
F = max(thrustVac, thrustSL) / 1 MN, pc in bar, C = `cycleMultiplier[cycle]`, TRL from `engine.trl` (default 7).

| Item | Formula | Constants |
|---|---|---|
| Engine dev | `kDev · F^0.6 · C · (pc/100)^0.35 · (1 + 0.25·(9 − TRL))` | kDev = 18,000 Kc |
| Engine unit 1 | `T1 = kUnit · F^0.7 · C · (pc/100)^0.25` | kUnit = 1,100 Kc |
| Learning | unit n costs `T1 · n^log2(0.85)`; a batch of k after b built = Σ_{i=1..k} round(T1·(b+i)^log2 0.85) | L = 0.85 |
| Cycle multiplier | pressure-fed 0.5, solid 0.6, hybrid 0.7, gas-generator 1.0, expander 1.6, staged 2.2, full-flow 2.8 | |
| Tanks & structure | `(m_struct · c_kg + A_tank · c_m2) · (1 + 0.35·[deep-cryo]) · (SF/1.4)^0.5`; m_struct = `structureMass` + interstage + separation mass | see below |
| Tank area | cylinder: `V = m_prop/ρ_bulk`, `L = V/(π d²/4)`, `A = π d L + π d²/2` | |
| Material c_kg / c_m2 | steel 8 / 1,500 cr; Al-Li 60 / 4,000 cr; composite 300 / 10,000 cr (cr = credits) | |
| Fairing | `m_fairing · c_kg + 0.75·π·d·L · c_m2` | |
| Separation systems | 100 Kc per separation event | |
| Propellant | `(m_prop + m_landing) · c_prop · (1 + boiloff/h · 6 h)` | kerolox 0.30, methalox 0.25, hydrolox 1.2, UDMH/NTO 6, MMH/NTO 8, solid 5, hybrid 3 cr/kg |
| Avionics | unit · (1 + 0.25·(stages − 1)); dev once per tier | basic 0 / 500 Kc, standard 1,500 / 2,000 Kc, redundant 4,000 / 5,000 Kc (dev / unit) |
| Recovery hardware | legs 800, grid fins 400, RCS 300 Kc | |
| Range fee | 1,500 Kc per launch | |
| Crew & standby | 120 Kc · (3 + stages) days | |
| Integration | 300 Kc · (stages + strap-ons) | |
| Transport | 500 Kc, ×1.8 if any diameter > 5.2 m | |
Strap-ons are priced as one group × count (engines learn across all of them).

Monotone by construction (non-negative exponents/multipliers, rounding is monotone): more thrust, pc, engines, propellant or strap-ons never cost less. Enforced in `tests/economy-cost.test.cjs`.

## Reliability (engine-out)
Per engine p = 1 − r (catalog `reliability`). A fraction **0.3** of failures is catastrophic (vehicle lost); the rest are benign shutdowns.
- Tolerance k: 0 for solids or clusters < 5 engines, else `max(1, floor(n/9))` (9 → 1, 27/33 → 3).
- Cluster success = `Σ_{j=0..k} C(n,j) · (0.7 p)^j · r^(n−j)`. Strap-on groups: per-booster value ^ count.
- Vehicle = avionics tier (0.985 / 0.995 / 0.999) × Π clusters × 0.998^separations × safety factor (×0.97 if SF < 1.25, ×1.002 if SF ≥ 1.5), capped at 0.9999.
- Simplification: tolerance does not yet check remaining thrust/TWR or burn extension (needs M3 sim).

## Marginal-cost hints
`marginalHints` re-prices +1 engine and +10 % propellant per stage, and −2 strap-ons; the four largest deltas show in the panel.

## Bus events (N1 bus, category `economy`)
`econ.quote` (emitted by the DESIGN panel when price changes) and `econ.purchase` (reserved for E3). See `docs/FLIGHT_EVENTS.md`.

## Decisions recorded (Trevor)
- **Failure: MODERATE** (E4/E5): hardware is lost, pad damage 10–40 % of pad value (by failure altitude), reputation dip, **15 %** of lost hardware back as lessons-learned R&D credit, bailout **always** available (no soft-lock). In `constants.json → failure`; not used by E1 code.
- **Difficulty:** Standard default; Relaxed suggested for KID (budget ×1.4), Hardcore ×0.8.
- **Currency:** fictional Kc, never "$".
- **2.0:** cost readout only. **App:** portrait-only.

## Not in E1
Pad cost (E2, needs launch-complex requirements), testing/R&D/insurance (E4), reuse/refurb (E5), econsim balance (E6).
