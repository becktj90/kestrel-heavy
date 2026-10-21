# GOLDEN_DIFF: #313 shared throttle director, with Kestrel Physics rulings 1-4

Branch `fix/shared-throttle-director`, local only (not pushed). Baseline "old" is `origin/main` before #313 (old2 run); "new" is this commit.
Batch is FlightBatch at 0.2 s. Live is the pad setup at 1/120 s with the batch monitor and the shared director. γ is the flight-path angle at engine cutoff (or at the last thrusting state on a burnout or failure).

Rulings applied:
1. The engines cut off at the target energy itself, with no 10 km offset and no half-band. The scoring bands are unchanged. An FPA miss is a real miss.
2. Energy cutoff for batch and live in the same code: ε = −μ/(2a), with a = R + (h_apo + h_peri)/2 (c3: ε = C3/2). It is located by bisection inside the step. Kestrel flights have no perigee cutoff (it stays only on the JS-parity `Stack.SimInputs`/raw `SimOptions` path).
3. The fuel-imbalance penalty throttles only its stage and ends when that stage separates or shuts down. I applied the same scoping to the stage-1 engine-out throttle (8/9 on a 9-engine stage), which had been carrying into the upper stage. **Physics: please confirm this extension.**
4. The design's GuidanceProfile max-Q throttle is the only max-Q law. A taken max-Q call on a design without one engages that same law (`EngageMaxQCap`). So bare sim = batch = designer = pad for every design with no events. KH-1 is back at 515 kg.

## Acceptance checks

| Check | Result |
|---|---|
| KH-1 starter ≥ 9.7 km on every seed | **PASS**: 9.786, 9.786, 9.786, 9.786, 9.786 km batch; live 9.787 km |
| m1b ≥ 9.7 km | **PASS**: 705 kg, 10.000 km on every seed (live 10.004) |
| m2b ≥ 97 km | **PASS**: 2585 kg, 101.017 km on every seed (live 101.027) |
| m2f ≥ 97 km | **PASS**: 1580 kg, 100.098 km on every seed (live 100.156) |
| Batch vs live: apo 0.5%, peri 1 km, SMA 0.1%, every winner and starter | **PASS for every flight that reaches orbit** (worst: m7 n5 0.40% apo and 0.09% SMA; m4 raw starter 0.88 km peri). **FAIL** only on the prop-depletion real misses m10 (1.76% apo), m17 (0.33% apo, 0.27% SMA) and m19 (0.89% apo). These burn to depletion and never reach the energy cutoff, so the cutoff never pins the SMA. |
| Every golden winner passes seeds 1-5 | **FAIL: m10, m17, m19** (real misses below). The other 17 pass. |
| Real-miss list with γ | below |

## Golden file changes (old → new)

All retunes use real performance. No band moved. An id changes only where it encodes the changed value, and the hash is re-verified on the renamed id.

| File | Mission | Design | Change | Why | Result (seeds 1-5, batch) |
|---|---|---|---|---|---|
| campaign-viable.json | m2 | m2b-2600 | propMass 2540 → 2585 kg (from a9ec49c) | #315 headroom ≥ 97 km | 101.017 km |
| campaign-viable.json | m2 | m2f-1600 | propMass 1560 → 1580 kg (from a9ec49c) | headroom ≥ 97 km (1560 kg flies 96.79) | 100.098 km |
| campaign-winners.json, campaign-viable.json, found.json, board-starters.json | m3 | h-n5-k8-24000-2500 → **h-n5-k8-23500-2500** | stage-1 propMass 24000 → 23500 | real FPA miss: γ −0.212°, 224.4/175.6 km | 205.20/194.80 km, γ 0.045°, 5/5 |
| campaign-viable.json | m3 | m3-eight | pitchKickDeg 8 → 7 | real FPA miss | 200.04/199.96 km, γ 0.000° |
| campaign-viable.json | m5 | n9-400000-k7-u40000 → **n9-400000-k8-u40000** | pitchKickDeg 7 → 8 | prop-depletion at 399.9 km | 400.30/399.70 km, γ 0.003° |
| campaign-winners.json, found.json, board-starters.json | m12 | kh12-win | pitchKickDeg 8 → 6 (id has no kick) | real FPA miss: γ 0.627° batch / 0.625° live, 676.3/523.7 km against 570-630 | 600.28/599.72 km, γ −0.002°, 5/5 |
| contracts-viable.json | flock-leo200-k0 | flock-leo200-k0-h-n5-k8-24000-2500 → **…-23500-2500** | stage-1 propMass 24000 → 23500 | same as m3 h-n5 (224.4/175.6) | passes 5/5 |
| contracts-viable.json | tern-leo600-k3 | tern-leo600-k3-n7-270000-k8-u40000 | pitchKickDeg 8 → 6 | real FPA miss: γ 1.246°, 751.8/448.2 | 600.4/599.6, γ −0.004° |
| contracts-viable.json | tern-leo600-k3 | …-n7-270000-k8-u40000-t3 | pitchKickDeg 8 → 6 | γ 1.564°, 790.5/409.5 | 600.5/599.5, γ −0.004° |
| contracts-viable.json | tern-sso-k3 | tern-sso-k3-n5-200000-k6 | pitchKickDeg 6 → 4 | γ 0.421°, 600.9/499.1 | 552.1/547.9, γ 0.017° |
| contracts-viable.json | tern-sso-k9 | …-n7-270000-k8-u40000-t3-t3 | pitchKickDeg 8 → 5 | γ 1.811° (seed 1 4/5) | 556.2/543.8, γ 0.052°, 5/5 |
| tests/vectors/prediction/launches.txt | — | presets | regenerated (`KESTREL_WRITE_PREDICT=1`) | energy cutoff | scout pass → fail (443.1/356.8); kid orbit 657.0/190.7 → suborbital 656.7/−256.8; rest ±0.1 km |

Kept: m1b at **705 kg**. It flies 10.000 km under (b); 645 kg peaks at 9.07 km and fails. KH-1 starter at **515 kg** (9.786 km).
Contract ids keep their old kick label, so the seeded schedules are unchanged. m12 kh12-win has no kick in its id.
Tried and reverted: m17 → the found.json 4-engine kh17-win. It passes the batch, but its liftoff TWR is 0.82, so the designer schema blocks it (NAV02).

## Hop peaks per seed (km; batch 0.2 s / live 1/120 s)

| Flight | s1 | s2 | s3 | s4 | s5 | live s1 | g batch/live |
|---|---|---|---|---|---|---|---|
| KH-1 starter (515 kg) | 9.786 | 9.786 | 9.786 | 9.786 | 9.786 | 9.787 | 6.00/6.00 |
| KH-2 starter | 97.549 | 97.549 | 97.549 | 97.549 | 97.549 | 97.669 | 6.90/6.92 |
| m1 m1a-520 | 10.044 | 10.044 | 10.044 | 10.044 | 10.044 | 10.044 | 3.27/3.27 |
| m1 m1b-705 | 10.000 | 10.000 | 10.000 | 10.000 | 10.000 | 10.004 | 4.92/4.92 |
| m1 m1p-360 | 9.926 | 9.926 | 9.926 | 9.926 | 9.926 | 9.926 | 2.18/2.18 |
| m2 m2a-2500 | 99.827 | 99.827 | 99.827 | 99.827 | 99.827 | 99.852 | 5.32/5.34 |
| m2 m2b-2600 | 101.017 | 101.017 | 101.017 | 101.017 | 101.017 | 101.027 | 5.41/5.41 |
| m2 m2f-1600 | 100.098 | 100.098 | 100.098 | 100.098 | 100.098 | 100.156 | 5.27/5.28 |

#316: the KH-2 starter pulls **6.90 g batch / 6.92 g live** (limit 6 g). Not changed here.

## Verdict flips vs origin/main: 90 flights (75 pass→fail, 15 fail→pass), out of 530

| Source | Flip | Flights | Designs |
|---|---|---|---|
| board-fly | pass→fail | 15 | m10 b9-p340-m83-u33-s14, m17 kh17-win, m19 kh19-win |
| campaign-viable | pass→fail | 20 | m7 n9-p260-m80-u28, m10 b8-p340-m82-u33-s14, m10 b10-p340-m83-u33-s14, m10 b9-p340-m83-u33-s14 |
| campaign-winners | pass→fail | 15 | m10 b9-p340-m83-u33-s14, m17 kh17-win, m19 kh19-win |
| found | pass→fail | 10 | m10 b9-p340-m83-u33-s14, m19 kh19-win |
| starter | fail→pass | 15 | m5 n8-360000-k8-u30000, m12 kh12-win, m14 kh14-win |
| starter | pass→fail | 15 | m10 b9-p340-m83-u33-s14, m17 kh17-win, m19 kh19-win |

Every pass→fail is the prop-depletion class below. The fail→pass flips are raw board starters m5, m12 and m14, which now fly the band without their board edit. On main they overshot under the perigee cutoff (1898, 649 and 1059 km apoapsis).

## Real misses (γ at burnout or cutoff; batch / live)

### A. Prop-depletion on ellipse and C3 targets: no clean retune; needs a Physics ruling
These designs won on main through the "delta-v" criterion: a 200 km perigee plus an on-paper Δv budget. Under ruling 2 they keep burning toward GTO or TLI energy and run dry a few hundred metres under the 200 km aim. PEG-lite approaches the aim from below. `FailureMonitor.OnEmpty` then calls prop-depletion, because the last stage empties below the aim altitude. None of them can actually reach the target energy, and that is real performance. Kick, kick altitude, turn shape, upper propellant and engine sweeps found no pass for m10 or m19, and the campaign search finds no m10 or m19 design. The delta-v winners that still pass (m7 n5, burnout 200.89 km; m15, 200.02 km) pass on the same knife-edge.

| Flight | Burnout alt km | Apo/peri km (batch) | Apo km live | SMA km | γ batch / live |
|---|---|---|---|---|---|
| **m10 winner** b9-p340-m83-u33-s14 (also starter, board, found) | 199.718 | 489.801/199.715 | 498.555 | 6722.895 | 0.008° / 0.007° |
| **m17 winner** kh17-win (also starter, board) | 199.772 | 57323.653/199.771 | 57515.737 | 35139.849 | 0.020° / 0.020° |
| **m19 winner** kh19-win (also starter, board, found) | 199.755 | 975.004/199.745 | 983.790 | 6965.511 | 0.023° / 0.023° |
| m7 n9-p260-m80-u28 (campaign-viable) | 199.769 | 7083.999/199.763 | 7087.531 | 10020.018 | -0.037° / -0.038° |
| m10 b8-p340-m82-u33-s14 (campaign-viable) | 199.723 | 207.477/199.361 | 217.430 | 6581.556 | -0.015° / -0.015° |
| m10 b9-p340-m83-u33-s14 (campaign-viable) | 199.718 | 519.335/199.715 | 527.938 | 6737.662 | 0.008° / 0.008° |
| m10 b10-p340-m83-u33-s14 (campaign-viable) | 199.737 | 715.023/199.727 | 721.315 | 6835.512 | 0.019° / 0.018° |

Contracts in the same class: comsat-gto-k3 (all 3 designs, 3606/5061/4498 × 199.7-199.8 km), comsat-gto-k6 (all 3, 2060/2447/3466 × 199.9 km), lark-tli-k6 n6 and n8 (28044/40449 × 199.8-199.9 km). γ at burnout is 0.017-0.027°. Those three cells drop below three viable designs.

Options for Physics, none applied: (i) for ellipse and C3 targets, cut off at the aim-perigee parking orbit and keep the delta-v criterion; (ii) don't call prop-depletion once the orbit's perigee is at the aim; (iii) accept these as misses and replace the m10, m17 and m19 goldens with new searched designs, which needs bigger upper stages.

### B. FPA misses that remain
| Flight | Seeds | Batch apo/peri km | γ at cutoff | Cause |
|---|---|---|---|---|
| contract finch-leo400-k3-c9-420-k7-u38 | 2 | 657.8/142.2 | 1.830° | seeded stage-1 engine-out: weak upper stage cuts at 260 km still climbing; kick 4-8 sweep: no 5/5 |
| contract pantry-leo400-k3-c8-360-k7-u35 | 3 | 637.7/162.3 | 1.842° | same; on the same seed it was prop-depletion before the engine-out fix |
| preset scout (free build, leo400) | predict | 443.1/356.8 | −0.108° (cutoff at 441 km) | legacy PEG-lite overshoots the 400 km altitude before target energy |
| preset kid (free build) | predict | 656.7/−256.8 | 0.069° (cutoff at 657 km) | legacy guidance lofts far above the 200 km target before target energy |

Raw (unedited) board starters that miss, as intended: m3 strength (γ −1.104°), m4 fix (−0.979°), m6 strength (0.104°), m8 strength (0.070°), m16 strength (0.016°). Their board edits pass.

## Orbital batch vs live, and step size (seed 1, every golden winner)

KH-6 (m6 t6-p300-m76-u26) at 0.2 s: 203.518/196.482 km, SMA 6578.137, against live 203.108/196.892/6578.137. That is 0.20% apo, 0.41 km peri and 0.0000% SMA, inside the named tolerances, so no 0.05 s step is needed.
| KH-6 (m6 t6-p300-m76-u26), 0.2 s | Batch apo | Live apo | Δapo | Batch peri | Live peri | Δperi | ΔSMA | In tolerance |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| main (perigee cutoff, snapped) | 207.154 | 202.697 | 2.199% | 200.303 | 192.770 | 7533 m | 0.0912% | no |
| a9ec49c (perigee cutoff, located) | 203.071 | 202.668 | 0.199% | 192.238 | 192.232 | 6 m | 0.0031% | yes |
| **this commit (energy cutoff)** | 203.518 | 203.108 | 0.202% | 196.482 | 196.892 | 410 m | 0.0000% | **yes** |

| Worst case m12 kh12-win, 0.2 s | Batch apo | Live apo | Δapo | Δperi | ΔSMA | γ batch/live |
|---|---:|---:|---:|---:|---:|---|
| a9ec49c (perigee cutoff, located; kick 8) | 602.240 | 905.397 | 33.48% | 689 m | 2.12% | — |
| energy cutoff, original kick 8 (real FPA miss) | 676.37 | 676.07 | 0.04% | — | 0.0000% | 0.627° / 0.625° |
| **this commit (kick 6)** | 600.279 | 600.281 | 0.000% | 2 m | 0.0000% | −0.002° / −0.002° |

Under the energy cutoff, the worst orbital winner that reaches orbit is m7 n5 (burnout-limited GTO): 0.40% apo and 0.09% SMA at 0.2 s, inside tolerance.

| Mission | Design | dt | outcome | apo km | peri km | SMA km | Δapo vs live | Δperi vs live | ΔSMA vs live | ms |
|---|---|---|---|---|---|---|---|---|---|---|
| m1 | m1a-520 | 0.2 | suborbital | 10.044 | -6367.523 | 3200.818 | 0.000% | — | — | 1 |
| m1 | m1a-520 | 0.1 | suborbital | 10.044 | -6367.499 | 3200.901 | 0.000% | — | — | 1 |
| m1 | m1a-520 | 0.05 | suborbital | 10.044 | -6367.486 | 3200.943 | 0.000% | — | — | 3 |
| m1 | m1a-520 | 0.00833 | suborbital | 10.044 | -6367.476 | 3200.978 | 0.000% | — | — | 21 |
| m2 | m2a-2500 | 0.2 | suborbital | 99.827 | -6364.713 | 3247.658 | 0.025% | — | — | 4 |
| m2 | m2a-2500 | 0.1 | suborbital | 99.841 | -6364.708 | 3247.704 | 0.011% | — | — | 8 |
| m2 | m2a-2500 | 0.05 | suborbital | 99.848 | -6364.708 | 3247.726 | 0.004% | — | — | 16 |
| m2 | m2a-2500 | 0.00833 | suborbital | 99.852 | -6364.707 | 3247.744 | 0.000% | — | — | 104 |
| m3 | h-n5-k8-23500-2500 | 0.2 | orbit | 205.199 | 194.801 | 6578.137 | 0.000% | 1 m | 0.0000% | 32 |
| m3 | h-n5-k8-23500-2500 | 0.1 | orbit | 205.198 | 194.802 | 6578.137 | 0.000% | 0 m | 0.0000% | 59 |
| m3 | h-n5-k8-23500-2500 | 0.05 | orbit | 205.198 | 194.802 | 6578.137 | 0.000% | 0 m | 0.0000% | 118 |
| m3 | h-n5-k8-23500-2500 | 0.00833 | orbit | 205.198 | 194.802 | 6578.137 | 0.000% | 0 m | 0.0000% | 731 |
| m4 | n5-200000-k6 | 0.2 | orbit | 504.304 | 495.696 | 6878.137 | 0.001% | 7 m | 0.0000% | 11 |
| m4 | n5-200000-k6 | 0.1 | orbit | 504.301 | 495.699 | 6878.137 | 0.001% | 4 m | 0.0000% | 23 |
| m4 | n5-200000-k6 | 0.05 | orbit | 504.299 | 495.701 | 6878.137 | 0.000% | 2 m | 0.0000% | 45 |
| m4 | n5-200000-k6 | 0.00833 | orbit | 504.297 | 495.703 | 6878.137 | 0.000% | 0 m | 0.0000% | 277 |
| m5 | n8-360000-k8-u30000 | 0.2 | orbit | 410.695 | 389.305 | 6778.137 | 0.022% | 89 m | 0.0000% | 15 |
| m5 | n8-360000-k8-u30000 | 0.1 | orbit | 410.667 | 389.333 | 6778.137 | 0.015% | 61 m | 0.0000% | 31 |
| m5 | n8-360000-k8-u30000 | 0.05 | orbit | 410.677 | 389.323 | 6778.137 | 0.017% | 71 m | 0.0000% | 61 |
| m5 | n8-360000-k8-u30000 | 0.00833 | orbit | 410.606 | 389.394 | 6778.137 | 0.000% | 0 m | 0.0000% | 373 |
| m6 | t6-p300-m76-u26 | 0.2 | orbit | 203.518 | 196.482 | 6578.137 | 0.202% | 410 m | 0.0000% | 41 |
| m6 | t6-p300-m76-u26 | 0.1 | orbit | 203.281 | 196.719 | 6578.137 | 0.085% | 173 m | 0.0000% | 79 |
| m6 | t6-p300-m76-u26 | 0.05 | orbit | 203.165 | 196.835 | 6578.137 | 0.028% | 57 m | 0.0000% | 161 |
| m6 | t6-p300-m76-u26 | 0.00833 | orbit | 203.108 | 196.892 | 6578.137 | 0.000% | 0 m | 0.0000% | 951 |
| m7 | n5-p260-m80-u28 | 0.2 | orbit | 3821.910 | 200.892 | 8389.538 | 0.395% | 5 m | 0.0904% | 42 |
| m7 | n5-p260-m80-u28 | 0.1 | orbit | 3831.826 | 200.892 | 8394.496 | 0.137% | 5 m | 0.0313% | 83 |
| m7 | n5-p260-m80-u28 | 0.05 | orbit | 3834.571 | 200.895 | 8395.870 | 0.065% | 2 m | 0.0150% | 167 |
| m7 | n5-p260-m80-u28 | 0.00833 | orbit | 3837.083 | 200.897 | 8397.127 | 0.000% | 0 m | 0.0000% | 1019 |
| m8 | c8-360-k7-u35 | 0.2 | orbit | 400.789 | 399.211 | 6778.137 | 0.002% | 8 m | 0.0000% | 16 |
| m8 | c8-360-k7-u35 | 0.1 | orbit | 400.785 | 399.215 | 6778.137 | 0.001% | 4 m | 0.0000% | 31 |
| m8 | c8-360-k7-u35 | 0.05 | orbit | 400.779 | 399.221 | 6778.137 | 0.000% | 2 m | 0.0000% | 63 |
| m8 | c8-360-k7-u35 | 0.00833 | orbit | 400.781 | 399.219 | 6778.137 | 0.000% | 0 m | 0.0000% | 379 |
| m9 | n4-p260-m100-u28 | 0.2 | orbit | 385644.400 | 199.768 | 199300.221 | 0.000% | 1 m | 0.0000% | 49 |
| m9 | n4-p260-m100-u28 | 0.1 | orbit | 385644.400 | 199.767 | 199300.221 | 0.000% | 0 m | 0.0000% | 103 |
| m9 | n4-p260-m100-u28 | 0.05 | orbit | 385644.401 | 199.767 | 199300.221 | 0.000% | 0 m | 0.0000% | 195 |
| m9 | n4-p260-m100-u28 | 0.00833 | orbit | 385644.401 | 199.767 | 199300.221 | 0.000% | 0 m | 0.0000% | 1164 |
| m10 | b9-p340-m83-u33-s14 | 0.2 | failure | 489.801 | 199.715 | 6722.895 | 1.756% | 0 m | 0.0651% | 108 |
| m10 | b9-p340-m83-u33-s14 | 0.1 | failure | 494.427 | 199.715 | 6725.208 | 0.828% | 0 m | 0.0307% | 212 |
| m10 | b9-p340-m83-u33-s14 | 0.05 | failure | 496.745 | 199.715 | 6726.367 | 0.363% | 0 m | 0.0135% | 424 |
| m10 | b9-p340-m83-u33-s14 | 0.00833 | failure | 498.555 | 199.715 | 6727.272 | 0.000% | 0 m | 0.0000% | 2566 |
| m11 | kh11-win | 0.2 | orbit | 550.412 | 549.588 | 6928.137 | 0.000% | 0 m | 0.0000% | 12 |
| m11 | kh11-win | 0.1 | orbit | 550.412 | 549.588 | 6928.137 | 0.000% | 0 m | 0.0000% | 25 |
| m11 | kh11-win | 0.05 | orbit | 550.411 | 549.589 | 6928.137 | 0.000% | 1 m | 0.0000% | 48 |
| m11 | kh11-win | 0.00833 | orbit | 550.412 | 549.588 | 6928.137 | 0.000% | 0 m | 0.0000% | 285 |
| m12 | kh12-win | 0.2 | orbit | 600.279 | 599.721 | 6978.137 | 0.000% | 2 m | 0.0000% | 12 |
| m12 | kh12-win | 0.1 | orbit | 600.281 | 599.719 | 6978.137 | 0.000% | 0 m | 0.0000% | 23 |
| m12 | kh12-win | 0.05 | orbit | 600.281 | 599.719 | 6978.137 | 0.000% | 0 m | 0.0000% | 47 |
| m12 | kh12-win | 0.00833 | orbit | 600.281 | 599.719 | 6978.137 | 0.000% | 0 m | 0.0000% | 274 |
| m13 | kh13-win | 0.2 | orbit | 550.063 | 549.937 | 6928.137 | 0.001% | 4 m | 0.0000% | 10 |
| m13 | kh13-win | 0.1 | orbit | 550.065 | 549.935 | 6928.137 | 0.000% | 2 m | 0.0000% | 21 |
| m13 | kh13-win | 0.05 | orbit | 550.067 | 549.933 | 6928.137 | 0.000% | 0 m | 0.0000% | 40 |
| m13 | kh13-win | 0.00833 | orbit | 550.067 | 549.933 | 6928.137 | 0.000% | 0 m | 0.0000% | 261 |
| m14 | kh14-win | 0.2 | orbit | 400.422 | 399.578 | 6778.137 | 0.000% | 2 m | 0.0000% | 16 |
| m14 | kh14-win | 0.1 | orbit | 400.423 | 399.577 | 6778.137 | 0.001% | 3 m | 0.0000% | 30 |
| m14 | kh14-win | 0.05 | orbit | 400.418 | 399.582 | 6778.137 | 0.000% | 2 m | 0.0000% | 65 |
| m14 | kh14-win | 0.00833 | orbit | 400.420 | 399.580 | 6778.137 | 0.000% | 0 m | 0.0000% | 365 |
| m15 | kh15-win | 0.2 | orbit | 4373.466 | 200.024 | 8664.882 | 0.291% | 4 m | 0.0736% | 45 |
| m15 | kh15-win | 0.1 | orbit | 4380.296 | 200.026 | 8668.298 | 0.135% | 2 m | 0.0343% | 87 |
| m15 | kh15-win | 0.05 | orbit | 4383.647 | 200.027 | 8669.974 | 0.059% | 1 m | 0.0149% | 175 |
| m15 | kh15-win | 0.00833 | orbit | 4386.233 | 200.028 | 8671.268 | 0.000% | 0 m | 0.0000% | 1075 |
| m16 | kh16-win | 0.2 | orbit | 503.668 | 496.332 | 6878.137 | 0.001% | 3 m | 0.0000% | 13 |
| m16 | kh16-win | 0.1 | orbit | 503.668 | 496.332 | 6878.137 | 0.001% | 3 m | 0.0000% | 28 |
| m16 | kh16-win | 0.05 | orbit | 503.672 | 496.328 | 6878.137 | 0.000% | 1 m | 0.0000% | 53 |
| m16 | kh16-win | 0.00833 | orbit | 503.671 | 496.329 | 6878.137 | 0.000% | 0 m | 0.0000% | 328 |
| m17 | kh17-win | 0.2 | failure | 57323.653 | 199.771 | 35139.849 | 0.334% | 1 m | 0.2726% | 64 |
| m17 | kh17-win | 0.1 | failure | 57441.079 | 199.771 | 35198.562 | 0.130% | 1 m | 0.1059% | 128 |
| m17 | kh17-win | 0.05 | failure | 57461.741 | 199.770 | 35208.892 | 0.094% | 0 m | 0.0766% | 247 |
| m17 | kh17-win | 0.00833 | failure | 57515.737 | 199.770 | 35235.890 | 0.000% | 0 m | 0.0000% | 1488 |
| m18 | kh18-win | 0.2 | orbit | 1980046.182 | 199.753 | 996501.105 | 0.000% | 1 m | 0.0000% | 50 |
| m18 | kh18-win | 0.1 | orbit | 1980046.183 | 199.752 | 996501.105 | 0.000% | 0 m | 0.0000% | 96 |
| m18 | kh18-win | 0.05 | orbit | 1980046.183 | 199.752 | 996501.105 | 0.000% | 0 m | 0.0000% | 192 |
| m18 | kh18-win | 0.00833 | orbit | 1980046.183 | 199.752 | 996501.105 | 0.000% | 0 m | 0.0000% | 1176 |
| m19 | kh19-win | 0.2 | failure | 975.004 | 199.745 | 6965.511 | 0.893% | 1 m | 0.0630% | 89 |
| m19 | kh19-win | 0.1 | failure | 979.287 | 199.744 | 6967.653 | 0.458% | 0 m | 0.0323% | 177 |
| m19 | kh19-win | 0.05 | failure | 981.855 | 199.744 | 6968.936 | 0.197% | 0 m | 0.0139% | 341 |
| m19 | kh19-win | 0.00833 | failure | 983.790 | 199.744 | 6969.904 | 0.000% | 0 m | 0.0000% | 2056 |
| m20 | kh20-win | 0.2 | orbit | 3175847.421 | 199.839 | 1594401.767 | 0.000% | 3 m | 0.0000% | 48 |
| m20 | kh20-win | 0.1 | orbit | 3175847.423 | 199.838 | 1594401.767 | 0.000% | 2 m | 0.0000% | 96 |
| m20 | kh20-win | 0.05 | orbit | 3175847.424 | 199.837 | 1594401.767 | 0.000% | 1 m | 0.0000% | 193 |
| m20 | kh20-win | 0.00833 | orbit | 3175847.424 | 199.836 | 1594401.767 | 0.000% | 0 m | 0.0000% | 1162 |

Total runtime, 20 winners, seed 1: 0.2 s = 678 ms, 0.1 s = 1341 ms, 0.05 s = 2654 ms, 0.00833 s = 16055 ms

## Test expectations changed (none loosened)

- `AscentDecisionTests.AutoAssist_ThrottlesInTheGreen…`: a design with a 60% max-Q throttle now flies that cap through its own guidance, so the director command is ThrottleScale 1 and EngageMaxQCap 0 (was ThrottleScale 0.60). Ruling 4.
- `AscentDecisionTests` (player tap): the tap engages the design law at the offered 70%. EngageMaxQCap is 0.70 and ThrottleScale 1 (was ThrottleScale 0.70). `SuppressDesignThrottle` is gone.
- `Kh1FirstHopTests`: the `SuppressDesignThrottle` assert becomes EngageMaxQCap 0 and ThrottleScale 1 (the design cap is the only law), and the hand-flown loop drops its suppression branch.
- `SharedDirectorTests`: the KH-1 starter is 515 kg (was 464) and m1b ≥ 9.7 km is added. Designer vs bare is now **equal** (was "not equal"), per ruling 4. The orbit batch-vs-live check covers every winner and every board starter (36 flights, was 18 winners). The perigee-cutoff location test is replaced by an energy-cutoff test (ε within 1e-9, SMA within 1 m at 0.2 s and 1/120 s; ellipse and C3 energies; raw SimOptions keep the parity path). New tests: TheDesignMaxQThrottle_IsTheOnlyMaxQLaw_ForEveryHop and TheFuelImbalancePenalty_ThrottlesOnlyItsStage_UntilItSeparates.
- `ReadoutV2Tests.ReferenceMaxQ_MatchesFullHeadlessPeak`: compares against the designer's directed flight, not a parity `Stack.SimInputs` run. `StabilityBand…`: the bare stock vehicle still checks at the documented 73.01 kPa. The readout now checks at 70.71 kPa, because the predict schedule's stage-1 fuel imbalance runs that stage at 97% until separation.
- `CampaignFlightLoopTests.BoardStarters_LoadFromThePack…`: the m3 board starter name is now h-n5-k8-23500-2500.
- `tests/vectors/prediction/launches.txt`: regenerated (see table above).

## Core tests still failing (21 of 765), by cause

- **Prop-depletion class (A), 12 tests:** CampaignWinners_FlyPassOnEverySeed, CampaignViableDesigns_WinM1ThroughM10, EveryMission_HasThreeViableDesigns, EveryCell_AndEveryOffer_IsViable (also class B), DesignFirstRocketsAreReadyAfterThePredictRun, GoldenWinnersHaveNoCertainFailureTheyDoNotHave, EveryOrbitalWinner_LiveStep_ReachesOrbit, NAV01_DesignFirstRocket…, NAV02_GoldenWinners…, BatchAndLivePad_Agree_Orbits… (m10, m17 and m19 only), DefaultPlayer_EveryGoldenWinnerAndStarter… (m10, m17 and m19 only), CampaignWinners_HideEachReleasedBody… (15 of 18).
- **Board premise, 1 test:** BoardStarter_FixItOrOneStrengthEdit_WinsEveryMission. The raw m5 (fix), m12 (strength) and m14 (strength) starters now win with no edit, so they are not near misses. **Arcade decision:** set those rows to mode "fly", or re-pick near-miss starters. Not changed here.
- **Preset scout misses under the energy cutoff (B), 4 tests:** UnchangedDesign_HasAnEmptyDiff, PropellantCut_FlagsAFailedPrediction…, BrokenPrecondition_FlagsCausality…, UpperEngineSwap_DropsRestart…. The fix is a legacy-guidance or preset retune, which Physics needs to decide.
- **Fix it knife-edge, 4 tests:** FixIt_WeakAirframe…(14) and (16) (two steps under the chosen strength now also fly), FixIt_ThatCannotFixTheFlight_SaysSo…, ReportFixIt_RanOutOfFuel…. The max-Q load and cutoff changed under rulings 2 and 4. Not investigated further.

## Appendix: every winner and starter, per seed (new; old verdict)

| Source | Mission | Design | Seed | old | new | batch apo/peri km | live apo/peri km | γ batch/live | criterion / failure |
|---|---|---|---|---|---|---|---|---|---|
| campaign-winners | m1 | m1a-520 | 1 | P | P | 10.044/-6367.523 | 10.044/-6367.476 | 37.695/37.695 | altitude |
| campaign-winners | m1 | m1a-520 | 2 | P | P | 10.044/-6367.523 | 10.044/-6367.476 | 37.695/37.695 | altitude |
| campaign-winners | m1 | m1a-520 | 3 | P | P | 10.044/-6367.523 | 10.044/-6367.476 | 37.695/37.695 | altitude |
| campaign-winners | m1 | m1a-520 | 4 | P | P | 10.044/-6367.523 | 10.044/-6367.476 | 37.695/37.695 | altitude |
| campaign-winners | m1 | m1a-520 | 5 | P | P | 10.044/-6367.523 | 10.044/-6367.476 | 37.695/37.695 | altitude |
| starter | m1 | armed hopper | 1 | P | P | 9.786/-6368.366 | 9.787/-6368.325 | 40.730/40.749 | altitude |
| starter | m1 | armed hopper | 2 | P | P | 9.786/-6368.366 | 9.787/-6368.325 | 40.730/40.749 | altitude |
| starter | m1 | armed hopper | 3 | P | P | 9.786/-6368.366 | 9.787/-6368.325 | 40.730/40.749 | altitude |
| starter | m1 | armed hopper | 4 | P | P | 9.786/-6368.366 | 9.787/-6368.325 | 40.730/40.749 | altitude |
| starter | m1 | armed hopper | 5 | P | P | 9.786/-6368.366 | 9.787/-6368.325 | 40.730/40.749 | altitude |
| campaign-winners | m2 | m2a-2500 | 1 | P | P | 99.827/-6364.713 | 99.852/-6364.707 | 66.994/66.993 | altitude |
| campaign-winners | m2 | m2a-2500 | 2 | P | P | 99.827/-6364.713 | 99.852/-6364.707 | 66.994/66.993 | altitude |
| campaign-winners | m2 | m2a-2500 | 3 | P | P | 99.827/-6364.713 | 99.852/-6364.707 | 66.994/66.993 | altitude |
| campaign-winners | m2 | m2a-2500 | 4 | P | P | 99.827/-6364.713 | 99.852/-6364.707 | 66.994/66.993 | altitude |
| campaign-winners | m2 | m2a-2500 | 5 | P | P | 99.827/-6364.713 | 99.852/-6364.707 | 66.994/66.993 | altitude |
| starter | m2 | armed hopper | 1 | P | P | 97.549/-6364.892 | 97.669/-6364.894 | 67.371/67.392 | altitude |
| starter | m2 | armed hopper | 2 | P | P | 97.549/-6364.892 | 97.669/-6364.894 | 67.371/67.392 | altitude |
| starter | m2 | armed hopper | 3 | P | P | 97.549/-6364.892 | 97.669/-6364.894 | 67.371/67.392 | altitude |
| starter | m2 | armed hopper | 4 | P | P | 97.549/-6364.892 | 97.669/-6364.894 | 67.371/67.392 | altitude |
| starter | m2 | armed hopper | 5 | P | P | 97.549/-6364.892 | 97.669/-6364.894 | 67.371/67.392 | altitude |
| board-strength | m3 | h-n5-k8-23500-2500 | 1 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| board-strength | m3 | h-n5-k8-23500-2500 | 2 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| board-strength | m3 | h-n5-k8-23500-2500 | 3 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| board-strength | m3 | h-n5-k8-23500-2500 | 4 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| board-strength | m3 | h-n5-k8-23500-2500 | 5 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| campaign-winners | m3 | h-n5-k8-23500-2500 | 1 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| campaign-winners | m3 | h-n5-k8-23500-2500 | 2 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| campaign-winners | m3 | h-n5-k8-23500-2500 | 3 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| campaign-winners | m3 | h-n5-k8-23500-2500 | 4 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| campaign-winners | m3 | h-n5-k8-23500-2500 | 5 | P | P | 205.199/194.801 | 205.198/194.802 | 0.045/0.045 | altitude |
| starter | m3 | h-n5-k8-23500-2500 | 1 | F | F | 327.694/72.306 | 327.703/72.297 | -1.104/-1.104 | altitude |
| starter | m3 | h-n5-k8-23500-2500 | 2 | F | F | 327.694/72.306 | 327.703/72.297 | -1.104/-1.104 | altitude |
| starter | m3 | h-n5-k8-23500-2500 | 3 | F | F | 327.694/72.306 | 327.703/72.297 | -1.104/-1.104 | altitude |
| starter | m3 | h-n5-k8-23500-2500 | 4 | F | F | 327.694/72.306 | 327.703/72.297 | -1.104/-1.104 | altitude |
| starter | m3 | h-n5-k8-23500-2500 | 5 | F | F | 327.694/72.306 | 327.703/72.297 | -1.104/-1.104 | altitude |
| board-fix | m4 | n5-200000-k6 | 1 | P | P | 503.386/496.614 | 503.478/496.522 | 0.028/0.029 | altitude |
| board-fix | m4 | n5-200000-k6 | 2 | P | P | 503.386/496.614 | 503.478/496.522 | 0.028/0.029 | altitude |
| board-fix | m4 | n5-200000-k6 | 3 | P | P | 503.386/496.614 | 503.478/496.522 | 0.028/0.029 | altitude |
| board-fix | m4 | n5-200000-k6 | 4 | P | P | 503.386/496.614 | 503.478/496.522 | 0.028/0.029 | altitude |
| board-fix | m4 | n5-200000-k6 | 5 | P | P | 503.386/496.614 | 503.478/496.522 | 0.028/0.029 | altitude |
| campaign-winners | m4 | n5-200000-k6 | 1 | P | P | 504.304/495.696 | 504.297/495.703 | 0.036/0.036 | altitude |
| campaign-winners | m4 | n5-200000-k6 | 2 | P | P | 504.304/495.696 | 504.297/495.703 | 0.036/0.036 | altitude |
| campaign-winners | m4 | n5-200000-k6 | 3 | P | P | 504.304/495.696 | 504.297/495.703 | 0.036/0.036 | altitude |
| campaign-winners | m4 | n5-200000-k6 | 4 | P | P | 504.304/495.696 | 504.297/495.703 | 0.036/0.036 | altitude |
| campaign-winners | m4 | n5-200000-k6 | 5 | P | P | 504.304/495.696 | 504.297/495.703 | 0.036/0.036 | altitude |
| starter | m4 | n5-200000-k6 | 1 | F | F | 617.511/382.489 | 618.386/381.614 | -0.979/-0.986 | altitude |
| starter | m4 | n5-200000-k6 | 2 | F | F | 617.511/382.489 | 618.386/381.614 | -0.979/-0.986 | altitude |
| starter | m4 | n5-200000-k6 | 3 | F | F | 617.511/382.489 | 618.386/381.614 | -0.979/-0.986 | altitude |
| starter | m4 | n5-200000-k6 | 4 | F | F | 617.511/382.489 | 618.386/381.614 | -0.979/-0.986 | altitude |
| starter | m4 | n5-200000-k6 | 5 | F | F | 617.511/382.489 | 618.386/381.614 | -0.979/-0.986 | altitude |
| board-fix | m5 | n8-360000-k8-u30000 | 1 | P | P | 402.869/397.131 | 402.861/397.139 | 0.024/0.024 | altitude |
| board-fix | m5 | n8-360000-k8-u30000 | 2 | P | P | 402.869/397.131 | 402.861/397.139 | 0.024/0.024 | altitude |
| board-fix | m5 | n8-360000-k8-u30000 | 3 | P | P | 402.869/397.131 | 402.861/397.139 | 0.024/0.024 | altitude |
| board-fix | m5 | n8-360000-k8-u30000 | 4 | P | P | 402.869/397.131 | 402.861/397.139 | 0.024/0.024 | altitude |
| board-fix | m5 | n8-360000-k8-u30000 | 5 | P | P | 402.869/397.131 | 402.861/397.139 | 0.024/0.024 | altitude |
| campaign-winners | m5 | n8-360000-k8-u30000 | 1 | P | P | 410.695/389.305 | 410.606/389.394 | -0.090/-0.090 | altitude |
| campaign-winners | m5 | n8-360000-k8-u30000 | 2 | P | P | 410.695/389.305 | 410.606/389.394 | -0.090/-0.090 | altitude |
| campaign-winners | m5 | n8-360000-k8-u30000 | 3 | P | P | 410.695/389.305 | 410.606/389.394 | -0.090/-0.090 | altitude |
| campaign-winners | m5 | n8-360000-k8-u30000 | 4 | P | P | 410.695/389.305 | 410.606/389.394 | -0.090/-0.090 | altitude |
| campaign-winners | m5 | n8-360000-k8-u30000 | 5 | P | P | 410.695/389.305 | 410.606/389.394 | -0.090/-0.090 | altitude |
| starter | m5 | n8-360000-k8-u30000 | 1 | F | P | 401.511/398.489 | 401.565/398.435 | 0.013/0.013 | altitude |
| starter | m5 | n8-360000-k8-u30000 | 2 | F | P | 401.511/398.489 | 401.565/398.435 | 0.013/0.013 | altitude |
| starter | m5 | n8-360000-k8-u30000 | 3 | F | P | 401.511/398.489 | 401.565/398.435 | 0.013/0.013 | altitude |
| starter | m5 | n8-360000-k8-u30000 | 4 | F | P | 401.511/398.489 | 401.565/398.435 | 0.013/0.013 | altitude |
| starter | m5 | n8-360000-k8-u30000 | 5 | F | P | 401.511/398.489 | 401.565/398.435 | 0.013/0.013 | altitude |
| board-strength | m6 | t6-p300-m76-u26 | 1 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| board-strength | m6 | t6-p300-m76-u26 | 2 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| board-strength | m6 | t6-p300-m76-u26 | 3 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| board-strength | m6 | t6-p300-m76-u26 | 4 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| board-strength | m6 | t6-p300-m76-u26 | 5 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| campaign-winners | m6 | t6-p300-m76-u26 | 1 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| campaign-winners | m6 | t6-p300-m76-u26 | 2 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| campaign-winners | m6 | t6-p300-m76-u26 | 3 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| campaign-winners | m6 | t6-p300-m76-u26 | 4 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| campaign-winners | m6 | t6-p300-m76-u26 | 5 | P | P | 203.518/196.482 | 203.108/196.892 | -0.024/-0.019 | altitude |
| starter | m6 | t6-p300-m76-u26 | 1 | F | F | 212.180/187.820 | 212.656/187.344 | 0.104/0.109 | altitude |
| starter | m6 | t6-p300-m76-u26 | 2 | F | F | 212.180/187.820 | 212.656/187.344 | 0.104/0.109 | altitude |
| starter | m6 | t6-p300-m76-u26 | 3 | F | F | 212.180/187.820 | 212.656/187.344 | 0.104/0.109 | altitude |
| starter | m6 | t6-p300-m76-u26 | 4 | F | F | 212.180/187.820 | 212.656/187.344 | 0.104/0.109 | altitude |
| starter | m6 | t6-p300-m76-u26 | 5 | F | F | 212.180/187.820 | 212.656/187.344 | 0.104/0.109 | altitude |
| board-fly | m7 | n5-p260-m80-u28 | 1 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| board-fly | m7 | n5-p260-m80-u28 | 2 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| board-fly | m7 | n5-p260-m80-u28 | 3 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| board-fly | m7 | n5-p260-m80-u28 | 4 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| board-fly | m7 | n5-p260-m80-u28 | 5 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| campaign-winners | m7 | n5-p260-m80-u28 | 1 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| campaign-winners | m7 | n5-p260-m80-u28 | 2 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| campaign-winners | m7 | n5-p260-m80-u28 | 3 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| campaign-winners | m7 | n5-p260-m80-u28 | 4 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| campaign-winners | m7 | n5-p260-m80-u28 | 5 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| starter | m7 | n5-p260-m80-u28 | 1 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| starter | m7 | n5-p260-m80-u28 | 2 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| starter | m7 | n5-p260-m80-u28 | 3 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| starter | m7 | n5-p260-m80-u28 | 4 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| starter | m7 | n5-p260-m80-u28 | 5 | P | P | 3821.910/200.892 | 3837.083/200.897 | -0.212/-0.212 | delta-v |
| board-strength | m8 | c8-360-k7-u35 | 1 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| board-strength | m8 | c8-360-k7-u35 | 2 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| board-strength | m8 | c8-360-k7-u35 | 3 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| board-strength | m8 | c8-360-k7-u35 | 4 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| board-strength | m8 | c8-360-k7-u35 | 5 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| campaign-winners | m8 | c8-360-k7-u35 | 1 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| campaign-winners | m8 | c8-360-k7-u35 | 2 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| campaign-winners | m8 | c8-360-k7-u35 | 3 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| campaign-winners | m8 | c8-360-k7-u35 | 4 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| campaign-winners | m8 | c8-360-k7-u35 | 5 | P | P | 400.789/399.211 | 400.781/399.219 | 0.007/0.007 | altitude |
| starter | m8 | c8-360-k7-u35 | 1 | F | F | 408.332/391.668 | 408.319/391.681 | 0.070/0.070 | altitude |
| starter | m8 | c8-360-k7-u35 | 2 | F | F | 408.332/391.668 | 408.319/391.681 | 0.070/0.070 | altitude |
| starter | m8 | c8-360-k7-u35 | 3 | F | F | 408.332/391.668 | 408.319/391.681 | 0.070/0.070 | altitude |
| starter | m8 | c8-360-k7-u35 | 4 | F | F | 408.332/391.668 | 408.319/391.681 | 0.070/0.070 | altitude |
| starter | m8 | c8-360-k7-u35 | 5 | F | F | 408.332/391.668 | 408.319/391.681 | 0.070/0.070 | altitude |
| board-fly | m9 | n4-p260-m100-u28 | 1 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| board-fly | m9 | n4-p260-m100-u28 | 2 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| board-fly | m9 | n4-p260-m100-u28 | 3 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| board-fly | m9 | n4-p260-m100-u28 | 4 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| board-fly | m9 | n4-p260-m100-u28 | 5 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| campaign-winners | m9 | n4-p260-m100-u28 | 1 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| campaign-winners | m9 | n4-p260-m100-u28 | 2 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| campaign-winners | m9 | n4-p260-m100-u28 | 3 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| campaign-winners | m9 | n4-p260-m100-u28 | 4 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| campaign-winners | m9 | n4-p260-m100-u28 | 5 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| starter | m9 | n4-p260-m100-u28 | 1 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| starter | m9 | n4-p260-m100-u28 | 2 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| starter | m9 | n4-p260-m100-u28 | 3 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| starter | m9 | n4-p260-m100-u28 | 4 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| starter | m9 | n4-p260-m100-u28 | 5 | P | P | 385644.400/199.768 | 385644.401/199.767 | 0.019/0.018 | c3 |
| board-fly | m10 | b9-p340-m83-u33-s14 | 1 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| board-fly | m10 | b9-p340-m83-u33-s14 | 2 | P | F | 502.464/199.715 | 511.520/199.715 | 0.005/0.005 | prop-depletion |
| board-fly | m10 | b9-p340-m83-u33-s14 | 3 | P | F | 502.464/199.715 | 511.520/199.715 | 0.005/0.005 | prop-depletion |
| board-fly | m10 | b9-p340-m83-u33-s14 | 4 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| board-fly | m10 | b9-p340-m83-u33-s14 | 5 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| campaign-winners | m10 | b9-p340-m83-u33-s14 | 1 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| campaign-winners | m10 | b9-p340-m83-u33-s14 | 2 | P | F | 502.464/199.715 | 511.520/199.715 | 0.005/0.005 | prop-depletion |
| campaign-winners | m10 | b9-p340-m83-u33-s14 | 3 | P | F | 502.464/199.715 | 511.520/199.715 | 0.005/0.005 | prop-depletion |
| campaign-winners | m10 | b9-p340-m83-u33-s14 | 4 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| campaign-winners | m10 | b9-p340-m83-u33-s14 | 5 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| starter | m10 | b9-p340-m83-u33-s14 | 1 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| starter | m10 | b9-p340-m83-u33-s14 | 2 | P | F | 502.464/199.715 | 511.520/199.715 | 0.005/0.005 | prop-depletion |
| starter | m10 | b9-p340-m83-u33-s14 | 3 | P | F | 502.464/199.715 | 511.520/199.715 | 0.005/0.005 | prop-depletion |
| starter | m10 | b9-p340-m83-u33-s14 | 4 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| starter | m10 | b9-p340-m83-u33-s14 | 5 | P | F | 489.801/199.715 | 498.555/199.715 | 0.008/0.007 | prop-depletion |
| board-fly | m11 | kh11-win | 1 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| board-fly | m11 | kh11-win | 2 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| board-fly | m11 | kh11-win | 3 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| board-fly | m11 | kh11-win | 4 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| board-fly | m11 | kh11-win | 5 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| campaign-winners | m11 | kh11-win | 1 | P | P | 550.412/549.588 | 550.412/549.588 | -0.003/-0.003 | altitude |
| campaign-winners | m11 | kh11-win | 2 | P | P | 550.412/549.588 | 550.412/549.588 | -0.003/-0.003 | altitude |
| campaign-winners | m11 | kh11-win | 3 | P | P | 550.412/549.588 | 550.412/549.588 | -0.003/-0.003 | altitude |
| campaign-winners | m11 | kh11-win | 4 | P | P | 550.412/549.588 | 550.412/549.588 | -0.003/-0.003 | altitude |
| campaign-winners | m11 | kh11-win | 5 | P | P | 550.412/549.588 | 550.412/549.588 | -0.003/-0.003 | altitude |
| starter | m11 | kh11-win | 1 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| starter | m11 | kh11-win | 2 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| starter | m11 | kh11-win | 3 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| starter | m11 | kh11-win | 4 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| starter | m11 | kh11-win | 5 | P | P | 550.408/549.592 | 550.408/549.592 | 0.003/0.003 | altitude |
| board-strength | m12 | kh12-win | 1 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| board-strength | m12 | kh12-win | 2 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| board-strength | m12 | kh12-win | 3 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| board-strength | m12 | kh12-win | 4 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| board-strength | m12 | kh12-win | 5 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| campaign-winners | m12 | kh12-win | 1 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| campaign-winners | m12 | kh12-win | 2 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| campaign-winners | m12 | kh12-win | 3 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| campaign-winners | m12 | kh12-win | 4 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| campaign-winners | m12 | kh12-win | 5 | P | P | 600.279/599.721 | 600.281/599.719 | -0.002/-0.002 | altitude |
| starter | m12 | kh12-win | 1 | F | P | 600.933/599.067 | 600.919/599.081 | -0.008/-0.008 | altitude |
| starter | m12 | kh12-win | 2 | F | P | 600.933/599.067 | 600.919/599.081 | -0.008/-0.008 | altitude |
| starter | m12 | kh12-win | 3 | F | P | 600.933/599.067 | 600.919/599.081 | -0.008/-0.008 | altitude |
| starter | m12 | kh12-win | 4 | F | P | 600.933/599.067 | 600.919/599.081 | -0.008/-0.008 | altitude |
| starter | m12 | kh12-win | 5 | F | P | 600.933/599.067 | 600.919/599.081 | -0.008/-0.008 | altitude |
| board-fly | m13 | kh13-win | 1 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| board-fly | m13 | kh13-win | 2 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| board-fly | m13 | kh13-win | 3 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| board-fly | m13 | kh13-win | 4 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| board-fly | m13 | kh13-win | 5 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| campaign-winners | m13 | kh13-win | 1 | P | P | 550.063/549.937 | 550.067/549.933 | -0.000/-0.000 | altitude |
| campaign-winners | m13 | kh13-win | 2 | P | P | 550.063/549.937 | 550.067/549.933 | -0.000/-0.000 | altitude |
| campaign-winners | m13 | kh13-win | 3 | P | P | 550.063/549.937 | 550.067/549.933 | -0.000/-0.000 | altitude |
| campaign-winners | m13 | kh13-win | 4 | P | P | 550.063/549.937 | 550.067/549.933 | -0.000/-0.000 | altitude |
| campaign-winners | m13 | kh13-win | 5 | P | P | 550.063/549.937 | 550.067/549.933 | -0.000/-0.000 | altitude |
| starter | m13 | kh13-win | 1 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| starter | m13 | kh13-win | 2 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| starter | m13 | kh13-win | 3 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| starter | m13 | kh13-win | 4 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| starter | m13 | kh13-win | 5 | P | P | 550.916/549.084 | 550.920/549.080 | -0.008/-0.008 | altitude |
| board-strength | m14 | kh14-win | 1 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| board-strength | m14 | kh14-win | 2 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| board-strength | m14 | kh14-win | 3 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| board-strength | m14 | kh14-win | 4 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| board-strength | m14 | kh14-win | 5 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| campaign-winners | m14 | kh14-win | 1 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| campaign-winners | m14 | kh14-win | 2 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| campaign-winners | m14 | kh14-win | 3 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| campaign-winners | m14 | kh14-win | 4 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| campaign-winners | m14 | kh14-win | 5 | P | P | 400.422/399.578 | 400.420/399.580 | 0.004/0.004 | altitude |
| starter | m14 | kh14-win | 1 | F | P | 402.748/397.252 | 402.739/397.261 | 0.023/0.023 | altitude |
| starter | m14 | kh14-win | 2 | F | P | 402.748/397.252 | 402.739/397.261 | 0.023/0.023 | altitude |
| starter | m14 | kh14-win | 3 | F | P | 402.748/397.252 | 402.739/397.261 | 0.023/0.023 | altitude |
| starter | m14 | kh14-win | 4 | F | P | 402.748/397.252 | 402.739/397.261 | 0.023/0.023 | altitude |
| starter | m14 | kh14-win | 5 | F | P | 402.748/397.252 | 402.739/397.261 | 0.023/0.023 | altitude |
| board-fly | m15 | kh15-win | 1 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| board-fly | m15 | kh15-win | 2 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| board-fly | m15 | kh15-win | 3 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| board-fly | m15 | kh15-win | 4 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| board-fly | m15 | kh15-win | 5 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| campaign-winners | m15 | kh15-win | 1 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| campaign-winners | m15 | kh15-win | 2 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| campaign-winners | m15 | kh15-win | 3 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| campaign-winners | m15 | kh15-win | 4 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| campaign-winners | m15 | kh15-win | 5 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| starter | m15 | kh15-win | 1 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| starter | m15 | kh15-win | 2 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| starter | m15 | kh15-win | 3 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| starter | m15 | kh15-win | 4 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| starter | m15 | kh15-win | 5 | P | P | 4373.466/200.024 | 4386.233/200.028 | -0.116/-0.117 | delta-v |
| board-strength | m16 | kh16-win | 1 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| board-strength | m16 | kh16-win | 2 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| board-strength | m16 | kh16-win | 3 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| board-strength | m16 | kh16-win | 4 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| board-strength | m16 | kh16-win | 5 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| campaign-winners | m16 | kh16-win | 1 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| campaign-winners | m16 | kh16-win | 2 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| campaign-winners | m16 | kh16-win | 3 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| campaign-winners | m16 | kh16-win | 4 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| campaign-winners | m16 | kh16-win | 5 | P | P | 503.668/496.332 | 503.671/496.329 | 0.031/0.031 | altitude |
| starter | m16 | kh16-win | 1 | F | F | 501.903/498.097 | 501.879/498.121 | 0.016/0.016 | altitude |
| starter | m16 | kh16-win | 2 | F | F | 501.903/498.097 | 501.879/498.121 | 0.016/0.016 | altitude |
| starter | m16 | kh16-win | 3 | F | F | 501.903/498.097 | 501.879/498.121 | 0.016/0.016 | altitude |
| starter | m16 | kh16-win | 4 | F | F | 501.903/498.097 | 501.879/498.121 | 0.016/0.016 | altitude |
| starter | m16 | kh16-win | 5 | F | F | 501.903/498.097 | 501.879/498.121 | 0.016/0.016 | altitude |
| board-fly | m17 | kh17-win | 1 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| board-fly | m17 | kh17-win | 2 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| board-fly | m17 | kh17-win | 3 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| board-fly | m17 | kh17-win | 4 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| board-fly | m17 | kh17-win | 5 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| campaign-winners | m17 | kh17-win | 1 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| campaign-winners | m17 | kh17-win | 2 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| campaign-winners | m17 | kh17-win | 3 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| campaign-winners | m17 | kh17-win | 4 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| campaign-winners | m17 | kh17-win | 5 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| starter | m17 | kh17-win | 1 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| starter | m17 | kh17-win | 2 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| starter | m17 | kh17-win | 3 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| starter | m17 | kh17-win | 4 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| starter | m17 | kh17-win | 5 | P | F | 57323.653/199.771 | 57515.737/199.770 | 0.020/0.020 | prop-depletion |
| board-fly | m18 | kh18-win | 1 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| board-fly | m18 | kh18-win | 2 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| board-fly | m18 | kh18-win | 3 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| board-fly | m18 | kh18-win | 4 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| board-fly | m18 | kh18-win | 5 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| campaign-winners | m18 | kh18-win | 1 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| campaign-winners | m18 | kh18-win | 2 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| campaign-winners | m18 | kh18-win | 3 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| campaign-winners | m18 | kh18-win | 4 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| campaign-winners | m18 | kh18-win | 5 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| starter | m18 | kh18-win | 1 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| starter | m18 | kh18-win | 2 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| starter | m18 | kh18-win | 3 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| starter | m18 | kh18-win | 4 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| starter | m18 | kh18-win | 5 | P | P | 1980046.182/199.753 | 1980046.183/199.752 | 0.017/0.017 | c3 |
| board-fly | m19 | kh19-win | 1 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| board-fly | m19 | kh19-win | 2 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| board-fly | m19 | kh19-win | 3 | P | F | 960.960/199.750 | 969.254/199.749 | 0.024/0.024 | prop-depletion |
| board-fly | m19 | kh19-win | 4 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| board-fly | m19 | kh19-win | 5 | P | F | 960.960/199.750 | 969.254/199.749 | 0.024/0.024 | prop-depletion |
| campaign-winners | m19 | kh19-win | 1 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| campaign-winners | m19 | kh19-win | 2 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| campaign-winners | m19 | kh19-win | 3 | P | F | 960.960/199.750 | 969.254/199.749 | 0.024/0.024 | prop-depletion |
| campaign-winners | m19 | kh19-win | 4 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| campaign-winners | m19 | kh19-win | 5 | P | F | 960.960/199.750 | 969.254/199.749 | 0.024/0.024 | prop-depletion |
| starter | m19 | kh19-win | 1 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| starter | m19 | kh19-win | 2 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| starter | m19 | kh19-win | 3 | P | F | 960.960/199.750 | 969.254/199.749 | 0.024/0.024 | prop-depletion |
| starter | m19 | kh19-win | 4 | P | F | 975.004/199.745 | 983.790/199.744 | 0.023/0.023 | prop-depletion |
| starter | m19 | kh19-win | 5 | P | F | 960.960/199.750 | 969.254/199.749 | 0.024/0.024 | prop-depletion |
| board-fly | m20 | kh20-win | 1 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| board-fly | m20 | kh20-win | 2 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| board-fly | m20 | kh20-win | 3 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| board-fly | m20 | kh20-win | 4 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| board-fly | m20 | kh20-win | 5 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| campaign-winners | m20 | kh20-win | 1 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| campaign-winners | m20 | kh20-win | 2 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| campaign-winners | m20 | kh20-win | 3 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| campaign-winners | m20 | kh20-win | 4 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| campaign-winners | m20 | kh20-win | 5 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| starter | m20 | kh20-win | 1 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| starter | m20 | kh20-win | 2 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| starter | m20 | kh20-win | 3 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| starter | m20 | kh20-win | 4 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
| starter | m20 | kh20-win | 5 | P | P | 3175847.421/199.839 | 3175847.424/199.836 | 0.021/0.021 | c3 |
