# Kestrel Heavy — network / Unity services evidence

Date: 2026-11-09 (America/New_York). Source: repo `becktj90/kestrel-heavy` on `main` tip at time of write (will refresh after `submitAnalytics: 0` lands).

## Goal
Confirm the built player has no Unity services and makes no unexpected network calls for 1.0 (CoS pre-submit).

## Packages (`unity/Packages/manifest.json`)
No `com.unity.services.*`, analytics, ads, IAP, or crash-reporting packages.
Present modules that can touch the network stack:
- `com.unity.modules.unitywebrequest`
- `com.unity.modules.unitywebrequestaudio`

These are required for local StreamingAssets audio load (Guide clips), not for Unity cloud services.

## Unity Connect (`unity/ProjectSettings/UnityConnectSettings.asset`)
| Setting | Value |
|---|---|
| CrashReportingSettings.m_Enabled | 0 |
| UnityPurchasingSettings.m_Enabled | 0 |
| UnityAnalyticsSettings.m_Enabled | 0 |
| UnityAdsSettings.m_Enabled | 0 |
| PerformanceReportingSettings.m_Enabled | 0 |

## `submitAnalytics` (`unity/ProjectSettings/ProjectSettings.asset`)
Pending change: set `submitAnalytics: 0` (was 1). Tracked as CoS item 2; PR TBD.

## First-party `UnityWebRequest` in Assets (non-Tests)
| File | Use |
|---|---|
| `Kestrel.Game/Guide/Runtime/GuideClipPlayer.cs` | `UnityWebRequestMultimedia.GetAudioClip` for **local** StreamingAssets / file URLs (guide voice clips) |

No other `UnityWebRequest`, `WWW`, or `HttpClient` call sites under `unity/Assets` outside Tests.

## Game Center
Runtime uses `NullGameCenter` (logs only). Game Center entitlement/GameKit link removed for 1.0 (DECISIONS #98 / branch `chore/remove-game-center-1.0`) — separate from Unity analytics.

## Built-player note
Full packet capture of an iOS device build is **not** claimed here. Evidence is config + source scan + package inventory. A device network capture remains optional for Trevor.

## Refresh checklist after `submitAnalytics: 0` PR
- [ ] Confirm `submitAnalytics: 0` on main
- [ ] Re-scan Packages / Assets
- [ ] Cite this file from docs stream / RELEASE_READINESS G-4
