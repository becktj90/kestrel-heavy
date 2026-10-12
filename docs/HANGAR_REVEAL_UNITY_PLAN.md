# Hangar Reveal: Unity plan

Status: **plan only, no code.** Author: Art. Feeds U8 "hangar 3D reveal" (`docs/UNITY_SHIP_PLAN.md`) and consumes the V renderer (audit §1, §5). The audit puts the V renderer → Hangar Reveal link in P0-6.
**Ownership flag:** UNITY_SHIP_PLAN U8 lists Graphics Dev for "hangar 3D", while the V1–V6 assignment gives Art "deepen the existing renderer and Hangar Reveal". Proposal: Art owns the reveal *scene, camera and vehicle staging* under `Vehicle/**`, Graphics Dev owns the hangar *environment* (walls, floor, lights) via the kit, and UX owns all UI. The lead should confirm.

## 1. Source: the JS version
The web/iOS game has no 3D reveal. "Hangar" there is a loadout sheet:
- `web/js/hud.js`: `ng-hangar` sheet, `setHangar(settings, catalog, combo, onEquip)`, `setHangarMedals`
- `web/js/loadout.js`: modular stack, combinatorial parts
- `web/js/mission.js`: `openHangar()`, `refreshHangar()`, and an unlock banner `HANGAR — <id> unlocked`
- `web/js/medals.js`: one-line medal shelf
- iOS copies of the same files live under `ios/KestrelHeavy/Game/js/`

What carries over as behaviour:
- **the moments:** open hangar, equip a part, and unlock (the banner)
- **the data:** the current loadout or design, the unlocked part, and the medal shelf

What's new in Unity is a 3D portrait reveal of the generated vehicle at those moments. Text and medals stay in UX's UI.

## 2. Scene and staging
- An additive scene `HangarReveal`, loaded by `SceneFlow` (Kestrel Arcade wires it). Its root lives under `Kestrel.Game/Vehicle/Hangar/`.
- The vehicle comes from the **same builders** as flight (RocketMeshBuilder + V1–V6), from `(Design, serial, seed)`. There is no second renderer and no hangar-only meshes.
- The vehicle stands on a service stand with its origin at the stage-1 base (contract frame), +Z facing the camera's key side.
- The environment is a Graphics Dev kit hangar: floor, back wall, 2–3 light rigs, and fictional, generic stencils only.
- Portrait framing: the vehicle fills about 70% of the safe-area height. Wide vehicles fall back to a 3/4 view. `PortraitLayout` safe area is respected.

## 3. Camera beats (portrait; total ≤ 6 s, skippable by tap)
| # | Beat | Time | Camera | Purpose |
|---|---|---|---|---|
| 1 | Doors / light-up | 0.0–0.8 s | locked wide, lights ramp | Establishes the hangar. Reduced-motion: cut |
| 2 | Engine-bay hero | 0.8–2.2 s | low dolly up from the bells (V1 hero detail) | Shows distinct bells and plumbing up close |
| 3 | Tilt up the stack | 2.2–4.0 s | crane up, slight orbit 15° | Welds, stringers, interstage, livery (V2/V5) |
| 4 | Nose + serial | 4.0–5.0 s | push in on fairing + fictional serial | Identity (V5) |
| 5 | Settle | 5.0–6.0 s | ease to the orbit-inspect home pose | Hand-off to interactive orbit |

- **Unlock variant:** beats 1→(the unlocked part's close-up)→5, 3 s max. The part's element id comes from the element graph (audit §6).
- Cameras are `Cinemachine`-free (no new package). It's a spline plus easing in our own code, deterministic, and replay-safe.
- **Accessibility:** reduced motion means cross-fades with no dolly. Reduced flash means no light strobes.

## 4. Inspect and cutaway hooks
- **Orbit inspect (2.0, solid):** after beat 5, drag to orbit (yaw ±180°, pitch −10…+45°) and pinch to zoom. Zoom is clamped so the camera never clips the vehicle.
- **Tap → element card:** tap raycasts to an instance id, then an element id (Gameplay Dev's `Kestrel.Core.Elements` graph). The vehicle exposes `IElementPicker.Resolve(instanceId) → elementId` from `Vehicle/**`. UX renders the card.
- **Cutaway (2.0, solid):** a clip plane on the vehicle materials, swept by a slider. Isogrid and internal tanks or lines become real geometry only in this mode (approved rule). Requires a kit material keyword (Graphics Dev ask).
- **Exploded / load-path:** hooks exist (`VehicleViewMode.Exploded | LoadPath`) but ship only if P0-6 is on schedule (approved). Otherwise 1.1.
- **Detail Lab:** the same scene with a debug panel toggling LOD tier, kit layers and wear. The UX panel is dev-only.

## 5. Perf budget (hangar is the close-up hero case)
- The vehicle uses its normal V budget at LOD0 with hero bias. The hangar environment is far simpler than the pad, so its share moves to the vehicle (budget rule: far props go first).

  | Tier | Hangar env tris | Vehicle tris | Draw calls (all) | Real lights | Frame |
  |---|---|---|---|---|---|
  | Low | ≤ 30 k | ≤ 150 k | ≤ 90 | 1 + baked | 60 Hz |
  | Medium | ≤ 60 k | ≤ 300 k | ≤ 150 | 2 | 60 Hz |
  | High | ≤ 100 k | ≤ 450 k | ≤ 200 | 3 | 120 Hz |

- Load ≤ 300 ms on Low, including vehicle generation, which is cached from the designer or flight.
- **IPA:** about 0 new asset bytes (procedural, kit materials). Any hangar trim sheet counts against Graphics Dev's env 8 MB.
- **Memory:** vehicle meshes are reused from cache. Only the hangar env adds memory, ≤ 6 MB.

## 6. Dependencies
| Need | From | What |
|---|---|---|
| Reveal moments + data (equip, unlock, open) | Gameplay Dev | Events or API in Core: current Design, unlocked part id, serial (Fleet record). Art doesn't read or write Fleet/Data Sheet numbers |
| Element graph + ids | Gameplay Dev | `Kestrel.Core.Elements` (audit §6) |
| Screens: hangar UI, element card, skip button, cutaway slider, Lab panel | UX | `UI/Screens/**`. Art exposes hooks only |
| Hangar environment, clip-plane keyword, LOD/instancing | Graphics Dev | Kit |
| Scene wiring in SceneFlow/SliceApp | Kestrel Arcade | One wiring PR |
| Reveal stinger / door sound | Sound | `Audio/**` anchors at beat times |
| Golden captures | QA | Beats 2/3/5 portrait stills |

## 7. Sequencing
H0 (after V2a/V1a): a static hangar with orbit inspect. H1: camera beats + skip + reduced motion. H2: tap → element id (after the element graph lands). H3: cutaway. H4 (if on schedule): exploded and load-path. Each is a small PR off main, with tests (beat timing determinism, camera never inside bounds, picker round-trip) and a perf note.
