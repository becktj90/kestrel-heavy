# Designer: Art section (for docs/DESIGNER_SPEC.md)

Owner: Art (Kestrel Art). UX owns `docs/DESIGNER_SPEC.md` (PR #142) and merges this section in. §0 answers the six Art questions in #142, and where this section and #142 differ, #142 and §0 win. **Spec only, no code.**

Rules:
- Unity 6.3 URP, portrait only, $0, procedural only (meshes are generated, not shipped).
- No real vehicle designs, brands or markings, and nothing from Trevor's workplace.
- Built on Graphics Dev's kit (MaterialLibrary, KitMesh, LodPolicy, InstanceBatch) and the umbilical contract.
- Data numbers (mass, thrust, ΔV) belong to Gameplay Dev; UI screens belong to UX. Art supplies the look, the variants and the IDs.

Sketches (procedural PIL, fictional marks only), in `docs/designer/art/`:
- `01-part-catalogue.png`: part catalogue sheet
- `02-livery-branding-editor.png`: livery and branding editor mock (portrait 1170×2532)
- `03-exploded-view.png`: exploded view
- `04-insignia-primitives.png`: insignia primitive set and safeguard examples

## 0. Answers to UX's Art questions (#142)
1. **`data/finishes.json`: accepted** as `{ id, label, partTypes[], albedo, roughness, metallic, tint? }`.
   - `id` is a stable snake_case string, `label` is a localisation key, `albedo` and `tint` are `#RRGGBB`, and roughness and metallic are 0–1.
   - `partTypes` is a subset of `tank, engine, booster, interstage, fairing, legs, fins, gridfins, rcs`.
   - Any field outside the schema is a validation error.
   - **2.0 ships 12 finishes:**

     | id | Covers #142's list | Earned? |
     |---|---|---|
     | `bare_metal` | bare metal | start |
     | `white_paint` | white paint | start |
     | `cork_insulation` | cork/insulation | start |
     | `carbon_composite` | carbon | start |
     | `soot_weathered` | soot weathering, cosmetic | earned |
     | `foam_orange` | | start |
     | `brushed_stainless` | | earned |
     | `matte_black_ablative` | | earned |
     | `safety_yellow` | hardware yellow | start |
     | `heat_tint_copper` | heat tint near bells | earned |
     | `anodized_blue` | | earned |
     | `cryo_frost` | pad frost look | earned |

     Earned finishes unlock the same way as liveries.
2. **`data/liveries.json`: accepted** as `{ id, label, primary, accent, pattern, earnRule: { type: "start"|"orbits"|"landings"|"mission", count|missionId }, codexText }`.
   - I added `"start"` for the presets you own from the beginning.
   - There are no price, expiry, random, streak or time fields; the validator rejects any extra key.
   - Patterns: `solid`, `belt_stripes`, `split_vertical`, `checker_roll`, `chevron_nose`, `gradient_aft`, `two_tone_stage`.
   - **2.0 ships 16 liveries**, 4 of them `start`.
3. **Decals:** ids `decal_<name>` (§4), anchors `stageBand`, `interstage` and `fairing`.
   - **At most 6 per vehicle**, scale in 3 steps (0.5×, 1×, 1.5×), rotation 0 or 90.
   - Each decal is 256×256 in **one 2048×2048 atlas** (64 cells; 2.0 uses 24), ASTC 6×6, about 1.8 MB.
   - Medium and High render as URP decal projectors; Low uses baked UVs.
4. **Branding:**
   - **12 glyphs and 6 frames ship as SVG**, with a 64×64 viewBox, single path or a few paths, `currentColor` fill and no text.
   - At build time they rasterise to **256×256 SDF PNG**, R8 inside a 1024² atlas (about 0.25 MB). The SVGs stay as the source.
   - **16-colour palette confirmed** (A2 `palette.json`), plus the engineer hex field.
   - **Trademark sign-off:** Chief of Staff signs the review checklist in each A2 or branding PR, and **Trevor has the final say** before store screenshots. This also covers the NAME_REVIEW step.
5. **Swatches: rendered sprites.**
   - A 128×128 sprite per finish and livery is baked at build time from the same shader, then put in one atlas.
   - Reasons: 16+ live 3D swatches would cost extra draw calls and GPU while the 3D vehicle is already on screen, and they would scroll badly on SE.
   - The live 3D vehicle already previews the selected finish, so swatches only need to be recognisable.
   - Sprites also give VoiceOver a stable element per swatch.
6. **Camera split: fine.** Art owns the Hangar *reveal* camera and Graphics owns the *designer* `HangarCamera` (R1), **as long as both read the same framing constants** from `DesignFraming`: visible fraction, minimum 0.35, safe-area offsets and iPad X-shift. Neither camera defines its own.

Where this section had looser numbers (brand name 3–28 characters, 6-layer marks, streak unlocks), #142's values replace them: name 1–20 characters, mark = 1 glyph + 1 frame + colours, no streaks.

## 1. Principles
1. **Every part is a parametric builder, never a model.** The player edits parameters; a V1–V6 builder regenerates the part deterministically from `(design, serial, seed)`.
2. **Detail scales with the camera.** The designer is the closest camera in the game, so parts use LOD0 plus hero bias there. Flight uses the normal LodPolicy.
3. **Stable IDs.** Every part variant, material, finish, livery, decal slot and mark has a snake_case string ID that never changes after ship. IDs are what get saved and synced; display names are localised keys.
4. **Earned, never bought.** Liveries, finishes and marks unlock only through play. There is no IAP, currency purchase or ad unlock for cosmetics (see §5).

## 2. Part art list (variants, tweakable parameters, detail, builder)
Parameter ranges are visual. Where a parameter affects physics (for example diameter), Gameplay Dev validates it and owns the number.

### 2.1 Tank sections (`part.tank.*`): V2 TankShellBuilder (PR #106)
| Variant ID | Look | Player parameters | Detail (LOD0) |
|---|---|---|---|
| `tank_barrel` | welded cylinder | diameter, length, panel count (auto from length), weld visibility (subtle/strong) | welds, stiffener rings, isogrid normal, ≤ 14k tris |
| `tank_barrel_stringer` | external stringers | as above + stringer count 12–48 | instanced stringers (V2b) |
| `tank_common_bulkhead` | two propellants, one shell, belt seam | as above + split ratio | extra mid weld + vent band |
| `tank_sphere_pair` | short stage, two domes | diameter | two ellipsoidal domes, equator weld |
| `tank_tapered` | conical adapter section | top and bottom diameter, length | lathe frustum, seams follow the taper |

Ends are `end_flat_skirt` (default, flush) or `end_dome_inset`. Cutaway shows the real isogrid and internal bulkheads.

### 2.2 Engine types, nozzles and clusters (`part.engine.*`): V1 engine builder
Engine types come from `data/engines.json`, one per cycle, and each has a distinct look:
- `engine_gas_generator`
- `engine_staged_combustion`
- `engine_full_flow`
- `engine_expander`
- `engine_pressure_fed`
- `engine_solid`
- `engine_hybrid`

Nozzle variants (`nozzle.*`):
| Nozzle ID | Shape source | Parameters |
|---|---|---|
| `nozzle_bell_sea_level` | profile from expansion ratio (V1 spec §2) | none (catalog driven) + seeded per-serial variation |
| `nozzle_bell_vacuum` | long bell + radiative extension skirt with joint ring | extension length 0–100% (visual) |
| `nozzle_cone_solid` | f = 0.70 cone-bell blend, ablative exit band | none |
| `nozzle_bell_regen_banded` | dense regen tube banding | band density (low/med/high, cosmetic) |
| `nozzle_annular_plug` (1.1) | fictional plug/annular | — (deferred) |

Cluster layouts (`cluster.*`), instanced per engine type:
- `cluster_single`
- `cluster_ring_3`, `cluster_ring_4`, `cluster_ring_5`
- `cluster_7_hex`
- `cluster_9_octaweb_like`, a generic name: centre + 8 ring
- `cluster_ring_plus_centre_n` (up to 27)

Parameters: engine count, ring radius, centre engine on/off, engine-bay heat-shield style (`boot_flexible`, `boot_rigid`). Plumbing parts follow the V1 spec §3.

### 2.3 Boosters (`part.booster.*`): V2 tank + V1 engines + V4 attach hardware
| Variant | Parameters |
|---|---|
| `booster_liquid` | count 2/3/4/6, diameter, length, nose `nose_ogive`/`nose_cone`/`nose_flat_cap` |
| `booster_solid_segmented` | segment count 1–5, field-joint rings, aft skirt |
| `booster_solid_monolithic` | length, aft skirt flare |

Attach struts and separation thrusters are instanced kit beams.

### 2.4 Interstages (`part.interstage.*`): V2c
- `interstage_solid_skin`: a stringered skin with a vent row.
- `interstage_open_truss`: kit beams in an X or V truss, with the upper bell visible inside.
- `interstage_hot_staging_vents`: a perforated band; the vent count is seeded.

Parameters: length, taper, vent row on/off.

### 2.5 Fairings (`part.fairing.*`): V2d
Shapes:
- `fairing_ogive`
- `fairing_von_karman` (a generic named curve)
- `fairing_conic`
- `fairing_biconic`
- `fairing_hammerhead` (fairing wider than the stage)
- `fairing_capsule_tower` (escape-tower silhouette, fictional)

Parameters: diameter (≥ stage), length, nose bluntness, separation-seam count (2 or 3 halves), latch row, acoustic-panel normal pattern.

### 2.6 Landing legs (`part.legs.*`): V4, real linkage kinematics
- `legs_folding_a_frame` (stowed flush, deploys with a telescoping strut)
- `legs_tripod_fixed` (small vehicles)
- `legs_skirt_hinged` (stowed under the aft skirt)

Parameters: count 3/4/6, span, foot-pad size. The designer previews the deploy animation.

### 2.7 Fins and grid fins (`part.fin.*`): V4
- **Grid fins:** `gridfin_square_lattice` and `gridfin_diamond_lattice`. Cells are instanced. Parameters: cell count 4×4 to 10×10, frame thickness, hinge position.
- **Fins:** `fin_trapezoid`, `fin_clipped_delta` and `fin_swept`. Parameters: count 3/4, span, sweep, root chord.

### 2.8 RCS pods (`part.rcs.*`): V3 raceway family
- `rcs_quad_block`: 4-nozzle block.
- `rcs_cold_gas_pair`: two opposed nozzles.
- `rcs_flush_ports`: recessed ports.

Parameters: pods 2/3/4, ring height, nozzle count. Nozzles are instanced and puffs are VFX hooks.

### 2.9 Always present, auto-placed
Raceways, umbilical plates and attach points come from the contract (`IUmbilicalVehicle`) and are not player-editable. The player can only choose raceway side, at azimuths 90 or 270.

## 3. Materials and finishes (stable IDs)
Each material maps to a kit `MaterialRole`, or to a requested vehicle role marked †. A finish is a modifier on top of a material.

| material_id | Kit role | Use |
|---|---|---|
| `mat_painted_aluminium` | †AluminiumPainted (stand-in PaintedSteel) | tank skins, default |
| `mat_bare_stainless` | †StainlessBright | tank skins, mirror-ish |
| `mat_foam_insulation` | Insulation | cryo tanks (orange/tan tones, procedural cells) |
| `mat_cork_ablative` | †Ablative | interstage, aft skirt |
| `mat_bare_metal_nozzle` | †BareMetalNozzle | bells |
| `mat_dark_radiative_extension` | †DarkRadiative | vacuum extensions |
| `mat_carbon_composite` | †Composite (new) | fairings, interstages, fins |
| `mat_galvanized_hardware` | Galvanized | welds, brackets |
| `mat_rubber_seal` | RubberHose | boots, seals |
| `mat_safety_yellow` | SafetyYellowSteel | actuators, GSE accents |

| finish_id | Effect (shader params only, no new textures) |
|---|---|
| `finish_satin` | default smoothness |
| `finish_gloss` | +0.25 smoothness |
| `finish_matte` | −0.25 smoothness |
| `finish_brushed` | anisotropic-like stretched detail normal |
| `finish_heat_tinted` | heat gradient near bells (V6) |
| `finish_flight_worn` | soot, fade and scorch from reuse count (V6, earned) |
| `finish_frost_on_pad` | cryo frost mask (V5, pad state, not a choice) |

## 4. Decal slots
Decals sit at fixed **slots** on each part so they never collide with welds or plates. They go in one 2048² atlas. On Medium and High they render as URP decal projectors; on Low the decal UV is baked into the part.

| slot_id | Location | Allowed content |
|---|---|---|
| `slot_wordmark_vertical` | stage 1 barrel, azimuth 90 (camera side) | branding wordmark (vertical) |
| `slot_insignia_upper` | upper barrel or fairing | insignia |
| `slot_serial_aft` | aft skirt | fictional serial `KH-<class>-<nnn>` (SDF) |
| `slot_flag_band` | interstage band | player 2-colour stripe |
| `slot_mission_patch` | fairing | earned mission patch |
| `slot_heritage_marks` | booster nose or stage 1 aft | earned marks (tally of landings, milestones) |
| `slot_safety_stencils` | near plates and vents | generic stencils (auto: `NO STEP`, `VENT`, `LIFT POINT`) |
| `slot_booster_number` | each booster | auto 1..n |

Each slot has an ID, a part anchor, a max size in metres, an allowed-content list, and z-order. The validator rejects decals that overlap a weld or umbilical plate's keep-out.

## 5. Earned liveries (presets + 2 colours + earned marks)
Structure:
```json
{
  "livery_id": "livery_pathfinder_stripes",
  "name_key": "livery.pathfinder_stripes.name",
  "zones": ["zone_primary", "zone_secondary", "zone_band", "zone_nose", "zone_aft"],
  "pattern": "pattern_belt_stripes",
  "defaults": { "primary": "#E8ECEF", "secondary": "#1E3A5F" },
  "player_colours": 2,
  "earnRule": { "type": "orbits", "count": 1 },
  "allowed_finishes": ["finish_satin", "finish_gloss", "finish_matte"]
}
```
- **Patterns** (procedural masks, no textures): `pattern_solid`, `pattern_belt_stripes`, `pattern_split_vertical`, `pattern_checker_roll` (a generic roll-reference pattern), `pattern_chevron_nose`, `pattern_gradient_aft`, `pattern_two_tone_stage`.
- **Colours:** the player picks 2 from a 24-swatch palette plus a hex picker. The picker is clamped to a contrast minimum so marks stay legible.
- **Earned by play only.** Unlock types are `milestone` (first orbit, first landing, 10 reuses), `mission` (complete mission X at grade ≥ B), and `codex` (read entries). There are no streaks (#142).
  - There are no shop, currency, IAP or ad unlocks for any cosmetic.
  - Unlock state lives in save data. Gameplay Dev owns the flags; Art owns the ID list.
- **Marks** (`mark_*`) are earned heritage marks: `mark_landing_tally`, `mark_reuse_flame`, `mark_first_orbit_star`, `mark_100_flights_laurel`. They are auto-placed in `slot_heritage_marks`.
- **2.0 ships 16 presets**, 4 of them available at start.

## 6. Branding editor (original fictional brand)
The player makes their own space agency or company identity from primitives. Nothing is uploaded or imported: no image import and no free drawing in 2.0.

### 6.1 Data schema (`branding_v1`, template-based per #142)
```json
{
  "schema": "branding_v1",
  "name": "Tallowmere",
  "mark": { "glyph": "glyph_chevron_up", "frame": "frame_roundel", "glyphColour": 3, "frameColour": 1 },
  "primary": 1,
  "accent": 3,
  "serialClass": "M"
}
```
- `name` maps to `livery.name`, 1–20 characters, and goes through the offline profanity filter and the brand filter.
- Colours are indices 0–15 into `palette.json`. The engineer hex field is stored as `"#RRGGBB"` and has to pass the contrast check.
- Glyphs: 12 original shapes (A2).
- Frames: `frame_none`, `frame_roundel`, `frame_shield`, `frame_hex`, `frame_diamond`, `frame_banner`.
- The mark has no free-form layers in 2.0.

### 6.2 Safeguards against real brands
1. **Name filter:** the name is checked against a deny-list of real space agencies, companies, vehicles and well-known consumer brands, plus their common abbreviations. Matching is normalised, with leetspeak folded and Levenshtein ≤ 2 on short names. A match is blocked with a neutral message.
2. **Mark similarity:** the mark is rendered at 64², and a perceptual hash (dHash plus a colour histogram) is compared with a small set of reference silhouettes. The references are abstracted from well-known agency-style roundels, meatball and swoosh motifs, built as primitives and kept as hashes only, with no logos stored.
   - Above the similarity threshold, the editor asks the player to change at least 2 layers.
3. **Structural rules:** some layer combos are blocked. These are a filled circle with an orbit ellipse plus a red vector swoosh, and a stylised letter-shape combo spelling a deny-listed abbreviation.
4. **No text in marks:** words exist only in the wordmark, which goes through the name filter.
5. **Presets:** every preset brand and mark goes through NAME_REVIEW before store screenshots (audit rec 19).
6. **Sharing:** branding is local only in 2.0. If sharing comes later, it needs a report path and moderation (1.1).

### 6.3 Serials
The format is `KH-<class>-<nnn>`: the class is the player's `serial_class` letter (A–Z), and nnn increments per airframe from Fleet (Gameplay Dev). The serial is auto-placed in `slot_serial_aft` and cannot be edited, so no real formats can be imitated.

## 7. Exploded view (2.0 only if P0-6 is on schedule; otherwise cutaway only)
- Parts separate along +Y in stack order, with a gap of 0.25 × diameter. Boosters move radially out by 0.6 × diameter, and fairing halves split ±X. The animation is 0.6 s ease-out, and reduced motion makes it a cut.
- Thin cyan leader lines connect each part to a portrait-safe label (UX renders the labels). Engine bays open, and the turbopump and feed lines are visible at LOD0.
- The look matches the blueprint mode: a navy background with a faint grid. Parts keep their livery, and inactive systems desaturate to 40%.
- It shares the element graph (audit §6), so tapping a part opens its inspect card.

## 8. Perf and IPA
- **Designer budget (static camera, one vehicle):**

  | Tier | Triangles | Draw calls | Rebuild per parameter change |
  |---|---|---|---|
  | Low | ≤ 250k | ≤ 120 | ≤ 30 ms, part-level regenerate, cached by (part, params) |
  | High | ≤ 450k | ≤ 200 | ≤ 30 ms |

  The flight budget is unchanged (audit §3).
- **Regeneration is incremental.** Only the edited part rebuilds; meshes are shared and ref-counted (`SharedMeshes`). Slider drags preview at LOD1 and commit LOD0 on release.
- **Liveries, finishes and patterns** are shader parameters and procedural masks, so they add 0 texture bytes.
- **IPA:**
  - The decal atlas (2048² ASTC 6×6) is about 1.8 MB.
  - The 2 SDF fonts are about 0.4 MB.
  - The insignia shapes are vector data, under 20 KB.
  - The brand-safety hash table is under 10 KB, and the deny-list under 30 KB.
  - That totals about 2.3 MB, inside the pad+vehicle ≤ 4 MB allotment.
- **Saving:** a design plus its branding JSON is under 8 KB per vehicle.

## 9. Dependencies and open questions
- **UX:** editor screens, part picker, colour picker and exploded-view labels.
- **Gameplay Dev:** parameter validation, unlock flags, Fleet serial counter.
- **Graphics Dev:** vehicle material roles (†), decal projector support, Composite role.
- **QA:** golden images per preset livery.
- **Open:** does UX want parameter ranges in this doc or in `data/designer_parts.json` (Gameplay Dev path)? Art recommends the JSON, with this doc keeping the IDs and looks.
