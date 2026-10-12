# Pad Detail Grammar

Owner: Kestrel Graphics Dev. Status: design rules for D2–D7 and the D1 seeded pad generator.

These rules come from looking at user-provided artist reference (ledger rows R-T01..R-T03). They describe **what makes a pad read as a launch pad**. They do not describe any real facility. Every value below is a **range** the generator samples from a seed. No layout, dimension set or marking from a real site is reproduced, and no image is committed, embedded or traced.

Conventions: `H_v` = vehicle height (base to tip), `D_v` = core diameter, `R` = seeded RNG. Archetypes match D1: **RAIL** (rail/erector pad, no fixed tower), **TOWER** (fixed service tower), **HEAVY** (tower + strongback, multi-core). Phase = the D-step that builds or consumes it.

## 1. Lattice lightning masts (proportions and bracing rhythm)

Observed: two to four free-standing square lattice masts stand well clear of the vehicle, taller than it, with regularly spaced platforms and a slim tip spike or insulator mast on top.

| Parameter | Range | Notes |
|---|---|---|
| Mast count | RAIL 0–2, TOWER 2–3, HEAVY 3–4 | Even counts are placed symmetrically about the vehicle axis ±10° jitter |
| Height `H_m` | 1.15–1.6 × `H_v` (to the tip of the spike) | Must clear the vehicle tip so the catenary covers it |
| Base width `W_b` | `H_m` / 9 – `H_m` / 14 | Square section |
| Top width `W_t` | 0.35–0.6 × `W_b` | Linear taper. Optional straight (no taper) top 15–25 % |
| Bay height | 0.9–1.3 × local width | Gives near-square bays. Bays shrink with the taper |
| Bay count | 10–22 | Derived = `H_m` / mean bay height, rounded |
| Bracing | Single X per bay (default), K brace in the bottom 1–3 bays (p = 0.4) | Alternate X/diagonal is allowed on RAIL to keep it light |
| Platforms | Every 4–7 bays, cantilever 0.3–0.6 × local width on 1–2 faces | Thin grating plus a rail. Small davit arm p = 0.3 |
| Tip | Fibreglass/insulator spike 0.06–0.12 × `H_m`, ⌀ ≤ 0.15 × `W_t` | Pale grey/white, reads as a "pin" at distance |
| Standoff from vehicle axis | 1.0–1.8 × `H_v` / 2 horizontally | Must sit outside the plume keep-out (§6) |

Rendering: members are instanced from the Kit's `LatticeMember` mesh (D1 kit). At > 300 m, swap to an alpha-tested lattice billboard (LOD2). Galvanized grey or, for TOWER/HEAVY, p = 0.25 red/white aviation banding in 1/7 height bands (generic, no agency colours).
Phase: D1 builds the mast generator. D6 adds aviation lights on the tip and every third platform.

## 2. Catenary wire layout and insulators

Observed: wires run from mast tips over the vehicle, and down to ground anchors, forming a protective "tent". Insulator strings sit near the attach points.

- **Topology**: connect every mast tip to every other mast tip (ring) and, p = 0.6, add a cross-wire over the pad centre. Each tip also has 1–2 guy/drop wires to a ground anchor 0.6–1.1 × `H_m` outward.
- **Sag**: catenary with sag/span 0.03–0.07 (`y = a·cosh(x/a)`, solve `a` from the sag). The lowest point over the vehicle must stay ≥ 0.1 × `H_v` above the tip.
- **Insulators**: string of 4–9 discs (⌀ 0.25–0.4 m) at each tip end, and p = 0.5 at mid-guy. Ceramic white or dark brown.
- **Render**: 1-px-safe line renderer with width fade to 0.5 px at distance, never thicker than 2 px on portrait iPhone (VISUAL_SPEC scaling). No wire at LOD3.
- Archetypes: RAIL 0–1 wire (or none when masts = 0), TOWER ring, HEAVY ring + cross.
Phase: D1 (geometry stub), D7 (wind sway ±0.2 % of span, timeline-driven).

## 3. Risers, lines and grouping around the mount

Observed: propellant, gas and electrical risers come up in **separate clusters on different sides of the mount**, oxidizer kept apart from fuel. Long horizontal runs leave the pad toward distant storage.

| Rule | Value |
|---|---|
| Riser clusters | 3–6 around the mount, at bearings spaced ≥ 60° |
| Oxidizer vs fuel separation | Opposite sides (bearing difference 150–210°). Never in the same cluster |
| Cluster contents | 2–5 pipes, ⌀ 0.15–0.6 m, plus 1 cable tray. Cryogenic lines are thicker with pale insulation jacket, and frost tint below 0.3 × `H_v` during fueling |
| Line classes (colour band only, generic) | Cryo oxidizer (pale blue band), cryo fuel (pale/white jacket), kerosene-type fuel (dark), high-pressure gas (yellow band), electrical (grey tray), water (green band) |
| Horizontal runs | Leave the mount along 1–3 corridors, on low pipe racks 0.5–1.5 m high, bends at 45/90° only, expansion loops every 60–120 m |
| Valves | Every 25–60 m along a run, plus 1–2 per riser near the deck. Handwheel or actuator box (Kit `Valve` prefab) |
| Stairs and service | 1 stair tower or ladder per 2 clusters |

Archetypes: RAIL is minimal (2–3 clusters, short runs), TOWER is standard (risers climb the tower to the arms), HEAVY gets duplicate clusters per core (side boosters get their own gas cluster).
Phase: D2 (routed piping and valves), D5 (umbilicals take over at the arm, per the umbilical contract).

## 4. Spherical tanks (placement and standoff)

Observed: large spherical tanks (cryogenic storage) stand far from the vehicle, often low on the skyline behind the pad, with walkways on top.

- Count: RAIL 1, TOWER 1–2, HEAVY 2 (one oxidizer, one fuel, on opposite sides, consistent with §3 bearings).
- Diameter: 0.12–0.25 × `H_v`, set on a skirt or 6–12 legs at 0.1–0.2 × diameter height.
- Standoff: 2.5–5 × `H_v` from the vehicle axis. Fuel and oxidizer tanks ≥ 4 × diameter apart. Never inside the plume sector (§6).
- Detail: top platform with rail, one stair spiral or caged ladder, vent stack, low bund wall 1–1.5 m around it at 1.5–2 × diameter. White/off-white with weathering streaks (D7 wear).
Phase: D3 (tank farms). Far-field LOD as an impostor sphere beyond 600 m.

## 5. Water tower

Observed: an elevated water tank on a braced frame or single column, for deluge supply, is a strong pad silhouette cue.

- Height to tank base: 0.4–0.7 × `H_v`. Tank: sphere, capsule or ellipsoid, ⌀ 0.1–0.18 × `H_v`.
- Support: 4–6 legs with X bracing every 3–5 bays and a central riser column (p = 0.6).
- Standoff: 1.5–3 × `H_v`, on the side away from the trench exit.
- A large-bore feed line (⌀ 1–2 m equivalent) runs to the deluge header (§6).
- Archetypes: RAIL p = 0.5, TOWER 1, HEAVY 1 (p = 0.3 for a second).
Phase: D4 (deluge with rain-bird nozzles) consumes it as the water source. D6 for utilities.

## 6. Trench-to-deflector geometry

Observed: the exhaust goes down through the mount into a trench under the deck, then is turned sideways by a wedge-shaped deflector that runs out to open ground. Ponds or catch basins sit off the trench ends.

| Parameter | Range |
|---|---|
| Trench width | 2.5–4 × the engine cluster diameter |
| Trench depth below deck | 0.12–0.25 × `H_v` |
| Deflector | Single wedge (exits both ways) or single-sided ramp (exits one way). Face angle 30–45° from horizontal, apex under the cluster centre |
| Trench exit length | 1–2.5 × `H_v`, flaring 10–20° toward the open end |
| Plume keep-out sector | ±25° around each exit bearing out to 3 × `H_v`. No masts, tanks or towers inside it |
| Ponds / catch basins | 1–2 shallow rectangular basins, 0.3–0.6 × `H_v` wide, at the trench exits or off to the side |
| Deluge | Rain-bird nozzles on the deck rim, 6–16 heads, spray cone 20–40° |
| Spare deflector | p = 0.3, parked on rails beside the trench (a strong "clutter" cue) |
| Slide wire / escape | TOWER/HEAVY: 1–3 wires from the upper tower down to a bunker 1.5–3 × `H_v` away, angle 10–20°, pointing away from the plume sector |

Archetypes: RAIL uses a shallow open trench or above-ground deflector. TOWER uses a wedge. HEAVY uses a wedge and a wider trench to cover the booster spread.
Phase: D4 (trench, deflector, deluge). The ground-cloud plume (R track) spawns at the trench exits and grows along the exit bearing.

## 7. Pad clutter density by distance

Observed: detail gets denser closer to the mount. Far away it is sparse and low: roads, fences, sheds, light poles.

| Ring (from axis) | Density | Content |
|---|---|---|
| 0–0.5 × `H_v` (deck) | High: 1 prop per 15–40 m² | Valves, junction boxes, hose reels, gas bottles, handrails, small cabinets, cable runs |
| 0.5–1.5 × `H_v` (apron) | Medium: 1 per 80–200 m² | Pipe racks, stair towers, utility sheds, light poles every 30–60 m, fences |
| 1.5–4 × `H_v` (support) | Low: 1 per 400–1000 m² | Tanks (§4), water tower (§5), small buildings, vehicles, antennas, camera sites |
| > 4 × `H_v` (perimeter) | Sparse | Perimeter road, fence line, 1–3 isolated poles/masts. Vegetation fill |

- Props are instanced (Kit `PropScatter`, Poisson disk with seed). Placement rejects the plume sector and line corridors.
- Perf: deck props cull at 150 m, apron at 400 m. Total clutter ≤ 6 draw calls per ring (GPU instancing). Far-field props are cut first if the budget is tight.
Phase: D6 (lighting layout: light poles in rings 2–3), D7 (props, wear, damage).

## Mapping summary

| Rule | RAIL | TOWER | HEAVY | Phase |
|---|---|---|---|---|
| §1 Masts | 0–2 light | 2–3 | 3–4 | D1, D6 |
| §2 Catenary | 0–1 wire | Ring | Ring + cross | D1, D7 |
| §3 Risers | 2–3 clusters | 3–5 | 4–6 per-core | D2, D5 |
| §4 Spherical tanks | 1 | 1–2 | 2 | D3 |
| §5 Water tower | p 0.5 | 1 | 1–2 | D4, D6 |
| §6 Trench/deflector | Shallow/ramp | Wedge | Wide wedge | D4 |
| §7 Clutter | Sparse | Standard | Dense | D6, D7 |

Guardrails: no real facility names, agency names, logos or flags anywhere in generated content. Colour bands are generic. If a seed produces a layout that resembles a specific real pad, re-roll the seed (the generator should vary bearings and mast counts enough that this stays rare).
