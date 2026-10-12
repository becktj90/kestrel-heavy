using System.Collections.Generic;
using Kestrel.Core.Economy;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class EconomyGoldenTests
    {
        [Test]
        public void CostModel_And_Reliability_MatchJs()
        {
            var c = Golden.Catalog; double relTol = Golden.File("economy").Obj("tolerance").Num("relReliability"); int n = 0;
            foreach (var g in Golden.Cases("economy"))
            {
                string fn = g.Str("fn");
                switch (fn)
                {
                    case "priceDesign":
                    {
                        var d = Design.FromJson(g.Obj("design")); CompanyState cs = null;
                        if (g.Obj("companyState") is JsonObject s)
                        {
                            cs = new CompanyState();
                            foreach (var k in s.Obj("unitsBuilt").Order) cs.unitsBuilt[k] = (int)s.Obj("unitsBuilt").Num(k);
                            foreach (var x in s.Arr("devDone")) cs.devDone.Add((string)x);
                        }
                        var q = CostModel.PriceDesign(c, d, cs); var e = g.Obj("out"); string w = g.Str("preset") + (cs != null ? "+state" : "");
                        Assert.AreEqual((long)e.Num("recurring"), q.recurring, w + " recurring");
                        Assert.AreEqual((long)e.Num("dev"), q.dev, w + " dev");
                        Assert.AreEqual((long)e.Num("total"), q.total, w + " total");
                        Assert.AreEqual(e["perKg"] == null ? (long?)null : (long)e.Num("perKg"), q.perKg, w + " perKg");
                        var lines = e.Arr("lines"); Assert.AreEqual(lines.Count, q.lines.Count, w + " lines");
                        for (int i = 0; i < lines.Count; i++)
                        {
                            var l = (JsonObject)lines[i];
                            Assert.AreEqual(l.Str("cat"), q.lines[i].cat); Assert.AreEqual(l.Str("label"), q.lines[i].label); Assert.AreEqual(l.Str("group"), q.lines[i].group);
                            Assert.AreEqual((long)l.Num("amount"), q.lines[i].amount, $"{w} line {l.Str("label")}");
                        }
                        var dl = e.Arr("devLines"); Assert.AreEqual(dl.Count, q.devLines.Count, w + " devLines");
                        for (int i = 0; i < dl.Count; i++) Assert.AreEqual((long)((JsonObject)dl[i]).Num("amount"), q.devLines[i].amount, w + " " + q.devLines[i].label);
                        foreach (var k in CostModel.CATEGORIES) Assert.AreEqual((long)e.Obj("byCategory").Num(k), q.byCategory[k], w + " " + k);
                        break;
                    }
                    case "reliabilityOf":
                    {
                        var r = Reliability.Of(c, Design.FromJson(g.Obj("design"))); var e = g.Obj("out");
                        Golden.Near("economy", r.reliability, e["reliability"], relTol, 0, g.Str("preset") + " reliability");
                        var es = e.Arr("stages"); Assert.AreEqual(es.Count, r.stages.Count);
                        for (int i = 0; i < es.Count; i++) { var x = (JsonObject)es[i]; Assert.AreEqual((int)x.Num("tolerance"), r.stages[i].tolerance); Golden.Near("economy", r.stages[i].reliability, x["reliability"], relTol, 0, "stage rel"); }
                        break;
                    }
                    case "engineDevCost": Assert.AreEqual((long)g.Num("out"), CostModel.EngineDevCost(c, c.EngineById[g.Str("engine")]), g.Str("engine")); break;
                    case "engineBatchCost": Assert.AreEqual((long)g.Num("out"), CostModel.EngineBatchCost(c, c.EngineById[g.Str("engine")], (int)g.Num("count"), (int)g.Num("built")), g.Str("engine")); break;
                    case "learningFactor": Golden.Near("economy", CostModel.LearningFactor(c, g.Num("n")), g["out"], relTol, 0, "learning"); break;
                    case "clusterReliability": Golden.Near("economy", Reliability.ClusterReliability(c, (int)g.Num("n"), g.Num("r"), (int)g.Num("k")), g["out"], relTol, 0, "cluster"); break;
                    default: Assert.Fail("unknown fn " + fn); break;
                }
                n++;
            }
            Assert.That(n, Is.GreaterThanOrEqualTo(30));
        }

        [Test]
        public void GoldenIndex_CountsAllVectors()
        {
            var idx = Kestrel.Core.Json.MiniJson.ParseObject(System.IO.File.ReadAllText(System.IO.Path.Combine(Golden.Root, "data", "golden", "index.json")));
            int total = 0; foreach (var f in idx.Obj("files").Order) total += Golden.File(f).Arr("cases").Count;
            Assert.AreEqual((int)idx.Num("total"), total);
        }
    }
}
