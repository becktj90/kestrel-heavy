# NAV_BUGS: what breaks navigation on main today

Base: main ca6c0ee (includes B-04, #303). Branch: test/nav-flow.
Starting point: Arcade's live play-through `/workspace/kh-nav-playthrough/NAV_BUGS_LIVE.md` (F1–F9, web build 50ae4f3).
F1–F4, F6 and F7 are being fixed on fix/F*-* branches. This PR covers their routes and fixes nothing.

Premise: bad rockets may fail and explode. What must always hold is that the game can be navigated.

**How the tests treat these bugs.** Each bug below is encoded as a test named `KnownBug_NAV..`. While the bug reproduces, the test is **Ignored**, and the ignore message carries the observed result. Once the bug stops reproducing, the test **fails** with "remove KnownBug". No test is deleted or loosened.

- Core = `unity/Assets/Kestrel.Core.Tests/NavFlowTests.cs`. Runs under dotnet and was verified here.
- Play = `unity/Assets/Kestrel.Capture.Tests/NavFlowPlayTests.cs`. These are GameCI PlayMode tests. They are **unverified locally**: they compile against the stubs, but I have not run them.

## The bug list

| ID | Live | Route / screen | Button | Expected | Observed on main | Where | Test |
|---|---|---|---|---|---|---|---|
| NAV-01 | F1 | Home → DESIGN FIRST → designer | Launch | The starter rocket for the next mission is launchable | **8/20 blocked** after the designer's reference ascent: m1, m2 prop-depletion "ran out of fuel at T+0 s"; m3, m4, m5, m12 tumble "flipped at T+0 s"; m11, m13 structural-maxq "bent in half at T+0 s". Launch is disabled; the only exits are Home/Back | DesignerV3Panel.cs:728–758 (Gate, settle), LaunchGate.cs:34–52, Kestrel.Core/Flight/ReferenceAscent | **Fixed by #309.** Core `NavLaunchGateTests.NAV01_DesignFirstRocket_IsReadyInTheDesignerAndFlies` (all 20 Ready and fly seeds 1–5) and `NAV01_PlayerWeakenedRocket_StillFails_AndCanLaunchAnyway` |
| NAV-02 | F1 | Missions → briefing → designer with the campaign winner | Launch | The golden winners (5/5 in FlightBatch) are launchable | The designer gate blocks **m1 (dv-margin), m2, m3, m4, m5, m11, m12 (tumble)**. The designer's reference ascent and readout disagree with the flight the batch actually flies. Every red risk reads T+0, so I suspect the reference ascent never gets the mission's setup (MissionFlightSetup). Unconfirmed | same as NAV-01 | **Fixed by #309** (the designer's predict run now flies the mission's MissionFlightSetup). Core `NavLaunchGateTests.NAV02_GoldenWinners_AreReadyInTheDesigner` |
| NAV-03 | F4 | Home → LAUNCH → briefing | To the pad | Briefing → designer, then Launch from the designer | `HomeRoutes.Launch` arms the pad with FlyOnPad=true and AutoPad=true, so "To the pad" goes straight to the countdown and the designer is skipped. `SceneFlow.LaunchChosen` does the same (Fly KH-n) | HomeAutoBuild.cs:517, SceneFlow.cs:128 | Core `KnownBug_NAV03_HomeLaunch_SkipsTheDesigner` |
| NAV-04 | F4, F2 | flight → report | Back to designer | The report offers Retry **and** Back to designer | The exits are report-next, report-retry, report-missions and report-home. The designer is reachable only through "Fix it", which does nothing (F2) | NextMission.cs:10 (AddRoutes) | Core `KnownBug_NAV04_Report_HasNoBackToDesigner` |
| NAV-05 | F2 | flight → report, when no debrief was built | everything except Fix it | Retry, Missions and Home are always on the card | The `ReportView.Build` fallback (taken when the debrief is null, e.g. `PostFlightSource.Publish` threw) adds only `failure-fix` and no Escape. With F2 present, that card is a **softlock** | ReportView.cs:45 (branch), 83 (only button) | Core `KnownBug_NAV05_ReportWithoutDebrief_HasOnlyFixIt` |
| NAV-06 | – | report | Next mission | Opens the next mission's briefing (KH-4 after KH-3) | Lands on the plain Missions list with no overlay, the same as the Missions button. The session silently moves to m4 | MissionContext.cs:337 | Core `KnownBug_NAV06_ReportNext_LandsOnTheList_NotTheNextMission` |
| NAV-07 | – | Home LAUNCH → briefing → designer → Back → briefing | Back | Back to the Missions board | `PadHandoff.TryTake` never clears FromHome, so `BackFromBriefing` sends the player to Home and skips Missions | HomeAutoBuild.cs:454–465, SceneFlow.cs:86 | Core `KnownBug_NAV07_FromHome_SurvivesTheDesigner_BriefingBackSkipsMissions` |
| NAV-08 | – | flight | Escape / Android back | Pause menu | `MenuBack.FlightEscape` is never assigned, so AppShell.HandleEscape does nothing in flight. Only the on-screen pause control pauses. Confirmed in GameCI run 118345963804: no pause menu, screen=Design/None | MenuBack.cs:20, AppShell.cs:129, 177 | Core `KnownBug_NAV08_EscapeInFlight_NeverPauses`, Play `KnownBug_NAV08_EscapeInFlight_DoesNotPause` |
| NAV-09 | – | Missions → briefing → designer | ◀ Home (`home`) | Back to the mission's briefing (the `DesignerBack` route) | `home` runs `GoHome` → Home, and the mission session is dropped. `DesignerBack` is reachable only from the ScreenStub placeholder. Confirmed in GameCI run 118345963804: Home/None, session=none | SliceApp.cs:340, 1059; ScreenStub.cs:71 | Play `KnownBug_NAV09_DesignerBack_DropsTheMission` |
| NAV-10 (low) | F9? | Home with cleared>0 and no draft | CONTINUE | "Opens the rocket you were working on" | CONTINUE shows when cleared>0 even with no draft, and opens an auto-built starter. The label misleads, but it is not a dead end. Documented only | ResumeOffer.cs:7–10, ResumeActions.Open | – |

Info: the Flight scene stub is unreachable because no route returns `SessionScreens.Flight`. Flight lives inside Design (SliceApp).

## Cross-reference to the live F list

| Live | Covered here | Note |
|---|---|---|
| F1 Launch disabled, fuel at T+0 | NAV-01, NAV-02 — **fixed by #309** | Was 8/20 starters and 7/20 golden winners; now 20/20 Ready. KH-11/KH-13 starters got the one strength step Fix it applied |
| F2 Fix it does nothing | NAV-04, NAV-05; Play crash test asserts `failure-fix` exists | Behaviour of the button is not asserted (it is being fixed) |
| F3 stepper Launch chip | not covered | The stepper isn't on the Unity routes driven here |
| F4 LAUNCH skips the designer, no Back to designer/Retry | NAV-03, NAV-04; Core Retry route is green | `ReportRoutes.Again` keeps mission+seed (green) |
| F5 Follow rocket | not covered | Camera, not navigation |
| F6 Skip count disabled | not covered | |
| F7 credits change without purchase | not covered | |
| F8 Home blank ~3 s after briefing Back | Play waits on `flow.Busy`; it doesn't time the blank | |
| F9 stray CONTINUE | NAV-10 is related (CONTINUE offered with no draft) | Layout part not covered |

## Green (holds on main)

- Every RouteId from a fresh start, a campaign session and a free build lands on a named screen and never throws.
- Every screen and overlay has an exit to somewhere else.
- Home LAUNCH briefs m1 on a fresh install, m6 with cleared=5, and m20 when complete. DESIGN FIRST opens the designer with the mission and does not auto-fly. Both still route correctly when no campaign data is present.
- Missions → briefing → Build → designer → Back → briefing → Back → Missions → Home. An unknown mission id stays on the board. The free-build designer Back goes Home.
- Report Retry keeps the mission and seed, and opens the designer even with no session. Missions and Home leave; Home clears the session. Next on KH-20 stays on KH-20.
- Crashing rockets through the **live** setup (`MissionFlightSetup` at `LiveDt`, failure monitor not gentle): one engine (tumble), no structural margin (max-Q) and 5 % fuel all compose a report card, and Retry and the designer Back route from it.
- Fresh store: no CONTINUE. After the designer's autosave and Exit to Home mid-flight, CONTINUE is offered and resumes the m3 draft.
