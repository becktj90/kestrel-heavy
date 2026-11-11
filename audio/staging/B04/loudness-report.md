# B04 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **140** (70 lines)
- Duration: 2.273–11.546 s (mean 5.464 s)
- LUFS: -16.96 to -16.41 (all inside −17…−15)
- True peak: -3.47 to -1.56 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **2** (once each).
  - `host/designer.materials.g5-1#b` take 2: first ['lufs:-17.08']; after ['lufs:-17.02']
  - `host/designer.staging.g5-2#b` take 1: first ['lufs:-17.29']; after in spec
- Processed size: 9.23 MB (140 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/designer.materials.g5-1#c t1: 11.546s, -16.44 LUFS, -3.29 dBTP
- Shortest: host/idle.pad.g5-3#a t2: 2.273s, -16.44 LUFS, -3.40 dBTP
- Quietest LUFS: host/designer.staging.g5-2#b t2: 5.094s, -16.96 LUFS, -1.73 dBTP
- Loudest LUFS: host/designer.gridfins.g5-1#a t2: 4.833s, -16.41 LUFS, -3.17 dBTP
- Highest peak: cohost/designer.legs.tip.g5-4 t2: 3.527s, -16.44 LUFS, -1.56 dBTP

## Loudness re-norm (no new ElevenLabs calls)

Takes that were still below −17 LUFS were re-normalized from their processed sources with makeup gain plus a −1.5 dBTP limiter. All listed takes now meet −16 LUFS ±1 and ≤ −1.5 dBTP. No takes were dropped.

- `host/designer.materials.g5-1#b` take 2: -17.02 LUFS / -1.88 dBTP, +1.02 dB → -16.67 LUFS / -1.68 dBTP

## ready-3.tsv

Added `host/designer.recovery.g5-1#c` (2 takes) using the 22-word short text from ready-3.tsv. This clip was not in the earlier B04 staging set.

