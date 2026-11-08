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
