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
| `recovery` | `none`/`barge`/`rtls` | `barge` = Haven lines |
| `legs`, `fairing`, `fins`, `finless` | bool | |
| `fairing.halves` | int | missing until Graphics confirms; gates `host/fairing.2` |
| `lit.stage`, `lit.engines`, `lit.propellant`, `lit.last` | event context | stage the event is about; `lit.last` = it is the top core stage |

Propellants: `kerolox`, `hydrolox`, `methalox`, `solid`, `storable`.

## `claims`
Non-empty list of the keys the spoken line asserts; a subset of the `requires` keys. Other `requires` keys are routing only.

## Lint
`tests/narration-facts-lint.test.cjs` (`npm test`): tag form, keys and values, fact words need claims or a neutral reason, banned names, both manifests byte-equal, every mp3 present in both trees with its sha256 and a Unity `.meta`, −16 LUFS ±1 and ≤ −1.5 dBTP, and a coverage floor: every shipped design keeps true mission-control takes for each event it flies.

## Adding a clip
Append it after the event's existing takes (the pre-picker round-robin plays them in file order), tag it, generate with the fixed voice and settings from `tools/clipgen/gen_static.py`, normalize to −16 LUFS / −1.5 dBTP, copy the mp3 into the Unity folder with a `.meta`, and update both manifests. Note: re-running `gen_static.py` rebuilds the manifest from `web/js/narration/manifest.js` and would drop the tags and the PR A clips; port the tags first.
