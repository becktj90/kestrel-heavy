# B04 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **138** (69 lines)
- Duration: 2.273–11.546 s (mean 5.439 s)
- LUFS: -17.02 to -16.41 (SOME OUT OF RANGE)
- True peak: -3.47 to -1.56 dBTP (all ≤ −1.5)
- Flags after retake: **1** still failing.
- Retakes: **2** (once each).
  - `host/designer.materials.g5-1#b` take 2: first ['lufs:-17.08']; after ['lufs:-17.02']
  - `host/designer.staging.g5-2#b` take 1: first ['lufs:-17.29']; after in spec
- Processed size: 9.06 MB (138 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/designer.materials.g5-1#c t1: 11.546s, -16.44 LUFS, -3.29 dBTP
- Shortest: host/idle.pad.g5-3#a t2: 2.273s, -16.44 LUFS, -3.40 dBTP
- Quietest LUFS: host/designer.materials.g5-1#b t2: 4.362s, -17.02 LUFS, -1.88 dBTP
- Loudest LUFS: host/designer.gridfins.g5-1#a t2: 4.833s, -16.41 LUFS, -3.17 dBTP
- Highest peak: cohost/designer.legs.tip.g5-4 t2: 3.527s, -16.44 LUFS, -1.56 dBTP

