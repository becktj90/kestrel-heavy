// Honest Ready (docs/DESIGNER_TIERS_SPEC.md, delivery step 1; audit B-10). The Starter badge reads the launch
// predictor: the designer practice flight (ReferenceAscent.Flight, the live pad's PlayedChecks and director, the same
// flight LaunchPredictor and the timeline fly), plus the batch's landing grade (FlightBatch.PredictLandingGrade) and the
// readout's fairing fit. A deterministic red result is a cause with one plain sentence from data/starter-readiness.json.
// Seeded random events (engine-out, bad mixture, the overpressure hazard) are not deterministic and never set a cause.
// Information only: Launch stays as the hard schema block decides (StarterRocket header, LaunchGate F1).
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Designer
{
    /// <summary>What the predictor says about one Starter setting. <see cref="Cause"/> null: no deterministic red.</summary>
    public sealed class StarterVerdict
    {
        public const string MaxQ = "maxq", Overload = "overload", OverG = "overg", Tumble = "tumble", Heat = "heat",
            BaseHeat = "base-heat", Cryo = "cryo", Separation = "separation", Legs = "legs", Overpressure = "overpressure",
            Pad = "pad", Short = "short", Landing = "landing", BoosterLanding = "booster-landing", Fit = "fit";

        /// <summary>Every cause the verdict can name. data/starter-readiness.json has copy for each.</summary>
        public static readonly IReadOnlyList<string> Causes = new[]
        {
            MaxQ, Overload, OverG, Tumble, Heat, BaseHeat, Cryo, Separation, Legs, Overpressure, Pad, Short, Landing, BoosterLanding, Fit,
        };

        public string Cause;
        /// <summary>The predictor flight's first failure (null when it stayed intact).</summary>
        public FailureEvent Failure;
        public string Outcome, LandingGrade, BoosterGrade;
        public bool Survives => Cause == null;

        /// <summary>The cause a flown failure names, or null for none or a seeded random event.</summary>
        public static string CauseOf(FailureEvent f)
        {
            if (f == null) return null;
            switch (f.kind)
            {
                case "structural-maxq":
                    if (f.cause == "q") return MaxQ;
                    if (f.cause == "margin")
                    {
                        string e = f.engineer ?? "";
                        // PlayedBreakup's played-only stops (Short, Impact, the pad tear) share the "margin" cause.
                        if (e.StartsWith("dv margin", StringComparison.Ordinal) || e.StartsWith("impact", StringComparison.Ordinal)) return Short;
                        if (e.StartsWith("liftoff TWR", StringComparison.Ordinal)) return Pad;
                    }
                    return Overload;
                case "overg": return OverG;
                case "tumble": return Tumble;
                case "thermal": return f.cause == "base-heating" ? BaseHeat : Heat;
                case "cryo-material": return Cryo;
                case "prop-depletion": return Short;
                case "separation-collision": return Separation;
                case "leg-collapse": return Legs;
                case "engine-overpressure": return f.cause == "pc-cap" ? Overpressure : null;
                default: return null; // bad-mixture, engine-out: seeded, not certain
            }
        }

        /// <summary>
        /// Judge a finished predictor flight of <paramref name="design"/> on <paramref name="mission"/>. Order: the cargo fit,
        /// a deterministic failure, then (intact flights only) the landing the mission requires.
        /// </summary>
        public static StarterVerdict Judge(Catalog c, Design design, CampaignMission mission, SimResult sim, bool? fits)
        {
            var v = new StarterVerdict { Failure = sim?.failure, Outcome = sim?.outcome };
            if (fits == false) { v.Cause = Fit; return v; }
            v.Cause = CauseOf(v.Failure);
            if (v.Cause != null || v.Failure != null || sim == null || mission == null) return v;
            if (mission.LandingRequired)
            {
                v.LandingGrade = FlightBatch.PredictLandingGrade(c, design, sim, mission.LandingMode, false);
                if (mission.LandBoosters) v.BoosterGrade = FlightBatch.PredictLandingGrade(c, design, sim, mission.LandingMode, true);
                if (v.LandingGrade == "short") v.Cause = Landing;
                else if (v.BoosterGrade == "short") v.Cause = BoosterLanding;
            }
            return v;
        }
    }

    /// <summary>One predictor flight for a Starter setting, advanced in wall-time slices (like <see cref="ReferenceJob"/>).
    /// On finish it stores the run in the shared <see cref="ReferenceAscent"/> cache and judges it.</summary>
    public sealed class StarterPredictionJob
    {
        readonly Catalog catalog;
        readonly Design design;
        readonly CampaignMission mission;
        readonly bool? fits;
        readonly string key;
        readonly DirectedFlight flight;
        public StarterChoice Choice { get; }
        public StarterVerdict Verdict { get; private set; }
        public bool Done => Verdict != null;

        public StarterPredictionJob(Catalog c, JsonObject designJson, CampaignMission mission, bool? fits, StarterChoice choice)
        {
            catalog = c; this.mission = mission; this.fits = fits; Choice = choice;
            design = Design.FromJson(designJson);
            key = ReferenceAscent.CanonicalKey(c, designJson);
            if (fits == false) { Verdict = StarterVerdict.Judge(c, design, mission, null, false); return; }
            flight = ReferenceAscent.Flight(c, design);
        }

        /// <summary>Run until done or <paramref name="budgetMs"/> elapses; at least one tick. True when done.</summary>
        public bool Step(double budgetMs)
        {
            if (Done) return true;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (flight.Step())
                if (sw.Elapsed.TotalMilliseconds >= budgetMs) return false;
            var sim = flight.Finish();
            ReferenceAscent.Store(key, sim);
            Verdict = StarterVerdict.Judge(catalog, design, mission, sim, fits);
            return true;
        }
    }

    /// <summary>The Starter readiness copy (data/starter-readiness.json). Hosts set <see cref="Active"/> at boot;
    /// with no file the screen falls back to LaunchGate's generic sentence.</summary>
    public sealed class StarterReadinessCopy
    {
        public sealed class CauseCopy
        {
            public string Badge, Kid, Engineer;
            public List<string> Fixes = new List<string>();
        }

        public const string FileName = "starter-readiness.json";
        public const string FallbackBadge = "✗ Breaks up.";

        public static StarterReadinessCopy Active;

        public string Checking = LaunchGate.Checking, TryOne = "Try {0}.", TryTwo = "Try {0} or {1}.", TryReset = "Tap Reset.";
        public readonly Dictionary<string, string> Fixes = new Dictionary<string, string>();
        public readonly Dictionary<string, CauseCopy> Causes = new Dictionary<string, CauseCopy>();

        /// <summary>The copy in use: <see cref="Active"/>, else an empty table (every cause falls back).</summary>
        public static StarterReadinessCopy Current => Active ?? (empty ??= new StarterReadinessCopy());
        static StarterReadinessCopy empty;

        public static StarterReadinessCopy Parse(string json)
        {
            var o = MiniJson.ParseObject(json);
            var copy = new StarterReadinessCopy();
            copy.Checking = o.Str("checking") ?? copy.Checking;
            copy.TryOne = o.Str("tryOne") ?? copy.TryOne;
            copy.TryTwo = o.Str("tryTwo") ?? copy.TryTwo;
            copy.TryReset = o.Str("tryReset") ?? copy.TryReset;
            if (o.Obj("fixes") is JsonObject fx)
                foreach (var k in fx.Order) if (fx.Str(k) is string s) copy.Fixes[k] = s;
            if (o.Obj("causes") is JsonObject cs)
                foreach (var k in cs.Order)
                {
                    if (!(cs.Obj(k) is JsonObject row)) continue;
                    var cc = new CauseCopy { Badge = row.Str("badge"), Kid = row.Str("kid"), Engineer = row.Str("engineer") };
                    if (row.Arr("fixes") is List<object> list) foreach (var f in list) if (f is string s) cc.Fixes.Add(s);
                    copy.Causes[k] = cc;
                }
            return copy;
        }

        /// <summary>data/starter-readiness.json from a checkout.</summary>
        public static StarterReadinessCopy Load(string repoRoot) => Parse(File.ReadAllText(Path.Combine(repoRoot, "data", FileName)));

        public string Badge(string cause) => Causes.TryGetValue(cause ?? "", out var c) && !string.IsNullOrEmpty(c.Badge) ? c.Badge : FallbackBadge;

        public string Engineer(string cause) => Causes.TryGetValue(cause ?? "", out var c) ? c.Engineer ?? "" : "";

        /// <summary>The kid lead sentence plus "Try ..." with the first two fixes <paramref name="can"/> allows, else Reset.</summary>
        public string Kid(string cause, Func<string, bool> can)
        {
            if (!Causes.TryGetValue(cause ?? "", out var c) || string.IsNullOrEmpty(c.Kid)) return LaunchGate.WillFail;
            var names = new List<string>(2);
            foreach (var f in c.Fixes)
            {
                if (names.Count == 2) break;
                if ((can == null || can(f)) && Fixes.TryGetValue(f, out var phrase) && !string.IsNullOrEmpty(phrase)) names.Add(phrase);
            }
            string tail = names.Count == 2 ? string.Format(TryTwo, names[0], names[1])
                : names.Count == 1 ? string.Format(TryOne, names[0]) : TryReset;
            return c.Kid + " " + tail;
        }
    }
}
