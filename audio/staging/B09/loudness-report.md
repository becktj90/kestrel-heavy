# B09 loudness and QC report

- Items: 10; takes: 39 (loops 9, one-shots 30)
- Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP for loops and event one-shots (matches existing long Unity SFX).
- UI / very short clips (<0.4 s, where integrated LUFS is unreliable, like existing pickup/hit): peak-normalized to −3 dBFS, TP ≤ −1.5. Playback gain in SfxCatalog is already lower for UI (0.28–0.34 vs 0.5–0.72 for events).
- One-shots: trailing silence trimmed (≈40 ms UI / 80 ms events) with a short fade-out.
- Loops: 200 ms equal-power crossfade; circular limiter; no dynaudnorm (it fades loop edges).
- High-crest one-shots: unclipped float path, dynaudnorm + iterative makeup, wav TP held near −3.5 dB so encoded ogg/mp3 stay ≤ −1.5 dBTP.
- Formats: ogg (Vorbis q5, 44.1 kHz mono) and mp3 (96 kbps, 44.1 kHz mono). Size 2.85 + 3.97 = 6.82 MB.
- Loop LUFS -16.65 to -16.08; TP -4.53 to -2.73.
- One-shot LUFS -16.71 to -16.02; TP -5.52 to -2.31; duration 2.357–3.939 s.
- Flagged takes: 1: engine.roar.far.v3 (fade_at_edge)

## Overlap with existing repo sounds

- engine.ignition.liquid → quindar(ignite alias) (upgrade/replacement, not a second file to play beside it)
- engine.roar.far → roar-loop (upgrade/replacement, not a second file to play beside it)
- engine.roar.near.hybrid → roar-loop (upgrade/replacement, not a second file to play beside it)
- engine.roar.near.solid → roar-loop (upgrade/replacement, not a second file to play beside it)
- engine.shutdown → meco (upgrade/replacement, not a second file to play beside it)
- engine.throttle.down → maxq (partial) (upgrade/replacement, not a second file to play beside it)
- landing.burn.relight → burn-loop (upgrade/replacement, not a second file to play beside it)
