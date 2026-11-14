# G6-B08 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **152** (76 lines)
- Duration: 3.657–7.941 s (mean 5.627 s)
- LUFS: -16.98 to -16.35 (all inside −17…−15)
- True peak: -3.51 to -1.52 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **3** (once each).
  - `host/idle.pad.g6-27#b` take 2: first ['lufs:-17.38']; after in spec
  - `host/idle.pad.g6-32#b` take 1: first ['lufs:-17.07']; after in spec
  - `host/idle.pad.g6-37#b` take 2: first ['lufs:-17.06']; after ['lufs:-17.11']
- Processed size: 10.32 MB (152 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/idle.pad.g6-35#a t2: 7.941s, -16.43 LUFS, -3.27 dBTP
- Shortest: host/idle.pad.g6-24#c t1: 3.657s, -16.44 LUFS, -3.18 dBTP
- Quietest LUFS: host/idle.pad.g6-34#b t1: 4.18s, -16.98 LUFS, -1.94 dBTP
- Loudest LUFS: cohost/menu.home.tip.g6-8 t2: 6.713s, -16.35 LUFS, -1.70 dBTP
- Highest peak: host/idle.pad.g6-27#b t1: 4.18s, -16.95 LUFS, -1.52 dBTP

