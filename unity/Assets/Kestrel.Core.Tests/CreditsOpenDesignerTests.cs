// F7: "Credits change with no purchase: 0, then 40 after KH-1, then 36 after opening and leaving the designer."
// Rule (docs/ECONOMY.md, Spend; DECISIONS #67): credits are spent on parts. A rocket that was already on the
// pad (an older save, a mission handoff, a stock rocket) is a gift. Only the part of the quote the player
// bought comes back. So opening the designer never moves the balance; only an edit pays or refunds.
// SaveHooks.Bind opens the designer with CatchUp(quote), which used to pay the gap whenever the balance could.
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CreditsOpenDesignerTests
    {
        static CreditsTable Table()
            => CreditsTable.Parse(File.ReadAllText(Path.Combine(Golden.Root, "data", "credits.json")));

        static JsonObject Starter(string missionId)
        {
            var catalog = Catalog.LoadFromRepo(Golden.Root);
            var hangar = HangarData.LoadFromRepo(Golden.Root);
            var campaign = Campaign.Load(Golden.Root);
            var choice = HomeAutoBuild.Choose(catalog, hangar.templates, campaign.ById(missionId));
            Assert.IsNotNull(choice?.Design, missionId);
            return choice.Design;
        }

        /// <summary>The live sequence: Hopper on a new hangar, KH-1 twice (28 + 12), then DESIGN FIRST opens KH-2's starter.</summary>
        [Test]
        public void OpeningTheDesignerOnTheKh2Starter_SpendsNothing()
        {
            var table = Table();
            long kh2 = table.Quote(Starter("m2"));
            Assert.That(kh2, Is.GreaterThan(0), "KH-2's starter has a priced part, so the old open path charged it");

            var wallet = CreditsWallet.Load(null, table);
            wallet.GiftBaseline(0);
            wallet.Award("m1", 1, true, new List<string> { "gold" });
            wallet.Award("m1", 1, true, new List<string> { "gold" });
            Assert.AreEqual(40, wallet.Balance);

            wallet.CatchUp(kh2);
            Assert.AreEqual(40, wallet.Balance, "opening the designer is not a purchase");
            Assert.AreEqual(kh2, wallet.PaidQuote, "the open rocket is the new baseline");

            Assert.IsTrue(wallet.TryApply(0));
            Assert.AreEqual(40, wallet.Balance, "stripping the handed-over rocket pays nothing back");
        }

        [Test]
        public void OpeningACheaperRocket_RefundsNothing_AndEditsStillPay()
        {
            var table = Table();
            var wallet = CreditsWallet.Load(null, table);
            wallet.GiftBaseline(0);
            wallet.Balance = 100;
            Assert.IsTrue(wallet.TryApply(table.Legs));
            Assert.AreEqual(100 - table.Legs, wallet.Balance, "an edit pays the difference");

            wallet.CatchUp(0);
            Assert.AreEqual(100 - table.Legs, wallet.Balance, "opening another rocket is not a sale");
            Assert.AreEqual(0, wallet.PaidQuote);

            Assert.IsTrue(wallet.TryApply(table.Legs));
            Assert.AreEqual(100 - 2 * table.Legs, wallet.Balance, "legs on the new rocket are a new purchase");
            Assert.IsTrue(wallet.TryApply(0));
            Assert.AreEqual(100 - table.Legs, wallet.Balance, "and come back in full when removed");
        }

        [Test]
        public void ReopeningTheSameRocket_KeepsWhatThePlayerPaid()
        {
            var table = Table();
            var wallet = CreditsWallet.Load(null, table);
            wallet.GiftBaseline(0);
            wallet.Balance = 40;
            Assert.IsTrue(wallet.TryApply(table.Legs));
            wallet.CatchUp(table.Legs);
            Assert.AreEqual(40 - table.Legs, wallet.Balance);
            Assert.IsTrue(wallet.TryApply(0));
            Assert.AreEqual(40, wallet.Balance, "a part the player paid for still refunds in full");
        }
    }
}
