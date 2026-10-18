// PlayMode: the shell's mission board can be asked for every KH id without throwing.
// The open mission (KH-1 on a fresh career) opens the briefing. The headless
// CampaignFlightLoopTests fly the guided ascent for KH-1..KH-20.
using System.Collections;
using Kestrel.Core.Flight;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Kestrel.Capture.Tests
{
    public class CampaignFlightLoopPlayTests
    {
        [UnityTearDown]
        public IEnumerator DropTheShell()
        {
            // LaunchChosen arms the pad and leaves it there. The next PlayMode
            // shot is the workhorse designer, and a leftover hopper replaces it.
            PadHandoff.Clear();
            GameSession.Clear();
            if (AppShell.Instance != null)
                UnityEngine.Object.Destroy(AppShell.Instance.gameObject);
            yield return null;
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator BoardAcceptsEveryMissionId()
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

            flow.HomePressed("missions");
            for (int i = 0; i < 600 && (flow.Busy || flow.Current != AppScreen.Missions); i++)
                yield return null;
            Assert.AreEqual(AppScreen.Missions, flow.Current);
            Assert.IsFalse(flow.Busy);

            for (int n = 1; n <= 20; n++)
            {
                string id = "m" + n;
                Assert.DoesNotThrow(() => flow.LaunchChosen(id), id);
                yield return null;
                Assert.IsFalse(flow.Busy, id);
            }

            flow.LaunchChosen("m1");
            for (int i = 0; i < 600 && (flow.Busy || flow.Overlay != "Briefing"); i++)
                yield return null;
            Assert.AreEqual("Briefing", flow.Overlay, "KH-1 is the open mission on a fresh career");
        }
    }
}
