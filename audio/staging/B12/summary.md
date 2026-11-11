# B12 summary

Recorded 2026-11-11, America/New_York. Model `eleven_text_to_sound_v3`. Staging only; not wired, not copied into `unity/Assets`.

- 14 items, 53 takes.
- Loops: 4 items / 10 takes with `loop: true`.
- One-shots: 10 items / 43 takes, `loop` omitted/false.
- Processed size 6.83 MB.

## Items

- `engine.vacuum.onboard` ×3, 20.0 s, loop=True
- `engine.ignition.hypergolic` ×4, 3.0 s, loop=False
- `engine.startup.spool` ×4, 3.0 s, loop=False
- `rcs.coldgas` ×7, 1.0 s, loop=False
- `amb.pad.night` ×3, 30.0 s, loop=True
- `pad.arm.retract` ×4, 4.0 s, loop=False
- `pad.holddown.release` ×4, 2.0 s, loop=False
- `pad.tankpress` ×4, 3.0 s, loop=False
- `pad.deluge.start` ×3, 6.0 s, loop=False
- `pad.deluge.loop` ×2, 20.0 s, loop=True
- `aero.slipstream` ×2, 15.0 s, loop=True
- `stage.sep.pusher` ×5, 1.5 s, loop=False (overlap: whoosh)
- `stage.hotstaging` ×4, 3.0 s, loop=False
- `strapon.sep` ×4, 3.0 s, loop=False (overlap: whoosh)

## QC
- All 53 unique hashes. Formats match B08–B11: ogg Vorbis q5 + mp3 96k, 44.1 kHz mono.
- All takes in −16 LUFS ±1 and ≤ −1.5 dBTP on the shipped ogg. No remaining flags.
- One-shots (hypergolic ignite, spool, RCS puffs, pad arm/hold-down/tank press/deluge start, pusher/hot-staging/strap-on seps) use an 80 ms tail trim and a short fade-out, event-level target.
- Loops: 200 ms equal-power crossfade. \`engine.vacuum.onboard.v2\` and \`.v3\` first faded at the edges; retakes replaced them and now loop cleanly.

## Overlaps
Flagged in the manifest as upgrades/replacements: whoosh (stage.sep.pusher, strapon.sep).

## Credits
Planned 13,460 at 40/s for 53 takes, plus 4 loop retake requests. Subscription is shared with a concurrent narration job. Before B12: 77,296 / 229,144 (151,848 remaining). After B12 including retakes (last read): 81880 / 229144 (147264 remaining).
