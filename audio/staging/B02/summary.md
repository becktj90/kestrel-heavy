# B02 summary

Recorded 2026-11-11, America/New_York. Model `eleven_multilingual_v2`. Voices: Eric (mission control), Sarah (host), Chris (cohost), same settings as kh-gen4 / B01. **Not added to the game manifest or `unity/Assets`.** Staging audio only. Name-free alternates were not recorded (real-name text is final).

## Recorded

- Ready rows in this batch: 189
- Recorded: **189 lines, 343 takes**
- Held: **0**
- Planned character upper bound: 1369
- ElevenLabs remaining before → after: 188131 → 158440 (used 29691)
- Output: `out/B02/raw/`, `out/B02/processed/`, `manifest.json`, `loudness.csv`, `loudness-report.md`

## Quality

See `loudness-report.md`. Flags still failing after one retake: 0.

## Events recorded

- `abort`: 3 lines (6 takes)
- `color.ascent`: 14 lines (20 takes)
- `color.coast`: 16 lines (22 takes)
- `color.karman`: 8 lines (10 takes)
- `color.pad`: 16 lines (22 takes)
- `color.recovery`: 10 lines (12 takes)
- `count.hold`: 8 lines (16 takes)
- `count.n1`: 1 lines (3 takes)
- `count.n10`: 1 lines (3 takes)
- `count.n2`: 1 lines (3 takes)
- `count.n3`: 1 lines (3 takes)
- `count.n4`: 1 lines (3 takes)
- `count.n5`: 1 lines (3 takes)
- `count.n6`: 1 lines (3 takes)
- `count.n7`: 1 lines (3 takes)
- `count.n8`: 1 lines (3 takes)
- `count.n9`: 1 lines (3 takes)
- `count.poll`: 8 lines (16 takes)
- `count.recycle`: 5 lines (10 takes)
- `count.scrub`: 8 lines (16 takes)
- `count.t15`: 1 lines (2 takes)
- `count.t30`: 1 lines (2 takes)
- `count.t60`: 2 lines (4 takes)
- `deploy`: 6 lines (12 takes)
- `entry`: 3 lines (6 takes)
- `fairing`: 5 lines (10 takes)
- `haven-maxq`: 2 lines (4 takes)
- `liftoff`: 4 lines (8 takes)
- `maxq-throttle`: 6 lines (10 takes)
- `maxq-through`: 5 lines (9 takes)
- `meco`: 3 lines (6 takes)
- `outcome.burnshort`: 6 lines (12 takes)
- `outcome.depart`: 7 lines (14 takes)
- `outcome.toohigh`: 2 lines (4 takes)
- `pad`: 4 lines (8 takes)
- `salvage`: 3 lines (6 takes)
- `seco`: 4 lines (8 takes)
- `ses1`: 3 lines (6 takes)
- `splash`: 3 lines (6 takes)
- `status.apogee`: 4 lines (8 takes)
- `struct-warn`: 2 lines (4 takes)
- `tip`: 2 lines (4 takes)
- `touchdown`: 5 lines (10 takes)

## ready-3.tsv update

Added new `host/color.pad.g5-1#a` and `#b` (1 take each) from ready-3.tsv.

## Transfer/departure retake (Physics, 2026-11-11, America/New_York)

Re-recorded all 13 held `outcome.depart` and `outcome.burnshort` clips (26 takes) with Physics-approved parking-orbit wording and `requires` gates copied from gen6:

- "transfer" lines play only on m7, m10, m15, and m19
- "departure" / "energy" lines play only on m9, m17, m18, and m20

`mission-control/outcome.depart.g5-4` uses "Departure energy is in the tank. Parking orbit holds."
`host/outcome.depart.g5-3` uses "The departure is covered. The energy is right on the plan."
Hold flags were removed. All 26 takes in spec (−16 LUFS ±1, true peak ≤ −1.5 dBTP, ≤ 6 s). Same voices, model `eleven_multilingual_v2`, and settings. Audio replaced in place; not wired.
