# B08 summary — near-pad engine roar loops

Recorded 2026-11-11, America/New_York. **Staging only.** Not wired, not copied into `unity/Assets`. No fact claims on these SFX.

## Loop model test
Official API (`POST /v1/sound-generation`, docs read this turn): `model_id` is `eleven_text_to_sound_v2` (default) or `eleven_text_to_sound_v3`. Parameters: `text`, `duration_seconds` (0.5–30), `prompt_influence` (0–1, default 0.3), `loop`. Output `mp3_44100_128` on Creator (44.1 kHz WAV/PCM needs Pro). The Nov 2, 2026 SFX v3 developer post said looping “currently only works with” v2.

A 5 s v3 test (`loop: true`, `duration_seconds: 5`, `prompt_influence: 0.5`, kerolox roar prompt) returned HTTP 200. Decoded length 5.00 s, stereo 44.1 kHz. Seam vs the existing `roar-loop` trim:

- v3: no fade (first/last 50 ms RMS 0.81× / 0.92× of the body); click energy at the wrap 0.84× the file median; 30 ms RMS at the wrap 0.87× typical.
- Existing `roar-loop`: fades at both ends (0.27× / 0.34×) and a level hole at the wrap (0.18× typical), so it avoids a click by dipping.

**Decision: generate B08 on `eleven_text_to_sound_v3` with `loop: true`.** v2 was not needed.

## Batch
- 4 items × 3 variants = **12** takes, all 30 s loops (plan credits 14,400 at 40/s).
- Prompts from `sfx.tsv`. `prompt_influence` 0.5 so kerolox / hydrolox / methalox / storable stay distinct; variants still differ (12 unique hashes).
- Processing: downmix to mono, 80 ms equal-power crossfade, constant gain to −16 LUFS, circular limiter only when needed for ≤ −1.5 dBTP.
- All 12 in spec: −16.28 to −16.03 LUFS, −3.73 to −1.55 dBTP, 29.920 s, seamless (click 0.85–1.28× median).
- Loudness target matches existing long Unity SFX (roar-loop, liftoff, maxq, meco, etc. are ~−16.4 LUFS) and the gen5 plan.
- Overlap: all four ids are upgrades for existing `roar-loop`, not extra files to play beside it.
- Formats: ogg (Vorbis q5, 44.1 kHz mono, gapless) and mp3 (96 kbps, 44.1 kHz mono). Processed total **7.38 MB**.

## Credits
See the PR body for the subscription counters after the test and after the batch. Remaining stayed well above the 30,000 reserve. No plan upgrade.
