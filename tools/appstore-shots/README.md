# App Store screenshot renderer

Regenerates the Kestrel Heavy store set in `docs/appstore/screenshots-2026-11-09/store/`:
iPhone 6.3" (1206×2622) and iPad 13" (2064×2752), 8 panels each, opaque RGB PNGs.

```bash
python3 tools/appstore-shots/render.py          # needs Pillow + numpy
```

- **Sources:** real, unedited in-game captures in `docs/appstore/screenshots-2026-11-09/<size>/<scene>.png`, from the
  **App Store screenshots** workflow (`AppStoreScreenshotTests`, artifact `appstore-screenshots`). Copy a new run's PNGs
  there (drop the `-<size>` suffix) and re-render.
- **Layout:** each size is drawn as one panorama (sky-to-space gradient, blueprint grid, stars, the flight trajectory with
  event nodes), then sliced, so adjacent panels join seamlessly. Panel 1–2 share a tilted hero device across the seam.
- **Copy and order:** `config.json` (titles ≤ 5 words; subtitles must fit one line or the script stops).
- **Fonts:** Fraunces Black + Space Grotesk from `unity/Assets/Art/Fonts/Resources/KHFonts`.
- Contact sheets: `store/contact-iphone.png`, `store/contact-ipad.png`.
