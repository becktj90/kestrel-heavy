# B07 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **12** (6 lines)
- Duration: 14.864–96.392 s (mean 44.622 s)
- LUFS: -16.65 to -16.38 (all inside −17…−15)
- True peak: -3.26 to -1.73 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Processed size: 6.43 MB (12 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/codex.orbits.eng.g5-1 t2: 96.392s, -16.43 LUFS, -3.21 dBTP
- Shortest: cohost/codex.booster-landing.kid.g5-1 t2: 14.864s, -16.38 LUFS, -1.80 dBTP
- Quietest LUFS: cohost/codex.orbits.kid.g5-1 t2: 18.155s, -16.65 LUFS, -1.73 dBTP
- Loudest LUFS: cohost/codex.booster-landing.kid.g5-1 t2: 14.864s, -16.38 LUFS, -1.80 dBTP
- Highest peak: cohost/codex.orbits.kid.g5-1 t2: 18.155s, -16.65 LUFS, -1.73 dBTP

