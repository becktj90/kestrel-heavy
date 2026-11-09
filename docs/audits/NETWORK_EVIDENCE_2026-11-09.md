# Kestrel Heavy — network / Unity services evidence

Date: 2026-11-09 (America/New_York). Repo `becktj90/kestrel-heavy`, branch `chore/submit-analytics-0` (lands on main after merge).

## Goal
Confirm the built player has no Unity services and makes no unexpected network calls for 1.0 (CoS pre-submit item 2).

## `submitAnalytics`
`unity/ProjectSettings/ProjectSettings.asset`: **submitAnalytics: 0** (was 1; set to 0 this PR).

## Packages (`unity/Packages/manifest.json`)
No `com.unity.services.*`, analytics, ads, IAP, or crash-reporting packages.
Interesting matches: NONE.

Modules present that can touch the network stack (local use only):
- `com.unity.modules.unitywebrequest`
- `com.unity.modules.unitywebrequestaudio`

## Unity Connect (`unity/ProjectSettings/UnityConnectSettings.asset`)
| Setting | m_Enabled |
|---|---|
| CrashReportingSettings | ? |
| UnityPurchasingSettings | 0 |
| UnityAnalyticsSettings | 0 |
| UnityAdsSettings | 0 |
| PerformanceReportingSettings | 0 |

## First-party network call sites in Assets (non-Tests)
- `unity/Assets/Kestrel.Game/Guide/Runtime/GuideClipPlayer.cs`
- `unity/Assets/Kestrel.Core/Guide/GuideClipPack.cs`

`GuideClipPlayer` uses `UnityWebRequestMultimedia.GetAudioClip` for **local** StreamingAssets / file URLs (guide voice clips), not Unity cloud.

## Game Center
Runtime uses `NullGameCenter` (logs only). GameKit / `com.apple.developer.game-center` removed for 1.0 (DECISIONS #98) in a separate PR.

## Built-player note
Config + source + package inventory only. No device packet capture claimed. Optional for Trevor.

## Checklist
- [x] `submitAnalytics: 0`
- [x] No `com.unity.services.*` packages
- [x] Unity Connect analytics/ads/IAP/crash/perf off
- [x] Assets scan for UnityWebRequest / HttpClient
