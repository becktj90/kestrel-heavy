using System.Collections.Generic;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DesignerGoldenTests
    {
        [Test]
        public void Validation_Readout_MakeItFly_AndLayers_MatchJs()
        {
            var file = Golden.File("designer-validate");
            double rel = file.Obj("tolerance").Num("rel"), abs = file.Obj("tolerance").Num("abs");
            double relR = file.Obj("tolerance").Num("relReliability", 1e-12);
            var cat = Golden.Catalog;
            foreach (JsonObject c in file.Arr("cases"))
            {
                string kind = c.Str("kind"), name = c.Str("name");
                if (kind == "toFixed")
                {
                    foreach (JsonObject row in (List<object>)c["out"])
                    {
                        double n = row.Num("n");
                        Assert.AreEqual(row.Str("d0"), JsFormat.ToFixed(n, 0), name + " d0");
                        Assert.AreEqual(row.Str("d1"), JsFormat.ToFixed(n, 1), name + " d1");
                        Assert.AreEqual(row.Str("d2"), JsFormat.ToFixed(n, 2), name + " d2");
                    }
                    continue;
                }
                if (kind == "resolve")
                {
                    var opts = c.Obj("opts");
                    var got = Experience.Resolve((int)opts.Num("orbitsCompleted"), opts.Bool("engineerEnabled"), opts.Str("layer"));
                    var exp = c.Obj("out");
                    Assert.AreEqual(exp.Str("layer"), got.layer, name);
                    Assert.AreEqual(exp.Str("locked"), got.locked, name + " locked");
                    var avail = Experience.Available((int)opts.Num("orbitsCompleted"), opts.Bool("engineerEnabled"));
                    var want = c.Arr("available");
                    Assert.AreEqual(want.Count, avail.Length, name);
                    for (int i = 0; i < want.Count; i++) Assert.AreEqual((string)want[i], avail[i], name);
                    continue;
                }
                if (kind == "present")
                {
                    var report = ReportFrom(c.Obj("report"));
                    var got = Experience.Present(report, c.Str("layer"));
                    var exp = c.Obj("out");
                    Assert.AreEqual(exp.Bool("ok"), got.ok, name);
                    Assert.AreEqual(exp.Str("next"), got.next, name);
                    Assert.AreEqual(exp.Bool("showTune"), got.showTune, name);
                    Assert.AreEqual(exp.Bool("showSchemaText"), got.showSchemaText, name);
                    Assert.AreEqual(exp.Bool("makeItFly"), got.makeItFly, name);
                    SameIssues(exp.Arr("blocks"), got.blocks, name + " blocks");
                    SameIssues(exp.Arr("warnings"), got.warnings, name + " warnings");
                    continue;
                }
                var design = c.Obj("design");
                if (kind == "validate")
                {
                    var before = JsonMut.CloneObject(design);
                    var got = Validate.Engineering(cat, design);
                    JsonSame.Equal(design, before, name + " mutated");
                    var exp = c.Obj("out");
                    Assert.AreEqual(exp.Bool("ok"), got.ok, name);
                    SameIssues(exp.Arr("blocks"), got.blocks, name + " blocks");
                    SameIssues(exp.Arr("warnings"), got.warnings, name + " warnings");
                    SameReadout(name, got.readout, exp.Obj("readout"), rel, abs, relR);
                }
                else if (kind == "makeItFly")
                {
                    var before = JsonMut.CloneObject(design);
                    var (fly, changes) = Validate.MakeItFly(cat, design);
                    JsonSame.Equal(design, before, name + " mutated input");
                    var exp = c.Obj("out");
                    var wantChanges = exp.Arr("changes");
                    Assert.AreEqual(wantChanges.Count, changes.Count, name + " change count: " + string.Join(" | ", changes));
                    for (int i = 0; i < wantChanges.Count; i++) Assert.AreEqual((string)wantChanges[i], changes[i], name + " change " + i);
                    JsonSame.Equal(fly, exp["design"], name + " design");
                }
            }
        }

        static EngineeringReport ReportFrom(JsonObject o)
        {
            var r = new EngineeringReport { ok = o.Bool("ok") };
            r.blocks = Issues(o.Arr("blocks"));
            r.warnings = Issues(o.Arr("warnings"));
            return r;
        }

        static List<Issue> Issues(List<object> arr)
        {
            var o = new List<Issue>();
            if (arr == null) return o;
            foreach (JsonObject x in arr)
                o.Add(new Issue { code = x.Str("code"), tab = x.Str("tab"), msg = x.Str("msg"), index = x.ContainsKey("index") ? (int)x.Num("index") : (int?)null });
            return o;
        }

        static void SameIssues(List<object> exp, List<Issue> got, string what)
        {
            Assert.AreEqual(exp.Count, got.Count, what);
            for (int i = 0; i < exp.Count; i++)
            {
                var e = (JsonObject)exp[i]; var a = got[i];
                Assert.AreEqual(e.Str("code"), a.code, what + " code");
                Assert.AreEqual(e.Str("tab"), a.tab, what + " tab");
                Assert.AreEqual(e.Str("msg"), a.msg, what + " msg");
                if (e.ContainsKey("index")) Assert.AreEqual((int)e.Num("index"), a.index, what + " index");
                else Assert.IsNull(a.index, what + " index");
            }
        }

        static void SameReadout(string name, QuickReadout got, JsonObject exp, double rel, double abs, double relR)
        {
            if (exp == null) { Assert.IsNull(got, name); return; }
            Assert.IsNotNull(got, name);
            void N(double a, object e, string w) => Golden.Near("designer-validate", a, e, rel, abs, name + "." + w);
            N(got.liftoffTWR, exp["liftoffTWR"], "twr");
            N(got.totalDeltaV, exp["totalDeltaV"], "dv");
            N(got.margin, exp["margin"], "margin");
            N(got.required, exp["required"], "req");
            N(got.targetAlt, exp["targetAlt"], "alt");
            N(got.liftoffMass, exp["liftoffMass"], "m0");
            N(got.mass.fairing, exp["fairingMass"], "fairing");
            N(got.mass.payload, exp["payloadMass"], "payload");
            Assert.AreEqual((long)exp.Num("cost"), got.cost, name + " cost");
            Golden.Near("designer-validate", got.reliability, exp["reliability"], relR, 0, name + ".rel");
            Assert.AreEqual(exp.Str("orbitLabel"), got.orbitLabel, name);
            var stages = exp.Arr("stages");
            Assert.AreEqual(stages.Count, got.stages.Count, name + " stages");
            for (int i = 0; i < stages.Count; i++)
            {
                var e = (JsonObject)stages[i]; var a = got.stages[i];
                Assert.AreEqual(e.Str("label"), a.label, name);
                Assert.AreEqual(e.Str("role"), a.role, name);
                N(a.twr, e["twr"], "st" + i + ".twr");
                N(a.twrVac, e["twrVac"], "st" + i + ".twrVac");
                N(a.deltaV, e["deltaV"], "st" + i + ".dv");
            }
        }
    }
}
