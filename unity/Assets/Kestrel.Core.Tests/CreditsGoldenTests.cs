// Credits prices and the gift rule. The Hopper quote stays 0. A miss pays nothing.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Designer;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CreditsGoldenTests
    {
        [TearDown]
        public void ClearWalletHook() => CareerStore.WalletSource = null;

        [Test]
        public void HopperQuote_IsZero_AndMatchesTheGolden()
        {
            var table = Table();
            var hopper = Template("hopper");
            Assert.AreEqual(0, table.Quote(hopper), "the free starter is the Hopper");
            Assert.AreEqual("hopper", table.FreeStarter);
            foreach (var row in Golden.Cases("credits"))
            {
                if (row.Str("name") != "hopper-free") continue;
                Assert.AreEqual((long)row.Num("quote"), table.Quote(hopper));
            }
        }

        [Test]
        public void Parts_MatchTheTable_AndTheGolden()
        {
            var table = Table();
            var legs = Template("hopper");
            JsonMut.Set(legs.Obj("landing"), "legs", true);
            var staged = Template("hopper");
            staged.Arr("stages").Add(JsonMut.Clone(staged.Arr("stages")[0]));
            Assert.AreEqual(table.Legs, table.Quote(legs));
            Assert.AreEqual(table.ExtraStage, table.Quote(staged));
            foreach (var row in Golden.Cases("credits"))
            {
                string name = row.Str("name");
                if (name == "legs") Assert.AreEqual((long)row.Num("quote"), table.Quote(legs));
                if (name == "extra-stage") Assert.AreEqual((long)row.Num("quote"), table.Quote(staged));
            }
            var strap = Template("hopper");
            Assert.AreEqual(0, strap.Obj("strapOns").Num("count"));
            Assert.AreEqual(0, table.Quote(strap), "a strap-on block with count 0 is not a charge");
        }

        [Test]
        public void Gift_DoesNotPrintMoney_AndAQuoteOfZeroAlwaysFits()
        {
            var wallet = CreditsWallet.Load(null, Table());
            wallet.GiftBaseline(40);
            Assert.IsTrue(wallet.TryApply(0));
            Assert.AreEqual(0, wallet.Balance);
            foreach (var row in Golden.Cases("credits"))
                if (row.Str("name") == "gift-does-not-pay") Assert.AreEqual((long)row.Num("balance"), wallet.Balance);

            wallet.Balance = 10;
            wallet.GiftBaseline(40);
            Assert.IsFalse(wallet.TryApply(64));
            Assert.AreEqual(10, wallet.Balance);
            Assert.AreEqual(40, wallet.PaidQuote);
            wallet.Balance = 24;
            Assert.IsTrue(wallet.TryApply(64));
            Assert.AreEqual(0, wallet.Balance);
            Assert.IsTrue(wallet.TryApply(40));
            Assert.AreEqual(24, wallet.Balance, "only the part the player paid comes back");
            Assert.IsTrue(wallet.TryApply(0));
            Assert.AreEqual(24, wallet.Balance, "stripping the gift adds nothing");
        }

        [Test]
        public void CannotOverspend_AndAMissPaysNothing()
        {
            var wallet = CreditsWallet.Load(null, Table());
            Assert.IsTrue(wallet.TryApply(0));
            Assert.IsFalse(wallet.TryApply(24));
            Assert.AreEqual(0, wallet.Balance);
            Assert.AreEqual(0, wallet.PaidQuote);
            Assert.AreEqual(0, wallet.Award("m1", 1, false, new List<string> { "gold" }));
            Assert.AreEqual(0, wallet.Balance);
            long first = wallet.Award("m1", 1, true, new List<string> { "gold" });
            long repeat = wallet.Award("m1", 1, true, new List<string> { "gold" });
            Assert.AreEqual(28, first);
            Assert.AreEqual(12, repeat);
            foreach (var row in Golden.Cases("credits"))
            {
                if (row.Str("name") == "kh1-one-medal") Assert.AreEqual((long)row.Num("earn"), first);
                if (row.Str("name") == "kh1-repeat") Assert.AreEqual((long)row.Num("earn"), repeat);
            }
        }

        [Test]
        public void Commit_PaysFirstClearOnce_AndAMissPaysZero()
        {
            string dir = Path.Combine(Path.GetTempPath(), "kh-credits-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var campaign = Campaign.Load(Golden.Root);
                var table = Table();
                CareerStore.WalletSource = store => CreditsWallet.Load(store, table);
                var career = CareerStore.Load(new NativeStore(dir, () => 1000, () => 0.2), campaign, 100000);
                var mission = MissionContext.FromMission(campaign, campaign.ById("m1"));
                var facts = new FlightFacts
                {
                    Passed = true,
                    LandingGrade = "Perfect",
                    ApoKm = 10,
                    GoalKm = 10,
                    Margin = 0.1,
                    PeakG = 2,
                    GLimit = 6,
                    Seed = 3,
                };
                var card = FlightDebrief.Commit(career, mission, facts, null, 100000, null);
                Assert.AreEqual(28, card.CreditsEarned);
                Assert.AreEqual(28, card.CreditsBalance);
                Assert.That(card.KcLine, Does.Not.Contain("credit"));

                var again = CareerStore.Load(new NativeStore(dir, () => 2000, () => 0.2), campaign, 100000);
                var repeat = FlightDebrief.Commit(again, mission, facts, null, 100000, null);
                Assert.AreEqual(12, repeat.CreditsEarned);
                Assert.AreEqual(40, repeat.CreditsBalance);

                facts.Passed = false;
                facts.LandingGrade = null;
                var miss = FlightDebrief.Commit(again, mission, facts, null, 100000, null);
                Assert.AreEqual(0, miss.CreditsEarned);
                Assert.AreEqual(40, miss.CreditsBalance);

                var round = CreditsWallet.Load(new NativeStore(dir, () => 3000, () => 0.2), table);
                Assert.AreEqual(40, round.Balance);
                Assert.IsTrue(round.HasRecord);
            }
            finally
            {
                CareerStore.WalletSource = null;
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        static CreditsTable Table()
            => CreditsTable.Parse(File.ReadAllText(Path.Combine(Golden.Root, "data", "credits.json")));

        static JsonObject Template(string id)
        {
            foreach (JsonObject row in MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "designer-templates.json"))).Arr("templates"))
                if (row.Str("id") == id) return JsonMut.CloneObject(row.Obj("design"));
            throw new InvalidOperationException(id);
        }
    }
}
