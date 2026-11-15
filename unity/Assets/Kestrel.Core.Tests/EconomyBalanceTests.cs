// DECISIONS #87 / #100: hardware cost matters, gently (Poly Bridge). Each mission's Starter default costs 5-20% of
// that mission's reward, over budget blocks launch, under budget pays an unspent bonus, the Hopper stays free, the
// range credit keeps every Starter default affordable however many flights miss, and an economy 1 bank is rescaled
// once on load. docs/ECONOMY.md carries the table.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class EconomyBalanceTests
    {
        const double MinShare = 0.05, MaxShare = 0.20;
        Catalog catalog;
        Campaign campaign;
        long perKc;

        [SetUp]
        public void SetUp()
        {
            catalog = StarterFixture.Catalog;
            campaign = StarterFixture.Campaign;
            perKc = CostModel.MinorPerKc(catalog);
        }

        long DefaultMinor(CampaignMission m)
        {
            var rocket = StarterFixture.Rocket(m.Id);
            return CostModel.PriceDesign(catalog, Design.FromJson(rocket.BuildDesign(rocket.Default))).recurring;
        }

        [Test]
        public void StarterDefault_CostsFiveToTwentyPercentOfItsReward()
        {
            var table = new StringBuilder("mission | default Kc | reward Kc | share\n");
            Assert.AreEqual(20, campaign.Missions.Count);
            foreach (var m in campaign.Missions)
            {
                double cost = DefaultMinor(m) / (double)perKc;
                double share = cost / m.RewardKc;
                table.Append("KH-" + m.Index + " | " + cost.ToString("0", CultureInfo.InvariantCulture) + " | " + m.RewardKc.ToString("0", CultureInfo.InvariantCulture)
                    + " | " + (share * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%\n");
                Assert.GreaterOrEqual(share, MinShare, m.Id + " hardware is too cheap to notice");
                Assert.LessOrEqual(share, MaxShare, m.Id + " hardware eats the reward");
                Assert.AreEqual(m.BudgetKc, m.RewardKc, m.Id + " reward equals budget");
            }
            TestContext.WriteLine(table.ToString());
        }

        [Test]
        public void NoStarterSetting_ExceedsBudget()
        {
            // Poly Bridge: over budget blocks launch. Every Starter setting, not just the default, fits under it.
            foreach (var m in campaign.Missions)
            {
                var rocket = StarterFixture.Rocket(m.Id);
                long budget = Career.KcToMinor(m.BudgetKc, perKc);
                foreach (var build in StarterRocket.BuildOrder)
                foreach (var e in StarterRocket.EngineOrder)
                foreach (var stages in StarterRocket.StageValues)
                for (int notch = 0; notch < StarterRocket.FuelNotches; notch++)
                foreach (var boosters in StarterRocket.BoosterValues)
                {
                    if (!rocket.IsStagesAllowed(stages) || !rocket.IsBoostersAllowed(boosters)) continue;
                    var choice = new StarterChoice(build, e, stages, notch, boosters);
                    var design = rocket.BuildDesign(choice);
                    long cost = CostModel.PriceDesign(catalog, Design.FromJson(design)).recurring;
                    Assert.LessOrEqual(cost, budget, m.Id + " " + choice.Key);
                    var check = StarterRocket.CheckDesign(catalog, design, m);
                    Assert.AreNotEqual("over-budget", check.BlockCode, m.Id + " " + choice.Key);
                }
            }
        }

        [Test]
        public void OverBudget_LaunchIsRefused()
        {
            var m = campaign.Mission(1);
            var rocket = StarterFixture.Rocket(m.Id);
            var design = rocket.BuildDesign(rocket.Default);
            long cost = CostModel.PriceDesign(catalog, Design.FromJson(design)).recurring;
            double costKc = cost / (double)perKc;
            Assert.Greater(costKc, 0);
            // Stamp a budget below the quote: Validate must block and name how far over.
            JsonMut.Set(design, "budgetKc", Math.Max(1, costKc * 0.5));
            var report = Validate.Engineering(catalog, design);
            Assert.IsFalse(report.ok);
            var block = report.blocks.Find(b => b.code == "over-budget");
            Assert.IsNotNull(block);
            StringAssert.Contains("Over budget by", block.msg);
            StringAssert.Contains("Kc", block.msg);
            var check = StarterRocket.CheckDesign(catalog, design, m);
            Assert.IsTrue(check.Blocked);
            Assert.AreEqual("over-budget", check.BlockCode);
            StringAssert.Contains("Over budget by", check.BlockReason);
        }

        [Test]
        public void UnderBudget_BonusIsPaid()
        {
            var m = campaign.Mission(1);
            long budget = Career.KcToMinor(m.BudgetKc, perKc);
            long reward = Career.KcToMinor(m.RewardKc, perKc);
            long cost = DefaultMinor(m);
            Assert.Less(cost, budget, "Starter default must sit under the budget");
            long wantBonus = budget - cost;
            var career = Career.Start(campaign, perKc);
            long before = career.BankMinor;
            var pass = career.Settle(m.Index, true, cost, 0, "core");
            Assert.IsTrue(pass.passed);
            Assert.AreEqual(wantBonus, pass.unspentBonusMinor, "unspent bonus equals budget − cost");
            Assert.AreEqual(reward, pass.rewardMinor);
            Assert.AreEqual(reward + wantBonus - cost, pass.deltaMinor, "reward + unspent bonus − hardware");
            Assert.AreEqual(before + pass.deltaMinor, career.BankMinor);
            Assert.Greater(pass.unspentBonusMinor, 0);

            // At the cap the bonus is 0 and the mission purse breaks even before recovery.
            var atCap = Career.Start(campaign, perKc);
            var cap = atCap.Settle(m.Index, true, budget, 0, "core");
            Assert.AreEqual(0, cap.unspentBonusMinor);
            Assert.AreEqual(reward - budget, cap.deltaMinor);
        }

        [Test]
        public void RangeFloor_CoversTheDearestStarterDefault()
        {
            Assert.AreEqual(2, campaign.Economy);
            long floor = Career.KcToMinor(campaign.RangeFloorKc, perKc);
            long start = Career.KcToMinor(campaign.StartingBankKc, perKc);
            Assert.GreaterOrEqual(start, floor, "a new career starts at or above the floor");
            foreach (var m in campaign.Missions)
                Assert.GreaterOrEqual(floor, DefaultMinor(m), m.Id + " default must fit inside the range credit");
        }

        [Test]
        public void WorstBank_EveryStarterDefaultStaysAffordable()
        {
            // Worst plausible player: misses every flight many times before each first clear, sometimes with a rocket
            // far over budget. Hangar items are paid in credits (a separate wallet), never in Kc, so they cannot drain this bank.
            var career = Career.Start(campaign, perKc);
            long huge = Career.KcToMinor(10_000_000, perKc);
            foreach (var m in campaign.Missions)
            {
                long cost = DefaultMinor(m);
                Assert.GreaterOrEqual(career.BankMinor, cost, m.Id + " affordable on arrival");
                for (int i = 0; i < 40; i++)
                {
                    var miss = career.Settle(m.Index, false, i % 8 == 7 ? huge : cost, 0, "core");
                    Assert.IsTrue(miss.accepted);
                    Assert.GreaterOrEqual(miss.rangeCreditMinor, 0);
                    Assert.AreEqual(career.BankMinor, miss.bankMinor);
                    Assert.GreaterOrEqual(career.BankMinor, career.FloorMinor, m.Id + " miss " + i);
                    Assert.GreaterOrEqual(career.BankMinor, cost, m.Id + " default still affordable after miss " + i);
                }
                var pass = career.Settle(m.Index, true, cost, 0, "core");
                Assert.IsTrue(pass.passed);
                Assert.AreEqual(0, pass.rangeCreditMinor, "a pass at the default never needs the range");
            }
            Assert.AreEqual(20, career.Cleared);
        }

        [Test]
        public void RangeCredit_OnlyTopsUpToTheFloor_AndAddsUpOnTheCard()
        {
            var career = Career.Start(campaign, perKc);
            long floor = career.FloorMinor;
            long cost = DefaultMinor(campaign.Mission(1));

            var first = career.Settle(1, false, cost, 0, "core");
            Assert.AreEqual(0, first.rangeCreditMinor, "the start sits above the floor");
            Assert.AreEqual(-cost, first.deltaMinor);

            career.BankMinor = floor + 1000;
            var low = career.Settle(1, false, cost, 0, "core");
            Assert.AreEqual(cost - 1000, low.rangeCreditMinor);
            Assert.AreEqual(floor, career.BankMinor);
            Assert.AreEqual(-cost + low.rangeCreditMinor, low.deltaMinor);
        }

        [Test]
        public void RangeCredit_IsNamedOnTheResultCard()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-econ-card-" + Guid.NewGuid().ToString("N"));
            try
            {
                var nstore = new NativeStore(dir, () => 1_700_000_000_000L, () => 0.25);
                var store = CareerStore.Load(nstore, campaign, perKc);
                store.Career.BankMinor = store.Career.FloorMinor;
                var facts = new FlightFacts
                {
                    Passed = false, EconomyKnown = true, CostMinor = DefaultMinor(campaign.Mission(1)),
                    BudgetKc = campaign.Mission(1).BudgetKc, Seed = 4, DesignId = "miss", ApoKm = 4, GoalKm = 10,
                };
                var card = FlightDebrief.Commit(store, MissionContext.FromCampaign(campaign, "m1", 4), facts,
                    NarrationPool.Load(Golden.Root), perKc, HangarData.LoadFromRepo(Golden.Root));
                Assert.IsTrue(card.EconomySettled);
                Assert.AreEqual(facts.CostMinor, card.RangeCreditMinor);
                Assert.AreEqual(store.Career.FloorMinor, card.BankMinor);
                StringAssert.Contains("Range credit 4.8k Kc.", card.KcLine);
                StringAssert.Contains("Bank 60.0k Kc.", card.KcLine);
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void Reuse_LandedHardwareVisiblyCutsTheBill()
        {
            foreach (var m in campaign.Missions)
            {
                if (!m.LandingRequired) continue;
                var rocket = StarterFixture.Rocket(m.Id);
                var quote = CostModel.PriceDesign(catalog, Design.FromJson(rocket.BuildDesign(rocket.Default)));
                long stage = CostModel.ReuseRefundMinor(CostModel.RecoveredHardwareMinor(quote, true, false), 0);
                Assert.GreaterOrEqual(stage, quote.recurring / 5, m.Id + " a landed core refunds at least a fifth of the rocket");
                if (m.LandBoosters)
                {
                    long both = CostModel.ReuseRefundMinor(CostModel.RecoveredHardwareMinor(quote, true, true), 0);
                    Assert.Greater(both, stage, m.Id + " landed boosters add to the refund");
                }
            }
        }

        [Test]
        public void Hopper_StaysFree()
        {
            var table = CreditsTable.Parse(File.ReadAllText(Path.Combine(Golden.Root, "data", "credits.json")));
            Assert.AreEqual("hopper", table.FreeStarter);
            foreach (JsonObject row in MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "designer-templates.json"))).Arr("templates"))
                if (row.Str("id") == "hopper") Assert.AreEqual(0, table.Quote(row.Obj("design")), "the Hopper quotes 0 credits");
        }

        [Test]
        public void Contracts_PayAboutTenTimesTheirCheapestDesign()
        {
            var o = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "contracts-viable.json")));
            foreach (JsonObject cell in o.Arr("cells"))
            {
                double budget = cell.Num("budgetKc"), min = double.MaxValue, max = 0;
                foreach (JsonObject raw in cell.Arr("designs"))
                {
                    double kc = CostModel.PriceDesign(catalog, Design.FromJson(raw)).recurring / (double)perKc;
                    min = Math.Min(min, kc);
                    max = Math.Max(max, kc);
                }
                string id = cell.Str("id");
                Assert.GreaterOrEqual(min / budget, MinShare, id);
                Assert.LessOrEqual(min / budget, MaxShare, id);
                Assert.LessOrEqual(max, budget, id + " every flown design fits the budget");
            }
        }

        // --- Migration (economy 1 -> 2) ---------------------------------------------------------------

        sealed class Dir : IDisposable
        {
            public readonly string Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kh-econ-" + Guid.NewGuid().ToString("N"));
            public readonly NativeStore Store;
            public Dir() { Store = new NativeStore(Path, () => 1_700_000_000_000L, () => 0.25); }
            public string File(string key) => System.IO.Path.Combine(Path, key + ".json");
            public void Dispose() { if (Directory.Exists(Path)) Directory.Delete(Path, true); }
        }

        static JsonObject OldCareer(long bankMinor, int cleared)
        {
            var o = new JsonObject();
            o.Add("bankMinor", bankMinor);
            o.Add("cleared", cleared);
            o.Add("unlocks", new List<object> { "core-stage", "engines", "fairing", "tanks" });
            o.Add("articleFlights", new JsonObject());
            o.Add("contractsCleared", 0);
            o.Add("loanUsed", false);
            return o;
        }

        [Test]
        public void Migration_RescalesAnEconomy1BankOnce_AndKeepsTheOldFile()
        {
            using (var d = new Dir())
            {
                long old = Career.KcToMinor(8_846_363, perKc);
                d.Store.PutRecord(CareerStore.CareerKey, "career", OldCareer(old, 3));
                string before = System.IO.File.ReadAllText(d.File(CareerStore.CareerKey));

                var store = CareerStore.Load(d.Store, campaign, perKc);
                // KH-1..KH-3 cleared: (100k + 40k + 45k + 70k) / (2M + 2M + 5M + 15M).
                double factor = 255_000.0 / 24_000_000.0;
                Assert.AreEqual(factor, Career.MigrationFactor(campaign, 3), 1e-12);
                long want = (long)Math.Round(old * factor, MidpointRounding.AwayFromZero);
                Assert.IsTrue(store.RescaledBank);
                Assert.AreEqual(old, store.RescaledBankFromMinor);
                Assert.AreEqual(want, store.Career.BankMinor);
                Assert.AreEqual(3, store.Career.Cleared);
                Assert.Greater(store.Career.BankMinor, store.Career.FloorMinor);
                Assert.AreEqual(before, System.IO.File.ReadAllText(d.File(CareerStore.CareerKey) + ".bak"), "the economy 1 file is kept");
                Assert.AreEqual(2d, d.Store.GetRecord(CareerStore.CareerKey).Num("economy"));

                var again = CareerStore.Load(d.Store, campaign, perKc);
                Assert.IsFalse(again.RescaledBank, "a migrated save is never rescaled twice");
                Assert.AreEqual(want, again.Career.BankMinor);
            }
        }

        [Test]
        public void Migration_LiftsASmallOrNegativeBankToTheFloor()
        {
            foreach (long bank in new[] { -Career.KcToMinor(50_000, 100_000), 0L, 8_800L })
            {
                using (var d = new Dir())
                {
                    d.Store.PutRecord(CareerStore.CareerKey, "career", OldCareer(bank, 4));
                    var store = CareerStore.Load(d.Store, campaign, perKc);
                    Assert.IsTrue(store.RescaledBank, bank.ToString());
                    Assert.AreEqual(store.Career.FloorMinor, store.Career.BankMinor, bank.ToString());
                }
            }
        }

        [Test]
        public void Migration_LeavesEconomy2AndFreshSavesAlone()
        {
            using (var d = new Dir())
            {
                var fresh = CareerStore.Load(d.Store, campaign, perKc);
                Assert.IsFalse(fresh.RescaledBank);
                Assert.AreEqual(Career.KcToMinor(campaign.StartingBankKc, perKc), fresh.Career.BankMinor);
                Assert.IsFalse(d.Store.HasBytes(CareerStore.CareerKey), "a fresh load writes nothing");
                fresh.Save();
                Assert.AreEqual(2d, d.Store.GetRecord(CareerStore.CareerKey).Num("economy"));

                var data = OldCareer(Career.KcToMinor(123_456, perKc), 2);
                data.Add("economy", 2);
                d.Store.PutRecord(CareerStore.CareerKey, "career", data);
                var current = CareerStore.Load(d.Store, campaign, perKc);
                Assert.IsFalse(current.RescaledBank);
                Assert.AreEqual(Career.KcToMinor(123_456, perKc), current.Career.BankMinor);
            }
        }

        [Test]
        public void Migration_FactorTracksProgress()
        {
            Assert.AreEqual(100_000.0 / 2_000_000.0, Career.MigrationFactor(campaign, 0), 1e-12);
            double prev = 0;
            for (int k = 0; k <= 20; k++)
            {
                double f = Career.MigrationFactor(campaign, k);
                Assert.Greater(f, 0, k.ToString());
                Assert.Less(f, 1, k.ToString());
                if (k > 0) Assert.AreNotEqual(prev, f);
                prev = f;
            }
            Assert.AreEqual(Career.MigrationFactor(campaign, 20), Career.MigrationFactor(campaign, 99));
        }
    }
}
