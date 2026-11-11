# B01 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. Calls must be ≤ 6 s and not silent.

- Takes measured: **360** (180 lines × 2)
- Duration: 0.679–5.016 s (mean 2.086 s)
- LUFS: -16.75 to -16.40 (all inside −17…−15)
- True peak: -4.47 to -1.79 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over 6 s, silent, or clipped.
- One retake: `mission-control/landing.sink.high.g5-5` take 1 was −17.14 LUFS, re-recorded once, then in spec.
- Processed size: 9.14 MB (360 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/fuel-low.g5-3 t2: 5.016s, -16.45 LUFS, -3.29 dBTP
- Shortest: mission-control/landing.alt.10.g5-2 t1: 0.679s, -16.60 LUFS, -1.81 dBTP
- Quietest LUFS: mission-control/landing.sink.high.g5-5 t1: 1.802s, -16.75 LUFS, -1.83 dBTP
- Loudest LUFS: mission-control/landing.fuel-low.g5-5 t1: 1.620s, -16.40 LUFS, -3.16 dBTP
- Highest peak: mission-control/landing.sink.high.g5-5 t2: 1.698s, -16.44 LUFS, -1.79 dBTP
