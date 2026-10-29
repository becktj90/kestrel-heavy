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
| `fairing.halves` | int | missing until Graphics confirms; gates `host/fairing.2` |
| `lit.stage`, `lit.engines`, `lit.propellant`, `lit.last` | event context | stage the event is about (ignition, ses1, seco, tankpress, fuel-low, abort-fuel); `lit.last` = it is the top core stage |

Propellants: `kerolox`, `hydrolox`, `methalox`, `solid`, `storable`.

## `claims`
Non-empty list of the keys the spoken line asserts; a subset of the `requires` keys. Other `requires` keys are routing only.

## Lint
`tests/narration-facts-lint.test.cjs` (`npm test`): tag form, keys and values, fact words need claims or a neutral reason, banned names, both manifests byte-equal, every mp3 present in both trees with its sha256 and a Unity `.meta`, −16 LUFS ±1 and ≤ −1.5 dBTP, and a coverage floor: every shipped design, on every pad, keeps true mission-control takes for each event it flies; and a claim-vs-design verifier that reads the spoken text of every take eligible for a design (starters, golden sets, winners, templates, presets, the hopper, on all four pads) and checks each stated fact (engine counts, stage words, strap-ons, propellants, Haven/deck/ground, pad names, "coming home") against the raw design JSON. A line that states one of those facts must claim it in a tag; a neutral line may not state it.

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
