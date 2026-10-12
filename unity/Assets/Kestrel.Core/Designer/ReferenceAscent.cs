// Cached Headless reference ascent for designer max-Q.
// Quick() never runs this. A miss leaves maxQStale true.
// Cost: Stack.SimInputs → the same RK4 step as Headless (dt 0.2 s, default steering, no sample buffer),
// stopped once AscentSim.MaxQSealed is set. That peak matches a full Headless.Simulate on every shipped design.
// Budget: ≤ 30 ms off the edit frame, measured by `kestrel readout-bench`.
// The UI waits ReferenceDebounce.DebounceMs after the last edit, then calls Readout.Reference
// off the main thread, or ReferenceJob.Step with a ≤ 2 ms budget.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Replay;
using Kestrel.Core.Save;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    public static class ReferenceAscent
    {
        public const double FrameBudgetMs = 2;
        public const double TotalBudgetMs = 30;

        struct Entry { public double qPa, altM; }
        static readonly Dictionary<string, Entry> Cache = new Dictionary<string, Entry>();
        static readonly object Gate = new object();

        public static void Reset()
        {
            lock (Gate) Cache.Clear();
        }

        public static bool TryGet(string hash, out double qPa, out double altM)
        {
            lock (Gate)
            {
                if (hash != null && Cache.TryGetValue(hash, out var e)) { qPa = e.qPa; altM = e.altM; return true; }
            }
            qPa = 0; altM = 0; return false;
        }

        public static void Store(string hash, double qPa, double altM)
        {
            lock (Gate) Cache[hash] = new Entry { qPa = qPa, altM = altM };
        }

        /// <summary>FNV-1a 32-bit of canonical design JSON (keys sorted at every object). Same mixer as D0Replay.Fnv1a.</summary>
        public static string DesignHash(JsonObject design) => D0Replay.Fnv1a(JsonWriter.Stringify(Canon(design)));

        public static QuickReadout Run(Catalog c, JsonObject design)
        {
            string hash = DesignHash(design);
            if (!TryGet(hash, out _, out _))
            {
                var sim = Simulate(c, design);
                Store(hash, sim.maxQ, sim.maxQAlt);
            }
            return Readout.Quick(c, design);
        }

        public static SimResult Simulate(Catalog c, JsonObject design)
        {
            var d = Design.FromJson(design);
            var input = Stack.SimInputs(c, d);
            var sim = new AscentSim(input.stages, input.opts);
            while (sim.Tick())
                if (sim.MaxQSealed) break;
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
        readonly string hash;
        readonly AscentSim sim;
        bool done;
        public bool Done => done;

        public ReferenceJob(Catalog c, JsonObject design)
        {
            hash = ReferenceAscent.DesignHash(design);
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
                if (!sim.Tick() || sim.MaxQSealed)
                {
                    var r = sim.Finish();
                    ReferenceAscent.Store(hash, r.maxQ, r.maxQAlt);
                    done = true;
                    return true;
                }
                if (sw.Elapsed.TotalMilliseconds >= budgetMs) return false;
            }
        }
    }
}
