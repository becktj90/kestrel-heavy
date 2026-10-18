// Mission Select: four acts, career locks, earned stars and medals, payload and target, contracts after KH-20.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Core.Save;
using Kestrel.Game.MissionSelect;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
    public class MissionSelectTests
    {
        static string Repo => Catalog.FindRepoRoot(Directory.GetCurrentDirectory());
        static Campaign Load() => Campaign.Load(Repo);
        static string Json() => File.ReadAllText(Path.Combine(Repo, "data", "campaign.json"));
        static Dictionary<string, PayloadLabel> Labels() => MissionMapText.ReadPayloadLabels(Json());
        static Career FreshCareer(Campaign campaign) => Career.Start(campaign, MissionMapText.MinorPerKc(Json()));

        static VisualElement Screen(Campaign campaign, Career career, IReadOnlyDictionary<string, MissionRecord> records)
            => MissionSelectScreen.Build(campaign, career, records, Labels(), _ => { }, () => { });

        [Test]
        public void MapReadsTheCampaignFile_NotThePayloadCatalog()
        {
            Assert.AreEqual(100000, MissionMapText.MinorPerKc(Json()));
            Assert.AreEqual(MissionMapCopy.MinorPerKcFallback, MissionMapText.MinorPerKc(""));
            Assert.AreEqual(MissionMapCopy.MinorPerKcFallback, MissionMapText.MinorPerKc("{"));
            var labels = Labels();
            Assert.AreEqual("Pebble", labels["m1"].Name);
            Assert.AreEqual("Sounding box", labels["m1"].Kind);
            Assert.AreEqual(0, MissionMapText.ReadPayloadLabels("not json").Count);

            var campaign = Load();
            int first = campaign.Missions[0].Index;
            var map = MissionMapBuilder.From(campaign, FreshCareer(campaign), null, labels);
            Assert.AreEqual(first, campaign.Missions[0].Index);
            Assert.AreEqual(4, map.Acts.Count);
            CollectionAssert.AreEqual(new[] { "act-1", "act-2", "act-3", "act-4" }, Ids(map.Acts, a => a.Id));
            CollectionAssert.AreEqual(new[]
            {
                "Act I · First Light",
                "Act II · Regulars",
                "Act III · Heavy",
                "Act IV · Far Shore",
            }, Ids(map.Acts, a => a.Title));
            CollectionAssert.AreEqual(new[] { "m1", "m2", "m3", "m4", "m5" }, MissionIds(map, "act-1"));
            CollectionAssert.AreEqual(new[] { "m6", "m7", "m8", "m9", "m10" }, MissionIds(map, "act-2"));
            CollectionAssert.AreEqual(new[] { "m11", "m12", "m13", "m14", "m15" }, MissionIds(map, "act-3"));
            CollectionAssert.AreEqual(new[] { "m16", "m17", "m18", "m19", "m20" }, MissionIds(map, "act-4"));

            var expectOrbit = new Dictionary<string, string>
            {
                { "m1", "Apogee 10 km" },
                { "m2", "Apogee 100 km" },
                { "m3", "Circular orbit 200 km, 28.5°" },
                { "m4", "Circular orbit 500 km, 51.6°" },
                { "m5", "Circular orbit 400 km, 51.6°" },
                { "m6", "Circular orbit 200 km, 51.6°" },
                { "m7", "Ellipse 200 km by 35,786 km, 27°" },
                { "m8", "Circular orbit 400 km, 51.6°" },
                { "m9", "C3 -2 km2/s2, closest point 200 km, 28.5°" },
                { "m10", "Ellipse 200 km by 35,786 km, 27°" },
                { "m11", "Circular orbit 550 km, 97.6°" },
                { "m12", "Circular orbit 600 km, 51.6°" },
                { "m13", "Circular orbit 550 km, 97.6°" },
                { "m14", "Circular orbit 400 km, 51.6°" },
                { "m15", "Ellipse 200 km by 35,786 km, 27°" },
                { "m16", "Circular orbit 500 km, 51.6°" },
                { "m17", "C3 -2 km2/s2, closest point 200 km, 28.5°" },
                { "m18", "C3 -0.4 km2/s2, closest point 200 km, 28.5°" },
                { "m19", "Ellipse 200 km by 35,786 km, 27°" },
                { "m20", "C3 -0.25 km2/s2, closest point 200 km, 28.5°" },
            };
            var expectPayload = new Dictionary<string, string>
            {
                { "m1", "Pebble · Sounding box · 50 kg" },
                { "m2", "Lantern · Sounding box · 100 kg" },
                { "m3", "Mote · Cubesat · 20 kg" },
                { "m4", "Tern-3 · Weather sat · 2,500 kg" },
                { "m5", "Bargeload · Cargo sat · 12,000 kg" },
                { "m6", "Homebound · Cargo sat · 8,000 kg" },
                { "m7", "Harbor-1 · Comsat · 5,500 kg" },
                { "m8", "Nest · Crew capsule · 12,000 kg" },
                { "m9", "Lumen · Lunar probe · 3,000 kg" },
                { "m10", "Twin Harbor · Comsat · 7,000 kg" },
                { "m11", "Plover-1 · Weather sat · 2,200 kg" },
                { "m12", "Meridian · Space telescope · 1,800 kg" },
                { "m13", "Cinder Rack · Cubesat dispenser · 480 kg" },
                { "m14", "Trunk-4 · Cargo capsule · 10,000 kg" },
                { "m15", "Westline-2 · GEO relay · 6,200 kg" },
                { "m16", "Nest-2 · Crew capsule · 12,000 kg" },
                { "m17", "Lumen Down · Lunar lander · 4,000 kg" },
                { "m18", "Nightjar · Probe · 2,500 kg" },
                { "m19", "Keel-9 · Comsat · 7,500 kg" },
                { "m20", "Far Hold · Probe · 3,200 kg" },
            };
            for (int i = 0; i < map.Acts.Count; i++)
            {
                var act = map.Acts[i];
                for (int e = 0; e < act.Entries.Count; e++)
                {
                    var row = act.Entries[e];
                    Assert.AreEqual(expectOrbit[row.MissionId], row.OrbitLine, row.MissionId);
                    Assert.AreEqual(expectPayload[row.MissionId], row.PayloadLine, row.MissionId);
                    StringAssert.DoesNotContain("escape", row.OrbitLine);
                    StringAssert.DoesNotContain("KH-K", row.PayloadLine);
                    StringAssert.DoesNotContain("300 kg", row.PayloadLine);
                }
            }
        }

        [Test]
        public void FreshBoard_OpensOnlyKh1_AndHidesContracts()
        {
            var campaign = Load();
            var root = Screen(campaign, FreshCareer(campaign), null);
            Assert.AreEqual("mission-map", root.name);
            Assert.IsNull(root.Q("mission-list"));
            Assert.AreEqual("Next is KH-1 First Hop.", root.Q<Label>("missions-note").text);
            Assert.AreEqual("Act I · First Light", root.Q<Label>("act-1-title").text);
            Assert.AreEqual("Act IV · Far Shore", root.Q<Label>("act-4-title").text);
            Assert.IsNull(root.Q("open-contracts"));
            Assert.IsNull(root.Q<Button>("open-contracts"));

            var first = root.Q<Button>("mission-m1");
            Assert.IsTrue(first.enabledSelf);
            Assert.IsTrue(first.ClassListContains("mission-row--next"));
            Assert.AreEqual("KH-1  First Hop", root.Q<Label>("mission-m1-name").text);
            Assert.AreEqual("Next", root.Q<Label>("mission-m1-next").text);
            Assert.AreEqual("Pebble · Sounding box · 50 kg", root.Q<Label>("mission-m1-payload").text);
            Assert.AreEqual("Apogee 10 km", root.Q<Label>("mission-m1-orbit").text);
            Assert.AreEqual("A pass opens fins and nose.", root.Q<Label>("mission-m1-parts").text);
            Assert.IsNull(root.Q("mission-m1-stars"));
            Assert.IsNull(root.Q("mission-m1-medals"));
            Assert.IsNull(root.Q("mission-m1-lock"));
            StringAssert.Contains("Gentle on.", first.tooltip);
            StringAssert.Contains("Fly this one.", first.tooltip);
            Assert.AreEqual(44f, first.style.minHeight.value.value);

            for (int i = 2; i <= 20; i++)
            {
                var row = root.Q<Button>("mission-m" + i);
                Assert.IsFalse(row.enabledSelf, "m" + i);
                Assert.IsTrue(row.ClassListContains("mission-row--locked"), "m" + i);
                Assert.AreEqual("Opens after KH-" + (i - 1) + ".", root.Q<Label>("mission-m" + i + "-lock").text);
                Assert.IsNotNull(root.Q<Label>("mission-m" + i + "-payload"), "m" + i);
                Assert.IsNotNull(root.Q<Label>("mission-m" + i + "-orbit"), "m" + i);
                Assert.IsNull(root.Q("mission-m" + i + "-next"), "m" + i);
                Assert.IsNull(root.Q("mission-m" + i + "-parts"), "m" + i);
                Assert.AreEqual(44f, row.style.minHeight.value.value);
                StringAssert.Contains("Opens after KH-" + (i - 1), row.tooltip);
            }

            var home = root.Q<Button>("missions-home");
            Assert.AreEqual("Home", home.text);
            Assert.AreEqual("Back to the title screen.", home.tooltip);
            Assert.AreEqual(44f, home.style.minHeight.value.value);
            AssertPlain(Visible(root));
        }

        [Test]
        public void StarsAndMedalsAreEarnedOnly_AndDoNotGateTheNextFlight()
        {
            var campaign = Load();
            var career = FreshCareer(campaign);
            career.Cleared = 1;
            career.Unlocks.Add("fins");
            career.Unlocks.Add("nose");
            var records = new Dictionary<string, MissionRecord>
            {
                ["m1"] = Record(1, "bronze", "gold"),
                ["m2"] = Record(0),
            };
            records["m2"].Attempts = 3;
            var root = Screen(campaign, career, records);

            Assert.IsTrue(root.Q<Button>("mission-m2").enabledSelf);
            Assert.IsNotNull(root.Q("mission-m2-next"));
            Assert.IsNull(root.Q("mission-m2-stars"));
            Assert.IsNull(root.Q("mission-m2-medals"));
            Assert.AreEqual("1 star", root.Q<Label>("mission-m1-stars").text);
            Assert.AreEqual("Bronze · Gold", root.Q<Label>("mission-m1-medals").text);
            Assert.AreEqual("Opened fins and nose.", root.Q<Label>("mission-m1-parts").text);
            Assert.AreEqual("A pass opens a second stage.", root.Q<Label>("mission-m2-parts").text);
            StringAssert.Contains("Replay.", root.Q<Button>("mission-m1").tooltip);
            Assert.IsFalse(root.Q<Button>("mission-m3").enabledSelf);
            Assert.IsNull(root.Q("open-contracts"));

            var odd = new Dictionary<string, MissionRecord> { ["m1"] = Record(4, "timing") };
            var oddRoot = Screen(campaign, career, odd);
            Assert.IsNull(oddRoot.Q("mission-m1-stars"));
            Assert.AreEqual("timing", oddRoot.Q<Label>("mission-m1-medals").text);
        }

        [Test]
        public void PartUnlocksDoNotSkipTheMissionOrder()
        {
            var campaign = Load();
            var career = FreshCareer(campaign);
            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var unlocks = campaign.Missions[i].Unlocks;
                for (int u = 0; u < unlocks.Count; u++) career.Unlocks.Add(unlocks[u]);
            }
            Assert.AreEqual(0, career.Cleared);
            var root = Screen(campaign, career, null);
            Assert.IsTrue(root.Q<Button>("mission-m1").enabledSelf);
            for (int i = 2; i <= campaign.Missions.Count; i++) Assert.IsFalse(root.Q<Button>("mission-m" + i).enabledSelf, "m" + i);
            Assert.IsNull(root.Q("mission-m1-parts"));
            Assert.IsNull(root.Q("open-contracts"));
        }

        [Test]
        public void OpenContractsStaysHiddenUntilKh20Clears()
        {
            var campaign = Load();
            var career = FreshCareer(campaign);
            career.Cleared = 9;
            var almost = Screen(campaign, career, null);
            Assert.IsNull(almost.Q("open-contracts"));
            Assert.IsTrue(almost.Q<Button>("mission-m10").enabledSelf);
            Assert.IsNotNull(almost.Q("mission-m10-next"));
            Assert.IsTrue(almost.Q<Button>("mission-m1").enabledSelf);
            Assert.IsNull(almost.Q("mission-m1-next"));

            career.Cleared = 10;
            var mid = Screen(campaign, career, null);
            Assert.IsNull(mid.Q("open-contracts"));
            Assert.IsTrue(mid.Q<Button>("mission-m11").enabledSelf);
            Assert.IsNotNull(mid.Q("mission-m11-next"));
            Assert.IsNull(mid.Q("mission-m10-next"));

            career.Cleared = 19;
            var last = Screen(campaign, career, null);
            Assert.IsNull(last.Q("open-contracts"));
            Assert.IsNotNull(last.Q("mission-m20-next"));

            career.Cleared = 20;
            var done = Screen(campaign, career, null);
            Assert.IsNotNull(done.Q("open-contracts"));
            Assert.IsNull(done.Q<Button>("open-contracts"));
            Assert.AreEqual("Open Contracts", done.Q<Label>("open-contracts-title").text);
            Assert.AreEqual(MissionMapCopy.ContractsBody, done.Q<Label>("open-contracts-body").text);
            Assert.AreEqual(MissionMapCopy.ContractsBody, done.Q("open-contracts").tooltip);
            Assert.AreEqual(44f, done.Q("open-contracts").style.minHeight.value.value);
            Assert.AreEqual("The board is clear.", done.Q<Label>("missions-note").text);
            for (int i = 1; i <= 20; i++)
            {
                Assert.IsTrue(done.Q<Button>("mission-m" + i).enabledSelf, "m" + i);
                Assert.IsNull(done.Q("mission-m" + i + "-next"), "m" + i);
                Assert.IsNull(done.Q("mission-m" + i + "-lock"), "m" + i);
            }
            Assert.IsNull(done.Q("mission-m20-parts"));
            AssertPlain(Visible(done));
        }

        [Test]
        public void EmptyCampaignStillOffersHome()
        {
            var root = MissionSelectScreen.Build(null, null, null, null, _ => { }, () => { });
            Assert.IsNotNull(root.Q<Label>("missions-empty"));
            Assert.AreEqual("The campaign file has no missions yet.", root.Q<Label>("missions-empty").text);
            Assert.IsNull(root.Q("missions-note"));
            Assert.IsNull(root.Q("act-1"));
            Assert.IsNull(root.Q("mission-m1"));
            Assert.IsNull(root.Q("open-contracts"));
            Assert.IsNotNull(root.Q<Button>("missions-home"));
            Assert.AreEqual(44f, root.Q<Button>("missions-home").style.minHeight.value.value);
        }

        [Test]
        public void WithoutBriefingLabels_TheRowUsesTheMissionPayloadId()
        {
            var campaign = Load();
            var root = MissionSelectScreen.Build(campaign, FreshCareer(campaign), null, null, _ => { }, () => { });
            Assert.AreEqual("Science box · 50 kg", root.Q<Label>("mission-m1-payload").text);
            StringAssert.DoesNotContain("KH-K", root.Q<Label>("mission-m1-payload").text);
            StringAssert.DoesNotContain("300 kg", root.Q<Label>("mission-m1-payload").text);
        }

        [Test]
        public void AMissionPastTheTwenty_StaysOnTheBoard_AndDoesNotOpenContracts()
        {
            var campaign = Load();
            campaign.Missions.Add(new CampaignMission
            {
                Index = 21,
                Id = "m21",
                Name = "Side Path",
                PayloadId = "kid-box",
                MassKg = 10,
                TargetKind = "apogee",
                ApoKm = 5,
            });
            var career = FreshCareer(campaign);
            career.Cleared = 9;
            var map = MissionMapBuilder.From(campaign, career, null, null);
            Assert.IsFalse(map.ShowOpenContracts);
            CollectionAssert.Contains(MissionIds(map, "act-4"), "m21");
            var side = Find(map, "m21");
            Assert.AreEqual(MissionAccess.Locked, side.Access);
            Assert.AreEqual("Apogee 5 km", side.OrbitLine);
            Assert.AreEqual("Science box · 10 kg", side.PayloadLine);
        }

        [Test]
        public void ReadSave_DoesNotCreateOrWrite_AndReadsTheMissionsRecord()
        {
            var campaign = Load();
            string missing = Path.Combine(Path.GetTempPath(), "kh-map-" + Guid.NewGuid().ToString("N"));
            var fresh = MissionProgress.ReadSave(campaign, missing, 100000);
            Assert.IsFalse(Directory.Exists(missing));
            Assert.AreEqual(0, fresh.Career.Cleared);
            Assert.AreEqual(0, fresh.Records.Count);
            Assert.IsTrue(fresh.Career.HasUnlock("core-stage"));
            Assert.IsFalse(fresh.Career.HasUnlock("fins"));

            string empty = Path.Combine(Path.GetTempPath(), "kh-map-" + Guid.NewGuid().ToString("N"));
            string filled = Path.Combine(Path.GetTempPath(), "kh-map-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(empty);
            Directory.CreateDirectory(filled);
            try
            {
                var untouched = MissionProgress.ReadSave(campaign, empty, 100000);
                Assert.AreEqual(0, untouched.Career.Cleared);
                Assert.IsFalse(File.Exists(Path.Combine(empty, CareerStore.CareerKey + ".json")));
                Assert.IsFalse(File.Exists(Path.Combine(empty, CareerStore.MissionsKey + ".json")));

                var store = new NativeStore(filled, () => 1_700_000_000_000L, () => 0.25);
                var saved = CareerStore.Load(store, campaign, 100000);
                saved.Career.Cleared = 4;
                saved.Career.Unlocks.Add("fins");
                saved.RecordAttempt("m1", 2, 1500, new[] { "bronze", "gold" }, 3, "pencil");

                var snap = MissionProgress.ReadSave(campaign, filled, 100000);
                Assert.AreEqual(4, snap.Career.Cleared);
                Assert.IsTrue(snap.Career.HasUnlock("fins"));
                Assert.AreEqual(2, snap.Records["m1"].BestStars);
                CollectionAssert.AreEqual(new[] { "bronze", "gold" }, snap.Records["m1"].BestMedals);
                Assert.AreEqual(1, snap.Records["m1"].Attempts);
            }
            finally
            {
                if (Directory.Exists(empty)) Directory.Delete(empty, true);
                if (Directory.Exists(filled)) Directory.Delete(filled, true);
            }
        }

        static MissionRecord Record(int stars, params string[] medals)
        {
            var rec = new MissionRecord { BestStars = stars, Attempts = 1 };
            if (medals != null)
                for (int i = 0; i < medals.Length; i++) rec.BestMedals.Add(medals[i]);
            return rec;
        }

        static string[] MissionIds(MissionMap map, string actId)
        {
            MissionAct act = null;
            for (int i = 0; i < map.Acts.Count; i++) if (map.Acts[i].Id == actId) act = map.Acts[i];
            Assert.IsNotNull(act, actId);
            var ids = new string[act.Entries.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = act.Entries[i].MissionId;
            return ids;
        }

        static MissionMapEntry Find(MissionMap map, string id)
        {
            for (int a = 0; a < map.Acts.Count; a++)
                for (int i = 0; i < map.Acts[a].Entries.Count; i++)
                    if (map.Acts[a].Entries[i].MissionId == id) return map.Acts[a].Entries[i];
            Assert.Fail(id);
            return null;
        }

        static string[] Ids<T>(List<T> items, Func<T, string> pick)
        {
            var ids = new string[items.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = pick(items[i]);
            return ids;
        }

        static string Visible(VisualElement node)
        {
            var sb = new StringBuilder();
            void Walk(VisualElement e)
            {
                if (e == null) return;
                if (e is TextElement text && !string.IsNullOrEmpty(text.text)) sb.AppendLine(text.text);
                if (!string.IsNullOrEmpty(e.tooltip)) sb.AppendLine(e.tooltip);
                for (int i = 0; i < e.childCount; i++) Walk(e[i]);
            }
            Walk(node);
            return sb.ToString();
        }

        static void AssertPlain(string text)
        {
            string lower = text.ToLowerInvariant();
            foreach (var bad in new[]
            {
                "you missed", "you failed", "you lost", "limited time", "watch an ad", "streak",
                "daily", " buy", "spacex", "nasa", "falcon", "starlink", "soyuz", "ariane", "boeing",
            })
                StringAssert.DoesNotContain(bad, lower);
        }
    }
}
