# Per-mission historical commentary (DECISIONS #77)

Status: plan + audio shipped in this PR. Sources checked 2026-11-06 (America/New_York).
Machine-readable script: `tools/clipgen/mission-lines.json` (same lines, same ids).
Generated audio: `assets/narration/` + Unity copy, manifests byte-identical (see Generation log).

## The decision

Trevor (DECISIONS #77, PR #449, Nov 6 2026): in-game commentary may name real companies,
rockets, programs, ships and people when it tells true spaceflight history (for example
Sputnik, Apollo, Saturn V, Falcon 9, New Glenn, Jacklyn). This supersedes WRITING_BIBLE §6.6
and §8 for the historical commentary lines only. The game's own world stays fictional: our
pads, ships, missions and rockets keep their names (Pier 7, Haven, KH-n, the stack), and no
real brand or trade dress is used in art, UI, store copy or code enums. Facts must be true and
sourced; no invented quotes.

Trevor's original feedback (quoted in #449): "The launches are taking too long and are kind of
boring. Can we make them a little faster and use my eleven labs credits to add historical,
technical, engineering commentary in the game that has custom content in each level."

## What ships here

- **80 takes, 4 per mission, one theme per mission** (table per mission below). Each take is a
  single-speaker color-layer line (Sarah or Chris), at most 14 words, tied to one flight window:
  `color.pad` (count hold), `color.ascent` (after Max-Q), `color.karman` (100 km crossing),
  `color.coast` (upper-stage coast), `color.recovery` (booster coast back).
- **Voices/model:** Sarah (host) and Chris (co-host), ElevenLabs premade library voices, model
  `eleven_multilingual_v2`, same voice IDs and settings as the shipped narration
  (`docs/NARRATION_LICENSE.md`). No new voices, no Jessica/Callum, no Crew Talk voices.
- **Gating:** every take carries `requires: {"mission": "m<n>"}` + `claims: ["mission"]` +
  `sources: [...]`. The `mission` key is new (see `docs/narration-facts.md`); a take only plays
  on its own mission, and stays silent everywhere else — including sandbox, where there is no
  campaign mission. The mission id is threaded into `NarrationFacts` at the two narration
  entry points (`FlightDriver`, `SliceApp.BeginCountNarration`) from the flown campaign
  mission (`CampaignMission.Id`, e.g. `m7`); a flight with no mission id simply never hears
  these lines.
- **Length rule:** at most 14 words per line (WRITING_BIBLE §6.2, the color lint). The longest
  line is 82 characters; every clip is under the 6 s manifest cap (measured durations in the
  manifest). A line that does not fit its window is dropped by the picker, never cut mid-line
  and never said late — so a short flight (m1, m2) may only hear 2 of its 4 takes, and the
  rest rotate in on replays. That is by design.
- **One verbatim quote in the whole set:** m17-coa-1 is Armstrong's "That is one small step for
  a man, one giant leap for mankind" (July 20 1969, NASA transcript). Everything else is
  paraphrase, not quotation. No invented quotes anywhere.

## Rules these lines follow

- Real names ONLY in these 80 lines. Every other line in the game stays fictional per
  WRITING_BIBLE §6.6/§8. No real brand or trade dress in art, UI, store copy or code enums.
- Every fact has a source in the table at the end (S-rows reuse `docs/narration-sources.md`;
  H-rows are new here). Nothing is sourced to "common knowledge". Two lines that could not be
  sourced tightly were reworded, not shipped as-is (see "Lines left out").
- No line states a fact about the flown design (no engine counts, no "booster", no strap-ons,
  no Haven/pad/site claims). That is deliberate: the claim-vs-design verifier only ever sees
  `claims: ["mission"]`, so a historical line can never be misread as a claim about the
  player's rocket. Where the natural wording used "booster" or "engines", the line says
  "first stage", "F-1s" or "Merlins" instead — same history, no verifier trip.
- Numbers are spoken as words ("forty-one", "one hundred forty-three"), per the existing
  no-digits-in-static-lines convention. Dates are the only digits, and they are part of the
  history being told.
- The game world stays fictional around the history: a New Glenn line plays while the player's
  own stack sits on Pier 7 and lands on Haven. The line is about the real world; the flight
  is the player's.

## How the lines plug into the narrator system

Read first: `docs/narration-webcast.md` (two-host color, PR #437) and
`docs/narration-facts.md` (fact tags). What exists on main today:

- **Color layer is live on main.** `ColorWindows` (five windows: pad / ascent / karman / coast /
  recovery), `ColorDirector` (each window fires at most once per flight), `NarrationSchedule`
  Call-vs-Color priority (a call cuts a color line with a 120 ms fade; a color line never
  queues behind a call), and the `AudioPrefs.CommentaryMode` Full / Calls only / Off setting
  all shipped via #384/#385. The color *clips* themselves are still open in PR #437 (25
  generic two-voice exchanges). This PR adds the first mission-specific color clips.
- **Picker:** `NarrationPicker.Order` takes the eligible takes for an event, most claims first,
  then clip id. A mission take has 1 claim (`mission`); generic color takes (PR #437,
  neutral) have 0. So on a mission flight the mission take is picked first, and the generic
  color exchange is the rotation behind it. On a flight with no mission (sandbox), only the
  generic takes are eligible.
- **Windows each take attaches to** (per mission below): `color.pad` only fires in a count hold
  with >20 s to T-60; `color.ascent` only after Max-Q with >12 s to MECO (or 10 s after Max-Q
  on a no-MECO design); `color.karman` within 3 s of the 100 km crossing; `color.coast` only
  with 15 s of quiet after MECO; `color.recovery` only between boostback (or MECO + 20 s) and
  the entry burn, and only on a design with a picked landing. Missions without a landing
  (m1–m5, m8, m9, m11, m12, m14, m16–m18, m20) get no `color.recovery` take; m1–m2 get no
  `color.coast` take (short flights, no quiet coast): m1's 4th take is a second ascent variant,
  m2's 4th is a second pad variant and its 3rd is the `color.karman` take.
- **What #437 changes:** nothing about these takes except the merge order. #437 adds 25 generic
  color clips on the same five events (clip-count pins 167 → 191 there, 167 → 247 here).
  Whichever lands second rebases and adds the counts: 191 + 80 = 271 (or 247 + 25 = 272 if the
  max-Q takes in #442 also land — see "Counts" below). The two sets do not overlap: generic
  history vs per-mission history, same windows, same layer.
- **What #442 (max-Q beats) changes:** nothing. Its `maxq-through` / `maxq-throttle` takes are
  call-layer takes on `maxq` events; these are color-layer takes on `color.*` events. No
  shared ids, no shared windows.
- **Skip / warp interaction:** DECISIONS #78 ("Skip to next stage") and #436 (coast warp ×4/×10)
  compress the gaps color lines play in. The picker gate (`duration + 1.5 s` must fit the wall
  seconds to the next call) drops a line that no longer fits instead of cutting it. A skipped
  or warped flight hears fewer history lines; a real-time flight hears more. No line is ever
  wrong for being dropped.

## Character budget (ElevenLabs)

Counting method: characters of the spoken text only — the `text` field of each take, including
spaces and punctuation, excluding ids, speaker labels and manifest fields. `spokenText` equals
`text` for all 80 takes (no LOX/RUD/SES-1/em-dash substitutions apply). ElevenLabs bills the
characters sent per TTS call; the generation script counts exactly what it sent, and the
"Generation log" below records the before/after account balance. Per-mission totals:

| Mission | Takes | Characters |
|---------|------:|-----------:|
| KH-1 (First Hop) | 4 | 212 |
| KH-2 (Edge of Space) | 4 | 207 |
| KH-3 (Tiny Orbit) | 4 | 229 |
| KH-4 (Weather Eye) | 4 | 269 |
| KH-5 (Heavy Lifter) | 4 | 225 |
| KH-6 (Come Back) | 4 | 230 |
| KH-7 (Haven at Sea) | 4 | 268 |
| KH-8 (Crew Day) | 4 | 243 |
| KH-9 (Moon Shot) | 4 | 268 |
| KH-10 (Big Reuse) | 4 | 263 |
| KH-11 (Dawn Ledger) | 4 | 266 |
| KH-12 (Glass Meridian) | 4 | 251 |
| KH-13 (Flock Yard) | 4 | 265 |
| KH-14 (Harbor Trunk) | 4 | 254 |
| KH-15 (Relay West) | 4 | 256 |
| KH-16 (Second Watch) | 4 | 267 |
| KH-17 (Lumen Down) | 4 | 218 |
| KH-18 (Nightjar) | 4 | 256 |
| KH-19 (Keel Reuse) | 4 | 251 |
| KH-20 (Far Hold) | 4 | 254 |
| **Total (20 missions, 80 takes)** | **80** | **4952** |

Total script: **4952 characters** for all 80 takes. For scale, that is about 2.2% of one
month of the Creator plan (229,144 characters/month). Re-generation is hash-skipped: the
generator only calls the API for lines whose (voice, model, text, settings) hash is not in
the raw cache, so re-running it after a wording fix re-bills only the changed lines.

## The script

Voice: H = Sarah (host), C = Chris (co-host). Window: the `color.*` event the take attaches
to. Sources: S-rows are `docs/narration-sources.md`; H-rows are the source table at the end
of this doc.

### KH-1 — First Hop

Flight: 50 kg sounding box to a 10 km apogee.

Theme: Robert Goddard and the first liquid-fuel rocket (1926).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m1.1` | `color.pad` | H | In 1926, Robert Goddard flew the first liquid-fuel rocket. | S2 |
| `cohost/color.pad.m1.2` | `color.pad` | C | It rose forty-one feet, in two and a half seconds. | S2 |
| `host/color.ascent.m1.1` | `color.ascent` | H | Goddard launched from a farm field in Massachusetts. | S2 |
| `cohost/color.ascent.m1.2` | `color.ascent` | C | It burned gasoline and liquid oxygen, not gunpowder. | S2 |

### KH-2 — Edge of Space

Flight: 100 kg sounding box to a 100 km apogee.

Theme: The Kármán line: the 100 km edge of space.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m2.1` | `color.pad` | H | One hundred kilometres up is the edge of space. | S10 |
| `cohost/color.pad.m2.2` | `color.pad` | C | The line is a convention, set in 1960 for record keeping. | S10 |
| `host/color.ascent.m2.1` | `color.ascent` | H | Above the line, the air is too thin for wings. | S10 |
| `cohost/color.karman.m2.1` | `color.karman` | C | The first American in space flew above this line in 1961. | H1 |

### KH-3 — Tiny Orbit

Flight: 20 kg cubesat to a 200 km circular orbit.

Theme: Sputnik 1, the first satellite (1957).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m3.1` | `color.pad` | H | On October 4, 1957, Sputnik became the first satellite. | S13 |
| `cohost/color.pad.m3.2` | `color.pad` | C | It weighed about eighty-four kilograms, a beach ball with antennas. | S13 |
| `host/color.ascent.m3.1` | `color.ascent` | H | Sputnik circled Earth once every ninety-six minutes. | S13 |
| `cohost/color.coast.m3.1` | `color.coast` | C | Its beep was heard by radio operators around the world. | S13 |

### KH-4 — Weather Eye

Flight: 2,500 kg weather sat to a 500 km circular orbit.

Theme: TIROS-1, the first weather satellite (1960).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m4.1` | `color.pad` | H | On April 1, 1960, TIROS-1 became the first weather satellite. | H2 |
| `cohost/color.pad.m4.2` | `color.pad` | C | It carried two television cameras and weighed two hundred seventy pounds. | H2 |
| `host/color.ascent.m4.1` | `color.ascent` | H | TIROS-1 sent back nineteen thousand usable pictures in seventy-eight days. | H2 |
| `cohost/color.coast.m4.1` | `color.coast` | C | Forecasters saw cloud patterns from space for the first time. | H2 |

### KH-5 — Heavy Lifter

Flight: 12,000 kg cargo sat to a 400 km circular orbit, strap-ons allowed.

Theme: Saturn V, the heaviest lifter ever flown.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m5.1` | `color.pad` | H | The Saturn V stood one hundred eleven metres tall. | H3 |
| `cohost/color.pad.m5.2` | `color.pad` | C | Five F-1s made seven and a half million pounds of thrust. | H3 |
| `host/color.ascent.m5.1` | `color.ascent` | H | It could lift one hundred forty tonnes to low Earth orbit. | H3 |
| `cohost/color.ascent.m5.2` | `color.ascent` | C | Thirteen Saturn Vs flew, and none ever lost crew or payload. | H3 |

### KH-6 — Come Back

Flight: 8,000 kg to a 200 km orbit, booster lands back at the pad (RTLS).

Theme: The first orbital booster landing (Falcon 9, 2015) and the tower catch (Starship, 2024).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m6.1` | `color.pad` | H | On December 21, 2015, a Falcon 9 first stage landed upright. | S16 |
| `cohost/color.pad.m6.2` | `color.pad` | C | It touched down at Landing Zone 1, nine minutes after liftoff. | S16, H23 |
| `host/color.ascent.m6.1` | `color.ascent` | H | Before that, no orbital first stage had ever landed. | S16 |
| `cohost/color.recovery.m6.1` | `color.recovery` | C | In 2024, Starship's first stage was caught by its tower. | H4 |

### KH-7 — Haven at Sea

Flight: 5,500 kg comsat to transfer orbit, booster lands on Haven.

Theme: the first upright booster landing (2015) and the first droneship landing (2016). (Was: New Glenn on Jacklyn, 2025; removed under DECISIONS #100.)

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m7.1` | `color.pad` | H | ~~In November 2025, New Glenn's first stage landed on Jacklyn.~~ **Fails DECISIONS #100 (banned names); re-record as:** "In December 2015, a Falcon 9 booster first landed upright after reaching space." | ~~H5~~ S16 |
| `cohost/color.pad.m7.2` | `color.pad` | C | ~~Jacklyn waited about six hundred kilometres downrange in the Atlantic.~~ **Fails DECISIONS #100 (banned names); re-record as:** "Landing ships wait far downrange, so the booster needn't fly all the way back." | ~~H5~~ S16, G49 |
| `host/color.coast.m7.1` | `color.coast` | H | A transfer orbit stretches from low orbit out to thirty-six thousand kilometres. | H6 |
| `cohost/color.recovery.m7.1` | `color.recovery` | C | The first droneship landing was a Falcon 9, in April 2016. | H7 |

### KH-8 — Crew Day

Flight: 12,000 kg crew capsule to a 400 km orbit, gentle ride.

Theme: The first humans in space: Gagarin (1961) and Glenn (1962).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m8.1` | `color.pad` | H | On April 12, 1961, Yuri Gagarin became the first human in space. | S14 |
| `cohost/color.pad.m8.2` | `color.pad` | C | His flight lasted one hundred eight minutes, launch to landing. | S14 |
| `host/color.ascent.m8.1` | `color.ascent` | H | One orbit proved a person could live and work in space. | S14 |
| `cohost/color.coast.m8.1` | `color.coast` | C | In 1962, John Glenn became the first American to orbit Earth. | H8 |

### KH-9 — Moon Shot

Flight: 3,000 kg lunar probe on a C3 -2 path.

Theme: Luna 2, first object to reach the Moon (1959), and Apollo 8 (1968).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m9.1` | `color.pad` | H | In September 1959, Luna 2 became the first object to reach the Moon. | H9 |
| `cohost/color.pad.m9.2` | `color.pad` | C | It flew thirty-six hours and struck the Moon at three kilometres per second. | H9 |
| `host/color.ascent.m9.1` | `color.ascent` | H | Luna 2 carried no engine of its own, only instruments and pennants. | H9 |
| `cohost/color.coast.m9.1` | `color.coast` | C | In 1968, Apollo 8 carried the first crew around the Moon. | H10 |

### KH-10 — Big Reuse

Flight: 7,000 kg comsat to transfer orbit, core and strap-ons land at the pad.

Theme: Falcon Heavy and the twin-booster landing (2018).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m10.1` | `color.pad` | H | Falcon Heavy is three Falcon 9 first stages strapped side by side. | H11 |
| `cohost/color.pad.m10.2` | `color.pad` | C | Twenty-seven Merlins lit together on its first flight in 2018. | H11 |
| `host/color.ascent.m10.1` | `color.ascent` | H | Its first payload was a red Tesla Roadster, with a mannequin driver. | H11 |
| `cohost/color.recovery.m10.1` | `color.recovery` | C | The two side boosters landed together, eight minutes after liftoff. | H11, H24 |

### KH-11 — Dawn Ledger

Flight: 2,200 kg weather sat to a 550 km sun-synchronous orbit.

Theme: Landsat 1, the first Earth-resources satellite (1972).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m11.1` | `color.pad` | H | Landsat 1 launched July 23, 1972, to map Earth's resources. | H12 |
| `cohost/color.pad.m11.2` | `color.pad` | C | It flew a sun-synchronous orbit, crossing the equator at the same morning hour. | H12 |
| `host/color.ascent.m11.1` | `color.ascent` | H | Its orbit repeated every eighteen days, imaging the whole planet. | H12 |
| `cohost/color.coast.m11.1` | `color.coast` | C | Landsat began the longest Earth-observation program ever flown. | H12 |

### KH-12 — Glass Meridian

Flight: 1,800 kg space telescope to a 600 km orbit, gentle ride.

Theme: The Hubble Space Telescope (1990–2009 servicing).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m12.1` | `color.pad` | H | Hubble launched aboard Space Shuttle Discovery in April 1990. | H13 |
| `cohost/color.pad.m12.2` | `color.pad` | C | It orbits about five hundred forty kilometres up, above the blurring air. | H13 |
| `host/color.ascent.m12.1` | `color.ascent` | H | Its mirror was flawed, and astronauts fixed it in orbit in 1993. | H13 |
| `cohost/color.coast.m12.1` | `color.coast` | C | Five shuttle crews serviced Hubble, the last in 2009. | H13 |

### KH-13 — Flock Yard

Flight: 480 kg cubesat dispenser to a 550 km sun-synchronous orbit, Haven landing.

Theme: CubeSats (1999–2003) and the Transporter-1 rideshare record (2021).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m13.1` | `color.pad` | H | The CubeSat standard was drawn up in 1999, a ten-centimetre cube. | H14 |
| `cohost/color.pad.m13.2` | `color.pad` | C | The first CubeSats flew in June 2003, built mostly by students. | H14 |
| `host/color.ascent.m13.1` | `color.ascent` | H | One Falcon 9 flight carried one hundred forty-three satellites at once. | H15 |
| `cohost/color.coast.m13.1` | `color.coast` | C | That rideshare beat the old record of one hundred four satellites. | H15 |

### KH-14 — Harbor Trunk

Flight: 10,000 kg cargo capsule to a 400 km station orbit.

Theme: Dragon CRS-1, the first commercial cargo run to the station (2012).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m14.1` | `color.pad` | H | In October 2012, Dragon flew the first commercial cargo run to the station. | H16 |
| `cohost/color.pad.m14.2` | `color.pad` | C | It carried about nine hundred kilograms of supplies to the station. | H16 |
| `host/color.ascent.m14.1` | `color.ascent` | H | The station's robotic arm caught Dragon and berthed it. | H16 |
| `cohost/color.coast.m14.1` | `color.coast` | C | Dragon could bring cargo back down, not just carry it up. | H16 |

### KH-15 — Relay West

Flight: 6,200 kg GEO relay to transfer orbit, Haven landing.

Theme: Syncom 3 (1964) and Early Bird (1965): the first geostationary relays.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m15.1` | `color.pad` | H | In 1964, Syncom 3 became the first satellite to hold still over Earth. | H17 |
| `cohost/color.pad.m15.2` | `color.pad` | C | It relayed the Tokyo Olympics live from Japan to America. | H17 |
| `host/color.ascent.m15.1` | `color.ascent` | H | Early Bird followed in 1965, the first commercial satellite of its kind. | H17 |
| `cohost/color.coast.m15.1` | `color.coast` | C | Early Bird carried two hundred forty phone calls at once. | H17 |

### KH-16 — Second Watch

Flight: 12,000 kg crew capsule to a 500 km orbit.

Theme: The space station, continuously crewed since 2000.

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m16.1` | `color.pad` | H | People have lived aboard the space station without a break since 2000. | H18 |
| `cohost/color.pad.m16.2` | `color.pad` | C | Expedition 1 arrived on November 2, three people in three modules. | H18 |
| `host/color.ascent.m16.1` | `color.ascent` | H | Crews hand the station over in person, so the chain never breaks. | H18 |
| `cohost/color.coast.m16.1` | `color.coast` | C | The station hosts crews from five space agencies working together. | H18 |

### KH-17 — Lumen Down

Flight: 4,000 kg lunar lander on a C3 -2 path.

Theme: Apollo 11, the first Moon landing (1969).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m17.1` | `color.pad` | H | Apollo 11 launched on July 16, 1969, atop a Saturn V. | H19 |
| `cohost/color.pad.m17.2` | `color.pad` | C | Eagle landed in the Sea of Tranquility on July 20. | H19 |
| `host/color.ascent.m17.1` | `color.ascent` | H | Armstrong flew the lander by hand past a rocky crater. | H19 |
| `cohost/color.coast.m17.1` | `color.coast` | C | That is one small step for a man, one giant leap for mankind. | H19 |

### KH-18 — Nightjar

Flight: 2,500 kg probe on a fast C3 -0.4 path.

Theme: New Horizons, the fastest launch, to Pluto and beyond (2006–2019).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m18.1` | `color.pad` | H | New Horizons left Earth faster than any spacecraft before it. | H20 |
| `cohost/color.pad.m18.2` | `color.pad` | C | It passed the Moon's orbit in just nine hours. | H20 |
| `host/color.ascent.m18.1` | `color.ascent` | H | Nine years later it flew past Pluto, twelve thousand five hundred kilometres away. | H20 |
| `cohost/color.coast.m18.1` | `color.coast` | C | It kept flying, past Arrokoth, the most distant world ever visited. | H20 |

### KH-19 — Keel Reuse

Flight: 7,500 kg comsat to transfer orbit, core and strap-ons land at the pad.

Theme: SES-10, the first reused orbital booster (2017).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m19.1` | `color.pad` | H | March 2017, a Falcon 9 flew again, the first reused orbital rocket. | H21 |
| `cohost/color.pad.m19.2` | `color.pad` | C | That stage had already flown a station cargo run in 2016. | H21 |
| `host/color.ascent.m19.1` | `color.ascent` | H | It landed again on a droneship, the first stage to fly twice. | H21 |
| `cohost/color.recovery.m19.1` | `color.recovery` | C | Today, single first stages have flown more than thirty times each. | H21, H25 |

### KH-20 — Far Hold

Flight: 3,200 kg probe on a C3 -0.25 path, 4-hour coast.

Theme: Voyager 1, the farthest human-made object (1977– ).

| Take | Window | Voice | Line | Sources |
|------|--------|-------|------|---------|
| `host/color.pad.m20.1` | `color.pad` | H | Voyager 1 launched in 1977 and is still flying, past the planets. | H22 |
| `cohost/color.pad.m20.2` | `color.pad` | C | It crossed into interstellar space in 2012, the first spacecraft to do so. | H22 |
| `host/color.ascent.m20.1` | `color.ascent` | H | Its golden record carries greetings in fifty-five languages. | H22 |
| `cohost/color.coast.m20.1` | `color.coast` | C | Voyager 1 is the farthest human-made object from Earth. | H22 |

## Sources

S-rows live in `docs/narration-sources.md` (checked Oct 28 2026). H-rows were checked
2026-11-06 by web search; each row states the fact as spoken and where it comes from.

- **S2**: NASA, "95 Years Ago: Goddard's First Liquid-Fueled Rocket" + National Park Service + Clark University (see docs/narration-sources.md S2). First liquid-fuel flight 16 Mar 1926, 41 ft, 2.5 s, farm field, Auburn MA; gasoline + liquid oxygen.
- **S3**: Braeunig "Rocket Propellants" + NASA NTRS LOX/methane report (see docs/narration-sources.md S3). Propellant background.
- **S10**: FAI ICARE boundary page + 1960 100 km definition paper (see docs/narration-sources.md S10). Kármán line 100 km, set 1960, a convention.
- **S13**: space.skyrocket.de (Sputnik-1) + BBC (see docs/narration-sources.md S13). 4 Oct 1957, 83.6 kg, 96.2 min period.
- **S14**: ESA timeline of early spaceflights + BBC (see docs/narration-sources.md S14). 12 Apr 1961, one orbit, 108 min launch to landing.
- **S16**: SpaceNews 21 Dec 2015 + Reuters + Guinness (see docs/narration-sources.md S16). 21 Dec 2015 first orbital-class booster landing; 30 Mar 2017 first reflight.
- **H1**: Smithsonian National Air and Space Museum, "First American In Space: The Flight of Alan B. Shepard" — https://airandspace.si.edu/stories/editorial/first-american-space-flight-alan-b-shepard — 5 May 1961, Freedom 7, 187 km apogee, first American in space.
- **H2**: NOAA NESDIS, "Celebrating 65 Years of the World's First Weather Satellite" — https://www.nesdis.noaa.gov/news/celebrating-65-years-of-the-worlds-first-weather-satellite — TIROS-1, 1 Apr 1960, 270 lb, two TV cameras, 78 days, 19,000+ usable pictures.
- **H3**: NASA, "50 Years Ago: Apollo Astronauts Land, Take First Steps on Moon" — https://www.nasa.gov/centers-and-facilities/kennedy/50-years-ago-apollo-astronauts-land-take-first-steps-on-moon/ — Saturn V five F-1s, 7.5M lb thrust; + Saturn V (Wikipedia) — https://en.wikipedia.org/wiki/Saturn_V — 111 m, 140,000 kg to LEO, 13 flights, no loss of crew or payload.
- **H4**: SpaceX Flight 5 mission record, as quoted by Muskularity — https://muskularity.com/space/starship-flight-tests/ — 13 Oct 2024, "on our first try, Mechazilla caught the booster", chopstick arms, Starbase.
- **H5**: Space.com, "Blue Origin lands huge New Glenn rocket booster for 1st time after acing Mars ESCAPADE launch for NASA" — https://www.space.com/space-exploration/launches-spacecraft/blue-origin-lands-huge-new-glenn-rocket-booster-for-1st-time-after-acing-mars-escapade-launch-for-nasa — NG-2, 13 Nov 2025, ESCAPADE, booster "Never Tell Me The Odds" landed on Jacklyn ~375 miles (604 km) downrange; NG-1 (16 Jan 2025) lost the booster.
- **H6**: The mission's own target in data/campaign.json (KH-7/KH-15: ellipse 200 × 35,786 km). No external source needed; it describes the flown transfer, not history.
- **H7**: Guinness World Records, "First rocket landing at sea after deploying a payload in orbit" — https://www.guinnessworldrecords.com/world-records/484421-first-rocket-landing-at-sea-after-deploying-a-payload-in-orbit — 8 Apr 2016, Falcon 9 core B1021, CRS-8, landed on Of Course I Still Love You, 300 km off Florida; + Universe Today — https://www.universetoday.com/articles/sensational-photos-show-super-smooth-droneship-touchdown-of-spacex-falcon-booster-spacex-vp-interview
- **H8**: NASA, "60 Years Ago: John Glenn, the First American to Orbit the Earth aboard Friendship 7" — https://www.nasa.gov/history/60-years-ago-john-glenn-the-first-american-to-orbit-the-earth-aboard-friendship-7/ — 20 Feb 1962, three orbits, Friendship 7.
- **H9**: Wikipedia, "Luna 2" — https://en.wikipedia.org/wiki/lunik_2 — launched 12 Sep 1959, impacted 13/14 Sep 1959, first human-made object to reach another celestial body, 390 kg, ~36-hour flight, ~3.3 km/s impact, no propulsion of its own, carried Soviet pennants.
- **H10**: NASA Science, "The Story Behind Apollo 8's Famous Earthrise Photo" — https://science.nasa.gov/resource/the-story-behind-apollo-8s-famous-earthrise-photo/ — 24 Dec 1968, Borman, Lovell, Anders, first humans to orbit the Moon; + NASA, "Apollo 8: Christmas at the Moon" — https://www.nasa.gov/missions/apollo/apollo-8-christmas-at-the-moon/
- **H11**: NASA, "5 Years Ago: First Flight of the Falcon Heavy Rocket" — https://www.nasa.gov/history/5-years-ago-first-flight-of-the-falcon-heavy-rocket/ — 6 Feb 2018, LC-39A, three Falcon 9 boosters, 27 engines, Tesla Roadster + Starman, side boosters landed together at LZ-1/LZ-2, center core missed the droneship.
- **H12**: NASA Science, "Landsat 1" — https://science.nasa.gov/mission/landsat-1/ — launched 23 Jul 1972 (ERTS-1), sun-synchronous 917 km, 99.2°, 103 min, 18-day repeat, longest-running Earth-observation program.
- **H13**: Orbital Radar, "Hubble Space Telescope" — https://orbitalradar.com/satellites/hubble-space-telescope — launched 24 Apr 1990 aboard Discovery (STS-31), ~540 km, 28.5°, five servicing missions, last STS-125 May 2009; mirror flaw fixed by STS-61 (Dec 1993, COSTAR).
- **H14**: Wikipedia, "CubeSat" — https://en.wikipedia.org/wiki/cubesat — spec proposed 1999 by Puig-Suari (Cal Poly) and Twiggs (Stanford), 10 cm cube, first CubeSats launched June 2003 on a Russian Eurockot.
- **H15**: Space.com, "SpaceX launches a record 143 satellites on one rocket" — https://www.space.com/spacex-launches-143-satellites-transporter-1-rocket-landing — Transporter-1, 24 Jan 2021, Falcon 9, 143 satellites (133 + 10 Starlink), most on one mission; previous record 104 (PSLV, Feb 2017).
- **H16**: NASA, "First Contracted SpaceX Resupply Mission Launches with NASA Cargo to Space Station" — https://www.nasa.gov/news-release/first-contracted-spacex-resupply-mission-launches-with-nasa-cargo-to-space-station/ — CRS-1, 7 Oct 2012, first contracted commercial cargo flight, Dragon, 882 lb up, berthed 10 Oct by Canadarm2, splashed down 28 Oct; first robotic spacecraft able to return cargo to Earth.
- **H17**: Today I Found Out, "Why the Geosynchronous Region is Called the Clark Orbit" — http://www.todayifoundout.com/index.php/2024/09/the-surprisingly-interesting-story-behind-why-the-geosynchronous-region-around-the-earth-is-called-the-clark-orbit/ — Syncom 3, 19 Aug 1964, first geostationary satellite, relayed Tokyo Olympics; Early Bird (Intelsat 1), 6 Apr 1965, first commercial geostationary comsat, 240 voice circuits.
- **H18**: National Space Society, "25 Years of Humans Off-Earth" — https://nss.org/25-years-of-humans-off-earth/ — Expedition 1 (Shepherd, Gidzenko, Krikalev) boarded 2 Nov 2000, continuous occupation since; three modules (Zarya, Unity, Zvezda); + ISS is a five-agency partnership (NASA, Roscosmos, ESA, JAXA, CSA).
- **H19**: NASA, "50 Years Ago: Apollo Astronauts Land, Take First Steps on Moon" — https://www.nasa.gov/centers-and-facilities/kennedy/50-years-ago-apollo-astronauts-land-take-first-steps-on-moon/ — launched 16 Jul 1969 on Saturn V; Eagle landed 20 Jul 1969 in the Sea of Tranquility; Armstrong first stepped out ~6 hours later, Aldrin 19 min after; Armstrong manually piloted past a rocky crater; + Britannica, "Apollo 11" — https://www.britannica.com/topic/Apollo-11 — Saturn V 111 m.
- **H20**: NASA, "15 Years Ago: New Horizons Launched to Pluto and Beyond" — https://www.nasa.gov/history/15-years-ago-new-horizons-launched-to-pluto-and-beyond/ — launched 19 Jan 2006 on Atlas V, fastest spacecraft to leave Earth (~58,500 km/h), passed the Moon in 9 hours, Pluto flyby 14 Jul 2015 within 12,500 km, Arrokoth flyby 1 Jan 2019 (most distant encounter).
- **H21**: Wikipedia, "SES-10" — https://en.wikipedia.org/wiki/SES-10 — 30 Mar 2017, first reuse of an orbital-class first stage (B1021, previously CRS-8, 8 Apr 2016), landed again on a droneship; + Scientific American, "Used SpaceX Rocket Launches Satellite, Then Lands in Historic First Reflight" — https://www.scientificamerican.com/article/used-spacex-rocket-launches-first-reflight1/
- **H22**: NASA Science, "Voyager 1: The Farthest Spacecraft" — https://science.nasa.gov/mission/voyager/voyager-1/ — launched 5 Sep 1977, first spacecraft to cross the heliosphere (25 Aug 2012), farthest human-made object, Golden Record carries greetings in 55 languages.
- **H23**: The Ancient Sky, "Falcon 9 at 16" — https://theancientsky.com/spacex-tesla/falcon9-evolution/ — 21 Dec 2015, ORBCOMM, first stage touched down upright at Landing Zone 1 about nine minutes after liftoff.
- **H24**: The Odyssey Online, "SpaceX's Falcon Heavy Test Launch A Resounding Success" — https://www.theodysseyonline.com/spacexs-falcon-heavy-test-launch-resounding-success — side boosters' tandem landing at 7 minutes 58 seconds after launch.
- **H25**: The Ancient Sky, "Falcon 9 at 16" — https://theancientsky.com/spacex-tesla/falcon9-evolution/ — as of mid-2026 six Falcon 9 boosters had each flown at least 30 times; one booster made its 37th flight on 25 Aug 2026.

## Lines left out (could not be sourced tightly enough)

- "Liquid fuel can be throttled and shut down in flight" (m1 draft): true as a class claim,
  but the Goddard sources do not support it as a statement about his 1926 rocket. Reworded
  to "It burned gasoline and liquid oxygen, not gunpowder" (S2).
- A "more than thirty times" reuse line for m19 was kept only after two sources confirmed it
  (H21 + H25); the first draft said "boosters now fly twenty times", which no source stated
  as a general fact.
- No line states a Falcon 9 Block 5 reuse count, a Starship flight count, or any 2026 record
  that is still moving. Counts that change monthly are left out on purpose.

## Generation log

<!-- Filled in by the generating PR; keep current on every regeneration. -->

- Generated: 2026-11-06, model `eleven_multilingual_v2`, voices Sarah
  (`EXAVITQu4vr4xnSDxMaL`) and Chris (`iP95p4xoKVk53GoZ742B`), settings per
  `tools/clipgen/gen_static.py` (host/co-host: stability 0.45, similarity_boost 0.8,
  style 0.25, speaker boost on).
- Characters sent: 5,084 (one-line auth test 58 + first run 4,900 + two reworded lines
  126; final script total 4,952 — 132 of the sent chars were two superseded wordings
  re-recorded one word shorter to fit the 6 s cap). Account before: 6,756 used / 229,144.
  Account after (counter read 2026-11-06): 8,992 used / 229,144 (delta 2,236 — the
  counter lags the 5,084 sent; re-check before treating it as final). Resets Dec 2, 2026.
- Three very quiet raw takes (LRA ~ 0) came out 1–2 dB under −16 LUFS on the first
  loudnorm pass; each converged with one extra pass of the same pipeline (no re-record,
  no setting change). Two lines were reworded one word shorter to fit the 6 s cap
  (m11.1 dropped "on", m19.1 dropped "In"); facts and sources unchanged.
- Normalization: −16 LUFS ±1, true peak ≤ −1.5 dBTP, 96 kbps mono 44.1 kHz, same two-pass
  loudnorm as `gen_static.py`. Measured LUFS / peak / duration per clip in
  `assets/narration/manifest.json`.
- Hash-skip: lines are re-generated only when their (voice, model, spoken text, settings)
  hash is new. No unchanged line was re-billed.

## Counts (clip-count pins)

- Main before this PR: 167 clips, 44 events.
- This PR: +80 clips (all `color.*`), +5 events → **247 clips, 49 events**. Pins updated:
  `NarrationScheduleTests` (167 → 247, 44 → 49), `NarrationTests` (167 → 247),
  `GuideCoreTests.EveryNarrationClipHasASubtitle` (167 → 247, ×2).
- If PR #437 (two-host color, +25 clips, same 5 events) lands first: 191 + 80 = **271**.
  If PR #442 (max-Q beats, +3 clips, no new events) also lands: **274**. Whoever lands second
  rebases and fixes the pins; the lint test (`tests/narration-facts-lint.test.cjs`) does not
  pin counts, so it needs no rebase for this.

## What is NOT in this PR

- No narrator wiring beyond the `mission` key + the two one-line fact threadings
  (`FlightDriver`, `SliceApp.BeginCountNarration`). No picker, schedule or `ColorDirector`
  changes: the existing color windows and Call/Color priority already do the job, and the
  mission gate is a fact, not a new event type.
- No `web/js/narration/manifest.js` port. That file is the source of truth for line text, but
  re-running `gen_static.py` from it would drop the fact tags (see `docs/narration-facts.md`).
  The port — mission lines into `manifest.js` plus a tags-preserving generator — is a
  follow-up; until then `tools/clipgen/mission-lines.json` is the source of truth for these
  80 lines and the manifest carries their hashes.
- No two-voice exchanges. #437's color clips are pre-mixed host+co-host exchanges; these 80
  are single-speaker takes, which keeps each clip short, cheap and independently droppable.
  A two-voice version of a mission theme is a future recording session, not a rewording.
- No audio for Jessica/Callum (Cube Squad) or Crew Talk voices. Host/co-host only.
- No Unity build, no TestFlight, no App Store change. The clips land in the Unity Resources
  tree with `.meta` files (the narration "Unity export" step); building is Trevor's call.

## Needs Trevor (not in this PR)

- **Listen check:** the 80 clips are generated but not listen-tested. Names to listen for:
  "Jacklyn", "Arrokoth", "F-1s", "Merlins", "TIROS-1", "Syncom". If the TTS misreads one,
  the fix is a spelling respell in the spoken text (the caption keeps the real spelling) —
  say which ones sound wrong and they get re-recorded, hash-skipped for the rest.
- **The Armstrong quote:** m17-coa-1 quotes Armstrong verbatim. It is the only quote in the
  set and it is sourced to the NASA transcript, but it is his words in a paid game — flagging
  it here so the call to keep or paraphrase it is explicit.
- **"Today" in m19-rec-1:** "Today, single first stages have flown more than thirty times
  each" dates itself. It stays true as counts grow, but if it ever sounds stale it is one
  line to re-record, not a script problem.
- **Merge order with #437:** this PR and #437 touch the same pins and the same five events.
  Either order works; the second PR rebases the counts (see "Counts").
