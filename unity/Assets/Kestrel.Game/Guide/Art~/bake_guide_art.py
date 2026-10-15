#!/usr/bin/env python3
"""Bake Kestrel Heavy's own guide art. No third-party sprites or fonts.

Writes:
  Assets/Resources/Guide/thrum.png       16x16 portrait of Mabel Thrum
  Assets/Resources/Guide/portrait.json   the same pixels, for the test lock
  Assets/Resources/Guide/atlas.png       5x7 glyph atlas (nearest-neighbour)
  Assets/Resources/Guide/glyphs.json     glyph metrics and bits, for the test lock
  /tmp/guide-preview.png                 scaled contact sheet (not shipped)

Original fictional character. Original letter shapes. Rerun from the repo root:

  python3 unity/Assets/Kestrel.Game/Guide/Art~/bake_guide_art.py
"""
import json
import os
import struct
import zlib

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", ".."))
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "Guide")

# Palette. Ink and cream match the subtitle box so the portrait sits on it.
PAL = {
    ".": (0, 0, 0, 0),
    "o": (0x1B, 0x1F, 0x2A, 255),  # ink outline
    "h": (0xF7, 0xF4, 0xEE, 255),  # white hair
    "H": (0xD5, 0xD0, 0xC6, 255),  # hair shade
    "s": (0xF3, 0xC9, 0xA4, 255),  # skin
    "S": (0xE0, 0xA8, 0x78, 255),  # skin shade
    "e": (0x1B, 0x1F, 0x2A, 255),  # eye
    "w": (0xFF, 0xFF, 0xFF, 255),  # eye light
    "b": (0xE0, 0x8A, 0x78, 255),  # cheek
    "m": (0x8A, 0x3A, 0x32, 255),  # mouth
    "r": (0xC2, 0x3B, 0x32, 255),  # shirt
    "R": (0x8D, 0x2A, 0x24, 255),  # shirt shade
    "c": (0xF2, 0xEF, 0xE6, 255),  # collar
    "g": (0xE8, 0xB1, 0x5A, 255),  # amber button
}

# 16x16. White hair, warm face, red shirt, collar button. Drawn for this game.
PORTRAIT = [
    "................",
    "..ohhhhhhhhho...",
    ".ohhHHHHhHHHho..",
    ".ohhssssssssho..",
    ".ohsewssswewho..",
    ".ohsssssssssso..",
    "..osSbbbbSso....",
    "..ossmmmmmso....",
    "..osssssssso....",
    "...occccccco....",
    "..orrrgrrrrrro..",
    ".orrrrrrrrrrrro.",
    ".orrRrrrrrrRrrro",
    ".orrrrrrrrrrrro.",
    "..orrrrrrrrrro..",
    "....oooooooo....",
]

# 5x7 glyphs. Each entry is 7 strings of 5 dots/hashes. Bit on the left is the
# left pixel. Space through tilde, then the gear used on engineer lines.
def G(*rows):
    assert len(rows) == 7
    for r in rows:
        assert len(r) == 5 and set(r) <= {".", "#"}
    return rows

GLYPHS = {
    " ": G(".....", ".....", ".....", ".....", ".....", ".....", "....."),
    "!": G("..#..", "..#..", "..#..", "..#..", ".....", "..#..", "....."),
    '"': G(".#.#.", ".#.#.", ".....", ".....", ".....", ".....", "....."),
    "#": G(".#.#.", "#####", ".#.#.", "#####", ".#.#.", ".....", "....."),
    "$": G("..#..", ".####", "#.#..", ".###.", "..#.#", "####.", "..#.."),
    "%": G("##...", "##..#", "...#.", "..#..", ".#...", "#..##", "...##"),
    "&": G(".##..", "#..#.", "#.#..", ".#...", "#.#.#", "#..#.", ".##.#"),
    "'": G("..#..", "..#..", ".....", ".....", ".....", ".....", "....."),
    "(": G("...#.", "..#..", ".#...", ".#...", ".#...", "..#..", "...#."),
    ")": G(".#...", "..#..", "...#.", "...#.", "...#.", "..#..", ".#..."),
    "*": G(".....", ".#.#.", ".###.", "#.#.#", ".###.", ".#.#.", "....."),
    "+": G(".....", "..#..", "..#..", "#####", "..#..", "..#..", "....."),
    ",": G(".....", ".....", ".....", ".....", "..#..", "..#..", ".#..."),
    "-": G(".....", ".....", ".....", "#####", ".....", ".....", "....."),
    ".": G(".....", ".....", ".....", ".....", ".....", "..#..", "....."),
    "/": G(".....", "...#.", "...#.", "..#..", ".#...", ".#...", "....."),
    "0": G(".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###."),
    "1": G("..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###."),
    "2": G(".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####"),
    "3": G(".###.", "#...#", "....#", "..##.", "....#", "#...#", ".###."),
    "4": G("...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#."),
    "5": G("#####", "#....", "####.", "....#", "....#", "#...#", ".###."),
    "6": G(".###.", "#....", "#....", "####.", "#...#", "#...#", ".###."),
    "7": G("#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..."),
    "8": G(".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###."),
    "9": G(".###.", "#...#", "#...#", ".####", "....#", "....#", ".###."),
    ":": G(".....", "..#..", "..#..", ".....", "..#..", "..#..", "....."),
    ";": G(".....", "..#..", "..#..", ".....", "..#..", "..#..", ".#..."),
    "<": G("...#.", "..#..", ".#...", "#....", ".#...", "..#..", "...#."),
    "=": G(".....", ".....", "#####", ".....", "#####", ".....", "....."),
    ">": G(".#...", "..#..", "...#.", "....#", "...#.", "..#..", ".#..."),
    "?": G(".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.."),
    "@": G(".###.", "#...#", "#.#.#", "#.#.#", "#.##.", "#....", ".###."),
    "A": G(".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"),
    "B": G("####.", "#...#", "#...#", "####.", "#...#", "#...#", "####."),
    "C": G(".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."),
    "D": G("####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####."),
    "E": G("#####", "#....", "#....", "####.", "#....", "#....", "#####"),
    "F": G("#####", "#....", "#....", "####.", "#....", "#....", "#...."),
    "G": G(".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###."),
    "H": G("#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"),
    "I": G(".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###."),
    "J": G("..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.."),
    "K": G("#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#"),
    "L": G("#....", "#....", "#....", "#....", "#....", "#....", "#####"),
    "M": G("#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#"),
    "N": G("#...#", "##..#", "#.#.#", "#.#.#", "#..##", "#...#", "#...#"),
    "O": G(".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."),
    "P": G("####.", "#...#", "#...#", "####.", "#....", "#....", "#...."),
    "Q": G(".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#"),
    "R": G("####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"),
    "S": G(".###.", "#...#", "#....", ".###.", "....#", "#...#", ".###."),
    "T": G("#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."),
    "U": G("#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."),
    "V": G("#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.."),
    "W": G("#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#"),
    "X": G("#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#"),
    "Y": G("#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.."),
    "Z": G("#####", "....#", "...#.", "..#..", ".#...", "#....", "#####"),
    "[": G(".###.", ".#...", ".#...", ".#...", ".#...", ".#...", ".###."),
    "\\": G(".....", ".#...", ".#...", "..#..", "...#.", "...#.", "....."),
    "]": G(".###.", "...#.", "...#.", "...#.", "...#.", "...#.", ".###."),
    "^": G("..#..", ".#.#.", "#...#", ".....", ".....", ".....", "....."),
    "_": G(".....", ".....", ".....", ".....", ".....", ".....", "#####"),
    "`": G(".#...", "..#..", ".....", ".....", ".....", ".....", "....."),
    "a": G(".....", ".....", ".###.", "....#", ".####", "#...#", ".####"),
    "b": G("#....", "#....", "####.", "#...#", "#...#", "#...#", "####."),
    "c": G(".....", ".....", ".###.", "#....", "#....", "#....", ".###."),
    "d": G("....#", "....#", ".####", "#...#", "#...#", "#...#", ".####"),
    "e": G(".....", ".....", ".###.", "#...#", "#####", "#....", ".###."),
    "f": G("..##.", ".#...", ".#...", "###..", ".#...", ".#...", ".#..."),
    "g": G(".....", ".....", ".####", "#...#", ".####", "....#", ".###."),
    "h": G("#....", "#....", "####.", "#...#", "#...#", "#...#", "#...#"),
    "i": G("..#..", ".....", "..#..", "..#..", "..#..", "..#..", ".###."),
    "j": G("...#.", ".....", "...#.", "...#.", "...#.", "#..#.", ".##.."),
    "k": G("#....", "#....", "#..#.", "#.#..", "##...", "#.#..", "#..#."),
    "l": G("..#..", "..#..", "..#..", "..#..", "..#..", "..#..", ".###."),
    "m": G(".....", ".....", "##.#.", "#.#.#", "#.#.#", "#...#", "#...#"),
    "n": G(".....", ".....", "####.", "#...#", "#...#", "#...#", "#...#"),
    "o": G(".....", ".....", ".###.", "#...#", "#...#", "#...#", ".###."),
    "p": G(".....", ".....", "####.", "#...#", "####.", "#....", "#...."),
    "q": G(".....", ".....", ".####", "#...#", ".####", "....#", "....#"),
    "r": G(".....", ".....", "#.##.", "##...", "#....", "#....", "#...."),
    "s": G(".....", ".....", ".####", "#....", ".###.", "....#", "####."),
    "t": G("..#..", "..#..", "#####", "..#..", "..#..", "..#..", "...##"),
    "u": G(".....", ".....", "#...#", "#...#", "#...#", "#..##", ".##.#"),
    "v": G(".....", ".....", "#...#", "#...#", "#...#", ".#.#.", "..#.."),
    "w": G(".....", ".....", "#...#", "#...#", "#.#.#", "#.#.#", ".#.#."),
    "x": G(".....", ".....", "#...#", ".#.#.", "..#..", ".#.#.", "#...#"),
    "y": G(".....", ".....", "#...#", "#...#", ".####", "....#", ".###."),
    "z": G(".....", ".....", "#####", "...#.", "..#..", ".#...", "#####"),
    "{": G("...#.", "..#..", "..#..", ".#...", "..#..", "..#..", "...#."),
    "|": G("..#..", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."),
    "}": G(".#...", "..#..", "..#..", "...#.", "..#..", "..#..", ".#..."),
    "~": G(".....", ".....", ".#.#.", "#.#..", ".....", ".....", "....."),
    "\u2699": G(".#.#.", "#.#.#", ".###.", "#.#.#", ".###.", "#.#.#", ".#.#."),
}


def png(w, h, rgba):
    assert len(rgba) == w * h * 4

    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    raw = bytearray()
    stride = w * 4
    for y in range(h):
        raw.append(0)
        raw += rgba[y * stride:(y + 1) * stride]
    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b"")


def portrait_rgba():
    assert len(PORTRAIT) == 16
    px = bytearray()
    for row in PORTRAIT:
        assert len(row) == 16
        for ch in row:
            px += bytes(PAL[ch])
    return bytes(px)


def atlas():
    order = [chr(i) for i in range(32, 127)] + ["\u2699"]
    assert len(order) == 96
    missing = [c for c in order if c not in GLYPHS]
    extra = [c for c in GLYPHS if c not in order]
    if missing or extra:
        raise SystemExit("glyph set mismatch missing=%r extra=%r" % (missing, extra))
    cols, cell_w, cell_h = 16, 6, 8
    w, h = cols * cell_w, (len(order) // cols) * cell_h
    px = bytearray(w * h * 4)
    glyphs = []
    for i, ch in enumerate(order):
        col, row = i % cols, i // cols
        bits = GLYPHS[ch]
        glyphs.append({"ch": ch, "rows": [r.replace(".", "0").replace("#", "1") for r in bits]})
        for gy, bits_row in enumerate(bits):
            for gx, bit in enumerate(bits_row):
                if bit != "#":
                    continue
                x = col * cell_w + gx
                y = row * cell_h + gy
                o = (y * w + x) * 4
                px[o:o + 4] = b"\xff\xff\xff\xff"
    meta = {
        "cellW": cell_w,
        "cellH": cell_h,
        "glyphW": 5,
        "glyphH": 7,
        "cols": cols,
        "atlasW": w,
        "atlasH": h,
        "glyphs": glyphs,
    }
    return w, h, bytes(px), meta


def scale(w, h, rgba, n):
    out = bytearray(w * n * h * n * 4)
    ow = w * n
    for y in range(h * n):
        for x in range(w * n):
            s = ((y // n) * w + (x // n)) * 4
            d = (y * ow + x) * 4
            out[d:d + 4] = rgba[s:s + 4]
    return ow, h * n, bytes(out)


def blit(dst, dw, rgba, w, h, ox, oy):
    for y in range(h):
        for x in range(w):
            s = (y * w + x) * 4
            if rgba[s + 3] == 0:
                continue
            d = ((oy + y) * dw + (ox + x)) * 4
            dst[d:d + 4] = rgba[s:s + 4]


def preview(portrait, atlas_px, aw, ah, meta):
    # Cream ink panel with the portrait and a few lines of the atlas font.
    pw, ph = 16 * 8, 16 * 8
    _, _, big_p = scale(16, 16, portrait, 8)
    scale_g = 4
    gw, gh = 5 * scale_g, 7 * scale_g
    adv = 6 * scale_g
    lines = [
        "MABEL THRUM",
        "Oh! A visitor!",
        "Mind the oil.",
        "0123456789",
        "Max-Q in five.",
    ]
    W, H = 420, 40 + ph + 16 + len(lines) * (9 * scale_g)
    img = bytearray(W * H * 4)
    for i in range(0, len(img), 4):
        img[i:i + 4] = bytes((0x1B, 0x1F, 0x2A, 255))
    blit(img, W, big_p, pw, ph, 16, 16)
    index = {g["ch"]: i for i, g in enumerate(meta["glyphs"])}
    y0 = 16 + ph + 12
    for li, line in enumerate(lines):
        x = 16
        y = y0 + li * (9 * scale_g)
        for ch in line:
            gi = index.get(ch)
            if gi is None:
                x += adv
                continue
            col, row = gi % 16, gi // 16
            cell = bytearray(5 * 7 * 4)
            for gy in range(7):
                for gx in range(5):
                    sx = col * 6 + gx
                    sy = row * 8 + gy
                    s = (sy * aw + sx) * 4
                    d = (gy * 5 + gx) * 4
                    if atlas_px[s + 3]:
                        cell[d:d + 4] = bytes((0xFA, 0xF0, 0xD7, 255))
            _, _, big = scale(5, 7, bytes(cell), scale_g)
            blit(img, W, big, gw, gh, x, y)
            x += adv
    return png(W, H, bytes(img))


def main():
    os.makedirs(OUT, exist_ok=True)
    p = portrait_rgba()
    aw, ah, ax, meta = atlas()
    with open(os.path.join(OUT, "thrum.png"), "wb") as f:
        f.write(png(16, 16, p))
    with open(os.path.join(OUT, "atlas.png"), "wb") as f:
        f.write(png(aw, ah, ax))
    with open(os.path.join(OUT, "portrait.json"), "w", encoding="utf-8") as f:
        json.dump({"w": 16, "h": 16, "rows": PORTRAIT, "palette": {k: "#%02X%02X%02X%02X" % v for k, v in PAL.items()}}, f, indent=1)
        f.write("\n")
    with open(os.path.join(OUT, "glyphs.json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, ensure_ascii=False, indent=1)
        f.write("\n")
    with open("/tmp/guide-preview.png", "wb") as f:
        f.write(preview(p, ax, aw, ah, meta))
    print("wrote", OUT)
    print("atlas", aw, ah, "glyphs", len(meta["glyphs"]))


if __name__ == "__main__":
    main()
