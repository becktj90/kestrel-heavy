# Vehicle Detail V1: engines and engine bays (spec)

Status: **spec only, no code.** Owner: Art (Kestrel Art). Track: VEHICLE DETAIL V1–V6, Kestrel Heavy 2.0, P0-3 in `docs/PAD_VEHICLE_DETAIL_AUDIT.md` §9 (V1 + V3 run once the contract is merged).
Built on Graphics Dev's shared detail kit and umbilical contract in **PR #86** (`unity/Assets/Kestrel.Game/Kit/**`, `docs/UMBILICAL_CONTRACT.md`). PR #86 isn't merged yet. If #86 changes before it merges, this spec follows it, not the other way round. No fork of either.
Rules: Unity 6.3 URP, portrait only, $0, procedural only (no shipped meshes), no real vehicle designs or markings, nothing from Trevor's workplace. References only as logged in `docs/REFERENCE_LEDGER.md`.

Approved direction (Chief of Staff, all 19 audit recommendations, 2026-10-12):
- Bell shape comes from the catalog expansion ratio, plus seeded per-serial variation, and engines keep instancing (audit rec 8).
- Isogrid is a normal map. Real geometry appears only in cutaway and the Lab (rec 7, applies to V2).
- Cutaway and inspect are SOLID in 2.0. Exploded and load-path views ship only if on schedule (rec 10).
- Livery is presets plus 2 colours plus earned marks (rec 11, V5).
- Anomaly damage is cosmetic only (rec 14, V6).
- **Budget rule:** cut far-field props before close-up hero detail. The engine bay is hero detail.

## 1. Inputs and ownership boundaries
| Input | Source | Owner | V1 use |
|---|---|---|---|
| Engine catalog | `data/engines.json` (15 entries): `id, cycle, propellant, expansionRatio, diameter, gimbal, thrustVac…` | Gameplay Dev | Read-only. Drives bell shape and bay part placement. V1 never writes numbers. |
| Stage layout | `Kestrel.Core/Vehicle/Design.cs`, `Stack.cs` (engine count, stage diameters) | Gameplay Dev | Read-only. Cluster pattern and bay height. |
| Existing renderer | `unity/Assets/Kestrel.Slice/Runtime/RocketMeshBuilder.cs` (32-seg lathe, nested bells, one mesh/material) | Kestrel Arcade wires `SliceApp.cs` | V1 **replaces the bell section of the lathe** behind a builder call. Kestrel Arcade does the one-line wiring in SliceApp (audit §9). |
| Materials | `Kestrel.Kit.MaterialLibrary.Get(MaterialRole)` | Graphics Dev | Only material source. No `new Material` in Vehicle. |
| Meshes | `Kestrel.Kit.KitMesh.Tube/Pipe/Hose/Beam/IBeam` | Graphics Dev | Feed lines, ducts, struts, harness. |
| Instancing | `Kestrel.Kit.InstanceBatch` (RenderMeshInstanced, 1023/chunk) | Graphics Dev | Bells, turbopumps, valves, brackets. |
| LOD | `Kestrel.Kit.LodPolicy.Select(q, DetailSize, d, bias)`, `RadialSegments(lod)` | Graphics Dev | All V1 parts. |
| Budget | `Kestrel.Kit.BudgetLedger` | Graphics Dev | V1 uses a **separate ledger instance** with vehicle limits (§6). The kit's `Within()` checks pad limits only. |
| Umbilical | `Kestrel.Kit.Contracts.IUmbilicalVehicle`, `AttachPoint`, `UmbilicalIds.Make` | Graphics Dev | Aft attach points (§5). |

### Assembly layout (owned paths only)
- `unity/Assets/Kestrel.Game/Vehicle/Core/Kestrel.Vehicle.Core.asmdef`: engine-free, `noEngineReferences: true`. It holds the bell profile math, part placement, and attach-point generation. References are **Kestrel.Kit.Contracts** and `Kestrel.Core`, read-only, for engine and stage data.
- `unity/Assets/Kestrel.Game/Vehicle/Runtime/Kestrel.Vehicle.asmdef`: meshes, instancing, and LOD. References `Kestrel.Vehicle.Core`, `Kestrel.Kit.Contracts`, `Kestrel.Kit` (runtime, for `MaterialLibrary`/`KitMesh`/`InstanceBatch`), and `Kestrel.Kit.Core` (for `LodPolicy`/`MaterialRole`). **Never** references any Pad assembly.
- `unity/Assets/Kestrel.Game/Vehicle/Tests/Kestrel.Vehicle.Tests.asmdef`: EditMode tests, Editor platform only, with `UNITY_INCLUDE_TESTS`. The `.meta` convention matches the existing `Kestrel.Game.Tests`.
- **Open point for the lead:** the brief says "Vehicle references only Kestrel.Kit.Contracts". That holds for **contract and pad coupling**, since Vehicle never sees Pad. But the materials, `KitMesh` and LOD live in `Kestrel.Kit` / `Kestrel.Kit.Core`, so the Runtime assembly must reference those as well. The engine-free Core assembly does reference Contracts only. Confirm this reading.

## 2. Bell profiles from expansion ratio
Every engine type gets its own lathe profile, computed from catalog data at load (≤ 1 ms per type). No profile is shipped.

### 2.1 Geometry (generic textbook nozzle relations)
- Exit radius `Re = diameter / 2` (catalog `diameter`, m). Throat radius `Rt = Re / sqrt(ε)`, where `ε = expansionRatio`.
- Converging section: chamber radius `Rc = Rt * sqrt(CR)`, with contraction ratio `CR = 3.0` for pump-fed cycles and `4.0` for pressure-fed. Chamber length is `Lc = 2.2 * Rc`. The converging angle is 30°. The throat blend radii are `1.5 Rt` upstream and `0.38 Rt` downstream.
- Diverging bell: a quadratic Bézier "thrust-optimised parabola" approximation. Length is a fraction `f` of a 15° cone: `Ln = f * (Re − Rt) / tan 15°`, with `f = 0.80` by default.
  - The initial angle θn and exit angle θe interpolate on ε (table below). θn grows and θe shrinks as ε rises.

  | ε | θn | θe |
  |---|---|---|
  | ≤ 10 | 21° | 12° |
  | 20 | 24° | 9° |
  | 50 | 28° | 8° |
  | 100 | 32° | 7° |
  | ≥ 200 | 35° | 6° |

- **Profile samples (half-section points) by LOD:** 24 at LOD0, 12 at LOD1, 6 at LOD2. Radial segments around the bell are 32, 16 and 8. Points cluster by curvature, since the throat needs the most.
- **Wall:** the bell is a two-sided shell. The outer surface sits at `+t` and the inner at `−t`, with `t = 0.012 Re`. A lip ring closes the exit. The inner surface is only visible from below. Low drops the inner surface on LOD ≥ 1 and draws backfaces with the same material instead.

### 2.2 Distinct per engine type (from today's catalog)
| Catalog id | Cycle | ε | Visual identity (no real-engine likeness) |
|---|---|---|---|
| gg-kerolox / -small | gas-generator | 16 | Short bell, visible GG exhaust duct dumping beside the bell (§3) |
| gg-kerolox-vac / -small-vac | gas-generator | 165 / 80 | Long, wide vacuum bell. The extension is a separate radiatively-cooled skirt with a joint ring |
| gg-methalox-small | gas-generator | 20 | Short bell, matte methalox-soot band |
| sc-kerolox | staged-combustion | 37 | Compact bell. A thick hot-gas manifold ring sits on top of the chamber, with no exhaust duct |
| sc-hydrolox | staged-combustion | 69 | Tall slender bell with dense regen tube banding. Two preburners |
| ff-methalox / -vac | full-flow | 34 / 90 | Two turbopumps mirrored either side, symmetric, short bulky powerhead |
| exp-hydrolox | expander | 285 | Very long thin-walled bell with a regen jacket over the top third. Small single pump |
| pf-hypergolic / pf-udmh | pressure-fed | 55 / 100 | No turbopump. Spherical valve body and simple short feed lines. Radiative bell |
| srm-small / srm-large | solid | 12 / 7.7 | §4 solid motor variant |
| hybrid-mid | hybrid | 15 | Solid-style casing with a small liquid oxidiser valve cluster at the top |

The *shape* comes from ε and cycle. The *identity* comes from the bay parts (§3). Two catalog engines that share ε and cycle can only differ in diameter. That's acceptable, and the test in §7 enforces it.

### 2.3 Seeded per-serial variation (keeps instancing)
- **Seed:** `hash(vehicleSerial, stageIndex, engineSlot)`. `vehicleSerial` is the fictional V5 serial (`KH-<class>-<nnn>`). Until V5 lands it is the design hash.
- **Mesh-level variation is per engine type per vehicle, not per engine.** Regen tube count (±10%, snapped so it divides the radial segments), tube banding depth, and the lip ring profile. This gives at most one bell mesh per (type, vehicle), and the cluster shares it. That keeps instancing.
- **Per-instance variation goes in the matrix and colour, not the mesh:**
  - gimbal offset of ±0.4° about two axes, at rest
  - roll about the axis (0–360°, hides tube alignment)
  - wear tint, after V6
  - heat-tint gradient amount, after V6

  This needs per-instance colour support in the kit (see §8, ask Graphics Dev). Until then, roll and gimbal variation work with today's `InstanceBatch`.

## 3. Turbopump and plumbing parts (engine bay)
All parts are kit parts placed by cycle, in engine-local space. The engine origin is the gimbal pivot at the top of the chamber, with −Y pointing down the bell.

| Part | Mesh source | Cycles | Count/engine | DetailSize | Material role |
|---|---|---|---|---|---|
| Turbopump body (pump volute + turbine) | Vehicle lathe (compound of 3 revolved sections) | GG, SC, FF (×2), expander | 1 (FF 2) | Small | Galvanized (stand-in for bare metal) |
| Gas generator / preburner | Vehicle lathe (capsule) | GG (1), SC (1–2), FF (2) | 1–2 | Small | Galvanized |
| GG exhaust duct | `KitMesh.Pipe`, 3-point bend | GG | 1 | Small | PaintedSteel + heat tint |
| Fuel and oxidiser feed lines (tank → pump inlet) | `KitMesh.Pipe` with a flexible bellows section (ring bands) | all liquid | 2 | Small | Insulation for cryo lines, Galvanized otherwise |
| Pump discharge → injector lines | `KitMesh.Pipe` | pump-fed | 2 | Small | Galvanized |
| Hot-gas manifold ring | `KitMesh.Tube` closed loop | SC, FF | 1 | Small | Galvanized |
| Main valves (spherical body + actuator) | Vehicle lathe + `KitMesh.Beam` | all liquid | 2–4 | Prop | SafetyYellowSteel actuator body, Galvanized valve |
| Gimbal actuators | 2× `KitMesh.Beam` cylinder stand-in + rod | engines with `gimbal > 0` | 2 | Small | PaintedSteel |
| Gimbal block / thrust frame struts | `KitMesh.IBeam` | all | 4–8 per engine | Medium | PaintedSteel |
| Harness / sensor lines | `KitMesh.Hose` (slack 1.05) | all | 2 | Prop | RubberHose |
| Engine bay heat shield / boot | Vehicle disc with engine cut-outs | stages with ≥ 2 engines | 1 per stage | Medium | Insulation |

Placement is deterministic per (cycle, seed). The turbopump sits at a fixed azimuth per cycle, mirrored for FF. The seed picks one of 4 azimuth layouts so a cluster doesn't look stamped. All engines of a type in one stage share a layout, because they share instance batches.

Bay plumbing collapses at LOD1 per the audit V1 Low note. Lines under 0.05 m radius merge into one static mesh per engine type, and harness lines drop out.

## 4. Solid-motor variant (srm-small, srm-large, hybrid-mid casing)
- Segmented cylindrical casing. The segment count is `clamp(round(L / (2.5 D)), 1, 5)` from stage length L and diameter D.
- Each joint is a raised field-joint ring (2 rings at LOD0, 1 at LOD1, none at LOD2, normal map only). There's a forward dome and an aft dome.
- **Nozzle:** a short, submerged, fixed or flexible-bearing conical-bell blend, from ε via §2.1 with `f = 0.70` and θn capped at 20°. A dark ablative exit-cone band uses RubberHose until a vehicle role exists (§8).
- **Aft skirt:** a short flared frustum with 4 hold-down posts. It also carries one TVC actuator pair when the catalog has `gimbal > 0`. The solid catalog has no gimbal field today, so the skirt is fixed. Gameplay Dev owns that field.
- There are no turbopumps or feed lines. The hybrid adds an oxidiser tank interface ring with 2 valves and 1 feed line.
- The solid uses the same bell mesh cache and instancing path, so strap-on boosters of the same type draw as one batch.

## 5. Attach points (umbilical contract, PR #86)
V1 implements the engine-bay half of `IUmbilicalVehicle.AttachPoints`. V3 adds tank-wall and upper-stage plates, and both feed **one** list per vehicle, through one `VehicleUmbilicals : IUmbilicalVehicle` in Vehicle/Runtime.

- **Ids** always come from `UmbilicalIds.Make(stage, type, index)`, never typed by hand.
  - **Stage indexing:** the contract says `Stage` 0 is the first stage, so the first stage's aft LOX plate is `s0.lox.0`, and `s1.lox.0` is the second stage. The kickoff example `s1.lox.0` follows the contract only if it refers to the second stage. **Confirm with Graphics Dev.** This spec follows the code: `Stage = 0` for the first stage.
- **Pose frame:** vehicle-local metres. The origin is the base of stage 1 (the first stage) on the centre line, +Y is up the stack, and +Z points toward the tower (azimuth 0). Each point's local +Z is the **outward skin normal**, and the pad connector mates along −Z. Rotation is built with `Quaternion.LookRotation(outwardNormal, Vector3.up)` and converted with `ContractConvert`.
- **V1 points: tail service plates on the aft skirt / engine bay**

  | Id | Type | Release | Station | Azimuth | PlateRadius |
  |---|---|---|---|---|---|
  | `s0.fuel.0` | Fuel | TailServiceMastDrop | Y = 0.6 · bay height | 0° | 0.25 m · clamp(D/3.7, 0.6, 1.6) |
  | `s0.lox.0` | Oxidizer | TailServiceMastDrop | same Y | +20° | same |
  | `s0.pwr.0` | PowerData | TailServiceMastDrop | Y = 0.85 · bay height | −20° | 0.15 m |
  | `s0.purge.0` | Purge | TailServiceMastDrop | Y = 0.4 · bay height | 0° | 0.12 m (pump-fed engines only) |
  | `s0.hyd.0` | Hydraulic | TailServiceMastDrop | Y = 0.4 · bay height | +20° | 0.12 m (only if any stage-0 engine has `gimbal > 0`) |

  Azimuth is measured from +Z toward +X. Position = `(R·sin az, Y, R·cos az)`, where R is the skin radius at Y.
- **Solid stages:** `s0.pwr.0` only (LanyardPull), plus `s0.hyd.0` if the solid has TVC. There are no fuel or oxidiser plates.
- **Hardware-existence rule (DECISIONS #32):** a point exists only when its hardware exists. That's why purge and hydraulic are conditional above.
- **`OnUmbilicalEvent`** is cosmetic only and must never throw. On Disconnect, the plate's door cover rotates closed over 0.3 s. On a Vent event it starts a vent puff hook, which Sound and VFX can bind to later. Abort reopens the door.
- **Validation:** for every catalog template, `UmbilicalTimeline.Validate(points, UmbilicalTimeline.Build(points))` must return empty. Ids are unique and every point's +Z is outward (`dot(rot·+Z, radial) > 0.99`).

## 6. LOD tiers, triangle budgets, instancing, perf
### 6.1 LOD (via `LodPolicy.Select`)
| Part family | DetailSize | LOD0 | LOD1 | LOD2 | Culled |
|---|---|---|---|---|---|
| Bell + chamber | Medium (hero, `lodBias = 1.5`) | 32×24, regen bands in geometry | 16×12, bands in normal map | 8×6 | never while the vehicle is on screen |
| Turbopump / GG / manifold | Small | full | single merged lathe | dropped (silhouette only) | per policy |
| Feed and discharge lines | Small | 12 radial (`RadialSegments(0)`) | 6 radial, merged per engine type | dropped | per policy |
| Valves, harness, brackets | Prop | full | dropped | – | per policy |
| Thrust frame struts | Medium | IBeam | Beam | dropped | per policy |
| Solid casing + joints | Large | 32 radial, 2 rings per joint | 16, 1 ring | 8, normal map | per policy |

Hero rule: the bell and chamber get `lodBias = 1.5` because the chase camera frames the engines at liftoff. When the budget is tight, bay props drop first, then lines. **Bells keep LOD0 longest.**

### 6.2 Triangle budget (vehicle engine-bay share of the audit §3 vehicle budget)
| Tier | Vehicle tris (audit) | V1 engine-bay cap | Per-engine LOD0 (bell + bay) | Notes |
|---|---|---|---|---|
| Low | 100 k | **35 k** | 1.9 k bell + 1.6 k bay ≈ 3.5 k | At 9 engines + 2 vac: about 30 k. 27 engines force LOD1 on the outer ring |
| Medium | 250 k | **80 k** | 3.0 k + 3.0 k ≈ 6 k | |
| High | 400 k | **130 k** | 3.0 k + 4.5 k ≈ 7.5 k | |

### 6.3 Draw calls and instancing
- **One `InstanceBatch` per (part mesh, material, vehicle).** For a 9-engine kerolox first stage plus one vac upper stage:
  - bells: 2 batches, 2 materials (bell and nozzle extension)
  - turbopump: 1 batch, GG: 1, duct: 1
  - feed lines: 2 (merged per type at LOD1), valves: 2
  - actuators: 1, struts: 1, harness: 1
  - heat shield: 1
  - That's about **14 batches**, which is ≤ 18 draw calls on Low (no shadows) and ≤ 30 on High (sun shadow pass).
- **V1 draw-call cap:** Low 20, Medium 32, High 40. This is inside the audit's vehicle share (pad+vehicle ≤ 120 / 200 / 260).
- **The vehicle moves:** the batches are drawn with matrices `vehicleRoot.localToWorld × engineLocal`, rebuilt once per frame per batch. That's ≤ 27 engines × ~12 parts, so about 330 matrix multiplies per frame, under 0.05 ms.
- **SRP Batcher:** materials come only from `MaterialLibrary`, so there's one material per role and no per-object instances. This fixes the audit §1 issue for the engine section.

### 6.4 Memory, generation time, IPA
- **Mesh memory:** about 15 unique meshes × 3 LODs × ~4 k verts × 48 B ≈ 8.6 MB worst case on High, and ≤ 4 MB on Low. It's cached per (design hash, serial) and freed on vehicle swap. The cap is ≤ 4 MB Low, ≤ 8 MB High, inside the audit §3 vehicle mesh share.
- **Texture memory:** 0 new in V1. Regen banding uses one generated 128×128 normal tile (64 KB with mips). Graphics Dev decides whether that belongs in the kit (§8).
- **Generation:** ≤ 15 ms on Low for V1 parts (the audit's whole-vehicle budget is ≤ 60 ms). It's pure C# in the Core assembly, Burst-able later.
- **IPA delta: about 0 bytes of assets.** Nothing is shipped, everything is generated. Code adds an estimated 40–60 KB of IL2CPP binary. That's well inside the audit's pad+vehicle ≤ 4 MB allotment and the 40 MB cap.
- **Perf note:** every V1 PR includes a perf note with tris, draw calls, mesh and texture memory, generation ms, and IPA delta, measured in the lead's Unity CLI pass. Estimates are labelled as estimates.

## 7. Test plan (EditMode, `Kestrel.Vehicle.Tests`)
Engine-free tests (Core, run under the dotnet runner too if added to `unity/dotnet`):
1. **Bell geometry:**
   - `BellProfile` for every catalog engine is monotonic in Y.
   - The radius goes chamber → throat (minimum) → exit, with `Re = diameter/2 ± 1e-4` and `Rt = Re/sqrt(ε) ± 1e-4`.
   - Length grows with ε at a fixed diameter.
2. **Distinctness:** for any two catalog engines with different (ε, cycle), the normalised profiles differ by ≥ 3% max radius deviation (catches cloned bells).
3. **Seed determinism and instancing:**
   - same seed → identical profile and part layout
   - a different serial changes per-instance roll and gimbal
   - all engines of a type within one vehicle share one mesh key
4. **Cycle placement:**
   - pressure-fed has 0 turbopumps
   - FF has 2
   - GG has exactly 1 exhaust duct
   - solids have no feed lines
5. **Attach points:**
   - ids equal `UmbilicalIds.Make` output and are unique
   - Stage 0 is the first stage
   - outward +Z, positions on the skin within ±2 cm
   - conditional purge and hydraulic follow the hardware rule
   - `UmbilicalTimeline.Validate(points, Build(points))` is empty for every `data/presets.json` template
6. **Budgets:**
   - the V1 tri and draw-call estimators stay under §6.2/6.3 caps per tier for the heaviest preset
   - `LodPolicy.Select` with hero bias keeps bells at LOD0 at the liftoff chase distance

Unity-runtime tests (EditMode, needs `UnityEngine`):
7. The mesh builder outputs non-empty meshes with matching vertex and normal counts and no NaN, with triangle counts matching the estimator ±5%.
8. Materials come only from `MaterialLibrary.Get`. The test asserts `ReferenceEquals` against the cache for every renderer and batch.
9. Generating the heaviest preset 10 times leaks no meshes (`Resources.FindObjectsOfTypeAll<Mesh>` count is stable after dispose).

Portrait screenshots (liftoff 02, ascent 03, iPad 05 framing per audit §2) are captured by Kestrel Arcade in the Unity CLI pass. Before/after goes in the PR, and QA promotes them to golden images.

## 8. Asks to Graphics Dev (kit changes, not forks)
1. **Vehicle `MaterialRole`s.** Today's roles are pad-only. V1 needs:
   - `BareMetalNozzle` (dark metallic, heat-tint capable)
   - `NiobiumLikeExtension` (generic dark radiative skirt, name TBD to stay generic)
   - `Ablative` (charcoal, rough)
   - `StainlessBright`
   - `AluminiumPainted` (for V2)

   Until they land, V1 maps to Galvanized, RubberHose and PaintedSteel stand-ins, and swaps over with no API change.
2. **Per-instance colour/float on `InstanceBatch`** (`MaterialPropertyBlock` array or a `RenderParams.matProps` instanced buffer) for wear and heat tint, without breaking batching.
3. **`InstanceBatch` matrix update** for a moving parent (a `SetAll(Matrix4x4 parent)` or per-frame rebuild API), plus **LOD-aware batches.** Today `Draw` only distance-culls, so V1 would hold one batch per LOD unless the kit adds LOD swapping.
4. **A vehicle budget in `KitBudgets`** (`VehicleTriangles(q)`, `VehicleDrawCalls(q)` from audit §3) so `BudgetLedger.Within` can check vehicle limits.
5. **Optional:** a `KitMesh.Lathe(profile, radialSegments)` helper. Bells, turbopumps, tanks and pad tanks all need it, and one shared lathe beats two.

## 9. V2–V6 sequencing (per audit §9; one small PR each, branched off main)
| Step | Audit slot | PRs (Art) | Gate |
|---|---|---|---|
| V2 | P0-2 | (a) tank welds + stringers in RocketMeshBuilder lathe; (b) isogrid normal map; (c) interstages open/solid; (d) fairing seams, latches, separation rail | Lead wires SliceApp. Isogrid geometry deferred to cutaway |
| V1 | P0-3 | (a) bell profiles + instancing + attach points (this spec); (b) turbopump/plumbing parts; (c) solid motor variant | **#86 merged** |
| V3 | P0-3 | raceways along the OML, umbilical plates for tank/upper-stage points into the same `VehicleUmbilicals` | #86 merged; pad D5 reads the same points |
| V4 | P0-4 | legs (two-bar linkage from the timeline deploy angle), grid fins (instanced cells, hinge axes); **cutaway + inspect (solid for 2.0)** | U6 G2/G3. Exploded and load-path only if P0-6 is on schedule |
| V5 | P0-5 | livery presets + 2 colours + fictional `KH-<class>-<nnn>` serial (SDF) + earned heritage marks; frost mask | R3 sky; NAME_REVIEW before store screenshots |
| V6 | P0-5 | timeline-driven element states via `MissionEvent.links.elements`; aging (reuse → soot/fade/scorch, kit wear model); cosmetic anomaly damage | Gameplay Dev's `MissionEvent.links` PR; Kit wear model |
| Lab / Hangar | P0-6 | Vehicle Detail Lab toggles; Hangar Reveal (see `docs/HANGAR_REVEAL_UNITY_PLAN.md`) | K5/K6 subscribers, UX screens |

The audit orders V2 before V1, because V2 deepens the lathe without needing the contract. Proposed order: V2a while #86 is in review, then V1a as soon as #86 merges. The lead decides.

## 10. Open questions for the lead
1. **Stage index in ids:** the contract makes the first stage `s0.*`, while the kickoff example says `s1.lox.0`. Follow the code (`s0`)?
2. **Assembly references:** Runtime references `Kestrel.Kit` + `Kestrel.Kit.Core` + Contracts, and Core references Contracts only (§1). Is that OK?
3. Is it OK to start V2a ahead of V1, per audit §9?
4. Unity CLI screenshots and perf numbers come from the lead's pass, as there's no Unity on the Art box.
