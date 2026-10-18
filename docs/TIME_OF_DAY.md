# Launch time of day

`LaunchTimeOfDay` is the lighting for one Kestrel Heavy flight: `Day`, `Sunset`, or `Night` (0, 1, 2). It is not `Kestrel.Core.Flight.TimeOfDay`, which is the weather-clock row (noon, morning, and the rest) already on the briefing.

```csharp
LaunchTimeOfDay tod = LaunchTime.PickTimeOfDay(missionId, seed);
```

`missionId` is the campaign flight code (`KH-1` … `KH-20`, `MissionContext.LabelFor`). `seed` is the flight seed. The same pair always returns the same value on every runtime.

## How the pick works

The hash is FNV-1a 64. The id is mixed as UTF-16 code units, then the seed as eight little-endian bytes. Offset basis `14695981039346656037`, prime `1099511628211`. The bucket is `hash % (day + sunset + night)`, walked in order Day, then Sunset, then Night. The 2^64 modulo bias is under a part in 10^18 and is ignored.

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

## Graphics Dev (PR #271)

`Kestrel.Kit.LaunchTimeOfDay` and `TimeOfDayPresets.ForMission(id)` are the placeholder. `ForMission` hashes the id alone and then uses a flat 50/25/25, so a mission never changes sky. `PickTimeOfDay` replaces that call.

Kestrel.Core cannot reference Kit. The Core asmdef has no references, the dotnet project compiles only `Assets/Kestrel.Core`, and Kit is the game layer that should reference Core. The enum in this change is the one to keep. Delete the Kit copy and use `Kestrel.Core.Flight.LaunchTimeOfDay`. The numeric values already match.
