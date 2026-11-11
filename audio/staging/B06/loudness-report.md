# B06 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **74** (37 lines)
- Duration: 2.508–96.496 s (mean 12.287 s)
- LUFS: -17.00 to -16.35 (all inside −17…−15)
- True peak: -3.72 to -1.51 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **2** (once each).
  - `host/idle.pad.g5-24#b` take 2: first ['lufs:-17.15']; after in spec
  - `host/idle.pad.g5-26#b` take 2: first ['lufs:-17.09']; after in spec
- Processed size: 10.94 MB (74 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/codex.delta-v.eng.g5-1 t1: 96.496s, -16.44 LUFS, -3.26 dBTP
- Shortest: host/idle.pad.g5-26#a t2: 2.508s, -16.43 LUFS, -2.56 dBTP
- Quietest LUFS: host/idle.pad.g5-24#b t2: 6.766s, -17.00 LUFS, -1.60 dBTP
- Loudest LUFS: cohost/codex.max-q.kid.g5-1 t1: 11.285s, -16.35 LUFS, -1.51 dBTP
- Highest peak: cohost/codex.max-q.kid.g5-1 t1: 11.285s, -16.35 LUFS, -1.51 dBTP

