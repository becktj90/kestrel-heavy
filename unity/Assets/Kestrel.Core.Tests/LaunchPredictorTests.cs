using System;
using System.IO;
using System.Text;
using Kestrel.Core.Designer;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class LaunchPredictorTests
    {
        static readonly string[] Ids = { "tpl-sounding-hybrid", "scout", "kid", "super-heavy" };

        [Test]
        public void Prediction_IsTheSim_AndTheTimeline()
        {
            var cat = Golden.Catalog;
            foreach (var id in Ids)
            {
                var d = Load(id);
                var p = LaunchPredictor.Predict(cat, d, "standard", 1);
                var again = LaunchPredictor.Predict(cat, d, "standard", 99);
                // #314 + KH-7 E: the prediction is ReferenceAscent.Flight (director + PlayedChecks + briefing wind).
                var sim = ReferenceAscent.Flight(cat, d).Run();
                var req = Stack.RequiredDeltaV(cat, d.payloadOrbit);
                double apo = MissionScore.FlownApoAltM(sim);
                bool success = LaunchPredictor.Accepts(cat, d, req, sim, apo);

                Assert.AreEqual(sim.outcome, p.Outcome, id);
                Assert.AreEqual(Join(sim), p.Timeline, id);
                Assert.AreEqual(success, p.Success, id);
                Assert.AreEqual(success ? 1 : 0, p.Probability, 0, id);
                Assert.AreEqual(success ? "pass" : "fail", p.Meter, id);
                Assert.AreEqual(1, p.Runs, id);
                Assert.AreEqual(Math.Floor(apo / 100.0) / 10.0, p.ApoKm, 1e-9, id);
                Assert.LessOrEqual(apo, sim.orbit.apoAlt + 1e-6, id + " drag only lowers the peak");
                Assert.AreEqual(p.Canonical(), again.Canonical().Replace("seed=99", "seed=1"), id + " seed");
                Assert.IsTrue(p.Text.IndexOf("almost", StringComparison.OrdinalIgnoreCase) < 0, id);
                if (!success) StringAssert.Contains("does not reach", p.Text, id);
            }
        }

        [Test]
        public void KidMeter_IsPassOrFail_WithoutAPercent()
        {
            var cat = Golden.Catalog;
            var d = Load("super-heavy");
            var kid = LaunchPredictor.Predict(cat, d, "kid", 1);
            var standard = LaunchPredictor.Predict(cat, d, "standard", 1);
            Assert.AreEqual(standard.Success, kid.Success);
            Assert.AreEqual(standard.Timeline, kid.Timeline);
            StringAssert.DoesNotContain("%", kid.Text);
            StringAssert.StartsWith(kid.Success ? "Pass." : "Fail.", kid.Text);
        }

        [Test]
        public void GoldenPredictions_MatchCommittedVectors()
        {
            var cat = Golden.Catalog;
            var sb = new StringBuilder();
            foreach (var id in Ids)
            {
                var d = Load(id);
                sb.Append(LaunchPredictor.Predict(cat, d, "standard", 1).Canonical());
                sb.Append("---\n");
                sb.Append(LaunchPredictor.Predict(cat, d, "kid", 1).Canonical());
                sb.Append("---\n");
            }
            string path = Path.Combine(Golden.Root, "tests", "vectors", "prediction", "launches.txt");
            if (Environment.GetEnvironmentVariable("KESTREL_WRITE_PREDICT") == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, sb.ToString());
            }
            Assert.IsTrue(File.Exists(path), "missing " + path);
            Assert.AreEqual(File.ReadAllText(path), sb.ToString());
        }

        static string Join(SimResult sim)
        {
            var s = "";
            for (int i = 0; i < sim.events.Count; i++)
            {
                if (i > 0) s += ",";
                s += sim.events[i].type;
            }
            return s;
        }

        static Design Load(string id)
        {
            if (id.StartsWith("tpl-", StringComparison.Ordinal))
            {
                string file = id.Substring(4) + ".json";
                string path = Path.Combine(Golden.Root, "web", "js", "data", "templates", file);
                return Design.FromJson(MiniJson.ParseObject(File.ReadAllText(path)));
            }
            return Golden.Catalog.Preset(id);
        }
    }
}
