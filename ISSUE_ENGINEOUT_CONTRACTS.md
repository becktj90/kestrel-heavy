# Engine-out contracts: viable designs that miss on their seeded stage-1 engine-out

Draft for the engine-out balance PR (from #313). Not filed.

## What happens

`contracts-viable.json` rows must pass `FlightBatch.Viable` on all five campaign seeds. That is what "viable"
means, and there is no expected-miss list. Under #313's closed-loop director and the honest cutoff, two k3 LEO-400
contract rows each lose one stage-1 engine on one seed. That seed then misses the 400 km circular band (380–420 km)
with no failure event. The rocket cuts off at the target energy at a bad flight-path angle (γ). The open-loop pitch
program (kick 7) cannot steer out of a thrust change: removing thrust drops the orbit's periapsis, and adding thrust
lofts it.

Arcade (12:14 PM) asked for a re-search: add an engine or propellant margin, at most 2 cost steps from the current
design, keep the row if a design passes all five seeds, otherwise leave the row failing and visible.

The steps tried were: S1 +1 engine, S2 +1 engine, S1 propellant +10%, and S2 propellant +10%. Any one or two were
combined, for 14 variants per row. Each variant was checked with `FlightBatch.Viable` on seeds 1–5.

## finch-leo400-k3: fixed in #313

| Design | Cost | Seeds 1–5 (apo × peri km) | Viable |
|---|---|---|---|
| finch-leo400-k3-c9-420-k7-u38 (was) | 26235 | 1 P 400.4×399.6, **2 miss 657.8×142.2**, 3–5 P 400.4×399.6 | no |
| **finch-leo400-k3-c10-462-k7-u38** (now) | **27495** (+1260) | all five P 400.0×400.0 | **yes** |

The fix is S1 9 → 10 engines and S1 propellant 420000 → 462000 kg (2 steps). Both the old and the new design carry
the same `payload-doesnt-fit` advisory. It predates #313 and does not affect `Viable`.

The other 13 variants fail as follows:
- 10 miss on every seed, most of them lofting high (566–706 km apoapsis); one of these also breaks up on seed 3.
- 1 breaks up on every seed: S1 propellant +20% gives structural-maxq.
- 2 run dry: S1 and S2 propellant both +10%, and S2 propellant +20%.

## pantry-leo400-k3: still failing, capped at 2 steps

Base: **pantry-leo400-k3-c8-360-k7-u35**, cost 24559. S1 has 8 engines and 360000 kg of propellant; S2 has 2
engines and 35000 kg. Seeds 1, 2, 4 and 5 pass at 400.3×399.7 km. **Seed 3 misses at 637.7×162.3 km** (the
stage-1 engine-out).

| Variant | Steps | Cost | Seeds 1–5 |
|---|---|---|---|
| S1 +1 engine | 1 | 25180 | all miss 605.4×194.6 |
| S1 +2 engines | 2 | 25787 | all miss 689.5×−1210.2 (seed 3 524.8×−409.4) |
| S1 +1, S2 +1 engine | 2 | 25417 | all miss (504.8×295.2; 585.6×214.4) |
| S1 +1 engine, S1 prop +10% | 2 | 25740 | all miss 535.4×264.6 |
| S1 +1 engine, S2 prop +10% | 2 | 25330 | all miss 573.5×226.4 |
| S2 +1 engine | 1 | 24795 | all miss 548.2×251.8 |
| S2 +2 engines | 2 | 24998 | all miss (625.3×174.7; seed 2 755.7×44.3) |
| S2 +1 engine, S1 prop +10% | 2 | 25355 | all miss 679.5×120.5 |
| S2 +1 engine, S2 prop +10% | 2 | 24945 | seed 1 tumble; others miss 621.1×178.9 |
| S1 prop +10% | 1 | 25119 | all miss 643.8×156.2 |
| S1 prop +20% | 2 | 25679 | all structural-maxq |
| S1 prop +10%, S2 prop +10% | 2 | 25268 | 1–4 miss 649.2×150.8, 5 structural-maxq |
| **S2 prop +10%** | 1 | 24708 | **1, 2, 4, 5 P 401.6×398.4; seed 3 miss 652.6×147.4** |
| S2 prop +20% | 2 | 24908 | all miss 402.1×−395.4 |

No variant within 2 cost steps passes all five seeds. The row stays in `contracts-viable.json` unchanged, and
`ContractGenTests.EveryCell_AndEveryOffer_IsViable` fails on it, by design and in plain view.

## What the balance PR needs to decide

- Either the ascent needs a closed-loop answer to a stage-1 engine-out (re-aim the pitch or cutoff after the event),
  or the k3 engine-out seed schedule or the contract generator's margins need to change so k3 LEO rows carry
  engine-out margin.
- After that, re-run `FlightBatch.Viable` on finch and pantry. Finch's +1260 cost fix may become unnecessary.
- Seeds: the campaign's five (1–5). Orbits: leo400 circular, ±5% band.
