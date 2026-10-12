# App Store Connect — every field for the Unity 1.0 release (A3–A6)

Docs only. **Never** run a submit, upload, metadata push or ASC edit without Trevor's explicit yes. Limits and sources: [APPSTORE_REQUIREMENTS.md](APPSTORE_REQUIREMENTS.md). Counts are checked by `node tools/asc/check_limits.mjs` (fields are the fenced `asc:` blocks below).
Rule: every claim must be true of the shipping **Unity** build. Anything the Unity build doesn't yet do is marked **[GATED: Ux]** (true only once that milestone passes QA) and must be removed if it slips. `[TO CONFIRM]` = Trevor-only fact.

## 0. Known current ASC state (set by Chief of Staff, 2026-10-11)
Arcade category · $0.99 · 175 countries · privacy "Data Not Collected" · App Review contact copied from Beckify · sign-in required unticked · leaderboards kh1–kh5 · version record **1.0.1** (web-wrapper build, will not ship).

## 1. Version plan (decision #23)
- The app has **never been released**, so the 1.0.1 version record can simply be **renamed to 2.0** in ASC before the Unity build is attached (Trevor action, G2). No need to release 1.0.1.
- Marketing version `CFBundleShortVersionString` = **2.0.0** (ASC shows "2.0"). Signals the full rebuild; matches MASTER_ROADMAP "1.0 (2.0 build)".
- Build number `CFBundleVersion`: monotonic integer from Unity Build Automation; start at **100** so it can never collide with web builds (1.0.1 (7)). [TO CONFIRM highest build number already uploaded]
- Bundle ID **`com.beckify.kestrelheavy`** (UNITY_SHIP_PLAN). ⚠ The Unity project currently has `com.beckify.kestrelheavy.unityproto` and a stray `com.kestrelheavy.app` in ProjectSettings — the swap happens at U9 with Trevor's yes (G2).
- Next: 2.0.x fixes; 2.1 = the roadmap's "1.1" content drop.

## 2. App information
| Field | Value |
|---|---|
| Name | see block | 
| Subtitle | see block |
| Primary language | English (U.S.) |
| Category | Games → **Simulation** primary, Games → **Arcade** secondary (decision #24: the Unity build is a rocket design + flight sim; Arcade-only undersells it). Changing the primary is a Trevor ASC action. |
| Content rights | Does not contain third-party content requiring rights beyond those licensed (original art/music/fiction; ElevenLabs narration on Trevor's paid plan; free assets per ASSETS_LICENSES.md) |
| Made for Kids | **No** (recommended — see §6) |
| License agreement | Apple standard EULA |
| Privacy policy URL | https://beckify.com/privacy [TO CONFIRM page lists Kestrel Heavy and says no data collected] |
| SKU | [TO CONFIRM — already set, immutable] |
| DSA trader status (EU) | [TO CONFIRM — Trevor: trader or not; if trader, address/phone/email are shown publicly] |
| Availability | 175 countries currently. Recommend removing **China mainland** (needs NPPA game approval number) and **Vietnam** (game license) [TO CONFIRM whether they're in the 175]. |

```asc:name
Kestrel Heavy
```
```asc:subtitle
Design, launch, land rockets
```

## 3. Version page (English U.S.)
```asc:promotional_text
Design a rocket, predict the flight, light it up. Learn from every launch and fly again in seconds. Offline, no ads, no tracking, no in-app purchases.
```
```asc:keywords
rocket,launch,space,orbit,booster,landing,simulator,engineering,physics,spaceflight,builder,stem
```
```asc:description
Kestrel Heavy is a rocket design and launch game for curious kids and serious engineers alike.

Snap together big, tactile parts in the hangar, check the predicted flight, then tap to ignite. Watch your rocket climb through max-Q, stage, and coast toward orbit, then bring the booster home. When a flight falls short, the debrief shows exactly what was missing and how to fix it, and you can relaunch in seconds.

• Three design layers: Simple to start, Standard after your first orbit, and an Engineer mode in Settings
• Honest physics: thrust, drag, staging and delta-v you can trust, tuned with friendly assists and presets
• Mission control and webcast narration, plus an original adaptive score that follows your flight
• Replays, a daily challenge and personal bests
• Five KH flights with Game Center leaderboards (optional)
• Plays fully offline. No ads, no tracking, no accounts, no in-app purchases
• Safe-flash on by default, and reduced motion follows your device setting

Kestrel Heavy and its world are original fiction. Not affiliated with any launch provider or space agency.
```
Claim gates (remove the bullet if the gate isn't met at submission): design layers **[GATED: U3/U8]**, narration **[GATED: U7]**, adaptive score **[GATED: U7, music stems]**, replay/daily **[GATED: per roadmap 1.0 row]**, Game Center **[GATED: U9]**, debrief/relaunch-in-seconds **[GATED: H-loop, QA loop-speed audit]**.

- What's New: **not available for the first release** (S2). Draft for 2.0.1 kept in §10.
- Support URL: https://beckify.com [TO CONFIRM it shows contact info: email/address] · Marketing URL: https://beckify.com/games/kestrel-heavy [TO CONFIRM page exists]
- Copyright: `2026 Trevor Beck`
- Version release: **Manual** release after approval (Trevor presses release).
- Phased release: n/a for first version.
- Game Center: ticked on the version **[GATED: U9]**.

```asc:review_notes
Kestrel Heavy needs no account or sign-in. Everything works offline. To test: on the title pad tap the rocket to ignite, or tap CONTINUE to open the hangar, build or pick a preset rocket, and tap Launch. Settings > Accessibility has a flat menu list, safe-flash and reduced-motion options. Game Center is optional; leaderboards are kh1-kh5, one per KH flight. No in-app purchases, ads, analytics or network calls besides Game Center. Narration was generated by the developer on a paid ElevenLabs plan; all music and art are original.
```
App Review contact: copied from Beckify [TO CONFIRM still current; phone in +1 format]. Sign-in required: **No**.

## 4. Pricing (fixed)
$0.99 premium (USD tier), **no in-app purchases**, no ads, no subscriptions. Paid Apps Agreement + banking + tax must be active [TO CONFIRM].

## 5. App privacy label (decision: Data Not Collected)
Answer: **"No, we do not collect data from this app."**
True only if, for the Unity build: Unity Analytics, Cloud Diagnostics, Unity Ads, Remote Config and any crash reporters are **off/removed**; no network calls except Game Center; saves stay on device; photo/CSV export goes through the share sheet (user-initiated, no Photos permission). QA checklist item: inspect the built Xcode project for `UnityAnalytics`/`ads` frameworks and run a proxy capture during a full session. On-device metrics from the H-brief stay local; any network analytics → Trevor + label change first.
Privacy manifest: Unity-generated `PrivacyInfo.xcprivacy` — NSPrivacyTracking false, no collected data types, required-reason APIs as Unity declares.

## 6. Age rating (recommendation — Trevor decides)
**Recommend 4+, Made for Kids = No, audience "everyone."** Questionnaire answers for the Unity build:
| Section | Answer |
|---|---|
| Violence (cartoon/fantasy, realistic, weapons) | None — rockets can fail/explode as mechanical failure; no characters harmed [TO CONFIRM against final failure VFX; "Infrequent cartoon violence" still yields 4+/9+] |
| Mature themes, profanity, horror, alcohol/drugs, sexual content | None |
| Medical or wellness | None (healthy-play reminders are not medical info) |
| Chance-based (gambling, simulated gambling, contests, loot boxes) | None — no random rewards (H-brief ban list) |
| Capabilities: unrestricted web access | No |
| User-generated content | No — design codes are local, sanitized data with no free text shared [TO CONFIRM design codes carry no player-typed names] |
| Messaging/chat, social media | No |
| Advertising | No |
| In-app controls: parental controls / age assurance | No |
Why not Kids Category: it's one-way and forbids even Game Center–style external links without a parental gate; "everyone, 4+" reaches kids without the lock-in. Trevor decides (DECISIONS #21).

## 7. Accessibility Nutrition Labels (declare per device; only what works end to end)
| Feature | iPhone | iPad | Basis / gap |
|---|---|---|---|
| VoiceOver | **No** | **No** | Gap: UI Toolkit + 3D flight need Apple Accessibility plugin wiring for every control; planned U9. |
| Voice Control | No | No | Depends on VoiceOver labels. |
| Larger Text | No | No | Gap: Dynamic Type scaling to 200% in UI Toolkit not built. |
| Dark Interface | **Yes** [GATED: U2 visual pass] | Yes | Game UI is dark-themed by design. |
| Differentiate Without Color Alone | No | No | Gap: readouts/warnings need shape/text redundancy audit. |
| Sufficient Contrast | No | No | Gap: contrast audit (WCAG 4.5:1) not run. |
| Reduced Motion | **Yes** [GATED: QA] | Yes | Follows OS setting (DECISIONS #6); must also cut camera shake + flashes. |
| Captions | No | No | Gap: narration subtitles exist? [TO CONFIRM — if captions for all narration land, flip to Yes]. |
| Audio Descriptions | No | No | No video content; n/a. |
Gap list owners: Kestrel UX (contrast/color), Arcade U9 (VoiceOver/Larger Text), Narrator Voice (captions).

## 8. Export compliance
Uses only exempt encryption (OS HTTPS for Game Center) → `ITSAppUsesNonExemptEncryption = NO` via a Unity post-process build script (Xcode-project change → Trevor gate) — otherwise answer "None of the algorithms mentioned above" per build in ASC.

## 9. Screenshot storyboard (portrait) and capture pipeline — redone at the end (DECISIONS #14)
Sizes: iPhone 1320×2868 + 1206×2622, iPad 2064×2752. 6 frames each, no alpha, short caption band at top.
| # | Scene | Caption |
|---|---|---|
| 1 | Ignition on the title pad, sunrise, plume filling frame | Light it up. |
| 2 | Hangar, Simple layer, big tactile parts + predicted outcome card | Build it your way. |
| 3 | Max-Q climb, telemetry HUD | Real physics, real numbers. |
| 4 | Stage separation + booster flip | Stage, coast, reach orbit. |
| 5 | Booster landing on Haven at dusk | Bring the booster home. |
| 6 | Debrief: gap-to-goal ("short 140 m/s — add a tank") | Learn. Fix. Fly again. |
Pipeline: a Unity `ScreenshotDirector` scene (Editor + device) with seeded flights and fixed camera shots → `ScreenCapture.CaptureScreenshotAsTexture` at native res per target size → caption compositing script (ImageMagick on Linux) → `tools/asc/check_screenshots` verifies size, RGB no alpha → `fastlane/screenshots/<locale>/`. Upload only via the gated `deliver` lane.

### App preview script (≈25 s, portrait, no captions needed)
0–3 s black → title pad, finger taps rocket (pre-roar silence) · 3–7 s ignition hit, plume · 7–11 s hangar snap-snap, predicted card shows 92% · 11–16 s max-Q shake, staging · 16–20 s orbit wonder shot · 20–24 s booster lands on Haven, results "Personal best" · 24–25 s logo. Audio: Kestrel motif full variation. Captured from device or Unity Recorder at preview spec (S14).

## 10. Game Center sheets
Leaderboards (existing, IDs permanent; Unity build reuses them via Apple Unity plugins, U9):
| ID | Display | Format | Type | Sort | Range |
|---|---|---|---|---|---|
| com.beckify.kestrelheavy.lb.kh1 | KH-1 · Aether Scout | Integer | Best | High→Low | 0–10,000,000 |
| …lb.kh2 | KH-2 · Ember Pair | same | | | |
| …lb.kh3 | KH-3 · Harbor Eye | | | | |
| …lb.kh4 | KH-4 · Nightglass | | | | |
| …lb.kh5 | KH-5 · Lumen Tug | | | | |
Unity score must match web scoring for the KH replay (U1 D0 vectors) or the old boards get a new ID set (don't mix). Achievements (proposed, 1.0, create only with G2): `ach.first_light` First Light (any launch), `ach.orbit` First Orbit, `ach.home_again` Booster recovered, `ach.gold_all` Gold on all KH flights, `ach.engineer` Fly a design in Engineer mode. 100 pts each max 1000 total; no hidden/grindy ones (H ban list).
Draft What's New 2.0.1: `Fixes and polish. Thanks for flying.`

## 11. TestFlight copy
```asc:testflight_whats_to_test
Fly a full mission: build or pick a preset, check the prediction, launch, stage, and land the booster. Tell us where you got stuck, anything that felt slow to relaunch, and any frame drops or heat on your device.
```
```asc:testflight_description
Kestrel Heavy is a rocket design and launch game. Offline, no account needed.
```
Beta review contact = App Review contact. Feedback email [TO CONFIRM].

## 12. Release runbook (zero-Mac)
1. **QA gate:** RESULTS.md pass criteria, full regression, privacy proxy capture, check_limits green, screenshots validated.
2. **Build:** Unity Build Automation (free tier) iOS target, bundle `com.beckify.kestrelheavy`, 2.0.0 (≥100). Signing assets live only in UBA's credential store (Trevor-uploaded). **Trevor yes #1: start the release build.**
3. **Upload:** UBA App Store Connect delivery using the existing ASC API key (KEY_ID/ISSUER/.p8 stored as UBA secrets, never in git). Fallback: `fastlane upload_testflight` on a CI runner with `asc_key.json` from secrets. **Trevor yes #2: upload.**
4. **TestFlight:** internal group (Trevor). Smoke on iPhone + iPad.
5. **Metadata:** rename version 1.0.1 → 2.0; apply this doc (by hand or `fastlane deliver` dry-run diff first). **Trevor yes #3: push metadata/screenshots.**
6. **Submit for review**, manual release. **Trevor yes #4: submit.**
7. Approved → **Trevor yes #5: release.** Then tag `v2.0.0` and update PROJECT_STATE.md.
Rollback: no auto-release; a bad build is pulled by "Remove from sale" or expedited 2.0.1.

## 13. Open [TO CONFIRM] list for Trevor
SKU · DSA trader status · App Review contact current · Paid Apps agreement/banking/tax · privacy/support/marketing URL contents · China/Vietnam availability · highest uploaded build number · feedback email · final age-rating call (4+ recommended) · primary category switch to Simulation.
