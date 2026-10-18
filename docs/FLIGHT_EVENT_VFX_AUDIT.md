# Flight event VFX audit

Read-only code audit of `origin/main` at feb6d22 (after #271). Nothing was run in Unity.
Owner column names the file that would change; "mine" means Pad/Kit/Environment (graphics), "Slice" and "Failure" files belong to others.

## What exists, in one paragraph
- **Sim events** (Core/Physics Headless.cs + the ascent sim): `ignition`, `liftoff`, `burnout`, `cutoff`, `separation`, `failure`, `impact`, `orbit`. There is no `maxq`, `fairing` or recovery-burn event. Max-Q is *inferred* in `FlightPresenter` (q peaked >= 15 kPa then fell below 98% of peak). Fairing jettison is a sim altitude condition inside `FlightStaging`.
- **Recovery** has its own phases (`RecoveryPhase`: Boostback, Coast, Entry, Descent, Landing, Down) sampled in `RecoveryShow`.
- **Feel layer** (`Core/Feel`): `FlightPresenter` turns cues `liftoff`, `stage`, `maxq`, `breakup` into camera shake, FOV punch, a `Flash` scalar, haptics and a 2D `DebrisField` (28 shards, no ground collision).
- **Slice show classes**: `FlightJuice` (applies presenter; one point light "PlumeGlow"), `FlightStageShow` (separation puff, ignition flash burst, tumbling spent stage, fairing halves), `FlightFailureShow` + `Game/Failure/FailureHost/FailureFx` (failure picture, bell flash, one pooled burst system), `RecoveryShow` (landing plume, dust/splash burst, touchdown break shards).
- **Comfort**: `Comfort.ReduceMotion` = reduce-motion OR reduce-flashing pref. Presenter/failure plans zero shake and flash when set.
- **Slow-mo**: none anywhere (`Time.timeScale` is only read, by Guide). Failure tail uses `unscaledDeltaTime` for its own clock.
- **Shockwave / heat distortion**: none. No refraction, no screen-space pass.
- **Time of day**: none of these effects read `PadLighting.NightFactor` / `TimeOfDayPreset`. Only `PlumeSplash`/`PadLights` (mine) react to the plume.

## Per event

| Event | Raised in | What exists | Missing | Timing today | Hook name / owner file |
|---|---|---|---|---|---|
| Ignition | sim `ignition` S1 at t=0 | Presenter cue only at "Running" (liftoff cue). Plume ramps with thrust. | Pre-liftoff ignition flash, pad dust/steam blast, camera rumble ramp, light on pad. | none distinct | `OnIgnition` — FlightDriver.cs (Slice) |
| Liftoff | sim `liftoff` (alt>1 m); presenter keys off `Running` | Cue: shake .55, flash .7, punch .8, haptic 28 ms; point light flash; plume glow. | Ground dust ring, pad light kick, slow-mo, shockwave on deck, lighting by time of day. | Flash decays e^-4t (~0.5 s) | `OnLiftoff` — FlightDriver.cs; vfx FlightJuice.cs |
| Max-Q | inferred in FlightPresenter | Cue: shake .72, punch .45, haptic; HUD q-hot class. | Condensation/vapor cone VFX, no real sim event (inference can mis-fire/lag one frame). | at q drop to 98% of peak | `OnMaxQ(q)` — Core ascent sim; FlightPresenter.cs |
| Staging (burnout/separation/ignition) | sim `burnout`,`cutoff`,`separation`,`ignition` | FlightStageShow: plume collapse, 14-particle separation puff, tumble of spent mesh (kinematic), 18-particle ignition flash, Presenter cue `stage` (shake .4, flash .35). | Real flash light on vehicle, physics debris (bolts/panel bits), hot-gas glow ring, slow-mo beat, night lighting. Tumble is kinematic, not collision. | puff 0.5 s, flash 0.28 s | `OnStageSeparation(stage)`, `OnStageIgnition(stage)` — FlightStageShow.cs |
| Fairing separation | `FlightStaging` altitude gate (110 km default) | Two halves peel (68 deg, 2.2 s, out 5.5 m, drop 3.2 m). | No event/hook, no pyro flash, no pieces tumbling with spin/drag, no sound anchor. | 2.2 s | `OnFairingSeparation(tSim)` — FlightStaging.cs (Core) |
| Boostback burn | `RecoveryPhase.Boostback` | Plume posed, corner inset. | Burn start/stop events, flip VFX, RCS puffs. | per phase sample | `OnRecoveryPhase(phase)` — RecoveryModel/RecoveryShow.cs |
| Landing burn | `RecoveryPhase.Landing` | Landing plume scaled by throttle; dust once touching. | Ground-effect dust ring that grows with altitude to deck, plume light on deck (my PlumeSplash hook already exists for ascent only), heat shimmer. | continuous | `OnLandingBurnStart/End` — RecoveryShow.cs |
| Landing (touchdown ok) | `RecoveryPhase.Down` + `RecoveryScore.Judge` | One 8/28 particle dust or splash burst (reduce-motion lowers it). | Leg-compression bounce, shockwave ring, camera thump, slow-mo, sparks, settle smoke. | single burst | `OnTouchdown(verdict, vz)` — RecoveryShow.cs |
| Crash / RUD (hard touchdown) | `RecoveryViews.BoomKind` | `Shatter`: up to 12 shards via FailureFx plan, no-op under reduce-motion. | Fireball, smoke column, shockwave, deck/ground collision for debris, light flash, camera shake (none in recovery camera). | shards live ~1 s | `OnCrash(kind, pos, energy)` — RecoveryShow.cs |
| In-flight breakup/RUD | sim `failure`; presenter `Lost` | Presenter: shake 1.0, flash 1.0, punch 1.0, 28 2D shards, haptic 40 ms. FailureHost: mesh split (FailureCut), `FailureBurst` system (cap 32 slots, pool), plume cut, tone. | Layered explosion (core flash, fireball, smoke, sparks, embers), real light, shockwave, debris collision, slow-mo, ground impact on `impact` event. | tail 2.8 s, callout 2.6 s | `OnFailure(kind, cause, pos, vel)` — FailureHost.cs / FlightFailureShow.cs |
| Engine failure | `failure` kind engine-out / bad-mixture / engine-overpressure | Plume sputter 0.45 s, per-engine bell flash, tone; overpressure = fireball-debris (8 pieces, 7.5 m). | Per-engine fire/smoke trail, sparks, light flicker (capped), overpressure fireball layers, camera nudge. | sputter 0.45 s, dry 0.40 s | `OnEngineFailure(engineIndex, mode)` — FailureHost.cs |
| Impact (unplanned ground hit) | sim `impact` | Ends sim; failure show if any. | No ground-hit explosion/crater/dust if the sim ends by `impact` with no `failure`. | n/a | `OnImpact(speed, pos)` — FlightDriver.cs |

## Gaps that cut across events
1. No central event bus: each show polls `Sim.Events` or phase samples. Hooks below let VFX subscribe instead.
2. Lighting: only one unshadowed point light (`PlumeGlow`, FlightJuice) and my `PadLights`. Explosions/flashes cast no light on the pad or vehicle; nothing scales with day/sunset/night.
3. Particle budgets are not tiered (`FailurePool.Capacity` 32, fixed). `KitBudgets` has pad tiers only.
4. Debris is 2D (`DebrisField`) with inter-shard collision only; no deck/ground collision, no tumbling bodies for stage/fairing.
5. Slow-mo does not exist; shake is only the presenter's (disabled by reduce-motion, correct).
6. Photosensitivity: presenter `Flash` is a scalar with exponential decay (no strobe) but `FlightJuice.Tint` lerps the body to white at 0.7, and failure/stage flashes are not frequency-capped globally.

## Hooks Gameplay Dev must expose

| Hook | Payload | Raised when | Why needed | Today |
|---|---|---|---|---|
| `OnIgnition(stage)` | stage idx, sim t | sim `ignition` (incl. stage 1 pre-liftoff) | pad flash/steam | polled via Events |
| `OnLiftoff(t)` | sim t, pad pos | alt > 1 m | dust ring, shake, slow-mo | polled |
| `OnMaxQ(q, t)` | q Pa, t | true max-Q point from sim | vapor VFX, sting | inferred in presenter |
| `OnStageSeparation(stage, pos, vel)` | stage, pos, vel (m, m/s) | `separation` | physics debris, tumble start | polled |
| `OnFairingSeparation(t, pos, vel)` | t, pos, vel | fairing jettison | halves tumble, pyro flash | no event |
| `OnRecoveryPhase(phase, t)` | RecoveryPhase | each phase change | boostback/landing burn VFX | sampled per frame |
| `OnTouchdown(vz, tilt, onDeck)` | speeds, deck/ground/sea | `Down` | dust, thump, slow-mo | verdict only |
| `OnCrash(kind, pos, vel, energy)` | boom kind, energy | hard touchdown / impact | explosion, shockwave | `BoomKind` string |
| `OnFailure(kind, cause, engineIndex, pos, vel, t)` | FailureEvent + world pos/vel | sim `failure` | layered explosion, debris | polled via `Result.failure` |
| `OnEngineFailure(engineIndex, mode)` | idx, sputter/overpressure | engine failure kinds | per-engine fire | in `FailureEvent.engineIndex` |
| `OnImpact(speed, pos)` | speed, pos | sim `impact` | ground explosion | polled |
| `Time scale request` | none: VFX owns a presentation-time scale | n/a | slow-mo must not scale the sim | (Slice/FlightDriver must step the sim with an unscaled-aware dt or ignore VFX time scale) |

## Audio sync points for Kestrel Sound
Anchor each to the hook above (sim time `t`). Existing tone ids come from `FlightFailure.ToneOf`.
- Ignition: t0 of `OnIgnition`; low rumble ramp 0.0–1.5 s.
- Liftoff: `OnLiftoff`; impact whump 0 ms, flash/shake peak at +0–80 ms.
- Max-Q: `OnMaxQ` sting (already tied to presenter constants 15 kPa/0.98).
- Staging: separation click at `OnStageSeparation`, ignition thump at `OnStageIgnition` (~+0.3 s).
- Fairing: pyro crack at `OnFairingSeparation`, whoosh over the 2.2 s peel.
- Landing burn: loop starts `OnRecoveryPhase(Landing)`; touchdown thump at `OnTouchdown`, then 1–2 s settle.
- Crash/RUD/failure: blast at `OnFailure/OnCrash` t, shockwave rumble +0.15 s, debris rain over 2–3 s (matches 2.8 s tail).
- Slow-mo: audio should keep real-time pitch; do not pitch-shift when presentation time slows.
