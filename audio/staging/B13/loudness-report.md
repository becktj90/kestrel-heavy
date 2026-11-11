# B13 loudness and QC report

- Items: 28; takes: 120 (loops 11, one-shots 109)
- Target: −16 LUFS ±1, true peak ≤ −1.5 dBTP for loops and event one-shots (matches existing long Unity SFX).
- UI / very short clips (<0.4 s, where integrated LUFS is unreliable, like existing pickup/hit): peak-normalized to −3 dBFS, TP ≤ −1.5. Playback gain in SfxCatalog is already lower for UI (0.28–0.34 vs 0.5–0.72 for events).
- One-shots: trailing silence trimmed (≈40 ms UI / 80 ms events) with a short fade-out.
- Loops: 200 ms equal-power crossfade; circular limiter; no dynaudnorm (it fades loop edges).
- High-crest one-shots: unclipped float path, dynaudnorm + iterative makeup, wav TP held near −3.5 dB so encoded ogg/mp3 stay ≤ −1.5 dBTP.
- Formats: ogg (Vorbis q5, 44.1 kHz mono) and mp3 (96 kbps, 44.1 kHz mono). Size 3.22 + 3.83 = 7.05 MB.
- Loop LUFS -16.32 to -16.05; TP -4.04 to -2.50.
- One-shot LUFS -33.60 to -15.73; TP -8.65 to -2.01; duration 0.057–5.359 s.
- Flagged takes: 0

## Overlap with existing repo sounds

- rud.distant → rud (upgrade/replacement, not a second file to play beside it)
- sting.success → recovered (success alias) (upgrade/replacement, not a second file to play beside it)
- touchdown.pad → touchdown (upgrade/replacement, not a second file to play beside it)
- ui.coin → quindar/pickup (upgrade/replacement, not a second file to play beside it)
- ui.tap → quindar/pickup (upgrade/replacement, not a second file to play beside it)
- ui.unlock → quindar/pickup (upgrade/replacement, not a second file to play beside it)
