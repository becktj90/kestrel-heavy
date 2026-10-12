# Pad ↔ vehicle umbilical contract

Assembly: `Kestrel.Kit.Contracts` (`unity/Assets/Kestrel.Game/Kit/Contracts/`). Engine-free (`noEngineReferences`), no dependencies. Vehicle code references this assembly only and never references Pad. Pad code never reads vehicle internals; it only sees what this contract exposes. Owner: Graphics Dev. Changes need a PR pinged to Kestrel Art.

## Frame and units
Metres, and seconds from T-0 (negative means before liftoff). Poses are vehicle-local: origin at the base of the first stage on the centre line, +Y up the stack, +Z toward the pad tower. An attach point's +Z axis points out of the skin, and the pad connector mates along −Z. `Quat` uses the same (x, y, z, w) convention as `UnityEngine.Quaternion`. `Kestrel.Kit.ContractConvert` converts between the two.

## Vehicle side: `IUmbilicalVehicle`
- `IReadOnlyList<AttachPoint> AttachPoints`, where each point has:
  - `Id`, in the form `s{stage}.{fuel|lox|pwr|purge|hyd|vent}.{index}` (see `UmbilicalIds.Make`)
  - `Type` (`UmbilicalType`): Fuel, Oxidizer, PowerData, Purge, Hydraulic, Vent
  - `Stage`, where 0 is the first stage
  - `Pose`
  - `PlateRadius`
  - `Release` (`ReleaseStyle`): SwingArmRetract, TailServiceMastDrop, LanyardPull, Hinged
- `OnUmbilicalEvent(in UmbilicalEvent)` is a cosmetic hook (door closes, vent puff). It must not throw.

## Pad side: `IUmbilicalConnector`
There is one connector per attach point: a swing arm, a tail service mast, or a hose.
- `Connect(AttachPoint, LocalPose vehicleToPad)` snaps to the mated pose at scene build. It returns false when the point is out of reach, and the pad generator then has to move or resize the arm.
- `OnUmbilicalEvent` drives the animation.
- `State` (`UmbilicalState`) is one of Detached, Connected, Flowing, Released, Retracted.

## Events and states
`UmbilicalEventKind` has these values: Connect, FlowStart, FlowStop, Disconnect, ArmRetractStart, ArmRetractDone, and Abort (a hold or recycle).

`UmbilicalStates.Apply` is the single transition table both sides use:
- Abort re-mates anything that hasn't retracted.
- Retracted arms stay retracted, so a scrub after that point needs a full reset.

## Release timeline (defaults; fictional and generic)
| Type / style | Disconnect T | Retract done |
|---|---|---|
| Fuel (arm) | −150 s | +8 s after disconnect |
| Oxidizer (arm) | −120 s | +8 s |
| Hydraulic | −60 s | +1.5 s |
| Vent | −45 s | +1.5 s (+8 s on an arm) |
| Purge | −2 s | +1.5 s |
| Power/data | −1 s | +1.5 s |
| Any, TailServiceMastDrop | 0 s (liftoff) | +1.5 s |
| Any, LanyardPull | +0.05 s (first motion) | +1.5 s |

`UmbilicalTimeline.Build` emits these events for each point:
- Connect at −3600 s
- FlowStart at −2100 s
- FlowStop at disconnect − 0.5 s
- Disconnect
- ArmRetractStart at disconnect + 0.2 s
- ArmRetractDone

The events are sorted by time.

The MissionEvent timeline (DECISIONS #30 and #32) can override the times, but `UmbilicalTimeline.Validate` must still return no errors. It checks for:
- unique IDs
- events in order for each point
- every point disconnects
- no Disconnect later than T+0.1 s
- no events for unknown IDs

Per DECISIONS #32, events exist only for hardware that exists. A vehicle without a Purge point gets no purge events.

## Tower anchors (D2): where pad-side arms mount
Assembly: `Kestrel.Pad.Core` (`Pad/Core/TowerAnchors.cs`), engine-free. D4 umbilical and service arms attach to named anchors on the tower instead of re-deriving tower geometry. `PadLayout.Anchors` (null for Rail pads) and `TowerLayout.Anchors` expose them.

- Frame: pad space. Origin on the vehicle centre line at deck level, +Y up, tower on +Z. An anchor's local +Z points out of the tower face it sits on (vehicle-side `s` anchors face pad −Z, toward the vehicle), and local +Y is up. `Rotation` is a contract `Quat`.
- `TowerAnchor`: `Id`, `Position` (`Vec3`), `Rotation` (`Quat`), `Level` (0 = first deck at `LevelHeight`, −1 = ground, `Levels` = cap) and `Face` (`s|e|n|w|top`).
- Ids:
  - `level.{NN}.{s|e|n|w}`: centre of each face at the deck height of level NN, which is (NN+1) × `LevelHeight`
  - `arm.crew`: root of the crew-access arm (`TowerAnchors.CrewArm`)
  - `arm.{attachId}`: swing-arm pivot for each `SwingArmRetract` attach point (`TowerAnchors.ArmId`)
  - `cap.top`: base of the lightning spike
  - `ground.s`: foot of the vehicle face
- API: `Get(id)` (throws `KeyNotFoundException` for an unknown id), `TryGet`, `Contains`, `All`, `LevelNearest(y)`, `OnFaceNear(y, face)`, and `Add(id, pos, face, level)` for generator-owned extras. Ids are unique, and the same seed always gives the same anchors.
- D4 rule: mount a new arm or connector on `arm.{attachId}` when it exists. Otherwise use `OnFaceNear(attach.Y)`. Do not hard-code tower dimensions.
