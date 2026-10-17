// G1: the session carried from a chosen mission through briefing, designer, ascent and report.
using System.IO;
using Kestrel.Core.Flight;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class MissionContextTests
    {
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            GameSession.Clear();
            campaign = Campaign.Load(Golden.Root);
        }

        [TearDown]
        public void Clear() => GameSession.Clear();

        [Test]
        public void BoardIsKh1ThroughKh20FromTheCampaignFile()
        {
            var board = MissionContext.CampaignBoard(campaign);
            Assert.AreEqual(20, board.Count);
            for (int i = 0; i < board.Count; i++)
            {
                Assert.AreEqual("m" + (i + 1), board[i].MissionId);
                Assert.AreEqual("KH-" + (i + 1), board[i].Label);
                Assert.AreEqual(MissionMode.Campaign, board[i].Mode);
                Assert.AreEqual(1UL, board[i].Seed);
                Assert.IsFalse(string.IsNullOrEmpty(board[i].Tooltip()));
            }
            var m1 = board[0];
            Assert.AreEqual("First Hop", m1.Name);
            Assert.AreEqual("kid-box", m1.PayloadId);
            Assert.AreEqual(50, m1.Mass);
            Assert.AreEqual(0.2, m1.Volume);
            Assert.AreEqual(0.6, m1.Diameter);
            Assert.AreEqual("apogee", m1.Target.Kind);
            Assert.AreEqual(10, m1.Target.ApoKm);
            Assert.AreEqual(2000000, m1.BudgetKc);
            Assert.AreEqual(2000000, m1.RewardKc);
            Assert.IsTrue(m1.Gentle);
            Assert.AreEqual(0, m1.Requirements.Count);
            Assert.AreEqual(0, m1.BonusGoals.Count);

            var m8 = board[7];
            Assert.IsFalse(m8.Gentle);
            Assert.AreEqual(4.5, m8.Find("maxG").Max);
            Assert.AreEqual(0.98, m8.Find("reliability").Min);
            Assert.IsNotNull(m8.Find("noRedRisks"));
            Assert.IsNotNull(m8.Find("randomFailures"));
            StringAssert.Contains("max g 4.5", m8.Tooltip());

            var m6 = board[5];
            var land = m6.Find("landing");
            Assert.AreEqual("rtls", land.Mode);
            Assert.AreEqual("good", land.Grade);
            Assert.AreEqual("pad", land.Site);
            Assert.IsFalse(land.Boosters);
            Assert.IsTrue(board[9].Find("landing").Boosters);
            Assert.AreEqual(3, board[8].Find("coastHours").Hours);
            Assert.AreEqual(-2, board[8].Target.C3Km2PerS2);
            Assert.IsNotNull(board[2].Find("margin"));
            Assert.IsTrue(board[4].Find("strapOns").Allow);
        }

        [Test]
        public void AbsentSessionIsAFreeBuildAndHangarSetsOne()
        {
            Assert.IsNull(GameSession.Current);
            Assert.IsTrue(GameSession.IsFreeBuild);
            var card = new MissionBriefing
            {
                WindLine = "4 kt east",
                TimeLabel = "morning",
                Replay = new ReplaySeed(0, 7),
                Site = new LaunchSite { Id = "wisp", Name = "Wisp Pad" },
            };
            card.Bonuses.Add(new BonusGoal { Id = "soft-ride", Label = "Keep peak g under 6" });
            var hangar = GameSession.Apply(RouteId.HomeHangar, briefing: card);
            Assert.AreEqual(SessionScreens.Design, hangar.Screen);
            Assert.AreEqual(SessionScreens.None, hangar.Overlay);
            Assert.AreEqual(MissionMode.Free, hangar.Session.Mode);
            Assert.AreEqual(7UL, hangar.Session.Seed);
            Assert.AreEqual("4 kt east", hangar.Session.Weather);
            Assert.AreEqual("morning", hangar.Session.TimeOfDay);
            Assert.AreEqual("Wisp Pad", hangar.Session.Site);
            Assert.AreEqual("Keep peak g under 6", hangar.Session.BonusGoals[0]);
            Assert.IsTrue(GameSession.IsFreeBuild);
            Assert.AreEqual(SessionScreens.Home, GameSession.Apply(RouteId.DesignerBack).Screen);
            Assert.IsNull(GameSession.Current);
        }

        [Test]
        public void ChosenMissionIsCarriedThroughBriefingAscentReportAndRetry()
        {
            var card = MissionBriefing.Load(Golden.Root, 1, 0, 1);
            var start = GameSession.Apply(RouteId.MissionChosen, campaign, "m1", card);
            Assert.AreEqual(SessionScreens.Design, start.Screen);
            Assert.AreEqual("m1", start.Session.MissionId);
            Assert.AreEqual("KH-1", start.Session.Label);
            Assert.AreEqual(MissionMode.Campaign, start.Session.Mode);
            Assert.AreEqual(card.MissionSeed, start.Session.Seed);
            Assert.AreEqual(card.WindLine, start.Session.Weather);
            Assert.AreEqual(card.Site.Name, start.Session.Site);
            Assert.AreEqual(card.TimeLabel, start.Session.TimeOfDay);
            Assert.AreEqual("Toss a 50 kg box to a 10 km apogee.", start.Session.Goal);
            CollectionAssert.Contains(start.Session.BonusGoals, "Keep peak g under 6");
            StringAssert.Contains("m/s", start.Session.EngineerLine);
            var again = MissionBriefing.Load(Golden.Root, 1, 0, start.Session.Seed);
            Assert.AreEqual(card.WindLine, again.WindLine);
            Assert.AreEqual(card.Site.Name, again.Site.Name);

            var back = GameSession.Apply(RouteId.DesignerBack);
            Assert.AreSame(start.Session, back.Session);
            Assert.AreEqual(SessionScreens.Missions, back.Screen);
            Assert.AreEqual(SessionScreens.Briefing, back.Overlay);
            var build = GameSession.Apply(RouteId.BriefingBuild);
            Assert.AreSame(start.Session, build.Session);
            Assert.AreEqual(SessionScreens.Design, build.Screen);
            Assert.AreEqual(SessionScreens.None, build.Overlay);

            var report = GameSession.Apply(RouteId.OpenReport);
            Assert.AreSame(start.Session, report.Session);
            Assert.AreEqual(SessionScreens.Missions, report.Screen);
            Assert.AreEqual(SessionScreens.Report, report.Overlay);
            ulong seed = report.Session.Seed;
            var retry = GameSession.Apply(RouteId.ReportRetry);
            Assert.AreSame(start.Session, retry.Session);
            Assert.AreEqual(seed, retry.Session.Seed);
            Assert.AreEqual(SessionScreens.Design, retry.Screen);

            GameSession.Apply(RouteId.OpenReport);
            var next = GameSession.Apply(RouteId.ReportNext, campaign);
            Assert.AreEqual("m2", next.Session.MissionId);
            Assert.AreEqual("KH-2", next.Session.Label);
            Assert.AreEqual(seed, next.Session.Seed);
            Assert.AreEqual(SessionScreens.Missions, next.Screen);
            Assert.AreEqual(SessionScreens.None, next.Overlay);

            var home = GameSession.Apply(RouteId.ExitHome);
            Assert.IsNull(home.Session);
            Assert.AreEqual(SessionScreens.Home, home.Screen);
            Assert.IsTrue(GameSession.IsFreeBuild);
        }

        [Test]
        public void LastMissionHasNoFurtherNextAndABadIdDoesNotDropTheCurrentOne()
        {
            GameSession.Apply(RouteId.MissionChosen, campaign, "m20");
            var next = GameSession.Apply(RouteId.ReportNext, campaign);
            Assert.AreEqual("m20", next.Session.MissionId);
            Assert.AreEqual(SessionScreens.Missions, next.Screen);
            var bad = GameSession.Apply(RouteId.MissionChosen, campaign, "nope");
            Assert.AreEqual("m20", bad.Session.MissionId);
            Assert.AreEqual(SessionScreens.Missions, bad.Screen);
            Assert.AreEqual(SessionScreens.Missions, GameSession.Apply(RouteId.ExitMissions).Screen);
            Assert.AreEqual("m20", GameSession.Current.MissionId);
        }

        [Test]
        public void LaunchDoesNotInventAMission()
        {
            var launch = GameSession.Apply(RouteId.HomeLaunch);
            Assert.AreEqual(SessionScreens.Missions, launch.Screen);
            Assert.AreEqual(SessionScreens.None, launch.Overlay);
            Assert.IsNull(launch.Session);
            Assert.IsTrue(File.Exists(Path.Combine(Golden.Root, "data", "campaign.json")));
        }
    }
}
