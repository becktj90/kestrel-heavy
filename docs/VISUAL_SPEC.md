# Kestrel Heavy — Visual Spec (engine-neutral)

Status: draft for review · Scope: procedural vehicle renderer, plumes, sky, portrait scaling.
Consumers: the Phaser 4 web build and any future Unity port. No engine API appears in the rules; every
rule is a pure function of the design JSON, flight state, and viewport. Brand: **Kestrel Heavy**;
missions are named **KH-n**; the recovery barge is **Haven**.

Sources of truth (all values below are quoted from these files on `main` @ 599e926):

| Topic | File |
|---|---|
| Design schema, defaults, tank sizing, nose shapes | `web/js/vehicle/design.js` |
| Engine catalog (`expansionRatio`, `diameter`, `thrustSL/Vac`, `ispSL/Vac`, `throttle`) | `web/js/data/gen/engines.js` (loaded by `web/js/data/engines.js`) |
| Propellant density / storability | `web/js/data/propellants.js` |
| Example designs | `web/js/data/templates/*.json` |
| Current sprite renderer (fixed 92×268 canvas) | `web/js/textures.js` (`makeRocket`, `drawEngineBells`, `makeUpperStage`, `makeFairingHalf`) |
| Current plume emitter | `web/js/mission.js` (`this.plume` config, `emitPlume`) — reference only, not modified |
| Sky palettes and atmo→vacuum blend | `web/js/tod.js` (`TOD`, `spaceBlend`, `skyLayerAlphas`) |
| Design world and stage scale | `web/js/config.js` (`W=1280`, `H=720`), `web/js/stage.js`, `web/js/main.js` |
| Viewport / safe areas | `web/js/fullscreen.js`, `web/index.html` (`env(safe-area-inset-*)`) |

Anything marked **[NEW]** is a rule this spec introduces (no repo value exists yet); everything else is
derived from the files above.

---

## 1. Units and coordinate system

* Design JSON is SI metres (`design.js`: "Units: SI (kg, m)").
* Render space is **design units (du)**: the 1280×720 world from `config.js`. Physics, camera and
  gameplay zoom stay in du (`main.js`, `stage.js`). Device resolution is handled only by stage scale `k` (§5).
* Vehicle local frame: origin at the centreline of the **lowest nozzle exit plane**, +y up (engine
  ports flip y if their screen space is y-down, e.g. Phaser).
* **Metres → du [NEW]:** `pxPerM = clamp(H_vehicle_target / L_total, 0.6, 6)` with
  `H_vehicle_target = 230 du`. 230 du matches the current fixed sprite's drawn height (body art spans
  y≈0–246 in the 268-px canvas in `makeRocket`), so a procedurally-generated vehicle of any length
  occupies the same screen budget as today's art. Diameters use the **same** `pxPerM` (never scale
  axes independently), then clamp each stage's drawn width to `≥ 10 du` for legibility.

## 2. Geometry from the design JSON

### 2.1 Inputs (schema, `design.js`)

| Field | Range / default | Used for |
|---|---|---|
| `stages[]` (1–`MAX_STAGES`=5) | bottom→top order | stage cylinders |
| `stages[i].diameter` | 0.3–15 m, default 2.0 | cylinder width |
| `stages[i].propMass`, `.propellant` | default 20000 kg kerolox | tank length via `sizeTank` |
| `stages[i].engine`, `.engineCount` | 1–40, default 1 | nozzle cluster |
| `stages[i].interstageMass` | ≥0 | interstage band visible if >0 |
| `strapOns.count` | 0, 2, 4 | side boosters |
| `strapOns.diameter`, `.engine`, `.engineCount`, `.propMass` | default 1.6 m, `srm-small` | booster geometry |
| `fairing.diameter`, `.length` | > 0, default 2.0 m × 5 m | payload fairing |
| `aero.nose` | `ogive` 0.30, `conic` 0.36, `blunt` 0.50, `von-karman` 0.28 (`NOSE_SHAPES`) | nose profile |
| `aero.fins`, `landing.legs/gridFins/rcs` | booleans | appendages |
| `livery.primary/accent` | default `#f2f2f2` / `#1a3c6e` | paint |

### 2.2 Stage length

Use the repo function verbatim (do not re-derive):

```
volume = propMass / bulkDensity * (1 + 0.03)            // ULLAGE = 0.03
length = volume / (π (d/2)²) + (solid ? 0 : 0.33 * d)   // DOME_LENGTH_FACTOR = 0.33
```

Drawn stage height = `length` (m) + engine section `0.6 * nozzleLen` (§2.3) for the lowest visible
stage only **[NEW]**. Interstage band: height `0.25 d` when `interstageMass > 0` **[NEW]**.

### 2.3 Nozzles

From `engines.js`: each engine has `diameter` (m, exit diameter) and `expansionRatio` ε.

* Exit radius `re = engine.diameter / 2`.
* Nozzle length **[NEW, bell approx.]** `nozzleLen = 0.8 * (re - rt) / tan(15°)` with throat
  `rt = re / sqrt(ε)` (80 % bell of a 15° cone).
* **Cluster layout [NEW]:** n = `engineCount`.
  * n = 1: centred. n ≤ 9: one ring (n=7 and n=9 put one in the centre, the rest on the ring).
  * n > 9: centre engine + inner ring of 8 + outer ring for the rest (matches the 7/9-bell look in
    `drawEngineBells`).
  * Ring radius `R = d/2 - re*1.05`. If the bells don't fit (`2πR < n_ring * 2re*1.05`), shrink
    drawn `re` uniformly to fit and flag `overpacked: true` (the designer validator owns the warning).
  * 2D side view projects the cluster onto x: draw each bell at `x = R cos θ`, back row darker
    (`#2b3038` bell / `#6d7380` throat highlight, the colours `drawEngineBells` uses today).
* Vacuum engines (`thrustSL == 0`, e.g. `ff-methalox-vac` ε=90, `exp-hydrolox` ε=285) are hidden
  inside the interstage until staging.

### 2.4 Fairing and nose

* Fairing cylinder: `fairing.diameter × (fairing.length - noseLen)`;
  `noseLen = NOSE_SHAPES[aero.nose] * fairing.length` **[NEW interpretation: factor = nose fraction of
  fairing length]**.
* Profiles: `conic` straight line; `ogive` tangent ogive; `von-karman` LD-Haack (C=0); `blunt` cone
  with a spherical cap of radius `0.25 d`.
* Fairing wider than the top stage (e.g. `methane-reusable.json`: 5.2 m on 5 m) gets a boat-tail cone
  of length `0.5 (d_f - d_s)/tan(15°)`.
* Jettison at `fairing.jettisonAltitudeKm` (default 110): two halves rotate out ±(0→35°) over 1.2 s.

### 2.5 Strap-ons

`count` 2 → at ±x; 4 → ±x plus two at the back (drawn at 0.7 brightness, partially hidden).
Centre offset `(d_core + d_strap)/2`; length from `sizeTank` with the strap-on propellant; nose cone
`conic`, length `1.5 × d_strap` **[NEW]**.

### 2.6 Worked example: `templates/methane-reusable.json` ("Kite")

Booster: 5 × `ff-methalox` (re 0.65 m, ε 34), d 5 m. Upper: 1 × `ff-methalox-vac` (re 1.15 m, ε 90).
Fairing 5.2 × 13 m, ogive → noseLen 3.9 m. Ring radius R = 2.5 − 0.68 = 1.82 m (fits 5 bells).

## 3. Plumes

### 3.1 Inputs

| Input | Source |
|---|---|
| `throttle` τ ∈ [throttle_min, 1] (`hydrolox` up to 1.09) | engine `throttle` |
| ambient pressure ratio `pa = P(h)/P0` | `pa = exp(-h_km / 7.0)` **[NEW]**, scale height 7 km |
| design-pressure proxy `pd` | see below |
| `ε`, `re`, propellant | engine catalog |

The catalog has **no exit pressure**. Proxy: an engine's sea-level/vacuum thrust ratio
`r = thrustSL / thrustVac` measures pressure loss; define the **design altitude pressure**
`pd = clamp((1 − r) * 6, 0.02, 1.2)`. For the catalog:
`gg-kerolox` r=0.905 → pd≈0.57; `ff-methalox` r=0.92 → pd≈0.48; `srm-large` r=0.909 → pd≈0.55.
Vac engines (thrustSL 0) → pd = 0.02 (always overexpanded in atmosphere; never fired there).

### 3.2 Regime

```
x = pd / max(pa, 1e-4)          // >1 underexpanded, <1 overexpanded
```

| Regime | Condition | Look |
|---|---|---|
| Overexpanded | x < 0.8 | narrow, pinched, shock diamonds visible, width < exit |
| Ideal | 0.8–1.25 | straight column, width ≈ exit |
| Underexpanded | 1.25–20 | bulges, width grows ∝ √x |
| Vacuum | pa < 0.001 | wide, faint, hemispherical bloom |

### 3.3 Curves (all **[NEW]**, lengths in nozzle exit diameters D = 2re)

```
s        = (τ - τmin) / (1 - τmin) clamped 0..1     // throttle normalised
length   = D * (6 + 10 s) * (1 + 0.6 * smoothstep(0.2, 0, pa))
width    = D * clamp(sqrt(x), 0.7, 6)
coreAlpha= 0.95 * (0.55 + 0.45 s) * (1 - 0.6 * smoothstep(0.3, 0.0005, pa))
diamonds = x < 1.25 && pa > 0.05 ? round(3 + 4 s) : 0  // spacing 1.2 D * sqrt(x)
```

Cluster: when engine count ≥ 3 and pa < 0.3, plumes merge into one envelope of width
`max(cluster span, width)`.

### 3.4 Colour by propellant (**[NEW]**, linear-space hex)

| propellant | core | mid | tail | soot |
|---|---|---|---|---|
| kerolox | `#fff4d8` | `#ffb040` | `#ff6a1a` | 0.35 |
| methalox | `#e8f0ff` | `#a0c8ff` | `#ff8a50` | 0.05 |
| hydrolox | `#f0f8ff` | `#c8d8ff` | `#80a0ff` (alpha ×0.4) | 0 |
| solid-apcp | `#ffffff` | `#fff0c0` | `#e0d8c8` smoke | 0.6 |
| mmh-nto / udmh-nto | `#fff0d0` | `#ffb070` | `#c86040` | 0.1 |
| hybrid | `#fff0d0` | `#ffa040` | `#d06030` | 0.4 |

Today's emitter (`mission.js`) uses ADD blend, lifespan 260–420 ms, alpha 0.95→0, angle 68–112°,
and switches to "space" particle counts above 28 km. Ports should keep ADD blend and the 28 km
switch as the reduced-particle threshold; `reducedMotion` disables plume emission (as now).

## 4. Sky gradient vs altitude

Use `tod.js` unchanged:

* `spaceBlend(altKm, status) = smoothstep(6, 56, altKm)`; forced to 1 in `SEP` / `HAVEN`.
* Layer alphas from `t = spaceBlend`:

| layer | alpha |
|---|---|
| sky | 1 − t |
| stars | t |
| milky | smoothstep(0.18, 0.9, t) × 0.74 |
| nebula | smoothstep(0.28, 1, t) × 0.52 |
| clouds | 1 − smoothstep(0.04, 0.58, t) |
| haze | 1 − smoothstep(0, 0.72, t) |

* Gradient stops are per time of day (`TOD.dawn|noon|golden|dusk…`.sky, position 0 = zenith/top,
  1 = horizon/bottom). Example `noon`: 0 `#010105`, 0.2 `#050a18`, 0.46 `#0c2048`, 0.7 `#2a6aa0`,
  0.92 `#9ec8e8`, 1 `#d0e4f0`. Ports must read the stop arrays from `tod.js` (or a generated JSON
  export of it), not copy them.
* Fill colour behind everything (the "themed fill", §5) = `lerpRgb(wash.horizon, wash.space, t)`
  from the active palette.

## 5. Portrait scaling (portrait-only, themed fill, never letterbox)

Current mechanism (`main.js`, `stage.js`): the 1280×720 world is **ENVELOP**-scaled (cover, crop
edges, never FIT/letterbox, never RESIZE). Backing-store scale
`k = clamp(round4(max(vw/1280, vh/720) * dpr), 1, min(2, 4096/1280, 4096/720))` where `round4` rounds
to quarter steps and `STAGE_K_MAX = 2`.

Rules for any engine:

1. Cover the full viewport; no bars. Any area outside the world (rounding, overscroll, notch) is
   painted with the themed fill (§4), never black.
2. Vehicle, plume and HUD-critical content stay inside the **safe rect** =
   viewport minus `env(safe-area-inset-*)` (Unity: `Screen.safeArea`), plus 8 pt side margin
   (from `#atb-left/right` in `index.html`).
3. Camera keeps the vehicle at 0.38 of safe-rect height from the top during ascent **[NEW]**.
4. Interaction targets ≥ 44 pt (`designer.css`).

Device table (portrait points; dpr; insets are Apple's published values, **not in repo**):

| class | example | pt (w×h) | dpr | safe top / bottom | envelop (css px per du) | k |
|---|---|---|---|---|---|---|
| iPhone | iPhone 15/16 | 393×852 | 3 | 59 / 34 | 1.183 | 2 (cap) |
| iPhone Pro Max | 16 Pro Max | 440×956 | 3 | 62 / 34 | 1.328 | 2 (cap) |
| iPad | iPad Air 11" | 820×1180 | 2 | 24 / 20 | 1.639 | 2 (cap) |
| iPad large | iPad Pro 13" | 1032×1376 | 2 | 24 / 20 | 1.911 | 2 (cap) |

Envelop on portrait = `vh/720` (height dominates), so horizontal world visible =
`vw / (vh/720)` du: ~332 du on iPhone, ~331 Pro Max, ~500 iPad 11", ~540 iPad 13". The vehicle
(≤ 92 du wide incl. strap-ons) and plume (§3, clamp drawn width to 0.8 × visible width) must fit
the narrowest (iPhone, ~332 du).

## 6. Machine-readable tables

```json
{
  "specVersion": 1,
  "sources": ["web/js/vehicle/design.js", "web/js/data/gen/engines.js", "web/js/tod.js", "web/js/stage.js", "web/js/config.js"],
  "world": { "W": 1280, "H": 720, "scaleMode": "envelop", "stageKMax": 2, "stageEdgeMax": 4096, "kStep": 0.25 },
  "vehicle": {
    "targetHeightDu": 230, "pxPerMClamp": [0.6, 6], "minStageWidthDu": 10,
    "ullage": 0.03, "domeLengthFactor": 0.33, "maxStages": 5, "strapOnCounts": [0, 2, 4],
    "noseShapes": { "ogive": 0.30, "conic": 0.36, "blunt": 0.50, "von-karman": 0.28 },
    "nozzle": { "bellFraction": 0.8, "coneHalfAngleDeg": 15, "ringClearance": 1.05,
                "bellColor": "#2b3038", "throatColor": "#6d7380" },
    "fairingJettisonSec": 1.2, "fairingJettisonDeg": 35
  },
  "plume": {
    "scaleHeightKm": 7.0, "pdFromThrustRatioGain": 6, "pdClamp": [0.02, 1.2],
    "regimes": { "overMax": 0.8, "idealMax": 1.25, "underMax": 20, "vacuumPa": 0.001 },
    "lengthD": { "base": 6, "throttleGain": 10, "vacuumGain": 0.6 },
    "widthClamp": [0.7, 6], "coreAlphaMax": 0.95,
    "diamonds": { "min": 3, "throttleGain": 4, "spacingD": 1.2, "minPa": 0.05 },
    "spaceParticleAltKm": 28, "blend": "add",
    "colors": {
      "kerolox":   { "core": "#fff4d8", "mid": "#ffb040", "tail": "#ff6a1a", "soot": 0.35 },
      "methalox":  { "core": "#e8f0ff", "mid": "#a0c8ff", "tail": "#ff8a50", "soot": 0.05 },
      "hydrolox":  { "core": "#f0f8ff", "mid": "#c8d8ff", "tail": "#80a0ff", "soot": 0.0 },
      "solid-apcp":{ "core": "#ffffff", "mid": "#fff0c0", "tail": "#e0d8c8", "soot": 0.6 },
      "mmh-nto":   { "core": "#fff0d0", "mid": "#ffb070", "tail": "#c86040", "soot": 0.1 },
      "udmh-nto":  { "core": "#fff0d0", "mid": "#ffb070", "tail": "#c86040", "soot": 0.1 },
      "hybrid":    { "core": "#fff0d0", "mid": "#ffa040", "tail": "#d06030", "soot": 0.4 }
    }
  },
  "sky": {
    "spaceBlendKm": [6, 56], "forceSpaceStatus": ["SEP", "HAVEN"],
    "layers": {
      "sky": "1-t", "stars": "t",
      "milky": [0.18, 0.9, 0.74], "nebula": [0.28, 1.0, 0.52],
      "clouds": "1-smoothstep(0.04,0.58,t)", "haze": "1-smoothstep(0,0.72,t)"
    },
    "paletteSource": "web/js/tod.js#TOD"
  },
  "portrait": {
    "letterbox": false, "fill": "lerp(wash.horizon, wash.space, t)", "sideMarginPt": 8,
    "minTouchPt": 44, "vehicleAnchorY": 0.38, "plumeMaxWidthOfVisible": 0.8,
    "devices": [
      { "class": "iphone",        "pt": [393, 852],   "dpr": 3, "safe": [59, 34] },
      { "class": "iphone-promax", "pt": [440, 956],   "dpr": 3, "safe": [62, 34] },
      { "class": "ipad",          "pt": [820, 1180],  "dpr": 2, "safe": [24, 20] },
      { "class": "ipad-large",    "pt": [1032, 1376], "dpr": 2, "safe": [24, 20] }
    ]
  }
}
```

## 7. Gaps (repo data missing)

* No nozzle exit pressure or engine length in `engines.js`: plume regime uses the thrustSL/thrustVac
  proxy (§3.1); nozzle length uses a bell approximation.
* No ambient-pressure model exported from the physics code was used: spec uses a 7 km exponential.
* No plume colours per propellant in repo: §3.4 is new art direction.
* Current flight sprite is a fixed 92×268 canvas independent of the design JSON; mapping in §1–2 is new.
* Device safe-area insets come from Apple specs, not the repo.
* No `sky` stop export as JSON: engines other than Phaser need a generated export of `tod.js`.
