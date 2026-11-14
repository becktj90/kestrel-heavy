# G6-B03 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **210** (105 lines)
- Duration: 1.802–8.803 s (mean 4.256 s)
- LUFS: -16.98 to -16.30 (all inside −17…−15)
- True peak: -3.58 to -1.59 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **4** (once each).
  - `host/color.recovery.g6-1#b` take 1: first ['peak:-1.44']; after in spec
  - `host/color.recovery.g6-4#b` take 1: first ['lufs:-17.28']; after in spec
  - `host/color.recovery.g6-4#b` take 2: first ['lufs:-17.42']; after in spec
  - `host/idle.mission.m4.g6-1#b` take 2: first ['lufs:-17.14']; after in spec
- Processed size: 10.80 MB (210 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/idle.mission.m6.g6-1#a t2: 8.803s, -16.43 LUFS, -3.17 dBTP
- Shortest: host/color.recovery.g6-1#a t2: 1.802s, -16.44 LUFS, -3.25 dBTP
- Quietest LUFS: host/color.recovery.g6-4#b t2: 2.638s, -16.98 LUFS, -1.66 dBTP
- Loudest LUFS: host/menu.mission.history.m3.g6-1#b t1: 3.762s, -16.30 LUFS, -2.01 dBTP
- Highest peak: host/explain.staging-risk.g6-1#b t2: 4.624s, -16.45 LUFS, -1.59 dBTP

