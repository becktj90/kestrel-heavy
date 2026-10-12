// Wall-clock budget for the designer instant path and the off-frame reference ascent.
// Warm up first: the numbers are steady-state, not cold JIT. `kestrel readout-bench` prints the same result.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Designer
{
    public static class ReadoutBench
    {
        public const double QuickP95BudgetMs = 0.5;
        public const double ValidateP95BudgetMs = 0.5;
        public const double ReferenceBudgetMs = ReferenceAscent.TotalBudgetMs;

        public sealed class Result
        {
            public int designs;
            public double quickP95Ms, validateP95Ms, referenceMaxMs;
            public string quickWorst, validateWorst, referenceWorst;
            public bool ok;
        }

        public static List<JsonObject> LoadDesigns(string repoRoot)
        {
            var list = new List<JsonObject>();
            var presets = MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", "presets.json")));
            foreach (JsonObject p in presets.Arr("presets")) list.Add(p);
            string dir = Path.Combine(repoRoot, "web", "js", "data", "templates");
            var files = new List<string>(Directory.GetFiles(dir, "*.json"));
            files.Sort(StringComparer.Ordinal);
            foreach (var f in files) list.Add(MiniJson.ParseObject(File.ReadAllText(f)));
            return list;
        }

        public static Result Measure(Catalog c, IList<JsonObject> designs, int samples = 60)
        {
            if (designs == null || designs.Count == 0) throw new ArgumentException("no designs");
            for (int i = 0; i < 3; i++)
                foreach (var d in designs) { Readout.Quick(c, d); Validate.Engineering(c, d); }
            Headless.Simulate(Headless.MediumLifter(), new SimOptions { dt = 0.2, tMax = 5 });
            GC.Collect();
            GC.WaitForPendingFinalizers();

            double quickWorst = 0, valWorst = 0;
            string quickName = "", valName = "";
            foreach (var d in designs)
            {
                var q = new double[samples];
                var v = new double[samples];
                for (int i = 0; i < samples; i++)
                {
                    long t0 = Stopwatch.GetTimestamp();
                    Readout.Quick(c, d);
                    long t1 = Stopwatch.GetTimestamp();
                    Validate.Engineering(c, d);
                    long t2 = Stopwatch.GetTimestamp();
                    q[i] = Ms(t0, t1);
                    v[i] = Ms(t1, t2);
                }
                double qp = Percentile(q, 0.95), vp = Percentile(v, 0.95);
                if (qp > quickWorst) { quickWorst = qp; quickName = NameOf(d); }
                if (vp > valWorst) { valWorst = vp; valName = NameOf(d); }
            }

            ReferenceAscent.Reset();
            double refWorst = 0; string refName = "";
            foreach (var d in designs)
            {
                long t0 = Stopwatch.GetTimestamp();
                Readout.Reference(c, d);
                double ms = Ms(t0, Stopwatch.GetTimestamp());
                if (ms > refWorst) { refWorst = ms; refName = NameOf(d); }
            }

            return new Result
            {
                designs = designs.Count,
                quickP95Ms = quickWorst, validateP95Ms = valWorst, referenceMaxMs = refWorst,
                quickWorst = quickName, validateWorst = valName, referenceWorst = refName,
                ok = quickWorst < QuickP95BudgetMs && valWorst < ValidateP95BudgetMs && refWorst < ReferenceBudgetMs,
            };
        }

        static string NameOf(JsonObject d) => d.Str("id") ?? d.Str("name") ?? "?";

        static double Ms(long a, long b) => (b - a) * 1000.0 / Stopwatch.Frequency;

        static double Percentile(double[] xs, double p)
        {
            Array.Sort(xs);
            int index = (int)Math.Ceiling(p * xs.Length) - 1;
            if (index < 0) index = 0;
            if (index >= xs.Length) index = xs.Length - 1;
            return xs[index];
        }
    }
}
