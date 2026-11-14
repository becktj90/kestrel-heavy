# B08 loudness and loop report

Target (from existing long Unity SFX and the gen5 plan): **−16 LUFS ±1**, true peak **≤ −1.5 dBTP**. Loops must be seamless.

- Items: 4 (`engine.roar.near.kerolox|hydrolox|methalox|storable`)
- Variants: 3 each = **12** takes
- Duration after 80 ms equal-power crossfade: **29.920 s** (requested 30 s)
- LUFS: -16.28 to -16.03
- True peak: -3.73 to -1.55 dBTP
- All 12 in spec. A circular limiter was used only when constant gain to −16 LUFS would have exceeded −1.5 dBTP.
- Seam: click energy at the wrap is 0.85–1.28× the file median (no click). Head/tail 50 ms RMS is 0.70–1.16× the body (no fade-down).
- Formats: **ogg** (Vorbis q5, 44.1 kHz mono, gapless) and **mp3** (96 kbps, 44.1 kHz mono, Unity Sfx folder convention). Existing catalog SFX are 22.05 kHz mono mp3; these are upgrades.
- Processed size: ogg 3.06 MB + mp3 4.32 MB = 7.38 MB (under 40 MB).

See `loudness.csv` and `manifest.json` for per-take values. Overlap: every B08 id is an upgrade/replacement for existing `roar-loop`, not a duplicate to import beside it.

## Loop model test (before the batch)
`eleven_text_to_sound_v3` with `loop: true`, 5 s, ~200 planned credits, HTTP 200. v3 produced a continuous loop (no end fade, no click). Official Nov 2, 2026 note said looping “currently only works with” v2; the test shows v3 does loop, so **B08 used v3**. Details in the batch summary.
