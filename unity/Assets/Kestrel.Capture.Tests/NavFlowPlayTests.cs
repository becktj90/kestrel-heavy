// test(nav) PlayMode: the live shell (Boot → SceneFlow → SliceApp → FlightDriver), fresh install and existing save.
// UNVERIFIED LOCALLY: this file compiles against the Unity stubs (tools/cs-stubcheck) but has only run where GameCI runs
// PlayMode. Bad rockets may fail and explode; what must hold is that every screen has a way on and a way out.
// Known bugs on main (NAV_BUGS.md) are Ignored with the observed result while they reproduce and fail once they stop.
using System;
using System.Collections;
using System.IO;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
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
            ("home-continue", new[] { HomeResumeBar.ContinueName }),
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
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Application.logMessageReceived -= OnLog;
            Time.timeScale = 1f;
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
            Assert.IsFalse(NavIds.Shown(NavIds.Find("home-continue", Shell)), "fresh install offers no CONTINUE");

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

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator KnownBug_NAV09_DesignerBack_DropsTheMission()
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
            bool briefing = flow.Current == AppScreen.Missions && flow.Overlay == "Briefing";
            Ignore("NAV-09", "the designer's back control returns to KH-2's briefing", briefing ? null : flow.Current + "/" + flow.Overlay + ", session=" + (GameSession.Current?.MissionId ?? "none"));
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
            var cont = NavIds.Find("home-continue", Shell);
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
            Assert.IsTrue(driver.Running, "the count reached liftoff");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator CrashingRocket_LiveFlight_ReachesTheReport_WithRetry()
        {
            yield return ToThePad(d => d.stages[0].engineCount = 1);
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
