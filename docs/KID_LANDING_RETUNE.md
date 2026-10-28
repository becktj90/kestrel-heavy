# KID landing retune — auto-straighten in the final approach

## The problem

On the last tested build, 0 of 4 KID-mode landings stuck. Playtesting the
unmodified code (headless browser, `?beat=haven`, KID difficulty, KH-1,
scripted pilots) isolated the cliff to exactly one case:

| Pilot | Result | Sink | Offset | Tilt | Grade |
|---|---|---|---|---|---|
| Steers + holds burn (good) ×2 | Recovered ×2 | 1.51 | 5.1 / 6.6 | 3.4° / 3.0° | Perfect |
| Sloppy steering + holds burn | Recovered | 1.69 | 10.8 | 7.9° | Perfect |
| Late burn (only below 200 px) | Recovered | 1.69 | 6.8 | 5.3° | Perfect |
| Fresh player, KH-1 grace, good pilot | Recovered | 1.68 | 2.0 | 1.4° | Perfect |
| Steers, **never holds burn** | **Salvage (tip)** | 1.64 | 6.8 | **27.9°** | Salvage |
| Holds burn, never steers | Splash (water) | 2.81 | 594 | 2.0° | — |

A kid who steers onto the paint softly but never finds the burn button tipped
over at ~28° — the glide pose — even though sink rate and offset were well
inside the limits. The burn is what stands the booster up: leveling only
applied when the burn was lit, or on a KH-1 first flight. So the landing
was gated on discovering the burn button, not on flying the approach well.

Sink rate was never the failure: the sink clamp (3.7) sits far below KID's
sink limit (7.1), so a KID landing cannot fail on sink rate alone. Offset
was never the failure either: KID's steering assist already centers the
booster (worst steered-burner offset was 10.8 px against a 78 px catch
window). Only the tip failed, and only for the never-burner. The never-
steerer splashing is not something tuning should fix — steering onto the
paint is the core skill, and KH-1 does not require a recovery anyway.

## The change (KID mode only, landing phase only)

KID mode now stands the booster up in the final approach (below 220 px, the
existing level altitude) even when the burn was never lit — the same
straightening the game already grants KH-1 first flights and burners. The
burn button still matters: it slows the descent, fires the burn spectacle,
and is still required to straighten on CADET and PAD_RAT. Nothing about the
burn, the glide, the beat order, other difficulties, the game name, or
pricing changes.

1. **`web/js/haven-control.js` — `havenWantAngle` gains a `kid` option.**
   The level condition was `burnLit || firstFlight`; it is now
   `burnLit || firstFlight || kid`. When `kid` is true and the booster is
   closing (below 220 px), the target angle blends from the glide pose to
   the upright deck pose exactly as it already did for burners. When `kid`
   is false, behavior is byte-for-byte unchanged.
2. **`web/js/mission.js` — `updateJacklyn` passes KID through.**
   - `isKid = settings.difficulty === 'KID'`, passed as `kid: isKid` to
     `havenWantAngle`.
   - The fast level slew (3.1) now applies when the burn is lit, on a
     first flight, **or in KID mode**.
   - The RCS straighten callout (`alt < 200`) now fires when the burn is
     lit **or in KID mode**, so the assist is visible ("RCS — straighten
     for the painted deck") instead of silent, and the straighten phase's
     stronger centering assist applies in the final approach.
3. **`tests/haven-control.test.cjs` — one new case.** A KID glide at 40 px
   without a lit burn must stand up under the 14° tip line, mirroring the
   existing first-flight case. All pre-existing assertions are untouched.

What was deliberately NOT changed: `landingTol` / `landingVy` (offset and
sink never failed), the assist strength (centering already works), the
level altitude (220 px), the tip line (14°), CADET / PAD_RAT values, the
Unity shipping app (it has no KID mode), and `ios/KestrelHeavy/Game/` —
that directory is generated output (see its PACK.md: "Do not edit files
here by hand"), regenerated from `web/` by the Xcode project's pre-compile
pack phase, so the CI simulator build picks this change up automatically.

## Playtest result (retuned code)

| Pilot | Result | Sink | Offset | Tilt | Grade |
|---|---|---|---|---|---|
| Steers, never holds burn ×2 | Recovered ×2 | 1.47 / 1.50 | 5.5 / 4.9 | 5.0° / 3.9° | Perfect |
| Steers + holds burn (good) | Recovered | 1.77 | 8.4 | 3.5° | Perfect |
| Sloppy steering + holds burn | Recovered | 1.40 | 11.0 | 9.2° | Perfect |
| Late burn (only below 200 px) | Recovered | 1.68 | 7.1 | 3.9° | Perfect |

The never-burner — the case this retune fixes — went from a 27.9° tip
salvage to a recovered Perfect landing, twice. The good, noisy, and late
pilots are unchanged (still Perfect), so the retune does not move the
already-working cases.

Note on one noisy re-run: a first noisy attempt on retuned code hit a
headless-harness stall (219 game-seconds without reaching the deck, in the
glide far above the 220 px level altitude where every retuned line lives,
so the retune cannot have caused it; the same stall family appeared on
unmodified code under headless load earlier). The re-run completed cleanly
as a recovered Perfect, matching its unmodified-code baseline.

## Method

Headless Chromium (system build) driving the repo's Phaser web game served
from a local mirror of `web/` at the change set, jumping straight to Haven
with `?beat=haven`. Pilots are scripted (steer toward the deck with a lead
on drift; hold burn per profile). A competent-burner baseline and the
never-burner case were each run before and after the change; the never-burn
case is the one this retune fixes. Node tests (`tests/*.test.cjs`) pass
6/6 on the retuned code, including the new KID case.
