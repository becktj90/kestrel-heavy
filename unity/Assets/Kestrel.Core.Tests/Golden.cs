// Golden-vector harness: loads data/golden/*.json (exported from the JS reference by
// tools/export-golden.mjs) and compares with |a-b| <= abs + rel*|b|. Tracks the worst error.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Json;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public static class Golden
    {
        static string root; static Catalog catalog;
        public static string Root => root ??= Catalog.FindRepoRoot(StartDir());
        public static Catalog Catalog => catalog ??= Catalog.LoadFromRepo(Root);

        static string StartDir()
        {
            // Unity: cwd = project folder (unity/). dotnet test: bin/<cfg>/<tfm>/. Both sit under the repo.
            var env = Environment.GetEnvironmentVariable("KESTREL_REPO_ROOT");
            return !string.IsNullOrEmpty(env) ? env : (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "..", "data")) ? Directory.GetCurrentDirectory() : AppContext.BaseDirectory);
        }

        /// <summary>
        /// #314: the old one-solid-motor sounding template, kept as a named test fixture so the solid code paths
        /// (burnout without MECO, no strongback, unguided max-Q exemption) stay covered. Not shipped: the shipped
        /// sounding template is tpl-sounding-hybrid. This solid breaks up over 20 g at T+0.6 s under the played checks.
        /// </summary>
        public const string SolidSoundingFixture = "sounding-solid.json";
        public static JsonObject FixtureJson(string file) => MiniJson.ParseObject(System.IO.File.ReadAllText(Path.Combine(Root, "tests", "fixtures", "designs", file)));
        public static Kestrel.Core.Vehicle.Design Fixture(string file) => Kestrel.Core.Vehicle.Design.FromJson(FixtureJson(file));

        /// <summary>A named design fixture from tests/fixtures/designs (test-only rockets, not shipped presets).</summary>
        public static JsonObject FixtureDesign(string name) => MiniJson.ParseObject(System.IO.File.ReadAllText(Path.Combine(Root, "tests", "fixtures", "designs", name + ".json")));
        public static JsonObject File(string name) => MiniJson.ParseObject(System.IO.File.ReadAllText(Path.Combine(Root, "data", "golden", name + ".json")));
        public static IEnumerable<JsonObject> Cases(string name) { foreach (JsonObject c in File(name).Arr("cases")) yield return c; }

        /// <summary>Worst relative error seen per golden file (for the report).</summary>
        public static readonly Dictionary<string, double> MaxRel = new Dictionary<string, double>();
        public static int Compared;

        public static double Num(object o) => o is double d ? d : o is string s ? (s == "Infinity" ? double.PositiveInfinity : s == "-Infinity" ? double.NegativeInfinity : double.NaN) : throw new ArgumentException("not a number: " + o);

        public static void Near(string file, double actual, object expected, double rel, double abs, string what)
        {
            double e = Num(expected);
            Compared++;
            if (double.IsInfinity(e) || double.IsNaN(e)) { Assert.AreEqual(e, actual, what); return; }
            double err = Math.Abs(actual - e), r = err / Math.Max(Math.Abs(e), 1e-300);
            if (err != 0) { MaxRel.TryGetValue(file, out var m); if (r > m) MaxRel[file] = r; }
            if (!(err <= abs + rel * Math.Abs(e))) Assert.Fail($"{what}: got {actual:R}, expected {e:R} (err {err:E3}, rel {r:E3})");
        }
    }
}
