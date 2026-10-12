# Flight event bus (`web/js/narration/bus.js`)

Shared, typed stream of what actually happened in a flight. Consumers: narration director (captions now, voice later) and the upcoming SFX system. Subscribers are output-only.

Narration lines carry `{ channel, variants, priority, neverDuck, tags }`; `lineFromTemplate()` maps style-guide P0–P3 templates (P0 = critical + neverDuck).

Event shape: `{ type, cat, t, seq, payload }`. `t` = game seconds (mission `nowSec`), `seq` = global emit order. Unknown types are rejected (`strict`) or dropped and counted.

Publishers today: `mission.js` (callout beats, aborts, flight begin/end) and `voice.js` (`radio.line` mirror). Arcade numbers are cosmetic, so nothing is spoken numerically until the M3 sim publishes the same ids with `source: "sim"`.

| type | category | payload |
|---|---|---|
| `flight.begin` | flight | missionId, payload, qa? |
| `flight.status` | flight | status (MENU/PRELAUNCH/ASCENT/SEP/JACKLYN/SUMMARY), prev |
| `flight.end` | flight | reason (rud/done/...), score |
| `pad` | count | missionId |
| `terminal` | count | tClock |
| `tankpress` | count | tClock |
| `internal` | count | tClock |
| `deluge` | count | tClock |
| `ignition` | count | tClock |
| `liftoff` | ascent | tClock |
| `maxq` | ascent | tClock (arcade: scripted beat, not measured q) |
| `meco` | ascent | tClock |
| `fuel-low` | ascent | fuel |
| `sep` | staging | tClock (sep zone opens) |
| `align-green` | staging |  |
| `align-wide` | staging |  |
| `sep-clear` | staging |  |
| `sep-wide` | staging |  |
| `sep-contact` | staging |  |
| `ses1` | staging | tClock |
| `fairing` | staging | tClock |
| `seco` | staging | tClock |
| `deploy` | staging | payload |
| `entry` | recovery | tClock |
| `landing` | recovery | tClock |
| `touchdown` | recovery | grade? |
| `haven-slide` | recovery |  |
| `haven-reentry` | recovery |  |
| `haven-maxq` | recovery |  |
| `haven-strakes` | recovery |  |
| `haven-straighten` | recovery |  |
| `haven-burn` | recovery |  |
| `haven-burn-hold` | recovery |  |
| `haven-burn-nag` | recovery |  |
| `hint-ascent` | gameplay |  |
| `hint-corridor` | gameplay |  |
| `hint-pickup` | gameplay |  |
| `hint-sep` | gameplay |  |
| `hint-haven` | gameplay |  |
| `first-save` | gameplay |  |
| `press-ok` | gameplay |  |
| `corridor-soft` | gameplay |  |
| `corridor-edge` | gameplay |  |
| `pickup-shield` | gameplay |  |
| `pickup-fuel` | gameplay |  |
| `pickup-boost` | gameplay |  |
| `shield-hit` | gameplay |  |
| `close-call` | gameplay |  |
| `struct-warn` | gameplay |  |
| `health-graze` | gameplay |  |
| `health-scrape` | gameplay |  |
| `health-lethal` | gameplay |  |
| `abort` | anomaly | abortId (one of voice.js ABORTS), reason, text |
| `recovered` | outcome |  |
| `salvage` | outcome |  |
| `tip` | outcome |  |
| `splash` | outcome | abortId (splash/fuelHaven) |
| `radio.line` | radio | key, text (exact legacy radio text shown) |
| `econ.quote` | economy | designId, recurring, dev, perKg (integer minor units), currency (`Kc`). Emitted by the DESIGN cost panel when the price changes. |
| `econ.purchase` | economy | item, category, amount (integer minor units), currency (`Kc`). Reserved for E3; not emitted in 2.0. |

Subscribe: `import flightBus from "./narration/bus.js"; const off = flightBus.on("maxq", (ev) => ...); flightBus.on("*", ...)`.

Adding an event: add it to `EVENTS`, document payload, emit it from the publisher. Narration lines for it go in `narration/manifest.js`.

## Captions
Off by default. Enable with `?captions=1` or set localStorage `kestrelHeavy.captions.v1` to `{"enabled":true,"size":"m","bgOpacity":0.6,"highContrast":false,"hosts":true}`. A settings-screen toggle lands with N5.
