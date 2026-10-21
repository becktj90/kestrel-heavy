// The first stage after MECO and separation. Every stage that leaves the stack gets its landing:
// a design that picked Haven or the pad flies that landing; an expendable design still flies the
// attempt with only what it kept (no fuel from nowhere), says up front why the booster is lost,
// and is never graded or counted as the flight's failure. A single-stage hop has no booster.
// RecoveryShow draws this; the play-through test drives it without Unity. Same steps, same stick,
// same samples, so the flight on the phone is the flight in the test.
using System;
using System.Globalization;
using Kestrel.Core.Data;
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

        /// <summary>
        /// Arm for one flight. Clears the last flight's log. Engages the log (so the report waits for
        /// touchdown) whenever a first stage will leave the stack.
        /// </summary>
        public static BoosterLanding Arm(Catalog catalog, Design design)
        {
            RecoveryLog.ArmFlight();
            var landing = new BoosterLanding();
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
        /// </summary>
        public RecoverySample Step(double dt, in RecoveryStick stick)
        {
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
                    if (body.Phase != before) Remember();
                }
            }
            var sample = RecoverySim.Sample(body, craft);
            RecoveryLog.Tick(body.T);
            if (sample.Phase == RecoveryPhase.Down && !Touched)
            {
                Touched = true;
                var verdict = RecoveryScore.Judge(sample, craft, TrimMarks());
                verdict.Attempted = true;
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
            }
            return sample;
        }

        public RecoverySample Sample() => Started ? RecoverySim.Sample(body, craft) : default;

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
