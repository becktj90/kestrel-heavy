# Release readiness (living checklist)

Shipping engine: **Unity** (iOS, **portrait only**). The Phaser build in `web/` is the beckify.com demo and is not release-gating.
Owner of this file: Kestrel Arcade (merge queue) with QA. Merges to `main` and every App Store Connect, signing, privacy or spend action: **Trevor's yes**.

_Snapshot: **2026-11-09** (America/New_York), `main` at `bbda86aa` (#475). Export/TF: **215 / 51** from `b2bd87b31b8a`. Last prior snapshot: 2026-10-28 `39cb8490` (#367)._

## Score

| Bucket | Count | Notes |
|---|---|---|
| **Verified (CI / config)** | **47** | Green required checks on the PR that merged the claim, and/or committed config pins |
| **Unverified (no CI/device proof)** | **11** | Includes "looks right in code" and audio-on-phone |
| **Gap / release decision** | **6** | Store claim false, or needs Trevor |
| **Device-verified** | **0** | Nothing on a real iPhone/iPad has been signed off in-repo |

**Overall: 47 / 62 checklist rows are verified (CI/config)** (includes #475 Ready/countdown, G-4 submitAnalytics, G-6 PrivacyInfo, N10 CONTINUE).  
**Device: 0 / 13 script steps passed in-repo** (see [`DEVICE_TEST_SCRIPT.md`](DEVICE_TEST_SCRIPT.md)).

Honesty rules:

1. **Nothing here is device-verified** unless Trevor (or a named tester) records a Pass in [`DEVICE_TEST_SCRIPT.md`](DEVICE_TEST_SCRIPT.md). CI captures use llvmpipe (no GPU).
2. Unity CI runs on pull requests, not on pushes to `main`. "Verified (CI)" means green on the merging PR, plus Core/Node on `main` where those run.
3. Xcode export **`export/2.0.0-215`** from `main` `b2bd87b31b8a` (#468) = TestFlight / Xcode Cloud **build 51**. After that tip, **#475** merged (KH-1 Ready and countdown share one spare verdict) → current `main` is `bbda86aa`. TF 51 does not yet include #475; re-export when convenient. Prior: 214 / TF 50 from `2f85c027`.

## How to read this

| Status | Meaning |
|---|---|
| **verified** | Backed by a test that ran green in CI, or by committed config. Proof column says which. |
| **unverified** | No test, capture or device run backs it. |
| **gap** | Known missing, wrong or open product/store decision. |

Owners: **Arcade**, **Gameplay**, **Graphics**, **Physics**, **Narrator**, **UX**, **QA**, **Trevor**.

## What landed since 2026-10-28 (high signal)

Merged through #468 (full list via `gh pr list --state merged`). Highlights that change readiness:

| Area | Merged | Effect on checklist |
|---|---|---|
| P0 nav / relaunch | #419, #425, #432, #427 | Results first-tap, Fix-it soft-lock, clean Restart, one-shot Accept |
| Fun-review UX | #423–#431, #424, #426, #428–#430, #441, #443 | Pass card, next→designer, designer chrome, Settings commentary/warp |
| Flight feel | #434, #436, #438, #439, #444, #451, #452 | Nudge gate/offset, coast-warp, no max-Q call, skip stage, player landing burn |
| Narration | #376, #437, #450, #442, #385 | Fact-gated takes, two-host commentary, historical clips, max-Q beats |
| Visual / pad / HUD | #371, #375, #398, #420, #435, #459–#462 | Magenta explosion fix, fairing shoulder, booster follow, plume/terrain/HUD/pad |
| Recovery / sim | #367, #405, #407–#409, #421, #458, #463–#468 | Haven landings CI, failure waits for booster, KH-5 wind gate, fairing mass, KH-12 guidance fit |
| Meta | #374, #378, #379, #381, #447, #448, #446 | Thrum off default, listing docs, this checklist's first cut, App Store shot CI, disk/CI flakes |

Open PRs at snapshot: **none**. Open GitHub issues at snapshot: **none** (former F-10 issues #326/#333/#335/#342/#369 closed).

---

## Navigation

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| N1 | Every route from a fresh start lands on a named screen; every screen has an exit | verified (CI) | Arcade | `NavFlowTests`, `NavFlowPlayTests` |
| N2 | Home → Missions → briefing → designer and back; CONTINUE rides `home-primary` | verified (CI) | Arcade | `NavFlowPlayTests`, `MenuPlayWalkTests`, NAV-03/04/05/07/09 fixed |
| N3 | Every flight ends in a report with Retry, Back to designer, Missions, Home | verified (CI) | Arcade | `NavFlowTests`, `ReportTests`; P0 #419/#425 |
| N4 | Portrait only | verified (config + CI layout) | UX | `ProjectSettings` portrait-only; `PortraitLayoutTests`. Real rotation: unverified (device §1) |
| N5 | Text fits at 335 / 390 / 430 pt | verified (CI render) | UX | `TextFitDeviceSweepTests`, Home/HUD device layout tests. Real safe areas: unverified |
| N6 | Flight camera picker; HUD touches never orbit | verified (CI) | Graphics | #364; `FlightCameraPickerDeviceTests` |
| N7 | Corner character (Thrum) off by default; Settings switch | verified (CI) | UX | #374; `CornerCharacterSettingTests`; DECISIONS #72; `home.thrum: false` |
| N8 | Next mission opens next designer | verified (CI) | Arcade | #426; `NAV06_ReportNext_OpensTheNextMissionsDesigner` |
| N9 | Escape/back in flight opens Pause (NAV-08) | verified (CI) | Arcade | [#472](https://github.com/becktj90/kestrel-heavy/pull/472); `MenuBack.FlightEscape` wired in `SliceApp.EnterFlight`; Core + PlayMode pins |
| N10 | CONTINUE label when cleared>0 and no draft (NAV-10) | verified (CI) | Arcade | this PR; `ResumeOffer.Show` ignores cleared; `SaveResumeTests` / `NAV10_ClearedAlone_DoesNotOfferContinue` |
| N11 | Lock-screen / app-switch resume mid-flight | unverified | Arcade | Device script §12 |

## Designer

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| D-1 | Starter designer: tiers / Why? / speed / X-ray | verified (CI) | UX | #366; `DesignerV3UiTests`, Starter device/capture tests |
| D-2 | Campaign starters Ready / launchable | verified (CI) | Gameplay | #309, #421, #456; launch-gate / wind-gate tests. **#473 fixed by [#475](https://github.com/becktj90/kestrel-heavy/pull/475)** (one spare verdict for Ready + countdown). |
| D-3 | Honest Ready; framed preview | verified (CI) | UX | #352, #365; Starter designer/preview device tests. KH-1 countdown/Ready aligned by #475. |
| D-4 | KH-1 pad framing in designer | verified (CI capture) | Graphics | #368 |
| D-5 | Designer Launch flies headless sim + fairing jettison | verified (CI) | Gameplay | #360 |
| D-6 | Fairing joint = stage diameter, 15° shoulder | verified (CI) | Graphics | #375, #383; fairing joint/mesh tests + captures |
| D-7 | Designer×mission discrepancy audit re-run | gap | Gameplay | [`audits/DESIGNER_MISSION_DISCREPANCIES.md`](audits/DESIGNER_MISSION_DISCREPANCIES.md) still the Oct 19 cut; many Blockers addressed by later PRs but audit not re-scored |
| D-8 | Starter vs flown landing Δv disagreement (#369) | verified (CI) | Gameplay | **Closed** by #408 |
| D-9 | Touch feel on a real phone | unverified | UX | Device script §3 |

## Flight and landing

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| F-1 | Boosters land on landing missions (Auto-assist) | verified (CI) | Gameplay | #367; `BoosterLandingMissionTests`, `HavenLandingGoldenTests`. Hand feel: unverified |
| F-2 | KID landing stand-up on final approach | verified (CI) | Gameplay | #362 (Unity path via shared guidance tests) |
| F-3 | Guidance computer on intended starters | verified (CI + data) | Physics | KH-13 winner + KH-12/KH-13 board starters (`guidanceComputer: true`); #367/#372/#468; `GuidanceComputerTests`. Store: do not over-claim |
| F-4 | Explosions: no magenta, bounded, visible on failure | verified (CI) | Graphics | #371, #425; explosion bounds/failure/capture tests. **Device still required** |
| F-5 | Flight events in order; guarded sinks | verified (CI) | Gameplay | #306; `FLIGHT_EVENTS.md` |
| F-6 | Report timing vs booster still flying | verified (CI) | Gameplay | #354, #405 |
| F-7 | Fairing sep + stage sep visible | verified (CI capture) | Graphics | Fairing separation + flight-phase captures |
| F-8 | First-flight (KH-1) fairness unaided | unverified | Gameplay | Device script §4; no Unity unaided first-flight harness |
| F-9 | Player-flown final landing burn graded on player input | verified (CI) | Gameplay | #452; DECISIONS #79. Device: unverified |
| F-10 | Former open sim issues #326/#333/#335/#342 | verified (closed) | Gameplay | Closed via #407/#405/#463/#367 family |
| F-11 | Coast time-warp bit-identical; Settings row | verified (CI) | Gameplay | #436, #441; `CoastWarpTests` |
| F-12 | No max-Q call; design law + HUD/narration beats | verified (CI) | Gameplay | #439, #442; DECISIONS #89 |
| F-13 | Briefing wind ships on device | verified (CI) | Physics | #458; DECISIONS #86 |

## Narration and audio

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| A-1 | Fact tags + lint | verified (CI) | Narrator | #350; narration-facts lint + `NarrationTests` |
| A-2 | Narrator only states true design facts | verified (CI) | Narrator | #376 verifier |
| A-3 | Two-host color commentary | verified (CI) | Narrator | #437 (re-land of #377) + Settings commentary (#441) |
| A-4 | Narration license / ElevenLabs plan active at release | unverified | Trevor | `NARRATION_LICENSE.md` |
| A-5 | Settings: music, sound, commentary, subtitles, reduce motion | verified (CI) | UX | `UiIds.SettingsControls`; #441 |
| A-6 | Phone audio: silent switch, headphones, interruptions | unverified | Narrator | Device script §10 |
| A-7 | Built player contains guide clips | verified (CI) | Narrator | `check-built-guide-clips.sh`, `AudioImportTests` |
| A-8 | Corner character off ⇒ no Thrum tips/lines | verified (CI) | UX | #374 tests |
| A-9 | Historical commentary clips (DECISIONS #77) | verified (CI) | Narrator | #450 (80 clips). Store copy must still avoid trademarks |

## Visual polish

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| V-1 | Launch pad tower / crown / apron | verified (CI) | Graphics | #361, #363, #462 |
| V-2 | Countdown Below framing | verified (CI capture) | Graphics | #348 |
| V-3 | Flight HUD polish; hidden on result | verified (CI) | Graphics | #346, #398, #420, #461 |
| V-4 | No magenta explosion material on iOS | verified (CI) | Graphics | #371 Resources material; **shader strip still device-only** |
| V-5 | Fairing shape (no flat ledge) | verified (CI) | Graphics | #375 |
| V-6 | Plume, trench, pad, chase horizon | verified (CI) | Graphics | plume/trench/pad/horizon tests; #459/#460 |
| V-7 | Photosensitivity bounds; safe-flash default | verified (CI) | Graphics | Flash limiter tests; DECISIONS #6. Reduce Motion on device: unverified |
| V-8 | Human review of CI capture artifacts | unverified | QA | PNGs exist per workflow; sign-off not recorded |

## Performance

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| P-1 | Pad / FX budgets | verified (CI, unit) | Graphics | `PadPerf`, explosion bounds |
| P-2 | Quality governor | verified (CI) | Graphics | `QualityGovernorTests` |
| P-3 | Loop speed (results → relaunch) | unverified (Unity) | QA | Web audit only; Unity not measured |
| P-4 | Frame rate / thermals / memory on device | gap | Trevor + QA | Device script long flight; nothing recorded |
| P-5 | Cold start / build size / load stalls | unverified | Arcade | Device script |
| P-6 | CI disk / GuidanceComputer / latency flakes mitigated | verified (CI) | Arcade | #447, #448, #446 (allowed one-shot reruns remain) |

## App Store text and screenshots

Canonical drafts: [`docs/appstore/`](appstore/).

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| S-1 | Field lengths within Apple limits | verified | QA | `tools/asc/check_limits.mjs`, `asc-limits` Node test |
| S-2 | Description matches Unity build | verified (docs) | Arcade | [`appstore/LISTING.md`](appstore/LISTING.md) refreshed 2026-11-09; still a **draft** until Trevor pastes into ASC |
| S-3 | Screenshots at store sizes | gap | Graphics + QA | CI path exists (#381 / `appstore-screenshots` workflow) but **final set not selected / uploaded**. Plan: [`appstore/SCREENSHOTS.md`](appstore/SCREENSHOTS.md) |
| S-4 | Version / build | verified (export tag) | Arcade | Marketing 2.0.0; export **215** / TestFlight **51** from `b2bd87b31b8a`. Main tip `bbda86aa` includes #475 (not in TF 51 yet) |
| S-5 | Age rating 4+ / not Kids | unverified | Trevor | Draft in AGE_RATING + [`appstore/TREVOR_OPEN_ITEMS.md`](appstore/TREVOR_OPEN_ITEMS.md) T7 |
| S-6 | TestFlight what-to-test text | verified (text) | QA | APPSTORE_CONNECT §11; update per build |
| S-7 | ASC [TO CONFIRM] block | unverified | Trevor | Privacy URL, Marketing URL, DSA, SKU, agreements — T1–T10 |

## Privacy and Game Center

| ID | Item | Status | Owner | Proof |
|---|---|---|---|---|
| G-1 | Game Center in Unity | **done (removed for 1.0)** | Trevor + QA | Trevor decided **2026-11-09 9:58 AM ET**; removed in [#478](https://github.com/becktj90/kestrel-heavy/pull/478) (DECISIONS #98). Listing omits GC. |
| G-2 | GC failures harmless offline | unverified (Unity) | Arcade | Code catches; no Unity test |
| G-3 | Unity Analytics / Ads / Purchasing / Cloud Diagnostics off | verified (config) | Arcade | `UnityConnectSettings.asset` all `m_Enabled: 0` |
| G-4 | `submitAnalytics` | verified (config) via #479 | Arcade | **0** on the #479 branch / main after merge; see network evidence |
| G-5 | Privacy label proof on built player | unverified | QA + Trevor | Framework inspect + proxy capture not run |
| G-6 | Unity `PrivacyInfo.xcprivacy`: NSPrivacyTracking false, empty collected/tracking-domain arrays; required-reason APIs kept | verified (CI) | Arcade | `IosXcodePostprocess.EnsurePrivacyManifests`; `IosPrivacyManifestTests` |
| G-7 | Export compliance `ITSAppUsesNonExemptEncryption = NO` (Game Center capability **not** added for 1.0; #478) | verified (code) | Arcade | `IosXcodePostprocess.cs`; exported plist unverified on next xcode-export |
| G-8 | No unexpected network at runtime | unverified (Unity) | QA | Device airplane-mode step |
| G-9 | Privacy policy page lists Unity Kestrel Heavy | unverified | Trevor | T1 |


## Network / Unity services evidence

Source: box `/workspace/kh-network-evidence.md`, mirrored in-repo as [`audits/NETWORK_EVIDENCE_2026-11-09.md`](audits/NETWORK_EVIDENCE_2026-11-09.md) / [`playtest/kh-network-evidence-2026-11-09.md`](playtest/kh-network-evidence-2026-11-09.md). Config change + evidence land with [#479](https://github.com/becktj90/kestrel-heavy/pull/479).

| Check | Result | Proof |
|---|---|---|
| `submitAnalytics` | **0** (was 1) | #479 / `ProjectSettings.asset` |
| Unity Connect Analytics / Ads / IAP / Perf | `m_Enabled: 0` | `UnityConnectSettings.asset` |
| Services packages | None | `Packages/manifest.json` |
| First-party `UnityWebRequest` | Local StreamingAssets guide audio only | `GuideClipPlayer` / evidence file |
| Game Center | Removed for 1.0 | Trevor 9:58 AM ET; #478 |
| Device packet capture | **Not claimed** | Optional Trevor (G-5) |

Do **not** tick ASC **Data Not Collected** until the privacy URL (T1) is published.



## Gaps that need a decision before release

Highest first:

1. **G-1 Game Center** — **removed for 1.0** (Trevor 9:58 AM ET; [#478](https://github.com/becktj90/kestrel-heavy/pull/478)). Listing omits GC.
2. **Device script** — Trevor runs [`DEVICE_TEST_SCRIPT.md`](DEVICE_TEST_SCRIPT.md); until then device score stays 0.
3. **S-3 screenshots** — regenerate via existing workflow, pick finals, Trevor uploads.
4. **G-5 / G-4 / G-6 / T1 privacy** — do not claim Data Not Collected until proof + policy page.
5. **F-8 first-flight fairness** — needs a device Pass on KH-1 (#475 fixed the Ready/countdown copy mismatch).
7. **D-7 discrepancy audit** — re-run or formally close Blockers against current main.
8. **P-4 performance on device** — required for heat/ stutter confidence.
9. **A-4 / T6 / T7** — license, AI-voice line, age-rating confirmation.

## Out of scope

- Cube Squad.
- Phaser web demo QA ([#66](https://github.com/becktj90/kestrel-heavy/issues/66)–[#74](https://github.com/becktj90/kestrel-heavy/issues/74) era) — does not gate Unity.
- Landscape on iPhone/iPad.
- Icon / splash art (do not touch).
- App Store Connect edits, pricing, signing (Trevor only).

## Update log

| Date (ET) | Change | By |
|---|---|---|
| 2026-10-28 | First version at `39cb8490`. | QA |
| 2026-11-09 | Refresh through #468; score 42/62 CI-verified, 0 device; point at `docs/appstore/` + `DEVICE_TEST_SCRIPT.md`. | Arcade |
| 2026-11-09 | Export **215** = main `b2bd87b` (was wrongly called 214-lag); KH-1 Ready caveat → issue #473. | Arcade |
| 2026-11-09 | Network evidence section; Trevor removes Game Center from 1.0 (9:58 AM ET). | Arcade |
| 2026-11-09 | G-6: post-process PrivacyInfo.xcprivacy (tracking false, empty collected types). Score 46/62. | Arcade |
| 2026-11-09 | N10: CONTINUE only with draft/saved design (not cleared alone). Score 47/62. | Arcade |
| 2026-11-09 | Resolve main merge after #478; N9 NAV-08 verified via #472; G-7 notes GC out. Score 45/62. | Arcade |
| 2026-11-09 | Build tip: export 215 / TF 51 from b2bd87b; main +#475. Network evidence + #479; GC removed #478; score 45/62. | Arcade |
