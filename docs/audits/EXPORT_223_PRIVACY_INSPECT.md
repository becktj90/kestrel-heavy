# Privacy / framework inspect — export 223

**Not device-verified. No IPA download. No proxy or packet capture.**

This is the G-5 framework inspect (and the G-7 export-plist check) of the Unity iOS Xcode project that Xcode Cloud **#58** built.

| Item | Value |
|---|---|
| Export branch | `xcode-export` `80cd819a9c527b69dc5eeda2f17bdded44facca8` |
| Message | `xcode-export 2.0.0 (223), iOS 16.0 minimum, from main 5f6059a591d3` |
| Actions run | [42899969664](https://github.com/becktj90/kestrel-heavy/actions/runs/42899969664) — success, 15:55–16:02 ET America/New_York |
| Xcode Cloud | **#58 SUCCEEDED** (`GIT_REF_CHANGE`), started 16:02:28 ET, finished 16:15:58 ET America/New_York |
| Script | `tools/ios-export/verify-privacy-export.sh origin/xcode-export` — **OK**, rc=0, 20:14 ET America/New_York 2026-11-10 |
| Also | `tools/ios-export/verify-no-game-center.sh origin/xcode-export` — **OK**, rc=0 (same tip, earlier the same afternoon) |

Current `main` at the time of this note may be one docs-only commit ahead (`b7d7f89f`, #490). That commit does not change the exported player.

## What was opened

- `Info.plist` — marketing version, build number, export-compliance flag
- `UnityFramework/PrivacyInfo.xcprivacy` — tracking / collected-types / required-reason APIs
- `Unity-iPhone/KestrelHeavy.entitlements`
- `Unity-iPhone.xcodeproj/project.pbxproj` — linked frameworks (Unity `GameCenterModule` il2cpp stubs ignored, same rule as `verify-no-game-center.sh`)

## Findings

| Check | Result |
|---|---|
| `CFBundleShortVersionString` | `2.0.0` |
| `CFBundleVersion` | `223` |
| `ITSAppUsesNonExemptEncryption` | `false` (G-7, on the export, not only in post-process tests) |
| `NSPrivacyTracking` | `false` |
| `NSPrivacyTrackingDomains` | empty |
| `NSPrivacyCollectedDataTypes` | empty |
| Required-reason APIs (kept) | SystemBootTime `35F9.1`; DiskSpace `E174.1`; UserDefaults `CA92.1`; FileTimestamp `0A2A.1`, `C617.1` |
| Entitlements | empty dict — no `com.apple.developer.game-center`, no associated domains, no iCloud |
| Linked Apple frameworks | AVFoundation, AVKit, AudioToolbox, CoreGraphics, CoreMedia, CoreText, CoreVideo, Foundation, GameController, Metal, QuartzCore, Security, SystemConfiguration, UIKit |
| Banned / unexpected | **None** — no GameKit, AdSupport, AppTrackingTransparency, iAd, StoreKit, WebKit, CoreTelephony, SafariServices, or analytics/ads SDKs |
| `submitAnalytics` on `main` | **0** (already pinned by #479; confirmed again on this inspect) |

`SystemConfiguration.framework` is the ordinary Unity / reachability link, not an analytics SDK. `GameController.framework` is gamepad input, not Game Center.

## What this does not prove

- No downloaded IPA or `otool`/`nm` of a compiled binary (Cloud #58 succeeded; this inspect is of the exported Xcode project that was its source).
- No device or proxy packet capture (G-8 / optional G-5 capture). Still **not claimed**.
- This is not permission to tick App Store Connect **Data Not Collected**. That stays Trevor's privacy-label decision, and it still waits on the published privacy URL (T1). See [`docs/appstore/TREVOR_OPEN_ITEMS.md`](../appstore/TREVOR_OPEN_ITEMS.md) T8.

Re-run:

```bash
bash tools/ios-export/verify-privacy-export.sh origin/xcode-export
```
