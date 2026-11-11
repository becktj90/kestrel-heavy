# App Store — open items that need Trevor

Docs only. **No App Store Connect edits, pricing, signing, icon or splash changes** without Trevor's explicit yes. Only Trevor submits to App Review.

Snapshot: 2026-11-10 (America/New_York). `main` at `b7d7f89f` (#490). Export **223** / Xcode Cloud **#58 SUCCEEDED** from `5f6059a591d3` (`xcode-export` `80cd819a`). TestFlight not claimed in this snapshot.

## Still Trevor's call

| ID | Topic | Draft / recommendation in repo | Blocker for submit? |
|---|---|---|---|
| T1 | **Privacy policy URL** | https://beckify.com/privacy — page must describe the **Unity** game (local saves, no analytics, no ads). Fetched earlier as still Phaser/WKWebView-oriented. | **Yes** |
| T2 | **Marketing URL** | https://beckify.com/games/kestrel-heavy currently hosts the Phaser web demo, not the Unity product. Relabel as demo, point elsewhere, or drop. | Should fix before submit |
| T3 | **Support URL** | https://beckify.com — confirm it shows contact info Apple accepts. | Should confirm |
| T4 | **Game Center** | **Removed for 1.0.** Trevor decided **2026-11-09 9:58 AM ET**; executed in [#478](https://github.com/becktj90/kestrel-heavy/pull/478). Listing stays GC-free. | **Done** |
| T5 | **iPad** | Project ships iPhone + iPad, `uIRequiresFullScreen: 1`, portrait only. iPad 13" screenshots become mandatory. Confirm keep iPad on the listing (DECISIONS #7 / #62). | Yes for screenshot set |
| T6 | **AI-voice disclosure** | Description line: "The voices are AI-generated." ElevenLabs on Trevor's paid Creator plan (`docs/NARRATION_LICENSE.md`). Confirm keep for ASC content-rights. | Confirm |
| T7 | **Age rating confirmation** | Draft **4+**, Made for Kids **No** (`docs/AGE_RATING_RECOMMENDATION.md`, DECISIONS #34). Re-check cartoon/fantasy violence against final explosion VFX; KH-8 crew capsule must not show harm. | Confirm |
| T8 | **Privacy label** | Draft **Data Not Collected**. Framework inspect of export **223** is done (`docs/audits/EXPORT_223_PRIVACY_INSPECT.md`): PrivacyInfo tracking false, empty collected types, `ITSAppUsesNonExemptEncryption=NO`, no GameKit/ads/analytics frameworks, `submitAnalytics: 0` (#479). Proxy/device packet capture still not claimed. Do **not** tick the ASC label until the privacy URL (T1) is published. | **Yes** — T1 + Trevor ticks the label |
| T9 | **Paid Applications agreement / pricing** | $0.99 Tier 1 is product intent; bank/tax/Paid Apps agreement still block setting price. | Yes to price |
| T10 | **SKU, DSA trader (EU), China mainland / Vietnam, App Review contact, feedback email** | Listed in `docs/APPSTORE_CONNECT.md` §13. | Confirm |

## Not open (already decided in docs / code)

- Bundle ID for iPhone: `com.beckify.kestrelheavy` (Standalone still shows `.unityproto` in ProjectSettings — export path must keep shipping id).
- Marketing version: **2.0.0**.
- Portrait only.
- No real trademarks in store copy (no Blue Origin, New Glenn, Jacklyn, LC-36). Historical **in-game** commentary may name real programs (DECISIONS #77) — that is not store copy.
- Thrum / corner character off by default (DECISIONS #72); do not feature it in screenshots.
- Screenshots: regenerate only via existing CI path (see [`SCREENSHOTS.md`](SCREENSHOTS.md)); DECISIONS #14 = redo at the end.

## Related

- Field text: [`LISTING.md`](LISTING.md)
- Longer evidence table: [`../APPSTORE_LISTING_UNITY.md`](../APPSTORE_LISTING_UNITY.md)
- ASC field encyclopedia: [`../APPSTORE_CONNECT.md`](../APPSTORE_CONNECT.md)
