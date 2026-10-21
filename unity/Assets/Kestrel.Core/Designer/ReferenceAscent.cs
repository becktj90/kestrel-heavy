// Cached Headless reference ascent for designer max-Q.
// Quick() never runs this. A miss leaves maxQStale true.
// Cost: MissionFlightSetup (Stack.SimInputs plus the mission's aim) → the same RK4 step as Headless (dt 0.2 s, default steering, no sample buffer).
// The run goes to thrust cutoff. There is no early stop: a climb-only seal drops a descent peak,
// and a vacuum-periapsis bound both false-stopped the sounding rocket and ran slower than the full ascent.
// The integrator returns when thrust ends, so this is not an unpowered re-entry.
// Budget: the reference ascent is measured by `kestrel readout-bench`. Release builds of the shipped
// presets stay under 30 ms. Debug (what CI runs) is posted against that budget in docs/PHYSICS.md.
// The UI waits ReferenceDebounce.DebounceMs after the last edit, then calls Readout.Reference
// off the main thread, or ReferenceJob.Step with a ≤ 2 ms budget.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
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

        struct Entry
        {
            public string key;
            public double qPa, altM, maxG;
            public bool flown;
            public double lossGravity, lossDrag, lossSteering, minStability, minCar, burnoutSpeed;
            public double apoAltM;
            public FailureEvent failure;
            public List<FlightRisk> risks;
        }

        /// <summary>What Quick can copy out of a cached ascent. <see cref="flown"/> is false for a max-Q seed that did not run the failure model.</summary>
        public struct CachedFlight
        {
            public bool hit, flown;
            public double qPa, altM, maxG;
            public double lossGravity, lossDrag, lossSteering, minStability, minCar, burnoutSpeed;
            /// <summary>Kepler apogee at cutoff (what FlightBatch scores an apogee trip on). NaN when not flown.</summary>
            public double apoAltM;
            public FailureEvent failure;
            public List<FlightRisk> risks;
        }
        static readonly Dictionary<ulong, Entry> Cache = new Dictionary<ulong, Entry>();
        static readonly object Gate = new object();

        public static void Reset()
        {
            lock (Gate) Cache.Clear();
        }

        /// <summary>
        /// Catalog version, catalog content hash, and canonical design JSON.
        /// The cache stores this and compares it on a hash hit.
        /// </summary>
        public static string CanonicalKey(Catalog c, JsonObject design)
        {
            string ver = c != null && c.VersionKey != null ? c.VersionKey : "0";
            string body = c != null && c.ContentHash != null ? c.ContentHash : "0";
            return "cat=" + ver + ":" + body + "\n" + JsonWriter.Stringify(Canon(design));
        }

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

        public static bool TryGet(string key, out double qPa, out double altM) => TryGet(key, out qPa, out altM, out _);

        public static bool TryGet(string key, out double qPa, out double altM, out double maxG)
        {
            lock (Gate)
            {
                if (key != null && Cache.TryGetValue(Fnv1a64(key), out var e) && e.key == key)
                {
                    qPa = e.qPa; altM = e.altM; maxG = e.maxG; return true;
                }
            }
            qPa = 0; altM = 0; maxG = 0; return false;
        }

        public static void Store(string key, double qPa, double altM, double maxG)
        {
            if (key == null) return;
            lock (Gate) Cache[Fnv1a64(key)] = new Entry { key = key, qPa = qPa, altM = altM, maxG = maxG };
        }

        /// <summary>Full predict-ascent cache: losses, stability, risks and the first failure, if one was predicted.</summary>
        public static void Store(string key, SimResult sim)
        {
            if (key == null || sim == null) return;
            double burnout = double.NaN;
            if (sim.events != null)
                foreach (var e in sim.events)
                    if (e.type == "burnout" && e.v.HasValue) { burnout = e.v.Value; break; }
            lock (Gate)
                Cache[Fnv1a64(key)] = new Entry
                {
                    key = key, qPa = sim.maxQ, altM = sim.maxQAlt, maxG = sim.maxG, flown = true,
                    lossGravity = sim.lossGravity, lossDrag = sim.lossDrag, lossSteering = sim.lossSteering,
                    minStability = sim.minStability, minCar = sim.minCar, burnoutSpeed = burnout,
                    apoAltM = MissionScore.FlownApoAltM(sim),
                    failure = sim.failure, risks = sim.risks,
                };
        }

        public static bool TryGet(string key, out CachedFlight flight)
        {
            lock (Gate)
            {
                if (key != null && Cache.TryGetValue(Fnv1a64(key), out var e) && e.key == key)
                {
                    flight = new CachedFlight
                    {
                        hit = true, flown = e.flown, qPa = e.qPa, altM = e.altM, maxG = e.maxG,
                        lossGravity = e.lossGravity, lossDrag = e.lossDrag, lossSteering = e.lossSteering,
                        minStability = e.minStability, minCar = e.minCar, burnoutSpeed = e.burnoutSpeed,
                        apoAltM = e.flown ? e.apoAltM : double.NaN,
                        failure = e.failure, risks = e.risks,
                    };
                    return true;
                }
            }
            flight = default;
            return false;
        }

        public static QuickReadout Run(Catalog c, JsonObject design)
        {
            string key = CanonicalKey(c, design);
            if (!TryGet(key, out _, out _))
            {
                var sim = Simulate(c, design);
                Store(key, sim);
            }
            return Readout.Quick(c, design);
        }

        public static SimResult Simulate(Catalog c, JsonObject design)
        {
            var input = Inputs(c, design);
            return PlayedChecks.Fly(c, Design.FromJson(design), input.stages, input.opts);
        }

        /// <summary>
        /// Predict inputs: the same <see cref="MissionFlightSetup"/> the live pad flight and FlightBatch fly (F1). The
        /// monitor is the live one (Predict off) and the flight runs under <see cref="PlayedChecks"/>, so the designer
        /// stops where play breaks up (#314). A design whose missionId (or kh- orbit) is a campaign
        /// mission gets that mission's aim, cutoff, run length, coast boil-off and held-out landing fuel. Before F1 the
        /// designer built its own Stack.SimInputs and aimed an apogee hop at its apogee, so a 100 km hop was called
        /// "out of fuel" at its normal burnout. A free build keeps the design's own orbit (an apogee target is
        /// judged against the 1 km hop floor, in MissionFlightSetup for the pad too).
        /// </summary>
        public static (List<SimStage> stages, SimOptions opts) Inputs(Catalog c, JsonObject design)
        {
            var d = Design.FromJson(design);
            var mission = MissionFor(c, d);
            var setup = MissionFlightSetup.Build(c, mission, d, MissionFlightSetup.BatchDt);
            return (setup.Stages, setup.Options);
        }

        /// <summary>The campaign mission a design flies: its missionId, else its catalog orbit key. Null for a free build.</summary>
        public static CampaignMission MissionFor(Catalog c, Design d)
        {
            if (c == null || d == null) return null;
            if (!string.IsNullOrEmpty(d.missionId) && c.CampaignMissions.TryGetValue(d.missionId, out var byId)) return byId;
            if (!string.IsNullOrEmpty(d.payloadOrbit) && c.CampaignMissions.TryGetValue(d.payloadOrbit, out var byOrbit)) return byOrbit;
            return null;
        }

        /// <summary>The reference ascent is a full powered flight. Same steps as <see cref="Headless.Simulate"/>.</summary>
        public static SimResult Fly(System.Collections.Generic.IList<SimStage> stages, SimOptions opts)
            => Headless.Simulate(stages, opts);

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
        readonly PlayedChecks checks;
        bool done;
        public bool Done => done;

        public ReferenceJob(Catalog c, JsonObject design)
        {
            key = ReferenceAscent.CanonicalKey(c, design);
            var input = ReferenceAscent.Inputs(c, design);
            sim = new AscentSim(input.stages, input.opts);
            checks = new PlayedChecks(c, Design.FromJson(design), input.stages, input.opts, sim);
        }

        /// <summary>Run ticks until the ascent ends or <paramref name="budgetMs"/> of wall time has elapsed. At least one tick runs when work remains, so a 0 budget still advances.</summary>
        public bool Step(double budgetMs)
        {
            if (done) return true;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                if (!checks.Tick(sim))
                {
                    var r = sim.Finish();
                    ReferenceAscent.Store(key, r);
                    done = true;
                    return true;
                }
                if (sw.Elapsed.TotalMilliseconds >= budgetMs) return false;
            }
        }
    }
}
