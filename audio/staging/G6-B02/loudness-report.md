# G6-B02 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **268** (134 lines)
- Duration: 1.332–5.016 s (mean 3.200 s)
- LUFS: -17.00 to -16.29 (all inside −17…−15)
- True peak: -3.58 to -1.51 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **5** (once each).
  - `cohost/abort-balloon.g6-1` take 2: first ['lufs:-17.13']; after in spec
  - `cohost/abort-debris.g6-1` take 1: first ['lufs:-17.03']; after in spec
  - `cohost/abort-tip.g6-1` take 1: first ['lufs:-17.17']; after in spec
  - `cohost/count.scrub.g6-2` take 1: first ['peak:-1.47']; after in spec
  - `host/color.ascent.g6-3#b` take 2: first ['lufs:-17.32']; after in spec
- Processed size: 10.39 MB (268 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/color.coast.g6-3#a t2: 5.016s, -16.43 LUFS, -3.27 dBTP
- Shortest: mission-control/outcome.short.g6-1 t1: 1.332s, -16.45 LUFS, -3.22 dBTP
- Quietest LUFS: cohost/abort-corridor.g6-1 t2: 3.187s, -17.00 LUFS, -1.77 dBTP
- Loudest LUFS: mission-control/outcome.burnshort.g6-1 t2: 3.344s, -16.29 LUFS, -2.17 dBTP
- Highest peak: cohost/abort-splash.g6-1 t2: 4.415s, -16.43 LUFS, -1.51 dBTP

