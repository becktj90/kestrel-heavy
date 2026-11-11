# B13 summary

Recorded 2026-11-11, America/New_York. Model `eleven_text_to_sound_v3`. Staging only; not wired, not copied into `unity/Assets`.

- 28 items, 120 takes.
- Loops: 4 items / 11 takes with `loop: true`.
- One-shots: 24 items / 109 takes, `loop` omitted/false.
- Processed size 7.05 MB.

## Items

- `amb.orbit.onboard` ×3, 20.0 s, loop=True
- `gridfin.deploy` ×5, 1.5 s, loop=False
- `touchdown.pad` ×4, 2.0 s, loop=False (overlap: touchdown)
- `amb.barge.machinery` ×3, 20.0 s, loop=True
- `rud.distant` ×3, 6.0 s, loop=False (overlap: rud)
- `alarm.range` ×4, 2.0 s, loop=False
- `telemetry.los` ×4, 2.0 s, loop=False
- `alarm.master` ×4, 1.5 s, loop=False
- `ui.tap` ×5, 1.0 s, loop=False (overlap: quindar/pickup)
- `ui.toggle` ×5, 1.0 s, loop=False
- `ui.slider.detent` ×5, 1.0 s, loop=False
- `ui.part.snap` ×5, 1.0 s, loop=False
- `ui.part.remove` ×5, 1.0 s, loop=False
- `ui.warning` ×5, 1.0 s, loop=False
- `ui.error` ×5, 1.0 s, loop=False
- `ui.saved` ×5, 1.0 s, loop=False
- `ui.stage.add` ×5, 1.0 s, loop=False
- `ui.launch.confirm` ×5, 1.0 s, loop=False
- `ui.back` ×5, 1.0 s, loop=False
- `ui.medal` ×5, 1.0 s, loop=False
- `ui.coin` ×5, 1.0 s, loop=False (overlap: quindar/pickup)
- `ui.unlock` ×5, 1.0 s, loop=False (overlap: quindar/pickup)
- `amb.hangar.work` ×3, 20.0 s, loop=True
- `sting.success` ×4, 4.0 s, loop=False (overlap: recovered (success alias))
- `sting.orbit` ×4, 3.0 s, loop=False
- `sting.fail` ×4, 3.0 s, loop=False
- `engine.turbopump` ×2, 10.0 s, loop=True
- `engine.hardstart` ×3, 2.0 s, loop=False

## QC
- All 120 unique hashes. Formats match prior batches: ogg Vorbis q5 + mp3 96k, 44.1 kHz mono.
- Event / ambience / sting / alarm takes with measurable LUFS are in −16 LUFS ±1 and ≤ −1.5 dBTP on the shipped ogg.
- UI (tap, toggle, detent, snap, remove, warning, error, saved, stage.add, launch.confirm, back, medal, coin, unlock) follows Kestrel Sound convention: files are still normalized; SfxCatalog already plays UI quieter (0.28–0.34 vs 0.50–0.72 for events). Clips shorter than 0.4 s, where integrated LUFS is unreliable (same as existing pickup/hit), are peak-normalized to −3 dBFS with TP ≤ −1.5. A 40 ms tail trim and short fade-out are applied.
- Two slightly longer UI takes (\`ui.slider.detent.v4\`, \`ui.back.v4\`) were also peak-normalized because integrated LUFS was unstable. \`ui.saved.v4\` was pulled back into the −16 LUFS window after a makeup overshoot.
- Loops: 200 ms equal-power crossfade. \`amb.hangar.work.v3\` first had a seam click (2.16× median); a retake replaced it and now loops cleanly (click 1.10×).
- No remaining flags.

## Overlaps
Flagged in the manifest as upgrades/replacements: touchdown, rud, quindar/pickup (ui.tap, ui.coin, ui.unlock), recovered (success alias) for sting.success.

## Credits
Planned 14,860 at 40/s for 120 takes (5 already on disk from the interrupted first start were reused, not regenerated), plus 2 hangar-loop retake requests. Subscription is shared with a concurrent Gen6 narration job. Before resuming B13: 82,320 / 229,144 (146,824 remaining). After first-pass generation: 90,281 / 229,144 (138,863 remaining). Last read: 99498 / 229144 (129646 remaining).
