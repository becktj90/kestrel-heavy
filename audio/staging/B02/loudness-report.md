# B02 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **343** (189 lines)
- Duration: 0.496–5.042 s (mean 2.632 s)
- LUFS: -16.96 to -16.26 (all inside −17…−15)
- True peak: -5.45 to -1.54 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **14** (once each).
  - `mission-control/count.n10.g5-1` take 2: first ['lufs:-17.45']; after in spec
  - `mission-control/count.n8.g5-1` take 1: first ['lufs:-17.05']; after in spec
  - `mission-control/count.n8.g5-1` take 2: first ['lufs:-17.16']; after in spec
  - `mission-control/count.n8.g5-1` take 3: first ['lufs:-17.02']; after ['lufs:-18.13']
  - `mission-control/count.n7.g5-1` take 1: first ['lufs:-17.72']; after ['lufs:-17.76']
  - `mission-control/count.n7.g5-1` take 2: first ['lufs:-17.24']; after ['lufs:-17.03']
  - `mission-control/count.n7.g5-1` take 3: first ['lufs:-17.6']; after ['lufs:-17.48']
  - `mission-control/count.n5.g5-1` take 2: first ['lufs:-17.21']; after ['lufs:-17.56']
  - `mission-control/count.n5.g5-1` take 3: first ['lufs:-17.09']; after ['lufs:-17.2']
  - `mission-control/count.n1.g5-1` take 2: first ['lufs:-18.14']; after in spec
  - `host/color.coast.g5-3#b` take 1: first ['lufs:-17.13']; after in spec
  - `host/color.coast.g5-5#b` take 1: first ['lufs:-17.13']; after in spec
  - `host/color.karman.g5-4#b` take 1: first ['lufs:-17.06']; after ['lufs:-17.24']
  - `cohost/maxq-throttle.g5-2` take 1: first ['peak:-1.33']; after in spec
- Processed size: 10.95 MB (343 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/color.pad.g5-5#b t1: 5.042s, -16.44 LUFS, -1.77 dBTP
- Shortest: mission-control/count.n2.g5-1 t3: 0.496s, -16.45 LUFS, -2.26 dBTP
- Quietest LUFS: host/color.recovery.g5-1#b t1: 3.657s, -16.96 LUFS, -1.97 dBTP
- Loudest LUFS: mission-control/count.n7.g5-1 t1: 0.731s, -16.26 LUFS, -1.88 dBTP
- Highest peak: host/color.pad.g5-2#b t1: 4.598s, -16.44 LUFS, -1.54 dBTP

## Loudness re-norm (no new ElevenLabs calls)

Takes that were still below −17 LUFS were re-normalized from their processed sources with makeup gain plus a −1.5 dBTP limiter. All listed takes now meet −16 LUFS ±1 and ≤ −1.5 dBTP. No takes were dropped.

- `mission-control/count.n8.g5-1` take 3: -18.13 LUFS / -1.77 dBTP, +3.40 dB → -16.79 LUFS / -1.80 dBTP
- `mission-control/count.n7.g5-1` take 1: -17.76 LUFS / -1.90 dBTP, +3.50 dB → -16.26 LUFS / -1.88 dBTP
- `mission-control/count.n7.g5-1` take 2: -17.03 LUFS / -1.89 dBTP, +1.03 dB → -16.70 LUFS / -1.88 dBTP
- `mission-control/count.n7.g5-1` take 3: -17.48 LUFS / -1.82 dBTP, +1.48 dB → -16.89 LUFS / -1.86 dBTP
- `mission-control/count.n5.g5-1` take 2: -17.56 LUFS / -1.89 dBTP, +3.50 dB → -16.31 LUFS / -1.75 dBTP
- `mission-control/count.n5.g5-1` take 3: -17.20 LUFS / -1.91 dBTP, +1.20 dB → -16.76 LUFS / -1.85 dBTP
- `host/color.karman.g5-4#b` take 1: -17.24 LUFS / -2.06 dBTP, +1.24 dB → -16.71 LUFS / -1.69 dBTP

## ready-3.tsv

Added new `host/color.pad.g5-1#a` and `#b` (1 take each) from ready-3.tsv.

## Transfer/departure retake (Physics, 2026-11-11, America/New_York)

Re-recorded all 13 held `outcome.depart` and `outcome.burnshort` clips (26 takes) with Physics-approved parking-orbit wording and `requires` gates copied from gen6:

- "transfer" lines play only on m7, m10, m15, and m19
- "departure" / "energy" lines play only on m9, m17, m18, and m20

`mission-control/outcome.depart.g5-4` uses "Departure energy is in the tank. Parking orbit holds."
`host/outcome.depart.g5-3` uses "The departure is covered. The energy is right on the plan."
Hold flags were removed. All 26 takes in spec (−16 LUFS ±1, true peak ≤ −1.5 dBTP, ≤ 6 s). Same voices, model `eleven_multilingual_v2`, and settings. Audio replaced in place; not wired.
