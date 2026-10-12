# tools/music: Kestrel motif MIDI sketches

Original MIDI sketches for the Kestrel motif, its seven variations, and a few
cue sketches. Everything here is original and owned by Trevor Beck (see
`MUSIC_LICENSES.md`). The spec lives in `docs/MUSIC_BIBLE.md` and `docs/CUES.md`.

## Generate

```sh
python3 -m venv .venv && .venv/bin/pip install -r tools/music/requirements.txt
.venv/bin/python tools/music/gen_midi.py            # writes tools/music/out/*.mid
.venv/bin/python tools/music/gen_midi.py --seed 7   # different seeded texture (cue09 only)
```

Output is deterministic: the same seed gives byte-identical files. The `.mid`
files are committed so non-Python folks can open them in any DAW.

| File | What |
| --- | --- |
| `motif_piano.mid` | Motif, solo piano, 84 BPM |
| `motif_cello.mid` | Motif an octave down, cello over soft strings, 76 BPM |
| `motif_synth_pad.mid` | Motif at half speed over pad chords, 72 BPM |
| `motif_full.mid` | Full ensemble statement (brass, strings, choir, timpani), 92 BPM |
| `motif_sub_pulse.mid` | Motif rhythm on the root, sub register, 84 BPM |
| `motif_music_box.mid` | Motif, high, double speed, music box, 96 BPM |
| `motif_defiant_minor.mid` | Same contour in D minor, ends rising, 88 BPM |
| `cue01_logo_sting.mid` | CUE-01 logo sting |
| `cue06_countdown_heartbeat.mid` | CUE-06 heartbeat (GM drum channel) |
| `cue08_adaptive_ascent.mid` | CUE-08 with each vertical layer (L0-L3) on its own track = stems |
| `cue09_vacuum_texture.mid` | CUE-09 glass pad + seeded generative texture |
| `cue11_orbit_wonder.mid` | CUE-11 orbit wonder |
| `cue12_success_tier3.mid` | CUE-12 top-tier success |
| `cue14_failure_stinger.mid` | CUE-14 respectful failure stinger |

## Render with free instruments ($0)

Use [FluidSynth](https://www.fluidsynth.org/) (LGPL-2.1, used only as a
tool; nothing of it ships in the app) with a General MIDI soundfont whose
license allows commercial use. Recommended: **FluidR3_GM** (MIT license),
packaged on Debian/Ubuntu as `fluid-soundfont-gm`.

```sh
sudo apt-get install fluidsynth fluid-soundfont-gm   # macOS: brew install fluid-synth, then download FluidR3_GM.sf2
for f in tools/music/out/*.mid; do
  fluidsynth -ni -g 0.6 -r 48000 -F "${f%.mid}.wav" /usr/share/sounds/sf2/FluidR3_GM.sf2 "$f"
done
# loudness-normalise to the -17 LUFS target from MUSIC_BIBLE (ffmpeg is free)
ffmpeg -i in.wav -af loudnorm=I=-17:TP=-1:LRA=9 -ar 48000 out.wav
```

To render stems, split a multi-track file per track in any free DAW (for
example LMMS or Ardour) or mute tracks before rendering. Before shipping any
rendered audio, add a row per file to `MUSIC_LICENSES.md` naming the soundfont
used. Do not use a soundfont whose license is unclear or non-commercial.
Rendered `.wav` files are not committed here.
