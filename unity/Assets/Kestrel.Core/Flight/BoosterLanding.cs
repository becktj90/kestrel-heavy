// The first stage after MECO and separation. Every stage that leaves the stack gets its landing:
// a design that picked Haven or the pad flies that landing; an expendable design still flies the
// attempt with only what it kept (no fuel from nowhere), says up front why the booster is lost,
// and is never graded or counted as the flight's failure. A single-stage hop has no booster.
// RecoveryShow draws this; the play-through test drives it without Unity. Same steps, same stick,
// same samples, so the flight on the phone is the flight in the test.
using System;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;

namespace Kestrel.Core.Flight
{
    public enum BoosterFate
    {
        /// <summary>No stage leaves the stack (a one-stage hop). No landing phase.</summary>
        None = 0,
        /// <summary>The design picked Haven or the pad. A graded landing.</summary>
        Planned = 1,
        /// <summary>The design picked no landing. The attempt flies and is not graded.</summary>
        Expendable = 2,
    }

    public sealed class BoosterLanding
    {
        /// <summary>How long the "booster lost" line stays up after separation.</summary>
        public const double LostCallSec = 6.0;
        public const string LostTitle = "BOOSTER LOST";

        RecoveryCraft craft;
        RecoveryBody body;
        RecoveryMark[] marks = new RecoveryMark[8];
        int markCount;
        double accum;
        bool haveShot;
        double shotDown, shotAlt, shotSpeed, shotPitchDeg;
        /// <summary>#79: the Haven deck's swell amplitude (metres). The deck's charted centre stays the
        /// guidance aim; the swell moves the deck around it, and the touchdown miss is measured against
        /// where the deck is when the stage arrives.</summary>
        double swellAmpM = LandingBurnDirector.DefaultSwellAmpM;
        /// <summary>#79: the player gave throttle or tilt input while the Landing phase was flying.</summary>
        bool playerFlown;

        public BoosterFate Fate { get; private set; }
        public int CoreSerial { get; private set; }
        public bool Armed { get; private set; }
        public bool Started { get; private set; }
        public bool Touched { get; private set; }
        public string LostWhy { get; private set; }
        public string LostWhyEngineer { get; private set; }
        public RecoveryVerdict Verdict { get; private set; }
        public RecoveryCraft Craft => craft;
        public RecoveryBody Body => body;
        public bool Expendable => Fate == BoosterFate.Expendable;
        /// <summary>#79: the Haven deck's swell amplitude in metres (0: the deck never moves, as before).</summary>
        public double SwellAmpM => swellAmpM;
        /// <summary>#79: where the swell has moved the deck at the booster's current clock.</summary>
        public double DeckOffsetM => Started ? LandingBurnDirector.DeckOffsetM(body.T, swellAmpM) : 0;

        /// <summary>
        /// Arm for one flight. Clears the last flight's log. Engages the log (so the report waits for
        /// touchdown) whenever a first stage will leave the stack.
        /// <paramref name="swellAmpM"/> is the Haven deck's swell (#79); the default is the gentlest
        /// mission-ramp entry, and 0 keeps the deck fixed as before.
        /// </summary>
        public static BoosterLanding Arm(Catalog catalog, Design design, double swellAmpM = LandingBurnDirector.DefaultSwellAmpM)
        {
            RecoveryLog.ArmFlight();
            var landing = new BoosterLanding();
            landing.swellAmpM = double.IsNaN(swellAmpM) || swellAmpM < 0 ? 0 : swellAmpM;
            if (!Releases(catalog, design, out int coreSerial)) return landing;
            landing.CoreSerial = coreSerial;
            if (RecoveryCrafts.TryFrom(catalog, design, out var planned))
            {
                landing.craft = planned;
                landing.Fate = BoosterFate.Planned;
            }
            else if (RecoveryCrafts.TryAnyway(catalog, design, out var anyway))
            {
                landing.craft = anyway;
                landing.Fate = BoosterFate.Expendable;
                Why(anyway, out string kid, out string engineer);
                landing.LostWhy = kid;
                landing.LostWhyEngineer = engineer;
            }
            else return landing;
            landing.Armed = true;
            RecoveryLog.Engage();
            return landing;
        }

        /// <summary>
        /// The first core stage leaves the stack, and its sim serial stage. Same rule as the staging
        /// picture (FlightStagingPlan.Bodies): strap-ons release first, the last stage stays on.
        /// </summary>
        public static bool Releases(Catalog catalog, Design design, out int coreSerial)
        {
            coreSerial = 0;
            if (catalog == null || design == null || design.stages == null || design.stages.Count < 2) return false;
            var serial = Stack.SerialStages(catalog, design).stages;
            var bodies = FlightStagingPlan.Bodies(serial, design.StrapOnCount);
            for (int i = 0; i < bodies.Count; i++)
            {
                if (bodies[i].Role != "stage") continue;
                coreSerial = bodies[i].Serial;
                return bodies[i].Releases;
            }
            return false;
        }

        /// <summary>
        /// The hand-over state <see cref="Watch"/> receives, read straight from the ascent state <paramref name="y"/>
        /// (x, y, vx, vy, inertial) at flight time <paramref name="t"/>: FlightDriver's downrange from the co-rotating pad,
        /// altitude, ground-relative (air-relative) speed, and the flight-path angle of the pad-frame track in degrees
        /// (FlightDriver's AttitudeDeg, d(alt)/d(downrange)). <paramref name="omegaAir"/> is the site rotation rate over
        /// Earth's radius (0 without rotation). The live pad and the batch hand the booster over in this one frame.
        /// </summary>
        public static void HandOver(double[] y, double t, double omegaAir, out double downrangeM, out double altM, out double speedMps, out double pitchDeg)
        {
            double x = y[0], yy = y[1];
            double r = Math.Sqrt(x * x + yy * yy);
            if (!(r > 1)) r = 1;
            double padAngle = Math.PI / 2 + omegaAir * t;
            downrangeM = Gravity.R_EARTH * (Math.Atan2(yy, x) - padAngle);
            altM = r - Gravity.R_EARTH;
            double vx = y[2] + omegaAir * yy, vy = y[3] - omegaAir * x;
            speedMps = Math.Sqrt(vx * vx + vy * vy);
            double up = (vx * x + vy * yy) / r;
            double east = (vy * x - vx * yy) / r;
            // FlightDriver differences downrange (on the ground arc), so the east rate is scaled by R / r.
            pitchDeg = Math.Atan2(up, east * Gravity.R_EARTH / r) * 180.0 / Math.PI;
        }

        /// <summary>
        /// Fly a planned landing out from a hand-over state, on Auto-assist (the default player and the stage-sep spec's
        /// Stay path), exactly as <see cref="Watch"/>, <see cref="Settle"/> and RecoveryShow fly it. Null when the design
        /// picked no Haven or pad landing (an expendable stage is never graded). FlightBatch judges the flight with this.
        /// </summary>
        public static RecoveryVerdict FlyPlanned(Catalog catalog, Design design, double downrangeM, double altM, double speedMps, double pitchDeg)
        {
            if (!RecoveryCrafts.TryFrom(catalog, design, out var craft)) return null;
            double rad = pitchDeg * Math.PI / 180.0;
            var body = RecoverySim.Begin(craft, downrangeM, altM, speedMps * Math.Cos(rad), speedMps * Math.Sin(rad));
            var verdict = RecoverySim.Fly(craft, body, RecoveryStick.Assisted);
            verdict.Attempted = true;
            return verdict;
        }

        /// <summary>
        /// The flown grade as the mission counts it (FlightBatch and the live report share this). A flown touchdown counts
        /// only for a stage with legs (RecoveryScore.Judge reads a legless stage as legs-down, so a soft legless touchdown would
        /// otherwise pass) and only at the site the mission asks for: a Haven landing never satisfies a required RTLS, nor
        /// the reverse. Anything else is "short", the batch's word for no qualifying landing.
        /// </summary>
        public static string MissionGrade(CampaignMission mission, Design design, string flownGrade)
        {
            if (string.IsNullOrEmpty(flownGrade) || mission == null || design == null) return flownGrade;
            if (mission.LandingMode != "rtls" && mission.LandingMode != "barge") return flownGrade;
            if (!design.legs) return "short";
            if (design.recoveryMode != mission.LandingMode) return "short";
            return flownGrade;
        }

        /// <summary>Why an expendable stage cannot land, in the words the flight shows.</summary>
        public static void Why(in RecoveryCraft craft, out string kid, out string engineer)
        {
            var inv = CultureInfo.InvariantCulture;
            bool fuel = craft.ReserveKg > 1;
            bool legs = craft.Legs;
            if (!fuel && !legs)
                kid = "This booster keeps no landing fuel and has no legs, so it falls into the sea. Add landing fuel and legs in Recovery to land it on Haven.";
            else if (!fuel)
                kid = "This booster keeps no landing fuel, so it cannot slow down for Haven. Add landing fuel in Recovery.";
            else if (!legs)
                kid = "No landing was picked. The booster tries for Haven with its fuel, but it has no legs. Pick a landing in Recovery.";
            else
                kid = "No landing was picked. The booster tries for Haven anyway. Pick a landing in Recovery to have it graded.";
            engineer = "Expendable first stage. Reserve " + craft.ReserveKg.ToString("0", inv) + " kg, legs "
                + (legs ? "on" : "off") + ", grid fins " + (craft.GridFins ? "on" : "off") + ". Haven attempt, not graded.";
        }

        /// <summary>
        /// Called once per presented ascent step (<paramref name="flightT"/> is the flight clock). Keeps the last
        /// state while the core is still the active stage and starts the landing on the first step after it has separated.
        /// True on the step the landing starts. <paramref name="over"/> is the ascent ending first.
        /// </summary>
        public bool Watch(double flightT, int stageIdx, double downrangeM, double altM, double speedMps, double pitchDeg, bool hasAttitude, bool over)
        {
            if (!Armed || Started) return false;
            if (stageIdx == CoreSerial && hasAttitude)
            {
                shotDown = downrangeM;
                shotAlt = altM;
                shotSpeed = speedMps;
                shotPitchDeg = pitchDeg;
                haveShot = true;
            }
            if (haveShot && stageIdx > CoreSerial)
            {
                RecoveryLog.Separate(flightT);
                Begin();
                return true;
            }
            if (over) Release();
            return false;
        }

        void Begin()
        {
            double rad = shotPitchDeg * Math.PI / 180.0;
            double vEast = shotSpeed * Math.Cos(rad);
            double vUp = shotSpeed * Math.Sin(rad);
            body = RecoverySim.Begin(craft, shotDown, shotAlt, vEast, vUp);
            marks = new RecoveryMark[8];
            markCount = 0;
            accum = 0;
            Remember();
            Started = true;
        }

        /// <summary>Stop without a touchdown (the flight left the pad, or the ascent ended before separation).</summary>
        public void Release()
        {
            Armed = false;
            RecoveryLog.Release();
        }

        /// <summary>
        /// Advance by presented time <paramref name="dt"/> (seconds, clamped to 0.1 per call) at the
        /// fixed recovery steps, at most 8 steps per call. Publishes the verdict on touchdown.
        /// <paramref name="sink"/> is the flight sink, as the batch flight passes it: phase changes,
        /// touchdown and a crash are announced on it (the touchdown camera beat is the slow-motion
        /// touchdown). Null keeps the old behaviour. A throwing sink never breaks the flight.
        /// The touchdown miss is measured against the deck where the swell has moved it.
        /// </summary>
        public RecoverySample Step(double dt, in RecoveryStick stick, IFlightSimSink sink = null)
        {
            sink = GuardedFlightSink.Wrap(sink);
            if (!Started) return default;
            if (body.Phase != RecoveryPhase.Down)
            {
                if (dt < 0 || double.IsNaN(dt)) dt = 0;
                accum += Math.Min(dt, 0.1);
                int guard = 0;
                while (guard++ < 8 && body.Phase != RecoveryPhase.Down)
                {
                    RecoveryFrames.Axes(body.X, body.Y, out _, out double alt, out _, out _, out _, out _, out _);
                    double step = alt < RecoverySim.NearAlt ? RecoverySim.NearDt : RecoverySim.Dt;
                    if (accum + 1e-9 < step) break;
                    var before = body.Phase;
                    RecoverySim.Step(ref body, craft, stick, step);
                    accum -= step;
                    // #79: input that shaped a step of the landing burn, on a planned landing, makes
                    // the burn player-flown. Auto-assist and expendable attempts never set it.
                    if (Fate == BoosterFate.Planned && body.Phase == RecoveryPhase.Landing && stick.PlayerInput)
                        playerFlown = true;
                    if (body.Phase != before)
                    {
                        Remember();
                        if (sink != null) sink.OnRecoveryPhase(body.Phase, body.T);
                    }
                }
            }
            var sample = RecoverySim.Sample(body, craft);
            RecoveryLog.Tick(body.T);
            if (sample.Phase == RecoveryPhase.Down && !Touched)
            {
                Touched = true;
                // The deck is where the swell has moved it at touchdown, not where it was charted.
                sample.MissM -= LandingBurnDirector.DeckOffsetM(sample.T, swellAmpM);
                if (markCount > 0 && marks[markCount - 1].Phase == RecoveryPhase.Down)
                    marks[markCount - 1].MissM = sample.MissM;
                var verdict = RecoveryScore.Judge(sample, craft, TrimMarks());
                verdict.Attempted = true;
                verdict.PlayerFlown = playerFlown;
                if (Expendable)
                {
                    verdict.Expendable = true;
                    verdict.LostWhy = LostWhy;
                    if (verdict.Failure != null)
                    {
                        verdict.Failure.kid = LostWhy;
                        verdict.Failure.engineer = LostWhyEngineer + " " + verdict.Failure.engineer;
                    }
                }
                Verdict = verdict;
                RecoveryLog.Publish(verdict);
                if (sink != null)
                {
                    sink.OnTouchdown(sample.Sink, sample.TiltDeg, FlightSurfaces.Choose(sample.MissM, RecoverySim.DeckRadiusM, craft.Mode));
                    string boom = RecoveryViews.Boom(verdict.Cause, sample.Sink, craft.VLeg);
                    if (!string.IsNullOrEmpty(boom))
                    {
                        double energy = 0.5 * sample.Mass * sample.Speed * sample.Speed;
                        sink.OnCrash(boom, new FlightVec(body.X, body.Y), new FlightVec(body.Vx, body.Vy), energy);
                    }
                }
            }
            return sample;
        }

        public RecoverySample Sample() => Started ? RecoverySim.Sample(body, craft) : default;

        /// <summary>Upper bound on the fixed recovery steps one <see cref="Settle"/> may run (MaxTime at NearDt, twice over).</summary>
        public const int SettleSteps = (int)(2 * RecoverySim.MaxTime / RecoverySim.NearDt) + 1000;

        /// <summary>
        /// Fly the rest of the landing now, at the same fixed recovery steps <see cref="Step"/> takes (the step size
        /// depends only on the booster's altitude, never on how presented time was sliced), and publish the verdict.
        /// The verdict is bit-identical to stepping it out live with the same stick. True once it has touched down.
        /// <paramref name="sink"/> is passed to <see cref="Step"/>, so a settled touchdown still announces itself.
        /// </summary>
        public bool Settle(in RecoveryStick stick, IFlightSimSink sink = null) => Settle(stick, sink, SettleSteps);

        /// <summary>Like <see cref="Settle(in RecoveryStick, IFlightSimSink)"/> but stops after <paramref name="maxSteps"/> recovery steps (for per-frame Conclude).</summary>
        public bool Settle(in RecoveryStick stick, IFlightSimSink sink, int maxSteps)
        {
            if (!Started) return false;
            if (Touched) return true;
            if (maxSteps < 1) maxSteps = 1;
            if (maxSteps > SettleSteps) maxSteps = SettleSteps;
            sink = GuardedFlightSink.Wrap(sink);
            int guard = 0;
            while (body.Phase != RecoveryPhase.Down && guard++ < maxSteps)
            {
                RecoveryFrames.Axes(body.X, body.Y, out _, out double alt, out _, out _, out _, out _, out _);
                double step = alt < RecoverySim.NearAlt ? RecoverySim.NearDt : RecoverySim.Dt;
                var before = body.Phase;
                RecoverySim.Step(ref body, craft, stick, step);
                if (Fate == BoosterFate.Planned && body.Phase == RecoveryPhase.Landing && stick.PlayerInput)
                    playerFlown = true;
                if (body.Phase != before)
                {
                    Remember();
                    if (sink != null) sink.OnRecoveryPhase(body.Phase, body.T);
                }
            }
            accum = 0;
            Step(0, stick, sink);
            return Touched;
        }

        /// <summary>
        /// Skip to next stage (DECISIONS #78): advance the booster by up to <paramref name="maxSeconds"/> of its own
        /// clock, at the same fixed recovery steps <see cref="Step"/> takes (the step size depends only on the
        /// booster's altitude, never on how presented time was sliced), so the landing is the one live stepping
        /// flies. Returns the burn phase (or touchdown) the advance entered — Entry, Landing or Down — or null
        /// when it ran out of budget without entering one. A booster already in its entry burn keeps advancing
        /// toward the landing burn; one already in its landing burn keeps advancing to touchdown. The verdict is
        /// published on touchdown, as <see cref="Step"/> does.
        /// </summary>
        public RecoveryPhase? AdvanceSkip(in RecoveryStick stick, int maxSteps, double maxSeconds)
        {
            if (!Started || Touched || body.Phase == RecoveryPhase.Down) return null;
            if (maxSteps < 1) maxSteps = 1;
            if (!(maxSeconds > 0)) maxSeconds = 0;
            RecoveryPhase? entered = null;
            double elapsed = 0;
            int guard = 0;
            var startPhase = body.Phase;
            while (guard++ < maxSteps && elapsed < maxSeconds)
            {
                if (body.Phase == RecoveryPhase.Down) break;
                RecoveryFrames.Axes(body.X, body.Y, out _, out double alt, out _, out _, out _, out _, out _);
                double step = alt < RecoverySim.NearAlt ? RecoverySim.NearDt : RecoverySim.Dt;
                var before = body.Phase;
                RecoverySim.Step(ref body, craft, stick, step);
                elapsed += step;
                if (body.Phase != before)
                {
                    Remember();
                    // A burn the advance started in does not count again: an entry burn that drops out and
                    // lights again is the same burn, and the skip is heading for the next one (the landing burn).
                    if (body.Phase != startPhase
                        && (body.Phase == RecoveryPhase.Entry || body.Phase == RecoveryPhase.Landing || body.Phase == RecoveryPhase.Down))
                    {
                        entered = body.Phase;
                        break;
                    }
                }
            }
            accum = 0;
            Step(0, stick);
            if (entered == null && body.Phase == RecoveryPhase.Down) entered = RecoveryPhase.Down;
            return entered;
        }

        /// <summary>
        /// Steps of recovery physics one <see cref="Conclude"/> may run per call. Full <see cref="Settle"/> can be
        /// tens of thousands of steps (KH-3 ~174 s still in the air at insertion); doing that on the main thread
        /// after SECO froze the live view with no report card (Trevor, 2026-11-09). Spread across Present frames
        /// instead; FailureHost still waits on <see cref="RecoveryLog.Active"/>.
        /// </summary>
        public const int ConcludeStepsPerCall = 512;

        /// <summary>
        /// t466 (build 204, KH-3): the upper stage was in orbit, but the report waited for the booster (RecoveryLog.Active,
        /// FailureHost) and the rail for its Boostback, minutes of dead air. Once the ascent has ended without a failure
        /// (insertion or a clean cutoff), a booster still in the air finishes on Auto-assist across Present frames
        /// (<see cref="ConcludeStepsPerCall"/>). A lost ascent is left alone. True when it settled on this call.
        /// #79: a planned landing whose landing burn has already opened is never settled: the player is flying
        /// the burn. If Settle cannot finish past MaxTime, Active is released so the report is never held forever.
        /// </summary>
        public bool Conclude(bool ascentOver, bool ascentLost, IFlightSimSink sink = null)
        {
            if (!ascentOver || ascentLost || !Started || Touched) return false;
            if (Fate == BoosterFate.Planned && body.Phase == RecoveryPhase.Landing) return false;
            if (Settle(RecoveryStick.Assisted, sink, ConcludeStepsPerCall)) return true;
            // Budget only — still falling within MaxTime: keep Active and resume next Present frame.
            if (body.Phase != RecoveryPhase.Down && body.T < RecoverySim.MaxTime && !double.IsNaN(body.T))
                return false;
            // Stuck or past MaxTime without touchdown. Never leave RecoveryLog.Active holding the report
            // forever (Trevor KH-3 freeze: sky / lone stage / no results card).
            if (!Touched && RecoveryLog.Active) RecoveryLog.Release();
            return false;
        }

        /// <summary>
        /// The line over the flight. An expendable stage says it is lost and why for the first
        /// seconds after separation; otherwise the phase callouts (BOOSTBACK ... TOUCHDOWN).
        /// </summary>
        public RecoveryCallout Callout(in RecoverySample sample)
        {
            if (!Started) return default;
            if (Expendable && !string.IsNullOrEmpty(LostWhy) && sample.Phase != RecoveryPhase.Down && sample.T < LostCallSec)
                return new RecoveryCallout { Visible = true, Title = LostTitle, Line = LostTitle + "  " + LostWhy, NarrationId = "" };
            return RecoveryViews.Callout(sample, TrimMarks());
        }

        public RecoveryMark[] TrimMarks()
        {
            int n = markCount;
            if (n < 0) n = 0;
            if (n > marks.Length) n = marks.Length;
            var kept = new RecoveryMark[n];
            for (int i = 0; i < n; i++) kept[i] = marks[i];
            return kept;
        }

        void Remember()
        {
            if (markCount > 0 && marks[markCount - 1].Phase == body.Phase) return;
            if (markCount >= marks.Length)
            {
                var grown = new RecoveryMark[marks.Length * 2];
                for (int i = 0; i < marks.Length; i++) grown[i] = marks[i];
                marks = grown;
            }
            var sample = RecoverySim.Sample(body, craft);
            marks[markCount++] = new RecoveryMark
            {
                Phase = body.Phase,
                T = body.T,
                Alt = sample.Alt,
                Speed = sample.Speed,
                Sink = sample.Sink,
                TiltDeg = sample.TiltDeg,
                MissM = sample.MissM,
                Prop = sample.Prop,
            };
            RecoveryLog.Track(TrimMarks());
        }

        /// <summary>
        /// The failure the post-flight card quotes. An ascent failure wins. A graded landing's failure
        /// is next. An expendable stage's loss is never the flight's failure.
        /// </summary>
        public static FailureEvent ReportFailure(FailureEvent ascent, RecoveryVerdict rec)
        {
            if (ascent != null) return ascent;
            if (rec != null && rec.Attempted && !rec.Expendable) return rec.Failure;
            return null;
        }

        /// <summary>The landing grade the card quotes. Null for no attempt or an expendable stage.</summary>
        public static string ReportGrade(FailureEvent ascent, RecoveryVerdict rec)
        {
            if (ascent != null || rec == null || !rec.Attempted || rec.Expendable) return null;
            return string.IsNullOrEmpty(rec.GradeName) ? null : rec.GradeName;
        }
    }
}
