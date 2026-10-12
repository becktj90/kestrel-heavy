# App Store requirements — verified (A1)

Status: research, docs only. Nothing here was entered in App Store Connect (ASC), Xcode, the asset catalog or signing.
All sources accessed **2026-10-11** unless noted. Re-verify before submission (Apple edits these pages without notice).

## Sources
| # | Source | URL |
|---|---|---|
| S1 | ASC Help: Screenshot specifications | https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications |
| S2 | ASC Help: Platform version information (promo text, description, keywords, What's New, review notes) | https://developer.apple.com/help/app-store-connect/reference/app-information/platform-version-information |
| S3 | ASC Help: App information (name, subtitle, SKU, bundle ID, age rating, Made for Kids, DSA) | https://developer.apple.com/help/app-store-connect/reference/app-information/app-information |
| S4 | Xcode docs: Creating your app icon using Icon Composer | https://developer.apple.com/documentation/xcode/creating-your-app-icon-using-icon-composer |
| S5 | Icon Composer product page (requires macOS Tahoe 26.4+) | https://developer.apple.com/icon-composer/ |
| S6 | WWDC25 session 361 "Create icons with Icon Composer" | https://developer.apple.com/videos/play/wwdc2025/361/ |
| S7 | HIG: App icons | https://developer.apple.com/design/human-interface-guidelines/app-icons |
| S8 | ASC Help: Age ratings values and definitions | https://developer.apple.com/help/app-store-connect/reference/app-information/age-ratings-values-and-definitions/ |
| S9 | Apple news: Updated age ratings in ASC (4+/9+/13+/16+/18+, new questions, answers due 2026-01-31) | https://developer.apple.com/news/?id=ks775ehf |
| S10 | ASC Help: Overview of Accessibility Nutrition Labels | https://developer.apple.com/help/app-store-connect/manage-app-accessibility/overview-of-accessibility-nutrition-labels |
| S11 | ASC Help: Manage Accessibility Nutrition Labels | https://developer.apple.com/help/app-store-connect/manage-app-accessibility/manage-accessibility-nutrition-labels |
| S12 | App privacy details on the App Store | https://developer.apple.com/app-store/app-privacy-details/ |
| S13 | Complying with encryption export regulations | https://developer.apple.com/documentation/security/complying-with-encryption-export-regulations |
| S14 | ASC Help: App preview specifications | https://developer.apple.com/help/app-store-connect/reference/app-information/app-preview-specifications |
| S15 | App Store Review Guidelines (1.3 Kids, 2.3 accurate metadata, 5.1 privacy) | https://developer.apple.com/app-store/review/guidelines/ |

S1–S4, S8–S11 were read directly today. S7/S12–S15 are cited from Apple's index; their pages render client-side and could not be fetched as text today — re-read before submission (marked ⚠).

## 1. App icon (S4, S5, S6, S7⚠)
- **Format:** an Icon Composer `.icon` file (single multilayer source for iPhone, iPad, Mac, Watch). Canvas **1024×1024** for iPhone/iPad/Mac (1088×1088 Watch, not needed).
- **Layers:** artwork exported as flat, unstyled layers, back-to-front, names numbered (e.g. `01-sky`, `02-plume`, `03-kestrel`). SVG preferred (convert text to outlines — SVG doesn't keep fonts); PNG for raster. No baked shadows, gloss or highlights: Icon Composer adds Liquid Glass specular/blur/translucency/shadow.
- **Appearances:** Default, Dark, Mono (Clear light/dark, Tinted light/dark) — six renditions previewed in Icon Composer. For mono, set at least one element to white and map the rest to greys. Tinted background is system-derived; recolor the glyph layers, not the backdrop.
- **Marketing 1024:** Icon Composer can export a flattened version. App Store marketing icon must be **1024×1024, opaque, no alpha, no pre-rounded corners** (S7⚠; also enforced by ASC upload validation).
- **Unity path (zero-Mac):** Icon Composer needs a Mac. Without one, the Unity iOS player still takes a single 1024 opaque PNG (Xcode generates sizes); dark/tinted variants then need either an `.icon` file added to the generated Xcode project or an asset-catalog dark/tinted set. Both are Xcode/asset-catalog changes → **Trevor gate**. Plan: Kestrel Art produces layered SVGs + validator now; the hook-up waits for approval.
- Validator (A2, Kestrel Art) must check: 1024×1024, PNG, no alpha channel, sRGB/Display P3, no transparency, legible at 29–180 px.

## 2. Screenshots (S1, read 2026-10-11)
- 1–10 per device size, `.png/.jpg/.jpeg`, **no alpha or transparency**.
- **Required:** iPhone with Dynamic Island (medium display) at least one; **iPad 13-inch if the app runs on iPad** (ours does).
- Upload the largest; ASC scales down when UI is consistent.
- Portrait sizes we'll use:
  | Slot | Size (portrait) | Devices |
  |---|---|---|
  | iPhone 6.9" (Dynamic Island large) | **1320×2868** (also 1290×2796, 1260×2736) | 16/17/18 Pro Max, Air |
  | iPhone 6.3" (Dynamic Island medium, **required**) | **1206×2622** or 1179×2556 | 15–18 Pro, 16, 17 |
  | iPad 13" (**required**) | **2064×2752** or 2048×2732 | iPad Pro M4/M5, Air 13" |
- Note from S1: "iPhone Duo" sizes (1398×2034 outer / 2007×2853 inner) will be required from **April 2027** for apps built with the iOS 27.1 SDK or later. Not a 1.0 blocker; track in the risk register.
- Duplicate of the 6.9" upload covers the medium size only if scaling from large is allowed; S1 says medium falls back to *Face ID large* scaled. **Decision:** upload both 1320×2868 and 1206×2622 natively to remove doubt.

## 3. App previews (S14⚠)
Up to 3 per localization per device size (S2). 15–30 s, H.264/HEVC, captured on device, portrait sizes match the screenshot class (e.g. 886×1920 for 6.9" class portrait per S14 — re-verify exact pixel sizes). Optional; ours is planned (see APPSTORE_CONNECT.md §9).

## 4. Field limits (S2, S3)
| Field | Limit | Notes |
|---|---|---|
| Name | 2–30 chars | Editable until submitted; afterwards only with a new version |
| Subtitle | 30 chars | |
| Promotional text | 170 chars | Editable anytime without review |
| Description | 4000 chars | Plain text, no HTML |
| Keywords | **100 bytes**, comma-separated, each > 2 chars | Don't repeat app/company name; no other apps' or companies' names |
| What's New | 4000 chars | **Not available for the first version** |
| Review notes | 4000 bytes | |
| Support URL | required | Must lead to real contact info |
| Marketing URL | optional | |
| Privacy policy URL | required (iOS) | |
| Copyright | required | "2026 Trevor Beck" — © added automatically |
| SKU | letters/digits/`-._`, not first char | Immutable |
| Bundle ID | must match the build | **Immutable after a build is uploaded** |
`tools/asc/check_limits.mjs` enforces these against APPSTORE_CONNECT.md.

## 5. Age rating (S8, S9)
- Ratings now **4+, 9+, 13+, 16+, 18+**, assigned per country/region. Questionnaire sections: content descriptors (frequency None/Infrequent/Frequent), **In-app controls** (parental controls, age assurance), **Capabilities** (unrestricted web access, user-generated content, messaging/chat, social media, advertising), **Medical or Wellness**, **Violent themes**, **Chance-based activities** (simulated gambling, contests, loot boxes).
- 13+ triggers include frequent cartoon/fantasy violence, frequent contests; 16+ unrestricted web access; 18+ gambling.
- You may set a **higher** age rating than calculated (for a minimum-age policy).
- **Made for Kids** (Kids Category) is a separate one-way choice: once approved it cannot be undone and every later update must follow Kids Category rules (no third-party analytics/ads, parental gate for links/purchases — Guideline 1.3⚠).

## 6. App privacy label (S12⚠)
Declare every data type collected by the app **and third-party SDKs**; "Data Not Collected" only if nothing leaves the device in a way that counts as collection. Game Center is Apple's service and does not by itself require declaring collection by the developer (Apple's privacy-details page lists data processed by Apple as not developer-collected — ⚠ re-verify). Unity: **Unity Analytics/Diagnostics/Cloud Diagnostics must be off** in Player/Services settings or the label changes. Also ship a `PrivacyInfo.xcprivacy` manifest (required-reason APIs: e.g. `UserDefaults`/`NSPrivacyAccessedAPICategoryUserDefaults`, file timestamp) — Unity 6 generates one; verify its contents per build.

## 7. Accessibility Nutrition Labels (S10, S11)
Nine features: **VoiceOver, Voice Control, Larger Text (≥200%), Dark Interface, Differentiate Without Color Alone, Sufficient Contrast, Reduced Motion, Captions, Audio Descriptions.** Declared per device (iPhone, iPad). Only claim a feature if users can complete all **common tasks** (first launch, core gameplay, settings, purchase) with it. Voluntary today; managed in ASC → App Accessibility or via the API.

## 8. Export compliance (S13⚠)
`ITSAppUsesNonExemptEncryption = NO` in Info.plist when the app uses only exempt encryption (HTTPS via OS, Apple frameworks such as Game Center). No custom crypto → no documentation upload, no CCATS. France/annual self-classification not required for exempt-only apps. In Unity: add the key via a post-process build step (Xcode-project change → Trevor gate) or answer the question per build in ASC.

## 9. Other ASC items found in S3
DSA trader status (EU) — Trevor-only; content rights declaration (we use only original/licensed content — see LICENSES.md, MUSIC_LICENSES.md, NARRATION_LICENSE.md); Korea GRAC number not needed (no 17+/gambling); China mainland needs an NPPA game approval number — **recommend excluding China mainland** from the 175 countries unless Trevor obtains one (paid games without it are rejected there) [TO CONFIRM current availability list]; Vietnam game license — likewise recommend excluding Vietnam [TO CONFIRM].
