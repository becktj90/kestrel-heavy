# B03 summary

Recorded 2026-11-11, America/New_York. Model `eleven_multilingual_v2`. Voices: Eric (mission control), Sarah (host), Chris (cohost), same settings as kh-gen4 / B01. **Not added to the game manifest or `unity/Assets`.** Staging audio only. Name-free alternates were not recorded (real-name text is final).

## Recorded

- Ready rows in this batch: 76
- Recorded: **76 lines, 152 takes**
- Held: **0**
- Planned character upper bound: 13064
- ElevenLabs remaining before → after: 203776 → 180584 (used 23192)
- Output: `out/B03/raw/`, `out/B03/processed/`, `manifest.json`, `loudness.csv`, `loudness-report.md`

## Quality

See `loudness-report.md`. Flags still failing after one retake: 2.

## Events recorded

- `color.recovery`: 2 lines (4 takes)
- `designer.avionics`: 3 lines (6 takes)
- `designer.crew`: 3 lines (6 takes)
- `designer.engines`: 17 lines (34 takes)
- `designer.engines.tip`: 4 lines (8 takes)
- `designer.fairings`: 3 lines (6 takes)
- `designer.fairings.tip`: 4 lines (8 takes)
- `designer.fins`: 7 lines (14 takes)
- `designer.fins.tip`: 4 lines (8 takes)
- `designer.legs`: 4 lines (8 takes)
- `designer.nose`: 3 lines (6 takes)
- `designer.payload`: 3 lines (6 takes)
- `designer.tanks`: 15 lines (30 takes)
- `designer.tanks.tip`: 4 lines (8 takes)

## QC failures after retake

- `cohost/designer.tanks.tip.g5-2` take 1: ['lufs:-17.22']
- `cohost/designer.tanks.tip.g5-2` take 2: ['lufs:-17.28']

## ready-2 additions (Physics second pass)

Folded in 2 newly cleared B03 rows (4 takes) from ready-2.tsv: `host/color.recovery.g5-6#a` and `#b`. Name-free alternates were not recorded.

## Loudness re-norm (no new ElevenLabs calls)

On 2026-11-11, America/New_York, 2 takes that were still below −17 LUFS were re-normalized from their processed sources with makeup gain plus a −1.5 dBTP limiter. All 2 now meet −16 LUFS ±1 and ≤ −1.5 dBTP. No takes were dropped.

