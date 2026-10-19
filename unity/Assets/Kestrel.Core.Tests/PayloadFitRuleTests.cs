// B-04: one fairing-fit rule. Validate, Readout, the stepper and LaunchGate used three different
// rules (docs/audits/DESIGNER_MISSION_DISCREPANCIES.md). They now all call PayloadFit, which is the
// rule FlightBatch judges with. Expected numbers come from the §2.9 formula
// usable = π(0.45·D)²·0.8·L, cargo diameter ≤ 0.9·D (same 0.45·D envelope), and the cargo in campaign.json / payloads.json.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class PayloadFitRuleTests
    {
        Catalog catalog;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            catalog = Catalog.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        /// <summary>What each of the four surfaces says about fit, plus the flight judge.</summary>
        (bool validate, bool readout, bool stepper, bool gate, bool judge) Verdicts(JsonObject design, CampaignMission mission)
        {
            var report = Validate.Engineering(catalog, design);
            bool validate = !report.blocks.Exists(b => b.code == "payload-doesnt-fit");
            var quick = Readout.Quick(catalog, design);
            bool readout = quick.fairingFits == true;
            var aero = StepperChecks.Aero(catalog, Design.FromJson(design), quick, null);
            bool stepper = aero.Code != "payload-doesnt-fit";
            var gate = LaunchGate.Evaluate(new EngineeringReport { ok = true, readout = quick }, quick);
            bool gateFits = gate.code != "payload-doesnt-fit";
            bool judge = PayloadFit.Check(Design.FromJson(design), PayloadFit.MissionSpec(mission)).fits;
            return (validate, readout, stepper, gateFits, judge);
        }

        void AssertAgree(JsonObject design, CampaignMission mission, bool expected, string where)
        {
            var v = Verdicts(design, mission);
            string got = $"validate {v.validate} readout {v.readout} stepper {v.stepper} gate {v.gate} judge {v.judge}";
            Assert.AreEqual(expected, v.judge, where + " judge: " + got);
            Assert.AreEqual(expected, v.validate, where + " Validate: " + got);
            Assert.AreEqual(expected, v.readout, where + " Readout: " + got);
            Assert.AreEqual(expected, v.stepper, where + " stepper: " + got);
            Assert.AreEqual(expected, v.gate, where + " LaunchGate: " + got);
        }

        JsonObject Stamped(string file, string missionId)
        {
            var root = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, file)));
            foreach (JsonObject row in root.Arr("missions"))
                if (row.Str("id") == missionId)
                {
                    var d = JsonMut.CloneObject((JsonObject)row.Arr("designs")[0]);
                    HomeAutoBuild.ApplyMission(catalog, d, campaign.ById(missionId));
                    return d;
                }
            return null;
        }

        static void SetFairing(JsonObject design, double diameter, double length)
        {
            JsonMut.Set(design.Obj("fairing"), "diameter", diameter);
            JsonMut.Set(design.Obj("fairing"), "length", length);
        }

        [Test]
        public void PayloadDiameter_IsParsedFromTheCatalog()
        {
            Assert.AreEqual(3.4, catalog.PayloadById["gto-comsat"].diameter);
            Assert.AreEqual(0.5, catalog.PayloadById["cubesat"].diameter);
            Assert.AreEqual(3.6, catalog.PayloadById["crew-capsule"].diameter);
            Assert.AreEqual(20, catalog.MissionPayloads.Count, "every mission's cargo is registered");
            Assert.AreEqual(25, catalog.MissionPayloads["m8"].volume, "KH-8 cargo from campaign.json, not the 40 m³ catalog capsule");
        }

        [Test]
        public void EveryWinnerAndBoardStarter_AllFourAgreeWithTheJudge_AndFit()
        {
            int n = 0;
            foreach (var m in campaign.Missions)
            {
                foreach (var file in new[] { Path.Combine("data", "golden", "campaign-winners.json"), Path.Combine("data", "board-starters.json") })
                {
                    var d = Stamped(file, m.Id);
                    if (d == null) continue;
                    AssertAgree(d, m, true, m.Id + " " + file);
                    n++;
                }
            }
            Assert.AreEqual(38, n, "20 winners + 18 board starters");
        }

        // The audit's "designer passes losers" table: fairing D × L that the mission check rejects.
        [TestCase("m4", 1.6, 15)] [TestCase("m7", 3.0, 15)] [TestCase("m8", 2.4, 15)] [TestCase("m9", 2.0, 12)]
        [TestCase("m10", 3.2, 15)] [TestCase("m11", 1.6, 8)] [TestCase("m12", 1.6, 10)] [TestCase("m14", 2.4, 15)]
        [TestCase("m15", 3.0, 12)] [TestCase("m16", 2.4, 15)] [TestCase("m18", 2.0, 12)] [TestCase("m19", 3.6, 15)]
        [TestCase("m20", 2.0, 15)]
        public void AuditLosers_AreRejectedEverywhere(string missionId, double diameter, double length)
        {
            var d = Stamped(Path.Combine("data", "golden", "campaign-winners.json"), missionId);
            SetFairing(d, diameter, length);
            AssertAgree(d, campaign.ById(missionId), false, missionId + " " + diameter + "x" + length);
        }

        [Test]
        public void Diameter_IsChecked_AWideCargoInANarrowLongFairingDoesNotFit()
        {
            var d = Stamped(Path.Combine("data", "golden", "campaign-winners.json"), "m12");
            SetFairing(d, 1.5, 20); // usable π(0.675)²·0.8·20 ≈ 22.9 m³ > 10 m³, but the telescope is 2.4 m wide
            var fit = PayloadFit.Check(catalog, d).Value;
            Assert.AreEqual(Math.PI * 0.675 * 0.675 * 0.8 * 20 - 10, fit.volumeMargin, 1e-9);
            Assert.Greater(fit.volumeMargin, 0);
            Assert.AreEqual(0.9 * 1.5 - 2.4, fit.diameterMargin, 1e-12);
            Assert.AreEqual(fit.diameterMargin, Readout.Quick(catalog, d).fairingDiameterMargin_m, 1e-12);
            AssertAgree(d, campaign.ById("m12"), false, "m12 1.5x20");
            var block = Validate.Engineering(catalog, d).blocks.Find(b => b.code == "payload-doesnt-fit");
            StringAssert.Contains("2.4 m wide", block.msg);
        }

        [Test]
        public void Boundary_ExactlyAtTheLimitFits_JustUnderDoesNot()
        {
            var m = campaign.ById("m4");
            var spec = PayloadFit.MissionSpec(m); // 12 m³, 2.0 m
            double dia = PayloadFit.RequiredDiameter(spec);
            Assert.AreEqual(2.0 / 0.9, dia, 1e-12);
            double len = PayloadFit.RequiredLength(spec, dia);
            Assert.AreEqual(12 / (Math.PI * Math.Pow(0.45 * dia, 2) * 0.8), len, 1e-9);
            var d = Stamped(Path.Combine("data", "golden", "campaign-winners.json"), "m4");
            SetFairing(d, dia, len * (1 + 1e-9));
            AssertAgree(d, m, true, "at the limit");
            SetFairing(d, dia, len * 0.999);
            AssertAgree(d, m, false, "0.1% short");
            SetFairing(d, dia - 0.01, 40);
            AssertAgree(d, m, false, "1 cm too narrow");
        }

        [TestCase(2.70, true)]
        [TestCase(2.71, false)]
        public void DiameterEnvelope_IsNineTenthsOfTheFairing(double cargoDiameter, bool fits)
        {
            // Physics review: D = 3 m holds a 2.70 m cargo, not a 2.71 m one.
            var spec = new PayloadSpec { id = "edge", volume = 1, diameter = cargoDiameter };
            var fit = PayloadFit.Check(3.0, 20, spec);
            Assert.AreEqual(fits, fit.fits);
            Assert.AreEqual(0.9 * 3.0 - cargoDiameter, fit.diameterMargin, 1e-12);
            // The designer surfaces follow the same rule for a mission carrying that cargo.
            var d = Stamped(Path.Combine("data", "golden", "campaign-winners.json"), "m4");
            SetFairing(d, 3.0, 20);
            catalog.MissionPayloads["m4"] = spec;
            Assert.AreEqual(fits, !Validate.Engineering(catalog, d).blocks.Exists(b => b.code == "payload-doesnt-fit"), "Validate");
            var quick = Readout.Quick(catalog, d);
            Assert.AreEqual(fits, quick.fairingFits == true, "Readout");
            Assert.AreEqual(fits, StepperChecks.Aero(catalog, Design.FromJson(d), quick, null).Code != "payload-doesnt-fit", "stepper");
            Assert.AreEqual(fits, LaunchGate.Evaluate(new EngineeringReport { ok = true, readout = quick }, quick).code != "payload-doesnt-fit", "LaunchGate");
        }

        [Test]
        public void FreeDesign_UsesTheCatalogCargo_NoPayloadMeansNoVerdict()
        {
            var d = Stamped(Path.Combine("data", "golden", "campaign-winners.json"), "m4");
            JsonMut.Set(d, "missionId", "");
            var spec = PayloadFit.SpecFor(catalog, d);
            Assert.AreEqual(2.0, spec.diameter, "leo-sat catalog diameter");
            Assert.AreEqual(12 * Math.Min(1.5, Math.Pow(2500.0 / 1500, 2.0 / 3.0)), spec.volume, 1e-9, "catalog volume scaled by mass");
            JsonMut.Set(d.Obj("payload"), "type", null);
            Assert.IsNull(PayloadFit.SpecFor(catalog, d));
            Assert.IsNull(Readout.Quick(catalog, d).fairingFits);
            Assert.IsFalse(Validate.Engineering(catalog, d).blocks.Exists(b => b.code == "payload-doesnt-fit"));
        }

        [Test]
        public void MakeItFly_WidensAndStretches_UntilTheRuleSaysFits()
        {
            var m = campaign.ById("m7");
            var d = Stamped(Path.Combine("data", "golden", "campaign-winners.json"), "m7");
            SetFairing(d, 1.5, 2);
            var (fly, changes) = Validate.MakeItFly(catalog, d);
            Assert.IsTrue(PayloadFit.Check(catalog, fly).Value.fits, string.Join(" | ", changes));
            Assert.IsTrue(PayloadFit.Check(Design.FromJson(fly), PayloadFit.MissionSpec(m)).fits);
        }
    }
}
