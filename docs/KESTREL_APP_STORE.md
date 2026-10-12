> **ARCHIVED.** This is the unreleased 1.0.1 Phaser/WKWebView listing. The shipping app is Unity 2.0.0. See docs/DECISIONS.md #30/#35 (2.0.0, bundle com.beckify.kestrelheavy), #34 (4+, Data Not Collected, not Kids), #37 (China mainland and Vietnam removed), #36 (Simulation primary, Arcade secondary), and APPSTORE_CONNECT.md. Price unchanged here.

# Kestrel Heavy — 1.0.1 App Store listing (paid $0.99 draft)

Prepared 2026-10-11. Nothing here has been submitted. Scheme **KestrelHeavy**, bundle ID `com.beckify.kestrelheavy`, team `9TR6R5LV8M`, version **1.0.1 (7)**.

## Listing

| Field | Value | Length |
|---|---|---|
| Name | Kestrel Heavy | 13/30 |
| Subtitle | Land the booster on the barge | 29/30 |
| Category | Games → Arcade (secondary: Games → Simulation, optional) | |
| Price | $0.99 (Tier 1 / USD 0.99), no IAP, no ads | |

**Promotional text (157/170)**
Launch a two-stage stack, thread the corridor, nail stage sep, then burn onto barge Haven. Five flights, three skill levels, hangar unlocks. Offline, no ads.

**Keywords (96/100)**
`rocket,lander,booster,landing,launch,space,barge,retro,pixel,flight,physics,pilot,offline,arcade`

**Description**

Kestrel Heavy is a one-thumb launch-and-landing arcade game. Light the engines at Pier 7, steer a two-stage stack up a corridor of birds, balloons, ice and debris, time stage separation, then glide the booster down and HOLD TO BURN onto the recovery barge Haven.

• Five KH-n flights, each with its own payload, time of day and hazards
• Three skill levels: KID, CADET and PAD RAT
• Flight objectives, combos and a per-flight best score
• Bronze, silver and gold landing medals for every flight
• Game Center leaderboards for each KH-n flight
• Hangar: unlock noses, bodies, strakes, engines, paint and accents
• Soft catch, hard catch, splash — every landing scores; only a breakup ends it
• Touch steering or on-screen pads, pause anytime
• Plays fully offline. No ads, no tracking, no in-app purchases. Game Center is optional

Kestrel Heavy, Pier 7 and Haven are original fiction. Not affiliated with any launch provider or space agency.

**What's New (1.0.1):** First release: five flights, three skill levels, landing medals, Game Center leaderboards, hangar unlocks, fully offline.

Support URL https://beckify.com · Marketing URL https://beckify.com/games/kestrel-heavy · Privacy https://beckify.com/privacy · Copyright 2026 Trevor Beck

## Age rating answers
All content questions **None** (cartoon/fantasy violence: None — hazards bump the rocket, no characters harmed). Unrestricted web access **No** (WKWebView loads only the bundled pack via `kestrel-heavy:`). Gambling, contests, UGC, chat, messaging **No**. Parental controls / age assurance **No**. Expected result **4+**.

## App privacy (nutrition label)
**Data Not Collected.** Verified 2026-10-11: no gtag/analytics/fetch to remote hosts in `ios/KestrelHeavy/Game` (only NASA URLs inside `audio/ATTRIBUTION.md`, not loaded); CSP has no remote origins; `PrivacyInfo.xcprivacy` declares no tracking and no collected types. Scores, medals and settings stay in on-device web storage. Flight scores are also sent to Apple Game Center when the player is signed in. That's Apple's own service, so nothing is collected by us and the label stays **Data Not Collected**.

Export compliance: `ITSAppUsesNonExemptEncryption = NO`.

## Screenshots
iPhone 6.9" (1320×2868) and iPad 13" (2064×2752, required because the target ships for iPad). Captured from the packed bundle; see `/workspace/kestrel-review/screenshots/` on the agent box.

## Game Center leaderboards (create these in App Store Connect)

The 1.0.1 (7) build submits one score per finished flight (the same score shown on the results screen) to that flight's leaderboard. It authenticates the local player quietly when the cabinet loads. Offline, signed out, or a leaderboard that isn't live yet are all silent no-ops (up to 20 scores are queued until sign-in). The bridge is the `kestrelGameCenter` WKWebView message handler (`Game/js/gamecenter.js` → `KestrelHeavyGameCenter` in `KestrelHeavyGameView.swift`).

App Store Connect → Apps → Kestrel Heavy → **Services → Game Center** (enable it for the app if prompted, and on the 1.0.1 version page tick **Game Center**). Then **Leaderboards → +** → **Classic Leaderboard**, five times:

| Leaderboard ID | Reference name | Display name (English U.S.) | Score format | Submission type | Sort order | Score range |
|---|---|---|---|---|---|---|
| `com.beckify.kestrelheavy.lb.kh1` | KH-1 Aether Scout | KH-1 · Aether Scout | Integer | Best Score | High to Low | 0 – 10000000 |
| `com.beckify.kestrelheavy.lb.kh2` | KH-2 Ember Pair | KH-2 · Ember Pair | Integer | Best Score | High to Low | 0 – 10000000 |
| `com.beckify.kestrelheavy.lb.kh3` | KH-3 Harbor Eye | KH-3 · Harbor Eye | Integer | Best Score | High to Low | 0 – 10000000 |
| `com.beckify.kestrelheavy.lb.kh4` | KH-4 Nightglass | KH-4 · Nightglass | Integer | Best Score | High to Low | 0 – 10000000 |
| `com.beckify.kestrelheavy.lb.kh5` | KH-5 Lumen Tug | KH-5 · Lumen Tug | Integer | Best Score | High to Low | 0 – 10000000 |

For each one: **Score Format Suffix** none, **Leaderboard Image** optional (512×512 or 1024×1024), and add **English (U.S.)** localization with the display name above and Integer formatting. Leaderboard IDs are permanent, so type them exactly. Leaderboards go live with the app version they're attached to, so add all five to the 1.0.1 version before you submit it for review. Testing in TestFlight uses the Game Center sandbox automatically.

## Medals

Each KH-n flight awards bronze, silver or gold based on the Haven touchdown: sink rate, offset from the paint, and tilt, scaled to the skill level's soft-catch limits. Any deck landing earns bronze. A soft catch on the paint within 80% of the sink limit, 75% of the offset limit and 8° of tilt earns silver. Within 55% of the sink limit, 40% of the offset limit and 4° earns gold. The medal and the touchdown numbers appear on the results screen, the best medal per flight shows in the Hangar, and medals persist in on-device storage.

## Notes
- Paid apps need the Paid Apps Agreement plus banking and tax in App Store Connect before the price can be set.
