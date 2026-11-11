# Per-mission technical commentary

Status: plan + audio shipped in this PR. Sources checked 2026-11-11 (America/New_York).
Machine-readable script: `tools/clipgen/mission-tech-lines.json` (same lines, same ids).
Generated audio: `assets/narration/` + Unity copy, manifests byte-identical (see Generation log).

## The decision

Trevor (Nov 11 2026, picking "Generate both batches"): spend the ElevenLabs credits on more
Kestrel Heavy and Cube Squad audio before downgrading the Creator plan, "perhaps more historical
commentary for kestrel and technical analysis". This PR is the technical half of his original
feedback (quoted in #449): "The launches are taking too long and are kind of boring. Can we make
them a little faster and use my eleven labs credits to add historical, technical, engineering
commentary in the game that has custom content in each level." The historical layer shipped in
PR #449/#450 (80 takes); this PR adds the technical layer: 80 takes, 4 per mission, one
engineering theme per mission. The Cube Squad batch is a separate PR.

## What ships here

- **80 takes, 4 per mission, one theme per mission** (table per mission below). Each take is a
  single-speaker color-layer line (Sarah or Chris), at most 14 words, at most 80 characters, tied
  to one flight window: `color.pad` (count hold), `color.ascent` (after Max-Q), `color.karman`
  (100 km crossing), `color.coast` (upper-stage coast), `color.recovery` (booster coast back).
- **Voices/model:** Sarah (host) and Chris (co-host), ElevenLabs premade library voices, model
  `eleven_multilingual_v2`, same voice IDs and settings as the shipped narration
  (`docs/NARRATION_LICENSE.md`). No new voices, no Jessica/Callum, no Crew Talk voices.
- **Gating:** every take carries `requires: {"mission": "m<n>"}` + `claims: ["mission"]` +
  `sources: [...]`. The `mission` key is the same one the historical set introduced (see
  `docs/narration-facts.md`); a take only plays on its own mission, and stays silent everywhere
  else — including sandbox, where there is no campaign mission.
- **Length rule:** at most 14 words and at most 80 characters per line (WRITING_BIBLE §6.2, the
  color lint). Every clip is under the 6 s manifest cap (measured durations in the manifest). A
  line that does not fit its window is dropped by the picker, never cut mid-line and never said
  late — so a short flight (m1, m2) may only hear 2 of its 4 takes, and the rest rotate in on
  replays. That is by design.
- **No real names anywhere in this set.** Unlike the historical set (DECISIONS #77), which may
  name real companies, rockets, programs, ships and people, these lines state generic
  engineering/physics facts only. No invented quotes anywhere (there are no quotes at all).

## Rules these lines follow

- Generic engineering/physics facts only. Every fact has a source in the table at the end (T-rows).
  Nothing is sourced to "common knowledge".
- No line states a fact about the flown design (no engine counts, no "booster", no strap-ons,
  no Haven/pad/site claims). That is deliberate: the claim-vs-design verifier only ever sees
  `claims: ["mission"]`, so a technical line can never be misread as a claim about the player's
  rocket. The banned words are enforced before generation by `tools/clipgen/gen_mission_tech.py`
  (every `statedFacts` trigger in `tests/narration-facts-lint.test.cjs` plus the plan-doc bans).
- Numbers are spoken as words ("forty-one", "one hundred forty-three"), per the existing
  no-digits-in-static-lines convention. There are no dates in this set at all.
- The game world stays fictional around the facts: a line about transfer orbits plays while the
  player's own stack sits on Pier 7 and lands on Haven. The line is about physics; the flight
  is the player's.

## How the lines plug into the narrator system

Read first: `docs/narration-webcast.md` (two-host color) and `docs/narration-facts.md` (fact tags).
What exists on main today is the same as when the historical set landed, plus the generic color
takes (#437) and max-Q beats (#442):

- **Color layer is live on main.** `ColorWindows` (five windows), `ColorDirector` (each window
  fires at most once per flight), `NarrationSchedule` Call-vs-Color priority, and the
  `AudioPrefs.CommentaryMode` Full / Calls only / Off setting all shipped. This PR adds the second
  mission-specific color set; no picker, schedule or `ColorDirector` changes.
- **Picker:** `NarrationPicker.Order` takes the eligible takes for an event, most claims first,
  then clip id. A mission take has 1 claim (`mission`); generic color takes (neutral) have 0.
  So on a mission flight a mission take is picked first, and the generic color exchange is the
  rotation behind it. The historical and technical mission takes both carry 1 claim, so the
  tie breaks on clip id: `...m1.1` (historical) sorts before `...m1.t1` (technical), so a
  flight hears the historical take first and the technical take on rotation/replays. On a flight
  with no mission (sandbox), only the generic takes are eligible.
- **Windows each take attaches to** (per mission below): identical to the historical set —
  `color.pad` only fires in a count hold with >20 s to T-60; `color.ascent` only after Max-Q with
  >12 s to MECO (or 10 s after Max-Q on a no-MECO design); `color.karman` within 3 s of the
  100 km crossing; `color.coast` only with 15 s of quiet after MECO; `color.recovery` only between
  boostback (or MECO + 20 s) and the entry burn, and only on a design with a picked landing.
  Missions without a landing (m1–m5, m8, m9, m11, m12, m14, m16–m18, m20) get no
  `color.recovery` take; m1–m2 get no `color.coast` take (short flights, no quiet coast): m1's
  4th take is a second ascent variant, m2's 4th is the `color.karman` take.
- **Skip / warp interaction:** DECISIONS #78 ("Skip to next stage") and #436 (coast warp ×4/×10)
  compress the gaps color lines play in. The picker gate (`duration + 1.5 s` must fit the wall
  seconds to the next call) drops a line that no longer fits instead of cutting it. A skipped or
  warped flight hears fewer lines; a real-time flight hears more. No line is ever wrong for being
  dropped.

## Character budget (ElevenLabs)

Counting method: characters of the spoken text only — the `text` field of each take, including
spaces and punctuation, excluding ids, speaker labels and manifest fields. `spokenText` equals
`text` for all 80 takes (no LOX/RUD/SES-1/em-dash substitutions apply). ElevenLabs bills the
characters sent per TTS call; the generation script counts exactly what it sent, and the
"Generation log" below records the before/after account balance. Per-mission totals:

| Mission | Takes | Characters |
|---------|------:|-----------:|
| KH-1 — First Hop | 4 | 243 |
| KH-2 — Edge of Space | 4 | 235 |
| KH-3 — Tiny Orbit | 4 | 235 |
| KH-4 — Weather Eye | 4 | 218 |
| KH-5 — Heavy Lifter | 4 | 220 |
| KH-6 — Come Back | 4 | 235 |
| KH-7 — Haven at Sea | 4 | 215 |
| KH-8 — Crew Day | 4 | 218 |
| KH-9 — Moon Shot | 4 | 237 |
| KH-10 — Big Reuse | 4 | 248 |
| KH-11 — Dawn Ledger | 4 | 264 |
| KH-12 — Glass Meridian | 4 | 233 |
| KH-13 — Flock Yard | 4 | 216 |
| KH-14 — Harbor Trunk | 4 | 229 |
| KH-15 — Relay West | 4 | 238 |
| KH-16 — Second Watch | 4 | 242 |
| KH-17 — Lumen Down | 4 | 216 |
| KH-18 — Nightjar | 4 | 232 |
| KH-19 — Keel Reuse | 4 | 257 |
| KH-20 — Far Hold | 4 | 235 |
| **Total (20 missions, 80 takes)** | **80** | **4666** |

Total script: **4666 characters** for all 80 takes. For scale, that is about 2.0% of one
month of the Creator plan (229,144 characters/month). Re-generation is hash-skipped: the
generator only calls the API for lines whose (voice, model, text, settings) hash is not in
the raw cache, so re-running it after a wording fix re-bills only the changed lines.

## The script

Voice: H = Sarah (host), C = Chris (co-host). Window: the `color.*` event the take attaches
to. Sources: T-rows are the source table at the end of this doc.

### KH-1 — First Hop

Flight: 50 kg sounding box to a 10 km apogee.

Theme: Thrust must beat weight; a rocket lightens as propellant burns.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m1.t1` | `color.pad` | H | Thrust must beat weight, or the rocket never leaves the pad. | T1 |
| `cohost/color.pad.m1.t2` | `color.pad` | C | A rocket lifts off only when thrust is stronger than weight. | T2 |
| `host/color.ascent.m1.t1` | `color.ascent` | H | As propellant burns away, the rocket grows lighter and quicker. | T1 |
| `cohost/color.ascent.m1.t2` | `color.ascent` | C | Same push, less mass: the rocket speeds up more each second. | T2 |

### KH-2 — Edge of Space

Flight: 100 kg sounding box to a 100 km apogee.

Theme: Orbit is sideways speed, not height; wings quit where the air ends.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m2.t1` | `color.pad` | H | Height alone is not orbit; sideways speed is the secret. | T3 |
| `cohost/color.pad.m2.t2` | `color.pad` | C | To stay up there, a craft must race sideways, not just climb. | T3 |
| `host/color.ascent.m2.t1` | `color.ascent` | H | Orbit needs sideways speed, so rockets lean over as they climb. | T3 |
| `cohost/color.karman.m2.t1` | `color.karman` | C | Up here the air is too thin for wings to lift anything. | T4 |

### KH-3 — Tiny Orbit

Flight: 20 kg cubesat to a 200 km circular orbit.

Theme: A satellite falls around Earth; speed and gravity balance.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m3.t1` | `color.pad` | H | A satellite never stops falling; it just keeps missing Earth. | T3 |
| `cohost/color.pad.m3.t2` | `color.pad` | C | Gravity pulls it down while speed bends its path around Earth. | T3 |
| `host/color.ascent.m3.t1` | `color.ascent` | H | Too slow and it sinks; too fast and it flies farther out. | T5 |
| `cohost/color.coast.m3.t1` | `color.coast` | C | In a circle, speed and gravity stay in perfect balance. | T3 |

### KH-4 — Weather Eye

Flight: 2,500 kg weather sat to a 500 km circular orbit.

Theme: Higher orbit, longer lap; altitude trades coverage against detail.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m4.t1` | `color.pad` | H | Higher orbits take longer to circle Earth just once. | T6 |
| `cohost/color.pad.m4.t2` | `color.pad` | C | A bigger loop means a slower lap around the planet. | T6 |
| `host/color.ascent.m4.t1` | `color.ascent` | H | From higher up, one view covers far more ground below. | T7 |
| `cohost/color.coast.m4.t1` | `color.coast` | C | Closer views show fine detail; wider views show whole storms. | T7 |

### KH-5 — Heavy Lifter

Flight: 12,000 kg cargo sat to a 400 km circular orbit, strap-ons allowed.

Theme: The rocket equation: most of a rocket is propellant; staging drops empty mass.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m5.t1` | `color.pad` | H | Most of a rocket is propellant, not metal or cargo. | T8 |
| `cohost/color.pad.m5.t2` | `color.pad` | C | Nine parts in ten can be propellant on a trip to orbit. | T8 |
| `host/color.ascent.m5.t1` | `color.ascent` | H | Speed grows from the ratio of full mass to empty mass. | T8 |
| `cohost/color.ascent.m5.t2` | `color.ascent` | C | Dropping empty tanks lets the rest speed up far more easily. | T2 |

### KH-6 — Come Back

Flight: 8,000 kg to a 200 km orbit, booster lands back at the pad (RTLS).

Theme: Coming home: speed turns into heat; slow down before the thick air.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m6.t1` | `color.pad` | H | Returning is the hard part; speed must be shed with care. | T9 |
| `cohost/color.pad.m6.t2` | `color.pad` | C | Air can brake a craft, but fast air turns speed into heat. | T9 |
| `host/color.ascent.m6.t1` | `color.ascent` | H | Thin air above slows a craft gently before the thick air. | T9 |
| `cohost/color.recovery.m6.t1` | `color.recovery` | C | A blunt shape spreads heat wide, shielding what sits behind it. | T10 |

### KH-7 — Haven at Sea

Flight: 5,500 kg comsat to transfer orbit, booster lands on Haven.

Theme: Transfer orbits: a push at the low point lifts the high point.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m7.t1` | `color.pad` | H | A push at the low point lifts the high point higher. | T11 |
| `cohost/color.pad.m7.t2` | `color.pad` | C | Speed up low down, and the far side of the path rises. | T11 |
| `host/color.coast.m7.t1` | `color.coast` | H | The path stretches into a long loop between two circles. | T11 |
| `cohost/color.recovery.m7.t1` | `color.recovery` | C | A second push at the far point rounds the path again. | T12 |

### KH-8 — Crew Day

Flight: 12,000 kg crew capsule to a 400 km orbit, gentle ride.

Theme: Crew rides stay gentle; people feel every push as weight.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m8.t1` | `color.pad` | H | Crew rides stay gentle, near three times Earth's gravity. | T13 |
| `cohost/color.pad.m8.t2` | `color.pad` | C | At three times your weight, lifting an arm feels heavy. | T13 |
| `host/color.ascent.m8.t1` | `color.ascent` | H | Crews feel each push as weight pressing them into seats. | T14 |
| `cohost/color.coast.m8.t1` | `color.coast` | C | When the push stops, the crew floats free at once. | T14 |

### KH-9 — Moon Shot

Flight: 3,000 kg lunar probe on a C3 -2 path.

Theme: Escape velocity; the Moon is days away on a curved path.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m9.t1` | `color.pad` | H | Escape takes about eleven kilometers per second from Earth. | T15 |
| `cohost/color.pad.m9.t2` | `color.pad` | C | That is more than a third again the speed for a low orbit. | T15 |
| `host/color.ascent.m9.t1` | `color.ascent` | H | The Moon is days away, along a curved path, not straight up. | T16 |
| `cohost/color.coast.m9.t1` | `color.coast` | C | Gravity slows the craft the whole way, yet it still arrives. | T17 |

### KH-10 — Big Reuse

Flight: 7,000 kg comsat to transfer orbit, core and strap-ons land at the pad.

Theme: Reuse: inspect between flights; reuse pays only when it costs less than new.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m10.t1` | `color.pad` | H | A flown rocket gets checked top to bottom before its next flight. | T18 |
| `cohost/color.pad.m10.t2` | `color.pad` | C | Crews look for heat marks, cracks, and worn parts each time. | T18 |
| `host/color.ascent.m10.t1` | `color.ascent` | H | Sharing one build across many flights can cut the cost per flight. | T19 |
| `cohost/color.recovery.m10.t1` | `color.recovery` | C | Reuse pays only when checks and fixes cost less than new. | T19 |

### KH-11 — Dawn Ledger

Flight: 2,200 kg weather sat to a 550 km sun-synchronous orbit.

Theme: Sun-synchronous orbits: same sunlight every pass, pictures that compare.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m11.t1` | `color.pad` | H | A sun-synchronous orbit crosses the equator at the same morning hour. | T20 |
| `cohost/color.pad.m11.t2` | `color.pad` | C | Its path turns slowly, keeping step with the Sun through the year. | T20, T21 |
| `host/color.ascent.m11.t1` | `color.ascent` | H | Matching shadows let pictures from different days compare cleanly. | T20 |
| `cohost/color.coast.m11.t1` | `color.coast` | C | A dawn-to-dusk path can stay in sunlight nearly the whole year. | T21 |

### KH-12 — Glass Meridian

Flight: 1,800 kg space telescope to a 600 km orbit, gentle ride.

Theme: Telescopes above the air: steadier starlight, light the air blocks.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m12.t1` | `color.pad` | H | Moving air bends starlight, so stars twinkle and pictures blur. | T22 |
| `cohost/color.pad.m12.t2` | `color.pad` | C | Above the air, a telescope sees steadier, sharper starlight. | T23 |
| `host/color.ascent.m12.t1` | `color.ascent` | H | Air blocks much ultraviolet and infrared light from space. | T22, T23 |
| `cohost/color.coast.m12.t1` | `color.coast` | C | In space, a telescope can catch light the air hides. | T22 |

### KH-13 — Flock Yard

Flight: 480 kg cubesat dispenser to a 550 km sun-synchronous orbit, Haven landing.

Theme: Rideshare: many small satellites share one ride.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m13.t1` | `color.pad` | H | Many small satellites can share one ride to orbit. | T24 |
| `cohost/color.pad.m13.t2` | `color.pad` | C | Sharing a ride spreads the cost across many teams. | T24 |
| `host/color.ascent.m13.t1` | `color.ascent` | H | Small satellites are released from their carrier one by one. | T25 |
| `cohost/color.coast.m13.t1` | `color.coast` | C | Each small satellite then follows its own path in orbit. | T25 |

### KH-14 — Harbor Trunk

Flight: 10,000 kg cargo capsule to a 400 km station orbit.

Theme: Rendezvous: match the orbit; catch up from below, where it is faster.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m14.t1` | `color.pad` | H | To meet a station, a visitor must match its orbit exactly. | T26 |
| `cohost/color.pad.m14.t2` | `color.pad` | C | A lower orbit is faster, so a visitor catches up from below. | T26 |
| `host/color.ascent.m14.t1` | `color.ascent` | H | Speeding up lifts a craft higher, where it moves slower. | T26, T27 |
| `cohost/color.coast.m14.t1` | `color.coast` | C | At the end, two craft meet at almost no relative speed. | T27 |

### KH-15 — Relay West

Flight: 6,200 kg GEO relay to transfer orbit, Haven landing.

Theme: Geostationary orbit: one spot in the sky, a quarter-second signal delay.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m15.t1` | `color.pad` | H | Thirty-six thousand kilometres up, a satellite matches Earth's spin. | T28, T29 |
| `cohost/color.pad.m15.t2` | `color.pad` | C | From the ground, that satellite seems to hang over one spot. | T28 |
| `host/color.ascent.m15.t1` | `color.ascent` | H | A signal takes about a quarter of a second there and back. | T28, T29 |
| `cohost/color.coast.m15.t1` | `color.coast` | C | A fixed dish can stay aimed at that still satellite. | T28 |

### KH-16 — Second Watch

Flight: 12,000 kg crew capsule to a 500 km orbit.

Theme: Weightlessness is freefall, not the absence of gravity.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m16.t1` | `color.pad` | H | Astronauts float because their craft falls around Earth with them. | T30 |
| `cohost/color.pad.m16.t2` | `color.pad` | C | Gravity is still there, pulling at about nine-tenths strength. | T30 |
| `host/color.ascent.m16.t1` | `color.ascent` | H | With no floor pushing up, floating feels like having no weight. | T31 |
| `cohost/color.coast.m16.t1` | `color.coast` | C | Orbit is a long fall that keeps missing the ground. | T30, T31 |

### KH-17 — Lumen Down

Flight: 4,000 kg lunar lander on a C3 -2 path.

Theme: Landing where there is no air: rockets do the braking.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m17.t1` | `color.pad` | H | The Moon has almost no air to slow a landing craft. | T32, T33 |
| `cohost/color.pad.m17.t2` | `color.pad` | C | With no air, parachutes cannot catch anything and slow it. | T32 |
| `host/color.ascent.m17.t1` | `color.ascent` | H | Rockets must do the braking, firing against the fall. | T32, T33 |
| `cohost/color.coast.m17.t1` | `color.coast` | C | Lunar gravity is about one-sixth of the pull on Earth. | T32, T33 |

### KH-18 — Nightjar

Flight: 2,500 kg probe on a fast C3 -0.4 path.

Theme: Gravity assists: borrow speed from a planet, like a slingshot.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m18.t1` | `color.pad` | H | A passing planet can lend a craft speed, like a slingshot. | T34, T35 |
| `cohost/color.pad.m18.t2` | `color.pad` | C | The craft curves around the planet and leaves on a new path. | T34 |
| `host/color.ascent.m18.t1` | `color.ascent` | H | The craft borrows motion from the planet, which barely slows. | T35 |
| `cohost/color.coast.m18.t1` | `color.coast` | C | The same trick can also slow a craft down, if needed. | T34 |

### KH-19 — Keel Reuse

Flight: 7,500 kg comsat to transfer orbit, core and strap-ons land at the pad.

Theme: Landing needs propellant held back for the trip home; then inspection.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m19.t1` | `color.pad` | H | A returning rocket saves propellant for slowing down and touching down. | T36 |
| `cohost/color.pad.m19.t2` | `color.pad` | C | Saving propellant for landing leaves less for lifting cargo. | T36, T37 |
| `host/color.ascent.m19.t1` | `color.ascent` | H | After landing, crews inspect the rocket before a later trip. | T36, T37 |
| `cohost/color.recovery.m19.t1` | `color.recovery` | C | Checked parts can be reused, saving the cost of building new ones. | T37 |

### KH-20 — Far Hold

Flight: 3,200 kg probe on a C3 -0.25 path, 4-hour coast.

Theme: Talking across deep space: light-speed signals that still take hours.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m20.t1` | `color.pad` | H | Radio waves travel at light speed, the fastest possible pace. | T38 |
| `cohost/color.pad.m20.t2` | `color.pad` | C | Even at that speed, a far probe may answer hours later. | T38 |
| `host/color.ascent.m20.t1` | `color.ascent` | H | Giant dishes on Earth must track a whisper-quiet signal. | T38, T39 |
| `cohost/color.coast.m20.t1` | `color.coast` | C | The farther the probe, the weaker and slower its messages grow. | T38, T39 |

## Sources

T-rows were checked 2026-11-11 by web search; each row states the fact as spoken and where it comes from.

- **T1**: MIT, "Rocket Principles" — http://web.mit.edu/16.00/www/aec/rocket.html — Thrust must be greater than weight for liftoff; as propellant burns, rocket mass falls and acceleration increases, so a rocket starts slowly and goes faster as it climbs.
- **T2**: FlowScholar, "Rocket Propulsion" — https://www.flowscholar.com/lessons/grade-8/engineering/aerospace-engineering/rocket-propulsion — Thrust-to-weight must be greater than 1 to lift off; as propellant burns the rocket gets lighter so the same thrust gives growing acceleration; staging drops empty tanks so a lighter vehicle takes over.
- **T3**: PhysicsTribune, "How Do Satellites Stay in Orbit? Physics Explained" — https://physicstribune.com/gravity/satellites/ — A satellite stays in orbit by falling around Earth: gravity pulls it in while sideways speed keeps it missing the surface; circular speed is about 7.7 km/s at 400 km and gravity there is still about 89% of surface gravity.
- **T4**: MagmaTribune, "Karman Line: Where Space Begins (100 km)" — https://magmatribune.com/atmosphere/karman-line/ — At 100 km the air is far too thin for wings to lift a craft; above there a vehicle is held up by orbital motion rather than by air, and 99.99997% of the atmosphere lies below 100 km.
- **T5**: Lucsus, "What Is Orbital Velocity and How Satellites Stay in Orbit" — https://www.lucsus.com/articles/space-travel-and-distances/orbital-velocity-explained/ — Too slow, and a spacecraft spirals back down into the atmosphere; too fast, and it flies off into a higher, more elongated orbit; in a circular orbit distance and speed stay constant.
- **T6**: DeltaV Academy, "Orbital Mechanics FAQ: How Satellites, Orbits, and SSA Actually Work" — https://www.deltavacademy.com/faq — Kepler's third law: the square of orbital period is proportional to the cube of the semi-major axis; higher orbits are slower and take longer, and higher altitude increases the geometric horizon and footprint.
- **T7**: TrueGeometry blog, "Sun-synchronous orbit altitude selection for specific mission requirements" — https://blog.truegeometry.com/api/exploreHTML/cb484cd32fc516807ff08ab90c2d0df5.exploreHTML — Higher altitude means a longer orbital period under Kepler's third law; lower altitudes allow higher spatial resolution, while higher altitudes cover a larger area with each pass.
- **T8**: Space Atlas, "The rocket equation explained: why reaching orbit takes so much fuel" — https://www.thespaceatlas.org/read/rocket-equation/ — The rocket equation gives speed gain as exhaust velocity times the natural logarithm of full mass divided by empty mass; in the NASA Glenn example about 90% of launch weight is propellant and payload is only about 1%.
- **T9**: Indiana Public Media (Moment of Science), "Reentry Heat" — https://www.ipm.org/show/amomentofscience/2019-09-12/reentry-heat — On the way back a craft starts at full speed and uses friction from air molecules like brakes to slow down; reentry friction can heat a shuttle to more than two thousand degrees Fahrenheit, and heating depends on speed and air thickness.
- **T10**: The Universe Episodes, "Why Do Spacecraft Get So Hot During Reentry?" — https://theuniverseepisodes.com/why-do-spacecraft-get-so-hot-during-reentry/ — A returning craft compresses air ahead of it into a shock layer reaching thousands of degrees; that is why reentry capsules are blunt rather than pointed, pushing the shock layer away from the structure.
- **T11**: Wikipedia (via Poast mirror), "Hohmann transfer orbit" — https://wiki.poast.org/w/index.php?title=Hohmann_transfer_orbit&oldid=1322191753 — A Hohmann transfer places a craft in an elliptical transfer orbit tangential to both the lower and higher circular orbits; the first burn adds energy and raises the apoapsis, and a second burn at apoapsis raises the periapsis to match the larger circular orbit.
- **T12**: DigitallyLearn, "Types of Satellite Orbits: LEO, MEO, GEO, Polar and SSO" — https://digitallylearn.com/science-tech/all-types-of-satellite-orbits-and-their-features/ — A transfer orbit is a long ellipse whose perigee is as low as low orbit and whose apogee is as high as the target orbit; at apogee the satellite fires to make the orbit circular, and each burn at perigee raises the apogee.
- **T13**: Vaia, "Problem 19: The maximum acceleration of the space shuttle" — https://www.vaia.com/en-us/textbooks/physics/physics-and-technology-for-future-presidents-10-edition/chapter-3/problem-19-the-maximum-acceleration-of-the-space-shuttle-is-/ — The maximum acceleration of the space shuttle is about 3 g, meaning astronauts feel a force three times their body weight, pushed back into their seats; acceleration is kept under a limit for crew health and safety.
- **T14**: BiologyInsights, "How Long Does It Take to Get to Space in a Rocket?" — https://biologyinsights.com/how-long-does-it-take-to-get-to-space-in-a-rocket/ — By cutoff, shuttle astronauts felt about three times their normal weight pressing them into their seats; the instant thrust stops, the sensation flips to floating freely, often within a single second.
- **T15**: CalculatorPod, "Escape Velocity Calculator" — https://www.calculatorpod.com/science/rocketry/escape-velocity-calculator/ — Earth's escape velocity at sea level is 11.186 km/s, and escape velocity is always exactly the square root of two (about 1.41) times the circular orbital velocity at the same altitude.
- **T16**: Headed for Space, "How Fast Rockets Must Travel To Reach Orbit Or Escape Earth's Gravity Entirely" — https://headedforspace.com/how-fast-rockets-travel/ — A spacecraft must reach about 11 kilometres per second to break free of Earth's gravity, and the time to travel to the Moon is approximately 3 days; the Moon is 384,400 km from Earth.
- **T17**: Physics Forums, "What's the point of Escape Velocity?" — https://www.physicsforums.com/threads/whats-the-point-of-escape-velocity.672959/ — A rocket sent away from Earth at escape speed slows gradually under gravity, its speed falling step by step as it climbs away from Earth.
- **T18**: OrbitalIntel, "How Reusable Rockets Work: Landing and Reflight" — https://www.orbitalintel.org/launch/how-reusable-rockets-work/ — A recovered vehicle is inspected for heat and stress damage, worn parts are replaced, and it is cleaned and tested before another launch; reuse saves money only when recovery and refurbishment cost less than building new.
- **T19**: FutureLens, "How reusable rockets cut the cost of reaching space" — https://futurelenshq.com/en/reusable-rockets.html — Reuse saves money only if recovery, inspections and refurbishment cost less than building a new vehicle; flying the same hardware often spreads its build cost across more flights.
- **T20**: NASA Science, "Catalog of Earth Satellite Orbits" — https://science.nasa.gov/earth/earth-observatory/catalog-of-earth-satellite-orbits/ — A sun-synchronous orbit crosses the equator at the same local solar time on every pass, keeping sunlight angle consistent so images from the same season can be compared without extreme shadow/lighting changes.
- **T21**: KeepTrack, "What Is a Sun-Synchronous Orbit (SSO)?" — https://keeptrack.space/space-terms/sun-synchronous-orbit — The SSO plane turns eastward about 0.9856 degrees per day, in step with Earth around the Sun; a dawn-dusk orbit stays in sunlight for nearly the whole year.
- **T22**: LoudComet, "Space Telescopes: Why We Put Observatories in Orbit" — https://www.loudcomet.com/space-telescopes-why-we-put-observatories-in-orbit — Pockets of air bend starlight and blur images; the atmosphere absorbs much ultraviolet and infrared light, so space opens wavelengths the ground cannot see.
- **T23**: Best in Science, "Hubble Space Telescope" — https://www.bestinscience.com/imaging/news/287-hubble-space-telescope — Above the atmosphere an orbiting telescope produces much finer detail than a ground telescope, and can observe ultraviolet and infrared light blocked by the atmosphere.
- **T24**: New Space Economy, "How Has the SpaceX Transporter Program Reshaped Satellite Earth Observation?" — https://newspaceeconomy.ca/2026/10/20/how-has-the-spacex-transporter-program-reshaped-satellite-earth-observation/ — Dedicated rideshare flights carry many small payloads on one rocket, with operators paying for a slice instead of chartering a dedicated launch.
- **T25**: Rocket Lab, "Electron Payload User Guide v8.0" — https://rocketlabcorp.com/assets/Rocket-Lab-Electron-Payload-User-Guide-8.0.pdf — Rideshare satellites get staggered deployment to precise and individual orbits, even when flying as a rideshare.
- **T26**: Wikipedia, "Space rendezvous" — https://en.wikipedia.org/wiki/Space_rendezvous — Rendezvous requires matching orbit, velocity and position; a chaser in a lower orbit has higher orbital velocity and catches up, then rises to the target orbit.
- **T27**: EngineerFix, "How Space Rendezvous Works: The Engineering Behind the Maneuver" — https://engineerfix.com/how-space-rendezvous-works-the-engineering-behind-the-maneuver/ — Speeding up raises a craft into a higher, slower orbit; the final docking approach is at only a few centimetres per second relative speed.
- **T28**: Celestrike, "Geostationary orbit: why satellites seem to stand still" — https://www.celestrike.com/geostationary-orbit — At 35,786 km above the equator a satellite circles once per Earth rotation and seems to hang in one place; a radio signal takes about a quarter of a second to go up and come back down.
- **T29**: Open University, "5.3 Signal speeds, propagation times and distance: the formula triangle" — https://www.open.edu/openlearn/mod/oucontent/view.php?id=2867&section=5.3 — A geostationary satellite is about 36,000 km up; the up-and-down radio delay is 0.24 seconds, about a quarter of a second.
- **T30**: Space Unpacked, "Why Astronauts Float: The Real Reason for Weightlessness" — https://www.spaceunpacked.com/explainers/why-astronauts-float-real-reason-weightlessness — Astronauts float because station, crew and contents fall together around Earth; gravity at that height is still about 90% of surface strength; the station constantly falls toward Earth but moves sideways fast enough to keep missing the ground.
- **T31**: Fiveable, "Apparent Weightlessness in College Physics I" — https://fiveable.me/intro-college-physics/key-terms/apparent-weightlessness — Weightlessness is free fall: gravity still acts, but no support force pushes up, so everything accelerates together and seems to float.
- **T32**: Popular Science, "Why India's Chandrayaan-3 moon landing is a big deal" — https://www.popsci.com/science/chandrayaan-3-moon-mission/ — The Moon has no atmosphere, so there is nothing to slow a spacecraft and no air for parachutes; rockets lower the probe all the way down, and lunar gravity is about one-sixth of Earth's.
- **T33**: Grokipedia, "Moon landing" — https://grokipedia.com/page/Moon_landing — The lunar near-vacuum eliminates aerodynamic braking or parachutes; surface gravity is about 1.62 m/s2, or one-sixth of Earth's, and soft landing requires fully powered descent.
- **T34**: Wikipedia, "Gravity assist" — https://en.wikipedia.org/wiki/Gravitational_slingshot — A gravity assist uses a planet's motion and gravity to alter a spacecraft's path and speed, typically to save propellant; it can increase or decrease speed.
- **T35**: ScienceABC, "Gravitational Slingshot: How Did Gravity Assist Voyager 1 & 2?" — https://www.scienceabc.com/innovation/gravitational-slingshot-how-did-gravity-assist-voyager-1-2-in-escaping-the-solar-system — A spacecraft borrows some of a planet's orbital motion to gain speed without burning fuel; the planet is slowed by an immeasurably small amount.
- **T36**: TrustAtoms, "Reusable Rockets and Launch Systems" — https://www.trustatoms.com/reusable-rockets-and-launch-systems/ — Recovery requires enough remaining propellant to slow the vehicle before touchdown, plus careful inspection between flights; reserving fuel for landing leaves less propellant for accelerating its payload.
- **T37**: Orbital Radar, "What Is a Reusable Rocket? Booster Reuse Explained" — https://orbitalradar.com/glossary/reusable-rocket — Propellant saved for landing reduces payload delivered to orbit; recovered vehicles are inspected and refurbished before flying again.
- **T38**: New Space Economy, "A History of Deep Space Communications" — https://newspaceeconomy.ca/2025/11/22/a-history-of-deep-space-communications/ — Radio travels at the speed of light, about 299,792 km/s; a signal to the farthest probe takes more than 22 hours one way, and its weak signal needs colossal antennas to catch.
- **T39**: Everything Space, "How We Talk to Spacecraft" — https://www.everythingspace.online/learn/how-we-talk-to-spacecraft — The Deep Space Network's largest dishes span 70 metres; a far probe's signal is whisper-weak (about a billionth of a billionth of a watt) and data rates fall as distance grows.

## Lines left out (could not be sourced tightly enough, or reworded)

- "Nearly half again the speed for a low orbit" (m9 draft): escape velocity is √2 ≈ 1.41×
  circular orbital speed, i.e. 41% more — "nearly half" overstates it. Reworded to "That is more
  than a third again the speed for a low orbit" (T15).
- "Nine parts in ten can be propellant" (m5): 90% is the NASA Glenn worked example in its source,
  not universal for every rocket — phrased "can be" for that reason.
- "Crew rides stay gentle, near three times Earth's gravity" (m8): anchored to the shuttle's ~3 g
  maximum; other crew vehicles peak slightly higher (~4 g), so this is not a universal crew limit.
- "Thirty-six thousand kilometres up" (m15): geostationary altitude is 35,786 km; "thirty-six
  thousand" is the rounded figure the sources state.
- "Gravity is still there, pulling at about nine-tenths strength" (m16): gravity at station height
  is ~89–90% of surface gravity.
- Reentry heat (m6): sources say the heat is mainly air *compression* ahead of the craft, not
  rubbing friction. The lines avoid the word "friction" and say only "fast air turns speed into
  heat" / "a blunt shape spreads heat wide".
- Weakest citations in the set: the Lucsus orbital-velocity page (T5) and the Physics Forums escape-
  velocity thread (T17) — basic orbital mechanics also supported by the stronger sources cited alongside
  them, but flagging them here.
- No line states a Falcon 9 Block 5 reuse count, a Starship flight count, or any 2026 record that
  is still moving. Counts that change monthly are left out on purpose.

## Generation log

<!-- Filled in by the generating PR; keep current on every regeneration. -->

- Generated: 2026-11-11, model `eleven_multilingual_v2`, voices Sarah
  (`EXAVITQu4vr4xnSDxMaL`) and Chris (`iP95p4xoKVk53GoZ742B`), settings per
  `tools/clipgen/gen_mission_tech.py` (host/co-host: stability 0.45, similarity_boost 0.8,
  style 0.25, speaker boost on).
- Characters sent: 4,666 (final script total 4,666; no reworded lines). Account before: 9,258 used /
  229,144. Account after (counter read 2026-11-11): 11,206 used / 229,144 (delta 1,948 — the
  counter lags the 4,666 sent; re-check before treating it as final). Resets Dec 2, 2026.
- One very quiet raw take (`cohost/color.recovery.m7.t1`) came out at −17.74 LUFS on the first
  loudnorm pass; it converged with one extra pass of the same pipeline run on the normalized
  output (−16.42 LUFS, −1.82 dBTP), no re-record, no setting change.
- Normalization: −16 LUFS ±1, true peak ≤ −1.5 dBTP, 96 kbps mono 44.1 kHz, same two-pass
  loudnorm as `gen_static.py`. Measured LUFS / peak / duration per clip in
  `assets/narration/manifest.json`.
- Hash-skip: lines are re-generated only when their (voice, model, spoken text, settings)
  hash is new. No unchanged line was re-billed.

## Counts (clip-count pins)

- Main before this PR: 274 clips, 50 events.
- This PR: +80 clips (all `color.*`), +0 events → **354 clips, 50 events**. Pins updated:
  `NarrationScheduleTests` (274 → 354, 50 unchanged), `NarrationTests` (274 → 354),
  `GuideCoreTests.EveryNarrationClipHasASubtitle` (274 → 354, ×2).

## What is NOT in this PR

- No narrator wiring beyond the `mission` key (already exists from the historical set). No picker,
  schedule or `ColorDirector` changes: the existing color windows and Call/Color priority already
  do the job, and the mission gate is a fact, not a new event type.
- No `web/js/narration/manifest.js` port. That file is the source of truth for line text, but
  re-running `gen_static.py` from it would drop the fact tags (see `docs/narration-facts.md`).
  The port — mission lines into `manifest.js` plus a tags-preserving generator — is a follow-up;
  until then `tools/clipgen/mission-tech-lines.json` is the source of truth for these 80 lines
  and the manifest carries their hashes.
- No two-voice exchanges. A two-voice version of a mission theme is a future recording session,
  not a rewording.
- No audio for Jessica/Callum (Cube Squad) or Crew Talk voices. Host/co-host only.
- No changes to the 80 historical takes, the Armstrong verbatim quote (m17-coa-1), or the #401
  pad fix. No Unity build, no TestFlight, no App Store change. The clips land in the Unity
  Resources tree with `.meta` files (the narration "Unity export" step); building is Trevor's call.

## Needs Trevor (not in this PR)

- **Listen check:** the 80 clips are generated but not listen-tested. Names to listen for:
  "kilometres", "propellant", "sun-synchronous", "ultraviolet", "infrared", "parachutes",
  "slingshot", "nine-tenths". If the TTS misreads one, the fix is a spelling respell in the
  spoken text (the caption keeps the real spelling) — say which ones sound wrong and they get
  re-recorded, hash-skipped for the rest.
- **The m8 "near three times" hedge:** anchored to the shuttle's ~3 g maximum; other crew vehicles
  peak slightly higher. The line stays as written unless Trevor wants it tightened.
- **The m9 "more than a third again" reword:** escape is √2 ≈ 1.41× low-orbit speed; the line is
  now strictly true rather than rounded. Flagging it here so the call is explicit.
