# G6-B07 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **158** (79 lines)
- Duration: 3.187–9.326 s (mean 5.841 s)
- LUFS: -17.00 to -16.33 (all inside −17…−15)
- True peak: -3.43 to -1.53 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **4** (once each).
  - `host/idle.pad.g6-1#b` take 2: first ['lufs:-17.04']; after in spec
  - `host/idle.pad.g6-6#b` take 2: first ['peak:-1.44']; after ['peak:-1.39']
  - `host/idle.pad.g6-9#b` take 1: first ['lufs:-17.26']; after in spec
  - `host/menu.home.idle.g6-10#b` take 2: first ['lufs:-17.01']; after in spec
- Processed size: 11.13 MB (158 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/idle.pad.g6-6#a t1: 9.326s, -16.43 LUFS, -3.15 dBTP
- Shortest: host/idle.pad.g6-2#b t1: 3.187s, -16.46 LUFS, -1.92 dBTP
- Quietest LUFS: host/idle.pad.g6-3#b t2: 5.433s, -17.00 LUFS, -1.53 dBTP
- Loudest LUFS: host/idle.pad.g6-18#c t2: 5.512s, -16.33 LUFS, -2.90 dBTP
- Highest peak: host/idle.pad.g6-3#b t2: 5.433s, -17.00 LUFS, -1.53 dBTP

