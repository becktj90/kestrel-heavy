// §2.2 propulsion. GV2 and GV6 are the spec vectors. Designs with no v3 fields stay on the catalog bell.
using System;
using Kestrel.Core.Data;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class EngineModelTests
    {
        [Test]
        public void GV2_IspVac_MatchesSpecToHundredth()
        {
            const double isp = 350, epsRef = 34, pcRef = 300;
            Assert.AreEqual(350.00, EngineModel.IspVac(isp, 34, epsRef, 300, pcRef), 0.01, "GV2a");
            Assert.AreEqual(360.80, EngineModel.IspVac(isp, 50, epsRef, 300, pcRef), 0.01, "GV2b");
            Assert.AreEqual(373.96, EngineModel.IspVac(isp, 80, epsRef, 300, pcRef), 0.01, "GV2c");
            Assert.AreEqual(347.17, EngineModel.IspVac(isp, 34, epsRef, 200, pcRef), 0.01, "GV2d");
        }

        [Test]
        public void GV2_Resolve_UsesTheSameIspVac()
        {
            var e = Synthetic();
            double[] eps = { 34, 50, 80, 34 };
            double[] pc = { 300, 300, 300, 200 };
            double[] expect = { 350.00, 360.80, 373.96, 347.17 };
            for (int i = 0; i < expect.Length; i++)
            {
                var r = EngineModel.Resolve(e, null, pc[i], eps[i], 0.4, 5, 0, null);
                Assert.AreEqual(expect[i], r.ispVac, 0.01, "GV2 " + i);
                Assert.AreEqual(EngineModel.IspVac(350, eps[i], 34, pc[i], 300), r.ispVac, 0, "formula");
            }
        }

        [Test]
        public void GV6_GimbalMoment()
        {
            // 9 × 2.3 MN at 5° on a 30 m arm → 54.12 MN·m.
            double nm = EngineModel.ControlMoment(9, 2.3e6, 5, 30);
            Assert.AreEqual(54.12e6, nm, 0.01e6);
        }

        [Test]
        public void NoV3_Identity_EveryCatalogEngine()
        {
            var c = Golden.Catalog;
            Assert.GreaterOrEqual(c.Engines.Count, 20, "spec audit counted 20; this catalog is at least that");
            int bells = 0;
            foreach (var e in c.Engines)
            {
                Check(c, e, null);
                Check(c, e, "default");
                if (e.nozzles == null) continue;
                for (int i = 0; i < e.nozzles.Count; i++) Check(c, e, e.nozzles[i].id);
            }
            Assert.GreaterOrEqual(bells, c.Engines.Count);

            void Check(Catalog cat, Engine e, string nozzle)
            {
                bells++;
                var table = EnginePoint.Of(e, nozzle);
                var part = new StagePart { engine = e.id, nozzle = nozzle, engineCount = 1 };
                var via = EnginePoint.Of(cat, part);
                string what = e.id + " " + (nozzle ?? "top");
                Assert.IsTrue(table.known, what);
                Assert.AreEqual(table.thrustSL, via.thrustSL, 0, what);
                Assert.AreEqual(table.thrustVac, via.thrustVac, 0, what);
                Assert.AreEqual(table.ispSL, via.ispSL, 0, what);
                Assert.AreEqual(table.ispVac, via.ispVac, 0, what);
                Assert.AreEqual(table.mass, via.mass, 0, what);
                Assert.AreEqual(table.diameter, via.diameter, 0, what);
                Assert.AreEqual(table.gimbal, via.gimbal, 0, what);
                Assert.AreEqual(table.restart, via.restart, what);
                Assert.AreEqual(0, via.hazardRate, 0, what);
                Assert.IsFalse(via.flowSeparation, what);
                Assert.AreSame(table.throttle, via.throttle, what);
                Assert.AreEqual(table.thrustVac, via.thrustVac, 0, what);
                if (string.IsNullOrEmpty(nozzle) || nozzle == "default")
                {
                    Assert.AreEqual(e.thrustVac, table.thrustVac, 0, what);
                    Assert.AreEqual(e.ispVac, table.ispVac, 0, what);
                    Assert.AreEqual(e.mass, table.mass, 0, what);
                    Assert.AreEqual(e.diameter, table.diameter, 0, what);
                }
            }
        }

        [Test]
        public void ReferenceInputs_ReproduceTheCatalogBell()
        {
            var c = Golden.Catalog;
            foreach (var e in c.Engines)
            {
                var r = EngineModel.Resolve(e, null, e.pc, e.expansionRatio, e.throttle[0], e.gimbal, 0, null);
                string id = e.id;
                Assert.AreEqual(e.thrustVac, r.thrustVac, 0, id);
                Assert.AreEqual(e.thrustSL, r.thrustSL, 0, id);
                Assert.AreEqual(e.ispVac, r.ispVac, 0, id);
                Assert.AreEqual(e.ispSL, r.ispSL, 0, id);
                Assert.AreEqual(e.mass, r.mass, 0, id);
                Assert.AreEqual(e.diameter, r.exitDiameter, 0, id);
                Assert.AreEqual(e.gimbal, r.gimbalDeg, 0, id);
                Assert.AreEqual(e.throttle[0], r.throttleMin, 0, id);
                Assert.AreEqual(e.throttle[1], r.throttleMax, 0, id);
                Assert.AreEqual(0, r.hazardRate, 0, id);
            }
        }

        [Test]
        public void V3Fields_RouteThroughTheModel_TuneAloneDoesNot()
        {
            var c = Golden.Catalog;
            var e = c.EngineById["ff-methalox"];
            var bare = new StagePart { engine = e.id, engineCount = 1 };
            var table = EnginePoint.Of(e, null);
            var same = EnginePoint.Of(c, bare);
            Assert.AreEqual(table.thrustVac, same.thrustVac, 0);
            Assert.AreSame(table.throttle, same.throttle);

            bare.tune = new Tune { thrustPct = 10, ispPct = 0 };
            var tunedOnly = EnginePoint.Of(c, bare);
            Assert.AreEqual(table.thrustVac, tunedOnly.thrustVac, 0, "tune stays on the Stack path");

            bare.tune = null;
            bare.engineParams = new EngineParams { pcSet = true, pc = e.pc * 1.1 };
            var raised = EnginePoint.Of(c, bare);
            Assert.AreEqual(table.thrustVac * 1.1, raised.thrustVac, 1e-6, "F ∝ pc");
            Assert.AreEqual(table.mass * Math.Sqrt(1.1), raised.mass, 1e-6, "mass ∝ pc^0.5");
            Assert.Greater(raised.ispVac, table.ispVac);
            Assert.AreEqual(0.02 * 0.1 * 0.1, raised.hazardRate, 1e-12, "pc/pcRated = 1.1 is over rated and under the 1.15 cap");
        }

        [Test]
        public void Overpressure_GimbalMass_DeepThrottle_Mixture_AndTune()
        {
            var e = Golden.Catalog.EngineById["ff-methalox"];
            var hot = EngineModel.Resolve(e, null, e.pc * 1.1, e.expansionRatio, e.throttle[0], e.gimbal, 0, null);
            Assert.AreEqual(0.02 * 0.1 * 0.1, hot.hazardRate, 1e-15);

            var capped = EngineModel.Resolve(e, null, 1000, e.expansionRatio, e.throttle[0], e.gimbal, 0, null);
            Assert.AreEqual(0.02 * 0.15 * 0.15, capped.hazardRate, 1e-15, "pc clamps at pcRated×1.15");

            var gimbal = EngineModel.Resolve(e, null, e.pc, e.expansionRatio, e.throttle[0], e.gimbal - 10, 0, null);
            Assert.AreEqual(e.mass * (1 + EngineModel.GimbalMassPerDeg * -10), gimbal.mass, 1e-9);

            var deep = EngineModel.Resolve(e, null, e.pc, e.expansionRatio, 0.20, e.gimbal, 0, null);
            Assert.AreEqual(e.mass * 1.02, deep.mass, 1e-9, "+1% mass per 10% below 40%");
            Assert.AreEqual(0.20, deep.throttleMin, 0);

            var mix = EngineModel.Resolve(e, null, e.pc, e.expansionRatio, e.throttle[0], e.gimbal, 10, null);
            Assert.AreEqual(e.ispVac * (1 - 0.006 * 10), mix.ispVac, 1e-9);
            Assert.IsTrue(mix.mixtureInstability);
            var edge = EngineModel.Resolve(e, null, e.pc, e.expansionRatio, e.throttle[0], e.gimbal, 6, null);
            Assert.IsFalse(edge.mixtureInstability);

            var tune = EngineModel.Resolve(e, null, e.pc, e.expansionRatio, e.throttle[0], e.gimbal, 0, new Tune { thrustPct = 10, ispPct = -5 });
            Assert.AreEqual(e.thrustVac * 1.10, tune.thrustVac, 1e-6);
            Assert.AreEqual(e.ispVac * 0.95, tune.ispVac, 1e-9);
            Assert.AreEqual(1 - 0.005 * 10, tune.tuneReliability, 1e-12, "only the positive tune percent");
        }

        [Test]
        public void FlowSeparation_FlagAndFifteenPercent()
        {
            var e = Golden.Catalog.EngineById["ff-methalox"];
            Assert.IsFalse(EngineModel.FlowSeparates(e.pc, e.expansionRatio));
            Assert.IsTrue(EngineModel.FlowSeparates(e.pc, 80));
            var wide = EngineModel.Resolve(e, null, e.pc, 80, e.throttle[0], e.gimbal, 0, null);
            Assert.IsTrue(wide.flowSeparation);

            double ispVac = EngineModel.IspVac(e.ispVac, 80, e.expansionRatio, e.pc, e.pc);
            double thrustVac = e.thrustVac;
            double exit = e.diameter * Math.Sqrt(80 / e.expansionRatio);
            double raw = EngineModel.SeaLevelIsp(ispVac, thrustVac, exit) * EngineModel.SeparationIspFactor;
            double rawRef = EngineModel.SeaLevelIsp(e.ispVac, e.thrustVac, e.diameter);
            Assert.AreEqual(e.ispSL + (raw - rawRef), wide.ispSL, 1e-6);
        }

        [Test]
        public void Envelope_MatchesTheCycleTable_AndCoversEveryBell()
        {
            var c = Golden.Catalog;
            foreach (var e in c.Engines)
            {
                var env = EngineModel.EnvelopeFor(e);
                Assert.AreEqual(e.pc, env.pcRated, 0, e.id);
                Assert.Less(env.pcMin, env.pcRated, e.id);
                Assert.GreaterOrEqual(env.epsMax, env.epsMin, e.id);
                Assert.LessOrEqual(env.epsMin, e.expansionRatio, e.id);
                Assert.GreaterOrEqual(env.epsMax, e.expansionRatio, e.id);
                Assert.AreEqual(EngineModel.CycleMassFactor(e.cycle), env.cycleMassFactor, 0, e.id);
                if (e.nozzles == null) continue;
                for (int i = 0; i < e.nozzles.Count; i++)
                {
                    double eps = e.nozzles[i].expansionRatio;
                    Assert.LessOrEqual(env.epsMin, eps, e.id);
                    Assert.GreaterOrEqual(env.epsMax, eps, e.id);
                }
            }
            Assert.AreEqual(0.8, EngineModel.CycleMassFactor("pressure-fed"), 0);
            Assert.AreEqual(1.0, EngineModel.CycleMassFactor("gas-generator"), 0);
            Assert.AreEqual(1.05, EngineModel.CycleMassFactor("expander"), 0);
            Assert.AreEqual(1.15, EngineModel.CycleMassFactor("staged-combustion"), 0);
            Assert.AreEqual(1.25, EngineModel.CycleMassFactor("full-flow"), 0);
            Assert.AreEqual(100 * 1.25 / 1.0 * Math.Sqrt(200.0 / 300.0), EngineModel.ScaledMass(100, 200, 300, 1.25, 1.0), 1e-9);
        }

        static Engine Synthetic()
        {
            return new Engine
            {
                id = "gv2-ref",
                cycle = "full-flow",
                thrustSL = 1,
                thrustVac = 2.3e6,
                ispSL = 350,
                ispVac = 350,
                mass = 1000,
                pc = 300,
                gimbal = 5,
                expansionRatio = 34,
                diameter = 1.3,
                throttle = new[] { 0.4, 1 },
                restart = true,
            };
        }
    }
}
