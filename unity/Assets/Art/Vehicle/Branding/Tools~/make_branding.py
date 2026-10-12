#!/usr/bin/env python3
"""Generates the 12 original glyphs, 6 frames, palette.json and a preview board for the Kestrel Heavy branding
editor (A2). All shapes are simple geometric primitives drawn for this game; no letterforms, no logos, no tracing.
Run from this folder: python3 make_branding.py  (needs cairosvg + Pillow only for the preview PNG)."""
import json, math, os
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.dirname(HERE)

def poly(pts): return "M" + " L".join(f"{x:.2f} {y:.2f}" for x, y in pts) + " Z"
def circle(cx, cy, r): return f"M{cx-r:.2f} {cy:.2f} A{r} {r} 0 1 0 {cx+r:.2f} {cy:.2f} A{r} {r} 0 1 0 {cx-r:.2f} {cy:.2f} Z"
def star(cx, cy, ro, ri, n, rot=0):
    return poly([(cx + (ro if k % 2 == 0 else ri) * math.sin(rot + k * math.pi / n), cy - (ro if k % 2 == 0 else ri) * math.cos(rot + k * math.pi / n)) for k in range(2 * n)])
def ring_path(cx, cy, ro, ri): return circle(cx, cy, ro) + " " + circle(cx, cy, ri)
def ngon(cx, cy, r, n, rot=0): return poly([(cx + r * math.sin(rot + 2 * math.pi * k / n), cy - r * math.cos(rot + 2 * math.pi * k / n)) for k in range(n)])

GLYPHS = {
    "glyph_chevron_up": poly([(14, 40), (32, 20), (50, 40), (42, 40), (32, 29), (22, 40)]),
    "glyph_star_four": star(32, 32, 20, 6, 4),
    "glyph_comet": circle(42, 22, 7) + " " + poly([(37, 27), (14, 50), (20, 44), (16, 52), (39, 30)]),
    "glyph_peak": poly([(10, 46), (26, 22), (32, 30), (38, 20), (54, 46)]),
    "glyph_wave": poly([(10, 34), (18, 26), (26, 34), (34, 26), (42, 34), (50, 26), (54, 30), (54, 36), (50, 32), (42, 40), (34, 32), (26, 40), (18, 32), (10, 40)]),
    "glyph_spark": star(32, 32, 20, 4, 6, 0.26),
    "glyph_arrow_split": poly([(32, 12), (44, 30), (36, 30), (36, 52), (28, 52), (28, 30), (20, 30)]) + " " + poly([(30, 36), (34, 36), (34, 50), (30, 50)]),
    "glyph_hex_cell": ngon(32, 32, 18, 6) + " " + ngon(32, 32, 9, 6),
    "glyph_kestrel_wing": poly([(10, 38), (30, 30), (54, 18), (44, 30), (52, 30), (36, 38), (44, 40), (24, 42)]),
    "glyph_planet_band": circle(32, 32, 13) + " " + poly([(10, 38), (54, 22), (55, 25), (11, 41)]),
    "glyph_triple_bar": poly([(14, 18), (50, 18), (50, 24), (14, 24)]) + " " + poly([(18, 29), (46, 29), (46, 35), (18, 35)]) + " " + poly([(22, 40), (42, 40), (42, 46), (22, 46)]),
    "glyph_orbit_dot": circle(32, 32, 6) + " " + circle(48, 22, 3.5),
}
FRAMES = {
    "frame_none": "",
    "frame_roundel": ring_path(32, 32, 30, 26),
    "frame_shield": poly([(8, 6), (56, 6), (56, 30), (32, 58), (8, 30)]) + " " + poly([(12, 10), (12, 29), (32, 53), (52, 29), (52, 10)]),
    "frame_hex": ngon(32, 32, 30, 6, math.pi / 6) + " " + ngon(32, 32, 26, 6, math.pi / 6),
    "frame_diamond": poly([(32, 2), (62, 32), (32, 62), (2, 32)]) + " " + poly([(32, 7), (7, 32), (32, 57), (57, 32)]),
    "frame_banner": poly([(2, 20), (62, 20), (56, 32), (62, 44), (2, 44), (8, 32)]) + " " + poly([(10, 24), (6, 32), (10, 40), (54, 40), (50, 32), (54, 24)]),
}
# 16-colour palette: generic, high-contrast set; index order is the save format (do not reorder).
PALETTE = ["#E8ECEF", "#1E3A5F", "#14161A", "#F2C21A", "#C83C32", "#D27A3A", "#2B7A5A", "#50DCF0",
           "#5A5E66", "#9EA3A8", "#7A5AA0", "#B58A5A", "#3A6EA5", "#8FBF4A", "#E07AA0", "#F5E6C8"]

def svg(d):
    body = f'<path d="{d}" fill="currentColor" fill-rule="evenodd"/>' if d else ""
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" width="64" height="64">{body}</svg>\n'

def main():
    for k, d in GLYPHS.items(): open(os.path.join(OUT, "Glyphs", k + ".svg"), "w").write(svg(d))
    for k, d in FRAMES.items(): open(os.path.join(OUT, "Frames", k + ".svg"), "w").write(svg(d))
    json.dump({"schema": "palette_v1", "colours": PALETTE}, open(os.path.join(OUT, "palette.json"), "w"), indent=2)
    open(os.path.join(OUT, "palette.json"), "a").write("\n")
    try:
        import cairosvg, io
        from PIL import Image, ImageDraw
        W, cell = 8, 150; im = Image.new("RGB", (W * cell, 4 * cell + 60), (14, 26, 46)); dr = ImageDraw.Draw(im)
        items = list(GLYPHS.items()) + list(FRAMES.items())
        for i, (k, d) in enumerate(items):
            x, y = (i % W) * cell, (i // W) * cell
            png = cairosvg.svg2png(bytestring=svg(d).replace("currentColor", "#E8ECEF").encode(), output_width=110, output_height=110)
            im.paste(Image.open(io.BytesIO(png)).convert("RGBA"), (x + 20, y + 8), Image.open(io.BytesIO(png)).convert("RGBA"))
            dr.text((x + 6, y + 126), k, fill=(80, 220, 240))
        for i, c in enumerate(PALETTE):
            dr.rectangle([10 + i * 74, 3 * cell + 20, 74 + i * 74, 3 * cell + 84], fill=c, outline=(80, 220, 240))
        im.save(os.path.join(HERE, "preview.png"))
    except ImportError:
        pass

if __name__ == "__main__": main()
