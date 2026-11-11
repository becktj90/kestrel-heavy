# Flight feedback plan

Wiring contract for Kestrel Heavy narration (gen5 + gen6) and SFX staging. Written for Gameplay Dev, Narrator Voice, and Chief of Staff. Physics reviews numerical thresholds on this PR; do not invent new numbers here.

Sources: `/workspace/kh-gen5/triggers.md`, `coverage.md`, `plan.md`, `review-physics.tsv`; `/workspace/kh-gen6/triggers.md`; current Unity and WebGL narration on `origin/main` after #493 and #476 (`a30ee9b4`, later #494 pins 358 clips / 51 events). Look-ups below are file:line on that tree (the cited code did not change in #476).

This plan does **not** spend ElevenLabs credits. Glimmer's already-shipped commentary is #450 (historical, 80 clips) and #493 (technical, 80 clips). New gen5 color is ungated and covers sandbox flights where those mission-gated sets are silent.

WebGL must stay in parity with Unity for every new game-code hook.

## Short brief

**Gameplay Dev.** Wire the events and gates in this doc. Do not wire any staging clip marked `hold:true` until its real trigger exists (fuel-low 12% rule, or a rewrite that no longer claims a flown burn / ellipse / "C-three on target"). Do not hook narration to the guide's strap-on imbalance offer. `status.offcourse` needs a real corridor-deviation signal, not a timer. `haven-maxq` is not emitted in Unity today; wire it to the RecoverySim q peak before any "peak" claim. Emit a new event id `hold_start` when the countdown enters a hold (hook `ColorDirector.NoteHold` + a production `LaunchSequence.Hold()`; not the pause-menu Hold). Fire `abort-splash` only when an upper stage can keep flying; tip/splash are booster landing outcomes, not "the upper is done." Outcome clips are mission-gated (transfer = m7/m10/m15/m19; departure/energy = m9/m17/m18/m20). SFX: never wire `engine.roar.far.v3`; only `loop:true` loops; upgrade/replace overlaps swap the current sound.

**Narrator Voice.** Word caps stay 14 in-flight and 24 for menu/designer/idle/preview. Subtitles must fit or scroll at 335 px. Clips marked `hold:true` stay unwired. No line may say "escape" (every C3 target is bound). Affiliation rule is DECISIONS #101: Blue Origin, New Glenn, Jacklyn, and LC-36 may be named as sourced history; nothing may imply KH affiliation. Keep `cohost/status.speed.g6-1` off hops. On #498 (`235ad603`) the 13 re-recorded transfer and departure clips are off hold and gated with `requires`; fuel-low stays on hold. Do not reintroduce flown-burn / ellipse / "C-three on target" claims without the matching trigger. Physics wording notes in `review-physics.tsv` still apply to any unfixed takes.

**Chief of Staff.** Settings sketch (not built): `kh.audio.commentary` = Full / Calls only / Off. Calls only plays feedback, count, and outcome calls; drops color, longform, tip, menu, and idle. Codex read-aloud is user-initiated and plays under Full or Calls only. Off mutes everything. Readiness stays 50/62 until a later scored item moves. No new ElevenLabs spend from this plan. Codex guard (#506) checks affiliation, not a name ban; trademark/livery filter stays in place. Open P2 (bare "Saturn", "BlueOrigin" variant) is a follow-up after #506 merges.

## Playback rules (already shipping)

- One line at a time. Calls preempt color (`docs/narration-webcast.md`).
- A flight call more than 4 s late is dropped.
- Each event fires once per flight unless the table says otherwise.
- Color starts only if `durationSec + 1.5 s` fits before the next call (coast warp, #436).
- Duration caps in lint: call clips 6 s, color 14 s.
- Word caps: **14 words in-flight**; **up to 24** for menu / designer / idle / preview. Subtitles fit or scroll at 335 px.
- Never say "escape." Every C3 target in the campaign is below 0 (bound orbit).

## Naming and affiliation (DECISIONS #100, #101)

Trevor, 8:28 AM and 8:36 AM ET, 2026-11-11, in his own words on #101: "It is not related to blue origin and can name new Glenn and Jaqueline if that history is relevant" (Jacklyn).

- Sourced history may name real government programs, people, dates, and commonly known companies, including Blue Origin, New Glenn, Jacklyn, and LC-36.
- Nothing may imply that Kestrel Heavy is connected to, endorsed by, or a product of Blue Origin. The KH rocket, pad, Haven, and branding stay fictional.
- Lint is a no-affiliation check, not a name ban: a take that names Blue Origin / New Glenn / Jacklyn / LC-36 must have sources, must not name Kestrel / KH / Haven / Pier 7 in the same clip, and must not use affiliation wording (our, partner, official, sponsor, endorse).
- Codex read-aloud may use the same rule. The trademark/livery filter is unchanged.

## `hold:true` (hard stop)

Staging manifests mark some takes `hold:true`. **Never wire a `hold:true` clip** until the real trigger exists, or Narrator Voice has rewritten the line so the claim is no longer premature.

Today that covers:

1. Fuel-low and landing.fuel-low takes, until the 12% rule (below) is implemented.
2. Gen5 takes that claim a burn was flown, mention an ellipse, or say "C-three on target," until the matching outcome/burn signal exists. On #498 head `235ad603`, the 13 re-recorded transfer and departure clips are **off hold** and carry `requires` gates (transfer = m7/m10/m15/m19; departure/energy = m9/m17/m18/m20). **Fuel-low clips on #498 stay `hold:true`.** Do not treat a cleared flag as permission to wire a still-premature claim.

## Fuel-low (Physics, binding)

There is **no real fuel-low trigger on main today**. Do **not** wire narration to the guide's strap-on imbalance offer (stage 1 above 3 km with nearly full tanks). Imbalance is a separate event.

New rule, once implemented:

- Fire `fuel-low` once per stage when the burning stage's usable propellant is at or below **12% of loaded propellant** and engines are still burning.
- Suppress if that stage burns out within 3 s of crossing the threshold (MECO / SECO already covers it).

`landing.fuel-low` is a different signal: landing reserve below 25% of `landingPropMass` during the landing burn, and only when recovery is barge or rtls.

## Look-up: `hold_start` (new emitter)

There is **no live hold-start event on main** `a30ee9b4` (confirmed by grep this session). Adopt Narrator Voice's id **`hold_start`** as a **new event**, listed with the other new emitters below. Fire it when the countdown enters a hold. Do not reuse or confuse it with the pause-menu Hold or with `haven-burn-hold`.

| Location | What it is |
|---|---|
| `unity/Assets/Kestrel.Game/Audio/ColorWindows.cs:188` | `ColorDirector.NoteHold` — closest unwired hook. |
| `ColorWindows.cs:146` | Comment: "the live countdown has no hold today." |
| `unity/Assets/Kestrel.Game/Launch/LaunchSequence.cs:91` | `LaunchSequence.Hold()` — only called from tests (`:91–113`). |
| `web/js/narration/bus.js` | No `hold_start`, `count.hold`, or `hold` type in the EVENTS list (`:41–111`). |
| `unity/Assets/Kestrel.Game/App/SliceApp.cs:1651–1655` | Pause-menu Hold. **Not** a countdown hold. Do not hook narration here. |

**Wiring:** when the countdown enters a hold, emit `hold_start` from both Unity and WebGL. Hook it to `ColorDirector.NoteHold` plus a production call to `LaunchSequence.Hold()`. Timeline `held` / `scrub` / `recycle` already exist in `TimelineGenerator` (around `:990–1001`); the narration emitter is new. Gen5 recorded `count.hold` takes for the same moment — play those (and gen6 `hold_start` takes) from the `hold_start` event, or map `count.hold` onto it. Do not invent a second live id.

## Look-up: `abort-splash` and `abort-tip`

These do **not** mean the upper stage has stopped. They are **booster landing outcomes**. The upper may still fly.

| Location | What it is |
|---|---|
| `web/js/mission.js:1856–1883` | JS emits `tip` / `splash`, then always `finishUpper()` (`:1890–1914`). |
| `unity/Assets/Kestrel.Game/Recovery/RecoveryScore.cs:40–41` | `tipped = tilted \|\| !legsDown`. |
| `unity/Assets/Kestrel.Game/Recovery/LandingGrade.cs:55` | Tip grades as Salvage. |
| Narration sink | No production `OnEvent("abort-splash" \| "abort-tip" \| "splash" \| "tip")` on main. |

**Physics gate:** fire `abort-splash` only when an upper stage can keep flying. Do not use "the upper is still flying" as the meaning of the event; use it as the gate.

## Events to wire

Existing events that already match the sim stay as they are: `maxq`; `seco` (orbit insertion on designs with 2+ stages); ignition at T−3; clamp release at T−0.2. The pad call before internal power at T−0:30 is fixed in the text; no gate needed.

Keep `cohost/status.speed.g6-1` off hops.

### Status and safety (many are new)

| Event | Fire when | Source today | Limits |
|---|---|---|---|
| `status.ontrack` | WebcastBoard verdict first becomes "On track" after "Climbing" | `WebcastBoard.Pulse` (exists) | Once per flight; recoveries use the `.clear` events |
| `status.speed` | Verdict "Speed is there" | Pulse (exists) | Once. Keep the gen6 cohost take off hops |
| `status.short` | Verdict "Short on speed" (LiveSpare held < 0 for 3 s, DECISIONS #90) | Pulse + LiveSpare.Short (exists) | Once, then only after a clear |
| `status.short.clear` | Verdict goes from Short back to On track | Pulse (exists) | Once per short |
| `status.heating` | Verdict "Heating hard": q ≥ 0.85 of the design's predicted peak q (`sheet.maxQ_kPa` / `Readout.cs`), **not** the structural limit `qLim_kPa` | Pulse (exists) | Once, **only before the q peak**. Suppress if `maxq-throttle` played within 5 s. Do not say "close to the design limit" |
| `status.offcourse` | Flight-path / heading error from corridor centre exceeds a warn band (Physics to confirm; triggers.md suggested 60% of `abort-corridorEdge`, held 1 s) | **Needs a real deviation signal. Not a timer.** | Once, then only after a clear |
| `status.offcourse.clear` | Deviation falls back under the clear band (triggers.md suggested 40% of the limit) | Same new signal | Once per offcourse |
| `status.apogee` | OrbitCoast "Apogee!" on hops | `OrbitCoast.CalloutApogee` (exists) | Once |
| `fuel-low` | 12% rule above | **Not on main. Do not use the imbalance offer.** | Once per stage; suppress if burnout is within 3 s |
| `engine-out` | An engine quits in flight (Thrum `asc.engineout`) | Guide hook exists for Thrum | Once per engine |
| `engine-out.ok` | Still climbing 3 s later | Same hook | Once. Requires `lit.engines >= 2` |
| `sep-clear`, `sep-wide` | Existing events; new takes | Exists | `sep-clear` and `meco` require stages ≥ 2 |

### Landing (booster recovery)

Height is above the **landing surface** (Haven deck or pad), not sea level. Drop a callout if one is still playing (1.5 s gap). Skip `landing.alt.10` if `sink.*` is due.

| Event | Fire when | Notes |
|---|---|---|
| `landing.alt.1k` / `500` / `100` / `10` | Booster height first drops below 1000 / 500 / 100 / 10 m during the landing burn | **New signal.** `landing.alt.1k` requires `recovery` barge or rtls |
| `landing.legs` | Legs deploy | Needs a leg-deploy event. Fact `legs: true` exists |
| `landing.sink.good` / `landing.sink.high` | Below 100 m, sink rate inside or outside `landingVy` | **New signal.** Once each. High is safety priority |
| `landing.drift` | Below 300 m, lateral offset above 50% of `landingTol` | **New signal.** Once |
| `landing.fuel-low` | Landing reserve < 25% of `landingPropMass` during the landing burn | **New signal.** Requires barge or rtls. `hold:true` until wired |

### Count and outcomes

| Event | Fire when | Notes |
|---|---|---|
| `count.poll` | `TimelineGenerator` `term.poll` all-go | MC poll first, host 1.5 s later. Hook is new |
| `count.t60`, `count.t30`, `count.t15` | Count clock crosses those marks |  |
| `count.n10` … `count.n1` | Each second T−10 to T−1 | One clip per number, under 1 s; must end 0.5 s before T−0. Exempt `count.*` from the lint "seven" check |
| `hold_start` | Countdown enters a hold | **New emitter.** See the look-up above. Play gen5 `count.hold` takes from this event. `count.scrub` / `count.recycle` stay on the timeline scrub/recycle branches |
| `outcome.orbit` / `.orbit.high` / `.orbit.low` / `.offtarget` / `.missed` | Result-card orbit check | See Physics outcome emitter. Do not use a "ninety minutes" line on transfer orbits (m7/m10/m15/m19 period is ~10.5 h) |
| `outcome.hop` / `.short` / `.toohigh` | Hop end states | Do not hook a hop to a circular-orbit clip |
| `outcome.depart` / `.burnshort` | C3-target missions only (m9, m17, m18, m20) | Never say "escape" |

### Physics outcome emitter (binding)

- `outcome.burnshort`: parked at 200 km with spare Δv below the transfer or departure Δv.
- `outcome.offtarget`: same pass tolerance as the verdict.
- GTO / C3 / C3-high wins: parked plus Δv left, with **no burn flown**.
- Gen5 **transfer** lines require missions **m7 / m10 / m15 / m19**.
- Gen5 **departure and energy** lines require **m9 / m17 / m18 / m20**. On #498 (`235ad603`) the 13 re-recorded transfer and departure clips are off hold and carry those `requires` gates; fuel-low clips on that PR stay on hold.

Do not hook an m18/m20 departure to a "stable orbit" or "it will come back" line. m20 apogee (~3.2 million km) is outside Earth's Hill sphere (~1.5 million km); no line may claim the real path comes back.

### Color, heating, and haven-maxq (Physics notes)

- `color.ascent` opens only **after** max-Q (`ColorWindows.AscentOk`). Mach 1 has already happened, so "about now" for the sound barrier is wrong. Neutral ascent lines that assume an orbital profile must not play on hops (m1, m2 climb nearly straight up). Gate or `requires` as needed.
- `color.coast` is neutral and can play on hop and transfer coasts, where the craft is not at 7.8 km/s. Low-orbit speed/period lines need a low-orbit gate.
- `color.recovery.g5-4` requires barge and a coastal site.
- `haven-maxq`: **no Unity code emits this today.** Only the web/iOS JS demo fires it on a timer (1.5 s after strakes), not at real descent peak q. Wire it to the RecoverySim q peak before any "peak" claim. `entry` already fires only when recovery is barge or rtls (`CallTriggers`).
- `fairing` is already heat-gated (half rho v cubed under maxHeatFlux for 2 s, or jettison height after max-Q when the payload sets no limit).

## Menus, designer, idle, Codex (new commentary manifest)

These are not flight events. The narration lint's "neutral lines state no design fact" rule would fail many of them, so they need `assets/commentary/manifest.json` plus a Unity copy, with its own lint (two copies byte-equal, loudness bar, no-affiliation check, sources on history lines, no "FAIL").

Layers: `longform` (two-voice, 15–45 s, interruptible, ducked under UI SFX), `tip` (≤ 8 s), `menu`, `codex` (read-aloud, 10–60 s; kid register by Chris, engineer by Sarah).

If Thrum's designer card is showing, hold designer commentary until it closes. Never play both.

### Gen5 events

`menu.home.welcome` (once per session, rotating); `menu.home.tip` (idle 20 s after welcome); `menu.home.idle` (idle ≥ 45 s, then wait 90 s); `menu.missions`; `menu.mission.mN` (card focused ≥ 1.5 s, requires that mission); first-open intros for Codex, medals, settings; `designer.<component>` and `.tip` as in `kh-gen5/triggers.md` section G; `idle.pad` (no input ≥ 25 s before the count, or a planned hold with more than 45 s left; never within 10 s of T−12); `codex.<entry>.kid` / `.eng`.

### Gen6 events (add to the same menu/Codex manifest)

| Event | Suggested trigger |
|---|---|
| `menu.mission.history.mN` | Mission-select / preview, after or instead of a repeat of `menu.mission.mN` |
| `idle.mission.mN` | Pre-count pad idle, mission already selected |
| `brief.mission.mN` | Pre-flight brief or first pad hold; physics/context, not a history repeat |
| `brief.haven` | Pre-flight when `recovery=barge`. Haven is fictional; no real-ship names |
| `explain.scrub` / `explain.abort` / `explain.failure` / `explain.staging-risk` | After `count.scrub`, a generic abort, `health-lethal` / structural loss, or `abort-sepFoul` / `abort-recontact` (or the debrief). Long-form, 24-word parts |
| `designer.guidance` and the other new designer topics in `kh-gen6/triggers.md` (ullage, restart, interstage, pressurization, boiloff, cycles, nozzles, cooling, turbopumps, pogo, slosh, reentry, landingburn, rtls, deluge, holddown, power, residuals, massratio, weather, telemetry) | Same pattern as gen5 `designer.engines`, each with a `.tip` |
| `hold_start` | Same new emitter as above: countdown enters a hold |

When new events join the Unity narration manifest, add them to `tests/narration-facts-lint.test.cjs` (`REC_EVENTS` and friends) the same way landing-alt events were requested. Add coverage floors to `eachCase()` only when the event is actually wired.

## SFX wiring

Staging lives under `audio/staging/` and must not change `unity/Assets` until Sound/Arcade explicitly land a wiring PR. Binding rules:

- **Never wire `engine.roar.far.v3`.** Its edges fade. Use **v1 or v2**.
- UI and countdown beeps are peak-normalized to **−3 dBFS** and rely on `SfxCatalog`'s lower UI volume. Do not raise them to the voice loudness bar.
- **Only** manifest items marked `loop:true` loop.
- Manifest overlaps marked `upgrade` or `replace` swap out the existing NASA-trim / alias sounds; they are not extra layers.
- Engine roar loops are per propellant, keyed by `stage{N}.propellant`. Cluster scaling stays the procedural layer from the SFX brief.
- `recovered` is a trim of Apollo 11 "Eagle has landed." If that real-voice clip stays, `sting.success` is optional.
- No real broadcast audio in the new beds. Add attribution rows in `web/audio/ATTRIBUTION.md` and `ASSETS_LICENSES.md` when they land.

## Lint and pins

- Landing events belong in `REC_EVENTS` so the "pad" word rule covers them.
- Count pins in `NarrationScheduleTests`, `NarrationTests`, and `GuideCoreTests` move with every batch that lands in the shipping tree. After #494: **358 clips, 51 events** (`maxq-throttle` is the 51st event).
- Staging batches under `audio/staging/` do not move those pins until they are wired into `assets/narration` / Unity Resources.

## Physics review

Physics already reviewed gen5 copy in `review-physics.tsv`. Thresholds named in this plan (offcourse warn/clear bands, landing height and drift fractions, heating 0.85, fuel-low 12%, landing fuel 25%) should be confirmed or corrected on this PR. Do not ship a new number that Physics has not signed.

## Out of scope

- No new ElevenLabs spend.
- Do not upload App Store screenshots or change icon/splash/Phaser demo/`voice.js` from this work.
- Test Flight / DECISIONS #102 (#508) and G6 narrator staging (#510, #512) are not part of this wiring wave.
