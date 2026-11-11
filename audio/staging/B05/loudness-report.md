# B05 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **182**
- Duration: 1.384–9.273 s (mean 4.775 s)
- LUFS: -16.86 to -16.38 (all inside −17…−15)
- True peak: -3.71 to -1.51 dBTP (all ≤ −1.5)
- Flags still failing: **0**.
- Re-normalized without new TTS (gain + limiter from processed source): **1** takes, all now in spec.
  - `host/idle.pad.g5-15#b` take 2: -17.33 LUFS / -2.24 dBTP, +1.63 dB → -16.41 LUFS / -1.75 dBTP
- Processed size: 10.49 MB (182 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/menu.home.idle.g5-4#a t1: 9.273s, -16.44 LUFS, -3.11 dBTP
- Shortest: host/menu.settings.intro.g5-1#b t2: 1.384s, -16.40 LUFS, -1.84 dBTP
- Quietest LUFS: host/menu.home.idle.g5-3#b t1: 4.493s, -16.86 LUFS, -1.94 dBTP
- Loudest LUFS: cohost/menu.home.tip.g5-3 t1: 3.527s, -16.38 LUFS, -1.66 dBTP
- Highest peak: host/menu.home.idle.g5-4#b t1: 3.344s, -16.75 LUFS, -1.51 dBTP

