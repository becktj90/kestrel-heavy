using System.Collections.Generic;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class SimulateGoldenTests
    {
        static IEnumerable<TestCaseData> Cases()
        {
            foreach (var c in Golden.Cases("simulate")) yield return new TestCaseData(c.Str("name")).SetName("Simulate_" + c.Str("name"));
        }

        [TestCaseSource(nameof(Cases))]
        public void Simulate_MatchesJs(string name)
        {
            var tol = Golden.File("simulate").Obj("tolerance"); double REL = tol.Num("rel"), ABS = tol.Num("abs");
            JsonObject g = null; foreach (var c in Golden.Cases("simulate")) if (c.Str("name") == name) g = c;
            var inp = g.Obj("in"); var oo = inp.Obj("opts");
            var stages = new List<SimStage>();
            foreach (JsonObject s in inp.Arr("stages"))
                stages.Add(new SimStage { thrustSL = s.Num("thrustSL"), thrustVac = s.Num("thrustVac"), ispSL = s.Num("ispSL"), ispVac = s.Num("ispVac"), propMass = s.Num("propMass"), dryMass = s.Num("dryMass"), diameter = s.Num("diameter"), cd = s.Num("cd", 0.3) });
            var opts = new SimOptions
            {
                payload = oo.Num("payload", 0), targetAlt = oo.Num("targetAlt", 200e3), latitude = oo.Num("latitude", 28.5), dt = oo.Num("dt", 0.05),
                tMax = oo.Num("tMax", 3000), sampleEvery = oo.Num("sampleEvery", 0), vacuum = oo.Bool("vacuum", false),
                rotate = !oo.ContainsKey("rotate") || oo.Bool("rotate"), cutoff = !oo.ContainsKey("cutoff") || oo.Bool("cutoff"),
            };
            var r = Headless.Simulate(stages, opts); var e = g.Obj("out");
            void N(double a, object x, string w, double absTol = -1) => Golden.Near("simulate", a, x, REL, absTol < 0 ? ABS : absTol, name + "." + w);

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
