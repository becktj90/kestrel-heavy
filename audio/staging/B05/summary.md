# B05 summary

Recorded 2026-11-11, America/New_York. Model `eleven_multilingual_v2`. Voices: Eric (mission control), Sarah (host), Chris (cohost), same settings as kh-gen4 / B01. **Not added to the game manifest or `unity/Assets`.** Staging audio only. Name-free alternates were not recorded (real-name text is final).

## Recorded

- Ready rows in this batch: 93
- Recorded: **93 lines, 186 takes**
- Held: **0**
- Planned character upper bound: 15008
- ElevenLabs remaining before → after: 179782 → 158440 (used 21342)
- Output: `out/B05/raw/`, `out/B05/processed/`, `manifest.json`, `loudness.csv`, `loudness-report.md`

## Quality

See `loudness-report.md`. Flags still failing after one retake: 0.

## Events recorded

- `idle.pad`: 44 lines (88 takes)
- `menu.codex.intro`: 2 lines (4 takes)
- `menu.home.idle`: 14 lines (28 takes)
- `menu.home.tip`: 4 lines (8 takes)
- `menu.home.welcome`: 5 lines (10 takes)
- `menu.medals.intro`: 2 lines (4 takes)
- `menu.mission.m1`: 1 lines (2 takes)
- `menu.mission.m11`: 1 lines (2 takes)
- `menu.mission.m12`: 1 lines (2 takes)
- `menu.mission.m13`: 1 lines (2 takes)
- `menu.mission.m14`: 1 lines (2 takes)
- `menu.mission.m15`: 1 lines (2 takes)
- `menu.mission.m16`: 1 lines (2 takes)
- `menu.mission.m17`: 1 lines (2 takes)
- `menu.mission.m18`: 1 lines (2 takes)
- `menu.mission.m2`: 1 lines (2 takes)
- `menu.mission.m20`: 1 lines (2 takes)
- `menu.mission.m3`: 1 lines (2 takes)
- `menu.mission.m4`: 1 lines (2 takes)
- `menu.mission.m5`: 1 lines (2 takes)
- `menu.mission.m6`: 1 lines (2 takes)
- `menu.mission.m7`: 1 lines (2 takes)
- `menu.mission.m8`: 1 lines (2 takes)
- `menu.missions`: 3 lines (6 takes)
- `menu.settings.intro`: 2 lines (4 takes)

## ready-3.tsv update

Replaced `host/menu.home.idle.g5-2#a`–`#c` (fruit flies, 6 takes) with the real-name V-2 text from ready-3.tsv. Added `host/menu.mission.m14.g5-1` and `host/menu.mission.m20.g5-1` (2 takes each). m20 uses the Physics-approved text: "Far Hold: the farthest target on the board. Out here, the Sun's pull already wins over Earth's."

## m20 retake (Physics, 2026-11-11, America/New_York)

Re-recorded both takes of `host/menu.mission.m20.g5-1` with the same Sarah voice (`EXAVITQu4vr4xnSDxMaL`), model `eleven_multilingual_v2`, and voice settings. Physics-approved text: "Far Hold: the farthest target on the board. Out here, the Sun's pull already wins over Earth's." This replaces the earlier "highest orbit" wording: m20 is not a stable Earth orbit. Manifest sha256 pins and loudness.csv were updated. Both takes in spec.

- take 1: 5.799 s, −16.45 LUFS, −3.38 dBTP
- take 2: 5.878 s, −16.44 LUFS, −3.30 dBTP
