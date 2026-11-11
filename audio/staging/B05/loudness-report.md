# B05 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **182** (91 lines)
- Duration: 1.384–9.273 s (mean 4.775 s)
- LUFS: -17.33 to -16.38 (SOME OUT OF RANGE)
- True peak: -3.71 to -1.51 dBTP (all ≤ −1.5)
- Flags after retake: **1** still failing.
- Retakes: **2** (once each).
  - `cohost/menu.home.tip.g5-4` take 2: first ['lufs:-17.12']; after in spec
  - `host/idle.pad.g5-15#b` take 2: first ['lufs:-17.01']; after ['lufs:-17.33']
- Processed size: 10.49 MB (182 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/menu.home.idle.g5-4#a t1: 9.273s, -16.44 LUFS, -3.11 dBTP
- Shortest: host/menu.settings.intro.g5-1#b t2: 1.384s, -16.40 LUFS, -1.84 dBTP
- Quietest LUFS: host/idle.pad.g5-15#b t2: 5.016s, -17.33 LUFS, -2.24 dBTP
- Loudest LUFS: cohost/menu.home.tip.g5-3 t1: 3.527s, -16.38 LUFS, -1.66 dBTP
- Highest peak: host/menu.home.idle.g5-4#b t1: 3.344s, -16.75 LUFS, -1.51 dBTP

