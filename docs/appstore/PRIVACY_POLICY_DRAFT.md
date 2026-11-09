# Privacy policy draft — Kestrel Heavy (Unity)

> **Repo draft only.** Do **not** publish to beckify.com or paste into App Store Connect from this file without Trevor's explicit yes. Trevor owns the live privacy URL and legal wording.

**Product:** Kestrel Heavy (Unity), bundle `com.beckify.kestrelheavy`, portrait iOS (and iPad if kept on the listing).  
**Not this product:** the Phaser web demo at beckify.com/arcade/kestrel-heavy/ (separate page copy if needed).  
**Draft date:** 2026-11-09 (America/New_York). Aligns with App Store privacy label draft **Data Not Collected** (`docs/appstore/TREVOR_OPEN_ITEMS.md` T1 / T8).

---

## Suggested page title
Privacy Policy — Kestrel Heavy

## Suggested body (plain language)

Kestrel Heavy is a rocket design and launch game by Trevor Beck (Beckify).

### What we collect
**We do not collect personal data from Kestrel Heavy.**

- The game plays **offline**. Play does not require an account, sign-in, or internet connection.
- Progress, settings, and flight results stay **on your device**.
- There are **no ads**, **no in-app purchases**, **no analytics SDKs**, and **no crash reporters** shipped for the purpose of sending your data to us.
- We do not sell or share player data because we do not collect it.

### Optional Apple services
**1.0 does not use Game Center.** Trevor decided on 2026-11-09 at 9:58 AM ET (America/New_York); removed in PR #478. Scores and progress stay on device only. A later version may revisit Game Center under a new decision.

### Children's privacy
Kestrel Heavy is rated for a general audience (draft App Store age rating **4+**, not Made for Kids). We do not knowingly collect personal information from children because we do not collect personal information from anyone through this app.

### Contact
For privacy questions about Kestrel Heavy: use the contact details on https://beckify.com (or the Support URL Trevor sets in App Store Connect).

### Changes
If this policy changes, we will update the page at the Privacy Policy URL listed on the App Store product page.

---

## Implementation checklist (Trevor)

| Step | Status |
|---|---|
| Replace Phaser/WKWebView wording on https://beckify.com/privacy (or add a Kestrel Heavy section / dedicated URL) | Open — Trevor |
| Point ASC Privacy Policy URL at the published page | Open — Trevor |
| Confirm Data Not Collected label after built-player proof (no Unity services / network) | Open — after QA `submitAnalytics: 0` + evidence section in RELEASE_READINESS |
| Game Center sentence: keep, edit, or remove to match the shipping build | Open — Trevor (T4) |

## Related
- [`TREVOR_OPEN_ITEMS.md`](TREVOR_OPEN_ITEMS.md)
- [`LISTING.md`](LISTING.md)
- [`../AGE_RATING_RECOMMENDATION.md`](../AGE_RATING_RECOMMENDATION.md)
