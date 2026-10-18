# Flight event VFX: per-tier budgets

All pools are built once per quality tier (`ExplosionFx.QualityOverride`, else `QualitySettings`) and only `Emit`/`SetActive` afterwards: no per-frame or per-event allocation. Flash amplitude and light ceilings fall toward night (`ExplosionBudgets.FlashAmplitudeCap` 0.85 to 0.6, `MaxLightIntensity` 8 to 5). Explosion and staging flashes share one 3 Hz `FlashGovernor`.

| Area | Low | Medium | High | Source |
|---|---|---|---|---|
| (a) Explosion particles (flash/fire/smoke/sparks/embers) | 61 (1/16/20/16/8) | 150 (2/36/48/40/24) | 323 (3/64/96/96/64) | `ExplosionBudgets` |
| (a) Realtime flash lights | 0 | 1 | 1 | `ExplosionBudgets.Lights` |
| (b) Debris: rigid + fragments + sparks | 3 + 12 + 24 = 39 | 3 + 32 + 80 = 115 | 3 + 64 + 200 = 267 | `DebrisBudgets` |
| (c) Shockwave rings | 0 | 1 | 2 | `ShockwaveBudgets.Rings` |
| (c) Haze cards | 0 | 2 | 4 | `ShockwaveBudgets.HazeCards` |
| (c) Ring alpha ceiling / haze alpha ceiling | n/a | 0.35 / 0.14 | 0.35 / 0.14 | `ShockwaveBudgets` |
| (d) Camera beats | same on every tier: no pool, no allocation; trauma <= 1, FOV punch <= 2.5 deg, roll <= 1.5 deg | | | `CameraFxLimits` |
| (d) Slow-mo | min scale 0.4, <= 0.35 s, >= 3 s between, presentation clock only | | | `CameraFxLimits` |
| (e) Staging glow particles | 2 | 4 | 8 | `StagingFlashBudgets.Particles` |
| (e) Staging light | 0 | 1 | 1 | `ExplosionBudgets.Lights` |

Reduce-motion / reduce-flashing: no shockwave, haze, camera beat or slow-mo; no separation flash; the ignition glow becomes a dim, slow, steady light (20% of the light ceiling, 1.7 s rise).
