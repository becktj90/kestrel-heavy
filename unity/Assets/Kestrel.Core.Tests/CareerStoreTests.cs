// Career store: schema stays 1, progress is migrated only when it already holds a profile, and the bytes are deterministic.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Save;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CareerStoreTests
    {
        const long MinorPerKc = 100;
        const long T0 = 1_700_000_000_000;

        [Test]
        public void Schema_Stays1()
        {
            Assert.AreEqual(1, NativeStore.Schema);
            Assert.AreEqual("kh.save.v1.career", CareerStore.CareerKey);
            Assert.AreEqual("kh.save.v1.missions", CareerStore.MissionsKey);
            Assert.IsTrue(NativeStore.CheckKey(CareerStore.CareerKey));
            Assert.IsTrue(NativeStore.CheckKey(CareerStore.MissionsKey));
        }

        [Test]
        public void FreshLoad_WritesNothing_AndMatchesANewCareer()
        {
            using (var fx = new Fixture())
            {
                var before = Snapshot(fx.Dir);
                var store = fx.Load();
                Assert.IsFalse(store.MigratedFromProgress);
                Assert.AreEqual(0, store.ContractsCleared);
                Assert.IsFalse(store.LoanUsed);
                Assert.AreEqual(0, store.Missions.Count);
                Assert.IsNull(store.Mission("m1"));
                AssertSameCareer(fx.Campaign, store.Career);
                Assert.AreEqual(before, Snapshot(fx.Dir));
            }
        }

        [Test]
        public void ExistingPlayer_ProgressAndDesignStayByteForByte()
        {
            using (var fx = new Fixture())
            {
                fx.Store.PutDesign(Obj(("name", "Scout")), "one", "Scout");
                fx.Store.PutProgress(Obj(("orbitsCompleted", 4d), ("engineerEnabled", true)));
                var before = Snapshot(fx.Dir);
                var store = fx.Load();
                Assert.IsFalse(store.MigratedFromProgress);
                AssertSameCareer(fx.Campaign, store.Career);
                Assert.IsFalse(store.Career.HasUnlock("fins"));
                Assert.AreEqual(0, store.Career.Cleared);
                Assert.AreEqual(before, Snapshot(fx.Dir));

                var progress = fx.Store.GetProgress();
                Assert.AreEqual(4d, progress.Num("orbitsCompleted"));
                Assert.IsTrue(progress.Bool("engineerEnabled"));
                Assert.AreEqual("Scout", fx.Store.GetDesign("one").Str("name"));
            }
        }

        [Test]
        public void Save_WritesSchema1Envelopes_AndLeavesProgressUntouched()
        {
            using (var fx = new Fixture())
            {
                fx.Store.PutProgress(Obj(("orbitsCompleted", 1d), ("engineerEnabled", false)));
                string progressBefore = File.ReadAllText(fx.File(NativeStore.ProgressKey));
                var store = fx.Load();
                store.Save();
                Assert.AreEqual(progressBefore, File.ReadAllText(fx.File(NativeStore.ProgressKey)));

                string career = "{\"schema\":1,\"kind\":\"career\",\"id\":\"career\",\"name\":\"career\",\"createdAt\":1700000000000,\"updatedAt\":1700000000000,\"data\":{\"bankMinor\":10000,\"cleared\":0,\"unlocks\":[\"core-stage\",\"engines\",\"fairing\",\"tanks\"],\"articleFlights\":{},\"contractsCleared\":0,\"loanUsed\":false}}";
                string missions = "{\"schema\":1,\"kind\":\"missions\",\"id\":\"missions\",\"name\":\"missions\",\"createdAt\":1700000000000,\"updatedAt\":1700000000000,\"data\":{}}";
                Assert.AreEqual(career, File.ReadAllText(fx.File(CareerStore.CareerKey)));
                Assert.AreEqual(missions, File.ReadAllText(fx.File(CareerStore.MissionsKey)));

                fx.Now = T0 + 500;
                store.ContractsCleared = 3;
                store.Save();
                var env = MiniJson.ParseObject(File.ReadAllText(fx.File(CareerStore.CareerKey)));
                Assert.AreEqual(1d, env.Num("schema"));
                Assert.AreEqual(T0, env.Num("createdAt"));
                Assert.AreEqual(T0 + 500d, env.Num("updatedAt"));
                Assert.AreEqual(3d, env.Obj("data").Num("contractsCleared"));
                Assert.AreEqual(progressBefore, File.ReadAllText(fx.File(NativeStore.ProgressKey)));
            }
        }

        [Test]
        public void RoundTrip_KeepsBankUnlocksAndMissionBests()
        {
            using (var fx = new Fixture())
            {
                var store = fx.Load();
                var pass = store.Career.Settle(1, true, 400, 500, "core");
                Assert.IsTrue(pass.accepted && pass.passed);
                store.ContractsCleared = 2;
                store.RecordAttempt("m1", 2, 1400, new[] { "silver", "silver" }, 4, "design-1");
                store.RecordAttempt("m10", 1, 1000, new[] { "bronze" }, 5, "design-2");
                store.RecordAttempt("m2", 0, 0, null, null, null);

                string missions = "{\"schema\":1,\"kind\":\"missions\",\"id\":\"missions\",\"name\":\"missions\",\"createdAt\":1700000000000,\"updatedAt\":1700000000000,\"data\":{"
                    + "\"m1\":{\"bestStars\":2,\"bestScore\":1400,\"bestMedals\":[\"silver\"],\"attempts\":1,\"lastSeed\":4,\"lastDesignId\":\"design-1\"},"
                    + "\"m10\":{\"bestStars\":1,\"bestScore\":1000,\"bestMedals\":[\"bronze\"],\"attempts\":1,\"lastSeed\":5,\"lastDesignId\":\"design-2\"},"
                    + "\"m2\":{\"bestStars\":0,\"bestScore\":0,\"bestMedals\":[],\"attempts\":1,\"lastSeed\":null,\"lastDesignId\":null}}}";
                Assert.AreEqual(missions, File.ReadAllText(fx.File(CareerStore.MissionsKey)));

                var again = fx.Load();
                Assert.IsFalse(again.MigratedFromProgress);
                Assert.AreEqual(store.Career.BankMinor, again.Career.BankMinor);
                Assert.AreEqual(1, again.Career.Cleared);
                Assert.IsTrue(again.Career.MissionOpen(2));
                Assert.IsTrue(again.Career.HasUnlock("fins"));
                Assert.IsTrue(again.Career.HasUnlock("nose"));
                Assert.IsFalse(again.Career.HasUnlock("stage-2"));
                Assert.AreEqual(1, again.Career.ArticleFlights["core"]);
                Assert.AreEqual(2, again.ContractsCleared);
                Assert.AreEqual(2, again.Mission("m1").BestStars);
                Assert.AreEqual(1400d, again.Mission("m1").BestScore);
                CollectionAssert.AreEqual(new[] { "silver" }, again.Mission("m1").BestMedals);
                Assert.AreEqual(4L, again.Mission("m1").LastSeed);
                Assert.AreEqual("design-1", again.Mission("m1").LastDesignId);
                Assert.IsNull(again.Mission("m2").LastSeed);
                Assert.IsNull(again.Mission("m2").LastDesignId);
                var career = MiniJson.ParseObject(File.ReadAllText(fx.File(CareerStore.CareerKey))).Obj("data");
                CollectionAssert.AreEqual(new[] { "core" }, new List<string>(career.Obj("articleFlights").Order));
                CollectionAssert.AreEqual(
                    new[] { "core-stage", "engines", "fairing", "fins", "nose", "tanks" },
                    Strings(career.Arr("unlocks")));
            }
        }

        [Test]
        public void RecordAttempt_StarsThenScore_AndRejectsABadCall()
        {
            using (var fx = new Fixture())
            {
                var store = fx.Load();
                store.RecordAttempt("m1", 1, 500, new[] { "bronze" }, 1, "a");
                store.RecordAttempt("m1", 1, 400, new[] { "gold" }, 2, "b");
                Assert.AreEqual(1, store.Mission("m1").BestStars);
                Assert.AreEqual(500d, store.Mission("m1").BestScore);
                CollectionAssert.AreEqual(new[] { "bronze" }, store.Mission("m1").BestMedals);
                Assert.AreEqual(2, store.Mission("m1").Attempts);
                Assert.AreEqual(2L, store.Mission("m1").LastSeed);
                Assert.AreEqual("b", store.Mission("m1").LastDesignId);

                store.RecordAttempt("m1", 1, 800, new[] { "silver" }, 3, "c");
                Assert.AreEqual(800d, store.Mission("m1").BestScore);
                CollectionAssert.AreEqual(new[] { "silver" }, store.Mission("m1").BestMedals);

                store.RecordAttempt("m1", 3, 100, new[] { "gold" }, 4, "d");
                Assert.AreEqual(3, store.Mission("m1").BestStars);
                Assert.AreEqual(100d, store.Mission("m1").BestScore);
                CollectionAssert.AreEqual(new[] { "gold" }, store.Mission("m1").BestMedals);
                Assert.AreEqual(4, store.Mission("m1").Attempts);

                string before = File.ReadAllText(fx.File(CareerStore.MissionsKey));
                Assert.Throws<ArgumentOutOfRangeException>(() => store.RecordAttempt("m1", 4, 0, null, null, null));
                Assert.Throws<ArgumentException>(() => store.RecordAttempt("m1", 1, double.NaN, null, null, null));
                Assert.Throws<ArgumentException>(() => store.RecordAttempt("../m1", 1, 0, null, null, null));
                Assert.Throws<ArgumentOutOfRangeException>(() => store.RecordAttempt("m1", 1, 0, null, CareerStore.SafeInteger + 1, null));
                Assert.AreEqual(before, File.ReadAllText(fx.File(CareerStore.MissionsKey)));
                Assert.AreEqual(4, store.Mission("m1").Attempts);
            }
        }

        [Test]
        public void ProgressProfile_MigratesOnce_AndIsNotRewritten()
        {
            using (var fx = new Fixture())
            {
                var progress = Obj(
                    ("orbitsCompleted", 2d),
                    ("engineerEnabled", true),
                    ("bankMinor", 4321d),
                    ("cleared", 1d),
                    ("unlocks", Arr("fins", "core-stage")),
                    ("articleFlights", Obj(("booster", 2d), ("core", 1d))),
                    ("contractsCleared", 4d),
                    ("loanUsed", true),
                    ("missions", Obj(("m1", MissionRow(3, 1700, Arr("gold"), 2, 7, "d-old")))));
                fx.Store.PutProgress(progress);
                string before = File.ReadAllText(fx.File(NativeStore.ProgressKey));
                var store = fx.Load();
                Assert.IsTrue(store.MigratedFromProgress);
                Assert.AreEqual(4321L, store.Career.BankMinor);
                Assert.AreEqual(1, store.Career.Cleared);
                Assert.IsTrue(store.Career.HasUnlock("fins"));
                Assert.IsTrue(store.Career.HasUnlock("core-stage"));
                Assert.IsFalse(store.Career.HasUnlock("engines"));
                Assert.AreEqual(2, store.Career.ArticleFlights["booster"]);
                Assert.AreEqual(1, store.Career.ArticleFlights["core"]);
                Assert.AreEqual(4, store.ContractsCleared);
                Assert.IsTrue(store.LoanUsed);
                Assert.AreEqual(3, store.Mission("m1").BestStars);
                Assert.AreEqual(1700d, store.Mission("m1").BestScore);
                Assert.AreEqual(7L, store.Mission("m1").LastSeed);
                Assert.AreEqual(before, File.ReadAllText(fx.File(NativeStore.ProgressKey)));
                Assert.AreEqual(2d, fx.Store.GetProgress().Num("orbitsCompleted"));
                Assert.IsTrue(fx.Store.GetProgress().Bool("engineerEnabled"));

                fx.Store.PutProgress(Obj(("bankMinor", 1d), ("cleared", 9d), ("orbitsCompleted", 2d)));
                var second = fx.Load();
                Assert.IsFalse(second.MigratedFromProgress);
                Assert.AreEqual(4321L, second.Career.BankMinor);
                Assert.AreEqual(1, second.Career.Cleared);
                Assert.AreEqual(3, second.Mission("m1").BestStars);
                Assert.AreEqual(4, second.ContractsCleared);
            }
        }

        [TestCase("profile")]
        [TestCase("career")]
        public void NestedProfile_MigratesAndLeavesProgressUntouched(string nest)
        {
            using (var fx = new Fixture())
            {
                var progress = Obj(
                    ("orbitsCompleted", 1d),
                    ("engineerEnabled", false),
                    (nest, Obj(
                        ("bankMinor", 80d),
                        ("cleared", 0d),
                        ("missions", Obj(("kc1", MissionRow(2, 1100, Arr("silver"), 1, 3, "contract-a")))))));
                fx.Store.PutProgress(progress);
                string before = File.ReadAllText(fx.File(NativeStore.ProgressKey));
                var store = fx.Load();
                Assert.IsTrue(store.MigratedFromProgress);
                Assert.AreEqual(80L, store.Career.BankMinor);
                Assert.AreEqual(2, store.Mission("kc1").BestStars);
                Assert.AreEqual("contract-a", store.Mission("kc1").LastDesignId);
                Assert.AreEqual(before, File.ReadAllText(fx.File(NativeStore.ProgressKey)));
                Assert.AreEqual(1d, fx.Store.GetProgress().Num("orbitsCompleted"));
            }
        }

        [Test]
        public void TopLevelProfile_WinsOverTheNestedCopy()
        {
            using (var fx = new Fixture())
            {
                fx.Store.PutProgress(Obj(
                    ("bankMinor", 10d),
                    ("profile", Obj(("bankMinor", 99d), ("loanUsed", true)))));
                var store = fx.Load();
                Assert.AreEqual(10L, store.Career.BankMinor);
                Assert.IsTrue(store.LoanUsed);
            }
        }

        [Test]
        public void ClearedWithoutUnlocks_FillsFromTheCampaign_ExplicitListIsKept()
        {
            using (var fx = new Fixture())
            {
                fx.Store.PutProgress(Obj(("cleared", 1d)));
                var filled = fx.Load();
                Assert.AreEqual(1, filled.Career.Cleared);
                Assert.IsTrue(filled.Career.HasUnlock("core-stage"));
                Assert.IsTrue(filled.Career.HasUnlock("fins"));
                Assert.IsTrue(filled.Career.HasUnlock("nose"));
                Assert.IsFalse(filled.Career.HasUnlock("stage-2"));
                Assert.AreEqual(Career.KcToMinor(100, MinorPerKc), filled.Career.BankMinor);
            }

            using (var fx = new Fixture())
            {
                fx.Store.PutProgress(Obj(("cleared", 1d), ("unlocks", Arr("fins"))));
                var listed = fx.Load();
                Assert.AreEqual(1, listed.Career.Cleared);
                Assert.IsTrue(listed.Career.HasUnlock("fins"));
                Assert.IsFalse(listed.Career.HasUnlock("nose"));
                Assert.IsFalse(listed.Career.HasUnlock("core-stage"));
            }
        }

        [Test]
        public void CorruptCareer_FallsBack_KeepsBak_AndRecoversAGoodTemp()
        {
            using (var fx = new Fixture())
            {
                fx.Store.PutDesign(Obj(("name", "Keep")), "one", "Keep");
                fx.Store.PutProgress(Obj(("orbitsCompleted", 3d), ("engineerEnabled", true), ("bankMinor", 999d)));
                string progressBefore = File.ReadAllText(fx.File(NativeStore.ProgressKey));
                string designBefore = File.ReadAllText(fx.File(NativeStore.DesignPrefix + "one"));
                var seeded = fx.Load();
                Assert.AreEqual(999L, seeded.Career.BankMinor);
                string good = File.ReadAllText(fx.File(CareerStore.CareerKey));
                File.WriteAllText(fx.File(CareerStore.CareerKey) + ".tmp", good);
                File.WriteAllText(fx.File(CareerStore.CareerKey), "{not json");

                var recovered = fx.Load();
                Assert.AreEqual(999L, recovered.Career.BankMinor);
                Assert.AreEqual("{not json", File.ReadAllText(fx.File(CareerStore.CareerKey) + ".bak"));
                Assert.AreEqual(progressBefore, File.ReadAllText(fx.File(NativeStore.ProgressKey)));
                Assert.AreEqual(designBefore, File.ReadAllText(fx.File(NativeStore.DesignPrefix + "one")));

                File.WriteAllText(fx.File(CareerStore.CareerKey), "{still bad");
                var fresh = fx.Load();
                Assert.AreEqual("{not json", File.ReadAllText(fx.File(CareerStore.CareerKey) + ".bak"));
                AssertSameCareer(fx.Campaign, fresh.Career);
                Assert.AreEqual(progressBefore, File.ReadAllText(fx.File(NativeStore.ProgressKey)));
                fresh.Save();
                Assert.AreEqual("{not json", File.ReadAllText(fx.File(CareerStore.CareerKey) + ".bak"));
                Assert.AreEqual("career", MiniJson.ParseObject(File.ReadAllText(fx.File(CareerStore.CareerKey))).Str("kind"));
            }
        }

        [Test]
        public void WrongSchemaOrKind_FallsBackAndKeepsBak()
        {
            using (var fx = new Fixture())
            {
                var env = Obj(
                    ("schema", 2d),
                    ("kind", "career"),
                    ("id", "career"),
                    ("name", "career"),
                    ("createdAt", 1d),
                    ("updatedAt", 1d),
                    ("data", Obj(("bankMinor", 1d), ("cleared", 9d))));
                File.WriteAllText(fx.File(CareerStore.CareerKey), JsonWriter.Stringify(env));
                var store = fx.Load();
                AssertSameCareer(fx.Campaign, store.Career);
                Assert.IsFalse(store.MigratedFromProgress);
                StringAssert.Contains("\"schema\":2", File.ReadAllText(fx.File(CareerStore.CareerKey) + ".bak"));
            }

            using (var fx = new Fixture())
            {
                var env = Obj(
                    ("schema", 1d),
                    ("kind", "progress"),
                    ("id", "career"),
                    ("name", "career"),
                    ("createdAt", 1d),
                    ("updatedAt", 1d),
                    ("data", Obj(("bankMinor", 1d))));
                File.WriteAllText(fx.File(CareerStore.MissionsKey), "{nope");
                File.WriteAllText(fx.File(CareerStore.CareerKey), JsonWriter.Stringify(env));
                var store = fx.Load();
                AssertSameCareer(fx.Campaign, store.Career);
                Assert.AreEqual(0, store.Missions.Count);
                Assert.IsTrue(File.Exists(fx.File(CareerStore.CareerKey) + ".bak"));
                Assert.IsTrue(File.Exists(fx.File(CareerStore.MissionsKey) + ".bak"));
            }
        }

        [Test]
        public void SameClock_WritesTheSameBytes()
        {
            string once = RunSequence();
            string twice = RunSequence();
            Assert.AreEqual(once, twice);
            StringAssert.Contains("\"m1\":", once);
            int m1 = once.IndexOf("\"m1\":", StringComparison.Ordinal);
            int m10 = once.IndexOf("\"m10\":", StringComparison.Ordinal);
            int m2 = once.IndexOf("\"m2\":", StringComparison.Ordinal);
            Assert.Less(m1, m10);
            Assert.Less(m10, m2);
        }

        [Test]
        public void HangarLoan_TopsUpOnce_AndDoesNotTouchAHealthyBank()
        {
            using (var fx = new Fixture())
            {
                var store = fx.Load();
                store.Career.BankMinor = 11_000;
                Assert.IsFalse(store.TryHangarLoan(50_000));
                Assert.AreEqual(11_000L, store.Career.BankMinor);
                Assert.IsFalse(store.LoanUsed);
                store.Career.BankMinor = 10_000;

                Assert.IsFalse(store.TryHangarLoan(8_000));
                Assert.IsFalse(store.LoanUsed);
                Assert.IsFalse(File.Exists(fx.File(CareerStore.CareerKey)));

                var miss = store.Career.Settle(1, false, 5_000, 0, null);
                Assert.IsTrue(miss.accepted);
                Assert.AreEqual(5_000L, store.Career.BankMinor);
                Assert.IsTrue(store.TryHangarLoan(8_000));
                Assert.AreEqual(10_000L, store.Career.BankMinor);
                Assert.IsTrue(store.LoanUsed);
                Assert.IsTrue(MiniJson.ParseObject(File.ReadAllText(fx.File(CareerStore.CareerKey))).Obj("data").Bool("loanUsed"));

                store.Career.Settle(1, false, 5_000, 0, null);
                Assert.AreEqual(5_000L, store.Career.BankMinor);
                Assert.IsFalse(store.TryHangarLoan(8_000));
                Assert.AreEqual(5_000L, store.Career.BankMinor);
                store.Save();

                var again = fx.Load();
                Assert.IsTrue(again.LoanUsed);
                Assert.AreEqual(5_000L, again.Career.BankMinor);
                Assert.IsFalse(again.TryHangarLoan(1));
            }
        }

        static string RunSequence()
        {
            using (var fx = new Fixture())
            {
                var store = fx.Load();
                store.Career.Settle(1, true, 100, 0, "core");
                store.RecordAttempt("m2", 1, 10, new[] { "bronze" }, 2, "b");
                store.RecordAttempt("m10", 2, 20, new[] { "silver" }, 10, "c");
                store.RecordAttempt("m1", 3, 30, new[] { "gold" }, 1, "a");
                return File.ReadAllText(fx.File(CareerStore.CareerKey)) + "\n" + File.ReadAllText(fx.File(CareerStore.MissionsKey));
            }
        }

        static void AssertSameCareer(Campaign campaign, Career career)
        {
            var fresh = Career.Start(campaign, MinorPerKc);
            Assert.AreEqual(fresh.BankMinor, career.BankMinor);
            Assert.AreEqual(fresh.Cleared, career.Cleared);
            CollectionAssert.AreEquivalent(fresh.Unlocks, career.Unlocks);
            Assert.AreEqual(0, career.ArticleFlights.Count);
        }

        static JsonObject MissionRow(int stars, double score, List<object> medals, int attempts, long seed, string design)
            => Obj(
                ("bestStars", (double)stars),
                ("bestScore", score),
                ("bestMedals", medals),
                ("attempts", (double)attempts),
                ("lastSeed", (double)seed),
                ("lastDesignId", design));

        static List<string> Strings(List<object> list)
        {
            var o = new List<string>();
            foreach (var x in list) o.Add((string)x);
            return o;
        }

        static Dictionary<string, string> Snapshot(string dir)
        {
            var o = new Dictionary<string, string>();
            foreach (var file in Directory.GetFiles(dir))
                o[Path.GetFileName(file)] = File.ReadAllText(file);
            return o;
        }

        static JsonObject Obj(params (string k, object v)[] pairs)
        {
            var o = new JsonObject();
            foreach (var (k, v) in pairs) o.Add(k, v);
            return o;
        }

        static List<object> Arr(params object[] xs) => new List<object>(xs);

        sealed class Fixture : IDisposable
        {
            public readonly string Dir;
            public long Now = T0;
            public readonly NativeStore Store;
            public readonly Campaign Campaign;

            public Fixture()
            {
                Dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kh-career-" + Guid.NewGuid().ToString("N"));
                Campaign = SampleCampaign();
                Store = new NativeStore(Dir, () => Now, () => 0.25);
            }

            public CareerStore Load() => CareerStore.Load(Store, Campaign, MinorPerKc);

            public string File(string key) => System.IO.Path.Combine(Dir, key + ".json");

            public void Dispose()
            {
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            }

            static Campaign SampleCampaign()
            {
                return Campaign.FromJson(Obj(
                    ("version", 1d),
                    ("startingBankKc", 100d),
                    ("starterUnlocks", Arr("core-stage", "engines", "tanks", "fairing")),
                    ("seeds", Arr(1d, 2d, 3d, 4d, 5d)),
                    ("missions", new List<object>
                    {
                        Mission(1, "m1", "First Hop", 10d, "fins", "nose"),
                        Mission(2, "m2", "Edge of Space", 20d, "stage-2"),
                    })));
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
                    ("diameterM", 0.4),
                    ("budgetKc", reward),
                    ("rewardKc", reward),
                    ("gentleDefault", true),
                    ("target", Obj(("kind", "apogee"), ("apoKm", 10d), ("periKm", 0d))),
                    ("unlocks", list));
            }
        }
    }
}
