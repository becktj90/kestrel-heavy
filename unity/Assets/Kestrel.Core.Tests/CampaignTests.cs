// §4 campaign: score formula, stars, career bank, unlock order, and GV11 viability.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CampaignTests
    {
        Catalog catalog;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            catalog = Golden.Catalog;
            campaign = Campaign.Load(Golden.Root);
        }

        [Test]
        public void Score_MatchesSpecFormula()
        {
            Assert.AreEqual(0, MissionScore.Points(false, 0.05, 0, 100, "perfect"), 0);
            Assert.AreEqual(1500, MissionScore.Points(true, 0.05, 0, 100, null), 1e-6);
            Assert.AreEqual(1200, MissionScore.Points(true, 0, 0, 100, null), 1e-6);
            Assert.AreEqual(1200, MissionScore.Points(true, 0.10, 0, 100, null), 1e-6);
            Assert.AreEqual(1350, MissionScore.Points(true, 0.025, 0, 100, null), 1e-6);
            Assert.AreEqual(1300, MissionScore.Points(true, 0.05, 100, 100, null), 1e-6);
            Assert.AreEqual(1400, MissionScore.Points(true, 0.05, 50, 100, null), 1e-6);
            Assert.AreEqual(1300, MissionScore.Points(true, 0.05, 150, 100, null), 1e-6);
            Assert.AreEqual(1700, MissionScore.Points(true, 0.05, 0, 100, "perfect"), 1e-6);
            Assert.AreEqual(1600, MissionScore.Points(true, 0.05, 0, 100, "good"), 1e-6);
            Assert.AreEqual(1500, MissionScore.Points(true, 0.05, 0, 100, "short"), 1e-6);
        }

        [Test]
        public void Stars_FollowMarginBudgetAndLanding()
        {
            Assert.AreEqual(0, MissionScore.Stars(false, 0.05, 0, 100, false, true, 1, 6));
            Assert.AreEqual(1, MissionScore.Stars(true, 0.019, 0, 100, false, false, 1, 6));
            Assert.AreEqual(1, MissionScore.Stars(true, 0.11, 0, 100, false, false, 1, 6));
            Assert.AreEqual(1, MissionScore.Stars(true, 0.05, 101, 100, false, false, 1, 6));
            Assert.AreEqual(2, MissionScore.Stars(true, 0.02, 100, 100, false, false, 5, 6));
            Assert.AreEqual(3, MissionScore.Stars(true, 0.05, 50, 100, false, false, 4, 6));
            Assert.AreEqual(2, MissionScore.Stars(true, 0.10, 0, 100, true, false, 1, 6));
            Assert.AreEqual(3, MissionScore.Stars(true, 0.10, 0, 100, true, true, 9, 6));
            Assert.IsTrue(MissionScore.AltitudeWithin(10500, 10000));
            Assert.IsFalse(MissionScore.AltitudeWithin(9400, 10000));
            Assert.IsTrue(MissionScore.InclinationWithin(52.1, 51.6));
            Assert.IsFalse(MissionScore.InclinationWithin(52.2, 51.6));
        }

        [Test]
        public void RequiredDeltaV_MatchesStackForCatalogOrbits()
        {
            double sub = Stack.RequiredDeltaV(catalog, "suborbital").required;
            double leo = Stack.RequiredDeltaV(catalog, "leo400").required;
            double gto = Stack.RequiredDeltaV(catalog, "gto").required;
            Assert.AreEqual(sub, MissionScore.RequiredDeltaV("apogee", 100, 0, 0), 1e-6);
            Assert.AreEqual(leo, MissionScore.RequiredDeltaV("circular", 400, 400, 0), 1e-6);
            Assert.AreEqual(gto, MissionScore.RequiredDeltaV("ellipse", 35786, 200, 0), 1e-6);
            double c3 = MissionScore.RequiredDeltaV("c3", 0, 200, -2);
            Assert.Greater(c3, gto);
            Assert.Greater(campaign.ById("m2").RequiredDeltaV(), campaign.ById("m1").RequiredDeltaV());
            Assert.Greater(campaign.ById("m3").RequiredDeltaV(), campaign.ById("m2").RequiredDeltaV());
            Assert.AreEqual(leo, campaign.ById("m5").RequiredDeltaV(), 1e-6);
            Assert.Greater(campaign.ById("m7").RequiredDeltaV(), campaign.ById("m5").RequiredDeltaV());
            Assert.Greater(campaign.ById("m9").RequiredDeltaV(), campaign.ById("m7").RequiredDeltaV());
        }

        [Test]
        public void CampaignTable_MatchesSpecWithRecordedTunes()
        {
            Assert.AreEqual(20, campaign.Missions.Count);
            Assert.IsFalse(campaign.Timers);
            Assert.IsFalse(campaign.Energy);
            Assert.IsFalse(campaign.Ads);
            Assert.IsFalse(DarkPatterns.Timers);
            Assert.IsFalse(DarkPatterns.Energy);
            Assert.IsFalse(DarkPatterns.Ads);
            Assert.AreEqual(2_000_000, campaign.StartingBankKc);
            CollectionAssert.AreEqual(new[] { "core-stage", "engines", "tanks", "fairing" }, campaign.StarterUnlocks);
            CollectionAssert.AreEqual(new ulong[] { 1, 2, 3, 4, 5 }, campaign.Seeds);

            Expect(1, "First Hop", "kid-box", 50, 0.2, "apogee", 10, 0, 2e6, new[] { "fins", "nose" }, true);
            Expect(2, "Edge of Space", "kid-box", 100, 0.2, "apogee", 100, 0, 5e6, new[] { "stage-2" }, true);
            Expect(3, "Tiny Orbit", "cubesat", 20, 0.05, "circular", 200, 200, 15e6, new[] { "nozzle-expansion" }, true);
            Expect(4, "Weather Eye", "leo-sat", 2500, 12, "circular", 500, 500, 40e6, new[] { "pc", "throttle" }, false);
            Expect(5, "Heavy Lifter", "leo-sat", 12000, 40, "circular", 400, 400, 90e6, new[] { "crossfeed" }, false);
            Expect(6, "Come Back", "leo-sat", 8000, 30, "circular", 200, 200, 70e6, new[] { "legs", "grid-fins" }, false);
            Expect(7, "Haven at Sea", "gto-comsat", 5500, 60, "ellipse", 35786, 200, 110e6, new[] { "insulation" }, false);
            Expect(8, "Crew Day", "crew-capsule", 12000, 25, "circular", 400, 400, 160e6, new[] { "redundant-avionics" }, false);
            Expect(9, "Moon Shot", "lunar-probe", 3000, 15, "c3", 0, 200, 180e6, new[] { "hydrolox-upper" }, false);
            Expect(10, "Big Reuse", "gto-comsat", 7000, 70, "ellipse", 35786, 200, 120e6, new string[0], false);
            Expect(11, "Dawn Ledger", "weather-sat", 2200, 8, "circular", 550, 550, 80e6, new string[0], false);
            Expect(12, "Glass Meridian", "space-telescope", 1800, 10, "circular", 600, 600, 100e6, new string[0], false);
            Expect(13, "Flock Yard", "cubesat-dispenser", 480, 1.2, "circular", 550, 550, 90e6, new string[0], false);
            Expect(14, "Harbor Trunk", "cargo-capsule", 10000, 35, "circular", 400, 400, 130e6, new string[0], false);
            Expect(15, "Relay West", "geo-relay", 6200, 50, "ellipse", 35786, 200, 140e6, new string[0], false);
            Expect(16, "Second Watch", "crew-capsule", 12000, 25, "circular", 500, 500, 180e6, new string[0], false);
            Expect(17, "Lumen Down", "lunar-lander", 4000, 18, "c3", 0, 200, 190e6, new string[0], false);
            Expect(18, "Nightjar", "lunar-probe", 2500, 15, "c3", 0, 200, 200e6, new string[0], false);
            Expect(19, "Keel Reuse", "gto-comsat", 7500, 70, "ellipse", 35786, 200, 210e6, new string[0], false);
            Expect(20, "Far Hold", "lunar-probe", 3200, 16, "c3", 0, 200, 240e6, new string[0], false);
            Assert.AreEqual("achievable", campaign.Mission(10).Band);
            Assert.AreEqual("hard", campaign.Mission(16).Band);
            Assert.AreEqual("hard", campaign.Mission(20).Band);
            Assert.AreEqual(4, campaign.Mission(20).CoastHours);
            Assert.AreEqual(-0.25, campaign.Mission(20).C3Km2PerS2);

            var m3 = campaign.Mission(3);
            Assert.IsTrue(m3.MarginNonNegative);
            Assert.AreEqual(6, campaign.Mission(4).MaxG);
            Assert.IsTrue(campaign.Mission(5).StrapOnsAllowed);
            var m6 = campaign.Mission(6);
            Assert.AreEqual("rtls", m6.LandingMode);
            Assert.AreEqual("good", m6.LandingGrade);
            Assert.AreEqual("pad", m6.LandingSite);
            var m7 = campaign.Mission(7);
            Assert.AreEqual("barge", m7.LandingMode);
            Assert.AreEqual("Haven", m7.LandingSite);
            var m8 = campaign.Mission(8);
            Assert.AreEqual(4.5, m8.MaxG);
            Assert.AreEqual(0.98, m8.ReliabilityMin);
            Assert.IsTrue(m8.NoRedRisks);
            Assert.IsTrue(m8.RandomFailures);
            Assert.AreEqual(3, campaign.Mission(9).CoastHours);
            Assert.AreEqual(-2, campaign.Mission(9).C3Km2PerS2);
            var m10 = campaign.Mission(10);
            Assert.IsTrue(m10.LandBoosters);
            Assert.AreEqual("rtls", m10.LandingMode);

            var raw = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "campaign.json")));
            string tune6 = null, tune8 = null, tune16 = null;
            foreach (JsonObject row in raw.Arr("missions"))
            {
                if (row.Str("id") == "m6") tune6 = row.Str("tune");
                if (row.Str("id") == "m8") tune8 = row.Str("tune");
                if (row.Str("id") == "m16") tune16 = row.Str("tune");
            }
            StringAssert.Contains("200 km", tune6);
            StringAssert.Contains("4.5", tune8);
            StringAssert.Contains("500 km", tune16);
            Assert.IsFalse(campaign.Mission(16).LandingRequired);

            var ramp = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "mission-ramp.json")));
            Assert.AreEqual("data/campaign.json", ramp.Str("designerCampaign"));
            var kh = ramp.Obj("missions");
            Assert.AreEqual(0.55, kh.Obj("KH-1").Num("wind"));
            Assert.AreEqual(52, kh.Obj("KH-1").Num("landingFuel"));
            Assert.AreEqual(6, kh.Obj("KH-1").Num("swellAmp"));
            Assert.AreEqual(1.45, kh.Obj("KH-5").Num("wind"));
            Assert.AreEqual(24, kh.Obj("KH-5").Num("landingFuel"));
            Assert.AreEqual(28, kh.Obj("KH-5").Num("swellAmp"));
        }

        void Expect(int index, string name, string payload, double mass, double volume, string kind, double apo, double peri, double budget, string[] unlocks, bool gentle)
        {
            var m = campaign.Mission(index);
            Assert.AreEqual("m" + index, m.Id);
            Assert.AreEqual(name, m.Name);
            Assert.AreEqual(payload, m.PayloadId);
            Assert.AreEqual(mass, m.MassKg);
            Assert.AreEqual(volume, m.VolumeM3);
            Assert.Greater(m.DiameterM, 0);
            Assert.AreEqual(kind, m.TargetKind);
            Assert.AreEqual(apo, m.ApoKm);
            Assert.AreEqual(peri, m.PeriKm);
            Assert.AreEqual(budget, m.BudgetKc);
            Assert.AreEqual(budget, m.RewardKc);
            Assert.AreEqual(gentle, m.GentleDefault);
            CollectionAssert.AreEqual(unlocks, m.Unlocks);
        }

        [Test]
        public void Career_BankRefundAndUnlocksInOrder()
        {
            long perKc = CostModel.MinorPerKc(catalog);
            Assert.AreEqual(100_000, perKc);
            Assert.AreEqual(600_000, CostModel.ReuseRefundMinor(1_000_000, 0));
            Assert.AreEqual(550_000, CostModel.ReuseRefundMinor(1_000_000, 1));
            Assert.AreEqual(0, CostModel.ReuseRefundMinor(1_000_000, 12));
            Assert.AreEqual(0, CostModel.ReuseRefundMinor(0, 0));

            var career = Career.Start(campaign, perKc);
            Assert.AreEqual(Career.KcToMinor(2_000_000, perKc), career.BankMinor);
            Assert.IsTrue(career.HasUnlock("engines"));
            Assert.IsFalse(career.HasUnlock("fins"));
            Assert.IsTrue(career.MissionOpen(1));
            Assert.IsFalse(career.MissionOpen(2));

            var skipped = career.Settle(2, true, 1, 1, "core");
            Assert.IsFalse(skipped.accepted);
            Assert.AreEqual(Career.KcToMinor(2_000_000, perKc), career.BankMinor);

            long cost = 1_000;
            long hardware = 5_000;
            long before = career.BankMinor;
            var pass = career.Settle(1, true, cost, hardware, "core");
            Assert.IsTrue(pass.accepted && pass.passed);
            long reward = Career.KcToMinor(campaign.Mission(1).RewardKc, perKc);
            Assert.AreEqual(reward, pass.rewardMinor);
            Assert.AreEqual(CostModel.ReuseRefundMinor(hardware, 0), pass.refundMinor);
            Assert.AreEqual(before + reward - cost + pass.refundMinor, career.BankMinor);
            Assert.AreEqual(1, pass.cleared);
            Assert.IsTrue(career.HasUnlock("fins"));
            Assert.IsTrue(career.HasUnlock("nose"));
            Assert.IsFalse(career.HasUnlock("stage-2"));
            Assert.IsTrue(career.MissionOpen(2));

            before = career.BankMinor;
            var miss = career.Settle(2, false, 4_000, hardware, "core");
            Assert.IsTrue(miss.accepted);
            Assert.IsFalse(miss.passed);
            Assert.AreEqual(0, miss.rewardMinor);
            Assert.AreEqual(0, miss.refundMinor);
            Assert.AreEqual(before - 4_000, career.BankMinor);
            Assert.AreEqual(1, career.Cleared);
            Assert.IsFalse(career.HasUnlock("stage-2"));

            var again = career.Settle(2, true, cost, hardware, "core");
            Assert.AreEqual(CostModel.ReuseRefundMinor(hardware, 1), again.refundMinor);
            Assert.Less(again.refundMinor, pass.refundMinor);
            Assert.IsTrue(career.HasUnlock("stage-2"));
            Assert.AreEqual(2, career.Cleared);

            for (int i = 3; i <= 10; i++)
            {
                Assert.IsTrue(career.MissionOpen(i), "mission " + i);
                career.Settle(i, true, cost, 0, null);
            }
            Assert.IsFalse(career.MissionOpen(10));
            Assert.AreEqual(10, career.Cleared);
            Assert.IsTrue(career.HasUnlock("hydrolox-upper"));
            Assert.IsTrue(career.HasUnlock("redundant-avionics"));

            campaign.Timers = true;
            Assert.Throws<InvalidOperationException>(() => Career.Start(campaign, perKc));
        }

        [Test, Timeout(300000)]
        public void EveryMission_HasThreeViableDesigns()
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-viable.json")));
            var blocks = file.Arr("missions");
            Assert.AreEqual(10, blocks.Count);
            var seen = new HashSet<string>();
            foreach (JsonObject block in blocks)
            {
                string id = block.Str("id");
                seen.Add(id);
                var mission = campaign.ById(id);
                Assert.IsNotNull(mission, id);
                var designs = block.Arr("designs");
                Assert.AreEqual(3, designs.Count, id);
                var prints = new List<string>();
                var loaded = new List<Design>();
                foreach (JsonObject raw in designs)
                {
                    var design = Design.FromJson(raw);
                    loaded.Add(design);
                    prints.Add(Fingerprint(design));
                    Assert.AreEqual(mission.PayloadId, design.payloadType, design.id);
                    Assert.AreEqual(mission.MassKg, design.payloadMass, design.id);
                    Assert.AreEqual("campaign", design.tier, design.id);
                    bool ok = FlightBatch.Viable(catalog, mission, design, campaign.Seeds, out var flights);
                    Assert.AreEqual(5, flights.Length, design.id);
                    var first = flights[0];
                    Assert.IsTrue(ok, id + " " + design.id + " " + first.failure + " apo=" + first.apoKm.ToString("0.0") + " peri=" + first.periKm.ToString("0.0") + " g=" + first.maxG.ToString("0.00") + " cost=" + first.costKc.ToString("0") + " land=" + first.landingGrade + "/" + first.boosterGrade + " red=" + first.redRisk + " rel=" + first.reliability.ToString("0.000"));
                    foreach (var flight in flights)
                    {
                        Assert.IsTrue(flight.passed, design.id + " seed " + flight.seed);
                        Assert.IsTrue(flight.underBudget, design.id);
                        Assert.IsTrue(flight.fairingFits, design.id);
                        Assert.IsTrue(flight.payloadMatch, design.id);
                        Assert.IsFalse(flight.gentle, design.id);
                        Assert.LessOrEqual(flight.costKc, mission.BudgetKc);
                        Assert.Greater(flight.score, 0);
                    }
                }
                Assert.AreEqual(3, new HashSet<string>(prints).Count, id + " designs are not distinct");
                for (int a = 0; a < loaded.Count; a++)
                    for (int b = a + 1; b < loaded.Count; b++)
                        Assert.IsTrue(Distinct(loaded[a], loaded[b]), id + " " + loaded[a].id + " vs " + loaded[b].id);
            }
            Assert.AreEqual(10, seen.Count);
        }

        static string Fingerprint(Design d)
        {
            var parts = new List<string> { d.stages.Count.ToString() };
            foreach (var s in d.stages)
                parts.Add(s.engine + ":" + s.engineCount + ":" + s.propellant + ":" + s.propMass.ToString("R") + ":" + s.diameter.ToString("R"));
            if (d.StrapOnCount > 0)
                parts.Add("strap:" + d.strapOns.engine + ":" + d.strapOns.count + ":" + d.strapOns.propMass.ToString("R"));
            return string.Join("|", parts);
        }

        static bool Distinct(Design a, Design b)
        {
            if (a.stages.Count != b.stages.Count) return true;
            if (a.StrapOnCount != b.StrapOnCount) return true;
            int n = Math.Max(a.stages.Count, b.stages.Count);
            for (int i = 0; i < n; i++)
            {
                var x = a.stages[i];
                var y = b.stages[i];
                if (x.engine != y.engine || x.engineCount != y.engineCount || x.propellant != y.propellant) return true;
                if (x.propMass != y.propMass || x.diameter != y.diameter) return true;
            }
            if (a.StrapOnCount > 0 && (a.strapOns.engine != b.strapOns.engine || a.strapOns.propMass != b.strapOns.propMass || a.strapOns.count != b.strapOns.count))
                return true;
            return false;
        }
    }
}
