# B10 loudness and QC report

- Items: 10; takes: 44 (loops 15, one-shots 29)
- Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP for loops and event one-shots (matches existing long Unity SFX).
- UI / very short clips (<0.4 s, where integrated LUFS is unreliable, like existing pickup/hit): peak-normalized to −3 dBFS, TP ≤ −1.5. Playback gain in SfxCatalog is already lower for UI (0.28–0.34 vs 0.5–0.72 for events).
- One-shots: trailing silence trimmed (≈40 ms UI / 80 ms events) with a short fade-out.
- Loops: 200 ms equal-power crossfade; circular limiter; no dynaudnorm (it fades loop edges).
- High-crest one-shots: unclipped float path, dynaudnorm + iterative makeup, wav TP held near −3.5 dB so encoded ogg/mp3 stay ≤ −1.5 dBTP.
- Formats: ogg (Vorbis q5, 44.1 kHz mono) and mp3 (96 kbps, 44.1 kHz mono). Size 3.08 + 4.07 = 7.15 MB.
- Loop LUFS -16.28 to -16.06; TP -4.13 to -2.72.
- One-shot LUFS -16.80 to -16.06; TP -3.86 to -2.33; duration 0.643–7.702 s.
- Flagged takes: 0

## Overlap with existing repo sounds

- aero.transonic.buffet → maxq (upgrade/replacement, not a second file to play beside it)
- fairing.sep → whoosh (upgrade/replacement, not a second file to play beside it)
- landing.burn.loop → burn-loop (upgrade/replacement, not a second file to play beside it)
- liftoff.layered → liftoff (upgrade/replacement, not a second file to play beside it)
- stage.sep.pyro → whoosh (upgrade/replacement, not a second file to play beside it)
