# B14 summary

Recorded 2026-11-11, America/New_York. Model `eleven_text_to_sound_v3`. Staging only; not wired, not copied into `unity/Assets`.

- 6 items, 15 takes.
- Loops: 0 items / 0 takes with `loop: true`.
- One-shots: 6 items / 15 takes, `loop` omitted/false.
- Processed size 0.88 MB.

## Items

- `pad.steam` ×2, 6.0 s, loop=False
- `pad.countdown.beep` ×4, 1.0 s, loop=False (overlap: quindar)
- `fairing.tumble` ×2, 4.0 s, loop=False
- `gridfin.actuate` ×3, 2.0 s, loop=False
- `landed.cooling` ×2, 6.0 s, loop=False
- `debris.patter` ×2, 4.0 s, loop=False

## QC
- All 15 unique hashes. Formats match prior batches: ogg Vorbis q5 + mp3 96k, 44.1 kHz mono. All one-shots (\`loop: false\`).
- Event one-shots (steam, fairing tumble, grid-fin actuate, cooling, debris) are in −16 LUFS ±1 and ≤ −1.5 dBTP after an 80 ms tail trim.
- \`pad.countdown.beep\` (4 takes, 0.14–0.21 s) is treated as UI: peak-normalized to −3 dBFS, TP ≤ −1.5, 40 ms tail trim, same convention as existing pickup/hit. SfxCatalog already plays UI quieter. Overlap: quindar.

## Overlaps
Flagged in the manifest as an upgrade/replacement: \`pad.countdown.beep\` → quindar.

## Credits
Planned 2,000 at 40/s for 15 takes. Subscription is shared with a concurrent Gen6 narration job. Before B14: 100,011 / 229,144 (129,133 remaining). After B14 (last read): 102669 / 229144 (126475 remaining). Observed delta 2658.
