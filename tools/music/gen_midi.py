#!/usr/bin/env python3
"""Generate original MIDI sketches of the Kestrel motif, its variations, and a
few cue sketches into tools/music/out/. Deterministic: same seed -> same bytes.

Usage:  python3 tools/music/gen_midi.py [--seed 1] [--out tools/music/out]

All material here is original to Kestrel Heavy (owned by Trevor Beck).
See MUSIC_LICENSES.md and docs/MUSIC_BIBLE.md.
"""
import argparse
import os
import random

import mido

PPQ = 480
BAR = PPQ * 4

# Home key: D Lydian (D E F# G# A B C#). The raised 4th (G#) is the "lift".
TONIC = 62  # D4
LYDIAN = [0, 2, 4, 6, 7, 9, 11]
AEOLIAN = [0, 2, 3, 5, 7, 8, 10]

# The Kestrel motif: 6 notes, scale degrees (0-based, may exceed 6 for next
# octave) and durations in beats. Shape: launch up a 5th, lean on the #4,
# climb past it, settle on the 3rd = hopeful, unresolved.
MOTIF_DEG = [0, 4, 3, 5, 4, 2]
MOTIF_DUR = [1, 1, 0.5, 0.5, 1, 4]

# GM programs (0-based)
PIANO, CELLO, PAD, MUSIC_BOX, STRINGS, BRASS, CHOIR, SYNTH_BASS, TIMPANI = 0, 42, 89, 10, 48, 61, 52, 38, 47


def deg_to_pitch(deg, scale=LYDIAN, tonic=TONIC):
    octave, idx = divmod(deg, 7)
    return tonic + 12 * octave + scale[idx]


class Song:
    def __init__(self, bpm, name):
        self.mid = mido.MidiFile(ticks_per_beat=PPQ, type=1)
        meta = mido.MidiTrack()
        meta.append(mido.MetaMessage("track_name", name=name, time=0))
        meta.append(mido.MetaMessage("set_tempo", tempo=mido.bpm2tempo(bpm), time=0))
        meta.append(mido.MetaMessage("time_signature", numerator=4, denominator=4, time=0))
        meta.append(mido.MetaMessage("copyright", text="(c) Trevor Beck. Original. All rights reserved.", time=0))
        self.mid.tracks.append(meta)
        self.ch = 0

    def track(self, name, program, notes, channel=None):
        """notes: list of (start_beats, dur_beats, pitch, velocity)."""
        if channel is None:
            channel = self.ch
            self.ch += 1
            if self.ch == 9:
                self.ch = 10
        tr = mido.MidiTrack()
        tr.append(mido.MetaMessage("track_name", name=name, time=0))
        if channel != 9:
            tr.append(mido.Message("program_change", program=program, channel=channel, time=0))
        ev = []
        for s, d, p, v in notes:
            a, b = int(round(s * PPQ)), int(round((s + d) * PPQ))
            ev.append((a, 1, p, v))
            ev.append((max(a + 1, b), 0, p, 0))
        ev.sort(key=lambda e: (e[0], e[1]))
        last = 0
        for t, on, p, v in ev:
            kind = "note_on" if on else "note_off"
            tr.append(mido.Message(kind, note=p, velocity=v, channel=channel, time=t - last))
            last = t
        self.mid.tracks.append(tr)

    def save(self, path):
        self.mid.save(path)


def motif(start=0.0, scale=LYDIAN, tonic=TONIC, octave=0, vel=80, stretch=1.0, degs=MOTIF_DEG, durs=MOTIF_DUR):
    out, t = [], start
    for d, l in zip(degs, durs):
        out.append((t, l * stretch * 0.95, deg_to_pitch(d + 7 * octave, scale, tonic), vel))
        t += l * stretch
    return out, t


def chord(start, dur, degs, scale=LYDIAN, tonic=TONIC, octave=-1, vel=60):
    return [(start, dur, deg_to_pitch(d + 7 * octave, scale, tonic), vel) for d in degs]


# Harmony: I - II (the Lydian major II) - vi - I. II carries the #4.
PROG = [[0, 2, 4], [1, 3, 5], [5, 7, 9], [0, 2, 4]]
PROG_MINOR = [[0, 2, 4], [5, 7, 9], [3, 5, 7], [4, 6, 8]]  # i - VI - iv - v


def pad_prog(bars, prog=PROG, scale=LYDIAN, vel=55, octave=-1):
    out = []
    for b in range(bars):
        out += chord(b * 4, 4, prog[b % len(prog)], scale, octave=octave, vel=vel)
    return out


def bass_prog(bars, prog=PROG, scale=LYDIAN, vel=70, pulse=None):
    out = []
    for b in range(bars):
        root = deg_to_pitch(prog[b % len(prog)][0] - 14, scale)
        if pulse:
            for k in range(int(4 / pulse)):
                out.append((b * 4 + k * pulse, pulse * 0.6, root, vel))
        else:
            out.append((b * 4, 4, root, vel))
    return out


# ---------------------------------------------------------------- variations
def v_piano(o):
    s = Song(84, "Kestrel motif - piano")
    m, _ = motif(0, vel=72)
    m2, _ = motif(8, vel=78)
    s.track("Piano RH", PIANO, m + m2)
    s.track("Piano LH", PIANO, [(b * 4 + k, 0.9, p, 50) for b in range(4) for k, p in enumerate(
        [deg_to_pitch(PROG[b][0] - 7), deg_to_pitch(PROG[b][2] - 7), deg_to_pitch(PROG[b][1]), deg_to_pitch(PROG[b][2] - 7)])])
    s.save(os.path.join(o, "motif_piano.mid"))


def v_cello(o):
    s = Song(76, "Kestrel motif - cello")
    m, _ = motif(0, octave=-1, vel=85, stretch=1.25)
    s.track("Cello", CELLO, m)
    s.track("Strings bed", STRINGS, pad_prog(3, vel=40, octave=-1))
    s.save(os.path.join(o, "motif_cello.mid"))


def v_pad(o):
    s = Song(72, "Kestrel motif - synth pad")
    m, _ = motif(0, octave=0, vel=60, stretch=2)
    s.track("Pad lead", PAD, m)
    s.track("Pad chords", PAD, pad_prog(4, vel=45))
    s.save(os.path.join(o, "motif_synth_pad.mid"))


def v_full(o):
    s = Song(92, "Kestrel motif - full ensemble")
    m, _ = motif(0, vel=95)
    m2, _ = motif(8, vel=105, octave=1)
    s.track("Brass", BRASS, m + m2)
    s.track("Strings", STRINGS, pad_prog(4, vel=70, octave=0))
    s.track("Choir", CHOIR, pad_prog(4, vel=50, octave=-1))
    s.track("Low strings", CELLO, bass_prog(4, vel=80))
    s.track("Timpani", TIMPANI, [(b * 4, 1, deg_to_pitch(PROG[b][0] - 14), 90) for b in range(4)])
    s.save(os.path.join(o, "motif_full.mid"))


def v_sub(o):
    s = Song(84, "Kestrel motif - sub pulse")
    # Rhythm of the motif on the root only, two octaves down, as a felt pulse.
    m, _ = motif(0, octave=-2, vel=100, degs=[0, 0, 0, 4, 0, 0])
    s.track("Sub", SYNTH_BASS, m)
    s.track("Sub 8ths", SYNTH_BASS, [(8 + i * 0.5, 0.3, deg_to_pitch(-14), 80) for i in range(16)])
    s.save(os.path.join(o, "motif_sub_pulse.mid"))


def v_music_box(o):
    s = Song(96, "Kestrel motif - music box")
    m, _ = motif(0, octave=1, vel=70, durs=[0.5, 0.5, 0.25, 0.25, 0.5, 2])
    m2, _ = motif(4, octave=1, vel=60, durs=[0.5, 0.5, 0.25, 0.25, 0.5, 2])
    s.track("Music box", MUSIC_BOX, m + m2)
    s.save(os.path.join(o, "motif_music_box.mid"))


def v_defiant(o):
    s = Song(88, "Kestrel motif - defiant minor")
    # Same contour in D Aeolian, last note pushed UP to the 5th: bowed, not broken.
    m, _ = motif(0, scale=AEOLIAN, vel=95, octave=-1, degs=[0, 4, 3, 5, 4, 4])
    m2, _ = motif(8, scale=AEOLIAN, vel=105, degs=[0, 4, 3, 5, 6, 7])
    s.track("Low strings + brass", CELLO, m + m2)
    s.track("Brass", BRASS, m2)
    s.track("Chords", STRINGS, pad_prog(4, PROG_MINOR, AEOLIAN, vel=65))
    s.save(os.path.join(o, "motif_defiant_minor.mid"))


# ------------------------------------------------------------------- cues
def c_logo(o):
    s = Song(84, "CUE-01 logo sting")
    m, _ = motif(0, vel=90, degs=MOTIF_DEG[:3] + [7], durs=[0.5, 0.5, 0.5, 2.5])
    s.track("Piano", PIANO, m)
    s.track("Pad", PAD, chord(1.5, 2.5, [0, 4, 7, 10], octave=0, vel=50))
    s.save(os.path.join(o, "cue01_logo_sting.mid"))


def c_countdown(o, rng):
    s = Song(72, "CUE-06 countdown heartbeat")
    beats = []
    for b in range(8):  # heartbeat: lub-dub per beat, tightening
        for k in range(4):
            t = b * 4 + k
            beats.append((t, 0.2, 36, 100 - (0 if k == 0 else 20)))
            beats.append((t + 0.3, 0.15, 36, 70))
    s.track("Heartbeat (GM kit)", 0, beats, channel=9)
    s.track("Tension pad", PAD, [(0, 32, deg_to_pitch(-7), 40), (16, 16, deg_to_pitch(-4), 45)])
    s.save(os.path.join(o, "cue06_countdown_heartbeat.mid"))


def c_ascent(o, rng):
    s = Song(88, "CUE-08 adaptive ascent (stems as tracks)")
    bars = 8
    s.track("L0 sub pulse", SYNTH_BASS, bass_prog(bars, pulse=0.5, vel=80))
    s.track("L1 pad", PAD, pad_prog(bars, vel=50))
    s.track("L2 strings ostinato", STRINGS, [
        (b * 4 + i * 0.5, 0.45, deg_to_pitch(PROG[b % 4][i % 3]), 65) for b in range(bars) for i in range(8)])
    m, _ = motif(16, vel=100)
    m2, _ = motif(24, vel=110, octave=1)
    s.track("L3 brass motif (Max-Q)", BRASS, m + m2)
    perc = [(b * 4 + k, 0.2, 36 if k % 2 == 0 else 38, 90) for b in range(4, bars) for k in range(4)]
    s.track("L3 percussion", 0, perc, channel=9)
    s.save(os.path.join(o, "cue08_adaptive_ascent.mid"))


def c_vacuum(o, rng):
    s = Song(72, "CUE-09 thinning to vacuum + seeded texture")
    s.track("Glass pad", PAD, chord(0, 16, [0, 4, 7, 10], octave=0, vel=40))
    pool = [deg_to_pitch(d) for d in (7, 9, 10, 11, 13, 14)]
    tex = [(round(rng.uniform(0, 15), 2), 1.5, rng.choice(pool), rng.randint(30, 55)) for _ in range(14)]
    s.track("Seeded texture (music box)", MUSIC_BOX, tex)
    s.save(os.path.join(o, "cue09_vacuum_texture.mid"))


def c_orbit(o):
    s = Song(72, "CUE-11 orbit wonder")
    m, _ = motif(4, octave=1, vel=60, stretch=2)
    s.track("Celesta-ish", MUSIC_BOX, m)
    s.track("Pad", PAD, pad_prog(6, vel=45))
    s.track("Cello", CELLO, bass_prog(6, vel=55))
    s.save(os.path.join(o, "cue11_orbit_wonder.mid"))


def c_fail(o):
    s = Song(76, "CUE-14 respectful failure stinger")
    m, _ = motif(0, scale=AEOLIAN, octave=-1, vel=60, degs=[0, 4, 3, 2], durs=[1, 1, 1, 3])
    s.track("Cello", CELLO, m)
    s.track("Pad", PAD, chord(0, 6, [0, 2, 4], AEOLIAN, vel=40))
    s.save(os.path.join(o, "cue14_failure_stinger.mid"))


def c_success(o):
    s = Song(92, "CUE-12 success (tier 3)")
    m, _ = motif(0, vel=105, octave=0)
    s.track("Brass", BRASS, m + [(8, 4, deg_to_pitch(7), 110)])
    s.track("Strings", STRINGS, chord(0, 4, [0, 2, 4], octave=0, vel=70) + chord(4, 8, [0, 4, 7, 9], octave=0, vel=80))
    s.track("Timpani", TIMPANI, [(8, 1, deg_to_pitch(-14), 110)])
    s.save(os.path.join(o, "cue12_success_tier3.mid"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--seed", type=int, default=1)
    ap.add_argument("--out", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "out"))
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    rng = random.Random(a.seed)
    for f in (v_piano, v_cello, v_pad, v_full, v_sub, v_music_box, v_defiant, c_logo, c_orbit, c_fail, c_success):
        f(a.out)
    for f in (c_countdown, c_ascent, c_vacuum):
        f(a.out, rng)
    files = sorted(x for x in os.listdir(a.out) if x.endswith(".mid"))
    for x in files:
        mf = mido.MidiFile(os.path.join(a.out, x))
        print(f"{x:36s} {len(mf.tracks)-1:2d} tracks {mf.length:6.1f}s")
    print(f"{len(files)} files -> {a.out}")


if __name__ == "__main__":
    main()
