# App Store — open items that need Trevor

Docs only. **No App Store Connect edits, pricing, signing, icon or splash changes** without Trevor's explicit yes. Only Trevor submits to App Review.

Snapshot: 2026-11-09 (America/New_York). `main` at `b2bd87b3` (#468). Latest Xcode export tag: **`export/2.0.0-215`** from `main` `b2bd87b31b8a` — **up to date with current main** (same tip). Prior tip was 214 from `2f85c027`.

## Still Trevor's call

| ID | Topic | Draft / recommendation in repo | Blocker for submit? |
|---|---|---|---|
| T1 | **Privacy policy URL** | https://beckify.com/privacy — page must describe the **Unity** game (local saves, no analytics, no ads). Fetched earlier as still Phaser/WKWebView-oriented. | **Yes** |
| T2 | **Marketing URL** | https://beckify.com/games/kestrel-heavy currently hosts the Phaser web demo, not the Unity product. Relabel as demo, point elsewhere, or drop. | Should fix before submit |
| T3 | **Support URL** | https://beckify.com — confirm it shows contact info Apple accepts. | Should confirm |
| T4 | **Game Center** | **Trevor decided 2026-11-09 9:58 AM ET:** remove Game Center from **1.0**. QA stream owns the entitlement/GameKit removal (`chore/remove-game-center-1.0`). Listing copy stays without Game Center (LISTING.md). Do not re-add GC claims for 1.0. | Decided — QA executes |
| T5 | **iPad** | Project ships iPhone + iPad, `uIRequiresFullScreen: 1`, portrait only. iPad 13" screenshots become mandatory. Confirm keep iPad on the listing (DECISIONS #7 / #62). | Yes for screenshot set |
| T6 | **AI-voice disclosure** | Description line: "The voices are AI-generated." ElevenLabs on Trevor's paid Creator plan (`docs/NARRATION_LICENSE.md`). Confirm keep for ASC content-rights. | Confirm |
| T7 | **Age rating confirmation** | Draft **4+**, Made for Kids **No** (`docs/AGE_RATING_RECOMMENDATION.md`, DECISIONS #34). Re-check cartoon/fantasy violence against final explosion VFX; KH-8 crew capsule must not show harm. | Confirm |
| T8 | **Privacy label** | Draft **Data Not Collected**. Needs built-player proof (framework inspect + optional proxy capture). `submitAnalytics: 1` still in `ProjectSettings.asset` while Unity Connect services are off — confirm or clear. | **Yes** before claiming the label |
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
