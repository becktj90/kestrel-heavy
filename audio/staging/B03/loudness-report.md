# B03 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **152**
- Duration: 1.933–12.382 s (mean 5.117 s)
- LUFS: -16.95 to -16.39 (all inside −17…−15)
- True peak: -3.48 to -1.56 dBTP (all ≤ −1.5)
- Flags still failing: **0**.
- Re-normalized without new TTS (gain + limiter from processed source): **2** takes, all now in spec.
  - `cohost/designer.tanks.tip.g5-2` take 1: -17.22 LUFS / -2.02 dBTP, +1.22 dB → -16.66 LUFS / -1.63 dBTP
  - `cohost/designer.tanks.tip.g5-2` take 2: -17.28 LUFS / -1.66 dBTP, +1.28 dB → -16.87 LUFS / -1.76 dBTP
- Processed size: 9.39 MB (152 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/designer.tanks.g5-1#c t1: 12.382s, -16.44 LUFS, -3.12 dBTP
- Shortest: host/color.recovery.g5-6#b t1: 1.933s, -16.95 LUFS, -1.75 dBTP
- Quietest LUFS: cohost/designer.fins.tip.g5-1 t1: 3.004s, -16.95 LUFS, -1.60 dBTP
- Loudest LUFS: host/designer.fins.g5-1#b t2: 4.18s, -16.39 LUFS, -1.62 dBTP
- Highest peak: host/designer.engines.g5-1#d t2: 4.206s, -16.45 LUFS, -1.56 dBTP

