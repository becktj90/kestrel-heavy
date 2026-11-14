# B11 summary

Recorded 2026-11-11, America/New_York. Model `eleven_text_to_sound_v3`. Staging only; not wired, not copied into `unity/Assets`.

- 9 items, 38 takes.
- Loops: 3 items / 11 takes with `loop: true`.
- One-shots: 6 items / 27 takes, `loop` omitted/false.
- Processed size 7.14 MB.

## Items

- `payload.deploy` ×4, 2.0 s, loop=False
- `legs.deploy` ×5, 3.0 s, loop=False
- `touchdown.deck` ×5, 2.0 s, loop=False (overlap: touchdown)
- `amb.barge.ocean` ×4, 30.0 s, loop=True
- `splash.booster` ×4, 4.0 s, loop=False (overlap: splash)
- `tip.deck` ×4, 4.0 s, loop=False
- `rud.layered` ×5, 6.0 s, loop=False (overlap: rud)
- `amb.hangar` ×3, 30.0 s, loop=True
- `engine.crackle` ×4, 15.0 s, loop=True

## QC
- All 38 unique hashes. Formats match B08–B10: ogg Vorbis q5 + mp3 96k, 44.1 kHz mono.
- Loudness: every take with a measurable integrated LUFS is in −16 LUFS ±1 and ≤ −1.5 dBTP on the shipped ogg. \`payload.deploy.v3\` is 0.315 s (too short for integrated LUFS) and was peak-normalized to −3 dBFS, TP −3.37, same convention as existing pickup/hit.
- One-shots use an 80 ms tail trim (payload deploy, legs, touchdown, splash, tip, RUD). No UI rows in B11.
- Loops: 200 ms equal-power crossfade. \`amb.barge.ocean.v1\` and \`engine.crackle.v3\` first faded at the edges; retakes replaced them and now loop cleanly. No remaining flags.

## Overlaps
Flagged in the manifest as upgrades/replacements: touchdown, splash, rud.

## Credits
Planned 14,600 at 40/s for 38 takes, plus 4 loop retake requests. Subscription is shared with a concurrent narration job. Before B11: 72,291 / 229,144 (156,853 remaining). After B11 (last read): 76306 / 229144 (152838 remaining).
