// EditMode checks for the report / exit routes. The campaign map is MissionSelectTests. No scene load.
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Game.Shell;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class SceneRouteTests
    {
        [Test]
        public void ScreenOrderKeepsHomeDesignFlightAndAddsMissions()
        {
            Assert.AreEqual(AppScreen.Home, (AppScreen)0);
            Assert.AreEqual(AppScreen.Design, (AppScreen)1);
            Assert.AreEqual(AppScreen.Flight, (AppScreen)2);
            Assert.AreEqual(AppScreen.Missions, (AppScreen)3);
            CollectionAssert.AreEqual(new[] { "Home", "Design", "Flight", "Missions" }, SceneFlow.SceneNames);
            Assert.IsTrue(SceneFlow.TryScreen(SessionScreens.Missions, out var screen));
            Assert.AreEqual(AppScreen.Missions, screen);
            Assert.IsTrue(SceneFlow.IsHomeControl("launch"));
            Assert.IsTrue(SceneFlow.IsHomeControl("design"));
            Assert.IsTrue(SceneFlow.IsHomeControl("missions"));
            Assert.IsFalse(SceneFlow.IsHomeControl("codex"));
            Assert.IsFalse(SceneFlow.TryHomeControl("launch", out _));
            Assert.IsTrue(SceneFlow.TryHomeControl("hangar", out var hangar));
            Assert.AreEqual(RouteId.HomeHangar, hangar);
        }

        [Test]
        public void MissionsKeepsTheShellListener()
        {
            Assert.IsTrue(ShellAudio.ShellListenerOn(AppScreen.Missions));
            Assert.IsTrue(ShellAudio.ShellListenerOn(AppScreen.Home));
            Assert.IsTrue(ShellAudio.ShellListenerOn(AppScreen.Flight));
            Assert.IsFalse(ShellAudio.ShellListenerOn(AppScreen.Design));
        }

        [Test]
        public void ReportAndBriefingCardsExposeExitsAt44()
        {
            var ctx = MissionContext.FromCampaign(Campaign.Load(Catalog.FindRepoRoot(System.IO.Directory.GetCurrentDirectory())), "m1");
            var report = ScreenStub.BuildReport(ctx, () => { }, () => { }, () => { }, () => { });
            foreach (var name in new[] { "report-next", "report-retry", "report-missions", "report-home" })
            {
                var b = report.Q<Button>(name);
                Assert.IsNotNull(b, name);
                Assert.IsNotEmpty(b.text, name);
                Assert.IsNotEmpty(b.tooltip, name);
                Assert.IsTrue(b.ClassListContains("kh-button"));
                Assert.AreEqual(44f, b.style.minHeight.value.value);
            }
            StringAssert.Contains("KH-1", report.Q<Label>("report-job").text);
            var brief = ScreenStub.BuildBriefing(ctx, () => { }, () => { });
            Assert.IsNotNull(brief.Q<Button>("briefing-build"));
            Assert.IsNotNull(brief.Q<Button>("briefing-back"));
            Assert.AreEqual(44f, brief.Q<Button>("briefing-build").style.minHeight.value.value);
            var flight = ScreenStub.BuildFlightRoutes(() => { }, () => { }, () => { });
            Assert.IsNotNull(flight.Q<Button>("flight-report"));
            Assert.IsNotNull(flight.Q<Button>("flight-missions"));
            Assert.IsNotNull(flight.Q<Button>("flight-home"));
        }
    }
}
