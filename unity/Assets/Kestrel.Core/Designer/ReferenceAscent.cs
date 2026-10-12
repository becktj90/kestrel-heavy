// Cached Headless reference ascent for designer max-Q.
// Quick() never runs this. A miss leaves maxQStale true.
// Cost: Stack.SimInputs → the same RK4 step as Headless (dt 0.2 s, default steering, no sample buffer).
// The run stops once MaxQSealed is set, unless a dense descent clears that stop (AscentSim.DenseDescent).
// Headless.Simulate itself always runs to thrust cutoff and ignores the flag, so simulate goldens do not move.
// It does not fly an unpowered re-entry: the integrator returns when thrust ends.
// Budget: ≤ 30 ms off the edit frame, measured by `kestrel readout-bench`.
// The UI waits ReferenceDebounce.DebounceMs after the last edit, then calls Readout.Reference
// off the main thread, or ReferenceJob.Step with a ≤ 2 ms budget.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    public static class ReferenceAscent
    {
        public const double FrameBudgetMs = 2;
        public const double TotalBudgetMs = 30;

        struct Entry { public string key; public double qPa, altM; }
        static readonly Dictionary<ulong, Entry> Cache = new Dictionary<ulong, Entry>();
        static readonly object Gate = new object();

        public static void Reset()
        {
            lock (Gate) Cache.Clear();
        }

        /// <summary>Catalog version plus canonical design JSON. The cache stores this and compares it on a hash hit.</summary>
        public static string CanonicalKey(Catalog c, JsonObject design)
            => "cat=" + (c != null && c.VersionKey != null ? c.VersionKey : "0") + "\n" + JsonWriter.Stringify(Canon(design));

        /// <summary>FNV-1a 64-bit of <see cref="CanonicalKey"/>, 16 lowercase hex digits.</summary>
        public static string DesignHash(Catalog c, JsonObject design) => Fnv1a64(CanonicalKey(c, design)).ToString("x16");

        public static ulong Fnv1a64(string s)
        {
            ulong h = 14695981039346656037UL;
            if (s == null) return h;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 1099511628211UL;
            }
            return h;
        }

        public static bool TryGet(string key, out double qPa, out double altM)
        {
            lock (Gate)
            {
                if (key != null && Cache.TryGetValue(Fnv1a64(key), out var e) && e.key == key)
                {
                    qPa = e.qPa; altM = e.altM; return true;
                }
            }
            qPa = 0; altM = 0; return false;
        }

        public static void Store(string key, double qPa, double altM)
        {
            if (key == null) return;
            lock (Gate) Cache[Fnv1a64(key)] = new Entry { key = key, qPa = qPa, altM = altM };
        }

        public static QuickReadout Run(Catalog c, JsonObject design)
        {
            string key = CanonicalKey(c, design);
            if (!TryGet(key, out _, out _))
            {
                var sim = Simulate(c, design);
                Store(key, sim.maxQ, sim.maxQAlt);
            }
            return Readout.Quick(c, design);
        }

        public static SimResult Simulate(Catalog c, JsonObject design)
        {
            var d = Design.FromJson(design);
            var input = Stack.SimInputs(c, d);
            return Fly(input.stages, input.opts);
        }

        /// <summary>Same steps as Headless.Simulate, returning early only while the peak is sealed and no dense descent has reopened it.</summary>
        public static SimResult Fly(System.Collections.Generic.IList<SimStage> stages, SimOptions opts)
        {
            var sim = new AscentSim(stages, opts);
            while (sim.Tick())
                if (sim.MaxQSealed && !sim.DenseDescent) break;
            return sim.Finish();
        }

        static object Canon(object v)
        {
            if (v is JsonObject o)
            {
                var keys = new List<string>(o.Order);
                keys.Sort(System.StringComparer.Ordinal);
                var n = new JsonObject();
                foreach (var k in keys) n.Add(k, Canon(o[k]));
                return n;
            }
            if (v is List<object> a)
            {
                var n = new List<object>(a.Count);
                foreach (var x in a) n.Add(Canon(x));
                return n;
            }
            return v;
        }
    }

    /// <summary>300 ms after the last committed edit, matching web SIM_DEBOUNCE_MS. The clock is injected so tests do not sleep.</summary>
    public sealed class ReferenceDebounce
    {
        public const int DebounceMs = 300;
        string hash;
        long editMs;
        bool pending;

        public void NoteEdit(string designHash, long nowMs)
        {
            hash = designHash;
            editMs = nowMs;
            pending = true;
        }

        public bool Due(long nowMs, out string designHash)
        {
            designHash = null;
            if (!pending || nowMs - editMs < DebounceMs) return false;
            designHash = hash;
            pending = false;
            return true;
        }
    }

    /// <summary>One reference ascent, advanced in wall-time slices. Step(2) stays inside a frame; Step until true matches Readout.Reference.</summary>
    public sealed class ReferenceJob
    {
        readonly string key;
        readonly AscentSim sim;
        bool done;
        public bool Done => done;

        public ReferenceJob(Catalog c, JsonObject design)
        {
            key = ReferenceAscent.CanonicalKey(c, design);
            var d = Design.FromJson(design);
            var input = Stack.SimInputs(c, d);
            sim = new AscentSim(input.stages, input.opts);
        }

        /// <summary>Run ticks until the ascent ends or <paramref name="budgetMs"/> of wall time has elapsed. At least one tick runs when work remains, so a 0 budget still advances.</summary>
        public bool Step(double budgetMs)
        {
            if (done) return true;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                if (!sim.Tick() || (sim.MaxQSealed && !sim.DenseDescent))
                {
                    var r = sim.Finish();
                    ReferenceAscent.Store(key, r.maxQ, r.maxQAlt);
                    done = true;
                    return true;
                }
                if (sw.Elapsed.TotalMilliseconds >= budgetMs) return false;
            }
        }
    }
}
