# D0 — arcade recorded-replay gate

Proves arcade KH-1..KH-5 × KID/CADET/PAD_RAT fly identically across `mission.js` refactors (D1–D10).

- `npm run test:replay` — replays the 15 golden traces in `tests/replay/traces/` (~50 s). CI runs it through `npm test` via `tests/arcade-replay-ci.test.cjs` (active only when `CI=true`).
- `npm run replay:record [KH-1_KID ...]` — re-record. Only for an **intended** behaviour change; commit the trace diff with it. Move-only refactors must never need this.

How it is deterministic with zero changes to game code (`page-hooks.js`, injected before any page script):
seeded `Math.random` (mulberry32, reseeded plus `Phaser.Math.RND.sow` at mission start), and a virtual clock for
`performance.now`/`Date.now`/`requestAnimationFrame` advanced 1/60 s per `__replay.step()`. Page `setTimeout`s (HUD banner timers) also run on
the virtual clock; the viewport is pinned to 430×932 and focus is emulated (a blur would clear held keys). Each case gets a fresh page and
empty storage. Rendering is skipped (it cannot change state); set `REPLAY_RENDER=1` to keep it.

Recorded per case: key input trace (frame, key, down/up), phase sequence (status/sepPhase/jacklynPhase with frame), every
flight-bus event with its game time (beat times), outcome/score/medal/recovered/fired beats, and a state snapshot + FNV
checksum every 30 frames (status, stage, rocket x/y/angle/vx/vy, score, INT, fuel, tClock, combo, altitude, nowSec).

Needs Chrome/Chromium (`CHROME_BIN` to override) and Node ≥ 20.10 (`--experimental-websocket`). No npm dependency.
