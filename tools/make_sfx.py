#!/usr/bin/env python3
"""Makes the sounds of the two defenders the Android version adds, next to the CPC's ones
that tools/import_assets.sh copies from ../VrouvesPC:

    tools/make_sfx.py        ->  Vrouves/Assets/audio/sfx_sling.wav, sfx_bell.wav

The Shepherd's sling is an AY effect like the others (VrouvesPC/tools/make_audio.py plays
the CPC's tables one step per 50 Hz frame: tone period, noise period, volume); the
Priest's bell is a small church bell, its partials dying away. Needs numpy (../VrouvesPC/.venv/bin/python has it).
"""
import os
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Vrouves", "Assets", "audio")
RATE = 44100
FRAME = 1 / 50

# the sling: whirled overhead twice, then the stone let go
SLING = [(0, 9, 8), (0, 6, 11), (0, 9, 8), (0, 6, 12), (0, 4, 14), (420, 3, 13), (340, 5, 11),
         (0, 8, 9), (0, 12, 6), (0, 16, 3)]


def write(name, x):
    x = np.clip(x, -1, 1)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((x * 32000).astype("<i2").tobytes())


def ay(steps, seed=1):
    """as VrouvesPC/tools/make_audio.py's sfx(): square tone and noise, one step a frame"""
    rng = np.random.default_rng(seed)
    n1 = int(FRAME * RATE)
    out, phase, lfsr, nph = [], 0.0, 1.0, 0.0
    for (period, nper, vol) in steps:
        amp = 0 if vol == 0 else 2 ** ((vol - 15) / 2)
        t = np.zeros(n1)
        if period:
            f = 1e6 / (16 * period)
            ph = phase + np.arange(1, n1 + 1) * f / RATE
            phase = ph[-1]
            t += np.sign(np.sin(2 * np.pi * ph)) * 0.6
        if nper:
            fn = 1e6 / (16 * nper)
            seq = np.empty(n1)
            for i in range(n1):
                nph += fn / RATE
                if nph >= 1:
                    nph %= 1
                    lfsr = rng.choice((-1.0, 1.0))
                seq[i] = lfsr
            t += seq * 0.6
        out.append(t * amp)
    return np.concatenate(out + [np.zeros(int(0.03 * RATE))]) * 0.8


def bell(f0=587.0, dur=1.1):
    """partials of a bell (hum, prime, minor third, fifth, octave...), each dying on its own"""
    t = np.arange(int(dur * RATE)) / RATE
    x = np.zeros_like(t)
    for ratio, amp, decay in [(0.5, 0.35, 1.6), (1.0, 0.6, 2.2), (1.2, 0.35, 3.0), (1.5, 0.25, 3.5),
                              (2.0, 0.3, 4.0), (2.5, 0.15, 5.5), (3.0, 0.12, 7.0), (4.2, 0.08, 9.0)]:
        x += amp * np.exp(-decay * t) * np.sin(2 * np.pi * f0 * ratio * t + ratio)
    strike = np.exp(-60 * t) * np.random.default_rng(3).uniform(-1, 1, t.size) * 0.25
    x = (x + strike) * np.minimum(1, t / 0.002)
    x *= np.minimum(1, (dur - t) / 0.08)
    return x / np.abs(x).max() * 0.55


if __name__ == "__main__":
    write("sfx_sling", ay(SLING))
    write("sfx_bell", bell())
    print("sfx_sling.wav and sfx_bell.wav in", OUT)
