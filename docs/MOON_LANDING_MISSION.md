# Tranquil Touch

Proposal only. This mission is not in `data/campaign.json`, and nothing in the flight code reads it.

Decision #40 keeps 2.0 on Earth orbit. Lunar landing is post-launch work. KH-9 Moon Shot and KH-17 Lumen Down stay trans-lunar: each one is a C3 burn from a 200 km Earth orbit, then a coast. Neither one has a surface.

## What the player would fly

A late contract, after the Earth campaign. The rocket has already left Earth. The player flies only the last powered descent: a small lander, already on a steep path toward a named patch of mare, with no air and no barge.

The lander is not Lumen Down. That payload is 4,000 kg and its contract ends at C3. Tranquil Touch is a smaller ship, about a tonne, with a throttle that can hover in lunar gravity instead of a high-thrust Earth booster.

Numbers the future sim has to meet:

| | |
| --- | --- |
| Moon radius | 1,737.4 km |
| Surface gravity | 1.62 m/s² |
| Atmosphere | none. No drag, no grid fins, no entry burn |
| Start of the playable descent | 15 km above the surface, sink 40 m/s, horizontal speed under 30 m/s, propellant still in the tanks |
| Touch | sink at most 2 m/s, tilt at most 8°, within 20 m of the aim point |
| Pass | same words as Haven: good or perfect. A miss, a tip, or an empty tank is not a landing |

At a thrust-to-weight of about 1.5 on the Moon (thrust / (mass × 1.62)), net acceleration is 0.81 m/s². Killing a 20 m/s sink takes on the order of v² / (2 × 0.81) ≈ 250 m of altitude, so the hover burn starts well above the surface and stays gentle. A stuck full throttle climbs off the aim point. A closed throttle falls the rest of the way. That is the skill, the same shape as Haven and not an automatic gold.

## Why it is not wired in

`MissionRamp` scores `circular`, `ellipse`, `c3`, and `apogee`. A `lunar-surface` target would be a new kind, and the ascent integrator never leaves Earth. Putting the entry in `data/campaign.json` now would either fail the load or pretend a C3 coast is a landing. KH-9 and KH-17 keep their current targets.

## Data entries (do not copy into the packs yet)

Campaign row. Index 21 is past KH-20 so it does not collide with a shipped id.

```json
{
  "index": 21,
  "id": "m21",
  "orbit": "kh-m21",
  "name": "Tranquil Touch",
  "payloadId": "tranquil-lander",
  "massKg": 1100,
  "volumeM3": 6,
  "diameterM": 2.2,
  "target": {
    "kind": "lunar-surface",
    "site": "Mare Tranquil",
    "startAltM": 15000,
    "startSinkMps": 40,
    "startEastMps": 12,
    "touchSinkMps": 2,
    "touchTiltDeg": 8,
    "padRadiusM": 20
  },
  "requirements": [
    {
      "id": "lunarLanding",
      "grade": "good",
      "site": "Mare Tranquil"
    }
  ],
  "budgetKc": 210000000,
  "rewardKc": 210000000,
  "unlocks": [],
  "gentleDefault": true,
  "band": "late",
  "briefing": {
    "payloadName": "Quiet Step",
    "payloadKind": "Small lunar lander",
    "customer": "Farwater Survey",
    "goal": "Set a 1,100 kg lander down on Mare Tranquil.",
    "control": "Quiet Step is 1,100 kg. The descent starts at 15 km with a 40 m/s sink. Touch under 2 m/s, within 8 degrees of upright, inside 20 m.",
    "whatItIs": "A small lander for a soft touch on the Moon.",
    "whatItDoes": "It slows on one engine and sets down. There is no air to help.",
    "why": "KH-9 and KH-17 only leave Earth. This one flies the last kilometres.",
    "form": "lander",
    "codex": "lunar-landing",
    "bonuses": [
      {
        "id": "soft",
        "label": "Touch down under 2 m/s"
      }
    ]
  }
}
```

Payload row. Separate from `lunar-lander` / Lumen Down.

```json
{
  "id": "tranquil-lander",
  "displayName": "Quiet Step",
  "label": "Small lunar lander",
  "mass": 1100,
  "volume": 6,
  "diameter": 2.2,
  "maxG": 3,
  "maxHeatFlux": 0,
  "orbit": "lunar-surface",
  "form": "lander",
  "dryKg": 800,
  "descentPropKg": 300,
  "thrustN": 2700,
  "ispSec": 310,
  "whatItIs": "A small lander with one engine and four legs.",
  "whatItDoes": "It flies a powered descent and stands on the mare.",
  "why": "1,100 kg on the Moon. Thrust 2,700 N is about 1.5 times its weight there, so the hover can be gentle. 2,700 / (1,100 × 1.62) ≈ 1.5."
}
```

Thrust check: 2,700 N / (1,100 kg × 1.62 m/s²) = 1.52. The engine can hover and can still climb if the throttle sticks open. Three hundred kilograms of propellant at 310 s is about 760 m/s of ideal delta-v (`310 × 9.80665 × ln(1100 / 800)`), enough for the short descent above and not enough to waste on a long hover.

## Later, when someone builds it

A lunar state, inverse-square Moon gravity, and a hoverslam that uses the lander's thrust. No Earth `RecoverySim` reuse without a new gravity and no drag term. Scoring can share the Haven grade words (perfect / good / hard / salvage) and the 2 m/s leg limit. The campaign loader grows a `lunar-surface` kind only in that change, and KH-9 and KH-17 stay C3.
