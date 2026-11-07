// Fun review P0 (DECISIONS #75): the briefing-wind gate for what a player actually flies by default. WindGateTests
// gates the board starters (Home LAUNCH's pick for KH-3..KH-20, after the row's one move); nothing gated the Starter
// screen's default (the mission's campaign winner in data/golden/campaign-winners.json, StarterBases), and KH-5's
// flew 427.3 x 372.7 km in the seed-1 briefing wind and failed the linear campaign. Here every mission's Starter
// default, at its default settings, must win the live pad in the seed-1 briefing wind (FlightDriver's 1/60 s frames at
// LiveDt, the shared director on default settings, played checks, the live report card's gates), and its launch
// predictor must not name a cause. Home LAUNCH's pick for the missions with no board-starters row (KH-1, KH-2) is
// flown as picked under the same gate, so with WindGateTests every Home LAUNCH pick is covered.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class StarterDefaultWindGateTests
    {
        [Test]
        public void StarterDefault_EveryMission_WinsTheLivePadInSeedOneWind()
        {
            var catalog = StarterFixture.Catalog;
            var misses = new List<string>();
            int seen = 0;
            foreach (var mission in StarterFixture.Campaign.Missions)
            {
                var rocket = StarterFixture.Rocket(mission.Id);
                Assert.IsNotNull(StarterBases.For(mission.Id), mission.Id + " has a Starter base");
                var design = Design.FromJson(rocket.BuildDesign());
                var live = LivePadLoop.Fly(catalog, mission, design, true);
                if (!LivePadLoop.Passes(catalog, mission, design, live)
                    && !LivePadLoop.IsM13HardButWins(catalog, mission, design, live))
                    misses.Add(mission.Id + " Starter default live seed-1 wind " + LivePadLoop.Text(live));
                seen++;
            }
            Assert.AreEqual(20, seen, "KH-1 through KH-20");
            Assert.That(misses, Is.Empty, string.Join(" | ", misses));
        }

        /// <summary>DECISIONS #86: with the Resources books installed and no checkout, ForPad builds the same seed-1
        /// card the checkout path builds. The device gate is then the seed-1 live gate above (no separate calm pad).</summary>
        [Test]
        public void InstalledBooks_MatchCheckoutSeedOne_WithNoCheckout()
        {
            string campaign = File.ReadAllText(Path.Combine(Golden.Root, "data", "campaign.json"));
            string sites = File.ReadAllText(Path.Combine(Golden.Root, "data", "sites.json"));
            string weather = File.ReadAllText(Path.Combine(Golden.Root, "data", "weather.json"));
            BriefingBooks.UseJson(campaign, sites, weather);
            try
            {
                Assert.IsTrue(BriefingBooks.HasBooks);
                for (int i = 1; i <= 20; i++)
                {
                    var fromDisk = MissionBriefing.Load(Golden.Root, i, 7, 1);
                    var fromBooks = MissionBriefing.Load(null, i, 7, 1);
                    Assert.AreEqual(fromDisk.Wind.SurfaceMs, fromBooks.Wind.SurfaceMs, 0, "m" + i + " surface");
                    Assert.AreEqual(fromDisk.Wind.Compass, fromBooks.Wind.Compass, "m" + i + " compass");
                    Assert.AreEqual(fromDisk.Site.Id, fromBooks.Site.Id, "m" + i + " site");
                    Assert.AreEqual(fromDisk.Site.LatitudeDeg, fromBooks.Site.LatitudeDeg, 0, "m" + i + " lat");
                    Assert.AreEqual(fromDisk.TimeId, fromBooks.TimeId, "m" + i + " time");
                    Assert.AreEqual(fromDisk.WindLine, fromBooks.WindLine, "m" + i + " wind line");
                    Assert.IsFalse(fromBooks.Wind.Calm, "m" + i + " must not be calm on seed 1");
                }
            }
            finally { BriefingBooks.Clear(); }
            Assert.IsFalse(BriefingBooks.HasBooks);
        }

        /// <summary>
        /// Trevor 11-07, KH-3 "Wrong place" on every retry: the card's 461 x -91 km is the KH-3 Starter at Kerosene and the
        /// lowest fuel dot (kerosene-2-f0-b0; the Starter saves its setting per mission, so every retry flew it), on a calm
        /// reference pad (the pre-#86 device path). The top stage burns dry short of orbit speed. The Starter default
        /// (methane, middle fuel) wins the same pad. Before the #456 fix the Starter read that setting "Ready… fuel to
        /// spare" and the card said "79 m/s too fast at the low point…"; now the Starter names Short before launch and
        /// the card says it never reached orbit and ran out of push. Kept on calm air so the orbit numbers stay pinned.
        /// #335 moved the card from 470 x -394 km: the fairing shell (89 kg) now leaves the stack at the fairing event,
        /// so the top stage flies lighter and the burnout state is far less eccentric.
        /// </summary>
        [Test]
        public void Kh3_TrevorsStarterSetting_ReproducesTheDeviceMiss_AndIsNamedBeforeLaunch()
        {
            var catalog = StarterFixture.Catalog;
            var mission = StarterFixture.Campaign.ById("m3");
            var rocket = StarterFixture.Rocket("m3");
            var trevor = new StarterChoice(StarterEngine.Kerosene, 2, 0, 0);
            var design = Design.FromJson(rocket.BuildDesign(trevor));
            var live = LivePadLoop.Fly(catalog, mission, design, false);
            string text = LivePadLoop.Text(live);
            Assert.IsNull(live.failure, text);
            Assert.AreEqual(460.7, MissionScore.FlownApoAltM(live) / 1000.0, 2.0, "the card's high point / " + text);
            Assert.AreEqual(-90.9, live.orbit.periAlt / 1000.0, 2.0, "the card's low point / " + text);
            Assert.IsFalse(LivePadLoop.Passes(catalog, mission, design, live), text);

            // The default on the same pad: in the band.
            var fixedLive = LivePadLoop.Fly(catalog, mission, Design.FromJson(rocket.BuildDesign()), false);
            Assert.IsTrue(LivePadLoop.Passes(catalog, mission, Design.FromJson(rocket.BuildDesign()), fixedLive), LivePadLoop.Text(fixedLive));

            // Honest Ready: the Starter no longer reads that setting Ready.
            var v = rocket.Verdict(trevor);
            Assert.IsNotNull(v);
            Assert.AreEqual(StarterVerdict.Short, v.Cause, "predictor " + v.Outcome);

            // The card: never reached orbit, ran out of push; on the Starter, Reset.
            var x = MissReason.Explain(mission, new MissInput
            {
                ApoM = MissionScore.FlownApoAltM(live), PeriM = live.orbit.periAlt, MaxG = live.maxG, Stages = design.stages.Count,
                CutoffGammaDeg = live.cutoffGammaDeg, PitchKickDeg = design.guidance.pitchKickDeg, Starter = true,
            });
            Assert.IsNotNull(x, text);
            Assert.AreEqual("suborbital-short", x.Code, x.Line);
            StringAssert.StartsWith("Didn't reach orbit. It climbed to 461 km", x.Reason);
            StringAssert.EndsWith(MissReason.StarterReset, x.Advice);
        }

        /// <summary>The launch predictor (Honest Ready) on every Starter default names no cause: it never reads a
        /// failure badge on the rocket the player is handed.</summary>
        [Test]
        public void StarterDefault_EveryMission_PredictorNamesNoCause()
        {
            var bad = new List<string>();
            foreach (var mission in StarterFixture.Campaign.Missions)
            {
                var v = StarterFixture.Rocket(mission.Id).Verdict();
                Assert.IsNotNull(v, mission.Id + " has a verdict");
                if (!v.Survives) bad.Add(mission.Id + " " + v.Cause);
            }
            Assert.That(bad, Is.Empty, string.Join(" | ", bad));
        }

        /// <summary>The KH-5 Starter default lands its 400 x 400 km band within 20 km in the seed-1 briefing wind
        /// (DECISIONS #75: kick 8 flew 427.3 x 372.7 km).</summary>
        [Test]
        public void StarterDefault_Kh5_Within20KmOf400InSeedOneWind()
        {
            var catalog = StarterFixture.Catalog;
            var mission = StarterFixture.Campaign.ById("m5");
            var design = Design.FromJson(StarterFixture.Rocket("m5").BuildDesign());
            var live = LivePadLoop.Fly(catalog, mission, design, true);
            Assert.IsNull(live.failure, LivePadLoop.Text(live));
            Assert.AreEqual(400.0, MissionScore.FlownApoAltM(live) / 1000.0, 20.0, LivePadLoop.Text(live));
            Assert.AreEqual(400.0, live.orbit.periAlt / 1000.0, 20.0, LivePadLoop.Text(live));
        }

        /// <summary>Home LAUNCH (HomeRoutes.Launch → HomeAutoBuild.Choose) on a mission with no board-starters row flies
        /// the pick as is; it must win the live pad in the seed-1 briefing wind. WindGateTests gates the rest.</summary>
        [Test]
        public void HomeLaunchPick_WithoutABoardRow_WinsTheLivePadInSeedOneWind()
        {
            var catalog = StarterFixture.Catalog;
            var hangar = HangarData.LoadFromRepo(Golden.Root);
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "board-starters.json")));
            var boarded = new HashSet<string>();
            foreach (JsonObject row in file.Arr("missions")) boarded.Add(row.Str("id"));
            var misses = new List<string>();
            var seen = new List<string>();
            foreach (var mission in StarterFixture.Campaign.Missions)
            {
                if (boarded.Contains(mission.Id)) continue;
                var choice = HomeAutoBuild.Choose(catalog, hangar.templates, mission);
                Assert.IsNotNull(choice, mission.Id + " Home LAUNCH has a pick");
                var design = Design.FromJson(choice.Design);
                var live = LivePadLoop.Fly(catalog, mission, design, true);
                if (!LivePadLoop.Passes(catalog, mission, design, live)
                    && !LivePadLoop.IsM13HardButWins(catalog, mission, design, live))
                    misses.Add(mission.Id + " Home LAUNCH pick live seed-1 wind " + LivePadLoop.Text(live));
                seen.Add(mission.Id);
            }
            Assert.AreEqual(new[] { "m1", "m2" }, seen.ToArray(), "KH-1 and KH-2 have no board row; WindGateTests covers KH-3..KH-20");
            Assert.That(misses, Is.Empty, string.Join(" | ", misses));
        }
    }
}
