# Two-voice webcast "color" lines (staged PR 2)

## What ships
25 short exchanges between the host (Sarah) and co-host (Chris), 59 lines, ~3,250 characters of text. Each exchange is ONE pre-mixed mono clip (the two voices overlap by 0.1-0.2 s on interjections), so the existing one-voice player can play it unchanged. Manifest fields added per clip: `layer: "color"`, `interruptible: true`, `lines: [{speaker, text, overlapSec}]`, `sources: ["S1"...]`. Channel stays `host` (subtitle code only accepts mc/host/cohost).
Events are NEW ids (inert until wired): `color.pad` (pad hold), `color.ascent` (first-stage climb after max-Q), `color.karman` (crossing 100 km), `color.coast` (upper-stage burn and coast), `color.recovery` (booster coast back; only for designs whose timeline has boostback/entry).
Fact rule is unchanged: anything about the flown rocket carries requires/claims and is checked by the lint test; history lines are `neutral` with a `neutralReason` and a source id in docs/narration-sources.md.

## Why the events are not wired to existing ids
NarrationSchedule plays one line at a time with one Pending slot, and only `abort*` preempts. A 6-10 s exchange on an existing event (pad, karman) could delay `ignition`, `meco`, `ses1` or `seco`. The style guide already says the host yields to the loop and the loop preempts the host with a 120 ms fade. That rule is not implemented. Until it is, the color events must not be fired.

## Settings: what exists today
- One toggle: `AudioPrefs.NarrationMuted` ("kh.audio.narration.muted"), toggled by the "Narration" button in FlightChrome (FlightChrome.cs:394-402). It mutes ALL narration (mission control, host, co-host and the Thrum guide voice). Subtitles have a separate toggle (`kh.settings.subtitles`).
- No control exists for "commentary" or "second voice" separately.

## C# changes needed (not done here, no code edits)
1. NarrationSchedule: priority classes. `Call` (mc and every existing event) and `Color` (events starting `color.` or clip `layer == "color"`).
   - A Call push stops a playing Color clip (120 ms fade) and plays immediately; a pending Color is dropped.
   - A Color push never queues behind a Call: if anything is playing or pending, drop it. Never replace a pending Call with a Color.
   - A Color clip starts only if the last line ended at least 1.5 s ago and no Call is within its look-ahead window (see 3).
2. AudioPrefs: new pref `kh.audio.commentary` with three states, Full (default), Calls only (drops all Color), Off (existing mute). FlightChrome: replace the on/off Narration button with a three-way cycle, and add the same row to the Settings screen. SettingsMirror needs the new key in its list.
3. Gameplay: fire `color.pad` only during a hold with more than 20 s left to T-minus 60 s. `color.ascent` only after max-Q and with more than 12 s to MECO. `color.karman` on the 100 km crossing. `color.coast` only when no Call is expected in the next 15 s. `color.recovery` at the boostback or entry-coast timeline event only. Drop it if the next Call is under the clip length away.
4. Picker (PR B): supply the same facts as the lint (site, recovery = the picked mode, stages, stageN.propellant). Round-robin per event, prefer the eligible clip with the most claims.
5. Mix (Kestrel Sound): Color clips at -3 dB relative to Call clips (manifest loudness is -16 LUFS for both, so use the voice bus gain), and the existing VoiceDuck on music and SFX applies to both.

## Loudness note
21 clips measured -16.4 to -16.5 LUFS with loudnorm linear. Four clips (color.pad.3, color.pad.6, color.ascent.2, color.karman.1) fell back to loudnorm dynamic mode and landed at -17.2 to -17.5 LUFS, outside the -16 +/-1 spec. They were re-rendered with a plain gain to -16 LUFS and a 4x-oversampled limiter (`normalization: "gain+limiter"`): -16.7 to -16.9 LUFS, true peak -1.7 to -2.2 dBTP. All 25 are within -17..-15 LUFS and at or below -1.5 dBTP (checked by the lint test). The "about 3 dB under calls" color mix is a hint for Sound (voice bus gain), not a manifest field.

## Tests
`tests/narration-facts-lint.test.cjs` also covers every `color.*` slot: contexts for all designs on all pads, the claim-vs-design verifier (propellant is checked against the stage named in `requires`, "oxygen" must be a liquid-oxygen stage, comparisons such as "warmer than hydrogen" are general facts), schema (two speakers, lines match text, <=14 words per line, overlap <=0.3 s, sources listed in docs/narration-sources.md), a neutral take per slot for every design, and `color.recovery` eligibility per recovery mode and pad.

## B01 mission-status calls (wired from #495)

Take 1 of 91 B01 lines is in the manifest (`batch: "B01"`); take 2 and the held lines stay in `audio/staging/B01`
(see `WIRED.md` there; `tools/clipgen/wire_b01.py` does the move). Every call goes through `CallTriggers` (4 s freshness,
never over a waiting call) and the picker's `requires` gates. A gate that cannot be read keeps the call silent.

| Event | Fires when (code) | Gate on the takes |
|---|---|---|
| `status.ontrack` | board verdict turns "On track" first time after "Climbing", live spare known and ≥ 0 (`StatusCalls`) | neutral |
| `status.short` / `.clear` | verdict turns "Short on speed" with the spare < 0; back to "On track" with spare ≥ 0; short re-arms only after a clear | neutral |
| `status.speed` | verdict turns "Speed is there" while an engine burns | neutral |
| `status.heating` | verdict "Heating hard" while q is still rising (before the peak); none within 5 s of a max-Q throttle beat | neutral |
| `engine-out` | sim `engine-out` (a tolerated stage-1 engine loss) | neutral; safety subtitle |
| `engine-out.ok` | 3 s after it, vehicle intact and climbing; lit stage 1 (none with strap-ons) | `lit.engines >= 2` |
| `sep-clear` | sim `separation` of the first core stage (a recontact fails the flight first) | `stages >= 2` |
| `landing.alt.1k/500/100/10` | booster height above the landing surface crosses the mark on a live step of the landing burn; a jump over two marks or a skip says none; dropped if a line plays | `recovery` barge or rtls |
| `landing.legs` | legs finish deploying on a live step of a planned landing | `legs`, `recovery` barge or rtls |
| `hold_start` | the count clock enters a hold (`LaunchSequence.Held`); dropped if the hold ends first | no takes yet (gen6) |

Held: hold:true lines (all `fuel-low` and `landing.fuel-low`), every `outcome.*` (no emitter), `status.offcourse(.clear)`
(no corridor-deviation signal in the Unity sim), `sep-wide` (no rough-separation signal), `landing.sink.*` and
`landing.drift` (the live test needs a Physics ruling), and seven landing takes that say more than the callout checks.
