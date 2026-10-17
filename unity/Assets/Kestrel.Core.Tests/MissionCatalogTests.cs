// Every campaign flight resolves in the catalog, and the card copy stays in plain language.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Guide;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class MissionCatalogTests
    {
        static readonly Regex RawId = new Regex(
            @"\b(kid-box|leo-sat|gto-comsat|crew-capsule|lunar-probe|weather-sat|space-telescope|cargo-capsule|cubesat-dispenser|geo-relay|lunar-lander|payloadId|maxG|noRedRisks|coastHours|nozzle-expansion|grid-fins|hydrolox-upper|stage-2|redundant-avionics|kh-m\d+)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        [Test]
        public void EveryMissionResolvesInTheCatalogAndMatchesTheDeltaVModel()
        {
            var campaign = Campaign.Load(Golden.Root);
            var catalog = Catalog.LoadFromRepo(Golden.Root);
            var raw = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "campaign.json")));
            var orbits = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "payloads.json"))).Obj("orbits");
            double proven = campaign.Mission(9).RequiredDeltaV();
            Assert.AreEqual(20, campaign.Missions.Count);

            var rows = new Dictionary<string, JsonObject>();
            foreach (JsonObject row in raw.Arr("missions")) rows[row.Str("id")] = row;

            for (int i = 0; i < campaign.Missions.Count; i++)
            {
                var mission = campaign.Missions[i];
                Assert.AreEqual(i + 1, mission.Index, mission.Id);
                Assert.IsTrue(catalog.PayloadById.ContainsKey(mission.PayloadId), mission.Id + " " + mission.PayloadId);
                Assert.IsTrue(orbits.ContainsKey(mission.OrbitId), mission.OrbitId);
                Assert.IsTrue(catalog.Orbits.ContainsKey(mission.OrbitId), mission.OrbitId);

                var orbit = catalog.Orbits[mission.OrbitId];
                var file = (JsonObject)orbits[mission.OrbitId];
                Assert.AreEqual(file.Num("apoKm"), orbit.apoKm, 1e-6, mission.OrbitId);
                Assert.AreEqual(file.Num("periKm"), orbit.periKm, 1e-6, mission.OrbitId);
                Assert.AreEqual(file.Num("incDeg"), orbit.incDeg, 1e-6, mission.OrbitId);

                if (mission.TargetKind == "c3")
                {
                    double apo = HomeAutoBuild.ApoKmForC3(mission.PeriKm, mission.C3Km2PerS2);
                    Assert.AreEqual(apo, orbit.apoKm, 0.01, mission.Id + " catalog apo");
                    Assert.AreEqual(mission.PeriKm, orbit.periKm, 0.01, mission.Id);
                }
                else if (mission.TargetKind == "apogee")
                {
                    Assert.AreEqual(mission.ApoKm, orbit.apoKm, 0.01, mission.Id);
                    Assert.Less(orbit.periKm, 0, mission.Id);
                }
                else
                {
                    Assert.AreEqual(mission.ApoKm, orbit.apoKm, 0.01, mission.Id);
                    Assert.AreEqual(mission.PeriKm, orbit.periKm, 0.01, mission.Id);
                    Assert.AreEqual(mission.IncDeg, orbit.incDeg, 0.01, mission.Id);
                }

                double stack = Stack.RequiredDeltaV(catalog, mission.OrbitId, HomeAutoBuild.LatitudeDeg).required;
                double asked = mission.RequiredDeltaV(HomeAutoBuild.LatitudeDeg);
                Assert.That(Math.Abs(stack - asked), Is.LessThan(50), mission.Id + " stack " + stack + " asked " + asked);

                Assert.Greater(mission.MassKg, 0, mission.Id);
                Assert.Greater(mission.VolumeM3, 0, mission.Id);
                Assert.LessOrEqual(mission.DiameterM + MissionHonesty.FairingClearanceM, MissionHonesty.FairingMaxM, mission.Id);
                Assert.IsTrue(PayloadForms.KnownForm(mission.Form), mission.Id + " " + mission.Form);
                Assert.IsFalse(string.IsNullOrEmpty(mission.Customer), mission.Id);
                Assert.IsFalse(string.IsNullOrEmpty(mission.Control), mission.Id);
                Assert.IsFalse(string.IsNullOrEmpty(mission.WhatItIs), mission.Id);
                Assert.IsFalse(string.IsNullOrEmpty(mission.WhatItDoes), mission.Id);
                Assert.IsFalse(string.IsNullOrEmpty(mission.Why), mission.Id);
                Assert.AreEqual(MissionHonesty.Band(mission, proven), mission.Band, mission.Id);
                Assert.That(mission.Band, Is.EqualTo("achievable").Or.EqualTo("hard"), mission.Id);
                Assert.AreEqual(CodexTitles.Line(mission.CodexId), "Codex · " + CodexTitles.Title(mission.CodexId), mission.Id);
                Assert.IsFalse(string.IsNullOrEmpty(CodexTitles.Title(mission.CodexId)), mission.Id);

                var reqs = rows[mission.Id].Arr("requirements");
                if (reqs != null)
                {
                    foreach (JsonObject req in reqs)
                    {
                        string id = req.Str("id");
                        Assert.IsTrue(MissionHonesty.KnownRequirement(id), mission.Id + " " + id);
                        if (id == "landing")
                            Assert.That(req.Str("mode"), Is.EqualTo("rtls").Or.EqualTo("barge"), mission.Id);
                    }
                }

                Plain(mission.Id + " customer", mission.Customer);
                Plain(mission.Id + " control", mission.Control);
                Plain(mission.Id + " what", mission.WhatItIs);
                Plain(mission.Id + " does", mission.WhatItDoes);
                Plain(mission.Id + " why", mission.Why);
                Plain(mission.Id + " goal", rows[mission.Id].Obj("briefing").Str("goal"));
                Plain(mission.Id + " name", rows[mission.Id].Obj("briefing").Str("payloadName"));
                Plain(mission.Id + " kind", rows[mission.Id].Obj("briefing").Str("payloadKind"));
            }

            Assert.AreEqual("achievable", campaign.Mission(10).Band);
            Assert.AreEqual("hard", campaign.Mission(16).Band);
            Assert.AreEqual("hard", campaign.Mission(17).Band);
            Assert.AreEqual("hard", campaign.Mission(18).Band);
            Assert.AreEqual("hard", campaign.Mission(19).Band);
            Assert.AreEqual("hard", campaign.Mission(20).Band);
        }

        [Test]
        public void PayloadBlurbsStayPlainAndEachFormRenders()
        {
            var doc = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "payloads.json")));
            var seen = new HashSet<string>();
            string artDir = Environment.GetEnvironmentVariable("KH_PAYLOAD_ART");
            if (!string.IsNullOrEmpty(artDir)) Directory.CreateDirectory(artDir);
            var hashes = new HashSet<int>();
            foreach (JsonObject row in doc.Arr("payloads"))
            {
                string form = row.Str("form");
                Assert.IsTrue(PayloadForms.KnownForm(form), row.Str("id"));
                Assert.IsTrue(seen.Add(form) || form.Length > 0);
                Plain(row.Str("id") + " is", row.Str("whatItIs"));
                Plain(row.Str("id") + " does", row.Str("whatItDoes"));
                Plain(row.Str("id") + " why", row.Str("why"));
                Assert.IsTrue(row.Str("why").IndexOfAny(new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }) >= 0, row.Str("id"));
            }
            foreach (string form in PayloadForms.Ids)
            {
                byte[] png = PayloadPortrait.Png(form, 128);
                Assert.AreEqual(137, png[0]);
                Assert.AreEqual(80, png[1]);
                Assert.AreEqual(78, png[2]);
                Assert.AreEqual(71, png[3]);
                byte[] rgba = PayloadPortrait.Rgba(form, 128);
                int opaque = 0;
                for (int i = 3; i < rgba.Length; i += 4) if (rgba[i] > 200) opaque++;
                Assert.Greater(opaque, 80, form);
                Assert.IsTrue(hashes.Add(Hash(rgba)), form + " portrait matches another form");
                if (!string.IsNullOrEmpty(artDir))
                    File.WriteAllBytes(Path.Combine(artDir, form + ".png"), PayloadPortrait.Png(form, 256));
            }
        }

        [Test]
        public void BriefingCardFitsSeProMaxAndIpadPortrait()
        {
            string what = "A weather satellite that sees the same towns at the same hour.";
            string does = "It repeats one ground track so the bureau can compare each day.";
            string why = "Same towns, same hour. 2,200 kg at 550 km. About 8,676 m/s.";
            string customer = "Brineglass Weather Bureau";
            string control = "Plover-1 is 2,200 kg. Sun-sync 550 km at 97.6 degrees. About 8,676 m/s.";
            string codex = "Codex · Orbits";
            foreach (var d in FlightHudLayout.PortraitDevices())
            {
                var frame = MissionCardLayout.Place(d, what, does, why, customer, control, codex);
                float left = d.SafeLeft + MissionCardLayout.MarginPt;
                float right = d.W - d.SafeRight - MissionCardLayout.MarginPt;
                AssertInside(d.Name + " art", frame.Art, left, right);
                AssertInside(d.Name + " what", frame.What, left, right);
                Assert.IsFalse(frame.Art.Overlaps(frame.What), d.Name);
                float fold = d.SafeTop + MissionCardLayout.TitlePt + frame.ScrollH;
                Assert.LessOrEqual(frame.Art.Bottom, fold + 0.6f, d.Name + " art");
                Assert.LessOrEqual(frame.What.Bottom, fold + 0.6f, d.Name + " what");
                Assert.AreEqual(MissionCardLayout.ButtonPt, frame.Build.H, 0.01f, d.Name);
                Assert.AreEqual(MissionCardLayout.ButtonPt, frame.Back.H, 0.01f, d.Name);
                Assert.AreEqual(frame.ColumnW, frame.Build.W, 0.01f, d.Name);
                Assert.AreEqual(frame.ColumnW, frame.Back.W, 0.01f, d.Name);
                Assert.GreaterOrEqual(frame.Build.Y, fold - 0.6f, d.Name);
                Assert.IsFalse(frame.Build.Overlaps(frame.Back), d.Name);
                Assert.LessOrEqual(frame.Back.Bottom, d.H - d.SafeBottom + 0.6f, d.Name);
            }
        }

        [Test]
        public void ThrumBriefsCarryNumbersAndNoRawIds()
        {
            var script = GuideScript.Load(Golden.Root);
            for (int n = 1; n <= 20; n++)
            {
                var line = script.ById("brief.kh" + n);
                Assert.IsNotNull(line, "brief.kh" + n);
                Assert.AreEqual("m" + n, line.When, line.Id);
                for (int c = 0; c < line.Cards.Count; c++)
                {
                    var card = line.Cards[c];
                    Plain(card.Id + " kid", card.Kid);
                    Plain(card.Id + " eng", card.Eng);
                    Assert.IsTrue(Regex.IsMatch(card.Eng, @"\d"), card.Id);
                }
            }
            Plain("first.5", script.ById("first.5").Cards[0].Eng);
            Plain("win.final", script.ById("win.final").Cards[0].Eng);
            StringAssert.Contains("KH-20", script.ById("win.final").Cards[0].Eng);
            StringAssert.DoesNotContain("kid-box", script.ById("brief.kh1").Cards[1].Kid);
            StringAssert.DoesNotContain("kid-box", script.ById("first.5").Cards[0].Eng);
        }

        static void AssertInside(string name, HudBox box, float left, float right)
        {
            Assert.GreaterOrEqual(box.X, left - 0.6f, name);
            Assert.LessOrEqual(box.Right, right + 0.6f, name);
            Assert.Greater(box.W, 0, name);
        }

        static void Plain(string where, string text)
        {
            Assert.IsFalse(string.IsNullOrEmpty(text), where);
            Assert.IsFalse(PlayerText.Leak(text), where + " " + text);
            Assert.IsFalse(RawId.IsMatch(text), where + " " + text);
        }

        static int Hash(byte[] bytes)
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < bytes.Length; i += 16) h = h * 31 + bytes[i];
                return h;
            }
        }
    }
}
