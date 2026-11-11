# G6-B04 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **168** (84 lines)
- Duration: 2.821–8.307 s (mean 5.375 s)
- LUFS: -17.00 to -16.41 (all inside −17…−15)
- True peak: -3.48 to -1.50 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **5** (once each).
  - `host/idle.mission.m12.g6-1#b` take 2: first ['lufs:-17.13']; after in spec
  - `host/idle.mission.m8.g6-1#b` take 2: first ['lufs:-17.12']; after ['lufs:-17.01']
  - `host/menu.mission.history.m11.g6-1#b` take 1: first ['peak:-1.46']; after in spec
  - `host/menu.mission.history.m16.g6-1#b` take 1: first ['lufs:-17.17']; after in spec
  - `host/menu.mission.history.m17.g6-1#b` take 2: first ['lufs:-17.02']; after in spec
- Processed size: 10.90 MB (168 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/menu.mission.history.m17.g6-1#a t1: 8.307s, -16.43 LUFS, -3.16 dBTP
- Shortest: host/idle.mission.m17.g6-1#b t1: 2.821s, -16.63 LUFS, -1.87 dBTP
- Quietest LUFS: host/menu.mission.history.m17.g6-1#b t1: 4.833s, -17.00 LUFS, -1.67 dBTP
- Loudest LUFS: host/brief.mission.m1.g6-1#a t2: 4.963s, -16.41 LUFS, -3.27 dBTP
- Highest peak: host/menu.mission.history.m9.g6-1#b t1: 6.635s, -16.80 LUFS, -1.50 dBTP

