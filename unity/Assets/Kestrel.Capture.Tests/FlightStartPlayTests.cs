// PlayMode: Boot -> Home LAUNCH -> briefing -> To the pad -> designer launch -> flight.
// Asserts the sim is running and altitude clears 100 m. A stuck T+0:00 / COUNT fails this.
using System.Collections;
using Kestrel.Game.Shell;
using Kestrel.Slice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Kestrel.Capture.Tests
{
    public class FlightStartPlayTests
    {
        [UnityTearDown]
        public IEnumerator DropTheShell()
        {
            if (AppShell.Instance != null)
                UnityEngine.Object.Destroy(AppShell.Instance.gameObject);
            yield return null;
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator HomeLaunchReachesAltitude()
        {
            SceneManager.LoadScene(SceneFlow.BootScene, LoadSceneMode.Single);
            SceneFlow flow = null;
            for (int i = 0; i < 600 && (flow == null || flow.Busy || flow.Current != AppScreen.Home); i++)
            {
                if (flow == null) flow = UnityEngine.Object.FindAnyObjectByType<SceneFlow>();
                yield return null;
            }
            Assert.IsNotNull(flow, "Boot must own a scene flow");
            Assert.AreEqual(AppScreen.Home, flow.Current);
            Assert.IsFalse(flow.Busy);

            flow.HomePressed("launch");
            for (int i = 0; i < 600 && (flow.Busy || flow.Overlay != "Briefing"); i++)
                yield return null;
            Assert.AreEqual("Briefing", flow.Overlay, "LAUNCH opens the briefing");
            Assert.IsFalse(flow.Busy);

            flow.BuildFromBriefing();
            SliceApp app = null;
            for (int i = 0; i < 600 && (flow.Busy || flow.Current != AppScreen.Design || app == null); i++)
            {
                app = UnityEngine.Object.FindAnyObjectByType<SliceApp>();
                yield return null;
            }
            Assert.AreEqual(AppScreen.Design, flow.Current, "To the pad loads the designer");
            Assert.IsNotNull(app);
            Assert.IsFalse(flow.Busy);
            // Home auto-build is already in the count. The designer path still has a launch control.
            if (!app.FlightHudVisible) app.PressLaunch();

            var flight = UnityEngine.Object.FindAnyObjectByType<FlightDriver>();
            Assert.IsNotNull(flight, "flight driver");
            bool climbed = false;
            float started = Time.realtimeSinceStartup;
            // The count opens at T-12. A fast player needs many frames; a slow one needs wall clock.
            for (int i = 0; i < 12000 && Time.realtimeSinceStartup - started < 90f; i++)
            {
                if (flight.Sim != null && flight.Altitude > 100)
                {
                    climbed = true;
                    break;
                }
                yield return null;
            }
            Assert.IsTrue(climbed,
                "liftoff simT=" + flight.SimTime
                + " running=" + flight.Running
                + " alt=" + flight.Altitude
                + " sim=" + (flight.Sim != null));
        }
    }
}
