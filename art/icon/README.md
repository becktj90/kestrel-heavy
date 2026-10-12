# Kestrel Heavy app icon (task A2)

Fictional world only: KH-n missions, the Kestrel Heavy booster, the recovery barge Haven. No real brands, logos, or liveries.

- `concepts/`: 6 concepts (SVG + 1024 PNG), defined in `scripts/concepts.py`
- `legibility-grid.png`: 29/40/58/60/80/87/120/180 px with the iOS squircle mask
- `previews-light-dark-tinted.png`: light, dark, and iOS 18-style monochrome tint previews
- `SIMILARITY.md` / `similarity.json`: pHash/dHash check against 111 iTunes Search API space-game icons
- `board.png`: contact sheet
- `final/`: top pick **c1-kestrel-booster**. `kestrel-heavy-icon.svg`, `AppIcon-1024.png` (RGB, no alpha), `layers/`

## Rebuild
```
pip install --user --break-system-packages cairosvg imagehash pillow
python3 art/icon/scripts/build.py        # KH_FINAL=<concept> to change the final
python3 art/icon/scripts/similarity.py   # needs network
python3 art/icon/scripts/sheets.py
python3 art/icon/scripts/validate.py     # exits 1 on failure
```

## Icon Composer import
In Icon Composer (Xcode 26+), create a new icon and drag in `final/layers/` in order. `0-background.svg` is the base fill, then `1-mid.svg` (halo + wings) and `2-foreground.svg` (booster + plume) as separate groups. Each layer is a full 1024 canvas, so they line up without offsets. Apply Liquid Glass/specular to the foreground group only, and leave the background flat. Use the PNG versions if SVG import gives you trouble. Do not add the result to the asset catalog until Trevor approves.
