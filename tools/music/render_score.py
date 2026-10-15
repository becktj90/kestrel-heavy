#!/usr/bin/env python3
"""Render the approved original motif with original additive instruments (no samples).
Run with tools/music/requirements.txt and numpy installed; ffmpeg encodes assets.
"""
import pathlib
import subprocess
import tempfile
import wave
import numpy as np

# Motif and harmony match tools/music/gen_midi.py (D Lydian, I–II–vi–I). Inlined so this
# render does not need the MIDI package.
TONIC = 62
LYDIAN = [0, 2, 4, 6, 7, 9, 11]
MOTIF_DEG = [0, 4, 3, 5, 4, 2]
MOTIF_DUR = [1, 1, 0.5, 0.5, 1, 4]
PROG = [[0, 2, 4], [1, 3, 5], [5, 7, 9], [0, 2, 4]]

def deg_to_pitch(deg, scale=LYDIAN, tonic=TONIC):
    octave, idx = divmod(deg, 7)
    return tonic + 12 * octave + scale[idx]

def motif(start=0.0, vel=80):
    out, t = [], start
    for d, length in zip(MOTIF_DEG, MOTIF_DUR):
        out.append((t, length * 0.95, deg_to_pitch(d), vel))
        t += length
    return out, t

def pad_prog(bars, vel=55):
    out = []
    for b in range(bars):
        degs = PROG[b % len(PROG)]
        for d in degs:
            out.append((b * 4, 4, deg_to_pitch(d - 7), vel))
    return out

RATE = 48000
OUT = pathlib.Path(__file__).resolve().parents[2] / 'unity/Assets/Kestrel.Game/Audio/Runtime/Resources/Music'


def render(name, bpm, bars, quiet=False, quality=5):
    beat = 60 / bpm
    length = bars * 4 * beat
    sound = np.zeros((round(length * RATE), 2), dtype=np.float64)
    notes = [(n, True) for n in pad_prog(bars, vel=38 if quiet else 45)]
    for bar in range(0, bars, 4):
        melody, _ = motif(bar * 4, vel=58 if quiet else 76)
        notes += [(n, False) for n in melody]
    for (start, duration, pitch, velocity), pad in notes:
        count = round((duration * beat + (0.8 if pad else 1.2)) * RATE)
        t = np.arange(count) / RATE
        freq = 440 * 2 ** ((pitch - 69) / 12)
        if pad:
            tone = np.sin(2 * np.pi * freq * t) + 0.2 * np.sin(4 * np.pi * freq * t)
            envelope = np.minimum(t / 0.6, 1) * np.minimum(np.maximum(duration * beat + 0.8 - t, 0) / 0.8, 1)
        else:
            tone = sum(np.sin(2 * np.pi * freq * h * t) * np.exp(-t * h * 0.5) / h ** 2 for h in range(1, 7))
            envelope = np.minimum(t / 0.008, 1) * np.minimum(np.maximum(duration * beat + 1.2 - t, 0) / 1.2, 1)
        signal = tone * envelope * velocity / 127 * (0.12 if pad else 0.25)
        pan = ((pitch % 12) / 11 - 0.5) * 0.35
        offset = round(start * beat * RATE)
        # Wrap release tails into the beginning for a continuous loop.
        indexes = (np.arange(count) + offset) % len(sound)
        np.add.at(sound[:, 0], indexes, signal * (0.5 - pan))
        np.add.at(sound[:, 1], indexes, signal * (0.5 + pan))
    sound *= 0.85 / max(0.85, np.max(np.abs(sound)))
    with tempfile.TemporaryDirectory() as directory:
        wav = pathlib.Path(directory) / 'score.wav'
        with wave.open(str(wav), 'wb') as stream:
            stream.setnchannels(2); stream.setsampwidth(2); stream.setframerate(RATE)
            stream.writeframes((sound * 32767).astype('<i2').tobytes())
        subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(wav), '-af',
                        'loudnorm=I=-17:TP=-1:LRA=9', '-ar', str(RATE),
                        '-c:a', 'libvorbis', '-q:a', str(quality), str(OUT / (name + '.ogg'))], check=True)
    print(name, round(length, 2), 'seconds')


if __name__ == '__main__':
    import sys
    OUT.mkdir(parents=True, exist_ok=True)
    want = sys.argv[1:]
    def want_name(name):
        return not want or name in want
    if want_name('kestrel-title'):
        render('kestrel-title', 84, 32)
    if want_name('kestrel-designer'):
        render('kestrel-designer', 80, 16, quiet=True)
    # Flight is the approved Gemini bed (kestrel-flight.ogg). This script must not overwrite it.
    if 'kestrel-flight' in want:
        print('kestrel-flight is the approved Gemini gameplay bed and is not rendered here')
