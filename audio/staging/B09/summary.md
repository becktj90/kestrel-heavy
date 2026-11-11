# B09 summary

Recorded 2026-11-11, America/New_York. Model `eleven_text_to_sound_v3`. Staging only; not wired, not copied into `unity/Assets`.

- 10 items, 39 takes.
- Loops: 3 items / 9 takes with `loop: true`.
- One-shots: 7 items / 30 takes, `loop` omitted/false.
- Processed size 6.82 MB.

## Items

- `engine.roar.near.solid` ×3, 30.0 s, loop=True (overlap: roar-loop)
- `engine.roar.near.hybrid` ×3, 30.0 s, loop=True (overlap: roar-loop)
- `engine.roar.far` ×3, 20.0 s, loop=True (overlap: roar-loop)
- `engine.ignition.liquid` ×4, 4.0 s, loop=False (overlap: quindar(ignite alias))
- `engine.ignition.solid` ×4, 4.0 s, loop=False
- `engine.shutdown` ×5, 3.0 s, loop=False (overlap: meco)
- `engine.throttle.down` ×4, 3.0 s, loop=False (overlap: maxq (partial))
- `engine.throttle.up` ×4, 3.0 s, loop=False
- `engine.out` ×5, 3.0 s, loop=False
- `landing.burn.relight` ×4, 4.0 s, loop=False (overlap: burn-loop)

## QC
- All 39 unique hashes. Formats match B08: ogg Vorbis q5 + mp3 96k, 44.1 kHz mono.
- Loudness: every take is in −16 LUFS ±1 and ≤ −1.5 dBTP on the shipped ogg (one-shots 2.36–3.94 s after an 80 ms tail trim).
- High-crest ignition / engine-out / relight takes used an unclipped float limiter plus dynaudnorm and iterative makeup, with wav true-peak held near −3.5 dB so Vorbis/LAME stay in spec.
- Loops use a 200 ms equal-power crossfade (slightly longer than B08's 80 ms) and a circular limiter. dynaudnorm is not applied to loops because it faded the edges.
- `engine.roar.near.solid.v3` and `engine.roar.far.v1` first came back faded at the edges; retakes with a constant-level prompt replaced them and now loop cleanly (head/tail ≈ 1.0–1.3× body, click ≈ 1.0–1.2× median).
- **Flag:** `engine.roar.far.v3` still fades at both ends (head/tail ≈ 0.41–0.43× body) after four extra retakes plus the original. Click is clean. Prefer v1 or v2 for a seamless far-roar loop. Flagged in the manifest.

## Overlaps
Flagged in the manifest the same way as B08 (upgrade/replacement, not a second file to play beside the existing clip): roar-loop, quindar (ignite alias), meco, maxq (partial), burn-loop.

## Credits
Planned 13,680 at the conservative 40 credits/s rate for 39 takes. Twelve retake requests (four earlier loop retakes, then eight fade retakes) added more. The subscription counter is shared with a concurrent narration job, so the batch delta is not B09-only. See the PR body for the counters.
