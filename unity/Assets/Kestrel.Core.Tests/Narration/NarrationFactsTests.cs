// Narration wiring: which facts a flown design has, which clips are true for them, and what the picker may play.
// The manifest it reads is the one the game ships. The oracle below re-reads the raw design JSON and never calls
// NarrationFacts, so a bug in the facts builder cannot hide behind itself.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Kestrel.Core.Flight.Narration;
using Kestrel.Core.Json;
using Kestrel.Core.Vehicle;
using NUnit.Framework;

namespace Kestrel.Core.Tests.Narration
{
    public class NarrationFactsTests
    {
        static readonly string[] Sites = { null, "pier-7", "coastal-pad-b", "salt-flat", "north-reach" };

        static string ManifestPath => Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Game", "Audio", "Runtime", "Resources", "Narration", "manifest.json");

        static JsonObject manifestRoot;
        static List<NarrationTake> takes;
        static Dictionary<string, string> texts;

        static void LoadManifest()
        {
            if (takes != null) return;
            manifestRoot = MiniJson.ParseObject(File.ReadAllText(ManifestPath));
            takes = new List<NarrationTake>();
            texts = new Dictionary<string, string>();
            foreach (JsonObject c in manifestRoot.Arr("clips"))
            {
                takes.Add(NarrationTake.FromJson(c));
                texts[c.Str("id")] = c.Str("text") ?? "";
            }
        }

        // ---- the evaluator ----------------------------------------------------------------------------

        [Test]
        public void Holds_ExactComparisonAndAnyOf()
        {
            Assert.IsTrue(NarrationRequires.Holds(7, 7.0));
            Assert.IsFalse(NarrationRequires.Holds(7, 6.0));
            Assert.IsTrue(NarrationRequires.Holds("kerolox", "kerolox"));
            Assert.IsFalse(NarrationRequires.Holds("kerolox", "methalox"));
            Assert.IsTrue(NarrationRequires.Holds(true, true));
            Assert.IsFalse(NarrationRequires.Holds(false, true));
            Assert.IsTrue(NarrationRequires.Holds(7, ">=2"));
            Assert.IsTrue(NarrationRequires.Holds(2, ">=2"));
            Assert.IsFalse(NarrationRequires.Holds(1, ">=2"));
            Assert.IsTrue(NarrationRequires.Holds(1, "<=1"));
            Assert.IsTrue(NarrationRequires.Holds(0, "<1"));
            Assert.IsFalse(NarrationRequires.Holds(1, "<1"));
            Assert.IsTrue(NarrationRequires.Holds(3, ">2"));
            Assert.IsFalse(NarrationRequires.Holds(2, ">2"));
            Assert.IsTrue(NarrationRequires.Holds(3, "!=2"));
            Assert.IsFalse(NarrationRequires.Holds(2, "!=2"));
            Assert.IsTrue(NarrationRequires.Holds("barge", new List<object> { "barge", "rtls" }));
            Assert.IsFalse(NarrationRequires.Holds("none", new List<object> { "barge", "rtls" }));
        }

        [Test]
        public void Holds_AMissingOrWrongTypedFactIsFalse()
        {
            Assert.IsFalse(NarrationRequires.Holds(null, 7));
            Assert.IsFalse(NarrationRequires.Holds(null, ">=2"));
            Assert.IsFalse(NarrationRequires.Holds(null, "!=2"), "missing is false even for !=");
            Assert.IsFalse(NarrationRequires.Holds("kerolox", ">=2"), "a comparison needs a number");
            Assert.IsFalse(NarrationRequires.Holds(3, ">=two"), "a malformed comparison is not a value");
            Assert.IsFalse(NarrationRequires.Holds(3, "\u22652"));
            Assert.IsFalse(NarrationRequires.Holds(true, ">=1"));
            Assert.IsFalse(NarrationRequires.Holds(null, new List<object> { "barge", "rtls" }));
        }

        [Test]
        public void Eligible_UnknownKeyUntaggedAndNullFactsFailClosed()
        {
            var f = NarrationFacts.From(Design.FromJson(StarterDesign("m3")), "pier-7");
            var neutral = new NarrationTake { Id = "n", Neutral = true, Tagged = true };
            var untagged = new NarrationTake { Id = "u" };
            var good = new NarrationTake { Id = "g", Tagged = true, Claims = 1 };
            good.Requires.Add(new NarrationRequirement("stages", ">=2"));
            var typo = new NarrationTake { Id = "t", Tagged = true, Claims = 1 };
            typo.Requires.Add(new NarrationRequirement("stage10.engines", 1.0));
            var bogus = new NarrationTake { Id = "b", Tagged = true, Claims = 1 };
            bogus.Requires.Add(new NarrationRequirement("windspeed", 3.0));
            Assert.IsTrue(NarrationRequires.Eligible(neutral, f));
            Assert.IsTrue(NarrationRequires.Eligible(neutral, null));
            Assert.IsFalse(NarrationRequires.Eligible(untagged, f));
            Assert.IsTrue(NarrationRequires.Eligible(good, f));
            Assert.IsFalse(NarrationRequires.Eligible(good, null));
            Assert.IsFalse(NarrationRequires.Eligible(typo, f));
            Assert.IsFalse(NarrationRequires.Eligible(bogus, f));
        }

        [Test]
        public void Picker_OrdersByClaimsThenManifestOrder_AndNullFactsOnlyGetNeutral()
        {
            var a = Tagged("a", 1); var b = Tagged("b", 3); var c = Tagged("c", 3); var n = new NarrationTake { Id = "n", Neutral = true, Tagged = true };
            var list = new List<NarrationTake> { a, n, b, c };
            var f = NarrationFacts.From(Design.FromJson(StarterDesign("m3")), null);
            var order = NarrationPicker.Order(list, f);
            CollectionAssert.AreEqual(new[] { 2, 3, 0, 1 }, order, "claims desc, ties in manifest order, neutral last");
            CollectionAssert.AreEqual(new[] { 1 }, NarrationPicker.Order(list, null), "null facts: neutral only");
            Assert.AreEqual(-1, NarrationPicker.Pick(new List<NarrationTake> { a }, null, 0));
            Assert.AreEqual(2, NarrationPicker.Pick(list, f, 0));
            Assert.AreEqual(3, NarrationPicker.Pick(list, f, 1));
            Assert.AreEqual(2, NarrationPicker.Pick(list, f, 4), "the cursor wraps");
        }

        static NarrationTake Tagged(string id, int claims)
        {
            var t = new NarrationTake { Id = id, Tagged = true, Claims = claims };
            t.Requires.Add(new NarrationRequirement("stages", ">=1"));
            return t;
        }

        // ---- the facts of a real design ---------------------------------------------------------------

        [Test]
        public void EveryShippedDesign_FactsMatchTheRawJson_OnEverySite()
        {
            int n = 0;
            foreach (var (label, raw) in AllDesigns())
            {
                var design = Design.FromJson(raw);
                foreach (var site in Sites)
                {
                    var facts = NarrationFacts.From(design, site);
                    var oracle = Oracle(raw, site);
                    foreach (var kv in oracle)
                    {
                        Assert.IsTrue(facts.TryGet(kv.Key, out var got), label + " " + kv.Key + " missing");
                        Assert.AreEqual(kv.Value, got, label + " " + kv.Key);
                    }
                    if (site == null) Assert.IsFalse(facts.TryGet("site", out _), label + ": no site means no site fact");
                    Assert.IsFalse(facts.TryGet("fairing.halves", out _), "fairing.halves stays missing until the sim reports halves");
                    Assert.IsFalse(facts.TryGet("lit.stage", out _), "lit.* only exists on a lit cue");
                    n++;
                }
            }
            Assert.GreaterOrEqual(n, 200, "the matrix is not empty");
        }

        [Test]
        public void EveryShippedDesign_EveryEligibleTakeIsTrue_ByTheOracle()
        {
            LoadManifest();
            int checkedTakes = 0;
            foreach (var (label, raw) in AllDesigns())
            {
                var design = Design.FromJson(raw);
                int stages = raw.Arr("stages").Count;
                foreach (var site in Sites)
                {
                    var facts = NarrationFacts.From(design, site);
                    var oracle = Oracle(raw, site);
                    for (int lit = 0; lit <= stages; lit++)
                    {
                        var ctx = lit == 0 ? facts : facts.WithLit(lit);
                        var truth = new Dictionary<string, object>(oracle);
                        if (lit > 0)
                        {
                            truth["lit.stage"] = lit;
                            truth["lit.engines"] = truth["stage" + lit + ".engines"];
                            if (truth.TryGetValue("stage" + lit + ".propellant", out var p)) truth["lit.propellant"] = p;
                            truth["lit.last"] = lit == stages;
                        }
                        foreach (var take in takes)
                        {
                            bool got = NarrationRequires.Eligible(take, ctx);
                            bool want = take.Neutral || (take.Tagged && take.Requires.All(r => OracleHolds(truth, r.Key, r.Want)));
                            Assert.AreEqual(want, got, label + " site " + site + " lit " + lit + " " + take.Id);
                            if (got) checkedTakes++;
                        }
                    }
                }
            }
            Assert.Greater(checkedTakes, 1000);
        }

        [Test]
        public void NoClipThatNeedsFairingHalvesIsEverEligible()
        {
            LoadManifest();
            foreach (var (label, raw) in AllDesigns())
            {
                var facts = NarrationFacts.From(Design.FromJson(raw), "pier-7");
                foreach (var take in takes.Where(t => t.Requires.Any(r => r.Key == "fairing.halves")))
                    Assert.IsFalse(NarrationRequires.Eligible(take, facts), label + " " + take.Id);
            }
        }

        [Test]
        public void KH3_NeverHearsSevenEngines()
        {
            LoadManifest();
            var raw = StarterDesign("m3");
            int engines = (int)raw.Arr("stages").Cast<JsonObject>().First().Num("engineCount");
            Assert.AreNotEqual(7, engines, "the regression needs a rocket that does not have seven");
            var design = Design.FromJson(raw);
            int stages = raw.Arr("stages").Count;
            foreach (var site in Sites)
                for (int lit = 0; lit <= stages; lit++)
                {
                    var facts = NarrationFacts.From(design, site);
                    var ctx = lit == 0 ? facts : facts.WithLit(lit);
                    foreach (var take in takes)
                        if (NarrationRequires.Eligible(take, ctx))
                            Assert.IsFalse(Regex.IsMatch(texts[take.Id], @"\bseven\b(?! point)", RegexOptions.IgnoreCase), take.Id + ": " + texts[take.Id]);
                }
        }

        [Test]
        public void KH1AndKH2_SingleStage_NeverHearAStagingMeco()
        {
            LoadManifest();
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "golden", "campaign-winners.json")));
            int seen = 0;
            foreach (JsonObject m in file.Arr("missions"))
            {
                if (m.Str("id") != "m1" && m.Str("id") != "m2") continue;
                var facts = NarrationFacts.From(Design.FromJson((JsonObject)m.Arr("designs")[0]), "pier-7");
                Assert.AreEqual(1, facts.Stages);
                foreach (var take in takes.Where(t => t.Event == "meco" || t.Event == "seco" || t.Event == "ses1"))
                    if (NarrationRequires.Eligible(take, facts.WithLit(take.Event == "meco" ? 1 : 0)))
                        Assert.IsTrue(take.Neutral, m.Str("id") + " " + take.Id + " talks about stages the rocket does not have");
                seen++;
            }
            Assert.AreEqual(2, seen);
        }

        [Test]
        public void ADesignWithNoLandingNeverHearsTheBoosterComingHome()
        {
            LoadManifest();
            foreach (var (label, raw) in AllDesigns())
            {
                string mode = raw.Obj("recovery")?.Str("mode");
                if (mode == "barge" || mode == "rtls") continue;
                var facts = NarrationFacts.From(Design.FromJson(raw), "pier-7");
                Assert.AreEqual("none", facts.Recovery, label);
                foreach (var take in takes.Where(t => t.Requires.Any(r => r.Key == "recovery")))
                    foreach (var r in take.Requires.Where(r => r.Key == "recovery"))
                        if (NarrationRequires.Eligible(take, facts))
                            Assert.IsTrue(NarrationRequires.Holds("none", r.Want), label + " " + take.Id + " claims a landing the rocket does not have");
            }
        }

        [Test]
        public void ThePickedRecoveryIsTheRecoveryFact()
        {
            var raw = StarterDesign("m3");
            foreach (var mode in new[] { "barge", "rtls", "expendable", "bogus", null })
            {
                var copy = Clone(raw);
                if (mode == null) copy.Remove("recovery"); else copy["recovery"] = MiniJson.ParseObject("{\"mode\":\"" + mode + "\"}");
                string want = mode == "barge" || mode == "rtls" ? mode : "none";
                Assert.AreEqual(want, NarrationFacts.From(Design.FromJson(copy), "pier-7").Recovery, mode ?? "null");
            }
        }

        [Test]
        public void LitStageFactsFollowTheCoreStage()
        {
            var f = NarrationFacts.From(Design.FromJson(StarterDesign("m10")), "pier-7");
            int stages = f.Stages;
            Assert.GreaterOrEqual(stages, 2);
            var lit2 = f.WithLit(2);
            Assert.IsTrue(lit2.TryGet("lit.stage", out var s)); Assert.AreEqual(2, s);
            Assert.IsTrue(lit2.TryGet("lit.engines", out var e)); Assert.AreEqual(f.EngineCount(2), e);
            Assert.IsTrue(lit2.TryGet("lit.last", out var last)); Assert.AreEqual(stages == 2, last);
            Assert.IsFalse(f.WithLit(0).TryGet("lit.stage", out _));
            Assert.IsFalse(f.WithLit(stages + 1).TryGet("lit.stage", out _), "a stage the design does not have is not lit");
            Assert.IsFalse(f.TryGet("lit.stage", out _), "WithLit does not change the original");
        }

        [Test]
        public void TheStepperReportCarriesWhetherTheDesignIsFinless()
        {
            foreach (var fins in new[] { true, false })
            {
                var raw = Clone(StarterDesign("m3"));
                raw["aero"] = MiniJson.ParseObject("{\"nose\":\"ogive\",\"fins\":" + (fins ? "true" : "false") + "}");
                var report = Kestrel.Core.Designer.Stepper.StepperModel.Evaluate(Golden.Catalog, raw);
                Assert.AreEqual(!fins, report.Finless);
            }
            Assert.IsNull(Kestrel.Core.Designer.Stepper.StepperModel.Evaluate(Golden.Catalog, null).Finless);
        }

        [Test]
        public void FinlessIsTheOppositeOfFins()
        {
            foreach (var (label, raw) in AllDesigns())
            {
                bool fins = raw.Obj("aero") != null && raw.Obj("aero").Bool("fins");
                var f = NarrationFacts.From(Design.FromJson(raw), null);
                Assert.AreEqual(fins, f.Fins, label);
                Assert.AreEqual(!fins, f.Finless, label);
            }
        }

        // ---- the independent oracle -------------------------------------------------------------------

        static string Prop(string p)
        {
            switch (p)
            {
                case "kerolox": case "hydrolox": case "methalox": return p;
                case "solid-apcp": return "solid";
                case "mmh-nto": return "storable";
                default: return null;
            }
        }

        static Dictionary<string, object> Oracle(JsonObject raw, string site)
        {
            var o = new Dictionary<string, object>();
            var stages = raw.Arr("stages").Cast<JsonObject>().ToList();
            o["stages"] = stages.Count;
            var all = new List<string>();
            for (int i = 0; i < stages.Count && i < 9; i++)
            {
                int engines = (int)Math.Max(1, stages[i].Num("engineCount"));
                o["stage" + (i + 1) + ".engines"] = engines;
                string p = Prop(stages[i].Str("propellant"));
                if (p != null) o["stage" + (i + 1) + ".propellant"] = p;
            }
            foreach (var s in stages) all.Add(Prop(s.Str("propellant")));
            var so = raw.Obj("strapOns");
            int straps = so != null ? (int)so.Num("count") : 0;
            o["strapons"] = straps;
            o["strapons.engines"] = straps > 0 ? (int)Math.Max(1, so.Num("engineCount")) : 0;
            string sp = straps > 0 ? Prop(so.Str("propellant")) : null;
            if (straps > 0 && sp != null) o["strapons.propellant"] = sp;
            if (straps > 0) all.Add(sp ?? all[0]);
            if (all.All(x => x != null)) o["propellant.all"] = all.All(x => x == all[0]) ? all[0] : "mixed";
            var aero = raw.Obj("aero");
            bool fins = aero != null && aero.Bool("fins");
            o["fins"] = fins;
            o["finless"] = !fins;
            var landing = raw.Obj("landing");
            o["legs"] = landing != null && landing.Bool("legs");
            string mode = raw.Obj("recovery")?.Str("mode");
            o["recovery"] = mode == "barge" || mode == "rtls" ? mode : "none";
            if (!string.IsNullOrEmpty(site)) o["site"] = site;
            return o;
        }

        static bool OracleHolds(Dictionary<string, object> truth, string key, object want)
        {
            if (key == "fairing" || key == "fairing.halves") return false; // not derived by the oracle: an eligible take that needs it fails the match on purpose
            if (!truth.TryGetValue(key, out var have)) return false;
            if (want is List<object> any) return any.Any(w => OracleHolds(truth, key, w));
            if (want is string cmp && Regex.IsMatch(cmp, @"^(>=|<=|!=|>|<)-?\d+$"))
            {
                if (!(have is int h)) return false;
                int v = int.Parse(Regex.Match(cmp, @"-?\d+$").Value);
                string op = Regex.Match(cmp, @"^[<>=!]+").Value;
                return op == ">=" ? h >= v : op == "<=" ? h <= v : op == "!=" ? h != v : op == ">" ? h > v : h < v;
            }
            if (want is double d) want = (int)d;
            return Equals(have, want);
        }

        static JsonObject Clone(JsonObject o)
        {
            var c = new JsonObject();
            foreach (var k in o.Order) c.Add(k, CloneValue(o[k]));
            return c;
        }

        static object CloneValue(object v)
        {
            if (v is JsonObject o) return Clone(o);
            if (v is List<object> a) { var l = new List<object>(); foreach (var x in a) l.Add(CloneValue(x)); return l; }
            return v;
        }

        // ---- design sources ---------------------------------------------------------------------------

        static JsonObject StarterDesign(string missionId)
        {
            var file = MiniJson.ParseObject(File.ReadAllText(Path.Combine(Golden.Root, "data", "board-starters.json")));
            foreach (JsonObject m in file.Arr("missions"))
                if (m.Str("id") == missionId) return (JsonObject)m.Arr("designs")[0];
            throw new InvalidOperationException("no starter " + missionId);
        }

        static List<(string, JsonObject)> allDesigns;

        static List<(string, JsonObject)> AllDesigns()
        {
            if (allDesigns != null) return allDesigns;
            var list = new List<(string, JsonObject)>();
            string data = Path.Combine(Golden.Root, "data");
            void FromMissions(string file, string label)
            {
                var root = MiniJson.ParseObject(File.ReadAllText(Path.Combine(data, file)));
                foreach (JsonObject m in root.Arr("missions"))
                {
                    int i = 0;
                    foreach (JsonObject d in m.Arr("designs")) list.Add((label + ":" + m.Str("id") + "#" + i++, d));
                }
            }
            FromMissions("board-starters.json", "starter");
            FromMissions(Path.Combine("golden", "campaign-viable.json"), "viable");
            FromMissions("campaign-winners.json", "winner");
            var contracts = MiniJson.ParseObject(File.ReadAllText(Path.Combine(data, "golden", "contracts-viable.json")));
            foreach (JsonObject cell in contracts.Arr("cells"))
            {
                int i = 0;
                foreach (JsonObject d in cell.Arr("designs")) list.Add(("contract:" + cell.Str("id") + "#" + i++, d));
            }
            var templates = MiniJson.ParseObject(File.ReadAllText(Path.Combine(data, "designer-templates.json")));
            foreach (JsonObject t in templates.Arr("templates")) list.Add(("template:" + t.Str("id"), t.Obj("design")));
            foreach (var f in Directory.GetFiles(Path.Combine(Golden.Root, "tests", "fixtures", "designs"), "*.json"))
                list.Add(("fixture:" + Path.GetFileName(f), MiniJson.ParseObject(File.ReadAllText(f))));
            // The hopper: one stage, no strap-ons, no landing. And a finned and a finless copy with each landing.
            var hopper = Clone(StarterDesign("m3"));
            var one = new List<object>(); one.Add(hopper.Arr("stages")[0]);
            hopper["stages"] = one;
            list.Add(("hopper", hopper));
            foreach (var mode in new[] { "barge", "rtls" })
                foreach (var fins in new[] { true, false })
                {
                    var v = Clone(StarterDesign("m3"));
                    v["recovery"] = MiniJson.ParseObject("{\"mode\":\"" + mode + "\"}");
                    v["aero"] = MiniJson.ParseObject("{\"nose\":\"ogive\",\"fins\":" + (fins ? "true" : "false") + "}");
                    list.Add(("variant:" + mode + (fins ? "+fins" : "-fins"), v));
                }
            allDesigns = list;
            return list;
        }
    }
}
