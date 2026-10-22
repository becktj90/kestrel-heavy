// #341: the guidance computer's law is the same in JS (web/js/physics/headless.js) and C# (AscentSim.ClosedLoopPitch),
// one physics. tests/fixtures/closed-loop-parity.json comes from tools/export-closed-loop-parity.mjs; this flies the
// same inputs through Headless.Simulate and holds them to the simulate() golden tolerance (SIMTOL).
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class ClosedLoopParityTests
    {
        static JsonObject file;
        static JsonObject File()
            => file ??= MiniJson.ParseObject(System.IO.File.ReadAllText(Path.Combine(Golden.Root, "tests", "fixtures", "closed-loop-parity.json")));

        static IEnumerable<TestCaseData> Cases()
        {
            foreach (JsonObject c in File().Arr("cases")) yield return new TestCaseData(c.Str("name")).SetName("ClosedLoopParity_" + c.Str("name"));
        }

        [Test]
        public void Tolerance_IsTheSimulateGoldens()
        {
            var tol = File().Obj("tolerance");
            var golden = Golden.File("simulate").Obj("tolerance");
            Assert.AreEqual(golden.Num("rel"), tol.Num("rel"));
            Assert.AreEqual(golden.Num("abs"), tol.Num("abs"));
            Assert.That(File().Arr("cases").Count, Is.GreaterThanOrEqualTo(10));
        }

        [TestCaseSource(nameof(Cases))]
        public void ClosedLoop_MatchesJs(string name)
        {
            var tol = File().Obj("tolerance"); double REL = tol.Num("rel"), ABS = tol.Num("abs");
            JsonObject g = null; foreach (JsonObject c in File().Arr("cases")) if (c.Str("name") == name) g = c;
            var inp = g.Obj("in"); var oo = inp.Obj("opts");
            var stages = new List<SimStage>();
            foreach (JsonObject s in inp.Arr("stages"))
                stages.Add(new SimStage { thrustSL = s.Num("thrustSL"), thrustVac = s.Num("thrustVac"), ispSL = s.Num("ispSL"), ispVac = s.Num("ispVac"), propMass = s.Num("propMass"), dryMass = s.Num("dryMass"), diameter = s.Num("diameter"), cd = s.Num("cd", 0.3) });
            var opts = new SimOptions
            {
                payload = oo.Num("payload", 0), targetAlt = oo.Num("targetAlt", 200e3), latitude = oo.Num("latitude", 28.5), dt = oo.Num("dt", 0.05),
                tMax = oo.Num("tMax", 3000), sampleEvery = oo.Num("sampleEvery", 0), vacuum = oo.Bool("vacuum", false),
                rotate = !oo.ContainsKey("rotate") || oo.Bool("rotate"), cutoff = !oo.ContainsKey("cutoff") || oo.Bool("cutoff"),
                energyCutoff = oo.Bool("energyCutoff", false), cutoffEnergy = oo.ContainsKey("cutoffEnergy") ? oo.Num("cutoffEnergy") : double.NaN,
                closedLoopInsertion = oo.Bool("closedLoop", false), closedLoopAimAlt = oo.ContainsKey("closedLoopAimAlt") ? oo.Num("closedLoopAimAlt") : double.NaN,
                closedLoopRelaxPitch = oo.Bool("closedLoopRelaxPitch", false),
            };
            var r = Headless.Simulate(stages, opts); var e = g.Obj("out");
            void N(double a, object x, string w, double absTol = -1) => Golden.Near("closed-loop-parity", a, x, REL, absTol < 0 ? ABS : absTol, name + "." + w);

            Assert.AreEqual(e.Str("outcome"), r.outcome, "outcome");
            N(r.time, e["time"], "time"); N(r.maxQ, e["maxQ"], "maxQ"); N(r.maxQTime, e["maxQTime"], "maxQTime"); N(r.maxG, e["maxG"], "maxG");
            var L = e.Obj("losses"); N(r.lossGravity, L["gravity"], "loss.g"); N(r.lossDrag, L["drag"], "loss.d"); N(r.lossSteering, L["steering"], "loss.s");
            N(r.idealDeltaV, e["idealDeltaV"], "idealDV"); N(r.deltaVExpended, e["deltaVExpended"], "dvExp"); N(r.finalMass, e["finalMass"], "finalMass");
            var st = e.Obj("state"); N(r.x, st["x"], "x", 1e-3); N(r.y, st["y"], "y", 1e-3); N(r.vx, st["vx"], "vx"); N(r.vy, st["vy"], "vy");
            var ob = e.Obj("orbit"); N(r.orbit.a, ob["a"], "orbit.a", 1e-3); N(r.orbit.e, ob["e"], "orbit.e"); N(r.orbit.periAlt, ob["periAlt"], "orbit.peri", 1e-3); N(r.orbit.apoAlt, ob["apoAlt"], "orbit.apo", 1e-3);

            var ev = e.Arr("events");
            Assert.AreEqual(ev.Count, r.events.Count, "event count: " + string.Join(", ", r.events));
            for (int i = 0; i < ev.Count; i++)
            {
                var x = (JsonObject)ev[i]; var a = r.events[i];
                Assert.AreEqual(x.Str("type"), a.type, $"event {i} type");
                if (x.ContainsKey("stage")) Assert.AreEqual((int)x.Num("stage"), a.stage, $"event {i} stage");
                N(a.t, x["t"], $"event{i}.t");
                if (x.ContainsKey("alt")) N(a.alt.Value, x["alt"], $"event{i}.alt", 1e-3);
            }
            var sm = e.Arr("samples");
            Assert.AreEqual(sm.Count, r.samples.Count, "sample count");
            for (int i = 0; i < sm.Count; i++)
            {
                var x = (JsonObject)sm[i]; var a = r.samples[i];
                N(a.t, x["t"], $"s{i}.t"); N(a.alt, x["alt"], $"s{i}.alt", 1e-3); N(a.v, x["v"], $"s{i}.v"); N(a.vrel, x["vrel"], $"s{i}.vrel");
                N(a.q, x["q"], $"s{i}.q", 1e-6); N(a.m, x["m"], $"s{i}.m"); N(a.pitch, x["pitch"], $"s{i}.pitch");
                Assert.AreEqual((int)x.Num("stage"), a.stage); Assert.AreEqual((int)x.Num("mode"), a.mode);
            }
        }
    }
}
