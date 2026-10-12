# Timeline audit (K1): countdown, events, narration, audio, telemetry, HUD

Refs are to `main` at K1 (Phaser web build; iOS copy under `ios/KestrelHeavy/Game/js/` mirrors it).

| Area | Today | Where | Gap vs Launch Timeline |
|------|-------|-------|------------------------|
| Countdown | Fixed 8 s compressed count (`T0_LEAD = 8`), hard-coded beats terminal → tankpress → internal → deluge → ignition | `web/js/sequence.js:13-30` | Same count for every rocket; no propellant-specific load, holds, polls, recycle or scrub |
| Ascent spine | Fixed MET table `PACE` (MAXQ 21.6 s … DEPLOY 104 s) | `web/js/config.js:81-92`, `sequence.js:16-175` | Max-Q, MECO, SEP are **scripted times**, not sim conditions; SES/fairing exist regardless of hardware |
| Beat firing | `fireDueBeats` loops `SPINE`, skip rules are ad-hoc `if`s per beat id | `web/js/mission.js:1529-1543`, `onBeat` `:1545-1560` | Dependencies are implicit code, not data; no explain-why |
| Phase chip | Clock thresholds + beat id | `sequence.js:233-263` | Duplicates timing logic |
| N1 narration bus | `createBus`, 59 typed events in 10 categories, `seq` counter, strict mode | `web/js/narration/bus.js:22-34`, `:41-112`, `:119-166` | Good base. Payloads are free-text strings; `maxq` payload notes "scripted beat, not measured q" (`bus.js:52`); no status, nominal/actual, tolerance |
| Bus emits | `flight.begin`, callouts, `abort`, `flight.end` | `mission.js:590`, `:746`, `:834`, `:2256` | Countdown beats emit via callout only |
| S1 audio events | Adapter onto the same bus | `web/js/audio/events.js:6-28`, connect `web/js/audio.js:72` | Good: already one bus. But 24 direct `AudioApi.play(...)` calls in `mission.js` bypass it |
| Economy | `econ.purchase` reserved on bus | `bus.js:110` | No launch-cost hooks per event |
| Telemetry | Arcade formula `ascentVelocity` (polynomial in t) | `web/js/telemetry.js:34-38`, `computeTelemetry` `:40` | Not from the sim; no event ↔ telemetry linkage |
| HUD | `setBanner(text, kind, holdMs)` transient banners; `formatClock` | `web/js/hud.js:25`, `sequence.js:197` | No persistent tracker, no next-event, no T± list, color-coded `kind` only |

**Verdict:** the N1 bus is the right spine. Replace `SPINE`/`PACE` + ad-hoc skips with generated `MissionEvent`s; route all SFX/music/narration/Codex/economy off the same events. Unity core (`unity/dotnet/Kestrel.Core`) has no event model yet.
