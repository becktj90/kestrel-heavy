// Home LAUNCH picks a starter for the next mission and routes through the briefing.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class HomeAutoBuildTests
    {
        Catalog catalog;
        HangarData hangar;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            GameSession.Clear();
            PadHandoff.Clear();
            catalog = Catalog.LoadFromRepo(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        [TearDown]
        public void Clear()
        {
            GameSession.Clear();
            PadHandoff.Clear();
        }

        [Test]
        public void FirstHopPicksTheLightestStarterThatFitsAndReaches()
        {
            var mission = campaign.Mission(1);
            var snaps = SnapTemplates();
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            Assert.IsNotNull(choice);
            Assert.IsNotNull(choice.Design);
            Assert.IsTrue(choice.FitsPayload);
            Assert.IsTrue(choice.ReachesOrbit);
            Assert.IsTrue(choice.LaunchEnabled);
            Assert.AreEqual(mission.MassKg, choice.Design.Obj("payload").Num("mass"));
            Assert.AreEqual(HomeAutoBuild.OrbitId("m1"), choice.Design.Obj("payload").Str("orbit"));
            Assert.AreEqual("m1", choice.Design["missionId"]);
            Assert.AreEqual(mission.BudgetKc, (double)choice.Design["budgetKc"]);

            AutoBuildChoice lightest = null;
            for (int i = 0; i < hangar.templates.Count; i++)
            {
                var other = ScoreFresh(hangar.templates[i], mission);
                if (other == null || !other.FitsPayload || !other.ReachesOrbit) continue;
                if (lightest == null || other.LiftoffMassKg < lightest.LiftoffMassKg
                    || (other.LiftoffMassKg == lightest.LiftoffMassKg && other.TemplateOrder < lightest.TemplateOrder))
                    lightest = other;
            }
            Assert.IsNotNull(lightest);
            Assert.AreEqual(lightest.TemplateId, choice.TemplateId);
            StringAssert.Contains(choice.TemplateName, choice.VehicleLine);
            StringAssert.Contains("m/s", choice.VehicleLine);
            StringAssert.Contains("Enough", choice.ReachLine);
            CollectionAssert.AreEqual(snaps, SnapTemplates());
        }

        [Test]
        public void ARocketThatCannotReachStillLaunchesHonestly()
        {
            // Circular GEO is only ~4200 m/s in this model (circular speed minus site rotation plus 1500).
            // A 100000 km apogee is past every starter, so the shortfall is real.
            var mission = new CampaignMission
            {
                Index = 99,
                Id = "too-high",
                Name = "Too High",
                PayloadId = "kid-box",
                MassKg = 50,
                VolumeM3 = 0.2,
                DiameterM = 0.5,
                TargetKind = "apogee",
                ApoKm = 100000,
                BudgetKc = 100000,
            };
            var snaps = SnapTemplates();
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            Assert.IsNotNull(choice);
            Assert.IsNotNull(choice.Design);
            Assert.IsTrue(choice.FitsPayload, "a tiny fairing payload still fits a starter");
            Assert.IsFalse(choice.ReachesOrbit);
            Assert.Less(choice.MarginMps, 0);
            Assert.IsTrue(choice.LaunchEnabled, "a shortfall is a warning, not a pad block");
            Assert.AreEqual(50, choice.Design.Obj("payload").Num("mass"));
            StringAssert.Contains("Short by", choice.ReachLine);
            StringAssert.Contains("can miss", choice.ReachLine);
            CollectionAssert.AreEqual(snaps, SnapTemplates());
        }

        [Test]
        public void HugePayloadThatStillFitsCanMissWithoutAFakeSuccess()
        {
            var mission = new CampaignMission
            {
                Index = 98,
                Id = "geo-mass",
                Name = "Heavy GEO",
                PayloadId = "kid-box",
                MassKg = 80000,
                VolumeM3 = 0.2,
                DiameterM = 0.5,
                TargetKind = "apogee",
                ApoKm = 100000,
                IncDeg = 28.5,
                BudgetKc = 100000,
            };
            var snaps = SnapTemplates();
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
            Assert.IsNotNull(choice.Design);
            Assert.IsTrue(choice.FitsPayload);
            Assert.IsFalse(choice.ReachesOrbit);
            Assert.AreEqual(80000, choice.Design.Obj("payload").Num("mass"));
            CollectionAssert.AreEqual(snaps, SnapTemplates());
        }

        [Test]
        public void RegisteredOrbitMatchesTheMissionAsk()
        {
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var mission = campaign.Missions[i];
                var fresh = Catalog.LoadFromRepo(Golden.Root);
                var orbit = HomeAutoBuild.EnsureOrbit(fresh, mission);
                var stack = Stack.RequiredDeltaV(fresh, orbit.id, HomeAutoBuild.LatitudeDeg).required;
                var asked = mission.RequiredDeltaV(HomeAutoBuild.LatitudeDeg);
                Assert.That(System.Math.Abs(stack - asked), Is.LessThan(50), mission.Id + " stack " + stack + " asked " + asked);
            }
        }

        [Test]
        public void LaunchRoutesTheNextMissionToTheBriefingAndArmsThePad()
        {
            var route = HomeRoutes.Launch(campaign, 0, catalog, hangar.templates);
            Assert.AreEqual(SessionScreens.Missions, route.Screen);
            Assert.AreEqual(SessionScreens.Briefing, route.Overlay);
            Assert.AreEqual("m1", route.Session.MissionId);
            Assert.AreEqual("KH-1", route.Session.Label);
            Assert.IsTrue(route.Session.AutoPad);
            Assert.IsFalse(string.IsNullOrEmpty(route.Session.VehicleLine));
            Assert.IsFalse(string.IsNullOrEmpty(route.Session.ReachLine));
            Assert.IsTrue(PadHandoff.FromHome);
            Assert.IsTrue(PadHandoff.FlyOnPad);
            Assert.IsTrue(PadHandoff.TryTake(out var design, out bool fly));
            Assert.IsTrue(fly);
            Assert.AreEqual("m1", design["missionId"]);
            Assert.IsFalse(PadHandoff.TryTake(out _, out _), "the handoff is consumed once");
            Assert.AreSame(route.Session, GameSession.Current);
        }

        [Test]
        public void ClearedProgressOpensTheFollowingMissionAndTheLastOneStays()
        {
            Assert.AreEqual("m1", HomeRoutes.NextMission(campaign, 0).Id);
            Assert.AreEqual("m3", HomeRoutes.NextMission(campaign, 2).Id);
            var route = HomeRoutes.Launch(campaign, 2, catalog, hangar.templates);
            Assert.AreEqual("m3", route.Session.MissionId);
            Assert.AreEqual("m10", HomeRoutes.NextMission(campaign, 10).Id);
            Assert.AreEqual("m10", HomeRoutes.NextMission(campaign, 40).Id);
        }

        [Test]
        public void DesignFirstOpensTheDesignerWithTheMissionLoadAndDoesNotAutoFly()
        {
            var route = HomeRoutes.DesignFirst(campaign, 0, catalog, hangar.templates);
            Assert.AreEqual(SessionScreens.Design, route.Screen);
            Assert.AreEqual(SessionScreens.None, route.Overlay);
            Assert.AreEqual("m1", route.Session.MissionId);
            Assert.IsFalse(route.Session.AutoPad);
            Assert.IsFalse(PadHandoff.FlyOnPad);
            Assert.IsTrue(PadHandoff.TryTake(out var design, out bool fly));
            Assert.IsFalse(fly);
            Assert.AreEqual(50, design.Obj("payload").Num("mass"));
            Assert.AreEqual("kh-m1", design.Obj("payload").Str("orbit"));
            Assert.AreEqual("m1", design["missionId"]);
        }

        [Test]
        public void LaunchOnMainOpensTheBriefingNotTheHangar()
        {
            string flow = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Game", "Shell", "SceneFlow.cs"));
            int launch = flow.IndexOf("if (controlId == \"launch\")", System.StringComparison.Ordinal);
            int design = flow.IndexOf("if (controlId == \"design\")", System.StringComparison.Ordinal);
            Assert.Greater(launch, 0);
            Assert.Greater(design, launch);
            string branch = flow.Substring(launch, design - launch);
            StringAssert.Contains("HomeLaunchRoute()", branch);
            Assert.That(branch, Does.Not.Contain("HomeHangar"));
            StringAssert.Contains("HomeRoutes.Launch", flow);
            string stub = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Game", "Shell", "ScreenStub.cs"));
            StringAssert.Contains("HomePressed(\"launch\")", stub);
            string screen = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Game", "Campaign", "BriefingScreen.cs"));
            StringAssert.Contains("To the pad", screen);
            var route = HomeRoutes.Launch(campaign, 0, catalog, hangar.templates);
            Assert.AreEqual(SessionScreens.Missions, route.Screen);
            Assert.AreEqual(SessionScreens.Briefing, route.Overlay);
            Assert.IsTrue(route.Session.AutoPad);
        }

        [Test]
        public void MissingCampaignFallsBackWithoutInventingARocket()
        {
            var launch = HomeRoutes.Launch(null, 0, catalog, hangar.templates);
            Assert.AreEqual(SessionScreens.Missions, launch.Screen);
            Assert.AreEqual(SessionScreens.None, launch.Overlay);
            Assert.IsNull(launch.Session);
            Assert.IsNull(PadHandoff.Pending);

            var design = HomeRoutes.DesignFirst(campaign, 0, catalog, new List<StarterTemplate>());
            Assert.AreEqual(SessionScreens.Design, design.Screen);
            Assert.IsTrue(design.Session.IsFree);
            Assert.IsNull(PadHandoff.Pending);
            Assert.IsTrue(File.Exists(Path.Combine(Golden.Root, "data", "designer-templates.json")));
        }

        [Test]
        public void EveryMissionRocketHasNonNegativeMarginOnAFreshCatalog()
        {
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var mission = campaign.Missions[i];
                var builtOn = Catalog.LoadFromRepo(Golden.Root);
                var choice = HomeAutoBuild.Choose(builtOn, hangar.templates, mission);
                Assert.IsNotNull(choice, mission.Id);
                Assert.IsNotNull(choice.Design, mission.Id);
                Assert.GreaterOrEqual(choice.MarginMps, 0, mission.Id + " " + choice.TemplateId + " " + choice.ReachLine);
                Assert.AreEqual(mission.OrbitId, choice.Design.Obj("payload").Str("orbit"), mission.Id);
                Assert.AreEqual(mission.MassKg, choice.Design.Obj("payload").Num("mass"), mission.Id);

                var fresh = Catalog.LoadFromRepo(Golden.Root);
                var shape = Kestrel.Core.Designer.Validate.Shape(fresh, choice.Design);
                Assert.IsFalse(shape.errors.Exists(e => e.Contains("payload.orbit")), mission.Id + " " + string.Join("; ", shape.errors));
                var readout = Kestrel.Core.Designer.Readout.Quick(fresh, choice.Design);
                Assert.GreaterOrEqual(readout.margin, 0, mission.Id);
                Assert.Greater(readout.liftoffTWR, 1, mission.Id);
            }
        }

        [Test]
        public void PlainHomeLaunchStillDoesNotInventAMission()
        {
            var launch = GameSession.Apply(RouteId.HomeLaunch);
            Assert.AreEqual(SessionScreens.Missions, launch.Screen);
            Assert.IsNull(launch.Session);
        }

        [Test]
        public void AudioAndGearClearTheWebExitOnPortraitPhones()
        {
            var devices = new[]
            {
                new[] { 375f, 20f },
                new[] { 390f, 47f },
                new[] { 440f, 62f },
                new[] { 1032f, 24f },
            };
            for (int i = 0; i < devices.Length; i++)
            {
                float width = devices[i][0];
                float top = devices[i][1];
                HomeCornerLayout.Place(width, top, true, out var audio, out var gear, out var exit);
                Assert.IsFalse(audio.Overlaps(gear), width.ToString());
                Assert.IsFalse(audio.Overlaps(exit), width.ToString());
                Assert.IsFalse(gear.Overlaps(exit), width.ToString());
                Assert.GreaterOrEqual(gear.X, audio.Right);
                Assert.GreaterOrEqual(exit.X, gear.Right);
                Assert.AreEqual(HomeCornerLayout.Gear, gear.W);
                Assert.AreEqual(44f, audio.H);
                HomeCornerLayout.Place(width, top, false, out var audioApp, out var gearApp, out _);
                Assert.IsFalse(audioApp.Overlaps(gearApp), width.ToString());
                Assert.AreEqual(width - HomeCornerLayout.Edge - HomeCornerLayout.Gear, gearApp.X);
            }
        }

        List<string> SnapTemplates()
        {
            var snaps = new List<string>();
            for (int i = 0; i < hangar.templates.Count; i++)
                snaps.Add(JsonWriter.Stringify(hangar.templates[i].design));
            return snaps;
        }

        AutoBuildChoice ScoreFresh(StarterTemplate template, CampaignMission mission)
        {
            var fresh = Catalog.LoadFromRepo(Golden.Root);
            return HomeAutoBuild.Choose(fresh, new List<StarterTemplate> { template }, mission);
        }
    }
}
