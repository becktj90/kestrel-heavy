# B02 summary

Recorded 2026-11-11, America/New_York. Model `eleven_multilingual_v2`. Voices: Eric (mission control), Sarah (host), Chris (cohost), same settings as kh-gen4 / B01. **Not added to the game manifest or `unity/Assets`.** Staging audio only. Name-free alternates were not recorded (real-name text is final).

## Recorded

- Ready rows in this batch: 187
- Recorded: **187 lines, 341 takes**
- Held: **0**
- Planned character upper bound: 14652
- ElevenLabs remaining before → after: 203776 → 188131 (used 15645)
- Output: `out/B02/raw/`, `out/B02/processed/`, `manifest.json`, `loudness.csv`, `loudness-report.md`

## Quality

See `loudness-report.md`. Flags still failing after one retake: 7.

## Events recorded

- `abort`: 3 lines (6 takes)
- `color.ascent`: 14 lines (20 takes)
- `color.coast`: 16 lines (22 takes)
- `color.karman`: 8 lines (10 takes)
- `color.pad`: 14 lines (20 takes)
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

## QC failures after retake

- `mission-control/count.n8.g5-1` take 3: ['lufs:-18.13']
- `mission-control/count.n7.g5-1` take 1: ['lufs:-17.76']
- `mission-control/count.n7.g5-1` take 2: ['lufs:-17.03']
- `mission-control/count.n7.g5-1` take 3: ['lufs:-17.48']
- `mission-control/count.n5.g5-1` take 2: ['lufs:-17.56']
- `mission-control/count.n5.g5-1` take 3: ['lufs:-17.2']
- `host/color.karman.g5-4#b` take 1: ['lufs:-17.24']

## ready-2 additions (Physics second pass)

Folded in 15 newly cleared B02 rows (24 takes) from ready-2.tsv. Skipped `cohost/maxq-throttle.1` because that take is already on PR #494. Name-free alternates were not recorded.

## Countdown

Per-second T-10 to T-1 clips (`count.n10`…`count.n1`) are new; main has no existing dedicated countdown-number takes, so they were recorded. T-60 / T-30 / T-15, poll, hold, scrub and recycle lines were also recorded.

## Loudness re-norm (no new ElevenLabs calls)

On 2026-11-11, America/New_York, 7 takes that were still below −17 LUFS were re-normalized from their processed sources with makeup gain plus a −1.5 dBTP limiter. All 7 now meet −16 LUFS ±1 and ≤ −1.5 dBTP. No takes were dropped.

