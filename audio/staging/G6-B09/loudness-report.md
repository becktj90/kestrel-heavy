# G6-B09 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **56** (28 lines)
- Duration: 3.579–8.255 s (mean 5.693 s)
- LUFS: -16.98 to -16.28 (all inside −17…−15)
- True peak: -3.39 to -1.68 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **2** (once each).
  - `cohost/menu.home.tip.g6-12` take 1: first ['peak:-1.3']; after in spec
  - `host/idle.pad.g6-41#b` take 2: first ['lufs:-17.12']; after in spec
- Processed size: 3.85 MB (56 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/idle.pad.g6-42#a t1: 8.255s, -16.45 LUFS, -3.21 dBTP
- Shortest: host/idle.pad.g6-38#c t2: 3.579s, -16.44 LUFS, -3.39 dBTP
- Quietest LUFS: cohost/menu.home.tip.g6-10 t2: 6.818s, -16.98 LUFS, -1.69 dBTP
- Loudest LUFS: cohost/menu.home.tip.g6-11 t2: 5.251s, -16.28 LUFS, -1.74 dBTP
- Highest peak: host/menu.home.idle.g6-17#b t1: 5.695s, -16.95 LUFS, -1.68 dBTP

