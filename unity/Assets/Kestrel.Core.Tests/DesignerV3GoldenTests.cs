// Designer v3 failure model and readout (§3, §5, §8 GV1–GV10).
using System;
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DesignerV3GoldenTests
    {
        static JsonObject Case(string id)
        {
            foreach (JsonObject c in Golden.File("designer-v3").Arr("cases"))
                if (c.Str("id") == id) return c;
            Assert.Fail("missing " + id);
            return null;
        }

        [Test]
        public void GV1_Tsiolkovsky()
        {
            var c = Case("GV1");
            double dv = RocketEq.DeltaV(c.Num("isp"), c.Num("m0_kg"), c.Num("mf_kg"));
            Assert.AreEqual(c.Num("deltaV_mps"), dv, 0.05, "311·g0·ln(550/150)");
        }

        [Test]
        public void GV2_IspVac()
        {
            var expect = Case("GV2").Arr("isp_s");
            const double isp = 350, epsRef = 34, pcRef = 300;
            double[] eps = { 34, 50, 80, 34 };
            double[] pc = { 300, 300, 300, 200 };
            Assert.AreEqual(4, expect.Count);
            for (int i = 0; i < 4; i++)
                Assert.AreEqual((double)expect[i], EngineModel.IspVac(isp, eps[i], epsRef, pc[i], pcRef), 0.01, "GV2");
        }

        [Test]
        public void GV3_DynamicPressure()
        {
            var c = Case("GV3");
            double qKPa = 0.5 * c.Num("rho") * c.Num("v") * c.Num("v") / 1000.0;
            Assert.AreEqual(c.Num("q_kPa"), qKPa, 0.001);
        }

        [Test]
        public void GV4_BoilOff_And_GV5_Wall()
        {
            var g4 = Case("GV4");
            double remain = TankModel.RemainingKg(g4.Num("mass_kg"), g4.Num("ratePerHour"), g4.Str("insulation"), g4.Num("hours"));
            Assert.AreEqual(g4.Num("remain_kg"), remain, 0.05);
            var g5 = Case("GV5");
            double mm = TankModel.WallThicknessM(g5.Num("safetyFactor"), g5.Num("pressure_bar"), g5.Num("radius_m"), g5.Num("yield_MPa")) * 1000.0;
            Assert.AreEqual(g5.Num("wall_mm"), mm, 1e-9);
        }

        [Test]
        public void GV6_GimbalMoment()
        {
            var c = Case("GV6");
            double mnm = EngineModel.ControlMoment(c.Num("engines"), c.Num("thrust_N"), c.Num("gimbal_deg"), c.Num("arm_m")) / 1e6;
            Assert.AreEqual(c.Num("moment_MNm"), mnm, 0.01);
        }

        [Test]
        public void GV7_NoV3Fields_DoNotEngage_AndMonitorStaysNull()
        {
            var cat = Golden.Catalog;
            foreach (var design in ReadoutBench.LoadDesigns(Golden.Root))
            {
                var d = Design.FromJson(design);
                string name = design.Str("id") ?? design.Str("name");
                Assert.IsFalse(FailureMonitor.Engages(d), name);
                var input = Stack.SimInputs(cat, d);
                Assert.IsNull(input.opts.monitor, name);
                Assert.IsNull(input.opts.guidance, name);
            }
        }

        [Test]
        public void GV8_CryoMaterial_FailsOnThePad()
        {
            var cat = Golden.Catalog;
            var d = cat.Preset("scout").Clone();
            d.version = 3;
            d.tier = "kid";
            for (int i = 0; i < d.stages.Count; i++)
            {
                d.stages[i].propellant = "hydrolox";
                d.stages[i].material = "composite";
            }
            var sim = Fly(cat, d, 5);
            Assert.IsNotNull(sim.failure, sim.outcome);
            Assert.AreEqual(Case("GV8").Str("kind"), sim.failure.kind);
            Assert.AreEqual(0, sim.failure.t, 1e-9);
            Assert.IsFalse(string.IsNullOrEmpty(sim.failure.kid));
            Assert.IsFalse(string.IsNullOrEmpty(sim.failure.engineer));
        }

        [Test]
        public void GV9_FinlessZeroGimbal_Tumbles_AndReadoutGoesRed()
        {
            JsonObject pencil = null;
            foreach (JsonObject c in Golden.File("designer-readout").Arr("cases"))
                if (c.Str("name") == "cone-nofin") pencil = JsonMut.CloneObject(c.Obj("design"));
            Assert.IsNotNull(pencil);
            JsonMut.Set(pencil, "version", 3d);
            JsonMut.Set(pencil, "tier", "kid");
            var parms = new JsonObject();
            parms.Add("gimbalDeg", 0d);
            ((JsonObject)pencil.Arr("stages")[0]).Add("engineParams", parms);
            var cat = Golden.Catalog;
            var d = Design.FromJson(pencil);
            var sim = Fly(cat, d, 30);
            Assert.IsNotNull(sim.failure, sim.outcome + " t=" + sim.time);
            Assert.AreEqual("tumble", sim.failure.kind);
            Assert.Less(sim.failure.t, 2, "the 0.5 s hold trips while dynamic pressure is still low");

            ReferenceAscent.Reset();
            var read = Readout.Reference(cat, pencil);
            Assert.IsFalse(read.maxQStale);
            Assert.IsNotNull(read.predictedFailure);
            Assert.AreEqual("tumble", read.predictedFailure.kind);
            Assert.IsNotNull(read.risks);
            FlightRisk tumble = null;
            foreach (var r in read.risks) if (r.kind == "tumble") tumble = r;
            Assert.IsNotNull(tumble);
            Assert.AreEqual("red", tumble.level);
            Assert.AreEqual("fins", tumble.field);
            Assert.AreEqual("aero", tumble.tab);

            var report = Validate.Engineering(cat, pencil);
            Issue warn = null;
            foreach (var w in report.warnings) if (w.code == "risk-tumble") warn = w;
            Assert.IsNotNull(warn, "a red risk is a warning, not a block");
            Assert.AreEqual("aero", warn.tab);
            Assert.AreEqual("fins", warn.field);
            Assert.IsTrue(report.ok || report.blocks.Count > 0);
            foreach (var b in report.blocks) Assert.AreNotEqual("risk-tumble", b.code);
        }

        [Test]
        public void GV10_LowSafetyFactor_BreaksAtMaxQ()
        {
            var spec = Case("GV10");
            var cat = Golden.Catalog;
            var d = cat.Preset("scout").Clone();
            d.version = 3;
            d.tier = "kid";
            d.safetyFactor = spec.Num("safetyFactor");
            for (int i = 0; i < d.stages.Count; i++) d.stages[i].material = "al-li";
            d.guidance = new GuidanceSpec { maxQThrottleSet = true, maxQThrottle = 100 };
            double qLim = StructureModel.QLimPa(d.safetyFactor, spec.Num("yield_MPa"));
            Assert.AreEqual(spec.Num("qLim_kPa") * 1000.0, qLim, 1e-6, "35 kPa · (1.1/1.4) · (500/500)");

            var sim = Fly(cat, d, 180);
            Assert.IsNotNull(sim.failure, sim.outcome + " maxQ " + sim.maxQ);
            Assert.AreEqual("structural-maxq", sim.failure.kind);
            Assert.Greater(sim.maxQ, qLim, "the air load crosses q_lim");
            Assert.Greater(sim.failure.t, 1, "this is the max-Q event, not a pad failure");
            StringAssert.Contains("kPa", sim.failure.kid);
        }

        [Test]
        public void Catalog_LoadsEngineEnvelope_AndMaterialModulus()
        {
            var cat = Golden.Catalog;
            var e = cat.EngineById["gg-kerolox"];
            Assert.Greater(e.pcRated, 0);
            Assert.Greater(e.cycleMassFactor, 0);
            Assert.AreEqual(e.pcRated, EngineModel.EnvelopeFor(e).pcRated, 0);
            double pc = e.pcRated, fac = e.cycleMassFactor, pcMin = e.pcMin;
            try
            {
                e.pcRated = 123;
                e.pcMin = 40;
                e.cycleMassFactor = 1.1;
                var env = EngineModel.EnvelopeFor(e);
                Assert.AreEqual(123, env.pcRated, 0);
                Assert.AreEqual(40, env.pcMin, 0);
                Assert.AreEqual(1.1, env.cycleMassFactor, 0);
            }
            finally
            {
                e.pcRated = pc;
                e.pcMin = pcMin;
                e.cycleMassFactor = fac;
            }
            Assert.AreEqual(78, cat.MaterialById["al-li"].elasticGPa, 0);
        }

        [Test]
        public void Readout_DeltaChip_AndStageFields_DoNotMoveDeltaV()
        {
            Assert.AreEqual("+42 m/s Δv · −0.03 TWR", Readout.DeltaChip(42, -0.03));
            var cat = Golden.Catalog;
            var design = (JsonObject)cat.Preset("scout").ToJson();
            var quick = Readout.Quick(cat, design);
            Assert.Greater(quick.stages[0].twrEnd, quick.stages[0].twr);
            Assert.Greater(quick.stages[0].burnTime_s, 0);
            Assert.Greater(quick.stages[0].massRatio, 1);
            Assert.IsFalse(double.IsNaN(quick.stages[0].boilOff_kg));
            Assert.IsNotNull(quick.qLim_kPa);
            Assert.AreEqual(8, quick.gLimit, 0);
            Assert.IsNull(quick.budget);
            Assert.IsNotNull(quick.stages[0].deltaVSL_mps);
            Assert.IsNull(quick.stages[quick.stages.Count - 1].deltaVSL_mps);

            JsonMut.Set(design, "version", 3d);
            var v3 = Readout.Quick(cat, design);
            Assert.AreEqual(quick.totalDeltaV, v3.totalDeltaV, 0);
            Assert.AreEqual(quick.liftoffTWR, v3.liftoffTWR, 0);
            Assert.AreEqual(quick.liftoffMass, v3.liftoffMass, 0);
            Assert.AreEqual(quick.stages[0].deltaVSL_mps, v3.stages[0].deltaVSL_mps);
        }

        [Test]
        public void Shape_AcceptsVersion3()
        {
            var design = (JsonObject)Golden.Catalog.Preset("scout").ToJson();
            JsonMut.Set(design, "version", 3d);
            var shape = Validate.Shape(Golden.Catalog, design);
            foreach (var err in shape.errors) StringAssert.DoesNotContain("version:", err);
        }

        static SimResult Fly(Catalog cat, Design d, double tMax)
        {
            var input = Stack.SimInputs(cat, d, 0.2);
            input.opts.tMax = tMax;
            Assert.IsNotNull(input.opts.monitor);
            Assert.IsFalse(input.opts.monitor.Predict, "a launch breaks up; predict is the readout cache");
            return Headless.Simulate(input.stages, input.opts);
        }
    }
}
