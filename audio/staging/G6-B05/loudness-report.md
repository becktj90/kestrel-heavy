# G6-B05 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **156** (78 lines)
- Duration: 3.944–7.602 s (mean 5.585 s)
- LUFS: -16.97 to -16.34 (all inside −17…−15)
- True peak: -3.48 to -1.50 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **3** (once each).
  - `cohost/designer.ullage.tip.g6-1` take 2: first ['lufs:-17.02']; after in spec
  - `host/brief.mission.m3.g6-1#b` take 2: first ['lufs:-17.05']; after in spec
  - `host/designer.interstage.g6-1#b` take 2: first ['lufs:-17.34']; after in spec
- Processed size: 10.51 MB (156 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/designer.guidance.g6-1#a t2: 7.602s, -16.45 LUFS, -3.33 dBTP
- Shortest: host/brief.mission.m14.g6-1#b t2: 3.944s, -16.43 LUFS, -2.25 dBTP
- Quietest LUFS: host/brief.mission.m9.g6-1#b t2: 5.747s, -16.97 LUFS, -1.84 dBTP
- Loudest LUFS: host/designer.boiloff.g6-1#a t2: 6.583s, -16.34 LUFS, -3.22 dBTP
- Highest peak: host/brief.mission.m3.g6-1#b t1: 4.415s, -16.93 LUFS, -1.50 dBTP

