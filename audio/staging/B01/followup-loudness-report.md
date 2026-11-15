# B01b loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **44** (22 lines)
- Duration: 1.855–5.329 s (mean 3.474 s)
- LUFS: -16.91 to -16.42 (all inside −17…−15)
- True peak: -3.48 to -1.69 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **1** (once each).
  - `cohost/status.heating.g5-3` take 1: first ['lufs:-17.34']; after in spec
- Processed size: 1.85 MB (44 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/sep-clear.g5-4 t1: 5.329s, -16.44 LUFS, -3.19 dBTP
- Shortest: mission-control/status.heating.g5-2 t2: 1.855s, -16.42 LUFS, -2.84 dBTP
- Quietest LUFS: cohost/status.heating.g5-1 t1: 4.441s, -16.91 LUFS, -1.81 dBTP
- Loudest LUFS: mission-control/status.heating.g5-2 t2: 1.855s, -16.42 LUFS, -2.84 dBTP
- Highest peak: cohost/status.heating.g5-3 t1: 4.18s, -16.78 LUFS, -1.69 dBTP

