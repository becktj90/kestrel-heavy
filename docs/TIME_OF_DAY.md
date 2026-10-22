# Launch time of day

`LaunchTimeOfDay` is the lighting for one Kestrel Heavy flight: `Day`, `Sunset`, or `Night` (0, 1, 2). It is not `Kestrel.Core.Flight.TimeOfDay`, which is the weather-clock row (noon, morning, and the rest) already on the briefing.

```csharp
LaunchTimeOfDay tod = LaunchTime.PickTimeOfDay(missionId, seed);
```

`missionId` is the campaign flight code (`KH-1` … `KH-20`, `MissionContext.LabelFor`). `seed` is the flight seed. The same pair always returns the same value on every runtime.

## How the pick works

The hash is FNV-1a 64. The id is mixed as its **UTF-8 bytes** (explicitly encoded, not UTF-16 code units; a lone surrogate encodes as U+FFFD, `EF BF BD`), then the seed as eight little-endian bytes. Offset basis `14695981039346656037`, prime `1099511628211`. ASCII ids are one byte per character either way, so every shipped `KH-n` pick is the same as in #272 (`Hash("KH-1", 0)` is still `0xC39D71C85DA55BA0`). `LaunchTime.Fnv1a64(text)` is the plain hash and matches the published vectors (`""` `cbf29ce484222325`, `"a"` `af63dc4c8601ec8c`, `"foobar"` `85944171f73967e8`).

The bucket is `hash % (day + sunset + night)`, walked in order Day, then Sunset, then Night. The modulo bias is at most weight total / 2^64 per bucket: about 5e-18 at a total of 100, and up to about 1.6e-13 at the largest allowed weights (3 × 1,000,000). It is ignored.

## JS port and shared golden

`web/js/flight/launch-time.js` is the BigInt port (`fnv1a64`, `hash`, `parseTable`, `pick`). It takes the parsed table, so it never reads the file system. `data/golden/launch-time.json` (written by `tools/golden-parts/launch-time.mjs` through `npm run gen:golden`) lists the published vectors, two non-ASCII ids, and hash plus pick for KH-1 … KH-20, an unknown id and `""` at eight seeds. `tests/launch-time.test.cjs` and `LaunchTimeGoldenTests` (Kestrel.Core.Tests) both replay it. Changing `data/time-of-day.json` changes picks, so regenerate the golden with it.

## Loading the table

Editor and dotnet tests: `PickTimeOfDay` loads `data/time-of-day.json` from the repo checkout (`KESTREL_REPO_ROOT`, then walking up from the working directory).

Player builds have no checkout. CI copies `data/*.json` into `Assets/Resources/KestrelData` (Kestrel > Sync Data does it in the editor), and `Kestrel.Game.Shell.LaunchTimeBoot` installs `Resources/KestrelData/time-of-day` with `LaunchTime.UseJson` before the first scene loads. Any other host can inject its own table with `LaunchTime.UseTable(table)`; `UseTable(null)` returns to the checkout lookup.

`System.Random` and `string.GetHashCode` are not used. Both change across runtimes.

Weights live in `data/time-of-day.json`.

- `KH-1` is pinned to Day. A `fixed` row ignores the seed, so a story beat can pin a value.
- Any other row varies with the seed inside its weights, so a repeat flight can look different.
- Across KH-1 … KH-20 the chances average 50% Day, 25% Sunset, 25% Night.
- KH-3, KH-4, and KH-5 are night launches (night is the largest weight on each), so the pad spotlights show up before the campaign leaves the first five flights.
- GEO and GTO flights (`gto-comsat`, `geo-relay`) and KH-16 … KH-20 lean Sunset or Night. Crew Day (`KH-8`) stays a day launch. Nightjar (`KH-18`) stays a night launch.
- An id with no row, including a typo or a free-build label, uses the default: 60 Day, 25 Sunset, 15 Night.

## Who reads it

The briefing displays the enum name (`Day`, `Sunset`, or `Night`). Narrator lines key off that same enum. The weather-clock label on the card stays the wind-clock time and is a different field.

Kit lighting (sun elevation, fog, stars, `-khTimeOfDay`, `TimeOfDayRig`) consumes the enum. It does not choose it. Call `PickTimeOfDay`, then the Kit preset for that value. A command-line override still wins over the table when Kit parses one.

## Graphics Dev

Done. `Kestrel.Kit.LaunchTimeOfDay`, `TimeOfDayPresets.Pick`, `SeedFor` and `ForMission` are deleted. Kit uses `Kestrel.Core.Flight.LaunchTimeOfDay`.
`Kestrel.Kit.Core` now references `Kestrel.Core` (one way: Core still references nothing). `Kestrel.Kit.Tests` references it too.

- `TimeOfDayRig.SetMission(missionId, seed)` calls `LaunchTime.PickTimeOfDay(missionId, seed)` and then `TimeOfDayPresets.For(tod, VarietySeed(seed))`.
  In a player build `PickTimeOfDay` uses the table `LaunchTimeBoot` installed from Resources. If no table can be loaded, `SetMission` falls back to Day.
- `-khTimeOfDay=night|sunset|day [-khTodSeed=N]` is unchanged. It still wins over the table through `TimeOfDayRig.Force`.
