// DECISIONS #86 (Trevor via Claude, 11-07): the phone flies the same briefing wind and launch site as the predictor
// (#74/#75). #458 ships data/campaign.json, sites.json and weather.json in Resources/KestrelData and MissionBriefingBoot
// installs them (BriefingBooks). These tests run the device path for real: KESTREL_REPO_ROOT points at a folder with no
// data/, so Catalog.RepoRootOrNull() is null as on a phone, and the books hold the bytes CI copies. They prove that path
// draws the editor's card for every mission, that the designer predictor (ReferenceAscent, which the Starter's Honest
// Ready flies) and the live pad fly the identical flight, and that every file the books need is on the ship lists.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    /// <summary>Runs code as a player build sees briefing data: no checkout, the Resources/KestrelData books installed.</summary>
    internal static class DevicePad
    {
        /// <summary>The data/*.json names the books are parsed from (each ships as Resources/KestrelData/&lt;name&gt;).</summary>
        public static readonly string[] Files = { "campaign", "sites", "weather" };

        static string Text(string name) => File.ReadAllText(Path.Combine(Golden.Root, "data", name + ".json"));

        /// <summary>Run <paramref name="body"/> with no checkout and, when <paramref name="installBooks"/>, the shipped books
        /// installed as MissionBriefingBoot installs them. The checkout, the books and ReferenceAscent's root are restored.</summary>
        public static void Run(Action body, bool installBooks = true)
        {
            string campaign = Text("campaign"), sites = Text("sites"), weather = Text("weather");
            string prev = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            string away = Path.Combine(Path.GetTempPath(), "kh-device-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(away);
            bool had = BriefingBooks.HasBooks;
            Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", away);
            ReferenceAscent.ForgetBriefingRoot();
            try
            {
                Assert.IsNull(Catalog.RepoRootOrNull(), "no checkout, as on a device");
                if (installBooks) BriefingBooks.UseJson(campaign, sites, weather);
                else BriefingBooks.Clear();
                body();
            }
            finally
            {
                Environment.SetEnvironmentVariable("KESTREL_REPO_ROOT", prev);
                ReferenceAscent.ForgetBriefingRoot();
                if (!had) BriefingBooks.Clear();
                try { Directory.Delete(away, true); } catch (IOException) { }
            }
        }

        /// <summary>The card AscentFlight.Prepare draws on the device: ForPad on Catalog.RepoRootOrNull() (null) and the books.</summary>
        public static MissionBriefing Card(CampaignMission mission, Design design)
        {
            MissionBriefing card = null;
            Run(() => card = MissionBriefing.ForPad(Catalog.RepoRootOrNull(), mission.Index, FlightBatch.DesignHash(design), 1));
            return card;
        }
    }

    public class DeviceBriefingTests
    {
        /// <summary>Every briefing book ships: on SliceBootstrap.DataFiles (Kestrel > Sync Data) and in the CI copy of
        /// data/*.json into Resources/KestrelData that the iOS builds run, under the Resources paths the boot loads.</summary>
        [Test]
        public void EveryBriefingBook_ShipsInResourcesKestrelData()
        {
            CollectionAssert.AreEqual(new[] { "KestrelData/campaign", "KestrelData/sites", "KestrelData/weather" },
                new[] { BriefingBooks.CampaignResource, BriefingBooks.SitesResource, BriefingBooks.WeatherResource });
            string sync = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Slice", "Editor", "SliceBootstrap.cs"));
            int at = sync.IndexOf("static readonly string[] DataFiles", StringComparison.Ordinal);
            Assert.Greater(at, 0, "SliceBootstrap.DataFiles");
            string list = sync.Substring(at, sync.IndexOf('}', at) - at);
            StringAssert.Contains("\"Assets/Resources/KestrelData\"", sync);
            foreach (var f in DevicePad.Files)
            {
                Assert.IsTrue(File.Exists(Path.Combine(Golden.Root, "data", f + ".json")), "data/" + f + ".json");
                StringAssert.Contains("\"" + f + "\"", list, "SliceBootstrap.DataFiles ships " + f);
            }
            foreach (var w in new[] { "unity-proto-ios.yml", "xcode-export.yml" })
            {
                string yml = File.ReadAllText(Path.Combine(Golden.Root, ".github", "workflows", w));
                StringAssert.Contains("cp data/*.json", yml, w + " copies data/*.json");
                StringAssert.Contains("unity/Assets/Resources/KestrelData", yml, w);
            }
        }

        /// <summary>The pre-#86 device: no checkout and no books. No card, so the pad would fly calm air on the reference pad
        /// and the predictor's cache key says calm. MissionBriefingBoot's install is what closes this.</summary>
        [Test]
        public void NoCheckout_AndNoBooks_IsThePreFixCalmPad()
        {
            var json = StarterFixture.Rocket("m3").BuildDesign();
            var d = Design.FromJson(json);
            DevicePad.Run(() =>
            {
                Assert.IsFalse(BriefingBooks.HasBooks);
                Assert.IsNull(ReferenceAscent.Briefing(d));
                StringAssert.Contains("\nbrief=calm\n", ReferenceAscent.CanonicalKey(StarterFixture.Catalog, json));
            }, installBooks: false);
        }

        /// <summary>The device's card equals the editor's for every campaign mission and the free build, at several seeds and
        /// design hashes: the same pad, latitude, wind knots, clock, flight code, bonuses, lines and failure mix.</summary>
        [Test]
        public void DevicePath_EveryMission_DrawsTheEditorsBriefing()
        {
            int n = 0;
            var editor = new List<MissionBriefing>();
            var keys = new List<(int index, ulong hash, ulong seed)>();
            for (int index = 0; index <= 20; index++)
            {
                ulong starter = index > 0 ? FlightBatch.DesignHash(Design.FromJson(StarterFixture.Rocket("m" + index).BuildDesign())) : 7;
                foreach (ulong hash in new[] { 0UL, starter })
                    foreach (ulong seed in new ulong[] { 1, 2, 7, 0xBEEF })
                    {
                        keys.Add((index, hash, seed));
                        editor.Add(MissionBriefing.ForPad(Golden.Root, index, hash, seed));
                    }
            }
            DevicePad.Run(() =>
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    var k = keys[i];
                    var device = MissionBriefing.ForPad(Catalog.RepoRootOrNull(), k.index, k.hash, k.seed);
                    AssertSame(editor[i], device, "index " + k.index + " hash " + k.hash.ToString("x") + " seed " + k.seed);
                    n++;
                }
            });
            Assert.AreEqual(21 * 2 * 4, n);
        }

        /// <summary>The designer readout and the Starter's Honest Ready (ReferenceAscent.Briefing / Flight, which
        /// StarterPredictionJob flies) see the seed-1 briefing wind on the device: the same card, cache key and flight.</summary>
        [Test]
        public void DevicePath_Predictor_FliesTheEditorsWindAndPad()
        {
            var catalog = StarterFixture.Catalog;
            var rows = new List<(string id, string json, MissionBriefing card, string key, SimResult sim)>();
            foreach (var mission in StarterFixture.Campaign.Missions)
            {
                var json = StarterFixture.Rocket(mission.Id).BuildDesign();
                var d = Design.FromJson(json);
                var card = ReferenceAscent.Briefing(d);
                Assert.IsNotNull(card, mission.Id);
                Assert.IsFalse(card.Wind.Calm, mission.Id + ": seed 1 is a windy briefing");
                rows.Add((mission.Id, Kestrel.Core.Save.JsonWriter.Stringify(json), card, ReferenceAscent.CanonicalKey(catalog, json), ReferenceAscent.Flight(catalog, d).Run()));
            }
            DevicePad.Run(() =>
            {
                foreach (var row in rows)
                {
                    var json = Kestrel.Core.Json.MiniJson.ParseObject(row.json);
                    var d = Design.FromJson(json);
                    AssertSame(row.card, ReferenceAscent.Briefing(d), row.id);
                    string key = ReferenceAscent.CanonicalKey(catalog, json);
                    Assert.AreEqual(row.key, key, row.id);
                    StringAssert.Contains("\nbrief=seed1\n", key, row.id);
                    var sim = ReferenceAscent.Flight(catalog, d).Run();
                    Assert.AreEqual(row.sim.outcome, sim.outcome, row.id);
                    Assert.AreEqual(row.sim.orbit.apoAlt, sim.orbit.apoAlt, 0, row.id);
                    Assert.AreEqual(row.sim.orbit.periAlt, sim.orbit.periAlt, 0, row.id);
                    Assert.AreEqual(row.sim.maxQ, sim.maxQ, 0, row.id);
                    Assert.AreEqual(row.sim.failure?.kind, sim.failure?.kind, row.id);
                }
            });
        }

        /// <summary>The live pad on the device's card (AscentFlight.Prepare's ForPad with no checkout) is the identical live
        /// flight the checkout's seed-1 card flies.</summary>
        [Test]
        public void DevicePath_LivePad_FliesTheEditorsFlight()
        {
            var catalog = StarterFixture.Catalog;
            foreach (var id in new[] { "m3", "m5", "m13" })
            {
                var mission = StarterFixture.Campaign.ById(id);
                var d = Design.FromJson(StarterFixture.Rocket(id).BuildDesign());
                var editor = LivePadLoop.Fly(catalog, mission, d, true);
                var device = LivePadLoop.FlyCard(catalog, mission, d, DevicePad.Card(mission, d));
                Assert.AreEqual(LivePadLoop.Text(editor), LivePadLoop.Text(device), id);
                Assert.AreEqual(editor.orbit.apoAlt, device.orbit.apoAlt, 0, id);
                Assert.AreEqual(editor.orbit.periAlt, device.orbit.periAlt, 0, id);
            }
        }

        internal static void AssertSame(MissionBriefing a, MissionBriefing b, string where)
        {
            Assert.IsNotNull(a, where + " editor");
            Assert.IsNotNull(b, where + " device");
            Assert.AreEqual(a.FlightCode, b.FlightCode, where);
            Assert.AreEqual(a.ContractId, b.ContractId, where);
            Assert.AreEqual(a.Title, b.Title, where);
            Assert.AreEqual(a.Goal, b.Goal, where);
            Assert.AreEqual(a.MassKg, b.MassKg, 0, where);
            Assert.AreEqual(a.ApoKm, b.ApoKm, 0, where);
            Assert.AreEqual(a.PeriKm, b.PeriKm, 0, where);
            Assert.AreEqual(a.Site.Id, b.Site.Id, where);
            Assert.AreEqual(a.Site.LatitudeDeg, b.Site.LatitudeDeg, 0, where);
            Assert.AreEqual(a.TimeId, b.TimeId, where);
            Assert.AreEqual(a.DayOfYear, b.DayOfYear, where);
            Assert.AreEqual(a.WindLine, b.WindLine, where);
            Assert.AreEqual(a.StatusLine, b.StatusLine, where);
            Assert.AreEqual(a.EngineerLine, b.EngineerLine, where);
            Assert.AreEqual(a.Wind.Calm, b.Wind.Calm, where);
            CollectionAssert.AreEqual(a.Wind.AltitudeM, b.Wind.AltitudeM, where + " wind knots");
            CollectionAssert.AreEqual(a.Wind.EastMs, b.Wind.EastMs, where + " east wind");
            CollectionAssert.AreEqual(a.Wind.NorthMs, b.Wind.NorthMs, where + " north wind");
            Assert.AreEqual(a.Bonuses.Count, b.Bonuses.Count, where);
            for (int i = 0; i < a.Bonuses.Count; i++) Assert.AreEqual(a.Bonuses[i].Id, b.Bonuses[i].Id, where);
            Assert.AreEqual(a.Replay.FailureRng, b.Replay.FailureRng, where);
        }
    }
}
