// Post-flight narrator, real failure numbers, and the Kc write into the career save.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FlightDebriefTests
    {
        const long Minor = 100;
        const long T0 = 1_700_000_000_000;

        [TearDown]
        public void ClearSession()
        {
            GameSession.Clear();
            FlightDebrief.ClearLatest();
        }

        [Test]
        public void Narration_CoversTheFiveCauses_WithTwoVariantsEach()
        {
            var pool = NarrationPool.Load(Golden.Root);
            Assert.AreEqual("Structural breakup", pool.Title("structural-maxq"));
            Assert.AreEqual("Engine failure", pool.Title("engine-out"));
            Assert.AreEqual("Engine failure", pool.Title("engine-overpressure"));
            Assert.AreEqual("Engine failure", pool.Title("bad-mixture"));
            Assert.AreEqual("Tumble", pool.Title("tumble"));
            Assert.AreEqual("Out of fuel", pool.Title("prop-depletion"));
            Assert.AreEqual("Stage collision", pool.Title("separation-collision"));
            Assert.GreaterOrEqual(pool.VariantCount("structural-maxq", "q", "any"), 2);
            Assert.GreaterOrEqual(pool.VariantCount("engine-out", "engine-out", "any"), 2);
            Assert.GreaterOrEqual(pool.VariantCount("engine-out", "engine-out", "redundant"), 2);
            Assert.GreaterOrEqual(pool.VariantCount("engine-overpressure", "pc-cap", "any"), 2);
            Assert.GreaterOrEqual(pool.VariantCount("tumble", "alpha", "any"), 2);
            Assert.GreaterOrEqual(pool.VariantCount("tumble", "stability", "any"), 2);
            Assert.GreaterOrEqual(pool.VariantCount("prop-depletion", "prop-depletion", "any"), 2);
            Assert.GreaterOrEqual(pool.VariantCount("separation-collision", "separation-collision", "any"), 2);

            string[] banned = { "you failed", "you lost", "you missed", "buy", "purchase", "streak", "timer", "spacex", "falcon", "nasa", "starship" };
            for (int i = 0; i < pool.Lines.Count; i++)
            {
                var line = pool.Lines[i];
                string blob = (line.Kid + " " + line.Eng).ToLowerInvariant();
                for (int b = 0; b < banned.Length; b++)
                    StringAssert.DoesNotContain(banned[b], blob, line.Id);
            }
        }

        [Test]
        public void Narration_SameSeedRepeats_AndRedundantAvionicsChangesTheLine()
        {
            var pool = NarrationPool.Load(Golden.Root);
            var a = pool.Pick("structural-maxq", "q", "any", 4);
            var b = pool.Pick("structural-maxq", "q", "any", 4);
            Assert.AreSame(a, b);
            bool differed = false;
            for (ulong seed = 1; seed < 40 && !differed; seed++)
                differed = pool.Pick("structural-maxq", "q", "any", seed).Id != a.Id;
            Assert.IsTrue(differed, "two q lines exist, so some seed says the other one");

            var spare = pool.Pick("engine-out", "engine-out", "redundant", 3);
            StringAssert.StartsWith("narr.engine.out.redundant", spare.Id);
            var plain = pool.Pick("engine-out", "engine-out", "single", 3);
            StringAssert.DoesNotStartWith("narr.engine.out.redundant", plain.Id);
        }

        [Test]
        public void Report_QuotesTheRealBreakupNumbers()
        {
            var pool = NarrationPool.Load(Golden.Root);
            var structural = FailureModel.Explain(new FailureHit { on = true, kind = FailureKind.StructuralMaxQ, cause = 0, value = 41000, limit = 33000, engine = -1, stage = 1 }, 62, 41000);
            var engine = FailureModel.Explain(new FailureHit { on = true, kind = FailureKind.EngineOut, cause = 0, value = 1, limit = 7, engine = 2, stage = 1 }, 48, 0);
            var pressure = FailureModel.Explain(new FailureHit { on = true, kind = FailureKind.EngineOverpressure, cause = 1, value = 80, limit = 60, engine = 0, stage = 1 }, 12, 0);
            var tumble = FailureModel.Explain(new FailureHit { on = true, kind = FailureKind.Tumble, cause = 1, value = 22, limit = 15, engine = -1, stage = 1 }, 30, 0);
            var fuel = FailureModel.Explain(new FailureHit { on = true, kind = FailureKind.PropDepletion, cause = 0, value = 40, limit = 0, engine = -1, stage = 1 }, 40, 0);
            var bump = FailureModel.Explain(new FailureHit { on = true, kind = FailureKind.SeparationCollision, cause = 0, value = 1.2, limit = 2.5, engine = -1, stage = 1 }, 80, 0);

            ExpectCause(pool, structural, "Structural breakup", "41 kPa", "33 kPa");
            ExpectCause(pool, engine, "Engine failure", "1", "7");
            ExpectCause(pool, pressure, "Engine failure", "80", "60");
            ExpectCause(pool, tumble, "Tumble", "22", "15");
            var dry = ExpectCause(pool, fuel, "Out of fuel", "40", null);
            Assert.That(dry.Numbers, Does.Contain("Short 100 m/s"));
            ExpectCause(pool, bump, "Stage collision", "1.20", "2.50");
        }

        [Test]
        public void Pass_WritesRewardStarsAndThrumsToolbox()
        {
            using (var fx = new Fixture())
            {
                var store = fx.Load();
                long bank = store.Career.BankMinor;
                var mission = MissionFor(fx.Campaign, "m1", 11);
                var facts = new FlightFacts
                {
                    Passed = true,
                    EconomyKnown = true,
                    Margin = 0.05,
                    CostMinor = 400,
                    BudgetKc = 50,
                    PeakG = 2,
                    GLimit = 6,
                    Seed = 11,
                    DesignId = "pencil",
                    TimingMedal = AscentMedal.Silver,
                    LandingGrade = "Perfect",
                    ApoKm = 10,
                    GoalKm = 10,
                };
                var card = FlightDebrief.Commit(store, mission, facts, NarrationPool.Load(Golden.Root), Minor, HangarData.LoadFromRepo(Golden.Root));
                Assert.IsTrue(card.Passed);
                Assert.IsTrue(card.WroteCareer);
                Assert.IsTrue(card.EconomySettled);
                Assert.AreEqual(3, card.Stars);
                Assert.That(card.StarsLine, Is.EqualTo("Stars: 3."));
                Assert.That(card.Medals, Does.Contain("gold"));
                Assert.That(card.Medals, Does.Contain("timing-silver"));
                Assert.That(card.MedalsLine, Does.Contain("Landing gold"));
                Assert.That(card.MedalsLine, Does.Contain("Timing silver"));
                Assert.AreEqual(1000, card.RewardMinor);
                Assert.AreEqual(400, card.CostMinor);
                Assert.AreEqual(bank - 400 + 1000, card.BankMinor);
                Assert.That(card.KcLine, Does.Contain("Reward 10 Kc"));
                Assert.That(card.KcLine, Does.Contain("Hardware 4 Kc"));
                Assert.That(card.KcLine, Does.Not.Contain("you failed"));
                Assert.IsTrue(store.Career.HasUnlock("fins"));
                Assert.IsTrue(store.Career.HasUnlock("decal_thrums_toolbox"));
                Assert.That(card.CosmeticLine, Does.Contain("Thrum's Toolbox"));
                Assert.That(card.UnlockLine, Does.Contain("fins"));

                var again = fx.Load();
                Assert.AreEqual(card.BankMinor, again.Career.BankMinor);
                Assert.AreEqual(1, again.Career.Cleared);
                var row = again.Mission("m1");
                Assert.AreEqual(3, row.BestStars);
                Assert.AreEqual(1, row.Attempts);
                Assert.AreEqual(11L, row.LastSeed);
                Assert.AreEqual("pencil", row.LastDesignId);
                CollectionAssert.Contains(row.BestMedals, "gold");
                CollectionAssert.Contains(row.BestMedals, "timing-silver");
            }
        }

        [Test]
        public void Breakup_WritesNoReward_AndKeepsTheCause()
        {
            using (var fx = new Fixture())
            {
                var store = fx.Load();
                long bank = store.Career.BankMinor;
                var mission = MissionFor(fx.Campaign, "m1", 5);
                var ev = FailureModel.Explain(new FailureHit { on = true, kind = FailureKind.StructuralMaxQ, cause = 0, value = 41000, limit = 33000, engine = -1, stage = 1 }, 62, 41000);
                var facts = new FlightFacts
                {
                    Passed = true,
                    EconomyKnown = true,
                    Failure = ev,
                    CostMinor = 400,
                    BudgetKc = 50,
                    Seed = 5,
                    DesignId = "thin",
                };
                var card = FlightDebrief.Commit(store, mission, facts, NarrationPool.Load(Golden.Root), Minor, HangarData.LoadFromRepo(Golden.Root));
                Assert.IsFalse(card.Passed, "a breakup is not a pass, even if the caller hoped");
                Assert.AreEqual(0, card.Stars);
                Assert.AreEqual("Structural breakup", card.CauseTitle);
                Assert.That(card.Numbers, Does.Contain("41 kPa"));
                Assert.That(card.Numbers, Does.Contain("33 kPa"));
                Assert.That(card.Kid, Does.Contain("41 kPa"));
                Assert.AreEqual(0, card.RewardMinor);
                Assert.AreEqual(bank - 400, store.Career.BankMinor);
                Assert.That(card.KcLine, Does.StartWith("No reward."));
                Assert.AreEqual(0, store.Career.Cleared);
                Assert.IsFalse(store.Career.HasUnlock("decal_thrums_toolbox"));
                Assert.AreEqual(0, store.Mission("m1").BestStars);
                Assert.AreEqual(1, store.Mission("m1").Attempts);
            }
        }

        [Test]
        public void FreeBuild_WritesNoKc_AndReplayDoesNotPayTwice()
        {
            using (var fx = new Fixture())
            {
                var store = fx.Load();
                var before = File.Exists(fx.File(CareerStore.CareerKey));
                var free = MissionContext.Free(3);
                var facts = new FlightFacts { Passed = true, EconomyKnown = true, CostMinor = 400, BudgetKc = 10, Margin = 0.05, PeakG = 1, GLimit = 6, TimingMedal = AscentMedal.Gold, Seed = 3 };
                var card = FlightDebrief.Commit(store, free, facts, NarrationPool.Empty, Minor, null);
                Assert.IsTrue(card.FreeBuild);
                Assert.IsFalse(card.WroteCareer);
                Assert.IsFalse(card.EconomySettled);
                Assert.That(card.KcLine, Does.Contain("No Kc"));
                Assert.AreEqual(store.Career.BankMinor, Career.Start(fx.Campaign, Minor).BankMinor);
                Assert.AreEqual(before, File.Exists(fx.File(CareerStore.CareerKey)));

                var mission = MissionFor(fx.Campaign, "m1", 8);
                var pass = new FlightFacts { Passed = true, EconomyKnown = true, CostMinor = 400, BudgetKc = 50, Margin = 0, PeakG = 9, GLimit = 6, Seed = 8, DesignId = "a" };
                var first = FlightDebrief.Commit(store, mission, pass, NarrationPool.Empty, Minor, null);
                Assert.AreEqual(1, first.Stars);
                long bank = store.Career.BankMinor;
                var better = new FlightFacts { Passed = true, EconomyKnown = true, CostMinor = 400, BudgetKc = 50, Margin = 0.05, PeakG = 2, GLimit = 6, Seed = 8, DesignId = "b" };
                var second = FlightDebrief.Commit(store, mission, better, NarrationPool.Empty, Minor, null);
                Assert.IsFalse(second.EconomySettled);
                Assert.AreEqual(3, second.Stars);
                Assert.AreEqual(bank, store.Career.BankMinor);
                Assert.That(second.KcLine, Does.Contain("first time"));
                Assert.AreEqual(2, store.Mission("m1").Attempts);
                Assert.AreEqual(3, store.Mission("m1").BestStars);
            }
        }

        [Test]
        public void Routes_KeepTheSeed_AndOfferTheFourExits()
        {
            var campaign = Campaign.Load(Golden.Root);
            GameSession.Apply(RouteId.MissionChosen, campaign, "m1");
            GameSession.Current.Seed = 42;
            var again = ReportRoutes.Again();
            Assert.AreEqual(SessionScreens.Design, again.Screen);
            Assert.AreEqual("m1", again.Session.MissionId);
            Assert.AreEqual(42UL, again.Session.Seed);
            Assert.AreEqual(42UL, GameSession.Current.Seed);

            var next = ReportRoutes.Next(campaign);
            Assert.AreEqual(SessionScreens.Missions, next.Screen);
            Assert.AreEqual("m2", next.Session.MissionId);
            Assert.AreEqual(42UL, next.Session.Seed);

            Assert.AreEqual(SessionScreens.Missions, ReportRoutes.Missions().Screen);
            var home = ReportRoutes.Home();
            Assert.AreEqual(SessionScreens.Home, home.Screen);
            Assert.IsNull(home.Session);
            Assert.AreEqual("One more launch", ReportRoutes.AgainLabel);
            Assert.That(ReportRoutes.AgainTip, Does.Contain("same seed"));
        }

        [Test]
        public void LookUnlocks_AreEarnedByTheMission_NotFromTheStart()
        {
            var look = HangarData.LoadFromRepo(Golden.Root);
            var toolbox = look.Decal("decal_thrums_toolbox");
            Assert.IsNotNull(toolbox);
            Assert.AreEqual("mission", toolbox.earn.type);
            Assert.AreEqual("KH-1", toolbox.earn.missionId);
            Assert.AreEqual("patch", toolbox.content);
            Assert.IsFalse(HangarProgress.None.Earned(toolbox.earn));
            var hopped = new HangarProgress();
            hopped.missions.Add("KH-1");
            Assert.IsTrue(hopped.Earned(toolbox.earn));
            Assert.AreEqual("Tidy Pin", FlightDebrief.CosmeticName("decal_tidy_pin"));
            Assert.AreEqual("Homecoming", FlightDebrief.CosmeticName("decal_homecoming"));
        }

        static FlightDebrief ExpectCause(NarrationPool pool, FailureEvent ev, string title, string has, string also)
        {
            var facts = new FlightFacts
            {
                Failure = ev,
                Passed = false,
                EconomyKnown = false,
                Seed = 2,
                Margin = ev.kind == "prop-depletion" ? -0.1 : 0,
                RequiredMps = ev.kind == "prop-depletion" ? 1000 : 0,
            };
            var card = FlightDebrief.Compose(null, null, facts, pool, Minor, null);
            Assert.AreEqual(title, card.CauseTitle, ev.kind);
            Assert.That(card.Numbers, Does.Contain(has), ev.kind);
            if (also != null) Assert.That(card.Numbers, Does.Contain(also), ev.kind);
            Assert.That(card.NarratorKid, Does.Contain(has), card.NarratorId);
            if (also != null && ev.kind != "prop-depletion") Assert.That(card.NarratorKid, Does.Contain(also), card.NarratorId);
            Assert.That(card.Kid, Is.EqualTo(ev.kid));
            Assert.That(card.Engineer, Is.EqualTo(ev.engineer));
            Assert.IsFalse(card.NarratorKid.Contains("{"), card.NarratorId);
            Assert.IsFalse(card.NarratorEng.Contains("{"), card.NarratorId);
            Assert.That(card.NarratorKid.ToLowerInvariant(), Does.Not.Contain("you failed"));
            return card;
        }

        static MissionContext MissionFor(Campaign campaign, string id, ulong seed)
        {
            var ctx = MissionContext.FromCampaign(campaign, id, seed);
            Assert.IsNotNull(ctx);
            return ctx;
        }

        sealed class Fixture : IDisposable
        {
            public readonly string Dir;
            public readonly NativeStore Store;
            public readonly Campaign Campaign;

            public Fixture()
            {
                Dir = Path.Combine(Path.GetTempPath(), "kh-debrief-" + Guid.NewGuid().ToString("N"));
                Campaign = Sample();
                Store = new NativeStore(Dir, () => T0, () => 0.25);
            }

            public CareerStore Load() => CareerStore.Load(Store, Campaign, Minor);
            public string File(string key) => Path.Combine(Dir, key + ".json");

            public void Dispose()
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }

            static Campaign Sample()
            {
                return Campaign.FromJson(Obj(
                    ("version", 1d),
                    ("startingBankKc", 100d),
                    ("starterUnlocks", Arr("core-stage", "engines", "tanks", "fairing")),
                    ("seeds", Arr(1d)),
                    ("missions", new List<object> { Mission(1, "m1", "First Hop", 10d, "fins", "nose"), Mission(2, "m2", "Edge", 20d, "stage-2") })));
            }

            static JsonObject Mission(int index, string id, string name, double reward, params string[] unlocks)
            {
                var list = new List<object>();
                foreach (var unlock in unlocks) list.Add(unlock);
                return Obj(
                    ("index", (double)index),
                    ("id", id),
                    ("name", name),
                    ("payloadId", "kid-box"),
                    ("massKg", 50d),
                    ("volumeM3", 0.2),
                    ("diameterM", 0.6),
                    ("target", Obj(("kind", "apogee"), ("apoKm", 10d))),
                    ("requirements", new List<object>()),
                    ("budgetKc", 50d),
                    ("rewardKc", reward),
                    ("unlocks", list),
                    ("gentleDefault", true));
            }

            static JsonObject Obj(params (string k, object v)[] pairs)
            {
                var o = new JsonObject();
                foreach (var (k, v) in pairs) o.Add(k, v);
                return o;
            }

            static List<object> Arr(params object[] xs) => new List<object>(xs);
        }
    }
}
