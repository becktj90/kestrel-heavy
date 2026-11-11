# G6-B06 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **158** (79 lines)
- Duration: 3.997–7.889 s (mean 5.734 s)
- LUFS: -16.97 to -16.32 (all inside −17…−15)
- True peak: -3.50 to -1.51 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **3** (once each).
  - `cohost/designer.rtls.tip.g6-1` take 2: first ['lufs:-17.55']; after in spec
  - `cohost/designer.weather.tip.g6-1` take 2: first ['peak:-1.49']; after in spec
  - `host/menu.home.idle.g6-1#b` take 1: first ['lufs:-17.09']; after in spec
- Processed size: 10.93 MB (158 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/menu.home.idle.g6-3#a t2: 7.889s, -16.44 LUFS, -3.31 dBTP
- Shortest: host/designer.residuals.g6-1#b t2: 3.997s, -16.44 LUFS, -1.78 dBTP
- Quietest LUFS: cohost/designer.residuals.tip.g6-1 t1: 5.251s, -16.97 LUFS, -1.77 dBTP
- Loudest LUFS: cohost/designer.massratio.tip.g6-1 t2: 6.4s, -16.32 LUFS, -1.82 dBTP
- Highest peak: cohost/designer.telemetry.tip.g6-1 t2: 4.127s, -16.85 LUFS, -1.51 dBTP

