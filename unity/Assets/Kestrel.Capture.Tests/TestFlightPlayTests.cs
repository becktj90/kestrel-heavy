// Test Flight (DECISIONS #102) PlayMode: Home's Test Flight tile → the designer (a Sandbox free build flagged as a test)
// → a live flight → the Test Flight card → Tweak & Retry, with progress and Kc unchanged.
// UNVERIFIED LOCALLY: compiles against the Unity stubs (tools/cs-stubcheck); runs where GameCI runs PlayMode.
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
    public class TestFlightPlayTests
    {
        string _errors, _saves, _parked;
        SceneFlow flow;

        [UnitySetUp]
        public IEnumerator FreshDevice()
        {
            _errors = "";
            Application.logMessageReceived += OnLog;
            _saves = CreditsPlay.SaveDir();
            _parked = null;
            if (Directory.Exists(_saves))
            {
                _parked = _saves + ".tf-parked-" + Guid.NewGuid().ToString("N");
                Directory.Move(_saves, _parked);
            }
            GameSession.Clear();
            PadHandoff.Clear();
            Time.timeScale = 1f;
            Time.captureDeltaTime = 0f;
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

        IEnumerator Until(Func<bool> done, float seconds = 60f)
        {
            var start = System.Diagnostics.Stopwatch.StartNew();
            while (!done() && start.Elapsed.TotalSeconds < seconds) yield return null;
        }

        static VisualElement Shell => AppShell.Instance != null ? AppShell.Instance.Root : null;

        static VisualElement SliceUi()
        {
            var app = UnityEngine.Object.FindAnyObjectByType<SliceApp>();
            var doc = app != null ? app.GetComponent<UIDocument>() : null;
            return doc != null ? doc.rootVisualElement : null;
        }

        static VisualElement Find(string id) => NavIds.Find(id, Shell, SliceUi());

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator HomeTile_Designer_Flight_Card_TweakAndRetry_ProgressUnchanged()
        {
            SceneManager.LoadScene(SceneFlow.BootScene, LoadSceneMode.Single);
            yield return Until(() => (flow = flow ?? UnityEngine.Object.FindAnyObjectByType<SceneFlow>()) != null && !flow.Busy && flow.Current == AppScreen.Home);
            Assert.AreEqual(AppScreen.Home, flow.Current, "Boot lands on Home");
            int cleared = ScreenStub.ClearedCount();
            long bank = CreditsPlay.Current != null ? CreditsPlay.Current.Balance : 0;

            var tile = NavIds.Find(UiIds.HomeTestFlight, Shell);
            Assert.IsNotNull(tile, "Home shows the Test Flight tile from the first launch");
            Assert.IsTrue(NavIds.Shown(tile));

            // A rocket that breaks up promptly (no safety margin), so the card comes quickly.
            var starter = TestFlightHome.Starter();
            Assert.IsNotNull(starter, "a starter rocket");
            var d = Design.FromJson(starter);
            d.stages[0].engineCount = 1;
            d.safetyFactor = 1.0;
            PadHandoff.Arm(d.ToJson(), false, false);
            Click(tile);
            SliceApp app = null;
            yield return Until(() => !flow.Busy && flow.Current == AppScreen.Design && (app = UnityEngine.Object.FindAnyObjectByType<SliceApp>()) != null);
            Assert.IsNotNull(app, "the tile opens the designer");
            Assert.IsTrue(GameSession.IsTestFlight, "a Test Flight session");
            Assert.IsTrue(GameSession.IsFreeBuild, "on the Sandbox free-build path");
            yield return Until(() => Find(UiIds.TestFlightBand) != null, 10f);
            Assert.IsNotNull(Find(UiIds.TestFlightEyebrow), "the designer shows TEST FLIGHT");

            if (!app.FlightHudVisible) app.PressLaunch();
            var driver = UnityEngine.Object.FindAnyObjectByType<FlightDriver>();
            Assert.IsNotNull(driver, "live FlightDriver");
            VisualElement card = null;
            yield return Until(() => (card = Find(UiIds.TestRetry)) != null && NavIds.Shown(card), 180f);
            Assert.IsNotNull(card, "the test flight reaches its card");
            Assert.IsNotNull(Find(UiIds.TestCardTitle));
            Assert.IsNotNull(Find(UiIds.TestNewDesign));
            Assert.IsNotNull(Find(UiIds.TestHome));
            Assert.IsNull(Find(UiIds.ReportNext), "no Next mission on a test");

            Click(Find(UiIds.TestRetry));
            yield return Until(() => !ReportShown(), 10f);
            Assert.IsFalse(ReportShown(), "Tweak & Retry leaves the card on the first tap");
            Assert.AreEqual(AppScreen.Design, flow.Current);
            Assert.IsTrue(GameSession.IsTestFlight, "still a test");
            Assert.AreEqual(cleared, ScreenStub.ClearedCount(), "no mission progress");
            Assert.AreEqual(bank, CreditsPlay.Current != null ? CreditsPlay.Current.Balance : 0, "no Kc spent or earned");
            Assert.IsEmpty(_errors, "exceptions logged on the way");
        }

        static bool ReportShown() { var r = Find("screen-report"); return r != null && NavIds.Shown(r); }
    }
}
