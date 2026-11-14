// Test Flight (DECISIONS #86): build anything, fly it, see if it reaches orbit or blows up. Engine-free so the
// dotnet runner and Unity EditMode share the same tests.
//
// What it is: the existing free build (MissionMode.Free, the Home hangar route) flown with MissionContext.TestFlight
// set, or a mission's rocket flown once with "Test it" (the mission stays open, the flight is flagged the same way).
// A test flight is the normal flight (same sim, director, failure monitor, stage-sep choice); nothing is fudged.
// It never touches the career: no Kc, stars, medals, unlocks, mission record, wallet or patch (FlightDebrief), no
// draft or Continue record (the last test design has its own save key, DesignLibrary.TestFlightKey), no guide save
// and no Game Center score. No network and no analytics.
//
// The result card reads only the flight's own numbers (SimResult, the failure event, the live sim's tanks):
//   made orbit   = the sim's own orbit rule (Headless: coasting with perigee above OrbitPerigeeFloorM)
//   no liftoff   = the sim never logged "liftoff" (thrust under weight on the pad)
//   max-Q        = the failure monitor's structural-maxq for dynamic pressure over the walls' limit (cause "q")
//   lost control = the monitor's tumble (cause "alpha": angle of attack past 15 deg in thick air)
//   out of fuel  = the monitor's prop-depletion
//   fell short   = an intact flight that did not make orbit
// Any other failure the monitor calls (engine, heat, stage collision, g, structure by bending) keeps the sim's own
// title and numbers (FlightDebrief.TitleOf / NumberLine) rather than inventing a reason. The first thing that
// happened wins: a pad failure before liftoff is that failure; no liftoff beats a later fuel-out on the pad.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Designer;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public enum TestOutcome { Orbit, OutOfFuel, NoLiftoff, MaxQBreakup, LostControl, FellShort, Failure }

    /// <summary>Kestrel UX copy for Test Flight (2026-11-11, authoritative). Every string is in the 335 px text-fit sweep.</summary>
    public static class TestFlightCopy
    {
        public const string Name = "Test Flight";
        /// <summary>Home tile label. The UX spec's flask emoji is drawn as a vector glyph (no shipped font has emoji).</summary>
        public const string HomeLabel = "Test Flight";
        public const string HomeSub = "Build anything. See if it flies.";
        public const string HomeSpoken = "Test Flight. Build anything. See if it flies.";
        /// <summary>The Starter screen's secondary button left of LAUNCH (flask glyph drawn beside it).</summary>
        public const string TestIt = "Test it";
        public const string TestItSpoken = "Test it. Flies this rocket as a test. Nothing counts and the mission stays open.";
        public const string Eyebrow = "TEST FLIGHT";
        public const string Rules = "No Kc spent, no stars. Just science.";

        public const string Retry = "Tweak & Retry";
        public const string RetrySpoken = "Tweak and retry. Back to the designer with the same rocket.";
        public const string NewDesign = "New Design";
        public const string NewDesignSpoken = "New design. A fresh rocket in the designer.";
        public const string Home = "Home";
        public const string HomeSpokenButton = "Home. Back to the title screen.";

        public const string OrbitTitle = "You made orbit!";
        public const string OrbitKid = "Your rocket is circling Earth.";
        public const string FuelTitle = "Out of fuel";
        public const string FuelKid = "The tanks ran dry before orbit.";
        public const string FuelHint = "More fuel, or a lighter top.";
        public const string LiftTitle = "Too heavy to fly";
        public const string LiftKid = "The engines can't push harder than the rocket weighs.";
        public const string LiftHint = "More engines, or less weight.";
        public const string MaxQTitle = "Kaboom at max-Q!";
        public const string MaxQKid = "The air pushed too hard when it was going fastest.";
        /// <summary>Real levers (FailureMonitor.Fix): a higher safety factor or stronger material ("stronger frame"), and a
        /// lower max-Q throttle ("gentler climb" through max-Q).</summary>
        public const string MaxQHint = "A gentler climb, or a stronger frame.";
        public const string SpinTitle = "Spun out";
        public const string SpinKid = "It tipped over and couldn't straighten up.";
        /// <summary>
        /// Real levers (FailureMonitor.Fix / Kid for tumble): bigger fins, more engine gimbal ("steering"), ease the turn,
        /// and for a negative static margin move mass UP. UX's "or less weight up top" is the opposite of the sim's lever,
        /// so it is left out until UX words the real one (reported on the PR).
        /// </summary>
        public const string SpinHint = "More steering or bigger fins.";
        public const string ShortTitle = "Fell back down";
        public const string ShortKid = "It went up, but not fast enough to stay up.";
        public const string ShortHint = "More fuel, or another stage.";
        public const string HintPrefix = "Try: ";

        public const string OrbitEngineer = "Orbit {apogee} × {perigee} km · {dvLeft} m/s left";
        public const string FuelEngineer = "Empty at {alt} km, {speed} m/s · {short} m/s short";
        public const string LiftEngineer = "Thrust-to-weight {twr} at launch · needs over 1.0";
        public const string MaxQEngineer = "{q} kPa at {alt} km · limit {limit} kPa";
        public const string SpinEngineer = "{angle}° off course at {alt} km";
        public const string ShortEngineer = "Peak {alt} km · {speed} of {needed} m/s";

        /// <summary>Every fixed player string, for the text-fit sweep and the no-emoji check.</summary>
        public static readonly string[] All =
        {
            HomeLabel, HomeSub, TestIt, Eyebrow, Rules, Retry, NewDesign, Home,
            OrbitTitle, OrbitKid, FuelTitle, FuelKid, FuelHint, LiftTitle, LiftKid, LiftHint,
            MaxQTitle, MaxQKid, MaxQHint, SpinTitle, SpinKid, SpinHint, ShortTitle, ShortKid, ShortHint,
        };
    }

    /// <summary>What one flight did, read from the sim. NaN where the flight never measured it.</summary>
    public sealed class TestFlightFacts
    {
        public FailureEvent Failure;
        public string Outcome = "";
        public bool LiftedOff;
        public double ApoAltM = double.NaN, PeriAltM = double.NaN;
        /// <summary>Highest altitude the flight reached, m (watched per step; else MissionScore.FlownApoAltM).</summary>
        public double PeakAltM = double.NaN;
        /// <summary>Inertial speed at that highest point, m/s. At the top of an arc the speed is all sideways, so this is
        /// the speed that counts toward staying up.</summary>
        public double PeakSpeedMps = double.NaN;
        /// <summary>Δv still in the tanks at the end (AscentSim.RemainingVacDeltaV), m/s.</summary>
        public double DvLeftMps = double.NaN;
        /// <summary>Altitude and inertial speed at the failure (the telemetry sample at or just before it).</summary>
        public double FailAltM = double.NaN, FailSpeedMps = double.NaN, FailQPa = double.NaN;
        /// <summary>Liftoff thrust over weight of the design (Readout.Quick's liftoffTWR).</summary>
        public double LiftoffTwr = double.NaN;
        /// <summary>Lowest structural q limit on the stack, Pa (FailureMonitor.StructuralQLimPa / SimResult.qLimPa).</summary>
        public double QLimPa = double.NaN;
    }

    public sealed class TestFlightCard
    {
        public TestOutcome Outcome;
        public string Title = "", Kid = "", Engineer = "", Hint = "";
        /// <summary>The failure kind the card explains ("" for orbit, "no-liftoff" and "fell-short" for the non-events).</summary>
        public string Kind = "";
        public bool MadeOrbit => Outcome == TestOutcome.Orbit;
        /// <summary>The hint line as shown ("Try: …"), empty when there is no hint.</summary>
        public string HintLine => string.IsNullOrEmpty(Hint) ? "" : TestFlightCopy.HintPrefix + Hint;
        /// <summary>VoiceOver order: title, kid, engineer, hint.</summary>
        public string Spoken => Join(Title, Kid, Engineer, HintLine);

        static string Join(params string[] parts)
        {
            var list = new List<string>();
            foreach (var p in parts) if (!string.IsNullOrEmpty(p)) list.Add(p.TrimEnd('.', '!') + ".");
            return string.Join(" ", list);
        }
    }

    public static class TestFlight
    {
        /// <summary>
        /// The test orbit (DECISIONS #86): the sim's own orbit rule. A coasting rocket whose perigee is above this
        /// height is in orbit (Headless.cs: outcome "orbit" when periAlt &gt; 120 km). The guidance aim is the free
        /// build's own: the design's catalog orbit, leo400 when unset (MissionFlightSetup).
        /// </summary>
        public const double OrbitPerigeeFloorM = 120e3;
        /// <summary>The structural-maxq cause that means dynamic pressure over the walls' limit (FailureMonitor.Cause).</summary>
        public const string QCause = "q";
        /// <summary>The tumble cause that measures an angle (FailureMonitor.Cause).</summary>
        public const string AlphaCause = "alpha";
        public const string NoLiftoffKind = "no-liftoff";
        public const string FellShortKind = "fell-short";

        /// <summary>
        /// The narration "mission" fact for the flight on the pad (NarrationFacts.Mission). Null on any test flight, so
        /// every mission-gated take (historical and per-mission commentary, requires.mission) stays silent: a test is
        /// not that mission's flight, so its mission lines are not facts about it (fail closed).
        /// </summary>
        public static string NarrationMission(string missionId) => GameSession.IsTestFlight ? null : missionId;

        /// <summary>Circular orbital speed at <paramref name="altM"/>, m/s: √(μ/(R+h)).</summary>
        public static double CircularSpeed(double altM) => Math.Sqrt(Gravity.MU_EARTH / (Gravity.R_EARTH + Math.Max(0, altM)));

        /// <summary>The speed an orbit needs at a height: circular speed at that height, or at the orbit floor when lower.</summary>
        public static double NeededSpeed(double altM) => CircularSpeed(double.IsNaN(altM) ? OrbitPerigeeFloorM : Math.Max(altM, OrbitPerigeeFloorM));

        /// <summary>
        /// Sideways speed a rocket needs at the top of its arc to stay up: the high-point speed of the orbit whose low
        /// point just clears the test floor (vis-viva, √(2μ·r_floor / (r_top·(r_top + r_floor))). Below the floor there
        /// is no such orbit, so the floor's circular speed.
        /// </summary>
        public static double NeededAtTop(double topAltM)
        {
            double rf = Gravity.R_EARTH + OrbitPerigeeFloorM;
            if (double.IsNaN(topAltM)) return Math.Sqrt(Gravity.MU_EARTH / rf);
            double ra = Gravity.R_EARTH + topAltM;
            if (ra <= rf) return Math.Sqrt(Gravity.MU_EARTH / rf);
            return Math.Sqrt(2 * Gravity.MU_EARTH * rf / (ra * (ra + rf)));
        }

        /// <summary>Speed at the high point of a coasting arc with these high and low points (vis-viva). NaN when unknown.</summary>
        public static double SpeedAtTop(double apoAltM, double periAltM)
        {
            if (double.IsNaN(apoAltM) || double.IsNaN(periAltM) || double.IsInfinity(apoAltM)) return double.NaN;
            double ra = Gravity.R_EARTH + apoAltM, rp = Math.Max(0, Gravity.R_EARTH + periAltM);
            if (ra <= 0 || ra + rp <= 0) return double.NaN;
            return Math.Sqrt(2 * Gravity.MU_EARTH * rp / (ra * (ra + rp)));
        }

        /// <summary>
        /// The sim's facts for one flight. <paramref name="sim"/> may be null (no tanks reading then).
        /// <paramref name="topAltM"/> and <paramref name="topSpeedMps"/> are the highest point the caller watched and the
        /// inertial speed there (the live pad and <see cref="FlyHeadless"/> track them per step); NaN reads the result's
        /// samples instead, then the drag-aware flown peak.
        /// </summary>
        public static TestFlightFacts Facts(SimResult result, FailureEvent failure, AscentSim sim, double liftoffTwr, double topAltM = double.NaN, double topSpeedMps = double.NaN)
        {
            var f = new TestFlightFacts { LiftoffTwr = liftoffTwr };
            if (result == null)
            {
                f.Failure = failure;
                return f;
            }
            f.Failure = failure ?? result.failure;
            f.Outcome = result.outcome ?? "";
            f.ApoAltM = result.orbit.apoAlt;
            f.PeriAltM = result.orbit.periAlt;
            f.QLimPa = result.qLimPa;
            SimEvent failAt = null;
            if (result.events != null)
                for (int i = 0; i < result.events.Count; i++)
                {
                    var e = result.events[i];
                    if (e == null) continue;
                    if (e.type == "liftoff") f.LiftedOff = true;
                    if (e.type == "failure" && failAt == null) failAt = e;
                }
            if (!double.IsNaN(topAltM) && !double.IsNaN(topSpeedMps))
            {
                f.PeakAltM = topAltM;
                f.PeakSpeedMps = topSpeedMps;
            }
            else if (result.samples != null && result.samples.Count > 0)
            {
                var top = result.samples[0];
                for (int i = 1; i < result.samples.Count; i++) if (result.samples[i].alt > top.alt) top = result.samples[i];
                f.PeakAltM = top.alt;
                f.PeakSpeedMps = top.v;
            }
            // A flight that ended in the air (coasting out, suborbital or a failure on the way up) has not reached its
            // top yet: the sim hands the rest of the arc to its orbit (MissionScore.FlownApoAltM is that arc's drag-aware
            // peak). The speed there is the arc's high-point speed (vis-viva on the result's orbit). An impact has already
            // flown its whole arc, so the watched top stands.
            if (result.outcome != "crash")
            {
                double arcTop = MissionScore.FlownApoAltM(result);
                double atTop = SpeedAtTop(f.ApoAltM, f.PeriAltM);
                if (!double.IsNaN(arcTop) && !double.IsNaN(atTop) && (double.IsNaN(f.PeakAltM) || arcTop > f.PeakAltM))
                {
                    f.PeakAltM = arcTop;
                    f.PeakSpeedMps = atTop;
                }
            }
            if (double.IsNaN(f.PeakAltM)) f.PeakAltM = MissionScore.FlownApoAltM(result);
            if (failAt != null && failAt.alt.HasValue && failAt.v.HasValue)
            {
                f.FailAltM = failAt.alt.Value;
                f.FailSpeedMps = failAt.v.Value;
            }
            else if (f.Failure != null && result.samples != null && result.samples.Count > 0)
            {
                SimSample at = result.samples[0];
                for (int i = 0; i < result.samples.Count; i++)
                {
                    if (result.samples[i].t <= f.Failure.t + 1e-9) at = result.samples[i];
                    else break;
                }
                f.FailAltM = at.alt;
                f.FailSpeedMps = at.v;
                f.FailQPa = at.q;
            }
            if (sim != null)
            {
                try { f.DvLeftMps = Math.Max(0, sim.RemainingVacDeltaV()); }
                catch (Exception) { f.DvLeftMps = double.NaN; }
            }
            if (double.IsNaN(f.DvLeftMps) && result.idealDeltaV > 0)
                f.DvLeftMps = Math.Max(0, result.idealDeltaV - result.deltaVExpended);
            return f;
        }

        /// <summary>The Test Flight result card for one flight's facts. Fails closed: a number the flight never measured
        /// leaves its line on the sim's own wording, never a made-up value.</summary>
        public static TestFlightCard Card(TestFlightFacts f)
        {
            if (f == null) f = new TestFlightFacts();
            var ev = f.Failure;
            bool padFailure = ev != null && ev.kind != "prop-depletion";
            if (!f.LiftedOff && !padFailure && f.Outcome != "")
                return new TestFlightCard
                {
                    Outcome = TestOutcome.NoLiftoff, Kind = NoLiftoffKind,
                    Title = TestFlightCopy.LiftTitle, Kid = TestFlightCopy.LiftKid, Hint = TestFlightCopy.LiftHint,
                    Engineer = double.IsNaN(f.LiftoffTwr) ? "" : TestFlightCopy.LiftEngineer.Replace("{twr}", N2(f.LiftoffTwr)),
                };
            if (ev != null) return FailureCard(ev, f);
            if (f.Outcome == "orbit" && f.PeriAltM > OrbitPerigeeFloorM)
                return new TestFlightCard
                {
                    Outcome = TestOutcome.Orbit, Kind = "",
                    Title = TestFlightCopy.OrbitTitle, Kid = TestFlightCopy.OrbitKid,
                    Engineer = TestFlightCopy.OrbitEngineer
                        .Replace("{apogee}", Km(f.ApoAltM)).Replace("{perigee}", Km(f.PeriAltM))
                        .Replace("{dvLeft}", Whole(double.IsNaN(f.DvLeftMps) ? 0 : f.DvLeftMps)),
                };
            double peakAlt = double.IsNaN(f.PeakAltM) ? f.ApoAltM : f.PeakAltM;
            return new TestFlightCard
            {
                Outcome = TestOutcome.FellShort, Kind = FellShortKind,
                Title = TestFlightCopy.ShortTitle, Kid = TestFlightCopy.ShortKid, Hint = TestFlightCopy.ShortHint,
                Engineer = double.IsNaN(peakAlt) || double.IsNaN(f.PeakSpeedMps) ? "" : TestFlightCopy.ShortEngineer
                    .Replace("{alt}", Km(peakAlt)).Replace("{speed}", Whole(f.PeakSpeedMps))
                    .Replace("{needed}", Whole(NeededAtTop(peakAlt))),
            };
        }

        static TestFlightCard FailureCard(FailureEvent ev, TestFlightFacts f)
        {
            switch (ev.kind)
            {
                case "prop-depletion":
                {
                    string eng = "";
                    if (!double.IsNaN(f.FailAltM) && !double.IsNaN(f.FailSpeedMps))
                    {
                        // Short by: the sideways push still missing at the top of the arc the rocket was on when the
                        // tanks ran dry (the sim stops on the failure, so the result's orbit is that arc).
                        double atTop = SpeedAtTop(f.ApoAltM, f.PeriAltM);
                        double shortBy = double.IsNaN(atTop)
                            ? Math.Max(0, NeededSpeed(f.FailAltM) - f.FailSpeedMps)
                            : Math.Max(0, NeededAtTop(f.ApoAltM) - atTop);
                        eng = TestFlightCopy.FuelEngineer.Replace("{alt}", Km(f.FailAltM)).Replace("{speed}", Whole(f.FailSpeedMps)).Replace("{short}", Whole(shortBy));
                    }
                    return new TestFlightCard { Outcome = TestOutcome.OutOfFuel, Kind = ev.kind, Title = TestFlightCopy.FuelTitle, Kid = TestFlightCopy.FuelKid, Engineer = eng, Hint = TestFlightCopy.FuelHint };
                }
                case "structural-maxq" when ev.cause == QCause:
                {
                    double alt = f.FailAltM;
                    // Whole kPa, unless both round to the same number ("35 kPa · limit 35 kPa"): then one decimal, as the
                    // g line does, so the card never shows a load equal to its limit.
                    string q = Whole(ev.value / 1000.0), lim = Whole(ev.limit / 1000.0);
                    if (q == lim) { q = One(ev.value / 1000.0); lim = One(ev.limit / 1000.0); }
                    string eng = double.IsNaN(alt) ? "" : TestFlightCopy.MaxQEngineer
                        .Replace("{q}", q).Replace("{alt}", Km(alt)).Replace("{limit}", lim);
                    return new TestFlightCard { Outcome = TestOutcome.MaxQBreakup, Kind = ev.kind, Title = TestFlightCopy.MaxQTitle, Kid = TestFlightCopy.MaxQKid, Engineer = eng, Hint = TestFlightCopy.MaxQHint };
                }
                case "tumble" when ev.cause == AlphaCause:
                {
                    double alt = f.FailAltM;
                    string eng = double.IsNaN(alt) ? "" : TestFlightCopy.SpinEngineer.Replace("{angle}", Whole(ev.value)).Replace("{alt}", Km(alt));
                    return new TestFlightCard { Outcome = TestOutcome.LostControl, Kind = ev.kind, Title = TestFlightCopy.SpinTitle, Kid = TestFlightCopy.SpinKid, Engineer = eng, Hint = TestFlightCopy.SpinHint };
                }
                case "tumble":
                    // A negative static margin: the sim measures stability (calibers), not an angle, so the engineer line
                    // is the sim's own (FlightDebrief.NumberLine). Same card and lever otherwise.
                    return new TestFlightCard { Outcome = TestOutcome.LostControl, Kind = ev.kind, Title = TestFlightCopy.SpinTitle, Kid = TestFlightCopy.SpinKid, Engineer = FlightDebrief.NumberLine(ev, null, false), Hint = TestFlightCopy.SpinHint };
                default:
                    // No UX copy for this failure: the sim's own title, kid line and engineer line, never a guessed reason.
                    return new TestFlightCard
                    {
                        Outcome = TestOutcome.Failure, Kind = ev.kind ?? "",
                        Title = FlightDebrief.TitleOf(ev.kind == "axial-overload" ? "structural-maxq" : ev.kind), Kid = ev.kid ?? "",
                        Engineer = ev.kind == "axial-overload" && !string.IsNullOrEmpty(ev.engineer) ? ev.engineer : FlightDebrief.NumberLine(ev, null, false),
                    };
            }
        }

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static string Km(double m) => double.IsNaN(m) ? "0" : Math.Round(m / 1000.0).ToString("#,0", Inv);
        static string Whole(double x) => double.IsNaN(x) ? "0" : Math.Round(x).ToString("#,0", Inv);
        static string One(double x) => double.IsNaN(x) ? "0" : x.ToString("#,0.0", Inv);
        static string N2(double x) => x.ToString("0.00", Inv);

        /// <summary>
        /// One test flight, headless: the free build's own setup (MissionFlightSetup with no mission: the design's catalog
        /// orbit, leo400 when unset), the seeded failure monitor at full strength and the live pad's director, stepped to
        /// the end. The same path the designer predictor and the batch use for a free build; nothing is softened.
        /// </summary>
        public static TestFlightFacts FlyHeadless(Catalog catalog, JsonObject designJson, ulong seed, double dt = MissionFlightSetup.BatchDt)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (designJson == null) throw new ArgumentNullException(nameof(designJson));
            var design = Design.FromJson(designJson);
            var setup = MissionFlightSetup.Build(catalog, null, design, dt);
            ulong hash = FlightBatch.DesignHash(design);
            var monitor = FailureMonitor.Create(catalog, setup.Flown, setup.TargetAltM, hash, seed);
            monitor.Gentle = false;
            setup.Options.monitor = monitor;
            var run = MissionFlightSetup.Start(setup, MissionFlightSetup.Director(catalog, setup, design, hash, seed));
            var top = new TopTracker();
            while (true)
            {
                bool more = run.Step();
                top.Watch(run.Sim.Y);
                if (!more) break;
            }
            var result = run.Finish();
            double twr = double.NaN;
            try { twr = Readout.Quick(catalog, designJson).liftoffTWR; }
            catch (Exception) { twr = double.NaN; }
            return Facts(result, result.failure, run.Sim, twr, top.AltM, top.SpeedMps);
        }

        /// <summary>The highest point of a flight and the inertial speed there, watched one state at a time.</summary>
        public sealed class TopTracker
        {
            public double AltM = double.NaN, SpeedMps = double.NaN;
            public void Reset() { AltM = SpeedMps = double.NaN; }
            /// <summary>One sim state: x, y, vx, vy (Earth-centred inertial, m and m/s).</summary>
            public void Watch(double[] y)
            {
                if (y == null || y.Length < 4) return;
                double alt = Math.Sqrt(y[0] * y[0] + y[1] * y[1]) - Gravity.R_EARTH;
                if (double.IsNaN(alt) || (!double.IsNaN(AltM) && alt <= AltM)) return;
                AltM = alt;
                SpeedMps = Math.Sqrt(y[2] * y[2] + y[3] * y[3]);
            }
        }

        /// <summary>Worst-case card strings for the text-fit sweep: the longest numbers each line can carry.</summary>
        public static IEnumerable<TestFlightCard> WorstCaseCards()
        {
            var sevenDigits = 9999999.0;
            yield return Card(new TestFlightFacts { Outcome = "orbit", LiftedOff = true, ApoAltM = sevenDigits * 1000, PeriAltM = sevenDigits * 1000, DvLeftMps = 99999 });
            yield return Card(new TestFlightFacts { LiftedOff = true, Outcome = "failure", Failure = new FailureEvent { kind = "prop-depletion" }, FailAltM = 9999e3, FailSpeedMps = 99999, });
            yield return Card(new TestFlightFacts { Outcome = "crash", LiftedOff = false, LiftoffTwr = 0.99 });
            yield return Card(new TestFlightFacts { LiftedOff = true, Outcome = "failure", Failure = new FailureEvent { kind = "structural-maxq", cause = QCause, value = 99999e3, limit = 99999e3 }, FailAltM = 9999e3 });
            yield return Card(new TestFlightFacts { LiftedOff = true, Outcome = "failure", Failure = new FailureEvent { kind = "tumble", cause = AlphaCause, value = 180 }, FailAltM = 9999e3 });
            yield return Card(new TestFlightFacts { LiftedOff = true, Outcome = "suborbital", PeakAltM = 9999e3, PeakSpeedMps = 99999 });
        }
    }
}
