// The free-build Hopper template (hangar "hopper", orbit "suborbital") was v2 with no guidance, so
// FailureMonitor.Engages was false: Stack.SimInputs attached no monitor, the designer predicted a clean
// flight and said Ready, and the live pad used only the loose PlayedBreakup bounds (2.4 x q_lim, 20 g).
// Same family as #314. Flown with the monitor (FlightBatch does), it broke up with structural-maxq on
// every seed at T+27 s, 3.7 km, v_rel 277 m/s. Not a unit bug: the limit is 35,000 Pa (QRefKPa 35 x
// SF 1.4/1.4 x al-li 500/500 MPa) and q is 0.5 rho v_rel^2 in Pa. A max-Q cap alone does not save it:
// the engines' 60% throttle floor on a 1.5 t burnout mass is past the 8 g limit. A 75% max-Q cap, a
// 7.5 g limit and a 1.8 t payload engage the monitor and fly inside every limit (peak q 26.9 kPa).
// The KH-1 and KH-2 forks (HomeAutoBuild) override the payload and guidance, so they do not change.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class HopperTemplateFlightTests
    {
        Catalog catalog;
        HangarData hangar;
        Campaign campaign;

        [SetUp]
        public void Load()
        {
            ReferenceAscent.Reset();
            catalog = Catalog.LoadFromRepo(Golden.Root);
            hangar = HangarData.LoadFromRepo(Golden.Root);
            campaign = Campaign.Load(Golden.Root);
        }

        [TearDown]
        public void Clear() => ReferenceAscent.Reset();

        JsonObject TemplateJson() => (JsonObject)JsonMut.Clone(hangar.Template("hopper").design);

        [Test]
        public void TemplateEngagesTheFailureMonitor()
        {
            var design = Design.FromJson(TemplateJson());
            Assert.AreEqual("suborbital", design.payloadOrbit);
            Assert.IsTrue(FailureMonitor.Engages(design), "predict, pad and batch all judge the template on the same limits");
            Assert.IsNotNull(Stack.SimInputs(catalog, design).opts.monitor);
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            double expected = StructureModel.QRefKPa * 1000 * (design.safetyFactor / StructureModel.SfRef);
            Assert.AreEqual(expected, qLim, 1e-6, "al-li q_lim in pascals, not zero");
        }

        [Test]
        public void FreeBuildHopperFliesEverySeed()
        {
            var design = Design.FromJson(TemplateJson());
            double qLim = FailureMonitor.StructuralQLimPa(catalog, design);
            var seeds = campaign.Seeds;
            Assert.That(seeds.Count, Is.GreaterThanOrEqualTo(5));
            var failures = new List<string>();
            foreach (double dt in new[] { MissionFlightSetup.BatchDt, MissionFlightSetup.LiveDt })
            {
                for (int i = 0; i < 5; i++)
                {
                    var setup = MissionFlightSetup.Build(catalog, null, design, dt);
                    var monitor = FailureMonitor.Create(catalog, setup.Flown, setup.TargetAltM, FlightBatch.DesignHash(design), seeds[i]);
                    monitor.Gentle = false;
                    setup.Options.monitor = monitor;
                    var sim = Headless.Simulate(setup.Stages, setup.Options);
                    string tag = "dt " + dt.ToString("0.000") + " seed " + seeds[i];
                    if (sim.failure != null)
                    {
                        failures.Add(tag + ": " + sim.failure.kind + " at T+" + sim.failure.t.ToString("0.0") + " s");
                        continue;
                    }
                    Assert.AreEqual("suborbital", sim.outcome, tag);
                    Assert.That(sim.orbit.apoAlt, Is.GreaterThan(100000), tag + ": the hop reaches space");
                    var q = sim.risks.Find(r => r.kind == "structural-maxq");
                    Assert.That(q.value, Is.LessThan(qLim * 0.85), tag + ": peak q keeps a 15% margin");
                    Assert.That(sim.maxG, Is.LessThanOrEqualTo(design.guidance.gLimit * 1.02), tag + ": the template's own g limit holds");
                }
            }
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        [Test]
        public void TemplateIsReadyInTheDesigner()
        {
            var json = TemplateJson();
            var job = new ReferenceJob(catalog, json);
            while (!job.Step(1000)) { }
            var readout = Readout.Quick(catalog, json);
            Assert.IsNull(readout.predictedFailure, readout.predictedFailure?.engineer);
            var engineering = Validate.Engineering(catalog, json);
            Assert.IsEmpty(engineering.blocks);
            Assert.IsFalse(engineering.warnings.Exists(w => w.code == "maxq-high"), "max-Q stays under the warning line");
            var gate = LaunchGate.Evaluate(engineering, readout);
            Assert.AreEqual(LaunchGateState.Ready, gate.state, gate.Line);
            var report = StepperModel.Evaluate(catalog, json);
            Assert.IsFalse(report.HasRedRisk);
        }
    }
}
