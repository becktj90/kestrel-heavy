// Briefing facts: campaign numbers and the seeded S4 card. No Unity scene.
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Game.MissionSelect;
using NUnit.Framework;

namespace Kestrel.Game.Tests
{
    public class BriefingBoardTests
    {
        static string Repo => Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
        static Campaign Load() => Campaign.Load(Repo);

        static readonly string[] Forbidden =
        {
            "you missed", "you failed", "you lost", "limited time", "watch an ad", "streak",
            "daily", " buy", "purchase", "spacex", "nasa", "falcon", "starlink", "soyuz",
            "ariane", "boeing", "blue origin", "ula", "cape canaveral",
        };

        [Test]
        public void ChosenFlightStaysOnTheMapUntilBuildIt()
        {
            var campaign = Load();
            var session = MissionContext.FromCampaign(campaign, "m4");
            var chosen = new RouteResult(SessionScreens.Design, SessionScreens.None, session);
            var held = BriefingBoard.BeforeDesign(chosen);
            Assert.AreEqual(SessionScreens.Missions, held.Screen);
            Assert.AreEqual(SessionScreens.Briefing, held.Overlay);
            Assert.AreSame(session, held.Session);

            var free = BriefingBoard.BeforeDesign(new RouteResult(SessionScreens.Design, SessionScreens.None, MissionContext.Free()));
            Assert.AreEqual(SessionScreens.Design, free.Screen);
            Assert.AreEqual(SessionScreens.None, free.Overlay);

            var bad = new RouteResult(SessionScreens.Missions, SessionScreens.None, session);
            var same = BriefingBoard.BeforeDesign(bad);
            Assert.AreEqual(SessionScreens.Missions, same.Screen);
            Assert.AreEqual(SessionScreens.None, same.Overlay);
            Assert.AreSame(session, same.Session);
        }

        [Test]
        public void EveryMissionShowsTheCampaignRowAndTheSeededCard()
        {
            var campaign = Load();
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var mission = campaign.Missions[i];
                var ctx = MissionContext.FromMission(campaign, mission);
                var card = MissionBriefing.Load(Repo, mission.Index, 0, ctx.Seed);
                ctx.ApplyBriefing(card);
                var board = BriefingBoard.Compose(ctx, card);

                Assert.AreEqual(MissionMapText.TitleLine(ctx.Label, ctx.Name), board.Title, mission.Id);
                Assert.AreEqual(card.Goal, board.Goal, mission.Id);
                Assert.AreEqual(MissionMapText.PayloadLine(mission, new PayloadLabel { Name = card.PayloadName, Kind = card.PayloadKind }), board.Payload, mission.Id);
                StringAssert.Contains(MissionMapText.Kilograms(mission.MassKg), board.Payload, mission.Id);
                Assert.AreEqual(Fit(mission), board.Fit, mission.Id);
                Assert.AreEqual(MissionMapText.OrbitLine(mission), board.Target, mission.Id);
                Assert.AreEqual("Budget " + MissionMapText.Number(mission.BudgetKc) + " Kc", board.Budget, mission.Id);
                Assert.AreEqual("Reward " + MissionMapText.Number(mission.RewardKc) + " Kc", board.Reward, mission.Id);
                Assert.AreEqual("Wind " + card.WindLine, board.Wind, mission.Id);
                Assert.AreEqual(card.TimeLabel + ", hour " + card.Hour.ToString(System.Globalization.CultureInfo.InvariantCulture), board.Time, mission.Id);
                Assert.AreEqual("Pad: " + card.Site.Name, board.Pad, mission.Id);
                Assert.AreEqual("Seed " + card.Replay.Label, board.Seed, mission.Id);
                Assert.AreEqual(card.EngineerLine, board.Engineer, mission.Id);
                Assert.AreEqual(mission.GentleDefault ? "Gentle on." : "Gentle off.", board.Gentle, mission.Id);
                Assert.IsTrue(board.CanBuild, mission.Id);
                Assert.AreEqual(card.Bonuses.Count, board.Bonuses.Count, mission.Id);
                for (int b = 0; b < card.Bonuses.Count; b++)
                    Assert.AreEqual(card.Bonuses[b].Label, board.Bonuses[b], mission.Id);
                CollectionAssert.AreEqual(ExpectRequirements(mission), board.Requirements, mission.Id);
                AssertPlain(Blob(board));
                StringAssert.DoesNotContain("escape", board.Target, mission.Id);
            }
        }

        [Test]
        public void WeatherEyeUsesTheCatalogNumbers()
        {
            var campaign = Load();
            var ctx = MissionContext.FromCampaign(campaign, "m4");
            var card = MissionBriefing.Load(Repo, 4, 0, ctx.Seed);
            ctx.ApplyBriefing(card);
            var board = BriefingBoard.Compose(ctx, card);

            Assert.AreEqual("KH-4  Weather Eye", board.Title);
            Assert.AreEqual("Put a 2,500 kg fictional weather sat in a 500 km, 51.6° orbit.", board.Goal);
            Assert.AreEqual("Tern-3 · Weather sat · 2,500 kg", board.Payload);
            Assert.AreEqual("12 m3 · Diameter 2 m", board.Fit);
            Assert.AreEqual("Circular orbit 500 km, 51.6°", board.Target);
            CollectionAssert.AreEqual(new[] { "Peak g at most 6" }, board.Requirements);
            CollectionAssert.AreEqual(new[] { "Land the booster", "Keep peak g under 4" }, board.Bonuses);
            Assert.AreEqual("Budget 40,000,000 Kc", board.Budget);
            Assert.AreEqual("Reward 40,000,000 Kc", board.Reward);
            Assert.AreEqual("Gentle off.", board.Gentle);
            Assert.AreEqual(BriefingBoard.SeedLabel(ctx.Seed), card.Replay.Label);
            StringAssert.Contains("m/s", board.Engineer);
            StringAssert.Contains("shear", board.Engineer);
        }

        [Test]
        public void FirstHopAndTheLaterContractsKeepTheirOwnConstraints()
        {
            var campaign = Load();
            var m1 = Board(campaign, "m1");
            Assert.AreEqual("KH-1  First Hop", m1.Title);
            Assert.AreEqual("Pebble · Sounding box · 50 kg", m1.Payload);
            Assert.AreEqual("0.2 m3 · Diameter 0.6 m", m1.Fit);
            Assert.AreEqual("Apogee 10 km", m1.Target);
            Assert.AreEqual(0, m1.Requirements.Count);
            CollectionAssert.AreEqual(new[] { "Keep peak g under 6" }, m1.Bonuses);
            Assert.AreEqual("Budget 2,000,000 Kc", m1.Budget);
            Assert.AreEqual("Gentle on.", m1.Gentle);

            var m6 = Board(campaign, "m6");
            CollectionAssert.AreEqual(new[] { "Land back at the pad. Grade good" }, m6.Requirements);
            var m7 = Board(campaign, "m7");
            CollectionAssert.AreEqual(new[] { "Land on Haven. Grade good" }, m7.Requirements);
            Assert.IsTrue(MissionBriefing.Load(Repo, 7, 0, 1).Site.Barge);
            var m8 = Board(campaign, "m8");
            CollectionAssert.AreEqual(new[]
            {
                "Peak g at most 4.5",
                "Reliability at least 0.98",
                "No red risks",
                "Random failures on",
            }, m8.Requirements);
            var m9 = Board(campaign, "m9");
            Assert.AreEqual("C3 -2 km2/s2, closest point 200 km, 28.5°", m9.Target);
            CollectionAssert.AreEqual(new[] { "Coast 3 h" }, m9.Requirements);
            var m10 = Board(campaign, "m10");
            CollectionAssert.AreEqual(new[] { "Land back at the pad. Grade good. Boosters too" }, m10.Requirements);
        }

        [Test]
        public void EmptyContextDoesNotInventAFlight()
        {
            var board = BriefingBoard.Compose(null, null);
            Assert.AreEqual("Briefing", board.Title);
            Assert.AreEqual("", board.Goal);
            Assert.AreEqual("", board.Payload);
            Assert.AreEqual("", board.Wind);
            Assert.AreEqual("", board.Seed);
            Assert.IsFalse(board.CanBuild);
            Assert.AreEqual(0, board.Requirements.Count);
            Assert.AreEqual(0, board.Bonuses.Count);
        }

        [Test]
        public void SourcesStayOfflineAndFictional()
        {
            string dir = Path.Combine(Repo, "unity", "Assets", "Kestrel.Game", "Campaign");
            string blob = File.ReadAllText(Path.Combine(dir, "BriefingBoard.cs"))
                + File.ReadAllText(Path.Combine(dir, "BriefingScreen.cs"))
                + File.ReadAllText(Path.Combine(dir, "Resources", "UI", "Briefing.uss"));
            string lower = blob.ToLowerInvariant();
            foreach (var bad in Forbidden) StringAssert.DoesNotContain(bad, lower);
            StringAssert.DoesNotContain("http", lower);
            StringAssert.DoesNotContain("unitywebrequest", lower);
            StringAssert.DoesNotContain("animation", lower);
        }

        static BriefingBoard Board(Campaign campaign, string id)
        {
            var ctx = MissionContext.FromCampaign(campaign, id);
            var card = MissionBriefing.Load(Repo, campaign.ById(id).Index, 0, ctx.Seed);
            ctx.ApplyBriefing(card);
            return BriefingBoard.Compose(ctx, card);
        }

        static string Fit(CampaignMission mission)
        {
            return MissionMapText.Number(mission.VolumeM3) + " m3 · Diameter " + MissionMapText.Number(mission.DiameterM) + " m";
        }

        static List<string> ExpectRequirements(CampaignMission mission)
        {
            var ctx = new MissionContext();
            if (mission.MarginNonNegative) ctx.Requirements.Add(new MissionRequirement { Id = "margin", Min = 0 });
            if (!double.IsNaN(mission.MaxG)) ctx.Requirements.Add(new MissionRequirement { Id = "maxG", Max = mission.MaxG });
            if (mission.StrapOnsAllowed) ctx.Requirements.Add(new MissionRequirement { Id = "strapOns", Allow = true });
            if (!string.IsNullOrEmpty(mission.LandingMode))
                ctx.Requirements.Add(new MissionRequirement
                {
                    Id = "landing",
                    Mode = mission.LandingMode,
                    Grade = mission.LandingGrade,
                    Site = mission.LandingSite,
                    Boosters = mission.LandBoosters,
                });
            if (!double.IsNaN(mission.ReliabilityMin)) ctx.Requirements.Add(new MissionRequirement { Id = "reliability", Min = mission.ReliabilityMin });
            if (mission.NoRedRisks) ctx.Requirements.Add(new MissionRequirement { Id = "noRedRisks" });
            if (mission.RandomFailures) ctx.Requirements.Add(new MissionRequirement { Id = "randomFailures" });
            if (mission.CoastHours > 0) ctx.Requirements.Add(new MissionRequirement { Id = "coastHours", Hours = mission.CoastHours });
            var lines = new List<string>();
            for (int i = 0; i < ctx.Requirements.Count; i++) lines.Add(BriefingBoard.RequirementLine(ctx.Requirements[i]));
            return lines;
        }

        static string Blob(BriefingBoard board)
        {
            var parts = new List<string>
            {
                board.Title, board.Goal, board.Payload, board.Fit, board.Target, board.Gentle,
                board.Budget, board.Reward, board.Wind, board.Time, board.Pad, board.Seed, board.Engineer,
            };
            parts.AddRange(board.Requirements);
            parts.AddRange(board.Bonuses);
            return string.Join("\n", parts);
        }

        static void AssertPlain(string text)
        {
            string lower = text.ToLowerInvariant();
            foreach (var bad in Forbidden)
            {
                string needle = bad.Trim();
                Assert.IsFalse(Regex.IsMatch(lower, @"\b" + Regex.Escape(needle) + @"\b"), needle + " in " + lower);
            }
        }
    }
}
