# B03 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **152** (76 lines)
- Duration: 1.933–12.382 s (mean 5.117 s)
- LUFS: -17.28 to -16.39 (SOME OUT OF RANGE)
- True peak: -3.48 to -1.56 dBTP (all ≤ −1.5)
- Flags after retake: **2** still failing.
- Retakes: **4** (once each).
  - `cohost/designer.engines.tip.g5-3` take 1: first ['peak:-1.44']; after in spec
  - `cohost/designer.engines.tip.g5-4` take 1: first ['peak:-1.25']; after in spec
  - `cohost/designer.tanks.tip.g5-2` take 1: first ['lufs:-17.36']; after ['lufs:-17.22']
  - `cohost/designer.tanks.tip.g5-2` take 2: first ['lufs:-17.44']; after ['lufs:-17.28']
- Processed size: 9.39 MB (152 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/designer.tanks.g5-1#c t1: 12.382s, -16.44 LUFS, -3.12 dBTP
- Shortest: host/color.recovery.g5-6#b t1: 1.933s, -16.95 LUFS, -1.75 dBTP
- Quietest LUFS: cohost/designer.tanks.tip.g5-2 t2: 4.441s, -17.28 LUFS, -1.66 dBTP
- Loudest LUFS: host/designer.fins.g5-1#b t2: 4.18s, -16.39 LUFS, -1.62 dBTP
- Highest peak: host/designer.engines.g5-1#d t2: 4.206s, -16.45 LUFS, -1.56 dBTP

