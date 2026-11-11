# B02 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **341** (187 lines)
- Duration: 0.496–5.042 s (mean 2.630 s)
- LUFS: -18.13 to -16.27 (SOME OUT OF RANGE)
- True peak: -5.45 to -1.54 dBTP (all ≤ −1.5)
- Flags after retake: **7** still failing.
- Retakes: **14** (once each).
  - `mission-control/count.n10.g5-1` take 2: first ['lufs:-17.45']; after in spec
  - `mission-control/count.n8.g5-1` take 1: first ['lufs:-17.05']; after in spec
  - `mission-control/count.n8.g5-1` take 2: first ['lufs:-17.16']; after in spec
  - `mission-control/count.n8.g5-1` take 3: first ['lufs:-17.02']; after ['lufs:-18.13']
  - `mission-control/count.n7.g5-1` take 1: first ['lufs:-17.72']; after ['lufs:-17.76']
  - `mission-control/count.n7.g5-1` take 2: first ['lufs:-17.24']; after ['lufs:-17.03']
  - `mission-control/count.n7.g5-1` take 3: first ['lufs:-17.6']; after ['lufs:-17.48']
  - `mission-control/count.n5.g5-1` take 2: first ['lufs:-17.21']; after ['lufs:-17.56']
  - `mission-control/count.n5.g5-1` take 3: first ['lufs:-17.09']; after ['lufs:-17.2']
  - `mission-control/count.n1.g5-1` take 2: first ['lufs:-18.14']; after in spec
  - `host/color.coast.g5-3#b` take 1: first ['lufs:-17.13']; after in spec
  - `host/color.coast.g5-5#b` take 1: first ['lufs:-17.13']; after in spec
  - `host/color.karman.g5-4#b` take 1: first ['lufs:-17.06']; after ['lufs:-17.24']
  - `cohost/maxq-throttle.g5-2` take 1: first ['peak:-1.33']; after in spec
- Processed size: 10.88 MB (341 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/color.pad.g5-5#b t1: 5.042s, -16.44 LUFS, -1.77 dBTP
- Shortest: mission-control/count.n2.g5-1 t3: 0.496s, -16.45 LUFS, -2.26 dBTP
- Quietest LUFS: mission-control/count.n8.g5-1 t3: 0.679s, -18.13 LUFS, -1.77 dBTP
- Loudest LUFS: host/seco.g5-2 t1: 2.926s, -16.27 LUFS, -3.25 dBTP
- Highest peak: host/color.pad.g5-2#b t1: 4.598s, -16.44 LUFS, -1.54 dBTP

