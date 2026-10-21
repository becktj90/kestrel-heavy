# GOLDEN_DIFF — #313 shared throttle director, option (b)

**Status: draft for Kestrel Physics. Not pushed. Not ready to merge: 81 flights still flip pass→fail and the orbital batch-vs-live test fails for m3, m5, m7, m12 and m19 (details below).**

Baseline: `origin/main` f22bb70. New: this branch (option (b) + event-located cutoff + hop retunes). Every golden, viable and starter design, seeds 1–5, `FlightBatch` (0.2 s). 530 flights.

## Golden file changes (old → new)

| File | Design | Field | Old | New | Why |
|---|---|---|---:|---:|---|
| `HomeAutoBuild.FirstHopPropKg` (KH-1 starter) | kh-1 starter | propMass kg | 515 | 464 | live peak back in band with headroom (Arcade retune) |
| `data/golden/campaign-viable.json` | m2b-2600 | propMass kg | 2540 | 2585 | #315 headroom ≥ 97 km |
| `data/golden/campaign-viable.json` | m2f-1600 | propMass kg | 1560 | 1580 | Physics headroom ≥ 97 km |
| `data/golden/campaign-viable.json` | m1b-705 | propMass kg | 705 | **705 (not changed)** | Arcade asked 705 → 645. Under (b) 645 flies 9.07 km and fails; 705 flies 10.000 km on every seed. Kept it. Needs Arcade's OK. |

Ids don't encode the exact mass (m2b-2600 already held 2540 kg), so no ids were renamed. Renaming changes `FlightBatch.DesignHash` and so the seeded failure draws.

## Hop peaks per seed (batch 0.2 s; live 1/120 s seed 1)

| Design | s1 | s2 | s3 | s4 | s5 | live | max g | band | headroom rule |
|---|---:|---:|---:|---:|---:|---:|---:|---|---|
| KH-1 starter (464 kg) | 10.028 | 10.028 | 10.028 | 10.028 | 10.028 | 10.046 | 6.00 | 9.5–10.5 km | ≥ 9.7 ✔ |
| m1b-705 | 10.000 | 10.000 | 10.000 | 10.000 | 10.000 | 10.004 | 4.92 | 9.5–10.5 km | — |
| m1a-520 (winner) | 10.046 | 10.046 | 10.046 | 10.046 | 10.046 | 10.044 | 3.27 | 9.5–10.5 km | — |
| KH-2 starter (1405 kg) | 99.360 | 99.360 | 99.360 | 99.360 | 99.360 | — | 7.66 | 95–105 km | — |
| m2b-2600 (2585 kg) | 99.175 | 99.175 | 99.175 | 99.175 | 99.175 | 99.187 | 5.79 | 95–105 km | ≥ 97 ✔ |
| m2f-1600 (1580 kg) | 100.114 | 100.114 | 100.114 | 100.114 | 100.114 | 100.156 | 5.27 | 95–105 km | ≥ 97 ✔ |

#316: the KH-2 starter pulls **7.66 g** in the batch and **7.69 g** live under the shared director (limit 6 g). Not changed here.

#315: with the shared director and the old masses, m2b (2540 kg) flies 95.516 km and m2f (1560 kg) 96.255 km. After the retune: 99.175 and 100.114 km.

## Verdict flips: 81 flights (81 pass→fail, 0 fail→pass)

| Source | Mission | Design | Seeds now failing | New outcome (apo / peri km) | Cause |
|---|---|---|---|---|---|
| board-strength | m3 | h-n5-k8-24000-2500 | 1,2,3,4,5 | orbit 205.0/189.8 | located cutoff lands peri on the 10 km margin = 189.8 < 190 km band edge |
| campaign-viable | m3 | h-n5-k8-24000-2500 | 1,2,3,4,5 | orbit 205.0/189.8 | located cutoff lands peri on the 10 km margin = 189.8 < 190 km band edge |
| campaign-viable | m3 | m3-eight | 1,2,3,4,5 | orbit 199.8/189.7 | located cutoff lands peri on the 10 km margin = 189.8 < 190 km band edge |
| campaign-winners | m3 | h-n5-k8-24000-2500 | 1,2,3,4,5 | orbit 205.0/189.8 | located cutoff lands peri on the 10 km margin = 189.8 < 190 km band edge |
| found | m3 | h-n5-k8-24000-2500 | 1,2,3,4,5 | orbit 205.0/189.8 | located cutoff lands peri on the 10 km margin = 189.8 < 190 km band edge |
| campaign-viable | m9 | n8-p260-m100-u28 | 1 | orbit 222.3/190.0 | peri 189.976 < 190 km edge (located cutoff on the margin) |
| board-fly | m10 | b9-p340-m83-u33-s14 | 1,2,3,4,5 | failure/prop-depletion 2.4/-6367.3; orbit 199.7/189.7 | seeds without a fuel imbalance: peri 189.7 < 190 km edge; seeds with the FuelBalance call: throttle scale 0.97 for the rest of the flight → prop-depletion |
| campaign-viable | m10 | b10-p340-m83-u33-s14 | 1,2,3,4,5 | failure/prop-depletion 2.4/-6367.2; orbit 200.0/189.7 | seeds without a fuel imbalance: peri 189.7 < 190 km edge; seeds with the FuelBalance call: throttle scale 0.97 for the rest of the flight → prop-depletion |
| campaign-viable | m10 | b8-p340-m82-u33-s14 | 1,2,3,4,5 | failure/prop-depletion 2.4/-6367.3; orbit 200.0/189.7 | seeds without a fuel imbalance: peri 189.7 < 190 km edge; seeds with the FuelBalance call: throttle scale 0.97 for the rest of the flight → prop-depletion |
| campaign-viable | m10 | b9-p340-m83-u33-s14 | 1,2,3,4,5 | failure/prop-depletion 2.4/-6367.3; orbit 199.7/189.7 | seeds without a fuel imbalance: peri 189.7 < 190 km edge; seeds with the FuelBalance call: throttle scale 0.97 for the rest of the flight → prop-depletion |
| campaign-winners | m10 | b9-p340-m83-u33-s14 | 1,2,3,4,5 | failure/prop-depletion 2.4/-6367.3; orbit 199.7/189.7 | seeds without a fuel imbalance: peri 189.7 < 190 km edge; seeds with the FuelBalance call: throttle scale 0.97 for the rest of the flight → prop-depletion |
| found | m10 | b9-p340-m83-u33-s14 | 1,2,3,4,5 | failure/prop-depletion 2.4/-6367.3; orbit 199.7/189.7 | seeds without a fuel imbalance: peri 189.7 < 190 km edge; seeds with the FuelBalance call: throttle scale 0.97 for the rest of the flight → prop-depletion |
| starter | m10 | b9-p340-m83-u33-s14 | 1,2,3,4,5 | failure/prop-depletion 2.4/-6367.3; orbit 199.7/189.7 | seeds without a fuel imbalance: peri 189.7 < 190 km edge; seeds with the FuelBalance call: throttle scale 0.97 for the rest of the flight → prop-depletion |
| board-fly | m19 | kh19-win | 1,2,3,4,5 | failure/prop-depletion 199.5/59.6; orbit 200.2/189.7 | FuelBalance call → throttle 0.97 → upper stage runs dry before cutoff (prop-depletion); other seeds peri 189.7 < 190 km |
| campaign-winners | m19 | kh19-win | 1,2,3,4,5 | failure/prop-depletion 199.5/59.6; orbit 200.2/189.7 | FuelBalance call → throttle 0.97 → upper stage runs dry before cutoff (prop-depletion); other seeds peri 189.7 < 190 km |
| found | m19 | kh19-win | 1,2,3,4,5 | failure/prop-depletion 199.5/59.6; orbit 200.2/189.7 | FuelBalance call → throttle 0.97 → upper stage runs dry before cutoff (prop-depletion); other seeds peri 189.7 < 190 km |
| starter | m19 | kh19-win | 1,2,3,4,5 | failure/prop-depletion 199.5/59.6; orbit 200.2/189.7 | FuelBalance call → throttle 0.97 → upper stage runs dry before cutoff (prop-depletion); other seeds peri 189.7 < 190 km |

All other flights keep their verdict. Every peak per flight is in the attached `new2.tsv`/`old2.tsv` (scratch) and can be inlined on request.


## Orbital batch vs live (seed 1, golden winners)

Live = 1/120 s (FlightDriver). Tolerances (named constants in `MissionFlightSetup`): apoapsis 0.5%, perigee 1 km absolute, SMA 0.1%.

### KH-6 (m6 `t6-p300-m76-u26`) before and after event location, 0.2 s batch

| | Batch apo | Live apo | Δapo | Batch peri | Live peri | Δperi | Batch SMA | Live SMA | ΔSMA | In tolerance |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| Before | 207.154 | 202.697 | 2.199% | 200.303 | 192.770 | 7533 m | 6581.865 | 6575.871 | 0.0912% | no (apo, peri) |
| After | 203.071 | 202.668 | 0.199% | 192.238 | 192.232 | 6 m | 6575.792 | 6575.587 | 0.0031% | **yes** |

Event location gets KH-6 inside every tolerance at 0.2 s, so Physics rule 4 (0.05 s orbital step) is not triggered.

### Worst orbital case (m12 `kh12-win`) before and after event location

| | Step | Batch apo | Live apo | Δapo | Batch peri | Live peri | Δperi | ΔSMA |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Before | 0.2 s | 602.240 | 905.410 | 33.48% | 592.334 | 591.551 | 783 m | 2.12% |
| After | 0.2 s | 602.240 | 905.397 | 33.48% | 592.239 | 591.550 | 689 m | 2.12% |
| After | 0.1 s | 637.685 | 905.397 | 29.57% | 592.135 | 591.550 | 585 m | 1.87% |
| After | 0.05 s | 690.061 | 905.397 | 23.78% | 592.003 | 591.550 | 453 m | 1.51% |

m12 is not a cutoff-location error: the same gap is in the bare `AscentSim` with no director. The cutoff fires 3.4 s later live (831.95 s vs 828.52 s) because the peri-based cutoff is grazing (peri creeps toward the margin while apoapsis grows fast). A smaller step only shrinks it slowly.

### Every orbital winner after event location, at each batch step

| Mission | Step s | Apo batch/live km | Δapo | Δperi | ΔSMA | Verdict |
|---|---|---|---|---|---|---|
| m3 | 0.20 | 205.005 / 254.745 | 19.525% | 795 m | 0.3828% | OUT |
| m3 | 0.10 | 216.923 / 254.745 | 14.847% | 602 m | 0.2911% | OUT |
| m3 | 0.05 | 244.725 / 254.745 | 3.933% | 174 m | 0.0772% | OUT |
| m4 | 0.20 | 500.636 / 501.696 | 0.211% | 150 m | 0.0066% | OK |
| m4 | 0.10 | 501.437 / 501.696 | 0.052% | 55 m | 0.0015% | OK |
| m4 | 0.05 | 501.545 / 501.696 | 0.030% | 36 m | 0.0008% | OK |
| m5 | 0.20 | 401.202 / 407.832 | 1.626% | 31 m | 0.0492% | OUT |
| m5 | 0.10 | 400.485 / 407.832 | 1.801% | 112 m | 0.0550% | OUT |
| m5 | 0.05 | 400.218 / 407.832 | 1.867% | 132 m | 0.0571% | OUT |
| m6 | 0.20 | 203.071 / 202.668 | 0.199% | 6 m | 0.0031% | OK |
| m6 | 0.10 | 202.834 / 202.668 | 0.082% | 3 m | 0.0013% | OK |
| m6 | 0.05 | 202.726 / 202.668 | 0.029% | 2 m | 0.0005% | OK |
| m7 | 0.20 | 236.804 / 235.424 | 0.586% | 3 m | 0.0105% | OUT |
| m7 | 0.10 | 236.296 / 235.424 | 0.370% | 3 m | 0.0066% | OK |
| m7 | 0.05 | 235.811 / 235.424 | 0.164% | 1 m | 0.0029% | OK |
| m8 | 0.20 | 400.036 / 400.071 | 0.009% | 14 m | 0.0004% | OK |
| m8 | 0.10 | 400.042 / 400.071 | 0.007% | 7 m | 0.0001% | OK |
| m8 | 0.05 | 400.038 / 400.071 | 0.008% | 8 m | 0.0002% | OK |
| m9 | 0.20 | 270.728 / 270.980 | 0.093% | 16 m | 0.0020% | OK |
| m9 | 0.10 | 270.847 / 270.980 | 0.049% | 8 m | 0.0011% | OK |
| m9 | 0.05 | 270.904 / 270.980 | 0.028% | 4 m | 0.0006% | OK |
| m10 | 0.20 | 199.737 / 199.722 | 0.008% | 5 m | 0.0002% | OK |
| m10 | 0.10 | 199.729 / 199.722 | 0.004% | 2 m | 0.0001% | OK |
| m10 | 0.05 | 199.725 / 199.722 | 0.002% | 1 m | 0.0000% | OK |
| m11 | 0.20 | 550.043 / 550.043 | 0.000% | 1 m | 0.0000% | OK |
| m11 | 0.10 | 550.044 / 550.043 | 0.000% | 0 m | 0.0000% | OK |
| m11 | 0.05 | 550.043 / 550.043 | 0.000% | 0 m | 0.0000% | OK |
| m12 | 0.20 | 602.240 / 905.397 | 33.483% | 689 m | 2.1221% | OUT |
| m12 | 0.10 | 637.685 / 905.397 | 29.568% | 585 m | 1.8741% | OUT |
| m12 | 0.05 | 690.061 / 905.397 | 23.784% | 453 m | 1.5076% | OUT |
| m13 | 0.20 | 550.065 / 550.053 | 0.002% | 8 m | 0.0001% | OK |
| m13 | 0.10 | 550.057 / 550.053 | 0.001% | 4 m | 0.0001% | OK |
| m13 | 0.05 | 550.055 / 550.053 | 0.000% | 2 m | 0.0000% | OK |
| m14 | 0.20 | 400.053 / 400.047 | 0.001% | 7 m | 0.0000% | OK |
| m14 | 0.10 | 400.049 / 400.047 | 0.000% | 0 m | 0.0000% | OK |
| m14 | 0.05 | 400.042 / 400.047 | 0.001% | 2 m | 0.0000% | OK |
| m15 | 0.20 | 270.411 / 270.644 | 0.086% | 14 m | 0.0019% | OK |
| m15 | 0.10 | 270.530 / 270.644 | 0.042% | 7 m | 0.0009% | OK |
| m15 | 0.05 | 270.582 / 270.644 | 0.023% | 4 m | 0.0005% | OK |
| m16 | 0.20 | 500.076 / 501.183 | 0.221% | 210 m | 0.0065% | OK |
| m16 | 0.10 | 500.274 / 501.183 | 0.181% | 182 m | 0.0053% | OK |
| m16 | 0.05 | 500.926 / 501.183 | 0.051% | 100 m | 0.0011% | OK |
| m17 | 0.20 | 255.682 / 256.534 | 0.332% | 16 m | 0.0066% | OK |
| m17 | 0.10 | 256.210 / 256.534 | 0.126% | 6 m | 0.0025% | OK |
| m17 | 0.05 | 256.377 / 256.534 | 0.061% | 3 m | 0.0012% | OK |
| m18 | 0.20 | 273.963 / 273.944 | 0.007% | 18 m | 0.0000% | OK |
| m18 | 0.10 | 273.982 / 273.944 | 0.014% | 7 m | 0.0002% | OK |
| m18 | 0.05 | 273.965 / 273.944 | 0.008% | 3 m | 0.0001% | OK |
| m19 | 0.20 | 199.519 / 199.675 | 0.078% | 17362 m | 0.1344% | OUT |
| m19 | 0.10 | 199.634 / 199.675 | 0.021% | 7936 m | 0.0612% | OUT |
| m19 | 0.05 | 199.660 / 199.675 | 0.008% | 3598 m | 0.0277% | OUT |
| m20 | 0.20 | 257.446 / 258.486 | 0.402% | 21 m | 0.0080% | OK |
| m20 | 0.10 | 258.038 / 258.486 | 0.173% | 9 m | 0.0035% | OK |
| m20 | 0.05 | 258.315 / 258.486 | 0.066% | 3 m | 0.0013% | OK |

m19 fails live and in the batch on seed 1 (prop-depletion); its gap is a perigee gap on a failed flight.

Runtime for the 18 orbital and 2 hop winners, seed 1, one thread: **0.2 s ≈ 630 ms, 0.1 s ≈ 1266 ms (×2.0), 0.05 s ≈ 2447 ms (×3.9)**, live 1/120 s ≈ 15055 ms. Still out at 0.05 s: m3 (3.9% apo), m5 (1.9% apo), m12 (23.8% apo, 1.5% SMA), m19 (3.6 km peri).

## Findings Physics needs to rule on

1. **The cutoff margin sits on the band edge.** The target-energy cutoff fires when perigee ≥ min(target, altitude) − 10 km (the JS parity value). Once located exactly, a 200 km mission's perigee lands at about 189.7 km, just under the 190 km band edge (−5%). The live pad already did this. The batch only passed because the unlocated 0.2 s step overshot. That is the cause of the m3, m9 and most of the m10/m19 flips, and of the `failed-prediction … Perigee 390.0 km, target 400` vectors. I tried a margin of half the band (5 km at 200 km). That cut the flips to 49 but made the grazing cutoff worse (m17 and m20 also go out of tolerance; m3 `m3-eight` flies 199.9 km batch vs 345.7 km live). I took it back out. Not in this diff.
2. **The peri-based cutoff is ill-conditioned.** m3 and m12 apoapses swing by tens to hundreds of km with the step size, and between neighbouring prop masses (m3 `h-n5`: 23800 kg → 201.9 km, 24200 kg → 311.0 km). Physics rule 6 suggests cutting off at target **energy** (SMA reaching the target's). That is monotone during the burn and would locate cleanly. It changes the model, so I have not done it without a ruling.
3. **FuelBalance penalty.** When the seeded FuelBalance call resolves without crossfeed, the director scales the throttle to 0.97 for the rest of the flight, on every stage. m10 and m19 upper stages then run dry before cutoff. More upper-stage propellant made it worse (m10 b9 stage 3 at 34–36 t: every seed fails). The campaign search finds no m10 or m19 design that passes every seed. Is the penalty meant to last past the stage where the imbalance occurs?
4. **Director max-Q vs the design's own max-Q throttle.** Under (b) the autopilot takes the max-Q call at the design's value and suppresses the guidance's own max-Q law. The two laws differ. KH-1 starter: guidance law alone (bare `Headless`) 8.746 km, director 10.028 km. KH-2 starter: 97.559 vs 99.360 km. Batch, timeline, predictor and pad now all fly the director, so they agree. But the old tests that compare against bare `Headless.Simulate` now fail (listed in PR_BODY). If Physics would rather "flies the design's settings" meant the guidance law, set `AutopilotAccept(MaxQ)` to false. Then KH-1 needs ~515 kg again.

## Appendix: every flight, old → new (apo km per seed; P pass / F fail)

| Source | Mission | Design | s1 | s2 | s3 | s4 | s5 |
|---|---|---|---|---|---|---|---|
| campaign-viable | m1 | m1a-520 | P 10.046 → P 10.046 | P 10.046 → P 10.046 | P 10.046 → P 10.046 | P 10.046 → P 10.046 | P 10.046 → P 10.046 |
| campaign-viable | m1 | m1b-705 | P 10.000 → P 10.000 | P 10.000 → P 10.000 | P 10.000 → P 10.000 | P 10.000 → P 10.000 | P 10.000 → P 10.000 |
| campaign-viable | m1 | m1p-360 | P 9.941 → P 9.941 | P 9.941 → P 9.941 | P 9.941 → P 9.941 | P 9.941 → P 9.941 | P 9.941 → P 9.941 |
| campaign-winners | m1 | m1a-520 | P 10.046 → P 10.046 | P 10.046 → P 10.046 | P 10.046 → P 10.046 | P 10.046 → P 10.046 | P 10.046 → P 10.046 |
| found | m1 | m1a-520 | P 10.046 → P 10.046 | P 10.046 → P 10.046 | P 10.046 → P 10.046 | P 10.046 → P 10.046 | P 10.046 → P 10.046 |
| starter | m1 | (starter) | P 9.787 → P 10.028 | P 9.787 → P 10.028 | P 9.787 → P 10.028 | P 9.787 → P 10.028 | P 9.787 → P 10.028 |
| campaign-viable | m2 | m2a-2500 | P 99.833 → P 98.708 | P 99.833 → P 98.708 | P 99.833 → P 98.708 | P 99.833 → P 98.708 | P 99.833 → P 98.708 |
| campaign-viable | m2 | m2b-2600 | P 97.127 → P 99.175 | P 97.127 → P 99.175 | P 97.127 → P 99.175 | P 97.127 → P 99.175 | P 97.127 → P 99.175 |
| campaign-viable | m2 | m2f-1600 | P 96.794 → P 100.114 | P 96.794 → P 100.114 | P 96.794 → P 100.114 | P 96.794 → P 100.114 | P 96.794 → P 100.114 |
| campaign-winners | m2 | m2a-2500 | P 99.833 → P 98.708 | P 99.833 → P 98.708 | P 99.833 → P 98.708 | P 99.833 → P 98.708 | P 99.833 → P 98.708 |
| found | m2 | m2a-2500 | P 99.833 → P 98.708 | P 99.833 → P 98.708 | P 99.833 → P 98.708 | P 99.833 → P 98.708 | P 99.833 → P 98.708 |
| starter | m2 | (starter) | P 97.559 → P 99.360 | P 97.559 → P 99.360 | P 97.559 → P 99.360 | P 97.559 → P 99.360 | P 97.559 → P 99.360 |
| board-strength | m3 | h-n5-k8-24000-2500 | P 205.992 → F 205.005 | P 205.992 → F 205.005 | P 205.992 → F 205.005 | P 205.992 → F 205.005 | P 205.992 → F 205.005 |
| campaign-viable | m3 | h-n5-k8-24000-2500 | P 205.992 → F 205.005 | P 205.992 → F 205.005 | P 205.992 → F 205.005 | P 205.992 → F 205.005 | P 205.992 → F 205.005 |
| campaign-viable | m3 | h-n7-k12-30000-4000 | P 200.029 → P 200.029 | P 200.029 → P 200.029 | P 200.029 → P 200.029 | P 200.029 → P 200.029 | P 200.029 → P 200.029 |
| campaign-viable | m3 | m3-eight | P 200.125 → F 199.829 | P 200.125 → F 199.829 | P 200.125 → F 199.829 | P 200.125 → F 199.829 | P 200.125 → F 199.829 |
| campaign-winners | m3 | h-n5-k8-24000-2500 | P 205.992 → F 205.005 | P 205.992 → F 205.005 | P 205.992 → F 205.005 | P 205.992 → F 205.005 | P 205.992 → F 205.005 |
| found | m3 | h-n5-k8-24000-2500 | P 205.992 → F 205.005 | P 205.992 → F 205.005 | P 205.992 → F 205.005 | P 205.992 → F 205.005 | P 205.992 → F 205.005 |
| starter | m3 | h-n5-k8-24000-2500 | F 220.609 → F 215.801 | F 220.609 → F 215.801 | F 220.609 → F 215.801 | F 220.609 → F 215.801 | F 220.609 → F 215.801 |
| board-fix | m4 | n5-200000-k6 | P 522.535 → P 521.213 | P 522.535 → P 521.213 | P 522.535 → P 521.213 | P 522.535 → P 521.213 | P 522.535 → P 521.213 |
| campaign-viable | m4 | n5-200000-k6 | P 504.472 → P 500.636 | P 504.472 → P 500.636 | P 504.472 → P 500.636 | P 504.472 → P 500.636 | P 504.472 → P 500.636 |
| campaign-viable | m4 | n6-190000-k6-u40000 | P 500.237 → P 500.188 | P 500.237 → P 500.188 | P 500.237 → P 500.188 | P 500.237 → P 500.188 | P 500.237 → P 500.188 |
| campaign-viable | m4 | n7-270000-k8-u40000 | P 500.036 → P 500.014 | P 500.036 → P 500.014 | P 500.036 → P 500.014 | P 500.036 → P 500.014 | P 500.036 → P 500.014 |
| campaign-winners | m4 | n5-200000-k6 | P 504.472 → P 500.636 | P 504.472 → P 500.636 | P 504.472 → P 500.636 | P 504.472 → P 500.636 | P 504.472 → P 500.636 |
| found | m4 | n5-200000-k6 | P 504.472 → P 500.636 | P 504.472 → P 500.636 | P 504.472 → P 500.636 | P 504.472 → P 500.636 | P 504.472 → P 500.636 |
| starter | m4 | n5-200000-k6 | F 2636.910 → F 2635.650 | F 2636.910 → F 2635.650 | F 2636.910 → F 2635.650 | F 2636.910 → F 2635.650 | F 2636.910 → F 2635.650 |
| board-fix | m5 | n8-360000-k8-u30000 | P 400.151 → P 400.092 | P 400.151 → P 400.092 | P 400.151 → P 400.092 | P 400.151 → P 400.092 | P 400.151 → P 400.092 |
| campaign-viable | m5 | n10-500000-k6-u35000 | P 400.048 → P 400.048 | P 400.048 → P 400.048 | P 400.048 → P 400.048 | P 400.048 → P 400.048 | P 400.048 → P 400.048 |
| campaign-viable | m5 | n8-360000-k8-u30000 | P 401.933 → P 401.205 | P 401.933 → P 401.205 | P 401.933 → P 401.205 | P 401.933 → P 401.205 | P 401.933 → P 401.205 |
| campaign-viable | m5 | n9-400000-k7-u40000 | P 400.955 → P 400.565 | P 400.955 → P 400.565 | P 400.955 → P 400.565 | P 400.955 → P 400.565 | P 400.955 → P 400.565 |
| campaign-winners | m5 | n8-360000-k8-u30000 | P 403.093 → P 401.202 | P 403.093 → P 401.202 | P 403.093 → P 401.202 | P 403.093 → P 401.202 | P 403.093 → P 401.202 |
| found | m5 | n8-360000-k8-u30000 | P 403.093 → P 401.202 | P 403.093 → P 401.202 | P 403.093 → P 401.202 | P 403.093 → P 401.202 | P 403.093 → P 401.202 |
| starter | m5 | n8-360000-k8-u30000 | F 1898.047 → F 1897.489 | F 1898.047 → F 1897.489 | F 1898.047 → F 1897.489 | F 1898.047 → F 1897.489 | F 1898.047 → F 1897.489 |
| board-strength | m6 | t6-p300-m76-u26 | P 207.154 → P 203.071 | P 207.154 → P 203.071 | P 207.154 → P 203.071 | P 207.154 → P 203.071 | P 207.154 → P 203.071 |
| campaign-viable | m6 | t6-p300-m76-u26 | P 203.995 → P 202.981 | P 203.995 → P 202.981 | P 203.995 → P 202.981 | P 203.995 → P 202.981 | P 203.995 → P 202.981 |
| campaign-viable | m6 | t7-p300-m70-u26 | P 202.435 → P 202.258 | P 202.435 → P 202.258 | P 202.435 → P 202.258 | P 202.435 → P 202.258 | P 202.435 → P 202.258 |
| campaign-viable | m6 | t9-p320-m64-u26 | P 207.060 → P 207.020 | P 207.060 → P 207.020 | P 207.060 → P 207.020 | P 207.060 → P 207.020 | P 207.060 → P 207.020 |
| campaign-winners | m6 | t6-p300-m76-u26 | P 207.154 → P 203.071 | P 207.154 → P 203.071 | P 207.154 → P 203.071 | P 207.154 → P 203.071 | P 207.154 → P 203.071 |
| found | m6 | t6-p300-m76-u26 | P 207.154 → P 203.071 | P 207.154 → P 203.071 | P 207.154 → P 203.071 | P 207.154 → P 203.071 | P 207.154 → P 203.071 |
| starter | m6 | t6-p300-m76-u26 | F 216.350 → F 215.557 | F 216.350 → F 215.557 | F 216.350 → F 215.557 | F 216.350 → F 215.557 | F 216.350 → F 215.557 |
| board-fly | m7 | n5-p260-m80-u28 | P 244.259 → P 236.804 | P 244.259 → P 236.804 | P 244.259 → P 236.804 | P 244.259 → P 236.804 | P 244.259 → P 236.804 |
| campaign-viable | m7 | n5-p260-m80-u28 | P 237.261 → P 236.561 | P 237.261 → P 236.561 | P 237.261 → P 236.561 | P 237.261 → P 236.561 | P 237.261 → P 236.561 |
| campaign-viable | m7 | n7-p260-m80-u28 | P 276.385 → P 274.989 | P 276.385 → P 274.989 | P 276.385 → P 274.989 | P 276.385 → P 274.989 | P 276.385 → P 274.989 |
| campaign-viable | m7 | n9-p260-m80-u28 | P 255.611 → P 251.686 | P 255.611 → P 251.686 | P 255.611 → P 251.686 | P 255.611 → P 251.686 | P 255.611 → P 251.686 |
| campaign-winners | m7 | n5-p260-m80-u28 | P 244.259 → P 236.804 | P 244.259 → P 236.804 | P 244.259 → P 236.804 | P 244.259 → P 236.804 | P 244.259 → P 236.804 |
| found | m7 | n5-p260-m80-u28 | P 244.259 → P 236.804 | P 244.259 → P 236.804 | P 244.259 → P 236.804 | P 244.259 → P 236.804 | P 244.259 → P 236.804 |
| starter | m7 | n5-p260-m80-u28 | P 244.259 → P 236.804 | P 244.259 → P 236.804 | P 244.259 → P 236.804 | P 244.259 → P 236.804 | P 244.259 → P 236.804 |
| board-strength | m8 | c8-360-k7-u35 | P 400.038 → P 400.036 | P 400.038 → P 400.036 | P 400.038 → P 400.036 | P 400.038 → P 400.036 | P 400.038 → P 400.036 |
| campaign-viable | m8 | c8-360-k7-u35 | P 400.082 → P 400.035 | P 400.082 → P 400.035 | P 400.082 → P 400.035 | P 400.082 → P 400.035 | P 400.082 → P 400.035 |
| campaign-viable | m8 | c9-420-k7-u32 | P 400.055 → P 400.052 | P 400.055 → P 400.052 | P 400.055 → P 400.052 | P 400.055 → P 400.052 | P 400.055 → P 400.052 |
| campaign-viable | m8 | c9-420-k7-u38 | P 400.639 → P 400.359 | P 400.639 → P 400.359 | P 400.639 → P 400.359 | P 400.639 → P 400.359 | P 400.639 → P 400.359 |
| campaign-winners | m8 | c8-360-k7-u35 | P 400.038 → P 400.036 | P 400.038 → P 400.036 | P 400.038 → P 400.036 | P 400.038 → P 400.036 | P 400.038 → P 400.036 |
| found | m8 | c8-360-k7-u35 | P 400.038 → P 400.036 | P 400.038 → P 400.036 | P 400.038 → P 400.036 | P 400.038 → P 400.036 | P 400.038 → P 400.036 |
| starter | m8 | c8-360-k7-u35 | F 1727.785 → F 1721.912 | F 1727.785 → F 1721.912 | F 1727.785 → F 1721.912 | F 1727.785 → F 1721.912 | F 1727.785 → F 1721.912 |
| board-fly | m9 | n4-p260-m100-u28 | P 278.096 → P 270.728 | P 278.096 → P 270.728 | P 278.096 → P 270.728 | P 278.096 → P 270.728 | P 278.096 → P 270.728 |
| campaign-viable | m9 | n4-p260-m100-u28 | P 218.275 → P 206.758 | P 218.275 → P 206.758 | P 218.275 → P 206.758 | P 218.275 → P 206.758 | P 218.275 → P 206.758 |
| campaign-viable | m9 | n6-p260-m100-u28 | P 278.096 → P 270.728 | P 278.096 → P 270.728 | P 278.096 → P 270.728 | P 278.096 → P 270.728 | P 278.096 → P 270.728 |
| campaign-viable | m9 | n8-p260-m100-u28 | P 242.222 → F 222.322 | P 242.222 → P 241.802 | P 242.222 → P 241.802 | P 242.222 → P 241.802 | P 242.222 → P 241.802 |
| campaign-winners | m9 | n4-p260-m100-u28 | P 278.096 → P 270.728 | P 278.096 → P 270.728 | P 278.096 → P 270.728 | P 278.096 → P 270.728 | P 278.096 → P 270.728 |
| found | m9 | n4-p260-m100-u28 | P 218.275 → P 206.758 | P 218.275 → P 206.758 | P 218.275 → P 206.758 | P 218.275 → P 206.758 | P 218.275 → P 206.758 |
| starter | m9 | n4-p260-m100-u28 | P 278.096 → P 270.728 | P 278.096 → P 270.728 | P 278.096 → P 270.728 | P 278.096 → P 270.728 | P 278.096 → P 270.728 |
| board-fly | m10 | b9-p340-m83-u33-s14 | P 201.282 → F 199.737 | P 201.282 → F 2.418 | P 201.282 → F 2.418 | P 201.282 → F 199.737 | P 201.282 → F 199.737 |
| campaign-viable | m10 | b10-p340-m83-u33-s14 | P 205.552 → F 199.953 | P 205.552 → F 2.372 | P 205.552 → F 199.953 | P 205.552 → F 199.953 | P 205.552 → F 199.953 |
| campaign-viable | m10 | b8-p340-m82-u33-s14 | P 200.468 → F 199.989 | P 200.468 → F 199.989 | P 200.468 → F 2.420 | P 200.468 → F 199.989 | P 200.468 → F 199.989 |
| campaign-viable | m10 | b9-p340-m83-u33-s14 | P 199.772 → F 199.734 | P 199.772 → F 2.445 | P 199.772 → F 2.445 | P 199.772 → F 199.734 | P 199.772 → F 199.734 |
| campaign-winners | m10 | b9-p340-m83-u33-s14 | P 201.282 → F 199.737 | P 201.282 → F 2.418 | P 201.282 → F 2.418 | P 201.282 → F 199.737 | P 201.282 → F 199.737 |
| found | m10 | b9-p340-m83-u33-s14 | P 201.282 → F 199.737 | P 201.282 → F 2.418 | P 201.282 → F 2.418 | P 201.282 → F 199.737 | P 201.282 → F 199.737 |
| starter | m10 | b9-p340-m83-u33-s14 | P 201.282 → F 199.737 | P 201.282 → F 2.418 | P 201.282 → F 2.418 | P 201.282 → F 199.737 | P 201.282 → F 199.737 |
| board-fly | m11 | kh11-win | P 550.053 → P 550.042 | P 550.053 → P 550.042 | P 550.053 → P 550.042 | P 550.053 → P 550.042 | P 550.053 → P 550.042 |
| campaign-winners | m11 | kh11-win | P 553.669 → P 550.043 | P 553.669 → P 550.043 | P 553.669 → P 550.043 | P 553.669 → P 550.043 | P 553.669 → P 550.043 |
| found | m11 | kh11-win | P 553.669 → P 550.043 | P 553.669 → P 550.043 | P 553.669 → P 550.043 | P 553.669 → P 550.043 | P 553.669 → P 550.043 |
| starter | m11 | kh11-win | P 550.053 → P 550.042 | P 550.053 → P 550.042 | P 550.053 → P 550.042 | P 550.053 → P 550.042 | P 550.053 → P 550.042 |
| board-strength | m12 | kh12-win | P 602.240 → P 602.240 | P 602.240 → P 602.240 | P 602.240 → P 602.240 | P 602.240 → P 602.240 | P 602.240 → P 602.240 |
| campaign-winners | m12 | kh12-win | P 602.240 → P 602.240 | P 602.240 → P 602.240 | P 602.240 → P 602.240 | P 602.240 → P 602.240 | P 602.240 → P 602.240 |
| found | m12 | kh12-win | P 602.240 → P 602.240 | P 602.240 → P 602.240 | P 602.240 → P 602.240 | P 602.240 → P 602.240 | P 602.240 → P 602.240 |
| starter | m12 | kh12-win | F 648.820 → F 639.971 | F 648.820 → F 639.971 | F 648.820 → F 639.971 | F 648.820 → F 639.971 | F 648.820 → F 639.971 |
| board-fly | m13 | kh13-win | P 550.780 → P 550.160 | P 550.780 → P 550.160 | P 550.780 → P 550.160 | P 550.780 → P 550.160 | P 550.780 → P 550.160 |
| campaign-winners | m13 | kh13-win | P 557.738 → P 550.065 | P 557.738 → P 550.065 | P 557.738 → P 550.065 | P 557.738 → P 550.065 | P 557.738 → P 550.065 |
| starter | m13 | kh13-win | P 550.780 → P 550.160 | P 550.780 → P 550.160 | P 550.780 → P 550.160 | P 550.780 → P 550.160 | P 550.780 → P 550.160 |
| board-strength | m14 | kh14-win | P 400.103 → P 400.053 | P 400.103 → P 400.053 | P 400.103 → P 400.053 | P 400.103 → P 400.053 | P 400.103 → P 400.053 |
| campaign-winners | m14 | kh14-win | P 400.103 → P 400.053 | P 400.103 → P 400.053 | P 400.103 → P 400.053 | P 400.103 → P 400.053 | P 400.103 → P 400.053 |
| found | m14 | kh14-win | P 400.103 → P 400.053 | P 400.103 → P 400.053 | P 400.103 → P 400.053 | P 400.103 → P 400.053 | P 400.103 → P 400.053 |
| starter | m14 | kh14-win | F 1058.692 → F 1056.020 | F 1058.692 → F 1056.020 | F 1058.692 → F 1056.020 | F 1058.692 → F 1056.020 | F 1058.692 → F 1056.020 |
| board-fly | m15 | kh15-win | P 271.196 → P 270.411 | P 271.196 → P 270.411 | P 271.196 → P 270.411 | P 271.196 → P 270.411 | P 271.196 → P 270.411 |
| campaign-winners | m15 | kh15-win | P 271.196 → P 270.411 | P 271.196 → P 270.411 | P 271.196 → P 270.411 | P 271.196 → P 270.411 | P 271.196 → P 270.411 |
| found | m15 | kh15-win | P 271.196 → P 270.411 | P 271.196 → P 270.411 | P 271.196 → P 270.411 | P 271.196 → P 270.411 | P 271.196 → P 270.411 |
| starter | m15 | kh15-win | P 271.196 → P 270.411 | P 271.196 → P 270.411 | P 271.196 → P 270.411 | P 271.196 → P 270.411 | P 271.196 → P 270.411 |
| board-strength | m16 | kh16-win | P 500.094 → P 500.076 | P 500.094 → P 500.076 | P 500.094 → P 500.076 | P 500.094 → P 500.076 | P 500.094 → P 500.076 |
| campaign-winners | m16 | kh16-win | P 500.094 → P 500.076 | P 500.094 → P 500.076 | P 500.094 → P 500.076 | P 500.094 → P 500.076 | P 500.094 → P 500.076 |
| starter | m16 | kh16-win | F 519.029 → F 517.573 | F 519.029 → F 517.573 | F 519.029 → F 517.573 | F 519.029 → F 517.573 | F 519.029 → F 517.573 |
| board-fly | m17 | kh17-win | P 259.667 → P 255.682 | P 259.667 → P 255.682 | P 259.667 → P 255.682 | P 259.667 → P 255.682 | P 259.667 → P 255.682 |
| campaign-winners | m17 | kh17-win | P 259.667 → P 255.682 | P 259.667 → P 255.682 | P 259.667 → P 255.682 | P 259.667 → P 255.682 | P 259.667 → P 255.682 |
| found | m17 | kh17-win | P 232.969 → P 226.814 | P 232.969 → P 226.814 | P 232.969 → P 226.814 | P 232.969 → P 226.814 | P 232.969 → P 226.814 |
| starter | m17 | kh17-win | P 259.667 → P 255.682 | P 259.667 → P 255.682 | P 259.667 → P 255.682 | P 259.667 → P 255.682 | P 259.667 → P 255.682 |
| board-fly | m18 | kh18-win | P 280.305 → P 273.963 | P 280.305 → P 273.963 | P 280.305 → P 273.963 | P 280.305 → P 273.963 | P 280.305 → P 273.963 |
| campaign-winners | m18 | kh18-win | P 280.305 → P 273.963 | P 280.305 → P 273.963 | P 280.305 → P 273.963 | P 280.305 → P 273.963 | P 280.305 → P 273.963 |
| found | m18 | kh18-win | P 209.059 → P 202.346 | F -0.000 → F -0.000 | P 209.059 → P 202.346 | P 209.059 → P 202.346 | P 209.059 → P 202.346 |
| starter | m18 | kh18-win | P 280.305 → P 273.963 | P 280.305 → P 273.963 | P 280.305 → P 273.963 | P 280.305 → P 273.963 | P 280.305 → P 273.963 |
| board-fly | m19 | kh19-win | P 201.793 → F 199.519 | P 201.793 → F 199.519 | P 201.793 → F 200.180 | P 201.793 → F 199.519 | P 201.793 → F 200.180 |
| campaign-winners | m19 | kh19-win | P 201.793 → F 199.519 | P 201.793 → F 199.519 | P 201.793 → F 200.180 | P 201.793 → F 199.519 | P 201.793 → F 200.180 |
| found | m19 | kh19-win | P 201.793 → F 199.519 | P 201.793 → F 199.519 | P 201.793 → F 200.180 | P 201.793 → F 199.519 | P 201.793 → F 200.180 |
| starter | m19 | kh19-win | P 201.793 → F 199.519 | P 201.793 → F 199.519 | P 201.793 → F 200.180 | P 201.793 → F 199.519 | P 201.793 → F 200.180 |
| board-fly | m20 | kh20-win | P 268.144 → P 257.446 | P 268.144 → P 257.446 | P 268.144 → P 257.446 | P 268.144 → P 257.446 | P 268.144 → P 257.446 |
| campaign-winners | m20 | kh20-win | P 268.144 → P 257.446 | P 268.144 → P 257.446 | P 268.144 → P 257.446 | P 268.144 → P 257.446 | P 268.144 → P 257.446 |
| found | m20 | kh20-win | P 210.557 → P 209.151 | P 210.557 → P 209.151 | P 210.557 → P 209.151 | P 210.557 → P 209.151 | P 210.557 → P 209.151 |
| starter | m20 | kh20-win | P 268.144 → P 257.446 | P 268.144 → P 257.446 | P 268.144 → P 257.446 | P 268.144 → P 257.446 | P 268.144 → P 257.446 |
