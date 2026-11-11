# B05 loudness report

Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP. In-flight calls ≤ 6 s, color lines ≤ 10 s, not silent.

- Takes measured: **186** (93 lines)
- Duration: 1.384–9.273 s (mean 4.822 s)
- LUFS: -16.86 to -16.38 (all inside −17…−15)
- True peak: -3.71 to -1.51 dBTP (all ≤ −1.5)
- Flags after measurement: **none** over duration cap, silent, or out of loudness spec.
- Retakes: **2** (once each).
  - `cohost/menu.home.tip.g5-4` take 2: first ['lufs:-17.12']; after in spec
  - `host/idle.pad.g5-15#b` take 2: first ['lufs:-17.01']; after ['lufs:-17.33']
- Processed size: 10.84 MB (186 mp3, 44.1 kHz mono 96k).

Per-take values are in `loudness.csv` and `manifest.json`.

## Extrema

- Longest: host/menu.home.idle.g5-4#a t1: 9.273s, -16.44 LUFS, -3.11 dBTP
- Shortest: host/menu.settings.intro.g5-1#b t2: 1.384s, -16.40 LUFS, -1.84 dBTP
- Quietest LUFS: host/menu.home.idle.g5-3#b t1: 4.493s, -16.86 LUFS, -1.94 dBTP
- Loudest LUFS: cohost/menu.home.tip.g5-3 t1: 3.527s, -16.38 LUFS, -1.66 dBTP
- Highest peak: host/menu.home.idle.g5-4#b t1: 3.344s, -16.75 LUFS, -1.51 dBTP

## Loudness re-norm (no new ElevenLabs calls)

Takes that were still below −17 LUFS were re-normalized from their processed sources with makeup gain plus a −1.5 dBTP limiter. All listed takes now meet −16 LUFS ±1 and ≤ −1.5 dBTP. No takes were dropped.

- `host/idle.pad.g5-15#b` take 2: -17.33 LUFS / -2.24 dBTP, +1.63 dB → -16.41 LUFS / -1.75 dBTP

## ready-3.tsv

Replaced `host/menu.home.idle.g5-2#a`–`#c` (fruit flies, 6 takes) with the real-name V-2 text from ready-3.tsv. Added `host/menu.mission.m14.g5-1` and `host/menu.mission.m20.g5-1` (2 takes each). m20 uses the Physics-approved text: "Far Hold: the farthest target on the board. Out here, the Sun's pull already wins over Earth's."

## m20 retake (Physics, 2026-11-11, America/New_York)

Re-recorded both takes of `host/menu.mission.m20.g5-1` with the same Sarah voice (`EXAVITQu4vr4xnSDxMaL`), model `eleven_multilingual_v2`, and voice settings. Physics-approved text: "Far Hold: the farthest target on the board. Out here, the Sun's pull already wins over Earth's." This replaces the earlier "highest orbit" wording: m20 is not a stable Earth orbit. Manifest sha256 pins and loudness.csv were updated. Both takes in spec.

- take 1: 5.799 s, −16.45 LUFS, −3.38 dBTP
- take 2: 5.878 s, −16.44 LUFS, −3.30 dBTP
