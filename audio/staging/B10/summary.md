# B10 summary

Recorded 2026-11-11, America/New_York. Model `eleven_text_to_sound_v3`. Staging only; not wired, not copied into `unity/Assets`.

- 10 items, 44 takes.
- Loops: 4 items / 15 takes with `loop: true`.
- One-shots: 6 items / 29 takes, `loop` omitted/false.
- Processed size 7.15 MB.

## Items

- `landing.burn.loop` ×3, 15.0 s, loop=True (overlap: burn-loop)
- `liftoff.layered` ×4, 8.0 s, loop=False (overlap: liftoff)
- `amb.pad.day` ×4, 30.0 s, loop=True
- `pad.cryo.vent` ×4, 15.0 s, loop=True
- `aero.transonic.buffet` ×4, 10.0 s, loop=True (overlap: maxq)
- `aero.sonicboom.double` ×5, 3.0 s, loop=False
- `aero.sonicboom.triple` ×4, 4.0 s, loop=False
- `stage.sep.pyro` ×6, 2.0 s, loop=False (overlap: whoosh)
- `fairing.sep` ×5, 3.0 s, loop=False (overlap: whoosh)
- `struct.stress` ×5, 3.0 s, loop=False

## QC
- All 44 unique hashes. Formats match B08/B09: ogg Vorbis q5 + mp3 96k, 44.1 kHz mono.
- All takes in −16 LUFS ±1 and ≤ −1.5 dBTP on the shipped ogg. No flags.
- One-shots (sonic booms, pyro/fairing seps, liftoff, structural stress) use an 80 ms tail trim and short fade-out, same event-level target as existing long Unity SFX (−16 LUFS). No UI rows in B10.
- Loops (landing burn, pad day, cryo vent, transonic buffet): 200 ms equal-power crossfade, circular limiter, no edge fade.
- High-crest booms and sep clunks used dynaudnorm + iterative makeup with wav TP held near −3.5 dB so the encoded files stay in spec.

## Overlaps
Flagged in the manifest as upgrades/replacements: burn-loop, liftoff, maxq, whoosh (stage.sep.pyro and fairing.sep).

## Credits
Planned 14,800 at 40/s for 44 takes. Subscription is shared with a concurrent narration job. Before B10: 67,547 / 229,144 (161,597 remaining). After B10: 72291 / 229144 (156853 remaining). Observed delta 4744.
