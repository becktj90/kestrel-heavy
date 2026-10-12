# Pad + Launch Vehicle Detail audit (D1–D7, V1–V7)

Status: **audit only**, no feature code. Brief: Trevor via Chief of Staff, 2026-10-12. Supersedes the "rocket-detail pass" note: V1–V7 replaces it. D and V are **one sequenced P0 track for 2.0.0**, ahead of the lower-priority R/S items. Unity 6.3 URP, portrait-only, $0, procedural only, no real-facility or real-vehicle imagery/designs/markings, no real trademarks, nothing from Trevor's workplace. References: text knowledge + public-domain US-government imagery only, logged in `docs/REFERENCE_LEDGER.md`.

Read for this audit: `unity/` (Kestrel.Slice runtime, Kestrel.Core Vehicle/Timeline), UNITY_SHIP_PLAN.md (U6 = G2/G3/G6 pad generator), MASTER_ROADMAP.md (incl. R/S), VISUAL_SPEC.md, MISSION_EVENTS.md, ENVIRONMENT_REALISM_AUDIT.md, SPACE_AUDIT.md.

## 1. Current state (main @ e326f0d)

| Area | What exists | Where | Gap |
|---|---|---|---|
| Pad geometry | `Plane` ground, `Cylinder` pad 30×0.5×30, `Cube` tower 4×90×4 at x=−14. Unity primitives, ~3 draw calls. | `SliceApp.cs:41-50` | No structure, no members, no plumbing. #81 merged `PadTower` (`Kestrel.Slice/Runtime/PadTower.cs`) replacing the tower cube; D1 rebuilds its internals behind the same `PadTower` entry point, not a fork. |
| Pad generator ("G") | **Not implemented in Unity.** Spec only: U6 "seeded pad generator" (G2/G3/G6). Logic stub `Pad.For(Catalog, Design)` returns hardware flags (`crewArms, strongback, umbilical, deluge, internalPower`, id `pad.tower`/`pad.rail`). | `Kestrel.Core/Timeline/PadMission.cs:8-45`, UNITY_SHIP_PLAN §U6 | D deepens this: `Pad` becomes the generator's output root; no second pad model. |
| Rocket geometry | Lathe (surface-of-revolution) mesh, 32 segments, from the same core math as physics (`DesignMath.SizeTank`, catalog exit diameters). Stages + nested upper bells + fairing ogive. Single mesh, single material. | `RocketMeshBuilder.cs` (137 lines) | No interstage detail, cloned identical bells, no plumbing, welds, stringers, raceways, legs, grid fins, decals, frost. |
| Materials | `Resources/Materials/KH_Body` (instanced per object with colour override: ground, pad, livery primary), `KH_Plume` + generated soft sprite (#79). | `SliceApp.cs:24-25,43,125` | Per-object `new Material()` breaks SRP Batcher grouping; no PBR maps, no masks, no LOD. |
| Hangar Reveal | U8 scope (3D, Unity). Not built. | UNITY_SHIP_PLAN U8 | V renderer feeds it directly. |
| Timeline | One `MissionEvent` schema (`links`, `status`, `actual`), `Pad` hardware-existence rule (no event for absent hardware). | `MissionEvent.cs`, MISSION_EVENTS.md §Pad | Needs element-id links for D/V animation (§6). |
| Art assets | Only the app icon. | `unity/Assets/Art/Icon` | Everything below is procedural. |

## 2. Comparison screenshots (current)
Baseline portrait captures for before/after (not committed; on the shared box):
- v2: `/workspace/kh-shots-unity/v2/01-design-iphone-1290x2796.png`, `02-liftoff-iphone-…`, `03-ascent-iphone-…`, `04-maxq-T46-iphone-…`, `05-flight-ipad13-2064x2752.png`.
- v3: `/workspace/kh-shots-unity/v3/` (iPhone / Pro Max / iPad 13 after #79/#80/#81; being produced now). Each D/V milestone re-shoots the same 02/03/05 framing for side-by-side diffs, and QA promotes them to `tests/golden-images/`.
Observed in v2: the pad reads as a flat disc + grey box; the vehicle reads as a smooth single-colour lathe with identical bells and no surface breakup at any distance.

## 3. Budgets per tier (pad + vehicle share)
Tiers match ENVIRONMENT_REALISM_AUDIT §2. Pad+vehicle get the slice below; the rest stays with environment/sky/plume/UI.

| Tier | Frame | Pad+vehicle GPU | Visible tris (pad+vehicle) | Draw calls (SRP-batched) | Texture mem | Mesh mem |
|---|---|---|---|---|---|---|
| Low (A13–A14) | 16.7 ms @60 | ≤ 3.0 ms | ≤ 250 k (pad 150 k, vehicle 100 k) | ≤ 120 | ≤ 24 MB | ≤ 20 MB |
| Medium | 16.7 ms @60 | ≤ 3.5 ms | ≤ 600 k (350 k / 250 k) | ≤ 200 | ≤ 48 MB | ≤ 40 MB |
| High @120 | 8.3 ms | ≤ 2.0 ms @120 / 4.0 @60 | ≤ 900 k (500 k / 400 k) | ≤ 260 | ≤ 80 MB | ≤ 64 MB |

IPA: pad+vehicle shipped bytes **≤ 4 MB compressed** (trim sheet atlases, decal atlas, SDF font for serials), separate from env ≤ 8 MB and space ≤ 8 MB, all inside the 40 MB cap. Everything else is generated at load from seeds (meshes are never shipped). Generation budget: pad ≤ 150 ms, vehicle ≤ 60 ms on Low, off the main thread where possible (Burst jobs), cached per (seed, design hash).

## 4. Shared kit: materials, LOD, instancing (`Kestrel.Kit`)
One kit for D and V; Graphics Dev owns it.
- **Materials:** one URP Lit-based `KH_Kit` shader variant set (SRP Batcher compatible, no per-object `new Material`). Per-instance variation via `MaterialPropertyBlock`-free approach: per-vertex colour + UV2 index into a 256-entry **material palette** (steel galvanised, painted steel, stainless, aluminium, insulated foam, cork/ablative, concrete, refractory, rubber, glass, emissive). Masks: tiling trim sheet (bolts, seams, grating, panel lines) 2048² ASTC 6×6; detail normal 512²; grime/wear computed from AO + world-height + a wear param (D6/V6).
- **Decals:** URP Decal Projector (Medium/High) or baked atlas UV (Low), one 2048² atlas for livery, stencils, fictional serials (SDF-rendered at runtime).
- **LOD:** LOD0/1/2 + impostor per generated part family, screen-size thresholds 0.25/0.08/0.02; cross-fade off on Low. Vehicle LOD swaps on chase-camera distance; thin parts (railings, cables, stringers) collapse into normal-map detail at LOD1.
- **Instancing:** members, valves, nozzles, lights, bolts are `Graphics.RenderMeshInstanced`/BatchRendererGroup instances of ~40 canonical meshes; GPU-resident instance buffers per kit family.
- **Static batching** for generated pad after generation; vehicle stays dynamic (one mesh per stage, merged per material).
- **Culling:** pad split into ~16 m cells; portrait frustum + distance cull.

## 5. Techniques per item

### D: Launch Pad Detail
| Item | Technique | Tier notes |
|---|---|---|
| D1 Structural steel members | Generator emits a member graph (nodes, members with section type W/HSS/angle, bracing); members are instanced extruded profiles, gusset/bolt detail from trim sheet. Deepens G's tower/strongback. | Low: lattice merged, bracing in normal map. |
| D2 Piping & valves, tank farms | Pipe router: Manhattan routing on a pad grid from tank farm → tower → umbilical, bends via swept tubes; valves/flanges/supports instanced. Spherical/cylindrical tanks (LOX/fuel/water/GN2) as lathes from kit. | Low: pipe radius < 0.15 m drops to LOD1 strips. |
| D3 Deluge + rain-bird nozzles, flame trench & deflector | Trench/deflector as CSG-free lofted profiles in concrete/refractory; rain-bird nozzles instanced on pad deck ring; water as GPU particle + sheet mesh driven by deluge event. | Water flow particle caps per tier; reduced-flash N/A. |
| D4 Cooling towers & utilities | Hyperbolic lathe towers, transformer yards, cable trays from kit. Mid/far only. | Impostors beyond 300 m. |
| D5 Sagging hoses & umbilicals | Catenary solved analytically (cosh) per hose; skinned spline mesh; release/retract animated per contract (§7). | Verlet sway High only. |
| D6 Lighting layout & site props; wear and damage | Light masts with baked emissive + few real lights (≤ 4 per tier); props scattered by seeded Poisson. Wear: procedural rust/soot/scorch masks from distance to plume axis and launch count; damage = mesh-free decal + param after anomaly. | Real-time shadows only from sun. |
| D7 Element graph + inspect mode, timeline animation, Detail Lab, Pad Walk | See §6. Pad Walk = first-person-ish portrait orbit/dolly camera on rails. Detail Lab = toggles kit layers, tiers, wear. | Pad Walk High/Medium; Low gets orbit only. |

### V: Launch Vehicle Detail
| Item | Technique | Tier notes |
|---|---|---|
| V1 Engines & engine bays | Bell profiles generated from catalog expansion ratio (distinct per engine type, plus per-engine seeded variation in tube/regenerative-cooling detail, gimbal offset). Turbopump, gas generator/preburner, feed lines as kit parts placed by cycle type. Solid motors: segmented casing, joints, nozzle, aft skirt. No cloned bells. | Low: bay plumbing LOD1 merged. |
| V2 Tank shells, interstages, fairings | Extend `RocketMeshBuilder` lathe: welds (ring + longitudinal geometry seams), stiffeners/stringers (instanced), isogrid as triangle-grid normal/parallax (geometry only in cutaway). Interstages with open/solid variants. Fairings: seam lines, latches, separation rail, acoustic panel. | Isogrid geometry only in V4 cutaway. |
| V3 Raceways & umbilical plates | Swept raceways along tank OML; plates at contract positions (§7). | — |
| V4 Legs & grid fins with real kinematics | Two-bar/strut linkage solved per frame (deploy angle from timeline); grid fins as instanced cells, hinge axes. Cutaway, exploded, load-path and inspect views share the element graph. | Exploded/cutaway are separate camera modes, static. |
| V5 Livery & decals | Livery rules (zones, stripes, heritage marks) + fictional serial generator (`KH-<class>-<nnn>`, no real formats/marks); SDF text. Frost: procedural mask on cryo tanks from propellant temp + time since load; sheds on liftoff. | Frost via shader only. |
| V6 Timeline states & aging | Element states driven by `MissionEvent` (§6); aging = reuse count → soot, paint fade, scorch (same wear model as D6). | — |
| V7 Vehicle Data Sheet, compare, image export; Fleet/Reuse tracker; Vehicle Detail Lab | Data Sheet auto-generated from Core (mass, ΔV, TWR, engines, dims) + rendered orthographic plates; compare = two designs side-by-side; PNG export via ScreenCapture to Photos share sheet (no new permissions beyond share). Fleet: per-airframe serial + flight history in save data. Lab mirrors Detail Lab. | Export resolution capped per tier. |

Deepens the existing renderer (RocketMeshBuilder) and feeds Hangar Reveal (U8); no second vehicle renderer.

## 6. Element graph + inspect data model; timeline-driven animation
Core (pure C#, no UnityEngine, in `Kestrel.Core.Elements`):
```text
Element { id: "pad.tower.arm.crew" | "veh.s1.eng3.tpump", kind, parent, tags[], system ("structure","prop","deluge","power","avionics"),
          material: paletteIndex, params{}, states[], inspect{ titleKey, factKeys[], codexLink } }
ElementGraph { root, elements[], links[] (pipe A→B, load path, umbilical mates), seed, version }
ElementState { id, value (0..1 or enum), t }   // e.g. arm angle, valve open, frost amount
```
- Generated deterministically from (pad seed, Design); golden graphs per template (Physics/QA).
- Inspect mode: tap → raycast to instance id → element id → card with title, facts, system highlight (pipe trace via links). Strings localised; no real-facility names.
- **Timeline:** reuse `MissionEvent`. Add `links.elements: string[]` (element ids) and `links.anim: string` (named state curve). An `ElementAnimator` subscriber (K6 pattern) maps event status changes (`pending→active→done`, `actual`) to `ElementState` targets. Hardware-existence rule is enforced by the graph: an event links only elements that exist. Holds/recycle/scrub replay states backwards (K7 scrub). No parallel schema (R14).

## 7. Shared pad–vehicle umbilical contract (`Kestrel.Core.Elements.Umbilical`)
Single source used by D5 (pad side), V3 (vehicle side), the timeline and Pad.For:
```text
UmbilicalPort { id: "s1.aft.fill" | "s2.fwd.lox" | "s1.elec" | "crew.access",
                stage, station: height from stage base (m), azimuth (deg, 0 = tower side),
                service: LOX|FUEL|LH2|GN2|ELEC|DATA|ECS|CREW,
                plateSize (m), disconnect: "T-0 liftoff" | "pre-launch retract",
                releaseEvent: MissionEvent id, retractEvent: MissionEvent id }
UmbilicalContract { vehicleDesignHash, ports[] }  // emitted by Core from Design
```
Rules: vehicle generator places plates at `ports`; pad generator routes swing arms/tail service masts to the same station/azimuth and orients the tower to azimuth 0; tower height ≥ max(station)+margin. Mismatch is a Core validation error (designer shows it). Events `umbilical.release.*`/`arm.retract.*` come from ports, so the timeline, pad and vehicle can never disagree.

## 8. Owners (D/V map)
| Item | Owner | Paths |
|---|---|---|
| D1–D7, shared kit, umbilical contract | Graphics Dev | `Pad/**`, `Kit/**`, `Environment/**` |
| V1–V6 | Art | `Vehicle/**`, `Art/Vehicle/**`, `Livery/**` |
| V7 Fleet + Data Sheet numbers, element graph core | Gameplay Dev | `Kestrel.Core/**` (except Astro), `data/**` |
| V7 Data Sheet / Fleet / inspect / Photo Mode screens | UX | `UI/Screens/**` |
| Validation suites, PAD_/VEHICLE_ENGINEERING_RULES.md | Physics | `docs/` |
| Pad/vehicle sound anchors (deluge, vents, latches) | Sound | `Audio/**` |
| Detail Labs Codex content | Codex | `Codex/**` |
| Golden images, CI, regression | QA | `.github/workflows/**`, `tests/golden-images/**` |
| Integration, merge queue, sole Unity CLI driver | Kestrel Arcade | — |
Contract file `Umbilical.cs` lives in Core (Gameplay Dev path) but changes need Graphics Dev + Art review.

## 9. Sequencing (one P0 track, no path collisions)
| Step | D | V | Gate / dependency | Collision notes |
|---|---|---|---|---|
| P0-1 | Kit + material palette + LOD rules; contract spec; ENGINEERING_RULES docs | — | U2 done | Kit only in `Kit/**`. |
| P0-2 | D1 members (deepens G tower; rebuilds merged #81 PadTower internals) | V2 shells/welds/stringers (deepens RocketMeshBuilder) | U4 render | SliceApp.cs: only Kestrel Arcade wires new builders, one PR per lane. |
| P0-3 | D2 piping/tanks + D5 umbilicals | V1 engines + V3 raceways/plates | contract merged | Both read `Umbilical.cs`; neither edits it. |
| P0-4 | D3 deluge/trench, D4 utilities | V4 legs/fins | U6 G2/G3 seeded generator | Water particles share plume budget with R6 → joint perf test. |
| P0-5 | D6 lighting/props/wear | V5 livery/decals/frost, V6 aging | R3 sky lighting for exposure | Wear model shared, owned by Kit. |
| P0-6 | D7 graph/inspect/animation, Detail Lab, Pad Walk | V7 Data Sheet, Fleet, Vehicle Lab | K5/K6 subscribers | `MissionEvent.links` additions: one Core PR (Gameplay Dev). |
Ordering vs others: U1–U3 unaffected; D/V P0-1…P0-3 run in U4, P0-4…P0-6 in U6/U8 (Hangar Reveal consumes V). K: needs K6 subscriber pattern. R: R2/S1 done; R3 sky before P0-5; R4 site terrain hosts the pad (pad footprint reserved, R4 does not place structures); R6 clouds/weather and S4–S5 slide after D/V P0-3 when they contend for Graphics Dev. S2/S3 (Gameplay Dev) run in parallel. Perf gate at end of P0-3 and P0-6 on the Low floor device.

## 10. Scope questions (with recommendations)
1. **Pad generator (G) first?** G2/G3 seeded generator isn't built yet. *Rec:* build G's seeded layout as D's first PR (P0-2) rather than as a separate U6 item, so there is one generator.
2. **#81 PadTower:** *Resolved:* #81 is merged; D1 replaces its mesh behind the same `PadTower` entry point.
3. **Number of pad archetypes in 2.0:** *Rec:* 3 seeded archetypes (rail pad for small solids, tower pad, tower+strongback heavy pad), seed varies layout; more in 1.1.
4. **Tank farm and cooling towers in 2.0?** *Rec:* yes, but D4 utilities/cooling towers as mid/far impostor-quality only.
5. **Pad Walk in 2.0?** *Rec:* yes as a rail-guided portrait camera tour (Medium/High); Low gets orbit inspect only. Free-roam walk is 1.1.
6. **Deluge water fidelity:** *Rec:* particle + sheet mesh with timeline-driven flow; no fluid sim.
7. **Isogrid as geometry?** *Rec:* normal-map in normal views, real geometry only in cutaway (V4) and Detail Lab close-ups.
8. **Distinct bells:** *Rec:* bell shape from catalog expansion ratio/cycle per engine type, plus small seeded per-serial variation; cluster engines of the same type share a mesh but vary in gimbal and wear (keeps instancing).
9. **Legs/grid-fin kinematics depth:** *Rec:* visual linkage kinematics driven by the timeline; no physics effect beyond what Core recovery already models.
10. **Cutaway/exploded/load-path in 2.0?** *Rec:* cutaway + inspect in 2.0; exploded and load-path views in 2.0 only if P0-6 is on schedule, else 1.1.
11. **Livery editor scope:** *Rec:* 2.0 = preset liveries + 2 colours + fictional serial + heritage marks earned from milestones; free-form decal painting 1.1.
12. **Data Sheet image export:** *Rec:* yes, PNG via iOS share sheet only (no Photos write permission); no watermark beyond the fictional serial and game name.
13. **Fleet/Reuse tracker in 2.0?** *Rec:* yes, minimal (serial, flights, landings, wear level) stored locally in save data; history charts 1.1.
14. **Damage after anomalies:** *Rec:* cosmetic only (scorch, missing panels decal, bent hand-rail); no destruction physics. Reduced-flash rules apply to any burn FX.
15. **Budget split:** *Rec:* accept §3 (pad+vehicle ≤ 4 MB shipped, ≤ 250 k tris on Low), taken from the 40 MB cap without reducing env/space 8 MB each.
16. **Umbilical contract ownership:** *Rec:* `Umbilical.cs` in Core, Gameplay Dev path, mandatory Graphics Dev + Art review; any port-schema change is a DECISIONS entry.
17. **P0 priority vs R/S:** *Rec:* D/V P0 ahead of R4 extra detail, R6 High volumetrics and S5 eclipse visuals for Graphics Dev; S2/S3 and R5 continue in parallel (other owners).
18. **Reference ledger:** *Rec:* `docs/REFERENCE_LEDGER.md` with source, licence (US-Gov PD or "text knowledge"), what it informed, and a "no direct copy" attestation per entry; reviewers reject any PR whose geometry is traceable to one specific real pad or vehicle.
19. **Trademark/marking check:** *Rec:* serial format, heritage marks and liveries go through the NAME_REVIEW process before 2.0 store screenshots.

Gates unchanged: Trevor keeps spend, signing and App Store Connect.

## 11. Approved scope (DECISIONS #46–#49)
Chief of Staff approved all 19 recommendations in §10 (Q2 already resolved by #81's merge), with these notes:
- **Umbilical contract (Q16):** Graphics Dev owns the contract doc and the visual side; Gameplay Dev owns the Core type `Umbilical.cs`. Any schema change needs review from Graphics Dev, Gameplay Dev and Art, plus a DECISIONS entry.
- **Must be solid in 2.0 (Q10/Q13):** cutaway view, inspect mode and the Fleet/Reuse tracker (serial, flights, landings, wear level). These are release-blocking for 2.0, not stretch.
- **Budget cuts (Q15):** if pad+vehicle exceed §3, cut far-field props first (site props, D4 utilities/cooling towers, distant lighting masts, impostor density), then mid-field. Close-up hero detail on the pad (tower, arms, umbilicals, deluge/trench) and the rocket is cut last.

### Deferred to 1.1 (honest list)
| Item | 2.0 | 1.1 |
|---|---|---|
| Exploded and load-path views | Only if P0-6 is on schedule; not promised | Committed |
| Fleet tracker | Serial, flights, landings, wear | History charts |
| Livery | Presets, 2 colours, fictional serial, earned heritage marks | Free-form decal painting |
| Pad Walk | Rail-guided tour (Medium/High); Low = orbit inspect | Free-roam walk |
| Pad archetypes | 3 seeded archetypes | More archetypes |
| Photo Mode | Not in 2.0 (G7) | Photo Mode incl. pad/vehicle and space shots |
