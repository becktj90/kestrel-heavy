// Open contracts after KH-10. The board is a seeded pick of viable cells. No timers, expiry, or purchases.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ContractGenTests
    {
        ContractPool pool;
        Catalog catalog;

        [SetUp]
        public void Load()
        {
            catalog = Golden.Catalog;
            pool = ContractGen.Load(Golden.Root);
        }

        [Test]
        public void Difficulty_FollowsTheClearedCount()
        {
            Assert.AreEqual(1, ContractGen.Difficulty(0), 1e-12);
            Assert.AreEqual(1.6, ContractGen.Difficulty(4), 1e-12);
            Assert.AreEqual(1, ContractGen.MassScale(0), 1e-12);
            Assert.AreEqual(1.3, ContractGen.MassScale(3), 1e-12);
            Assert.AreEqual(20, ContractGen.MassKg(20, 0), 0);
            Assert.AreEqual(3250, ContractGen.MassKg(2500, 3), 0);
            Assert.AreEqual(12000, ContractGen.MassKg(10400, 3), 0);
            Assert.AreEqual(12000, ContractGen.MassKg(8000, 40), 0);
            Assert.AreEqual(1, ContractGen.WindScale(0), 1e-12);
            Assert.AreEqual(1.3, ContractGen.WindScale(6), 1e-12);
            Assert.AreEqual(1.6, ContractGen.WindScale(12), 1e-12);
            Assert.AreEqual(1.6, ContractGen.WindScale(40), 1e-12);
            Assert.AreEqual(0, ContractGen.ExtraRequirements(0));
            Assert.AreEqual(0, ContractGen.ExtraRequirements(2));
            Assert.AreEqual(1, ContractGen.ExtraRequirements(3));
            Assert.AreEqual(2, ContractGen.ExtraRequirements(8));
            Assert.AreEqual(3, ContractGen.ExtraRequirements(9));
            Assert.AreEqual(3, ContractGen.ExtraRequirements(40));
            Assert.AreEqual(0, ContractGen.OrbitFloor(0, 5));
            Assert.AreEqual(1, ContractGen.OrbitFloor(3, 5));
            Assert.AreEqual(2, ContractGen.OrbitFloor(6, 5));
            Assert.AreEqual(4, ContractGen.OrbitFloor(12, 5));
            Assert.AreEqual(5, ContractGen.OrbitFloor(30, 5));
            Assert.AreEqual(ContractGen.Difficulty(0), ContractGen.Difficulty(-4), 0);
            Assert.IsFalse(ContractGen.PurchasePath);
            Assert.IsFalse(ContractGen.Expires);
        }

        [Test]
        public void Hash_IsTheProfileAndClearedCount()
        {
            Assert.AreEqual(15690580978656486129UL, ContractGen.Hash("pilot", 0));
            Assert.AreEqual(ContractGen.Hash(null, 3), ContractGen.Hash("", 3));
            Assert.AreNotEqual(ContractGen.Hash("pilot", 0), ContractGen.Hash("pilot", 1));
            Assert.AreNotEqual(ContractGen.Hash("pilot", 4), ContractGen.Hash("hangar", 4));
            Assert.AreEqual(ContractGen.Hash("pilot", 4), ContractGen.Hash("pilot", 4));
        }

        [Test]
        public void Pool_IsFictionalSeededAndHasNoDarkPatterns()
        {
            Assert.IsFalse(pool.Timers);
            Assert.IsFalse(pool.Energy);
            Assert.IsFalse(pool.Ads);
            Assert.IsFalse(pool.Expiry);
            Assert.IsFalse(pool.Purchases);
            Assert.IsFalse(pool.Daily);
            Assert.AreEqual(3, pool.OfferCount);
            Assert.AreEqual(11, pool.FirstNumber);
            CollectionAssert.AreEqual(new ulong[] { 1, 2, 3, 4, 5 }, pool.Seeds);
            CollectionAssert.AreEqual(
                new[] { "tern", "comsat", "lark", "flock", "finch", "pantry" },
                pool.Archetypes.ConvertAll(a => a.Id));
            CollectionAssert.AreEqual(
                new[] { "leo200", "leo400", "leo600", "sso", "gto", "tli" },
                pool.Orbits.ConvertAll(o => o.Id));
            Assert.AreEqual("Tern-{n}", pool.ArchetypeById["tern"].NamePattern);
            Assert.AreEqual("Lark-{n}", pool.ArchetypeById["lark"].NamePattern);
            Assert.AreEqual("Finch-{n}", pool.ArchetypeById["finch"].NamePattern);
            Assert.AreEqual("Pantry-{n}", pool.ArchetypeById["pantry"].NamePattern);
            Assert.AreEqual("Flock-{n}", pool.ArchetypeById["flock"].NamePattern);
            Assert.AreEqual(3, pool.OrbitById["tli"].CoastHours);
            Assert.AreEqual(-2, pool.OrbitById["tli"].C3Km2PerS2);
            Assert.AreEqual(28.5, pool.OrbitById["gto"].IncDeg);
            foreach (var arch in pool.Archetypes)
            {
                Assert.IsTrue(catalog.PayloadById.ContainsKey(arch.CatalogId), arch.CatalogId);
                Assert.LessOrEqual(arch.BaseMassKg, ContractGen.MassCapKg);
                Assert.IsFalse(Forbidden(arch.NamePattern + arch.Kind));
            }
            var raw = File.ReadAllText(Path.Combine(Golden.Root, "data", "contracts.json"));
            StringAssert.Contains("data/sites.json", raw);
            StringAssert.Contains("data/weather.json", raw);
            Assert.IsFalse(raw.Contains("\"expires\""));
            Assert.IsFalse(raw.Contains("\"sku\""));
            Assert.IsTrue(File.Exists(Path.Combine(Golden.Root, "data", "sites.json")));
            Assert.IsTrue(File.Exists(Path.Combine(Golden.Root, "data", "weather.json")));
        }

        [Test]
        public void Offer_IsDeterministicAndStaysInsideViableCells()
        {
            var sites = SiteBook.Load(Golden.Root);
            var weather = WeatherBook.Load(Golden.Root);
            var viable = new HashSet<string>();
            foreach (var cell in pool.Cells) viable.Add(cell.Id);
            var rejected = new HashSet<string>(pool.Rejected);
            Assert.Greater(pool.Cells.Count, 3);
            Assert.Greater(rejected.Count, 0);
            foreach (var id in rejected) Assert.IsFalse(viable.Contains(id), id);

            string[] profiles = { "", "pilot", "hangar", "kid" };
            int[] cleared = { 0, 1, 2, 3, 6, 9, 12, 15, 30 };
            var boards = new List<string>();
            foreach (var profile in profiles)
            {
                foreach (var k in cleared)
                {
                    var a = ContractGen.Offer(pool, sites, weather, profile, k);
                    var b = ContractGen.Offer(pool, sites, weather, profile, k);
                    Assert.AreEqual(3, a.Count, profile + " " + k);
                    Assert.AreEqual(ContractGen.Hash(profile, k), a[0].BoardSeed);
                    for (int i = 0; i < a.Count; i++)
                    {
                        Assert.AreEqual(a[i].CellId, b[i].CellId);
                        Assert.AreEqual(a[i].Name, b[i].Name);
                        Assert.AreEqual(a[i].WindLine, b[i].WindLine);
                        Assert.AreEqual(a[i].Site.Id, b[i].Site.Id);
                        Assert.AreEqual(a[i].BudgetKc, b[i].BudgetKc);
                        Assert.AreEqual(a[i].BudgetKc, a[i].RewardKc);
                        Assert.IsTrue(viable.Contains(a[i].CellId), a[i].CellId);
                        Assert.IsFalse(rejected.Contains(a[i].CellId));
                        Assert.LessOrEqual(a[i].ProvenK, k < 0 ? 0 : k);
                        Assert.AreEqual(ContractGen.Difficulty(a[i].ProvenK), a[i].Difficulty, 1e-9);
                        Assert.AreEqual(ContractGen.WindScale(a[i].ProvenK), a[i].WindScale, 1e-9);
                        Assert.AreEqual(ContractGen.MassKg(a[i].Cell.Archetype.BaseMassKg, a[i].ProvenK), a[i].MassKg, 0);
                        Assert.AreEqual(ContractGen.ExtraRequirements(a[i].ProvenK), a[i].Requirements.Count);
                        Assert.AreEqual("KH-" + (11 + (k < 0 ? 0 : k) + i), a[i].Label);
                        Assert.AreEqual("kc" + (11 + (k < 0 ? 0 : k) + i), a[i].Id);
                        Assert.IsTrue(a[i].Site.InclinationOpen(a[i].IncDeg), a[i].Site.Id + " " + a[i].IncDeg);
                        Assert.IsTrue(Direction(weather, a[i].Wind.Compass), a[i].Wind.Compass);
                        Assert.IsTrue(Time(weather, a[i].TimeId), a[i].TimeId);
                        Assert.GreaterOrEqual(a[i].DayOfYear, 1);
                        Assert.LessOrEqual(a[i].DayOfYear, 365);
                        Assert.AreEqual(a[i].MassKg, a[i].Mission.MassKg);
                        Assert.AreEqual(a[i].CatalogPayloadId, a[i].Mission.PayloadId);
                        Assert.AreEqual(a[i].TargetKind, a[i].Mission.TargetKind);
                        Assert.AreEqual(a[i].BudgetKc, a[i].Mission.BudgetKc);
                        Assert.AreEqual(a[i].RewardKc, a[i].Mission.RewardKc);
                        Assert.IsFalse(a[i].Mission.GentleDefault);
                        Assert.IsFalse(Forbidden(a[i].Name + a[i].Goal + a[i].StatusLine));
                    }
                    if (k == 0) boards.Add(a[0].CellId + "," + a[1].CellId + "," + a[2].CellId);
                }
            }
            Assert.Greater(new HashSet<string>(boards).Count, 1, "profile id should change the k=0 board");
            var first = ContractGen.Offer(pool, sites, weather, "pilot", 0);
            var negative = ContractGen.Offer(pool, sites, weather, "pilot", -3);
            Assert.AreEqual(first[0].CellId, negative[0].CellId);
            Assert.AreEqual(first[1].CellId, negative[1].CellId);
            Assert.AreEqual("KH-11", negative[0].Label);
            Assert.AreEqual("KH-11", first[0].Label);
            Assert.AreEqual(0, first[0].Requirements.Count);
            var late = ContractGen.Offer(pool, sites, weather, "pilot", 9);
            bool sawReliability = false;
            foreach (var offer in late)
                if (offer.Requirements.Contains("reliability")) sawReliability = true;
            Assert.IsTrue(sawReliability);
        }

        [Test, Timeout(300000)]
        public void EveryCell_AndEveryOffer_IsViable()
        {
            Assert.GreaterOrEqual(pool.Cells.Count, 8);
            var seen = new HashSet<string>();
            foreach (var cell in pool.Cells)
            {
                Assert.IsTrue(seen.Add(cell.Id), cell.Id);
                Assert.IsFalse(pool.Rejected.Contains(cell.Id));
                Assert.AreEqual(3, cell.Designs.Count, cell.Id);
                Assert.AreEqual(ContractGen.MassKg(cell.Archetype.BaseMassKg, cell.K), cell.MassKg, 0.5, cell.Id);
                Assert.Greater(cell.BudgetKc, 0);
                Assert.AreEqual(cell.BudgetKc, cell.RewardKc);
                var mission = ContractGen.ToMission(cell);
                Assert.AreEqual(ContractGen.ExtraRequirements(cell.K), RequirementCount(mission), cell.Id);
                if (cell.K >= 3) Assert.IsTrue(mission.MarginNonNegative, cell.Id);
                if (cell.K >= 6) Assert.AreEqual(cell.Archetype.MaxG, mission.MaxG, cell.Id);
                if (cell.K >= 9) Assert.AreEqual(0.98, mission.ReliabilityMin, cell.Id);
                if (cell.Orbit.Kind == "c3") Assert.AreEqual(3, mission.CoastHours, cell.Id);
                var prints = new List<string>();
                foreach (var design in cell.Designs)
                {
                    prints.Add(Fingerprint(design));
                    Assert.AreEqual("contract", design.tier, design.id);
                    Assert.AreEqual(cell.Archetype.CatalogId, design.payloadType, design.id);
                    Assert.AreEqual(cell.MassKg, design.payloadMass, design.id);
                    Assert.IsFalse(FailureMonitor.GentleTier(design), design.id);
                    bool ok = FlightBatch.Viable(catalog, mission, design, pool.Seeds, out var flights);
                    Assert.AreEqual(5, flights.Length, design.id);
                    var first = flights[0];
                    Assert.IsTrue(ok, cell.Id + " " + design.id + " " + first.failure
                        + " apo=" + first.apoKm.ToString("0.0") + " peri=" + first.periKm.ToString("0.0")
                        + " g=" + first.maxG.ToString("0.00") + " cost=" + first.costKc.ToString("0")
                        + " land=" + first.landingGrade + " red=" + first.redRisk
                        + " rel=" + first.reliability.ToString("0.000") + " " + first.criterion);
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
                Assert.AreEqual(3, new HashSet<string>(prints).Count, cell.Id + " designs are not distinct");
                for (int a = 0; a < cell.Designs.Count; a++)
                    for (int b = a + 1; b < cell.Designs.Count; b++)
                        Assert.IsTrue(Distinct(cell.Designs[a], cell.Designs[b]), cell.Id);
            }

            var sites = SiteBook.Load(Golden.Root);
            var weather = WeatherBook.Load(Golden.Root);
            foreach (var offer in ContractGen.Offer(pool, sites, weather, "pilot", 0))
            {
                var mission = offer.Mission;
                foreach (var design in offer.Cell.Designs)
                {
                    bool ok = FlightBatch.Viable(catalog, mission, design, pool.Seeds, out var flights);
                    Assert.IsTrue(ok, offer.Id + " " + offer.CellId + " " + design.id + " " + flights[0].failure);
                }
            }
        }

        static int RequirementCount(CampaignMission mission)
        {
            int n = 0;
            if (mission.MarginNonNegative) n++;
            if (!double.IsNaN(mission.MaxG)) n++;
            if (!double.IsNaN(mission.ReliabilityMin)) n++;
            if (mission.NoRedRisks) n++;
            return n;
        }

        static bool Direction(WeatherBook weather, string compass)
        {
            for (int i = 0; i < weather.Directions.Count; i++)
                if (weather.Directions[i].Id == compass) return true;
            return false;
        }

        static bool Time(WeatherBook weather, string id)
        {
            for (int i = 0; i < weather.Times.Count; i++)
                if (weather.Times[i].Id == id) return true;
            return false;
        }

        static bool Forbidden(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string lower = text.ToLowerInvariant();
            string[] words = { "spacex", "falcon", "starlink", "nasa", "blue origin", "ula", "soyuz", "saturn" };
            foreach (var word in words) if (lower.Contains(word)) return true;
            return false;
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
            for (int i = 0; i < a.stages.Count; i++)
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
