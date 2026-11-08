// Honest Ready (docs/DESIGNER_TIERS_SPEC.md, delivery step 1; audit B-10). The Starter badge reads the launch
// predictor: the designer practice flight (ReferenceAscent.Flight, the live pad's PlayedChecks and director, the same
// flight LaunchPredictor and the timeline fly), plus the batch's landing grade (FlightBatch.PredictLandingGrade) and the
// readout's fairing fit. A deterministic red result is a cause with one plain sentence from data/starter-readiness.json.
// Seeded random events (engine-out, bad mixture, the overpressure hazard) are not deterministic and never set a cause.
// Information only: Launch stays as the hard schema block decides (StarterRocket header, LaunchGate F1).
// Trevor 11-07 (KH-3 "Wrong place"): an intact flight that misses a circular mission's band names a cause too. KH-3 at
// Kerosene and the lowest fuel read "Ready… fuel to spare", then flew a suborbital 470 x -394 km arc on the device every
// time (the setting is saved per mission, so every retry flew it again). Only the default and one other setting of the
// 30 KH-3 Starter settings make the circle; the predictor now says so.
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
            Pad = "pad", Short = "short", Landing = "landing", BoosterLanding = "booster-landing", Fit = "fit", Orbit = "orbit",
            Low = "low", High = "high", Shape = "shape", Goal = "goal";

        /// <summary>Every cause the verdict can name. data/starter-readiness.json has copy for each.</summary>
        public static readonly IReadOnlyList<string> Causes = new[]
        {
            MaxQ, Overload, OverG, Tumble, Heat, BaseHeat, Cryo, Separation, Legs, Overpressure, Pad, Short, Landing, BoosterLanding, Fit, Orbit,
            Low, High, Shape, Goal,
        };

        public string Cause;
        /// <summary>The predictor flight's first failure (null when it stayed intact).</summary>
        public FailureEvent Failure;
        public string Outcome, LandingGrade, BoosterGrade;
        /// <summary>Why an intact flight does not pass, without making it a failure: the band miss
        /// (<see cref="MissOf"/>) or an in-band result-screen gate (<see cref="Goal"/>). Null when the flight
        /// passes, fails for certain, or ends on a seeded random event. <see cref="Cause"/> stays null for a miss,
        /// so <see cref="Survives"/> stays true: an intact miss is a missed goal, not a breakup (Trevor 11-07), and
        /// the golden honest-readiness test agrees (its truth names a band miss only for circular missions, which
        /// <see cref="OrbitMiss"/> already puts in <see cref="Cause"/>).</summary>
        public string Miss;
        /// <summary>The predictor's flight stayed intact: no deterministic failure, no short landing, no fit block,
        /// no circular-mission band miss. A hop/ellipse/C3 band miss or an in-band gate failure is not a failure:
        /// it lands in <see cref="Miss"/> and <see cref="Passes"/> is false, but the flight survived.</summary>
        public bool Survives => Cause == null;
        /// <summary>An intact flight that misses the mission band (DECISIONS #83). Circular misses name
        /// <see cref="Short"/>/<see cref="Orbit"/> via <see cref="OrbitMiss"/>; this set covers the hop/ellipse/C3 names.</summary>
        public static bool IsMiss(string cause) => cause == Low || cause == High || cause == Shape || cause == Goal;
        /// <summary>DECISIONS #83: the predictor's flight passes the mission (the batch's acceptance rule plus the landing).
        /// "Ready" reads only this. An intact flight that misses the band gets a miss cause in <see cref="Miss"/> (hop: <see cref="Low"/>/
        /// <see cref="High"/>; circular: <see cref="Short"/>/<see cref="Orbit"/> in <see cref="Cause"/>; ellipse/C3: <see cref="Low"/>/
        /// <see cref="High"/>/<see cref="Shape"/>); in the band but failing a result-screen gate gets <see cref="Goal"/> in <see cref="Miss"/>.</summary>
        public bool Passes;

        /// <summary>This verdict as the one verdict source (<see cref="FlightVerdict"/>).</summary>
        /// <summary>The predictor flight ended on a seeded random event (bad mixture, engine-out): not certain either way.</summary>
        public bool Chance => Cause == null && Failure != null;

        /// <summary>This verdict as the one verdict source (<see cref="FlightVerdict"/>). A seeded random end is not a
        /// verdict, so it stays Checking and callers keep their own line. A miss fails with its miss reason, so the
        /// count card still names it.</summary>
        public FlightVerdict AsFlightVerdict() => Passes ? FlightVerdict.Pass() : Chance ? FlightVerdict.Checking : FlightVerdict.Fail(Cause ?? Miss);

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
            // The orbit first, as the report's fix order has it (MissReason): a landing never fixes a missed circle.
            v.Cause = OrbitMiss(mission, sim);
            if (v.Cause != null) return v;
            if (mission.LandingRequired)
            {
                v.LandingGrade = FlightBatch.PredictLandingGrade(c, design, sim, mission.LandingMode, false);
                if (mission.LandBoosters) v.BoosterGrade = FlightBatch.PredictLandingGrade(c, design, sim, mission.LandingMode, true);
                if (v.LandingGrade == "short") v.Cause = Landing;
                else if (v.BoosterGrade == "short") v.Cause = BoosterLanding;
            }
            if (v.Cause != null) return v;
            double apoM = MissionScore.FlownApoAltM(sim);
            // A band miss is a missed goal, not a failure: it lands in Miss (Passes stays false) and Cause stays
            // null, so Survives stays true for an intact flight. Circular misses already returned above via
            // OrbitMiss, which the honest-readiness truth also names; hops/ellipse/C3 do not set Cause.
            if (!LaunchPredictor.AcceptsMission(mission, sim, apoM)) { v.Miss = MissOf(mission, sim, apoM); return v; }
            // In the band: the result screen's other gates (margin, reliability, red risk, budget, ...) decide the pass.
            if (FlightVerdict.ResultPasses(c, mission, design, sim, landingJudged: true)) v.Passes = true;
            else v.Miss = Goal;
            return v;
        }

        /// <summary>
        /// The cause an intact flight gets for missing a circular mission's band (<see cref="MissionScore.TargetMet"/>, the
        /// live card's rule, on the flown peak and the periapsis): <see cref="Short"/> when the flown orbit's mean altitude,
        /// (apo + peri) / 2, is under the band (not enough energy for the circle), else <see cref="Orbit"/> (the energy was
        /// there, but it climbed too steeply and fell back). Null on a hit or another target kind: hops are judged against
        /// the flown hop (StarterScreen.HopLine), and an ellipse or C3 target also counts the departure burn.
        /// </summary>
        public static string OrbitMiss(CampaignMission mission, SimResult sim)
        {
            if (mission == null || sim == null || sim.failure != null || mission.TargetKind != "circular") return null;
            double apo = MissionScore.FlownApoAltM(sim), peri = sim.orbit.periAlt;
            if (double.IsNaN(apo) || double.IsNaN(peri)) return null;
            if (MissionScore.TargetMet(mission.TargetKind, mission.ApoKm, mission.PeriKm, mission.C3Km2PerS2, apo, peri, 0, false, out _)) return null;
            // #335: the mean is the FLOWN apogee, not the Keplerian one. The energy cutoff fires when the specific
            // orbital energy reaches the target, so the Keplerian (apo + peri) / 2 is exactly the target by construction
            // and would always read Orbit for a cutoff flight - even one that climbed to 461 km and fell back (mean 184.9).
            // MissReason.Suborbital judges the same flight by the flown apogee; the verdict must agree with the card.
            double mean = 0.5 * (apo + peri);
            double low = 0.5 * (mission.ApoKm + mission.PeriKm) * 1000 * (1 - MissionScore.AltitudeFraction);
            return mean < low ? Short : Orbit;
        }

        /// <summary>Which way an intact flight missed the mission band: low, high, or the right height in the wrong shape.</summary>
        public static string MissOf(CampaignMission mission, SimResult sim, double flownApoM)
        {
            if (mission.TargetKind == "apogee")
                return flownApoM > mission.ApoKm * 1000 ? High : Low;
            if (sim.outcome != "orbit") return Low;
            if (mission.TargetKind == "c3") return Low;
            double want = (mission.ApoKm + (mission.PeriKm > 0 ? mission.PeriKm : mission.ApoKm)) * 500;
            double mean = (sim.orbit.apoAlt + sim.orbit.periAlt) / 2;
            double band = MissionScore.AltitudeFraction * want;
            if (mean > want + band) return High;
            if (mean < want - band) return Low;
            return Shape;
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
            if (fits == false) { Verdict = StarterVerdict.Judge(c, design, mission, null, false); FlightVerdict.Remember(key, Verdict.AsFlightVerdict()); return; }
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
            FlightVerdict.Remember(key, Verdict.AsFlightVerdict());
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
