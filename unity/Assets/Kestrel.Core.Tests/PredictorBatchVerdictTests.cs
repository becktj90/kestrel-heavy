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
    /// <summary>
    /// #313 (Physics): the launch predictor calls the batch's acceptance rule, MissionScore.TargetMet, so the two can
    /// never disagree on whether a design reaches its target. Every shipped template and preset is flown both ways on
    /// seed 1: the predictor, and the batch path (FlightBatch.Fly for a campaign design; the same MissionFlightSetup,
    /// seeded monitor and director for a free build, judged on its catalog orbit).
    /// </summary>
    public class PredictorBatchVerdictTests
    {
        static IEnumerable<(string id, Design d)> Designs(Catalog cat)
        {
            string dir = Path.Combine(Golden.Root, "web", "js", "data", "templates");
            var files = Directory.GetFiles(dir, "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var f in files)
                yield return ("tpl-" + Path.GetFileNameWithoutExtension(f), Design.FromJson(MiniJson.ParseObject(File.ReadAllText(f))));
            foreach (var p in cat.Presets) yield return (p.id, p);
        }

        static bool BatchVerdict(Catalog cat, Design d, ulong seed, out string text)
        {
            var mission = ReferenceAscent.MissionFor(cat, d);
            if (mission != null)
            {
                var flight = FlightBatch.Fly(cat, mission, d, seed);
                bool intact = flight.failure == null && flight.outcome != "failure" && flight.outcome != "crash" && flight.outcome != "error";
                text = mission.Id + " " + flight.outcome + " " + flight.apoKm.ToString("0.0") + " x " + flight.periKm.ToString("0.0") + " km " + flight.failure;
                return intact && flight.reached;
            }
            var setup = MissionFlightSetup.Build(cat, null, d, MissionFlightSetup.BatchDt);
            var monitor = FailureMonitor.Create(cat, setup.Flown, setup.TargetAltM, FlightBatch.DesignHash(d), seed);
            monitor.Gentle = false;
            setup.Options.monitor = monitor;
            var sim = MissionFlightSetup.Fly(setup, MissionFlightSetup.Director(cat, setup, d, FlightBatch.DesignHash(d), seed));
            var o = Stack.RequiredDeltaV(cat, d.payloadOrbit).orbit;
            string kind = MissionScore.FreeOrbitKind(o.apoKm, o.periKm);
            double apo = kind == "apogee" ? MissionScore.FlownApoAltM(sim) : sim.orbit.apoAlt;
            text = (d.payloadOrbit ?? "leo400") + " " + sim.outcome + " " + (apo / 1000).ToString("0.0") + " x " + (sim.orbit.periAlt / 1000).ToString("0.0") + " km " + (sim.failure != null ? sim.failure.kind : "");
            bool ok = sim.failure == null && sim.outcome != "failure" && sim.outcome != "crash";
            return ok && MissionScore.TargetMet(kind, o.apoKm, o.periKm, double.NaN, apo, sim.orbit.periAlt, double.NaN, false, out _);
        }

        [Test]
        public void Predictor_AndBatch_GiveTheSameVerdict_OnEveryTemplateAndPreset()
        {
            var cat = Golden.Catalog;
            var campaign = Campaign.Load(Golden.Root);
            int n = 0;
            var wrong = new List<string>();
            foreach (var (id, d) in Designs(cat))
            {
                var p = LaunchPredictor.Predict(cat, d, "standard", 1);
                bool batch = BatchVerdict(cat, d, campaign.Seeds[0], out var text);
                string line = id + ": predict " + (p.Success ? "reach" : "miss") + ", batch " + (batch ? "reach" : "miss") + " (" + text + ")";
                TestContext.WriteLine(line);
                if (p.Success != batch) wrong.Add(line);
                n++;
            }
            Assert.That(n, Is.GreaterThanOrEqualTo(10), "templates and presets found");
            CollectionAssert.IsEmpty(wrong, "the predictor and the batch disagree");
        }
    }
}
