# B01 summary (mission-status feedback)

Recorded 2026-11-11, America/New_York. Model `eleven_multilingual_v2`. Voices: Eric (mission control), Sarah (host), Chris (cohost), same settings as kh-gen4. **Not added to the game manifest or `unity/Assets`.** Events are not wired; this is staging audio only.

## Recorded

- Plan rows in B01: 202
- Recorded (empty `claim_to_check`): **180 lines × 2 takes = 360 clips**
- Held for Codex/Physics (or History): **22 lines** (2 takes each, not recorded)
- Planned character upper bound for recorded rows: 12,472 (plus one retake of `landing.sink.high.g5-5` take 1)
- Countdown: **no B01 rows are countdown or per-second T-minus clips.** Those are in B02. Main has no dedicated `count.*` / T-minus clips (only `pad` and `color.pad` lines). Nothing was skipped as a duplicate.
- Output: `out/B01/raw/`, `out/B01/processed/`, `manifest.json`, `loudness.csv`, `loudness-report.md`

## Quality

All 360 processed takes meet −16 LUFS ±1, ≤ −1.5 dBTP, and 0.679–5.016 s (no call over 6 s, none silent). One LUFS miss was re-taken once and passed. See `loudness-report.md`.

## Events recorded

- `engine-out`: 6 lines (2 takes each)
- `engine-out.ok`: 4 lines (2 takes each)
- `fuel-low`: 7 lines (2 takes each)
- `landing.alt.10`: 4 lines (2 takes each)
- `landing.alt.100`: 6 lines (2 takes each)
- `landing.alt.1k`: 6 lines (2 takes each)
- `landing.alt.500`: 6 lines (2 takes each)
- `landing.drift`: 4 lines (2 takes each)
- `landing.fuel-low`: 5 lines (2 takes each)
- `landing.legs`: 5 lines (2 takes each)
- `landing.sink.good`: 6 lines (2 takes each)
- `landing.sink.high`: 6 lines (2 takes each)
- `outcome.hop`: 7 lines (2 takes each)
- `outcome.missed`: 6 lines (2 takes each)
- `outcome.offtarget`: 6 lines (2 takes each)
- `outcome.orbit`: 11 lines (2 takes each)
- `outcome.orbit.high`: 5 lines (2 takes each)
- `outcome.orbit.low`: 5 lines (2 takes each)
- `outcome.short`: 6 lines (2 takes each)
- `outcome.toohigh`: 4 lines (2 takes each)
- `sep-clear`: 7 lines (2 takes each)
- `sep-wide`: 4 lines (2 takes each)
- `status.offcourse`: 11 lines (2 takes each)
- `status.offcourse.clear`: 5 lines (2 takes each)
- `status.ontrack`: 12 lines (2 takes each)
- `status.short`: 11 lines (2 takes each)
- `status.short.clear`: 6 lines (2 takes each)
- `status.speed`: 9 lines (2 takes each)

## Held rows (not recorded)

These wait for Codex and Physics (and one History check) before any ElevenLabs call.

| id | voice | check_by | claim_to_check | text |
|---|---|---|---|---|
| `cohost/status.speed.g5-1` | Chris | Physics | Orbit is mainly a horizontal-speed problem | Speed reached. Remember, orbit is about going sideways fast, not just high. |
| `mission-control/status.short.g5-5` | Eric | Codex | 'Delta-v margin negative' matches LiveSpare < 0 | Below the line. Delta-v margin is negative. |
| `mission-control/status.heating.g5-1` | Eric | Physics | q near the warn fraction precedes the q peak (not after it) | Dynamic pressure is high. Close to the design limit. |
| `mission-control/status.heating.g5-2` | Eric | Physics | q near the warn fraction precedes the q peak (not after it) | Q is building. Near the structural limit. |
| `mission-control/status.heating.g5-3` | Eric | Physics | q near the warn fraction precedes the q peak (not after it) | Heavy air load. Watch the Q bar. |
| `mission-control/status.heating.g5-4` | Eric | Physics | q near the warn fraction precedes the q peak (not after it) | Aero load high. Approaching max-Q. |
| `mission-control/status.heating.g5-5` | Eric | Physics | q near the warn fraction precedes the q peak (not after it) | Pressure on the airframe is near its limit. |
| `mission-control/status.heating.g5-6` | Eric | Physics | q near the warn fraction precedes the q peak (not after it) | Q is high. Easy on the throttle. |
| `cohost/status.heating.g5-1` | Chris | Physics | q = 1/2 rho v^2 (density and speed together) | This is the rough part. Fast rocket, thick air. The air is pushing hard. |
| `cohost/status.heating.g5-2` | Chris | Physics | q = 1/2 rho v^2 (density and speed together) | That push is dynamic pressure: speed and air density working together. |
| `cohost/status.heating.g5-3` | Chris | Physics | q = 1/2 rho v^2 (density and speed together) | Throttling back here is like easing off the gas over a bumpy road. |
| `host/fuel-low.g5-2` | Sarah | Physics | Constant thrust + falling mass => acceleration increases late in the burn | Low fuel. But it's lightest now, so every second buys more speed. |
| `mission-control/landing.fuel-low.g5-4` | Eric | Physics | Booster cannot hover (min throttle T/W > 1 for typical boosters) | Low landing prop. No hover. Straight in. |
| `host/sep-clear.g5-2` | Sarah | Physics | Dropping dry mass raises later acceleration/delta-v | Beautiful staging. Dropping that empty weight is the whole point. |
| `host/sep-clear.g5-4` | Sarah | Physics | Dropping dry mass raises later acceleration/delta-v | Staging is good. The lighter rocket can speed up faster now. |
| `host/engine-out.ok.g5-1` | Sarah | Physics | Cluster redundancy: remaining engines burn longer to compensate | With several engines, losing one doesn't have to end the flight. |
| `host/engine-out.ok.g5-2` | Sarah | Physics | Cluster redundancy: remaining engines burn longer to compensate | That's why rockets carry extra engines. The others push a bit longer. |
| `host/engine-out.ok.g5-3` | Sarah | History | Multi-engine vehicles have flown through an engine loss (e.g. 1970 and 2012 in-flight engine shutdowns, general history) | Engine out, but still going. Real rockets have survived that. |
| `host/outcome.orbit.g5-4` | Sarah | Physics | LEO period ~88-92 min | And that's orbit. Around the world in about ninety minutes. |
| `host/outcome.orbit.high.g5-1` | Sarah | Physics | Orbit size set by horizontal speed at cutoff | It's in orbit, just higher than planned. A little too much speed. |
| `host/outcome.orbit.low.g5-1` | Sarah | Physics | Orbit size set by horizontal speed at cutoff | It's in orbit, but lower than planned. Just short on speed. |
| `host/outcome.missed.g5-1` | Sarah | Physics | Orbit size set by horizontal speed at cutoff | Not quite orbit this time. It needed more sideways speed. |

Held credits reserved (not spent): 2448 (2,448 planned).


## Follow-up commit

Added **22** newly recorded B01 lines (**44** takes) into this same staging folder:

- 11 leftover ready.tsv B01 rows that had been held for fact-checks (22 takes)
- 11 Physics second-pass rows from ready-2.tsv (22 takes)

No overlap with the original 360 takes. One retake (`cohost/status.heating.g5-3` t1) was slightly under −17 LUFS, re-recorded once, and is now in spec. **0** takes still failing.

See `followup-manifest.json`, `followup-loudness.csv`, `followup-loudness-report.md`, and `followup-summary.md` for the new rows only. The main `manifest.json` and `loudness.csv` now include both the original and follow-up takes.

## Hold flags (2026-11-11, America/New_York)

Marked 28 takes (14 clips) with `hold: true` and `hold_reason`: "no low-propellant trigger yet (Physics 2026-11-11)". Audio files were not deleted.

Clips:
- `host/fuel-low.g5-1`
- `host/fuel-low.g5-2`
- `host/fuel-low.g5-3`
- `mission-control/fuel-low.g5-1`
- `mission-control/fuel-low.g5-2`
- `mission-control/fuel-low.g5-3`
- `mission-control/fuel-low.g5-4`
- `mission-control/fuel-low.g5-5`
- `mission-control/landing.fuel-low.g5-1`
- `mission-control/landing.fuel-low.g5-2`
- `mission-control/landing.fuel-low.g5-3`
- `mission-control/landing.fuel-low.g5-4`
- `mission-control/landing.fuel-low.g5-5`
- `mission-control/landing.fuel-low.g5-6`
