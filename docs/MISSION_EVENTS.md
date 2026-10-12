# Mission events and the Launch Timeline (K1 spec)

Every rocket the player builds gets its own mission event sequence, generated in the shared C# core, shown in the designer and live in flight. **One schema.** Narration, SFX, music, telemetry, pad, economy and Codex all subscribe to `MissionEvent`; no parallel event types (the N1 bus types become `MissionEvent.id`s).

## 1. Schema: `MissionEvent`
| Field | Type | Notes |
|-------|------|-------|
| `id` | string | Stable, e.g. `count.lox.s1.load`, `asc.maxq`, `stg.sep.1` |
| `name`, `shortLabel` | loc key | Full name + ≤12-char label |
| `phase` | enum | `pad`, `load`, `terminal`, `ignition`, `ascent`, `staging`, `orbit`, `deploy`, `transfer`, `disposal`, `recovery`, `post` |
| `trigger` | union | `tRel {t, from: padLoadData}` · `simCondition {expr, e.g. q==max}` · `after {eventId, delay}` · `action {player\|autopilot, id}` |
| `preconditions` / `blocks` | eventId[] | Must be done / is blocked by |
| `holdable` | bool | Count can hold here |
| `criticality` | enum | `info`, `nominal`, `critical`, `safety` |
| `abortRules` | rule[] | Condition → branch event id (`abort.*`, `scrub`, `recycle`) |
| `nominal`, `actual`, `tolerance`, `variance` | seconds / units | Nominal from generator; actual from sim; variance seeded per difficulty |
| `status` | enum | `pending`, `inProgress{progress 0–1}`, `done`, `skipped`, `held`, `delayed`, `anomaly`, `failed` |
| `links` | object | `narration` cue ids, `sfx` ids, `music` cue/stinger, `telemetry` channels, `pad` hardware ids, `codex` entry ids |
| `visibility` | enum | `kid`, `standard`, `engineer` (tier shown in tracker) |
| `economy` | hooks | cost/refund on done/scrub (fictional Kc) |
| `strings` | loc keys | name, short, explain-why, anomaly text |

Event status changes are emitted on the existing bus as `event.status {id, status, actual, progress}`.

## 2. Event library
**Load sequences by propellant** (from catalogs `propellants.json`):
- Kerolox: go for prop load → fuel + LOX load per stage → LOX topping → engine chill → tank press.
- Hydrolox: + LH2 chilldown, slow/fast fill, **leak checks** (hydrogen sniff), **topping/replenish**.
- Methalox: **chilldown** of both sides, CH4 + LOX load, topping.
- Hypergolics: loaded **off-clock** (pre-count, safety-gated, pad clear), shown as done-before-count.
- Solids: **arming** (safe/arm devices), igniter checks; no load.

**Pad (from the generated pad, G2/G3):** crew/access **arms** retract, **strongback** retract (only if the pad has one), umbilical drop, deluge, internal power. Hardware-existence check: no event for hardware that isn't there.

**Terminal:** go/no-go poll (holdable built-in hold), terminal count, auto-sequencer, **staggered ignition** (per engine count), thrust-OK, **release**, liftoff.

**Ascent:** Max-Q (**sim condition**: peak q), throttle bucket (only if throttleable), booster sep (if boosters), MECO, stage sep, **SES-n / SECO-n only if the engine `restart` is true** (and within `restarts`), fairing sep (if fairing; condition: low heating).

**Orbit/beyond:** payload deploy, transfer burn(s), disposal/deorbit burn.

**Recovery per mode:** expendable (none), droneship (boostback? no; entry + landing burn), RTLS (boostback + entry + landing), parachute fairing/stage.

**Branches:** abort/anomaly per phase, holds (built-in + unplanned), **recycle** (back to a T- point), **scrub** (with economy refund hooks).

## 3. Generator (shared core, `Kestrel.Core.Timeline`)
- **Pure + deterministic**: `Generate(vehicle, pad, mission, seed) → Timeline`. No Unity types, no clock, no IO. Same input ⇒ byte-identical JSON.
- **Checks:** causality (topological order, no cycles, preconditions precede), hardware existence (every event references hardware present), staging consistency (SES-2 needs restart).
- **Explain-why**: each event carries the rule that produced it ("SES-2 because upper engine is restartable and mission needs a transfer burn").
- **Tests:** golden timelines for every template (JSON under `tests/vectors/timeline/`), property tests (random valid vehicles ⇒ checks pass, timeline monotonic, deterministic), perf: **< 2 ms** typical, **< 5 ms** worst on the floor device.
- **Calibration** (public timelines, ordering and relative spacing only; all names, wording and vehicles fictional):
  - Falcon 9 countdown/ascent tables (go for prop load T-38:00, LOX/fuel T-35:00, upper LOX T-16:00, engine chill T-7:00, tank press T-1:00, go for launch T-0:45, ignition T-0:03; Max-Q ~T+1:07–1:12, MECO ~2:25, SES-1 ~2:36): https://www.spacex.com/launches/ussf259, https://www.spacex.com/launches/kf-03, https://www.spacex.com/launches/gpsiii9
  - Atlas V built-in holds at T-2 h and T-4 min with readiness polls; hydrolox topping; SRB arming before ignition: https://www.ulalaunch.com/missions/archived-launched/atlas-v-aehf-4, https://blog.ulalaunch.com/blog/cft-a-preview-of-launch-day-activities
  - Game time compresses real T- durations by a per-mode factor (KID ~20–40 s total count; Engineer full or warpable).

## 4. Designer preview
- Timeline strip under the designer readouts; regenerates on every edit (fast enough to be live).
- **Diff highlighting**: added/removed/moved events since the last edit (e.g. "+ SES-2", "− fairing sep").
- **Pad loading view**: per-stage propellant load bars and chill/topping lanes over T-.
- Tap any event → explain-why + Codex link.

## 5. Live flight UI (portrait)
- **Bottom-sheet tracker** (peek / half / full): rows with status **by shape** (○ pending, ◐ in progress with arc, ● done, ⊘ skipped, ‖ held, ◷ delayed, △ anomaly, ✕ failed) plus text label; **tabular** monospaced T± column; T+ shows **`--:--:--` until the event happens**; Δ vs nominal (+0.4 s) in Standard/Engineer.
- **Next-event ribbon** at top: name + countdown to it.
- **Progress rail**: phase segments along the edge, current marker.
- **Lower-thirds** synced with narration cue start (same event id).
- **Parallel lanes** (booster recovery vs upper stage) as tabs/columns in the full sheet.
- **Holds and anomalies**: count freezes visibly, reason shown, recycle target shown.
- **Warp**: tracker compresses; events passed in warp still marked done with actual times.
- **Replay scrub**: scrub the timeline after flight; events re-render from recorded actuals.
- **Achievements**: event-based (first hydrolox count, perfect hold-free count, first SES-2).
- **Accessibility**: VoiceOver reads "Max-Q, done, T plus 1 minute 9, 2 seconds late"; Dynamic Type; shapes not color; reduced motion; all strings localizable, numbers locale-formatted.

## 6. K1–K7
| K | Scope | Lands in | Owner |
|---|-------|----------|-------|
| K1 | Audit + this spec (docs) | done | Kestrel Arcade |
| K2 | `MissionEvent` schema + bus adapter; N1/S1 events mapped onto it | U1 (core), U7 (subscribers) | Gameplay Dev |
| K3 | Generator + library + checks + explain-why + golden/property/perf tests | U1→U3 | Gameplay Dev, Physics |
| K4 | Designer preview, diff, pad loading view | U3 (+U6 pad hardware) | Gameplay Dev, UX |
| K5 | Live tracker, ribbon, rail, lanes, holds/anomalies, warp | U4 (+U5 anomalies, U6 holds/recycle/scrub) | Graphics Dev, UX |
| K6 | Narration lower-thirds, SFX/music, Codex, economy hooks off events | U7, U8 | Sound, Narrator Voice, Codex |
| K7 | Replay scrub, achievements, a11y + localization pass, calibration review | U5 (replay), U9 | QA, Physics, UX |
