// test(nav) PlayMode: the live shell (Boot → SceneFlow → SliceApp → FlightDriver), fresh install and existing save.
// UNVERIFIED LOCALLY: this file compiles against the Unity stubs (tools/cs-stubcheck) but has only run where GameCI runs
// PlayMode. Bad rockets may fail and explode; what must hold is that every screen has a way on and a way out.
// Known bugs on main (NAV_BUGS.md) are Ignored with the observed result while they reproduce and fail once they stop.
using System;
using System.Collections;
using System.IO;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Ui;
using Kestrel.Core.Vehicle;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Kestrel.Capture.Tests
{
    /// <summary>
    /// Control lookup by the UiIds names (&lt;screen&gt;-&lt;control&gt;[-&lt;option&gt;], screen-&lt;id&gt;, &lt;screen&gt;-back),
    /// falling back to the names on main until UiIds lands. One place to drop the fallbacks.
    /// </summary>
    public static class NavIds
    {
        static readonly (string id, string[] main)[] Names =
        {
            ("home-primary", new[] { "home-launch", HomeResumeBar.ContinueName }),
            ("home-missions", new string[0]),
            ("home-settings", new string[0]),
            ("settings-close", new[] { "home-settings-close" }),
            ("screen-settings", new[] { "home-settings-sheet" }),
            ("screen-missions", new[] { "mission-board", "mission-map" }),
            // Main's live board is MissionBoardScreen ("mission-home"); MissionSelectScreen ("missions-home") is the older map.
            ("missions-back", new[] { "mission-home", "missions-home" }),
            ("screen-briefing", new[] { "briefing-card" }),
            ("briefing-build", new string[0]),
            ("briefing-back", new string[0]),
            ("starter-launch", new[] { "launch-btn", "launch" }),
            ("starter-back", new[] { "home" }),
            ("flight-pause", new[] { "pause" }),
            ("screen-pause", new[] { "pause-menu" }),
            ("pause-resume", new[] { "resume" }),
            ("pause-exit-home", new[] { "exit-home" }),
            ("screen-report", new[] { "post-flight" }),
            ("report-retry", new string[0]),
            ("report-home", new string[0]),
            ("report-missions", new string[0]),
            ("report-fix", new[] { "failure-fix" }),
        };

        public static VisualElement Find(string id, params VisualElement[] roots)
        {
            foreach (var root in roots)
            {
                if (root == null) continue;
                var hit = root.Q(id);
                if (hit != null) return hit;
                foreach (var row in Names)
                    if (row.id == id)
                        foreach (var name in row.main)
                        {
                            hit = root.Q(name);
                            if (hit != null) return hit;
                        }
            }
            return null;
        }

        /// <summary>The CONTINUE offer on Home, or null. Since feat(home-simple) (#300) CONTINUE is the one primary CTA
        /// (home-primary reading CONTINUE, <see cref="MainMenuOverlay.CtaIsContinue"/>); the separate home-continue button
        /// (<see cref="HomeResumeBar.ContinueName"/>) only appears where no Home CTA can take it.</summary>
        public static VisualElement Continue(VisualElement root)
        {
            if (root == null) return null;
            var separate = root.Q(HomeResumeBar.ContinueName);
            if (separate != null) return separate;
            var primary = root.Q(UiIds.HomePrimary);
            return primary != null && MainMenuOverlay.CtaIsContinue(root) ? primary : null;
        }

        public static bool Shown(VisualElement e)
        {
            for (var v = e; v != null; v = v.parent)
                if (v.resolvedStyle.display == DisplayStyle.None || v.ClassListContains("hidden")) return false;
            return e != null;
        }
    }

    public class NavFlowPlayTests
    {
        string _errors;
        string _saves;
        string _parked;
        SceneFlow flow;

        [UnitySetUp]
        public IEnumerator FreshDevice()
        {
            _errors = "";
            Application.logMessageReceived += OnLog;
            // Park any save so "fresh install" really is empty prefs and save; restored in teardown.
            _saves = CreditsPlay.SaveDir();
            _parked = null;
            if (Directory.Exists(_saves))
            {
                _parked = _saves + ".nav-parked-" + Guid.NewGuid().ToString("N");
                Directory.Move(_saves, _parked);
            }
            GameSession.Clear();
            PadHandoff.Clear();
            // A shell left by an earlier fixture would survive Boot with its Home view gone (see MenuPlayWalkTests).
            if (AppShell.Instance != null)
            {
                UnityEngine.Object.Destroy(AppShell.Instance.gameObject);
                yield return null;
            }
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Application.logMessageReceived -= OnLog;
            Time.timeScale = 1f;
            Kestrel.Game.Failure.FailureHost.ReduceMotionOverride = null;
            GameSession.Clear();
            PadHandoff.Clear();
            if (AppShell.Instance != null) UnityEngine.Object.Destroy(AppShell.Instance.gameObject);
            yield return null;
            try
            {
                if (Directory.Exists(_saves)) Directory.Delete(_saves, true);
                if (_parked != null && Directory.Exists(_parked)) Directory.Move(_parked, _saves);
            }
            catch (Exception) { }
        }

        void OnLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Exception) _errors += condition + "\n";
        }

        static void Click(VisualElement e)
        {
            Assert.IsNotNull(e, "control");
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = e;
                e.SendEvent(submit);
            }
        }

        IEnumerator Boot()
        {
            SceneManager.LoadScene(SceneFlow.BootScene, LoadSceneMode.Single);
            for (int i = 0; i < 600 && (flow == null || flow.Busy || flow.Current != AppScreen.Home); i++)
            {
                if (flow == null) flow = UnityEngine.Object.FindAnyObjectByType<SceneFlow>();
                yield return null;
            }
            Assert.IsNotNull(flow, "Boot owns a scene flow");
            Assert.AreEqual(AppScreen.Home, flow.Current, "Boot lands on Home");
        }

        IEnumerator Until(Func<bool> done, float seconds = 60f)
        {
            float start = Time.realtimeSinceStartup;
            for (int i = 0; i < 20000 && !done() && Time.realtimeSinceStartup - start < seconds; i++)
                yield return null;
        }

        IEnumerator At(AppScreen screen, string overlay = null)
        {
            yield return Until(() => !flow.Busy && flow.Current == screen && (overlay == null || flow.Overlay == overlay));
            Assert.IsFalse(flow.Busy, "scene flow settled");
            Assert.AreEqual(screen, flow.Current);
            if (overlay != null) Assert.AreEqual(overlay, flow.Overlay);
        }

        static VisualElement Shell => AppShell.Instance != null ? AppShell.Instance.Root : null;

        static VisualElement SliceUi()
        {
            var app = UnityEngine.Object.FindAnyObjectByType<SliceApp>();
            var doc = app != null ? app.GetComponent<UIDocument>() : null;
            return doc != null ? doc.rootVisualElement : null;
        }

        static VisualElement Find(string id) => NavIds.Find(id, Shell, SliceUi());

        void NothingThrew() => Assert.IsEmpty(_errors, "exceptions logged on the way");

        static void Ignore(string id, string expected, string observed)
        {
            if (observed != null) Assert.Ignore("KNOWN BUG " + id + " (NAV_BUGS.md). Expected: " + expected + ". Observed: " + observed);
            Assert.Fail("KNOWN BUG " + id + " no longer reproduces. Remove the Ignore and close it in NAV_BUGS.md.");
        }

        // ---------------------------------------------------------------- fresh install

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator FreshInstall_HomeSettingsMissionsBriefingDesignerAndBackOut()
        {
            yield return Boot();
            Assert.IsTrue(NavIds.Shown(NavIds.Find("home-primary", Shell)), "Home shows its one primary CTA");
            Assert.IsFalse(NavIds.Shown(NavIds.Continue(Shell)), "fresh install offers no CONTINUE");

            Click(NavIds.Find("home-settings", Shell));
            yield return null;
            var sheet = NavIds.Find("screen-settings", Shell);
            Assert.AreEqual(DisplayStyle.Flex, sheet.style.display.value, "Settings opens");
            Click(NavIds.Find("settings-close", Shell));
            yield return null;
            Assert.AreEqual(DisplayStyle.None, sheet.style.display.value, "Settings closes");
            Assert.AreEqual(AppScreen.Home, flow.Current);

            Click(NavIds.Find("home-missions", Shell));
            yield return At(AppScreen.Missions);
            Assert.IsNotNull(NavIds.Find("screen-missions", Shell), "Missions lands on the board");

            flow.StartMission("m1");
            yield return At(AppScreen.Missions, "Briefing");
            Assert.IsNotNull(NavIds.Find("screen-briefing", Shell), "a mission opens its briefing");
            Click(NavIds.Find("briefing-back", Shell));
            yield return At(AppScreen.Missions, "None");

            flow.StartMission("m1");
            yield return At(AppScreen.Missions, "Briefing");
            Click(NavIds.Find("briefing-build", Shell));
            yield return At(AppScreen.Design);
            Assert.IsNotNull(Find("starter-launch"), "the designer has a launch control");

            flow.BackFromDesigner();
            yield return At(AppScreen.Missions, "Briefing");
            Click(NavIds.Find("briefing-back", Shell));
            yield return At(AppScreen.Missions, "None");
            Click(NavIds.Find("missions-back", Shell));
            yield return At(AppScreen.Home);
            NothingThrew();
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator Escape_FromEveryMenuScreen_StepsBack()
        {
            yield return Boot();
            Click(NavIds.Find("home-settings", Shell));
            yield return null;
            AppShell.Instance.HandleEscape();
            yield return null;
            Assert.AreEqual(DisplayStyle.None, NavIds.Find("screen-settings", Shell).style.display.value, "Escape closes Settings");

            flow.HomePressed("missions");
            yield return At(AppScreen.Missions);
            flow.StartMission("m1");
            yield return At(AppScreen.Missions, "Briefing");
            AppShell.Instance.HandleEscape();
            yield return At(AppScreen.Missions, "None");
            AppShell.Instance.HandleEscape();
            yield return At(AppScreen.Home);
            NothingThrew();
        }

        /// <summary>NAV-09 (fixed by #301: the Starter screen's ◀ Back, starter-back, runs SceneFlow.BackFromDesigner).
        /// The designer's back control returns to the mission's briefing and keeps the mission, not Home with no session.</summary>
        [UnityTest]
        [Timeout(240000)]
        public IEnumerator NAV09_DesignerBack_ReturnsToTheBriefing()
        {
            yield return Boot();
            flow.HomePressed("missions");
            yield return At(AppScreen.Missions);
            flow.StartMission("m2");
            yield return At(AppScreen.Missions, "Briefing");
            flow.BuildFromBriefing();
            yield return At(AppScreen.Design);
            var back = Find("starter-back");
            Assert.IsNotNull(back, "the designer shows a way out");
            Click(back);
            yield return Until(() => !flow.Busy && (flow.Current != AppScreen.Design));
            NothingThrew();
            string where = flow.Current + "/" + flow.Overlay + ", session=" + (GameSession.Current?.MissionId ?? "none");
            Assert.AreEqual(AppScreen.Missions, flow.Current, "the designer's back control returns to the board, not Home: " + where);
            Assert.AreEqual("Briefing", flow.Overlay, "the designer's back control reopens the briefing: " + where);
            Assert.AreEqual("m2", GameSession.Current?.MissionId, "the mission is kept (KH-2): " + where);
        }

        // ---------------------------------------------------------------- existing save

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator ExistingSave_Continue_OpensTheSavedMission()
        {
            Directory.CreateDirectory(_saves);
            var store = new Kestrel.Core.Save.NativeStore(_saves, () => 1000, () => 0.5);
            // The fixture is the KH-3 rocket the game itself builds (HomeRoutes.DesignFirst on the device's
            // Resources catalog), saved the way the designer autosaves it. A stageless stub is not a save
            // the game can write: it threw in Stack.BaseCd when the designer opened it (GameCI run 118345963804).
            var text = Resources.Load<TextAsset>("KestrelData/campaign");
            Assert.IsNotNull(text, "Resources/KestrelData/campaign is synced");
            var campaign = Kestrel.Core.Flight.Campaign.FromJson(MiniJson.ParseObject(text.text));
            Assert.IsNotNull(SliceData.Hangar, "designer templates are synced");
            var built = HomeRoutes.DesignFirst(campaign, 2, SliceData.Catalog, SliceData.Hangar.templates);
            Assert.AreEqual("m3", built.Session?.MissionId, "two cleared: the starter is for KH-3");
            Assert.IsTrue(PadHandoff.TryTake(out var draft, out _), "DesignFirst armed the KH-3 rocket");
            Assert.Greater(draft.Arr("stages")?.Count ?? 0, 0, "the fixture is a real rocket");
            new Kestrel.Core.Hangar.DesignLibrary(store).SaveDraft(draft, "Hopper", "m3");
            GameSession.Clear();
            PadHandoff.Clear();
            yield return Boot();
            var cont = NavIds.Continue(Shell);
            Assert.IsTrue(NavIds.Shown(cont), "an existing save offers CONTINUE");
            Click(cont);
            yield return At(AppScreen.Design);
            Assert.AreEqual("m3", GameSession.Current?.MissionId, "CONTINUE resumes KH-3");
            NothingThrew();
        }

        // ---------------------------------------------------------------- live flight

        IEnumerator ToThePad(Action<Design> spoil)
        {
            yield return Boot();
            flow.HomePressed("launch");
            yield return At(AppScreen.Missions, "Briefing");
            if (spoil != null)
            {
                Assert.IsTrue(PadHandoff.TryTake(out var json, out _), "Home LAUNCH armed a rocket");
                var d = Design.FromJson(json);
                spoil(d);
                PadHandoff.Arm(d.ToJson(), flyNow: true, fromHome: true);
            }
            flow.BuildFromBriefing();
            SliceApp app = null;
            yield return Until(() => !flow.Busy && flow.Current == AppScreen.Design && (app = UnityEngine.Object.FindAnyObjectByType<SliceApp>()) != null);
            Assert.IsNotNull(app);
            if (!app.FlightHudVisible) app.PressLaunch();
            var driver = UnityEngine.Object.FindAnyObjectByType<FlightDriver>();
            Assert.IsNotNull(driver, "live FlightDriver");
            yield return Until(() => driver.Sim != null && driver.Running, 90f);
            Assert.IsTrue(driver.Running, "the count reached liftoff (counting=" + app.CountingForCapture + " held=" + driver.Held
                + " sim=" + (driver.Sim != null) + " timeScale=" + Time.timeScale + " hold=" + SliceApp.HoldCountForCapture + ")");
        }

        /// <summary>
        /// Home LAUNCH on a fresh install is KH-1, whose winner already has one engine. Setting engineCount=1 alone
        /// leaves a hop that flies to MECO without a failure (~minutes wall-clock under CI load), so the report wait
        /// times out (PlayMode #458 OneMoreLaunch, and the same flake on docs #457 FixIt). Drop the safety factor so
        /// the airframe breaks up promptly, matching the climax-coast PlayMode spoil.
        /// </summary>
        static void SpoilForBreakUp(Design d)
        {
            d.stages[0].engineCount = 1;
            d.safetyFactor = 1.0;
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator CrashingRocket_LiveFlight_ReachesTheReport_WithRetry()
        {
            yield return ToThePad(SpoilForBreakUp);
            VisualElement report = null;
            yield return Until(() => (report = Find("screen-report")) != null && NavIds.Shown(report), 150f);
            Assert.IsNotNull(report, "a rocket that breaks up still reaches the report");
            Assert.IsNotNull(Find("report-retry"), "the report offers Retry");
            Assert.IsNotNull(Find("report-home"), "the report offers Home");
            Assert.IsNotNull(Find("report-fix"), "the report offers the designer (Fix it)");
            Click(Find("report-retry"));
            yield return At(AppScreen.Design);
            NothingThrew();
        }

        // ---------------------------------------------------------------- one more launch

        static IEnumerator RelaunchFrame(string name)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            var d = new FlightCapture.Device { Name = name, W = Math.Max(64, Screen.width), H = Math.Max(64, Screen.height) };
            int distinct = 0;
            var png = FlightCapture.Png(d, FlightCapture.OutDir(""), name, n => distinct = n);
            while (png.MoveNext()) yield return png.Current;
            Assert.That(distinct, Is.GreaterThan(8), name + " capture is flat");
        }

        // ---------------------------------------------------------------- the climax and the break-up show

        static IEnumerator Frame(string name)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            var d = new FlightCapture.Device { Name = name, W = Math.Max(64, Screen.width), H = Math.Max(64, Screen.height) };
            int distinct = 0;
            var png = FlightCapture.Png(d, FlightCapture.OutDir(""), name, n => distinct = n);
            while (png.MoveNext()) yield return png.Current;
            Assert.That(distinct, Is.GreaterThan(8), name + " capture is flat");
        }

        /// <summary>The count after One more launch / Restart starts from a clean pad: nothing of the last flight is left.</summary>
        static void AssertBackOnThePad(SliceApp app, FlightDriver driver, string how)
        {
            Assert.IsTrue(app.FlightHudVisible, how + ": the flight HUD is up for the count");
            Assert.IsTrue(app.CountingForCapture, how + ": the count is running");
            Assert.IsFalse(driver.Running, how + ": the last flight is not still flying under the count");
            Assert.IsNull(driver.Result, how + ": the last result is dropped (no replayed card)");
            Assert.AreEqual(0.0, driver.Altitude, 1e-6, how + ": altitude back to the pad");
            Assert.AreEqual(0.0, driver.Downrange, 1e-6, how + ": downrange back to the pad");
            Assert.AreEqual(0.0, driver.OriginX, 1e-6, how + ": floating origin back at the pad");
            Assert.AreEqual(0.0, driver.OriginY, 1e-6, how + ": floating origin back at the pad");
            Assert.IsTrue(driver.AnchorsAtOrigin(), how + ": ground, deck and tower are where the pad is");
            Assert.That(app.VehicleForCapture.localPosition.magnitude, Is.LessThan(0.01f), how + ": the rocket is on the pad");
            if (driver.Vehicle != null)
                Assert.That(driver.Vehicle.localPosition.magnitude, Is.LessThan(0.01f), how + ": the flight vehicle is on the pad");
            var cam = app.CameraForCapture;
            Assert.IsNotNull(cam, "flight camera");
            Assert.That(Vector3.Distance(cam.transform.position, app.VehicleForCapture.position), Is.LessThan(600f),
                how + ": the camera looks at the pad, not the sky the last flight left it in");
            var exit = Find(UiIds.FlightExit);
            Assert.IsTrue(exit == null || exit.ClassListContains("hidden"), how + ": no stale BACK TO DESIGN");
            Assert.IsFalse(ReportShownAfterRelaunch(), how + ": no report card over the count");
        }

        static bool ReportShownAfterRelaunch() { var r = Find("screen-report"); return r != null && NavIds.Shown(r); }

        IEnumerator LaunchesFresh(FlightDriver driver, string how)
        {
            yield return Until(() => driver.Sim != null && driver.Running, 90f);
            Assert.IsTrue(driver.Running, how + ": the count reaches liftoff again");
            Assert.That(driver.SimTime, Is.LessThan(5.0), how + ": a new flight from T+0, not the old one resumed");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator CrashingRocket_OneMoreLaunch_CountsDownFromACleanPad()
        {
            yield return ToThePad(SpoilForBreakUp);
            var app = UnityEngine.Object.FindAnyObjectByType<SliceApp>();
            var driver = UnityEngine.Object.FindAnyObjectByType<FlightDriver>();
            yield return Until(ReportShownAfterRelaunch, 150f);
            Assert.IsTrue(ReportShownAfterRelaunch(), "the break-up reaches the report");
            Assert.IsNotNull(driver.Result, "the report has a result");
            SliceApp.HoldCountForCapture = true;
            try
            {
                Click(Find("report-retry"));
                for (int i = 0; i < 6; i++) yield return null;
                AssertBackOnThePad(app, driver, "One more launch");
                yield return RelaunchFrame("relaunch-countdown");
            }
            finally { SliceApp.HoldCountForCapture = false; }
            yield return LaunchesFresh(driver, "One more launch");
            NothingThrew();
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator MidFlightRestart_LaunchesANewFlight_FromThePad()
        {
            yield return ToThePad(null);
            var app = UnityEngine.Object.FindAnyObjectByType<SliceApp>();
            var driver = UnityEngine.Object.FindAnyObjectByType<FlightDriver>();
            // Past RebaseDistance the floating origin has moved the pad: the case that left it displaced on relaunch.
            yield return Until(() => driver.Rebases > 0 || !driver.Running, 120f);
            Debug.Log("[Relaunch] restart at alt=" + driver.Altitude.ToString("F0") + " m rebases=" + driver.Rebases);
            Click(Find("flight-pause"));
            yield return null;
            SliceApp.HoldCountForCapture = true;
            try
            {
                Click(Find(UiIds.PauseRestart));
                for (int i = 0; i < 6; i++) yield return null;
                Assert.IsFalse(NavIds.Shown(Find("screen-pause")), "Restart closes the pause menu");
                Assert.AreEqual(1f, Time.timeScale, "Restart unfreezes time");
                AssertBackOnThePad(app, driver, "Restart");
                yield return RelaunchFrame("relaunch-restart-countdown");
            }
            finally { SliceApp.HoldCountForCapture = false; }
            yield return LaunchesFresh(driver, "Restart");
            NothingThrew();
        }

        /// <summary>
        /// UX fun review 2026-11-03 #1 / QA #417: Fix it after a break-up rebuilt the rocket, the new FailureHost
        /// re-reported the old crash and replayed it on the pad with the designer hidden (empty sky, no buttons).
        /// Fix it must land in the designer and stay there, with no card coming back and no fallback needed.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator CrashingRocket_FixIt_LandsInTheDesigner_AndStays()
        {
            yield return ToThePad(SpoilForBreakUp);
            VisualElement report = null;
            yield return Until(() => (report = Find("screen-report")) != null && NavIds.Shown(report), 150f);
            Assert.IsNotNull(report, "a rocket that breaks up reaches the report");
            var fix = Find("report-fix");
            Assert.IsNotNull(fix, "the report offers Fix it");
            var app = UnityEngine.Object.FindAnyObjectByType<SliceApp>();
            Click(fix);
            // Past the landing grace: the old bug re-opened the card a frame later and stayed stuck for 35 s.
            float until = Time.realtimeSinceStartup + Kestrel.Core.Flight.DesignerLanding.GraceSeconds + 1.5f;
            bool cardCameBack = false;
            while (Time.realtimeSinceStartup < until)
            {
                var again = Find("screen-report");
                if (again != null && NavIds.Shown(again)) cardCameBack = true;
                yield return null;
            }
            Assert.IsFalse(cardCameBack, "Fix it re-opened the report (stale result re-reported by the rebuilt rocket)");
            Assert.AreEqual(AppScreen.Design, flow.Current, "Fix it stays on the Design screen");
            Assert.IsFalse(app.FlightHudVisible, "the flight HUD is closed");
            var design = SliceUi()?.Q("design");
            Assert.IsNotNull(design, "the design sheet exists");
            Assert.IsFalse(design.ClassListContains("hidden"), "the designer is on screen after Fix it");
            Assert.AreEqual(0, app.ForcedDesignerLandings, "Fix it landed on its own; the fallback did not have to fire");
            var driver = UnityEngine.Object.FindAnyObjectByType<FlightDriver>();
            Assert.IsTrue(driver == null || !driver.Running, "Fix it does not launch");
            var marker = GameObject.Find("FailureMarker");
            Assert.IsTrue(marker == null || !marker.activeInHierarchy, "no break-up replay on the pad");
            NothingThrew();
        }

        /// <summary>
        /// #431: the ascent dock ("Bronze medal / i / Auto-assist on") never sits over Follow / Auto / Pause. It folds away
        /// for the climax coast and the loss show (FlightHudLayout.DockShown); <paramref name="folded"/> requires that.
        /// </summary>
        static void DockClearOfTheToolsRow(string where, bool folded)
        {
            var ui = SliceUi();
            var tools = ui?.Q("flight-tools");
            Assert.IsNotNull(tools, where + ": the tools row");
            var dock = ui.Q("ascent-actions");
            bool up = dock != null && dock.resolvedStyle.display != DisplayStyle.None && dock.worldBound.height > 0.5f;
            if (folded) Assert.IsFalse(up, where + ": the dock folds away");
            if (!up) return;
            Assert.IsFalse(dock.worldBound.Overlaps(tools.worldBound), where + ": dock " + dock.worldBound + " over the tools row " + tools.worldBound);
        }

        /// <summary>
        /// #431: the failure callout ("BREAKUP Air pushed…") sits between the stage banner and Follow / Auto / Pause, and its
        /// text fits its box (FlightHudLayout.CalloutInBand). At 94e79369 it sat over the tools row.
        /// </summary>
        static void CalloutInTheBand()
        {
            var ui = SliceUi();
            var chip = ui?.Q<Label>("failure-callout");
            if (chip == null || chip.resolvedStyle.display == DisplayStyle.None) return;
            var c = chip.worldBound;
            var tools = ui.Q("flight-tools");
            Assert.IsNotNull(tools, "the tools row");
            Assert.That(c.yMax, Is.LessThanOrEqualTo(tools.worldBound.yMin + 0.5f), "the callout " + c + " clears the tools row " + tools.worldBound);
            foreach (var id in new[] { "webcast", "ascent-stages", "ascent-telemetry" })
            {
                var e = ui.Q(id);
                if (e == null || e.resolvedStyle.display == DisplayStyle.None || !(e.worldBound.height > 0.5f)) continue;
                Assert.IsFalse(c.Overlaps(e.worldBound), "the callout " + c + " clears " + id + " " + e.worldBound);
            }
            Assert.That(TextFit.Problems(chip), Is.Empty, "the callout's text fits its box: '" + chip.text + "'");
        }

        static bool ReportShown() { var r = Find("screen-report"); return r != null && NavIds.Shown(r); }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator GoodRocket_FirstHop_CoastsToApogee_BeforeTheReport()
        {
            yield return ToThePad(null);
            var driver = UnityEngine.Object.FindAnyObjectByType<FlightDriver>();
            driver.ReduceMotionOverride = false;   // the CI player keeps the comfort prefs on (fast frames); this test is about the motion
            yield return Until(() => driver.Coasting || driver.Result != null, 150f);
            Assert.IsTrue(driver.Coasting, "the clean First Hop cutoff starts the climax (CoastEnd=" + driver.CoastEnd + ")");
            Assert.IsNull(driver.Result, "the result waits for the coast");
            Assert.IsTrue(driver.Running, "the flight is still on screen");
            Assert.IsFalse(driver.Coast.IsOrbit, "a hop coasts to its apex");
            Assert.AreEqual(OrbitCoast.CalloutApogee, driver.Coast.Callout);
            Assert.That(driver.Coast.WallSeconds, Is.InRange(OrbitCoast.MinWallSeconds, OrbitCoast.MaxWallSeconds));
            double startAlt = driver.Altitude;
            yield return Until(() => driver.CoastWall >= 1.5 || !driver.Coasting, 15f);
            Assert.IsTrue(driver.Coasting, "still coasting 1.5 s in");
            driver.Hold(true);
            for (int i = 0; i < 4; i++) yield return null;
            Assert.That(driver.Altitude, Is.GreaterThan(startAlt), "the coast climbs on the propagated arc");
            var verdict = SliceUi()?.Q<Label>("h-verdict");
            Assert.IsNotNull(verdict, "flight HUD verdict");
            Assert.AreEqual(OrbitCoast.CalloutApogee, verdict.text, "the HUD calls the climax");
            Assert.IsFalse(ReportShown(), "no card during the coast");
            var exit = SliceUi()?.Q<Button>(UiIds.FlightExit);
            Assert.IsTrue(exit == null || exit.ClassListContains("hidden") || exit.resolvedStyle.display == DisplayStyle.None
                || exit.resolvedStyle.visibility == Visibility.Hidden,
                "BACK TO DESIGN stays off the climax");
            DockClearOfTheToolsRow("climax", true);
            // The stack sits between the Apogee! banner and Follow / Auto / Pause (FitBand over FlightHudLayout.CoastBand);
            // at 1085e8b7 / 38e8e1b5 it was mostly behind them.
            var app = UnityEngine.Object.FindAnyObjectByType<SliceApp>();
            var band = app.CoastTrace;
            Assert.That(band.y - band.x, Is.GreaterThan(0.1f), "a coast band between the banner and the tools row: " + band);
            Assert.That(band.z, Is.GreaterThanOrEqualTo(band.x - 0.01f), "the stack's base clears Follow / Auto / Pause: " + band);
            Assert.That(band.w, Is.LessThanOrEqualTo(band.y + 0.01f), "the nose clears the Apogee! banner: " + band);
            Assert.That(band.w - band.z, Is.GreaterThan(0.05f), "the stack is clearly there: " + band);
            yield return Frame("climax-apogee");
            driver.Hold(false);
            yield return Until(() => driver.Result != null, 15f);
            Assert.IsNotNull(driver.Result, "the coast ends and hands over the result");
            Assert.AreEqual("played", driver.CoastEnd);
            Assert.That(driver.Altitude, Is.EqualTo(driver.Coast.EndAltM).Within(Math.Max(50.0, driver.Coast.EndAltM * 0.02)), "it ends at the apex");
            yield return Until(ReportShown, 30f);
            Assert.IsTrue(ReportShown(), "the report opens after the climax");
            yield return Frame("climax-apogee-report");
            NothingThrew();
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator GoodRocket_TapSkipsTheClimax()
        {
            yield return ToThePad(null);
            var driver = UnityEngine.Object.FindAnyObjectByType<FlightDriver>();
            driver.ReduceMotionOverride = false;   // the CI player keeps the comfort prefs on (fast frames); this test is about the motion
            yield return Until(() => driver.Coasting || driver.Result != null, 150f);
            Assert.IsTrue(driver.Coasting, "the climax starts");
            yield return null;
            driver.SkipCoast();   // what a tap does (FlightDriver.TappedToSkip)
            Assert.AreEqual("skipped", driver.CoastEnd);
            Assert.IsNotNull(driver.Result, "a skip hands over the result at once");
            Assert.IsFalse(driver.Running);
            Assert.That(driver.Altitude, Is.EqualTo(driver.Coast.EndAltM).Within(Math.Max(50.0, driver.Coast.EndAltM * 0.02)), "a skip lands on the end of the coast");
            yield return Until(ReportShown, 30f);
            Assert.IsTrue(ReportShown(), "the report opens after a skip");
            NothingThrew();
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator GoodRocket_ReduceMotion_HasNoTimeWarp()
        {
            yield return ToThePad(null);
            var driver = UnityEngine.Object.FindAnyObjectByType<FlightDriver>();
            driver.ReduceMotionOverride = true;
            bool coasted = false;
            yield return Until(() => { coasted |= driver.Coasting; return driver.Result != null; }, 150f);
            Assert.IsNotNull(driver.Result, "the flight ends");
            Assert.IsFalse(coasted, "Reduce Motion: no warped coast");
            Assert.AreEqual("reduced", driver.CoastEnd);
            yield return Until(ReportShown, 30f);
            Assert.IsTrue(ReportShown(), "the report opens straight away");
            NothingThrew();
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator CrashingRocket_PlaysTheBreakUp_BeforeTheCard()
        {
            // The CI player keeps the comfort prefs on (fast frames); this test is about the break-up show, so turn the motion
            // on for live failures before the launch (a one-engine stack can fail on the pad). Teardown clears it.
            Kestrel.Game.Failure.FailureHost.ReduceMotionOverride = false;
            // One engine alone no longer breaks the KH-1 hop (the CI profile is on m1: it flies suborbital to MECO, so
            // there was no break-up to play; run 41781261062). Strength 1.0 (the designer's minimum, so Launch is not
            // blocked) tears it at max-Q on every seed of m1-m5 (FlightBatch probe).
            yield return ToThePad(d => { d.stages[0].engineCount = 1; d.safetyFactor = 1.0; });
            Kestrel.Game.Failure.FailureHost host = null;
            yield return Until(() => (host = UnityEngine.Object.FindAnyObjectByType<Kestrel.Game.Failure.FailureHost>()) != null && host.CardPending || ReportShown(), 150f);
            Assert.IsNotNull(host, "the rocket has a failure host");
            Assert.IsTrue(host.CardPending, "the break-up plays before its card (not a hard cut)");
            Assert.IsFalse(ReportShown(), "no card yet");
            float start = Time.realtimeSinceStartup;
            yield return Until(() => Time.realtimeSinceStartup - start >= 0.6f || !host.CardPending, 5f);
            if (host.CardPending)
            {
                DockClearOfTheToolsRow("break-up", false);
                CalloutInTheBand();
                yield return Frame("breakup-live-show");
            }
            yield return Until(ReportShown, 15f);
            Assert.IsTrue(ReportShown(), "the card opens once the break-up has played");
            Assert.IsFalse(host.CardPending);
            NothingThrew();
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator PauseResume_ThenQuitMidFlight_ThenContinue()
        {
            yield return ToThePad(null);
            var driver = UnityEngine.Object.FindAnyObjectByType<FlightDriver>();
            Click(Find("flight-pause"));
            yield return null;
            Assert.IsTrue(NavIds.Shown(Find("screen-pause")), "Pause opens the pause menu");
            Click(Find("pause-resume"));
            yield return null;
            Assert.IsFalse(NavIds.Shown(Find("screen-pause")), "Resume closes it");
            Assert.IsTrue(driver == null || driver.Running, "Resume keeps flying");

            Click(Find("flight-pause"));
            yield return null;
            Click(Find("pause-exit-home"));
            yield return At(AppScreen.Home);
            Assert.AreEqual(1f, Time.timeScale, "Exit to Home unfreezes time");
            var cont = NavIds.Find("home-primary", Shell);
            Assert.IsTrue(NavIds.Shown(cont), "Home has a primary way back in");
            Click(cont);
            yield return Until(() => !flow.Busy && flow.Current != AppScreen.Home);
            Assert.AreNotEqual(AppScreen.Home, flow.Current, "Home's primary button goes somewhere");
            NothingThrew();
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator KnownBug_NAV08_EscapeInFlight_DoesNotPause()
        {
            yield return ToThePad(null);
            AppShell.Instance.HandleEscape();
            yield return null;
            NothingThrew();
            bool paused = NavIds.Shown(Find("screen-pause"));
            Ignore("NAV-08", "Escape in flight opens the pause menu", paused ? null : "no pause menu; screen=" + flow.Current + "/" + flow.Overlay);
        }
    }
}
