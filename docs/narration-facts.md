# Narration fact tags (webcast clips)

Rule: the narrator only states a rocket fact when it is true for the rocket flying.
Every clip in `assets/narration/manifest.json` (byte-equal copy in `unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Narration/`) has exactly one of:

- `"neutral": true` + `"neutralReason": "<why no vehicle fact is stated>"`
- `"requires": {…}` + `"claims": [...]`

## `requires`
AND over fact keys. Values: exact (number, bool, string), a comparison string on a numeric key (`">=2"`, `"<3"`, `"!=7"`, regex `^(>=|<=|!=|>|<)-?\d+$`), or an array (any-of). A missing fact makes the condition false; an unknown key makes the take ineligible. `{}` is not allowed (use `neutral`).

| Key | Type | Meaning |
|---|---|---|
| `stages` | int | core stages |
| `stage{N}.engines`, `stage{N}.propellant` | int, enum | per core stage, 1-based |
| `strapons`, `strapons.engines`, `strapons.propellant` | int, int, enum | strap-on count, engines each, propellant (missing with none) |
| `propellant.all` | enum or `mixed` | one id shared by every stage and strap-on, else `mixed` |
| `recovery` | `none`/`barge`/`rtls` | the landing the design picked (`design.recovery.mode`; no pick = `none`). `barge` = Haven lines (Haven, deck, sea state, in the drink). `rtls` = the booster lands on the pad; a miss is ground, not sea |
| `site` | enum | launch pad id from `data/sites.json` (`pier-7`, `coastal-pad-b`, `salt-flat`, `north-reach`); flight context (`MissionBriefing.Site.Id`), not a design field. Only Pier 7 and Coastal Pad B have Haven |
| `legs`, `fairing`, `fins`, `finless` | bool | |
| `fairing.halves` | int | deliberately unset: no design data says how a fairing splits, so no take may require or state it (the "two halves" take was cut) |
| `throttle.maxq` | bool | the design has its own max-Q throttle setting (`design.guidance.maxQThrottle`; DECISIONS #89, #439). Gates the `maxq-throttle` (throttle-down starts) and `maxq-through` (throttle back up) takes. `maxq-through` fires slightly after the true q peak, so no take on it may say the peak is happening now |
| `fairing.halves` | int | missing until Graphics confirms; gates `host/fairing.2` |
| `lit.stage`, `lit.engines`, `lit.propellant`, `lit.last` | event context | stage the event is about (ignition, ses1, seco, tankpress, fuel-low, abort-fuel); `lit.last` = it is the top core stage |
| `mission` | enum `m1`–`m20` | the campaign mission being flown (`CampaignMission.Id`); absent on sandbox/free flights, so a mission-gated take stays silent there. A take that requires `mission` must claim `mission` and carry a `sources` list (see below) |

## Historical commentary (DECISIONS #77)

DECISIONS #100 and #101 (below) now set the naming rule for every surface; this section only describes the
shape of the mission-gated historical takes. A historical take:

- is a color-layer take (`"layer": "color"`) on a `color.*` event, gated to exactly one mission:
  `"requires": {"mission": "m<n>"}`, `"claims": ["mission"]`, plus `"mission": "m<n>"` and
  `"sources": [...]` fields on the manifest entry;
- states no fact about the flown design (no engine counts, no "booster", no strap-ons, no
  Haven/pad/site claims) — the claim-vs-design verifier only ever sees `claims: ["mission"]`;
- has every fact sourced in `docs/plans/per-mission-commentary.md` (S-rows reuse
  `docs/narration-sources.md`; H-rows are checked there). The lint fails a mission-gated take that
  has no sources or does not claim `mission`, and runs the no-affiliation check (DECISIONS #101) on
  every take. As with "oxygen", a historical take's "booster" is history, not a design claim, so
  the claim-vs-design verifier skips it on mission-gated historical takes.

The game's own world stays fictional: pads, ships, missions and rockets keep their names (Pier 7,
Haven, KH-n, the stack), and no real brand or trade dress is used in art, UI, store copy or code
enums. The mission id is threaded into `NarrationFacts` at the two narration entry points
(`FlightDriver` from the aimed mission, `SliceApp.BeginCountNarration` from
`AscentFlight.CurrentMission()`); a flight with no mission id simply never hears these lines.

### Real names on every surface (DECISIONS #100, #101)

Trevor, 2026-11-11 8:28 AM ET (#100): *"Use real names and real history and real dates. It should be
educational. The kestrel heavy missions are fictional but all other things can use real world
information. You can use other companies too as long as it's common knowledge."*

Trevor, 2026-11-11 8:36 AM ET (#101, superseding the banned-names part of #100): *"It is not related
to blue origin and can name new Glenn and Jaqueline if that history is relevant"* ("Jaqueline" =
Jacklyn).

- Real names, history, dates and **commonly known companies** (for example Falcon 9) are allowed on
  **every surface**: webcast color, mission previews, designer, idle or pad, menus, and Codex.
- **Blue Origin, New Glenn, Jacklyn and LC-36** may be named as real history **when the history is
  relevant and sourced**, like any other real fact.
- **Every fact needs a source id** (`docs/narration-sources.md` S- and G-rows, or
  `docs/plans/per-mission-commentary.md` H-rows) **and a Codex or Physics check** (Codex for history
  and game facts, Physics for physics claims, both for mixed lines) before it is recorded.
- **No affiliation.** Kestrel Heavy is not related to Blue Origin. No line, Codex entry or listing
  text may imply affiliation or endorsement, or that the KH rocket, Pier 7 or Haven are real Blue
  Origin assets. The lint's no-affiliation check fails any take that names Blue Origin, New Glenn,
  Jacklyn or LC-36 without sources, together with Kestrel, KH, Haven or Pier 7, or with affiliation
  wording (our, partner, official, sponsor, endorse).
- **The game's own world stays fictional**: KH missions, the KH rocket, the pad (Pier 7) and the
  barge Haven are never presented as real, and no line says the player's stack is a real rocket.
- **No implied endorsement** of any company.
- Word caps: in-flight lines (feedback, status, color) stay at 14 words per spoken line; menu,
  designer, idle and mission-preview lines may run up to 24 words.

Any fact about the flown design still needs `requires`/`claims` as usual; a history line that
states none is `neutral`.

**m20 (Far Hold) apogee note (Codex/Physics, 2026-11-11):** m20's C3 -0.25 path has an apogee of about
3.2 million km. That is outside Earth's Hill sphere (about 1.5 million km), where in reality the Sun's
gravity dominates. The game's two-body model keeps it bound to Earth, so "still tied to Earth" is fine
as a game statement, but **no line may claim the real-world path comes back to Earth**.

Propellants: `kerolox`, `hydrolox`, `methalox`, `solid`, `storable`.

## `claims`
Non-empty list of the keys the spoken line asserts; a subset of the `requires` keys. Other `requires` keys are routing only.

## Lint
`tests/narration-facts-lint.test.cjs` (`npm test`): tag form, keys and values, fact words need claims or a neutral reason, the no-affiliation check for Blue Origin names (DECISIONS #101), both manifests byte-equal, every mp3 present in both trees with its sha256 and a Unity `.meta`, −16 LUFS ±1 and ≤ −1.5 dBTP, and a coverage floor: every shipped design, on every pad, keeps true mission-control takes for each event it flies; and a claim-vs-design verifier that reads the spoken text of every take eligible for a design (starters, golden sets, winners, templates, presets, the hopper, on all four pads) and checks each stated fact (engine counts, stage words, strap-ons, propellants, Haven/deck/ground, pad names, "coming home") against the raw design JSON. A line that states one of those facts must claim it in a tag; a neutral line may not state it.

## Adding a clip
Append it after the event's existing takes (the pre-picker round-robin plays them in file order), tag it, generate with the fixed voice and settings from `tools/clipgen/gen_static.py`, normalize to −16 LUFS / −1.5 dBTP, copy the mp3 into the Unity folder with a `.meta`, and update both manifests. Note: re-running `gen_static.py` rebuilds the manifest from `web/js/narration/manifest.js` and would drop the tags and the PR A clips; port the tags first.

## Event calls the sim does not raise

`pad`, `tankpress`, `seco`, `karman` and `entry` come from `CallTriggers` (Kestrel.Core/Flight/Narration). Each is due only
when the flown design really does it, read from its generated timeline:

- `pad`: once, when the live count opens (T−12). `tankpress`: when a `count.press.s<n>` beat fires (only parts the pad
  pressurises: no solids, no storables, the pad has an umbilical; a strap-on-only press says nothing).
- `seco`: on orbit insertion right after the top core stage's guided cutoff, only if the timeline flies a SECO for it.
- `karman`: on the 100 km crossing, only if the timeline has `asc.karman`.
- `entry`: on a live entry-burn mark under 4 s old, only with `rec.entry` in the timeline and a picked landing.

Never over a call: a trigger waits while a call is queued. A pre-launch call starts only into silence (and not over a
staged guide line) and only with a take that ends 0.5 s before T−0, because launch stops the voice. A flight call past
4 s late is dropped, not said late. Each event fires once per flight.
