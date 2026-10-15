// Copy catalog for the guided designer (work package S3).
// data/designer-stepper.json is the only player-facing wording. Ids are stable.
// This checks that every DesignerStep and every check code StepperModel can raise has both depths.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Kestrel.Core.Designer;
using Kestrel.Core.Designer.Stepper;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class DesignerStepperCopyTests
    {
        static readonly string[] Depths = { "simple", "engineer" };

        // Gates StepperChecks actually raises. Failure kinds always carry pass, amber, and red.
        static readonly Dictionary<string, string[]> Gates = new Dictionary<string, string[]>
        {
            ["payload-missing"] = new[] { "pass", "red" },
            ["orbit-missing"] = new[] { "pass", "red" },
            ["math"] = new[] { "pass", "red" },
            ["twr-low"] = new[] { "pass", "red" },
            ["twr-marginal"] = new[] { "pass", "amber" },
            ["zero-engines"] = new[] { "pass", "red" },
            ["engines-dont-fit"] = new[] { "pass", "red" },
            ["vac-engine-pad"] = new[] { "pass", "red" },
            ["cluster-count"] = new[] { "pass", "red" },
            ["cluster-overhang"] = new[] { "pass", "amber" },
            ["nozzle-unknown"] = new[] { "pass", "red" },
            ["weak-upper"] = new[] { "pass", "amber" },
            ["zero-propellant"] = new[] { "pass", "red" },
            ["dv-margin"] = new[] { "pass", "amber", "red" },
            ["maxq-high"] = new[] { "pass", "amber" },
            ["weak-upper-dv"] = new[] { "pass", "amber" },
            ["cryo-composite"] = new[] { "pass", "amber" },
            ["schema"] = new[] { "pass", "red" },
            ["top-heavy"] = new[] { "pass", "red" },
            ["interstage-shape"] = new[] { "pass", "amber" },
            ["fairing-narrow"] = new[] { "pass", "red" },
            ["payload-doesnt-fit"] = new[] { "pass", "red" },
            ["heavy-fairing"] = new[] { "pass", "amber" },
            ["unstable"] = new[] { "pass", "amber", "red" },
            ["overstable"] = new[] { "pass", "amber" },
            ["payload-g"] = new[] { "pass", "amber", "red" },
            ["landing-required"] = new[] { "pass", "red" },
            ["landing-need-stale"] = new[] { "pass", "amber" },
            ["landing-reserve"] = new[] { "pass", "red" },
            ["landing-hardware"] = new[] { "pass", "amber" },
            ["rcs-for-landing"] = new[] { "pass", "amber" },
            ["recovery-skip"] = new[] { "skip" },
            ["risks-stale"] = new[] { "pass" },
            ["red-risk"] = new[] { "pass", "amber" },
        };

        static readonly Dictionary<string, string[]> WhyMustCite = new Dictionary<string, string[]>
        {
            ["mission"] = new[] { "Δv", "kg" },
            ["engines"] = new[] { "9.80665", "1.3", "TWR" },
            ["tanks"] = new[] { "q_lim", "35 kPa" },
            ["aero"] = new[] { "D_f", "CAR" },
            ["recovery"] = new[] { "v_h", "m/s" },
            ["look"] = new[] { "Δv", "q_lim" },
            ["review"] = new[] { "q_lim" },
        };

        static readonly Regex IdPattern = new Regex("^[a-z][a-z0-9-]*(\\.[a-z0-9-]+)+$", RegexOptions.CultureInvariant);
        static readonly Regex Jargon = new Regex(
            @"(\bTWR\b|Δv|\bIsp\b|\bCAR\b|q_lim|\bg0\b|\bMECO\b|\bRTLS\b|\bkPa\b|\bcalibers?\b|delta-?v|\bMach\b|\bgimbal\b|\bfairing\b|\bpropellant\b|\bavionics\b|\bnozzle\b|\bthrottle\b|\bhydrolox\b|\binterstage\b|\bbulkhead\b|max-?Q|\bmixture\b|\bchamber\b|\bexpansion\b|dynamic pressure|safety factor|\bsigma\b|\bstability\b|\bm/s\b|π|σ_|\bpayload\b|\bmaxG\b|\bD_f\b|\bv_h\b|\bv_leg\b|\bv_z\b)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static readonly Regex Cites = new Regex(
            @"(TWR|Δv|q_lim|g0|9\.80665|m/s|kPa|CAR|D_f|σ|π|pc|Isp|v_h|v_leg|v_z|λ|α|°|m³|kg\b|%|ε|8 g|maxG|Cd0|cryoOK|maxTempK|maxHeatFlux|F_SL|v_rel|½ρ)",
            RegexOptions.CultureInvariant);
        static readonly Regex Trademark = new Regex(
            @"\b(SpaceX|Falcon|Starship|Starlink|NASA|Soyuz|Ariane|Raptor|Merlin|Blue Origin|Electron|Vulcan|Atlas|Saturn|Apollo|Artemis|SLS)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        [SetUp]
        public void FreshAscentCache() => ReferenceAscent.Reset();

        [Test]
        public void EveryStepId_HasGoalWhyAndPass_ForSimpleAndEngineer()
        {
            var doc = Load();
            var steps = doc.Obj("steps");
            Assert.IsNotNull(steps);
            var ids = new List<string>();
            var seen = new HashSet<string>();
            foreach (DesignerStep step in Enum.GetValues(typeof(DesignerStep)))
            {
                string id = step.ToString().ToLowerInvariant();
                seen.Add(id);
                var row = steps.Obj(id);
                Assert.IsNotNull(row, id);
                Assert.AreEqual(id, row.Str("id"));
                Assert.AreEqual((int)step, row.Num("number"), 0);
                Assert.AreEqual(step.ToString(), row.Str("enum"));
                Assert.AreEqual(id, StepperCatalog.Get(step).Step.ToString().ToLowerInvariant());
                foreach (var depth in Depths)
                {
                    var voice = row.Obj(depth);
                    Assert.IsNotNull(voice, id + " " + depth);
                    Leaf(voice.Obj("goal"), "step." + id + "." + depth + ".goal", ids, depth == "simple", depth == "engineer");
                    Leaf(voice.Obj("pass"), "step." + id + "." + depth + ".pass", ids, depth == "simple", depth == "engineer");
                    var why = voice.Obj("why");
                    Leaf(why.Obj("kid"), "step." + id + "." + depth + ".why.kid", ids, true, false);
                    Leaf(why.Obj("engineer"), "step." + id + "." + depth + ".why.engineer", ids, false, true);
                    foreach (var needle in WhyMustCite[id])
                        StringAssert.Contains(needle, why.Obj("engineer").Str("text"), id + " " + depth);
                }
                Assert.AreNotEqual(row.Obj("simple").Obj("goal").Str("text"), row.Obj("engineer").Obj("goal").Str("text"), id);
                Assert.AreNotEqual(row.Obj("simple").Obj("pass").Str("text"), row.Obj("engineer").Obj("pass").Str("text"), id);
                Assert.AreNotEqual(row.Obj("simple").Obj("why").Obj("kid").Str("text"), row.Obj("engineer").Obj("why").Obj("kid").Str("text"), id);
                Assert.AreNotEqual(row.Obj("simple").Obj("why").Obj("engineer").Str("text"), row.Obj("engineer").Obj("why").Obj("engineer").Str("text"), id);
            }
            StringAssert.Contains("seed", steps.Obj("review").Obj("engineer").Obj("why").Obj("engineer").Str("text"));
            StringAssert.Contains("0.6%", steps.Obj("engines").Obj("engineer").Obj("why").Obj("engineer").Str("text"));
            StringAssert.Contains("λ", steps.Obj("engines").Obj("engineer").Obj("why").Obj("engineer").Str("text"));
            StringAssert.Contains("SF/1.4", steps.Obj("tanks").Obj("engineer").Obj("why").Obj("engineer").Str("text"));
            StringAssert.Contains("α = 4°", steps.Obj("aero").Obj("engineer").Obj("why").Obj("engineer").Str("text"));
            StringAssert.Contains("20 kPa", steps.Obj("aero").Obj("engineer").Obj("why").Obj("engineer").Str("text"));
            StringAssert.Contains("TWR_land", steps.Obj("recovery").Obj("engineer").Obj("why").Obj("engineer").Str("text"));
            CollectionAssert.AreEquivalent(seen, steps.Keys);
            AssertUnique(ids);
        }

        [Test]
        public void EveryCheckCodeFromStepperModel_HasCopy()
        {
            var doc = Load();
            var checks = doc.Obj("checks");
            var failures = FailureKinds();
            var discovered = DiscoverCodes(failures);
            var ids = new List<string>();
            var expectedSteps = FailureSteps();

            CollectionAssert.AreEquivalent(discovered, checks.Keys, "the catalog must match the codes StepperModel can raise");
            var mapped = new HashSet<string>(Gates.Keys);
            foreach (var code in discovered)
            {
                if (failures.Contains(code) || code.StartsWith("risk-", StringComparison.Ordinal)) continue;
                Assert.IsTrue(mapped.Contains(code), "add the gates for " + code);
            }
            foreach (var code in mapped)
                Assert.IsTrue(discovered.Contains(code), "gate map has a code the stepper does not raise: " + code);

            foreach (var code in discovered)
            {
                var row = checks.Obj(code);
                Assert.IsNotNull(row, code);
                if (code.StartsWith("risk-", StringComparison.Ordinal))
                {
                    string kind = code.Substring("risk-".Length);
                    Assert.AreEqual(kind, row.Str("sameAs"), code);
                    Assert.IsTrue(failures.Contains(kind), code);
                    Assert.IsNull(row.Obj("simple"), "alias rows point at the failure kind");
                    continue;
                }

                string[] gates = failures.Contains(code)
                    ? new[] { "pass", "amber", "red" }
                    : Gates[code];
                CollectionAssert.AreEquivalent(gates, Strings(row, "gates"), code);
                if (failures.Contains(code))
                {
                    Assert.IsTrue(row.Bool("failure"), code);
                    CollectionAssert.AreEquivalent(expectedSteps[code], Strings(row, "steps"), code);
                }
                else
                    Assert.IsFalse(row.Bool("failure"), code);

                string focus = StepperChecks.FieldFor(code);
                if (focus != null) Assert.AreEqual(focus, row.Str("field"), code);
                else Assert.IsFalse(string.IsNullOrEmpty(row.Str("field")), code);

                foreach (var depth in Depths)
                {
                    var voice = row.Obj(depth);
                    Assert.IsNotNull(voice, code + " " + depth);
                    var want = new HashSet<string>(gates) { "fix" };
                    CollectionAssert.AreEquivalent(want, voice.Keys, code + " " + depth);
                    foreach (var slot in want)
                        Leaf(voice.Obj(slot), "check." + code + "." + depth + "." + slot, ids, depth == "simple", depth == "engineer");
                    foreach (var slot in gates)
                        Assert.AreNotEqual(row.Obj("simple").Obj(slot).Str("text"), row.Obj("engineer").Obj(slot).Str("text"), code + " " + slot);
                    Assert.AreNotEqual(row.Obj("simple").Obj("fix").Str("text"), row.Obj("engineer").Obj("fix").Str("text"), code);
                }
            }
            AssertUnique(ids);
        }

        [Test]
        public void EvaluatedDesigns_UseOnlyCodesThatHaveCopy()
        {
            var known = Load().Obj("checks");
            var report = StepperModel.Evaluate(Golden.Catalog, HangarFixtures.Template("workhorse"));
            for (int n = 1; n <= StepperCatalog.Count; n++)
            {
                var check = report[(DesignerStep)n];
                if (!string.IsNullOrEmpty(check.Code))
                    Assert.IsNotNull(known.Obj(check.Code), check.Code);
                for (int i = 0; i < check.Findings.Count; i++)
                    if (!string.IsNullOrEmpty(check.Findings[i].Code))
                        Assert.IsNotNull(known.Obj(check.Findings[i].Code), check.Findings[i].Code);
            }
        }

        static JsonObject Load()
        {
            string path = Path.Combine(Golden.Root, "data", "designer-stepper.json");
            var doc = MiniJson.ParseObject(File.ReadAllText(path));
            Assert.AreEqual("designer_stepper_copy_v1", doc.Str("schema"));
            Assert.AreEqual(1, doc.Num("version"), 0);
            Assert.AreEqual("en", doc.Str("locale"));
            CollectionAssert.AreEqual(Depths, Strings(doc, "depths"));
            return doc;
        }

        static void Leaf(JsonObject node, string expectedId, List<string> ids, bool kid, bool cite)
        {
            Assert.IsNotNull(node, expectedId);
            Assert.AreEqual(expectedId, node.Str("id"), expectedId);
            Assert.IsTrue(IdPattern.IsMatch(expectedId), expectedId);
            string text = node.Str("text");
            Assert.IsFalse(string.IsNullOrWhiteSpace(text), expectedId);
            Assert.IsFalse(Trademark.IsMatch(text), expectedId);
            if (kid) AssertKid(expectedId, text);
            if (cite) Assert.IsTrue(Cites.IsMatch(text), expectedId + " needs a unit or formula: " + text);
            ids.Add(node.Str("id"));
        }

        static void AssertKid(string id, string text)
        {
            var sentences = Sentences(text);
            Assert.LessOrEqual(sentences.Count, 2, id + ": " + text);
            Assert.GreaterOrEqual(sentences.Count, 1, id);
            foreach (var sentence in sentences)
                Assert.LessOrEqual(sentence.Length, 120, id + ": " + sentence);
            Assert.IsFalse(Jargon.IsMatch(text), id + " has jargon: " + text);
        }

        static List<string> Sentences(string text)
        {
            var parts = Regex.Split(text.Trim(), "[.!?]+");
            var kept = new List<string>();
            foreach (var part in parts)
            {
                string s = part.Trim();
                if (s.Length > 0) kept.Add(s);
            }
            return kept;
        }

        static void AssertUnique(List<string> ids)
        {
            var seen = new HashSet<string>();
            foreach (var id in ids)
                Assert.IsTrue(seen.Add(id), "duplicate id " + id);
        }

        static HashSet<string> FailureKinds()
        {
            var set = new HashSet<string>();
            foreach (var def in StepperCatalog.All)
                foreach (var kind in def.FailureKinds)
                    if (!string.IsNullOrEmpty(kind)) set.Add(kind);
            return set;
        }

        static Dictionary<string, HashSet<string>> FailureSteps()
        {
            var map = new Dictionary<string, HashSet<string>>();
            foreach (var def in StepperCatalog.All)
            {
                string id = def.Step.ToString().ToLowerInvariant();
                foreach (var kind in def.FailureKinds)
                {
                    if (!map.TryGetValue(kind, out var steps))
                    {
                        steps = new HashSet<string>();
                        map[kind] = steps;
                    }
                    steps.Add(id);
                }
            }
            return map;
        }

        static HashSet<string> DiscoverCodes(HashSet<string> failures)
        {
            string checks = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Core", "Designer", "Stepper", "StepperChecks.cs"));
            string rules = File.ReadAllText(Path.Combine(Golden.Root, "unity", "Assets", "Kestrel.Core", "Designer", "DesignRules.cs"));
            var codes = new HashSet<string>();
            AddMatches(codes, checks, @"Set\(\s*\w+\s*,\s*StepGate\.\w+\s*,\s*""([a-z0-9-]+)""");
            AddMatches(codes, checks, @"Finding\(\s*""([a-z0-9-]+)""");
            AddMatches(codes, checks, @"(?<![A-Za-z])(?:code|Code)\s*(?<![=])=(?!=)\s*(?:[^;\n]{0,200}?)""([a-z0-9-]+)""");
            CollectCases(codes, FirstSwitch(checks, "StepForAdvisory"));
            CollectCases(codes, FirstSwitch(checks, "static string FieldFor"));
            foreach (var kind in failures) codes.Add(kind);
            if (rules.Contains("\"risk-\" +", StringComparison.Ordinal))
                foreach (var kind in failures) codes.Add("risk-" + kind);
            return codes;
        }

        static void CollectCases(HashSet<string> codes, string switchBody)
        {
            var skip = new HashSet<string>();
            foreach (Match group in Regex.Matches(switchBody, @"((?:case\s+""[a-z0-9-]+""\s*:\s*)+)return\s+null"))
                foreach (Match label in Regex.Matches(group.Groups[1].Value, @"""([a-z0-9-]+)"""))
                    skip.Add(label.Groups[1].Value);
            foreach (Match label in Regex.Matches(switchBody, @"case\s+""([a-z0-9-]+)"""))
                if (!skip.Contains(label.Groups[1].Value)) codes.Add(label.Groups[1].Value);
        }

        static void AddMatches(HashSet<string> codes, string source, string pattern)
        {
            foreach (Match match in Regex.Matches(source, pattern))
                codes.Add(match.Groups[1].Value);
        }

        static string FirstSwitch(string source, string methodName)
        {
            int start = source.IndexOf(methodName, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, methodName);
            int sw = source.IndexOf("switch", start, StringComparison.Ordinal);
            int open = source.IndexOf('{', sw);
            int depth = 0;
            for (int i = open; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0) return source.Substring(open, i - open + 1);
                }
            }
            Assert.Fail("unclosed switch in " + methodName);
            return "";
        }

        static List<string> Strings(JsonObject obj, string key)
        {
            var list = new List<string>();
            foreach (var item in obj.Arr(key)) list.Add((string)item);
            return list;
        }
    }
}
