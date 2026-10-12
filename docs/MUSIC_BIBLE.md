# Kestrel Heavy: Music Bible

Owner: Kestrel Sound (lane X2). Status: spec. Ships on Unity (U7 audio lane); the Phaser web demo keeps its current `theme` loop.
Companion docs: `docs/CUES.md` (cue sheet), `tools/music/` (MIDI sketches), `MUSIC_LICENSES.md` (rights ledger).

**Hard rules.** Every note is original. No sound-alikes, no "in the style of", no references to existing composers or works in briefs, prompts, file names, or comments. $0: generative + MIDI rendered with free instruments. AI music only if its terms clearly allow paid apps (none in use). Narration and alarms always win over music.

## 1. Musical identity

One sentence: *a small, stubborn, hopeful machine climbing out of the weather.* Warm and human at the pad, huge and physical at Max-Q, glassy and quiet in vacuum, never sad for long.

| Trait | Do | Don't |
| --- | --- | --- |
| Hope | Lydian raised 4th, open 5ths, rising contour | Heroic clichés, constant triumph |
| Physicality | Music breathes with q, throttle, staging | Wall-to-wall loud loops |
| Space | Thin out with altitude; silence is a cue | Fill vacuum with drums |
| Respect | Failure is gentle, short, and invites retry | Mocking "wah-wah" stings |

## 2. The Kestrel motif

- **Key:** D Lydian (D E F# G# A B C#). Tempo home **84 BPM**; all cues live in **72–96 BPM**.
- **Notes (6):** D4 – A4 – G#4 – B4 – A4 – F#4. Rhythm: q q e e q | w (beats 1, 1, ½, ½, 1, 4).
- **Shape:** launch up a 5th, lean on the raised 4th (the "lift"), climb past it, settle on the 3rd: hopeful, not closed. The unresolved 3rd lets it loop into the next phrase.
- **Harmony:** I – II – vi – I (D – E – Bm – D). The major II chord carries G# and is the Lydian signature; use it at every "lift" moment.
- **Short forms:** head (first 3 notes) for UI and stings; tail (last 3) for transitions.

### Variations

| ID | Variation | Tempo | Treatment | Used in |
| --- | --- | --- | --- | --- |
| M-PNO | Piano | 84 | Solo, intimate, LH broken I–II–vi–I | Cold open, menu (night), codex |
| M-VC | Cello | 76 | Octave down, slower (×1.25), over soft strings | Hangar, recovery, credits mvt 2 |
| M-PAD | Synth pad | 72 | Half speed (×2), wide, slow attack | Designer, orbit, pause |
| M-FULL | Full ensemble | 92 | Brass lead, strings, wordless choir, low strings, timpani; 2nd phrase up an octave | Max-Q peak, tier-3 success, credits finale |
| M-SUB | Sub pulse | 84 | Motif rhythm on the root, sub register, felt more than heard | Countdown, ascent L0, staging |
| M-BOX | Music box | 96 | Up an octave, double speed, plucked/bell | Title (dawn), daily, logo tail, vacuum texture pool |
| M-DEF | Defiant minor | 88 | Same contour in D minor; last note rises to the 5th instead of falling | Failure → try-again bed, hard campaign missions |

MIDI for all seven: `tools/music/out/motif_*.mid`.

## 3. Palette and instrumentation

Instruments are rendered from MIDI with free General MIDI soundfonts (licence must allow commercial use; see `MUSIC_LICENSES.md`) plus synthesis done in-engine.

| Family | Role | Register / notes |
| --- | --- | --- |
| Felt piano | Intimacy, narration-friendly | Keep under C6 when voice is live |
| Cello / low strings | Warmth, recovery, gravity | Main body 65–400 Hz |
| Strings ensemble | Ostinato engine for ascent | 8th-note cells tied to throttle |
| Warm synth pad | Glue, vacuum, pause | Slow attack, high-cut follows altitude |
| Brass | Max-Q, success only | Never during alarms |
| Wordless choir pad | Size at Max-Q / orbit | Vowel "ah" only, no words |
| Music box / bells | Wonder, UI, seeded texture | Above 1 kHz, sparse |
| Sub synth | Heartbeat, pulse | < 80 Hz; stays out of the engine-roar band during burns |
| Timpani / low hits | Ignition, staging, success | One-shots, bar-quantized |

**Frequency courtesy:** the rocket roar (`engine` bus) owns 40–250 Hz while burning; music low end ducks there (see §5). Voice owns 1–4 kHz; music has a dynamic EQ dip in that band while narration plays.

## 4. Mix targets

| Target | Value |
| --- | --- |
| Integrated loudness (music stems and full cues, measured alone) | **−18 to −16 LUFS** (aim −17) |
| True peak | ≤ −1 dBTP (matches the S1 limiter threshold −1) |
| Loudness range | 6–10 LU for beds, ≤ 6 LU for loops |
| Music bus default | 0.85 linear (S1 `level.music`) under master 0.72 |
| Narration duck | music × **0.35** while any voice line is active, release 250 ms (S1 `DUCK.musicDuck`, `holdMs`) |
| Critical/alarm dip | music × **0.28** for 780 ms on each alarm (S1 `criticalMul`, `criticalMs`), plus the alarm-exclusion rules below |
| Delivery | 48 kHz, OGG Vorbis q5 for beds, stereo; stems summed must null against the full mix within 0.5 dB |

**Priority order (never violated):** alarms/warnings → narration → critical SFX → engine → music.

**Never mask alarms.** While any alarm/warning SFX is active (abort, RUD warning, low fuel, structural), in addition to the S1 dip: (a) music ducks a further −6 dB, (b) no new brass, choir, or percussion layer may enter, (c) music applies a −6 dB notch at the alarm's fundamental band (alarm tones are authored in 800 Hz–2.5 kHz), (d) stingers are deferred until the alarm clears. The flight-sim tests in §6.6 assert this.

These numbers come from the shipped S1 mixer (`ios/KestrelHeavy/Game/js/audio/mixer.js`, `ducking.js`). The U7 Unity Audio Mixer port keeps the same bus names (`master`, `music`, `sfx`, `engine`, `voice`) and values; this doc adds music-side behaviour only.

## 5. Parameter mappings

All inputs come from `Kestrel.Core` telemetry (AscentSim) and the flight event bus (`docs/FLIGHT_EVENTS.md`). All smoothed with a 0.5 s one-pole unless noted.

| Game parameter | Normalised as | Drives |
| --- | --- | --- |
| Dynamic pressure q | `qn = q / qMaxPredicted` (0..1+), from Core aero | Ascent intensity: L1 ≥ 0.15, L2 ≥ 0.40, L3 ≥ 0.75; string ostinato velocity 60→110; brass enters at the `maxq` event |
| Max-Q event / peak | `maxq` flight event, or `dq/dt` sign flip | Bar-quantized jump to the M-FULL statement; after peak, intensity follows `qn` down with 2-bar hysteresis |
| Altitude | `an = clamp(log10(h/1 km)/2, 0, 1)` (1 km → 100 km) | Vacuum thinning: low-pass on sub/perc 18 kHz→400 Hz, percussion out by an = 0.6, reverb wet 15%→55%, pad gets the glassy high shelf, seeded texture density up |
| Ambient pressure | `p / p0` | Cross-check for thinning (used if altitude is unavailable) |
| Throttle | 0..1 | Ostinato note density (8ths ↔ 16ths) at bar lines |
| Stage events | `meco`, sep, ignition | Staging cue: 1-bar drop, low hit, re-entry of layers one by one |
| Vertical speed / attitude error | deg off-nominal | Tension layer (cluster pad) gain 0→0.6 above 5° error |
| Fuel remaining | 0..1 | Below 0.1: sub pulse doubles tempo feel (no tempo change) |
| Mission tier | 1..3 | Title/success variant choice; tier 3 unlocks M-FULL in title loop |
| Local time of day | dawn/day/dusk/night from `data/tod.json` | Title + menu orchestration (§CUES) |
| Pause | bool | Music bus low-pass 18 kHz→600 Hz over 300 ms, −6 dB |
| Narration active | voice count > 0 | S1 duck 0.35 + 1–4 kHz dynamic EQ dip |
| Alarm active | any alarm SFX | Alarm exclusion rules (§4) |

## 6. Unity adaptive engine (spec for U7)

Built on the plain Unity Audio Mixer + `AudioSource.PlayScheduled` against `AudioSettings.dspTime`. No paid middleware. Pure scheduling logic lives in a `noEngineReferences` assembly (`Kestrel.Music.Core`) so it is dotnet-testable like `Kestrel.Core`.

### 6.1 Horizontal re-sequencing
- Each cue is a set of **segments** (intro, loop A, loop B, bridge, outro, tails) with bar counts and allowed exits.
- A **transition table** (from-segment, to-cue, rule) where rule is one of: `nextBar`, `nextBeat`, `endOfPhrase` (4 or 8 bars), `immediate` (stingers/alarms only), `crossfadeBars:n`.
- Transitions are scheduled on the **next bar line** by default (bar-quantized), with the next segment's pickup/tail allowed to pre-roll.

### 6.2 Vertical layering
- Each segment has stem layers L0–L3 (L0 sub/pulse, L1 pad/harmony, L2 ostinato/rhythm, L3 melody/brass/perc) all started sample-locked; layers fade by gain only.
- Layer gain targets come from §5 mappings; changes take effect at the next beat, ramp over 1 beat, with hysteresis to stop flapping.

### 6.3 Seeded generative texture layer
- A sparse note generator (pool of Lydian upper-structure pitches, density + register from parameters) plays short music-box/bell/glass samples.
- Seeded by `missionSeed` (from the replay/daily seed) so a replay sounds the same and tests are deterministic. Uses its own `System.Random(seed)`; never `UnityEngine.Random`.
- Quantized to 8ths; never more than 2 notes per beat; muted when narration or alarms are active.

### 6.4 Stems as data
Cues, segments, stems, transitions, and mappings live in JSON (`data/music/cues.json`, `data/music/stems/*.json`) so stems can be swapped without code changes. Sketch:

```json
{
  "id": "CUE-08", "name": "ascent", "bpm": 88, "meter": [4, 4],
  "segments": [
    { "id": "climb", "bars": 8, "loop": true,
      "layers": { "L0": "asc_sub.ogg", "L1": "asc_pad.ogg", "L2": "asc_ost.ogg", "L3": "asc_brass.ogg" } }
  ],
  "layerRules": [ { "layer": "L2", "param": "qn", "on": 0.40, "off": 0.32 } ],
  "transitions": [ { "to": "CUE-09", "when": "an>=0.6", "rule": "endOfPhrase" } ],
  "duck": { "voice": 0.35, "alarm": 0.28 },
  "license": "MUSIC_LICENSES.md#original-midi"
}
```
A schema check runs with the existing JSON drift tests; each audio file referenced must have a row in `MUSIC_LICENSES.md` (test fails otherwise).

### 6.5 Music Lab (debug screen)
Dev-build-only screen (stripped from release): pick any cue/segment; sliders for q, altitude, throttle, tier, time of day; toggles for narration and alarm; per-layer meters and mute/solo; live bar/beat clock with the queued transition; seed field for the texture layer; a short-term LUFS meter on the music bus; and a "replay telemetry file" button that plays a recorded flight through the engine.

### 6.6 Flight-sim automated tests
- Feed recorded telemetry (`tests/fixtures/flights/*.json`, recorded from AscentSim / the replay tool) through `Kestrel.Music.Core` with a fake clock; no audio output.
- Assert: transition order (e.g. countdown → ignition → ascent → vacuum → orbit); every transition lands on a bar line (±1 sample at 48 kHz); L3 is active at Max-Q and off by an = 0.6; music duck = 0.35 whenever voice count > 0; music ≤ 0.28 × level and no new L3 entry while an alarm is active; stingers deferred during alarms; failure flight → failure stinger then try-again bed; same seed → identical texture event list.
- Run in the existing dotnet test job (no Unity needed), plus an EditMode smoke test once U7 lands.

## 7. Production pipeline ($0)

1. Compose / edit in MIDI (`tools/music/gen_midi.py` sketches → any free DAW).
2. Render with FluidSynth + a commercial-OK free GM soundfont (see `tools/music/README.md`), or synthesize in-engine.
3. Loudness-normalise to −17 LUFS, ≤ −1 dBTP (ffmpeg `loudnorm`).
4. Cut stems to bar lengths, export OGG, write the cue JSON.
5. Add every file to `MUSIC_LICENSES.md` in the same PR. No row, no merge.

## 8. Decisions
- **Legacy `theme` loop (2026-10-11):** it came from an AI music service and was not made on a paid plan, or Trevor isn't sure. Trevor decided on 2026-10-11 to replace it with the original Kestrel score. It is **excluded from the Unity/paid build** (see `MUSIC_LICENSES.md`). The CUE-03 title loop, built from the motif, takes its place. The existing files stay in the web demo and iOS folders for now; a separate replacement PR is planned.
- Paid upgrades are listed in `MUSIC_LICENSES.md` as an optional ask; nothing is approved.
