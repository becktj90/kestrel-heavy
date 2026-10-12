# Home & Menu spec (2.0) — Kestrel UX

Owner: Kestrel UX. Status: spec for build, not implemented. Replaces the 2D vector Home from #116/#127.
Mockups (labelled MOCKUP, rendered with Python/PIL at 2x/3x; flat stand-ins for the live 3D scene):

| Device | File |
|---|---|
| iPhone SE 375×667 pt @2x | `docs/mockups/home/home-iphone-se-375x667@2x.png` |
| iPhone Pro Max 440×956 pt @3x | `docs/mockups/home/home-iphone-promax-440x956@3x.png` |
| iPhone Pro Max, Reduce Motion @2x | `docs/mockups/home/home-iphone-promax-reduce-motion-440x956@2x.png` |
| iPad 13" 1032×1376 pt @2x | `docs/mockups/home/home-ipad13-1032x1376@2x.png` |

References: none copied. No third-party UI or reference library was used for these; layout follows Apple HIG
(44 pt targets, safe areas, Reduce Motion). No trademarks: the vehicle is the generic Kestrel stack, the pad is
"Pad 1". Never name real companies, vehicles or launch complexes.

Hard rules (unchanged): portrait only; themed fill edge to edge, never letterbox; kid-readable; Reduce Motion +
in-game Reduced flash toggle; no energy, loot boxes, timers-to-return, FOMO, streak punishment, confirmshaming,
social pressure or manipulation of children; $0 tools.

---

## 1. Layout per device (points)

All UI is UI Toolkit on the shell panel (Scale With Screen Size, 375×667 reference, match height; see #116), so
"pt" below are SE points. Values in parentheses are the native pt on that device. The 3D scene renders full
bleed behind everything; only interactive UI respects the safe area.

Safe-area rules
- Interactive and text: inside `Screen.safeArea`, plus 16 pt side inset (8 pt minimum on SE).
- Sky, ground, scrims: full bleed under status bar and home indicator.
- Bottom: last row sits 16 pt above the safe-area bottom (home indicator on Face ID phones; 16 pt from the
  screen edge on SE).
- Never place a control in the top 44 pt of the safe area on phones (one-handed reach, Dynamic Island).

### iPhone (SE 375×667, Pro Max 440×956)
Top to bottom. The menu block is in the bottom third, within thumb reach.

| Region | SE | Pro Max (native) | Notes |
|---|---|---|---|
| Status/safe top | 20 | 62 | full-bleed sky behind |
| Title scrim | safe top → +210 | same | gradient, see §6 |
| Wordmark lockup | top at safe+40, ~150 tall | ~170 tall | KESTREL / HEAVY + rule + tagline |
| Scene window | between lockup and strip | grows | the 3D pad; camera frames stack + deck here |
| Telemetry strip | 28 tall, full width − 32 | same | real numbers, see §5 |
| LAUNCH (primary) | 64 tall, full width − 32 | 64 | label + "Missions" subline |
| Secondary 2×2 grid | 2 rows × 48, 10 gap | 48 | Hangar, Codex, Medals, Settings |
| Bottom gap | 16 above safe bottom | 16 + 34 inset | |

Menu block height: 28 + 12 + 64 + 12 + 48 + 10 + 48 + 16 = 238 pt + bottom inset. On SE that is 36% of the
height (bottom third plus the strip); the camera frame (§3) uses the space above the strip.

### iPad 13" (1032×1376) — two columns
Decision: two columns: scene and stack on the left ~60%, menu card on the right ~34%, vertically centred in the
middle third. Why: a centred phone column on iPad either floats tiny in a huge sky or sits over the rocket. The
wide canvas lets the stack stay large and unobstructed, and the card stays in reach for a two-handed hold
(lower middle, not the bottom edge). The wordmark stays centred across the top.

| Region | Size (native pt) |
|---|---|
| Wordmark lockup | centred, top at safe+40, ~240 tall |
| Menu card | x 66%–97%, top at safe+330, 72 (LAUNCH) + 4×64 rows, 14 gap, 16 padding |
| Secondary rows | label + one-line description ("design your rocket") |
| Telemetry strip | under the deck, left column, 28 tall |

## 2. Menu flow

```
Boot ──(≤ 300 ms)──▶ Home (title scene)
Home ─ LAUNCH/Missions ─▶ Missions list ─▶ Design (Hangar w/ mission brief) ─▶ Flight ─▶ result ─▶ Home | Missions
Home ─ Hangar ─────────▶ Design scene (SliceApp, ◀ Home, #126/#127) ─▶ Flight (ROLL OUT & LAUNCH) ─▶ Back to Design
Home ─ Codex ──────────▶ Codex (placeholder in 2.0)   ◀ Home
Home ─ Medals ─────────▶ Medals (placeholder in 2.0)  ◀ Home
Home ─ Settings ───────▶ Settings sheet (real in 2.0) ◀ Home / swipe down
```

- Every destination has a visible ◀ Home (44 pt, top-left inside safe area) and returns to Home with the title
  in its settled state (no re-intro).
- 2.0 real: Home, Hangar (= current Design scene), Flight (in Design scene), Settings.
- 2.0 placeholder: Missions list (one card: "Free flight", goes to Hangar), Codex ("Coming soon" + 3 sample
  entries), Medals (earned list only, never "you missed"). Placeholders use the existing ScreenStub pattern.
- Missions → Design and Flight reuse the existing shell scenes; no new scene types.

## 3. Motion & timing budget

| Item | Budget |
|---|---|
| First frame after Boot | ≤ 300 ms (themed fill + static sky; the 3D scene can stream in after) |
| Menu tappable | ≤ 1,200 ms from first frame (target < 1.5 s); taps during intro skip to settled |
| Full reveal | ≤ 2,400 ms |

Title reveal timeline (ms from first frame):

| t | Event |
|---|---|
| 0 | Dawn sky + ground, camera at rail start |
| 0–800 | sky warms (exposure +0.4 EV, horizon hue to dawn) |
| 200 | vapour emitters ramp 0 → 1 over 600 ms |
| 400–1,000 | wordmark fades/rises 12 pt, ease-out |
| 1,000–1,600 | light sweep across the wordmark, once (no loop) |
| 700–1,200 | telemetry strip + menu fade in, 8 pt rise; tappable at 1,200 |
| 1,200 | `OnTitleReady`; audio sting at 1,000 (§4) |

Camera: closed rail, 40 s loop, ≤ 1.5°/s yaw, ≤ 0.3 m/s dolly, stack and deck always fully inside the scene
window (same framing rule as Design: fit max(rocket, tower top) above the strip). Parallax from device tilt:
≤ 2° camera yaw / 1° pitch, low-pass 0.5 s, off by default on iPad.

Reduce Motion (iOS) or in-game Reduced motion: static camera at the rail's hero pose, no parallax, no sweep,
vapour at 30% and slow, all fades become 200 ms cross-fades; menu tappable at ≤ 400 ms. Reduced flash
(Settings, also forced on by Reduce Motion): no sweep highlight, no sun glints, light changes ≤ 3 per second.

Performance: 60 fps target on A15+ / M1 iPads; 30 fps cap on A12–A14 (QualityService Low) with vapour particles
≤ 400, one shadowed light, no SSAO. Title scene CPU ≤ 6 ms, GPU ≤ 10 ms at 60 fps; ≤ 70 MB extra memory.

## 4. Haptics & audio sting

| Action | UIImpactFeedbackGenerator |
|---|---|
| LAUNCH / Missions tap | medium |
| Hangar, Codex, Medals, Settings tap | light |
| ◀ Home | light |
| Toggle in Settings | selection (UISelectionFeedbackGenerator) |
| Title ready | none (no surprise buzz) |
| Liftoff (Flight) | heavy, once |

Haptics follow the iOS system setting and a Settings toggle "Haptics" (default on).

Sting: 2.4 s, plays once at t=1,000 over the ambient pad bed (wind + venting), never over silence. Music is not
playing on Home in 2.0; if it is later, the sting ducks it −6 dB for 2.4 s. Respects the ring/silent switch
(AVAudioSession ambient), Settings "Sound" (master), "Music" and "Effects" sliders; the sting is an Effect.
Not replayed on return to Home in the same session.

## 5. Scene hooks (names and signatures)

| Hook | Signature | Owner |
|---|---|---|
| `TitleSceneDirector` | `void Play(bool reducedMotion)`, `void Skip()`, `event Action OnTitleReady`, `bool IsSettled` | Graphics Dev |
| `CameraRail` | `float LoopSeconds = 40; float MaxYawDegPerSec = 1.5f; float ParallaxYawDeg = 2; float ParallaxPitchDeg = 1; Pose HeroPose` | Graphics Dev |
| `VapourEmitter` | `float Intensity {get;set;}` 0..1, `void Ramp(float to, float seconds)` | Graphics Dev |
| `TitleWordmark` | `void Reveal(float ms)`, `void Sweep(float ms)` (no-op when reduced flash) | UX (UI Toolkit) |
| `MenuController` | `event Action<Destination> Navigate`; `enum Destination { Missions, Hangar, Codex, Medals, Settings, Home }` | UX |
| `PadStatusFeed` | `PadStatus Current` (wind m/s, temp °C, LOX %, countdown, GO/HOLD) from the seeded weather/LCC (#113) | UX reads, Core provides |
| `Haptics` | `static void Impact(ImpactStyle s)`, `static void Selection()`, `static bool Enabled` | UX |
| `Audio` | `static void PlaySting(string id = "title")`, `static void Duck(float db, float seconds)` | Sound |
| `MotionPrefs` | `static bool ReduceMotion` (iOS ∨ in-game), `static bool ReducedFlash` | UX |

`MenuController.Navigate` goes to `SceneFlow.Go(...)`; Home never calls scenes directly.

## 6. Accessibility

- VoiceOver order: wordmark ("Kestrel Heavy", header) → telemetry strip (one element: "Pad 1. Wind 4 metres
  per second. 18 degrees. Oxygen 97 percent. Go.") → LAUNCH ("Launch. Missions. Button.") → Hangar ("Hangar.
  Design your rocket.") → Codex → Medals → Settings. The 3D scene is hidden from VoiceOver.
- Dynamic Type: labels scale to AX3 (≈ 1.6×); the wordmark is artwork with a fixed size; at AX3+ the iPhone 2×2
  grid becomes a 4-row list and the telemetry strip wraps to two lines. LAUNCH stays one line.
- Targets: ≥ 44 pt everywhere (LAUNCH 64, grid 48, iPad rows 64).
- Contrast over the moving sky: top scrim from rgba(4,6,18,0.55) at the top to 0 at safe+210 pt, behind the
  wordmark; menu cells on opaque #0A0E1E with a 1 pt #465A82 border; text ≥ 4.5:1 (body) and ≥ 3:1 (wordmark)
  against the brightest sky frame. Telemetry is #96E6BE on #04060F (≈ 11:1).
- Nothing conveys state by colour alone (GO/HOLD also as text).

## 7. PR split

Order to avoid conflicts: **UX-1 → GFX-1 ∥ SND-1 → UX-2**. UX-1 lands behind placeholders, so the others never
touch the menu files.

| PR | Owner | Scope | Acceptance | Depends |
|---|---|---|---|---|
| UX-1 | Kestrel UX | `MenuController`, `Destination`, Home UI Toolkit layout (§1) over the current backdrop, placeholders for Missions/Codex/Medals, Settings sheet with Sound/Music/Effects/Haptics/Reduced motion/Reduced flash, `Haptics`, `MotionPrefs`, VoiceOver labels | SE/Pro Max/iPad captures match the mockups; all targets ≥ 44 pt; each destination returns with ◀ Home; Reduce Motion path; no overlap at AX3 | #126, #127 |
| GFX-1 | Graphics Dev | Title 3D scene: real pad + stack, dawn light, `CameraRail`, `VapourEmitter`, `TitleSceneDirector` raising `OnTitleReady` | first frame ≤ 300 ms, tappable ≤ 1.2 s, 60/30 fps budgets on device, stack + deck never clipped on all three sizes | UX-1 (hooks stubbed there) |
| SND-1 | Sound | `Audio.PlaySting`, `Audio.Duck`, the 2.4 s sting asset, ambient pad bed, Settings sliders wiring | silent switch respected; mute works; plays once per session | UX-1 |
| UX-2 | Kestrel UX | `TitleWordmark` lockup + sweep, reveal timeline wired to `TitleSceneDirector`, `PadStatusFeed` strip with live numbers | timeline within ±100 ms of §3; sweep off under Reduced flash | GFX-1, SND-1 |

Behind placeholders: UX-1 ships with the current 2D backdrop and a no-op `Audio`; GFX-1 replaces the backdrop
only; SND-1 replaces the no-op. Missions/Codex/Medals content are later PRs, not 2.0.
