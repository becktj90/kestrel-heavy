# G6-B01 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **344** (172 lines)
- Duration: 1.254–5.016 s (mean 2.457 s)
- LUFS: -16.88 to -16.29 (all inside −17…−15)
- True peak: -3.62 to -1.64 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **2** (once each).
  - `cohost/status.heating.g6-1` take 2: first ['lufs:-17.03']; after in spec
  - `cohost/status.heating.g6-2` take 2: first ['peak:-1.25']; after in spec
- Processed size: 10.27 MB (344 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/sep-clear.g6-1 t1: 5.016s, -16.44 LUFS, -3.39 dBTP
- Shortest: mission-control/landing.sink.good.g6-1 t1: 1.254s, -16.41 LUFS, -2.70 dBTP
- Quietest LUFS: cohost/status.heating.g6-1 t1: 3.605s, -16.88 LUFS, -2.01 dBTP
- Loudest LUFS: host/liftoff.g6-1 t2: 2.952s, -16.29 LUFS, -2.95 dBTP
- Highest peak: cohost/status.heating.g6-1 t2: 4.415s, -16.40 LUFS, -1.64 dBTP

