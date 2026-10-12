// Parity against data/golden/catalog-flight.json (every engine and preset).
// Bands are the docs/PHYSICS.md golden-tolerances block.
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class CatalogFlightTests
    {
        static readonly Dictionary<string, (double abs, double rel)> Bands = new Dictionary<string, (double, double)>
        {
            ["time"] = (1e-6, 1e-6), ["t"] = (1e-6, 1e-6),
            ["maxQTime"] = (0.2, 0),
            ["finalAlt"] = (1e-2, 1e-6), ["alt"] = (1e-2, 1e-6), ["apoAlt"] = (1e-2, 1e-6), ["periAlt"] = (1e-2, 1e-6),
            ["x"] = (1e-2, 1e-6), ["y"] = (1e-2, 1e-6),
            ["finalV"] = (1e-4, 1e-6), ["v"] = (1e-4, 1e-6), ["vrel"] = (1e-4, 1e-6), ["vx"] = (1e-4, 1e-6), ["vy"] = (1e-4, 1e-6),
            ["finalMass"] = (1e-3, 1e-6), ["m"] = (1e-3, 1e-6),
            ["gravity"] = (1e-4, 1e-6), ["drag"] = (1e-4, 1e-6), ["steering"] = (1e-4, 1e-6), ["dragLoss"] = (1e-4, 1e-6),
            ["deltaVExpended"] = (1e-3, 1e-6),
            ["idealDeltaV"] = (1e-9, 1e-9),
            ["maxG"] = (1e-6, 1e-6),
            ["maxQ"] = (1e-3, 1e-6),
            ["dragForce"] = (1e-9, 1e-9),
            ["dynamicPressure"] = (1e-9, 1e-9),
            ["cd"] = (1e-12, 1e-12),
            ["a"] = (1e-2, 1e-6), ["periapsis"] = (1e-2, 1e-6), ["apoapsis"] = (1e-2, 1e-6),
            ["e"] = (1e-6, 1e-6), ["i"] = (1e-6, 1e-6),
            ["energy"] = (1e-2, 1e-6), ["h"] = (1e-2, 1e-6), ["period"] = (1e-4, 1e-6),
        };
        const double Closed = 1e-9;

        [Test]
        public void EveryEngineAndPreset_MatchesCatalogFlight()
        {
            var doc = Golden.File("catalog-flight");
            Assert.AreEqual(136, (int)doc.Num("count"));
            var cases = new List<JsonObject>();
            foreach (JsonObject c in doc.Arr("cases")) cases.Add(c);
            var order = new List<string>();
            var stages = new Dictionary<string, SimStage[]>();
            var opts = new Dictionary<string, SimOptions>();
            foreach (var c in cases)
            {
                if (c.Str("kind") != "ascent") continue;
                string key = c.Obj("subject").Str("type") + ":" + c.Obj("subject").Str("id");
                order.Add(key);
                stages[key] = Stages(c.Obj("in").Arr("stages"));
                opts[key] = Opts(c.Obj("in").Obj("opts"));
            }
            Assert.AreEqual(34, order.Count, "one sim per engine and preset");
            var stageList = new List<SimStage[]>();
            var optList = new List<SimOptions>();
            foreach (var key in order) { stageList.Add(stages[key]); optList.Add(opts[key]); }
            var flown = FlightBatch.Simulate(stageList, optList);
            var byKey = new Dictionary<string, SimResult>();
            for (int i = 0; i < order.Count; i++) byKey[order[i]] = flown[i];

            foreach (var c in cases)
            {
                string key = c.Obj("subject").Str("type") + ":" + c.Obj("subject").Str("id");
                string what = key + " " + c.Str("kind");
                var r = byKey[key];
                var o = c.Obj("out");
                if (c.Str("kind") == "ascent") Ascent(what, r, o);
                else if (c.Str("kind") == "staging") Staging(what, r, o);
                else if (c.Str("kind") == "drag") Drag(what, r, stages[key], c.Obj("in"), o);
                else if (c.Str("kind") == "orbit") CompareOrbit(what, r, c.Obj("in"), o);
                else Assert.Fail("kind " + c.Str("kind"));
            }
        }

        static void Ascent(string what, SimResult r, JsonObject o)
        {
            Assert.AreEqual(o.Str("outcome"), r.outcome, what);
            N(what, "time", r.time, o["time"]);
            N(what, "finalAlt", JsMath.Hypot(r.x, r.y) - Gravity.R_EARTH, o["finalAlt"]);
            N(what, "finalV", JsMath.Hypot(r.vx, r.vy), o["finalV"]);
            N(what, "deltaVExpended", r.deltaVExpended, o["deltaVExpended"]);
            N(what, "idealDeltaV", r.idealDeltaV, o["idealDeltaV"]);
            N(what, "finalMass", r.finalMass, o["finalMass"]);
            N(what, "maxG", r.maxG, o["maxG"]);
            var L = o.Obj("losses");
            N(what, "gravity", r.lossGravity, L["gravity"]);
            N(what, "drag", r.lossDrag, L["drag"]);
            N(what, "steering", r.lossSteering, L["steering"]);
            var st = o.Obj("state");
            N(what, "x", r.x, st["x"]); N(what, "y", r.y, st["y"]);
            N(what, "vx", r.vx, st["vx"]); N(what, "vy", r.vy, st["vy"]); N(what, "m", r.m, st["m"]);
        }

        static void Staging(string what, SimResult r, JsonObject o)
        {
            var ev = o.Arr("events");
            Assert.AreEqual(ev.Count, r.events.Count, what);
            for (int i = 0; i < ev.Count; i++)
            {
                var e = (JsonObject)ev[i]; var a = r.events[i];
                Assert.AreEqual(e.Str("type"), a.type, what + " type");
                if (e.ContainsKey("stage")) Assert.AreEqual((int)e.Num("stage"), a.stage.Value, what + " stage");
                N(what, "t", a.t, e["t"]);
                if (e.ContainsKey("alt")) N(what, "alt", a.alt.Value, e["alt"]);
                if (e.ContainsKey("v")) N(what, "v", a.v.Value, e["v"]);
                if (e.ContainsKey("apoAlt")) N(what, "apoAlt", a.apoAlt.Value, e["apoAlt"]);
                if (e.ContainsKey("periAlt")) N(what, "periAlt", a.periAlt.Value, e["periAlt"]);
            }
        }

        static void Drag(string what, SimResult r, SimStage[] stages, JsonObject inp, JsonObject o)
        {
            N(what, "maxQ", r.maxQ, o["maxQ"]);
            N(what, "maxQTime", r.maxQTime, o["maxQTime"]);
            N(what, "dragLoss", r.lossDrag, o["dragLoss"]);
            var sample = CatalogDrag.Sample(r.samples, stages);
            var es = o.Obj("sample");
            N(what, "t", sample.t, es["t"]);
            N(what, "alt", sample.alt, es["alt"]);
            N(what, "vrel", sample.vrel, es["vrel"]);
            Assert.AreEqual((int)es.Num("stage"), sample.stage, what + " sample stage");
            N(what, "dragForce", sample.dragForce, es["dragForce"]);
            N(what, "dynamicPressure", sample.dynamicPressure, es["dynamicPressure"]);
            N(what, "cd", sample.cd, es["cd"]);
            Peak(what, CatalogDrag.Peak(stages[0].cd, Aero.RefArea(stages[0].diameter)), o.Obj("machPeak"));
            if (o.ContainsKey("nozzleMachPeak"))
                Peak(what + " nozzle", CatalogDrag.Peak(stages[0].cd, Aero.RefArea(inp.Num("nozzleDiameter"))), o.Obj("nozzleMachPeak"));
        }

        static void Peak(string what, MachPeak got, JsonObject exp)
        {
            Assert.AreEqual(exp.Num("gridAltitudeM"), got.gridAltitudeM, what + " grid");
            Assert.AreEqual(exp.Num("mach"), got.mach, what + " mach");
            N(what, "dragForce", got.dragForce, exp["dragForce"]);
            N(what, "dynamicPressure", got.dynamicPressure, exp["dynamicPressure"]);
            N(what, "cd", got.cd, exp["cd"]);
        }

        static void CompareOrbit(string what, SimResult r, JsonObject inp, JsonObject o)
        {
            var integrated = Orbit.Elements(r.x, r.y, 0, r.vx, r.vy, 0);
            Elem(what, integrated, o, false);
            var rv = inp.Arr("r"); var vv = inp.Arr("v");
            var closed = Orbit.Elements((double)rv[0], (double)rv[1], (double)rv[2], (double)vv[0], (double)vv[1], (double)vv[2]);
            Elem(what + " closed", closed, o, true);
        }

        static void Elem(string what, OrbitElements e, JsonObject o, bool closed)
        {
            void One(string key, double actual)
            {
                if (closed) Golden.Near("catalog-flight", actual, o[key], Closed, Closed, what + "." + key);
                else N(what, key, actual, o[key]);
            }
            One("a", e.a); One("e", e.e); One("i", e.i); One("energy", e.energy); One("h", e.h);
            One("periapsis", e.periapsis); One("apoapsis", e.apoapsis); One("periAlt", e.periAlt); One("apoAlt", e.apoAlt); One("period", e.period);
        }

        static void N(string what, string key, double actual, object expected)
        {
            var b = Bands[key];
            Golden.Near("catalog-flight", actual, expected, b.rel, b.abs, what + "." + key);
        }

        static SimStage[] Stages(List<object> arr)
        {
            var o = new SimStage[arr.Count];
            for (int i = 0; i < arr.Count; i++)
            {
                var s = (JsonObject)arr[i];
                o[i] = new SimStage { thrustSL = s.Num("thrustSL"), thrustVac = s.Num("thrustVac"), ispSL = s.Num("ispSL"), ispVac = s.Num("ispVac"), propMass = s.Num("propMass"), dryMass = s.Num("dryMass"), diameter = s.Num("diameter"), cd = s.Num("cd", 0.3) };
            }
            return o;
        }

        static SimOptions Opts(JsonObject o) => new SimOptions
        {
            payload = o.Num("payload"), targetAlt = o.Num("targetAlt", 200e3), latitude = o.Num("latitude", 28.5),
            dt = o.Num("dt", 0.05), tMax = o.Num("tMax", 3000), sampleEvery = o.Num("sampleEvery"),
            vacuum = o.Bool("vacuum"), rotate = !o.ContainsKey("rotate") || o.Bool("rotate"), cutoff = !o.ContainsKey("cutoff") || o.Bool("cutoff"),
        };
    }
}
