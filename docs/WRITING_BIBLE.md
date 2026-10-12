# Kestrel Heavy: Writing Bible

How every word in Kestrel Heavy sounds: captions, voice clips, Hangar Talk, briefings,
debriefs, Codex, and UI. Player-first: short, skippable, honest.

Tier tags: **[1.0]** current build, **[1.1]** next feature release, **[Later]** parked.
World facts (cast, places, arc, market) live in `docs/WORLD_BIBLE.md`.

---

## 1. Tone: kids and engineers at once [1.0]

- **Write for a smart 9-year-old sitting next to a propulsion engineer.** The kid should
  follow it, and the engineer shouldn't wince.
- **Warm, competent, curious.** The crew love the work and like the player.
- **Wonder over hype.** "The booster is standing on Haven." beats "EPIC LANDING!!!"
- **Plain words first, the real term second, once:** "the hardest push of the air,
  called Max-Q." After that, just "Max-Q."
- **No homework voice.** Never "Today we will learn..." Teach by doing: a fact shows up
  when it's useful, in one line.
- **Humor:** gentle, dry, situational. Never at the player's expense.

## 2. Reading level [1.0]

| Surface | Target | Notes |
|---|---|---|
| Captions, Hangar Talk, UI | Grade 3–5 (Flesch-Kincaid) | Short words, one idea per sentence |
| Briefings, debriefs | Grade 5–6 | One technical term max per card |
| Codex L1 [1.1] | Grade 6 | Diagram first, text second |
| Codex Engineer layer [1.1] | Unrestricted | Equations, units, references allowed |

Rules: average sentence under 12 words. Active voice. Cut "very," "really," "just."
Define jargon once per screen or don't use it. Acronyms only if they're spoken that way
in the world (Max-Q, MECO, LOX) and appear in the Codex.

## 3. Honest-physics rules [1.0]

1. **No fake numbers.** Any number on screen or in a line comes from the sim or the data
   catalogs (`web/js/data/*`, `economy.json`). Never invent a figure for flavor.
2. **Real units, SI first.** m/s, km, kN, kg, kPa, K, g. Show imperial only as a
   secondary toggle [Later]. Write units with a space: "120 km," "3.2 g."
3. **Name the real cause.** If the stack broke at Max-Q, say Max-Q, not "bad luck."
   Abort reasons map 1:1 to `abort-*` events.
4. **No magic.** No "boost" items, no "lucky" parts, no physics that change with story
   progress. Difficulty comes from assists and presets (DECISIONS #12), and the text says so.
5. **Simplify, never falsify.** "Air pushes hardest here" is a fine simplification of
   dynamic pressure. "Rockets push off the air" is false and banned.
6. **Explain failure kindly.** Every failure gets cause + one fix + encouragement, in that
   order (see section 5).
7. **Engineers can check us.** Codex Engineer-layer claims link to the sim variable or
   catalog field they come from.

## 4. Vocabulary [1.0]

| Use | Avoid |
|---|---|
| the stack, the booster, the upper (stage) | real vehicle names |
| landing burn, hoverslam | "suicide burn" |
| range safe, flight terminated, stack lost | explode, destroyed, killed, dead |
| soft catch, tipped, splashed | crashed and burned |
| methane, LOX, kerosene | LCH4 (TTS mispronounces), RP-1 in speech |
| scrubbed, hold | delayed forever |

Generic aerospace terms are welcome: Max-Q, MECO, SECO, deluge, fairing, Isp, TWR.

## 5. Failure voice: moderate penalty [1.0]

The game has a moderate failure penalty (DECISIONS #12): in 1.1 a lost payload dips reputation and costs
credits, never the save. The words match that weight: **serious, short, then forward.**

**Pattern (debrief card):** *What happened. Why. One fix. Back to it.*

> Max-Q tore the fairing. You were at full throttle through the thick air.
> Try throttling down near 12 km. Hollis's got a new fairing on the bench.

Rules:
- Name the cause in the first five words.
- One fix, not a list. The rest goes in the Codex.
- No blame words ("you failed," "wrong," "bad"). Use "the stack" as the subject when possible.
- No fake comfort ("That was great!") after a loss. Being honest is how you're kind here.
- Penalty is stated plainly once: "Reputation dipped a little." [1.1] No guilt, no repetition.
- Crewed payloads [1.1]: escape system always works, and the line says so first:
  "Crew are safe. Escape worked." Then the cause.
- KID / Relaxed: same causes, softer framing, no penalty text.

## 6. Narrator voice spec [1.0]

> **Coordination section, owned with the Kestrel Narrator Voice agent.** Folded in as
> written from their spec. Edits here go through Narrator Voice first. Source of truth for
> line text: `web/js/narration/manifest.js`. Runtime: `queue.js` plays one line at a
> time, by priority, and drops stale lines. Applies to static, pre-rendered clips
> (`assets/narration/`, `eleven_multilingual_v2`).

### 6.1 The three voices

| Channel | Voice | Role | Tone | Cadence |
|---|---|---|---|---|
| `mc` | **Eric**, mission control | Calls the state of the vehicle and tells the player what to do next. Owns every abort, warning, and instruction. | Flat, clipped, unflappable. Dry humor at most once per flight ("Coffee earned."). | Fragments ending in periods. Noun, then status, then the order. 4–8 words is ideal. |
| `host` | **Sarah**, host | Broadcast anchor. Names the milestone and its emotional beat for a viewer. Never gives orders. | Warm, assured, a little awed; somber and calm on failure. | One or two short sentences, natural speech. |
| `cohost` | **Chris**, co-host | Explainer. One plain fact about why the milestone matters. | Curious, friendly, never cute. | One or two sentences. Facts, not hype. |

Hard role rules: only MC uses the imperative ("Hold the glide."). Host and co-host never
repeat MC's exact words. Co-host never reacts emotionally to an abort (that is the host's
job). Nobody says the player's name, score numbers, or stats.

### 6.2 Length

- **Barks** (MC calls, warnings, abort reasons): **4–10 words**, at most about 3 s of audio.
  Anything over 14 words is a rewrite, no exceptions.
- **Briefings / commentary** (host, co-host): **at most 2 sentences and 14 words**, at most
  about 3.5 s. Host and co-host go stale after 3 s in the queue, so shorter is what actually
  gets heard.
- No clip over 6 s, ever.

### 6.3 When lines fire, and cooldowns

- Lines fire only on bus events (`pad`, `tankpress`, `internal`, `deluge`, `ignition`,
  `liftoff`, `maxq`, `meco`, `sep-*`, `ses1`, `fairing`, `seco`, `entry`, `haven-maxq`,
  `landing`, `touchdown`/`recovered`/`tip`/`salvage`/`splash`, `struct-warn`, `fuel-low`,
  `abort-*`).
- Priority: aborts and `health-lethal` are CRITICAL/P0 (never ducked; they can preempt).
  MC milestone calls are NORMAL. Host and co-host are LOW and get flushed by anything higher.
- Staleness (current `STALE_SEC`): MC 6 s, host 3 s, co-host 3 s, coach 2.5 s. Write each
  line so it still makes sense if it plays up to that late.
- Repeat guard: the same line isn't replayed within 8 s (`repeatWindow`). Writers supply
  **at least 2, ideally 3, variants per event per channel** so retries and replays rotate.
- Per flight, at most one host line and one co-host line per milestone, and never both in
  the same milestone unless MC has gone quiet for 3 s or more.

### 6.4 Skippability

Every line has to survive being cut off, dropped, or heard out of context:
- Put the key word first ("Max-Q.", "Sep wide.", "Landing burn."). A player who hears 1 s
  should still get it.
- No line may depend on another line (no "As I said…", no "And then…" setups). Don't open
  with "And": that's the existing cohost/pad.3 and host/touchdown.2, so drop the "And"
  when they're next rewritten.
- No ALL-CAPS for emphasis (TTS shouts it). No em dashes; use periods.

### 6.5 Static clips: no placeholders

Static clips must never contain `{player}`, `{score}`, `{mission}`, numbers that vary, or
anything else from the player's state. Lines that need those stay text-only or live TTS and
are left out of the clip library (as PR #30 already does). Say "Score clipped." rather than
"Score down 200."

### 6.6 Names and trademarks

- **Banned anywhere (spoken text, captions, code enums, file names):** Blue Origin, New
  Glenn, Jacklyn, LC-36. Also avoid any other real company, vehicle, or ship name.
- **Use instead:** missions `KH-n` (for example KH-3), launch site **Pier 7**, recovery
  barge **Haven**, rocket "the stack" / "the booster" / "the upper (stage)".
- Generic aerospace terms are fine and encouraged: Max-Q, MECO, SECO, LOX, methane,
  deluge, range safe. Write "methane" rather than "LCH4", which TTS mispronounces.

### 6.7 Example lines (from the shipped script)

| Channel | Event | Line | Words |
|---|---|---|---|
| MC (Eric) | pad | Pier 7 standing by. Terminal count is armed. | 8 |
| MC (Eric) | ignition | Ignition confirmed. Thrust is building. | 5 |
| MC (Eric) | maxq | Max-Q. q is peaking. Hold the line. | 7 |
| MC (Eric) | sep-wide | Hot sep. Steer off the upper stage. | 7 |
| MC (Eric) | haven-maxq | Through descent max-Q. Ride the diagonal into Haven. | 8 |
| MC (Eric) | recovered | Soft catch. Booster is standing on Haven. | 7 |
| MC (Eric) | abort-bird | Bird strike. Structural failure. Range safe. | 6 |
| Host (Sarah) | touchdown | Touchdown on the deck. What a sight. | 7 |
| Host (Sarah) | meco | Engine cutoff on the booster. Watch for separation. | 8 |
| Co-host (Chris) | ses1 | Upper stage ignition. Two vehicles, two jobs from here. | 9 |

### 6.8 Audio delivery spec

Integrated loudness −16 LUFS (±1 LU) for every voice, true peak at or below −1.5 dBTP,
lead silence at most 0.1 s, tail at most 0.25 s, MP3 at 96 kbps or higher, 44.1 kHz mono.
Model `eleven_multilingual_v2` for static clips, with fixed voice IDs (Eric
`cjVigY5qzO86Huf0OWal`, Chris `iP95p4xoKVk53GoZ742B`; Sarah `EXAVITQu4vr4xnSDxMaL`).
Paid-app clip use is subject to DECISIONS #13 (paid account confirmed by Trevor, else
on-device speech).

### 6.9 Open questions for Narrator Voice

- **N1.** Host/co-host lines inside a 3 s stale window: should the floor be about 8 words for
  them in practice, even though the cap is 14?
- **N2.** The `coach` channel is in `STALE_SEC` (2.5 s) and carries abort debrief tips, but
  it's not in the three-voice table. Who voices it, and is it text-only?
- **N3.** Missing variants: `abort-salvage`, `abort-splash`, `abort-tip`, `liftoff` and
  `seco` (MC) have fewer than 2 clips in `assets/narration/`; `pad` (MC) has 1.
- **N4.** Should the crew (Rook, Marlo, Hollis, Tamsin) ever take a flight channel, or stay off-air?
  This draft says off-air.
- **N5.** Localized clips: re-record per locale with the same voice IDs, or captions-only for non-English in 1.0?

## 7. Character dialect guides: Hangar Talk [1.0]

Hangar Talk is text cards in the hangar, briefings, and debriefs. Never in flight.
Each card is one speaker, at most 2 sentences, at most 90 characters (see section 10).
A dialect is a set of habits, not an accent spelled out phonetically. Never write accents as
misspellings.

### Rook, propulsion
- **Habits:** short sentences, "yep," shop words (bench, stand, burn, huff). Names engines.
- **Never:** long explanations, jargon without a picture.
- *"Yep. She lit clean. Good huff on start."*
- *"Isp is how far the fuel gets you. More is better."*

### Marlo, guidance and landing
- **Habits:** numbered steps out loud, quick exclamations, talks to her own charts.
- **Never:** sarcasm, hedging forever.
- *"Okay. One: aim off the deck. Two: divert late. Three: catch!"*
- *"Light late, stop right at the deck. That's the whole trick."*

### Hollis, structures
- **Habits:** complete, calm sentences. Understatement. One-word praise ("Tidy.").
- **Never:** exclamation marks, hurry.
- *"The air pushes hardest near Max-Q. Ease off, and the tanks will thank you."*
- *"Tidy landing. That bolt goes in the jar."*

### Tamsin, mission manager
- **Habits:** lists, schedules, "lovely," nicknames for payloads.
- **Never:** pressure to spend, guilt, real-time urgency.
- *"Lovely. Next up: one weather sat, polar orbit, nicknamed Puffin."*
- *"GTO is higher, so it costs more fuel. And pays more."*

### Tomas [1.1] and Wren [1.1]
- **Tomas:** chatty, proud of Pier 7, harbor slang. *"Deluge is primed. Best pier on the coast."*
- **Wren:** few words, sea terms. *"Swell's low. Haven's ready."*

### Shared rules
- Characters address the player as "Chief." No names inserted into audio.
- No character contradicts the sim. If Marlo says "light late," the sim agrees.
- One technical term per card at most.

## 8. Banned content [1.0]

- **Real brands and people:** no real companies, vehicles, ships, pads, people, or
  trademarks (Blue Origin, New Glenn, Jacklyn, LC-36, SpaceX, NASA marks, astronaut names,
  etc.). Applies to text, audio, code enums, file names, art.
- **Scary or violent framing:** no death, injury, explosions described graphically, fire on
  people, weapons, military payloads, disaster imagery. Failures are "stack lost," "range safe."
- **Monetization pressure:** no "buy now," no countdown offers, no FOMO, no "your friends
  are ahead," no nagging for ratings. There's no real money in the game; the words don't pretend
  otherwise.
- **Other:** no politics, religion, nationalism, real geography as a flag, romance, or
  bullying. No mocking accents. No player-name or score in static audio.

## 9. Localization notes [1.0 groundwork; 1.1 locales]

- **Meaning over words.** Each line is translated for its full meaning and rendered in the
  speaker's dialect in the target language. Never a stock phrase, never a literal translation.
  Hollis stays understated in every language; Rook stays short.
- **Translator kit per line:** speaker, event or screen, intent (one sentence), max length,
  key word that must come first, and whether it's a static clip.
- **Key word first survives translation.** If the grammar resists, restructure the line rather
  than bury "Max-Q."
- **Units:** SI everywhere; localize number formatting (decimal comma etc.), not units.
- **Terms of art:** Max-Q, MECO, SECO stay as-is unless a locale has a standard term.
- **Fictional names stay:** Kestrel, Pier 7, Haven, KH-n are not translated.
- **Length budget:** +30% text expansion allowance; UI limits in section 10 are for English,
  and layouts must flex for the expansion.
- **Audio [Later]:** localized voice clips only via the decision in N5.

## 10. Line-length and UI limits (portrait) [1.0]

Portrait-only (DECISIONS #7). The base is an iPhone-width at 390 pt; iPad gets themed fill, not more text.

| Surface | Limit (English) | Lines on screen |
|---|---|---|
| In-flight caption (MC bark) | 4–10 words, ≤ 60 chars | 1 (2 max) |
| In-flight caption (host/co-host) | ≤ 14 words, ≤ 80 chars | 2 |
| HUD label | ≤ 12 chars | 1 |
| Button | ≤ 14 chars, verb first | 1 |
| Toast / warning | ≤ 40 chars | 1 |
| Hangar Talk card | ≤ 2 sentences, ≤ 90 chars | 3 |
| Debrief card | ≤ 3 sentences, ≤ 160 chars | 5 |
| Contract card [1.1] | title ≤ 28, body ≤ 100 chars | 4 |
| Codex L1 entry [1.1] | ≤ 300 chars + diagram | scroll |

Rules:
- Captions sit above the thumb zone, never over the vehicle or the landing target.
- Min 17 pt text in flight; Dynamic Type supported for menus and cards.
- Captions persist at least 1.5 s or until the clip ends, whichever is longer.
- Every voiced line has a caption; captions can be turned off, and voice can be turned off on its own.
- Skip is always one tap and always in the same place (bottom right on cards).

## 11. Review checklist

Before any line ships: real cause named? Real units? Key word first? Under the length
limit? Fictional names only? Not scary? No pressure to spend? Would the kid follow it and
the engineer nod?
