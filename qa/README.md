# Kestrel Heavy QA bot harness

Parallel Playwright bots against `web/` (served locally, no network). Maps to STUDIO_ROADMAP §6
"Bot playtests" plus parts of G6 (HUD overlap across viewports/orientations) and G8 (no off-box requests).

    cd qa && npm i && npx playwright install chromium
    node run.mjs --seeds 2 --workers 8 --viewports phone,max,ipad,phone-land,max-land,ipad-land
    node report.mjs out/results.json      # pass-rate tables + findings
    node simfuzz.mjs 300                  # G3-style fuzz of the M1 headless sim (no browser)

- `bot.js` — injected autopilot. Seeds `Math.random` (mulberry32) for reproducible spawns; bot noise uses a
  separate RNG. Skills: `high` / `mid` / `low` (reaction tick, look-ahead, dodge miss rate, aim noise, burn timing).
  Game difficulty stays at default CADET.
- Checks: page errors / console errors / crashes; DOM HUD bounding-box overlaps + offscreen per phase;
  stuck state (no change for `--stuck` s) or `--timeout`; SUMMARY overlay shown and returns to MENU;
  offline Game Center runs (fake `webkit.messageHandlers.kestrelGameCenter`, `ok` and `throw`, context offline).
- Default renderer is Canvas (`--webgl` for swiftshader WebGL): headless WebGL with many browsers runs ~0.5x
  game speed. One browser per job — pages sharing a browser starve rAF and fake "stuck on pad".
- Repro one run: `node run.mjs --flights KH-1 --skills low --viewports phone --seeds 1 --gc 0 --workers 1`.
