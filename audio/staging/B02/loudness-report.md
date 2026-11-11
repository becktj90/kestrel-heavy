# B02 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **341**
- Duration: 0.496–5.042 s (mean 2.630 s)
- LUFS: -16.96 to -16.26 (all inside −17…−15)
- True peak: -5.45 to -1.54 dBTP (all ≤ −1.5)
- Flags still failing: **0**.
- Re-normalized without new TTS (gain + limiter from processed source): **7** takes, all now in spec.
  - `mission-control/count.n8.g5-1` take 3: -18.13 LUFS / -1.77 dBTP, +3.40 dB → -16.79 LUFS / -1.80 dBTP
  - `mission-control/count.n7.g5-1` take 1: -17.76 LUFS / -1.90 dBTP, +3.50 dB → -16.26 LUFS / -1.88 dBTP
  - `mission-control/count.n7.g5-1` take 2: -17.03 LUFS / -1.89 dBTP, +1.03 dB → -16.70 LUFS / -1.88 dBTP
  - `mission-control/count.n7.g5-1` take 3: -17.48 LUFS / -1.82 dBTP, +1.48 dB → -16.89 LUFS / -1.86 dBTP
  - `mission-control/count.n5.g5-1` take 2: -17.56 LUFS / -1.89 dBTP, +3.50 dB → -16.31 LUFS / -1.75 dBTP
  - `mission-control/count.n5.g5-1` take 3: -17.20 LUFS / -1.91 dBTP, +1.20 dB → -16.76 LUFS / -1.85 dBTP
  - `host/color.karman.g5-4#b` take 1: -17.24 LUFS / -2.06 dBTP, +1.24 dB → -16.71 LUFS / -1.69 dBTP
- Processed size: 10.88 MB (341 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/color.pad.g5-5#b t1: 5.042s, -16.44 LUFS, -1.77 dBTP
- Shortest: mission-control/count.n2.g5-1 t3: 0.496s, -16.45 LUFS, -2.26 dBTP
- Quietest LUFS: host/color.recovery.g5-1#b t1: 3.657s, -16.96 LUFS, -1.97 dBTP
- Loudest LUFS: mission-control/count.n7.g5-1 t1: 0.731s, -16.26 LUFS, -1.88 dBTP
- Highest peak: host/color.pad.g5-2#b t1: 4.598s, -16.44 LUFS, -1.54 dBTP

