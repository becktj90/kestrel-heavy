// Fairing joint and flare, phase 1 (DECISIONS #71; data/designer-limits.json fairingFlare). The base is the top stage's
// diameter, the barrel stays fairing.diameter, a 15 degree cone joins them, and a very big flare is a WARNING only.
// tests/fairing-flare.test.cjs is the web twin; the texts match. Nothing here moves a mass, Cd, fit or flight number.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FairingFlareTests
    {
        const string FlareText = "This fairing flares a lot above the stage. Try a wider stage or a smaller fairing.";
        Catalog catalog;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        static JsonObject LimitsRow() => MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "designer-limits.json"))).Obj("fairingFlare");
        static bool Has(List<Issue> issues, string code) => issues.Exists(i => i.code == code);
        static Issue Find(List<Issue> issues, string code) => issues.Find(i => i.code == code);
        static double TopD(JsonObject d) { var s = d.Arr("stages"); return ((JsonObject)s[s.Count - 1]).Num("diameter"); }
        static void SetFairing(JsonObject d, string key, double v) => JsonMut.Set(d.Obj("fairing"), key, v);

        // ---------------- the data ----------------

        [Test]
        public void LimitsFile_CarriesTheThresholds_AndDesignerLimitsParsesThem()
        {
            var row = LimitsRow();
            Assert.IsNotNull(row, "fairingFlare must be in designer-limits.json");
            Assert.AreEqual(1.5, row.Num("warnRatio"), 0);
            Assert.AreEqual(30, row.Num("warnAngleDeg"), 0);
            Assert.AreEqual(15, row.Num("shoulderAngleDeg"), 0);
            Assert.AreEqual(0.5, row.Num("shoulderMaxCylFraction"), 0);
            var lim = DesignerLimits.Active;
            Assert.AreEqual(row.Num("warnRatio"), lim.fairingFlareWarnRatio, 0);
            Assert.AreEqual(row.Num("warnAngleDeg"), lim.fairingFlareWarnAngleDeg, 0);
            Assert.AreEqual(row.Num("shoulderAngleDeg"), lim.fairingShoulderAngleDeg, 0);
            Assert.AreEqual(row.Num("shoulderMaxCylFraction"), lim.fairingShoulderMaxCylFraction, 0);
        }

        [Test]
        public void WebLimits_MatchTheLimitsFile()
        {
            string js = File.ReadAllText(Path.Combine(Golden.Root, "web", "js", "designer", "limits.js"));
            var row = LimitsRow();
            string F(double v) => v.ToString("0.0###", System.Globalization.CultureInfo.InvariantCulture);
            StringAssert.Contains("FAIRING_FLARE_WARN_RATIO = " + F(row.Num("warnRatio")) + ";", js);
            StringAssert.Contains("FAIRING_FLARE_WARN_ANGLE_DEG = " + (int)row.Num("warnAngleDeg") + ";", js);
            StringAssert.Contains("FAIRING_SHOULDER_ANGLE_DEG = " + (int)row.Num("shoulderAngleDeg") + ";", js);
            StringAssert.Contains("FAIRING_SHOULDER_MAX_CYL_FRACTION = " + F(row.Num("shoulderMaxCylFraction")) + ";", js);
        }

        // ---------------- the shoulder maths ----------------

        [Test]
        public void Shoulder_IsFifteenDegrees_UntilTheCylinderLimitBites()
        {
            // 0.3 m of radius change at 15 degrees: 0.3 / tan(15) = 1.12 m, well inside half of an 8 m cylinder.
            double l = FairingFlare.ShoulderLength(1.85, 2.15, 8.0);
            Assert.AreEqual(0.3 / Math.Tan(15 * Math.PI / 180), l, 1e-9);
            Assert.AreEqual(15.0, FairingFlare.ShoulderAngleDeg(1.85, 2.15, l), 1e-9);
            // The same flare on a short cylinder is limited to half of it, so it is steeper than 15 degrees.
            double lim = FairingFlare.ShoulderLength(1.85, 2.15, 1.0);
            Assert.AreEqual(0.5, lim, 1e-12);
            Assert.AreEqual(Math.Atan(0.3 / 0.5) * 180 / Math.PI, FairingFlare.ShoulderAngleDeg(1.85, 2.15, lim), 1e-9);
            // Matching radii: no shoulder at all.
            Assert.AreEqual(0, FairingFlare.ShoulderLength(2.0, 2.0, 8.0), 0);
            Assert.AreEqual(0, FairingFlare.ShoulderLength(2.0, 2.5, 0), 0);
        }

        [Test]
        public void Radius_RunsFromTheStageRadiusToTheBarrel_NoLedge()
        {
            double l = FairingFlare.ShoulderLength(1.85, 2.6, 9.0);
            Assert.AreEqual(1.85, FairingFlare.Radius(1.85, 2.6, l, 0), 0, "starts at the stage radius, not the barrel");
            Assert.AreEqual(2.225, FairingFlare.Radius(1.85, 2.6, l, l / 2), 1e-9, "linear: a cone");
            Assert.AreEqual(2.6, FairingFlare.Radius(1.85, 2.6, l, l), 1e-12);
            Assert.AreEqual(2.6, FairingFlare.Radius(1.85, 2.6, l, l + 5), 0);
            Assert.AreEqual(2.0, FairingFlare.Radius(2.0, 2.0, 0, 0), 0);
        }

        [Test]
        public void CargoDrawRadius_IsTheRealCargoDiameter()
        {
            Assert.AreEqual(1.7, FairingFlare.CargoDrawRadius(3.4, 2.6), 1e-12, "3.4 m cargo draws 1.7 m, not 0.55 x the barrel");
            Assert.AreEqual(2.6 * 0.98, FairingFlare.CargoDrawRadius(6.0, 2.6), 1e-12, "held just inside the skin");
            Assert.AreEqual(2.6 * 0.55, FairingFlare.CargoDrawRadius(0, 2.6), 1e-12, "no known cargo: the old fallback");
        }

        // ---------------- the advisory ----------------

        [Test]
        public void Extreme_RatioOrAngle_AndNeverWhenNotAFlare()
        {
            Assert.IsFalse(FairingFlare.Extreme(5.55, 3.7, 12, "ogive"), "exactly 1.5 does not warn");
            Assert.IsTrue(FairingFlare.Extreme(5.95, 3.7, 12, "ogive"), "r = 1.608");
            Assert.IsFalse(FairingFlare.Extreme(3.6, 4.1, 12, "ogive"), "narrower than the stage is not a flare");
            Assert.IsFalse(FairingFlare.Extreme(3.7, 3.7, 12, "ogive"));
            // r = 1.46, so the ratio alone is quiet, but a 4 m fairing gives a 2.2 m cylinder: L_sh 1.1 m, ~37.7 degrees.
            Assert.IsTrue(FairingFlare.Extreme(5.4, 3.7, 4, "ogive"), "steeper than 30 degrees");
            Assert.IsFalse(FairingFlare.Extreme(5.4, 3.7, 12, "ogive"), "the same widths on a long fairing are a gentle 15 degrees");
        }

        static JsonObject Workhorse() => HangarFixtures.Template("workhorse");

        [Test]
        public void RatioOverOnePointFive_RaisesTheAdvisory_AsAWarningOnly()
        {
            var d = Workhorse();
            SetFairing(d, "diameter", TopD(d) * 1.6);
            var r = Validate.Engineering(catalog, d);
            var w = Find(r.warnings, "fairing-flare");
            Assert.IsNotNull(w, "r = 1.6 raises fairing-flare");
            Assert.AreEqual("payload", w.tab);
            Assert.AreEqual(FlareText, w.msg);
            Assert.IsFalse(Has(r.blocks, "fairing-flare"), "never a block");
            Assert.IsTrue(r.ok, "the design is still allowed: " + string.Join(",", r.blocks.ConvertAll(b => b.code)));
        }

        [Test]
        public void SteepShoulder_RaisesTheAdvisory_AsAWarningOnly()
        {
            var d = Workhorse(); // top 3.7 m
            SetFairing(d, "diameter", 5.4);
            SetFairing(d, "length", 4);
            var r = Validate.Engineering(catalog, d);
            Assert.IsTrue(Has(r.warnings, "fairing-flare"), "theta over 30 degrees raises it with r under 1.5");
            Assert.IsFalse(Has(r.blocks, "fairing-flare"));
        }

        [Test]
        public void ModestFlare_RaisesNothing()
        {
            var d = Workhorse(); // 4.2 on 3.7
            Assert.IsFalse(Has(Validate.Engineering(catalog, d).warnings, "fairing-flare"));
        }

        [Test]
        public void FairingNarrow_StillBlocks_AndTheTextNamesTheFitRule()
        {
            var d = Workhorse();
            SetFairing(d, "diameter", TopD(d) * 0.4);
            var r = Validate.Engineering(catalog, d);
            var b = Find(r.blocks, "fairing-narrow");
            Assert.IsNotNull(b, "narrower than half the stage still blocks");
            Assert.IsFalse(r.ok);
            StringAssert.Contains("less than half as wide as the top stage", b.msg);
            StringAssert.Contains("0.9", b.msg, "matches the cargo fit rule (cargo <= 0.9 x the fairing width)");
            Assert.AreEqual(0.9, PayloadFit.DiameterFactor, 0, "the rule the text quotes");
            // Exactly half is allowed.
            SetFairing(d, "diameter", TopD(d) * 0.5);
            Assert.IsFalse(Has(Validate.Engineering(catalog, d).blocks, "fairing-narrow"));
        }

        [Test]
        public void NothingThatShips_RaisesTheFlareAdvisory()
        {
            int n = 0;
            foreach (var d in HangarFixtures.AllDesigns())
            {
                Assert.IsFalse(Has(Validate.Engineering(catalog, d).warnings, "fairing-flare"), "hangar/preset design");
                n++;
            }
            Assert.Greater(n, 5);
            foreach (var m in campaign.Missions)
                foreach (var file in new[] { Path.Combine("data", "board-starters.json"), Path.Combine("data", "golden", "campaign-winners.json") })
                {
                    var root = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, file)));
                    foreach (JsonObject row in root.Arr("missions"))
                    {
                        if (row.Str("id") != m.Id) continue;
                        var d = JsonMut.CloneObject((JsonObject)row.Arr("designs")[0]);
                        HomeAutoBuild.ApplyMission(catalog, d, m);
                        Assert.IsFalse(Has(Validate.Engineering(catalog, d).warnings, "fairing-flare"), Path.GetFileName(file) + " " + m.Id);
                        n++;
                    }
                }
            Assert.Greater(n, 30);
        }

        [Test]
        public void TheAdvisory_NeverMovesTheNumbers()
        {
            // Same design, fairing only wider: the advisory text is the only thing that appears; the block list is unchanged.
            var a = Workhorse();
            var b = Workhorse();
            SetFairing(b, "diameter", TopD(b) * 1.6);
            var ra = Validate.Engineering(catalog, a);
            var rb = Validate.Engineering(catalog, b);
            CollectionAssert.AreEqual(ra.blocks.ConvertAll(x => x.code), rb.blocks.ConvertAll(x => x.code));
        }
    }
}
