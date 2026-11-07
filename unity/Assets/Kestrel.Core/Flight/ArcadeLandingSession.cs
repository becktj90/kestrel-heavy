// One booster landing with the player-flown final burn (DECISIONS #79). Wraps the BoosterLanding the live pad already
// steps: the autopilot's stick until ArcadeLanding.HandoverDue, then the player's ArcadeInput as a RecoveryStick into the
// same fixed steps. Feeds presented time through ArcadeLanding.TimeScale (slow motion is pacing only). Keeps the HUD
// readout, the punchy callouts and, at touchdown, the real verdict's splash and the ArcadeScore points. RecoveryShow
// draws this; ArcadeLandingTests fly it with scripted pilots at the phone's 60 fps frame. No Unity types.
using System;
using System.Globalization;

namespace Kestrel.Core.Flight
{
    public sealed class ArcadeLandingSession
    {
        /// <summary>Real seconds a callout stays up (the splash stays until the flight ends).</summary>
        public const double CallSec = 2.2;

        public const string HandoverTitle = "YOUR BURN";
        public const string HandoverLine = "Autopilot off. Light it on the cue.";
        public const string LightNowTitle = "LIGHT NOW";
        public const string LateTitle = "TOO LATE";
        public const string LateLine = "Full throttle!";
        public const string LegsTitle = "LEGS DOWN";

        public readonly BoosterLanding Landing;
        public readonly ArcadeLandingTune Tune;
        public readonly ArcadePilot Pilot;

        /// <summary>The player's axes; the HUD writes them every frame. Ignored until the hand-over and on the autopilot.</summary>
        public ArcadeInput Input;

        public bool HandedOver { get; private set; }
        public double HandoverT { get; private set; } = double.NaN;
        public double HandoverAltM { get; private set; } = double.NaN;
        public double HandoverPropKg { get; private set; } = double.NaN;
        public ArcadeReadout Readout { get; private set; }
        public PresentationTimeScale Scale { get; private set; } = PresentationTimeScale.Realtime;
        public ArcadeScore Score { get; private set; }
        public ArcadeSplash Splash { get; private set; }
        /// <summary>The punchy line over the flight (empty: none), and real seconds since it went up.</summary>
        public string CallTitle { get; private set; } = "";
        public string CallLine { get; private set; } = "";
        public double CallAge { get; private set; }
        /// <summary>Real seconds since touchdown; below 0 before it.</summary>
        public double DownWallSec { get; private set; } = -1;

        bool calledLight, calledLate, calledLegs;

        public ArcadeLandingSession(BoosterLanding landing, ArcadeLandingTune tune, bool autoAssist)
        {
            Landing = landing ?? throw new ArgumentNullException(nameof(landing));
            Tune = tune ?? ArcadeLandingTune.Default;
            Pilot = ArcadeLanding.Pilot(landing.Fate, landing.Craft.Mode, autoAssist);
        }

        public bool PlayerFlying => Pilot == ArcadePilot.Player && HandedOver && !Landing.Touched;

        /// <summary>The stick for this frame: the autopilot until the hand-over, then the player's input.</summary>
        public RecoveryStick Stick
        {
            get
            {
                if (Pilot != ArcadePilot.Player || !HandedOver) return RecoveryStick.Assisted;
                var b = Landing.Body;
                RecoveryFrames.Axes(b.X, b.Y, out _, out double alt, out _, out _, out _, out _, out _);
                return ArcadeLanding.Stick(Input, Landing.Craft, b.Mass, alt, Tune);
            }
        }

        /// <summary>
        /// A skip ("next stage") may run the flight up to the player's hand-over but never past it: true while the player
        /// is still to take the burn. <see cref="RunToHandover"/> is that skip.
        /// </summary>
        public bool SkipStopsAtHandover => Pilot == ArcadePilot.Player && Landing.Started && !HandedOver && !Landing.Touched;

        /// <summary>
        /// Advance by real time <paramref name="wallDt"/>: scaled by <see cref="Scale"/> (slow motion feeds less time, the
        /// fixed steps never change), stepped through BoosterLanding.Step on <see cref="Stick"/>.
        /// </summary>
        public RecoverySample Step(double wallDt)
        {
            if (!Landing.Started) return default;
            if (double.IsNaN(wallDt) || wallDt < 0) wallDt = 0;
            Scale = ArcadeLanding.TimeScale(Pilot, HandedOver, Readout.AltM, DownWallSec, Tune);
            var s = Landing.Step(Scale.ToSimSeconds(wallDt), Stick);
            CallAge += wallDt;
            if (DownWallSec >= 0) DownWallSec += wallDt;
            Note(s);
            return s;
        }

        /// <summary>Run the autopilot to the hand-over now (the skip). True once the player has the burn.</summary>
        public bool RunToHandover()
        {
            if (!SkipStopsAtHandover) return HandedOver;
            var craft = Landing.Craft;
            var tune = Tune;
            Landing.AdvanceUntil(RecoveryStick.Assisted, b => ArcadeLanding.HandoverDue(b, craft, tune));
            Note(Landing.Sample());
            return HandedOver;
        }

        /// <summary>
        /// The ascent is over. On the autopilot the booster settles at once (BoosterLanding.Conclude, t466). When the player
        /// flies the burn, the autopilot runs straight to the hand-over instead, so there is no dead air and the burn is still theirs.
        /// </summary>
        public bool Conclude(bool ascentOver, bool ascentLost)
        {
            if (Pilot != ArcadePilot.Player) return Landing.Conclude(ascentOver, ascentLost);
            if (!ascentOver || ascentLost || !Landing.Started || Landing.Touched) return false;
            if (!HandedOver) RunToHandover();
            return false;
        }

        void Note(in RecoverySample s)
        {
            if (!Landing.Started) return;
            if (!HandedOver && Pilot == ArcadePilot.Player && !Landing.Touched && ArcadeLanding.HandoverDue(Landing.Body, Landing.Craft, Tune))
            {
                HandedOver = true;
                HandoverT = s.T;
                HandoverAltM = Math.Max(0, s.Alt);
                HandoverPropKg = s.Prop;
                // The player's slider starts where the engine is (the autopilot may already have lit it), lean centred.
                Input = new ArcadeInput { Throttle = ThrottleInput(s.Throttle), Steer = 0 };
                Call(HandoverTitle, HandoverLine);
            }
            Readout = ArcadeLanding.Read(s, Landing.Craft, Tune, HandedOver && Pilot == ArcadePilot.Player);
            if (PlayerFlying)
            {
                if (!calledLight && Readout.Cue == LightCue.LightNow) { calledLight = true; Call(LightNowTitle, ""); }
                if (!calledLate && Readout.Cue == LightCue.Late) { calledLate = true; Call(LateTitle, LateLine); }
                if (!calledLegs && Readout.LegsDown && Landing.Craft.Legs) { calledLegs = true; Call(LegsTitle, ""); }
            }
            if (Landing.Touched && Score == null && Landing.Verdict != null)
            {
                DownWallSec = 0;
                var v = Landing.Verdict;
                Splash = ArcadeLanding.Splash(v, Landing.Craft.Mode);
                if (Pilot == ArcadePilot.Player && HandedOver)
                {
                    Score = ArcadeLanding.Score(v, HandoverPropKg, Tune);
                    Call(Splash.Title, Bonus(Score));
                }
                else Score = new ArcadeScore { Grade = v.GradeName };
            }
        }

        /// <summary>The slider position that gives engine throttle <paramref name="throttle"/> now (ArcadeLanding.SliderFor at the body's mass and height).</summary>
        public double ThrottleInput(double throttle)
        {
            var b = Landing.Body;
            RecoveryFrames.Axes(b.X, b.Y, out _, out double alt, out _, out _, out _, out _, out _);
            return ArcadeLanding.SliderFor(throttle, Landing.Craft, b.Mass, alt, Tune);
        }

        /// <summary>The slider position that asks for thrust over weight <paramref name="tw"/> (1 holds a hover).</summary>
        public double SliderForTw(double tw) => Tune.MaxTw > 0 ? Math.Max(0, Math.Min(1, tw / Tune.MaxTw)) : 0;

        void Call(string title, string line)
        {
            CallTitle = title ?? "";
            CallLine = line ?? "";
            CallAge = 0;
        }

        /// <summary>The callout to draw now (the splash stays up after touchdown).</summary>
        public bool CallVisible => !string.IsNullOrEmpty(CallTitle) && (Landing.Touched && Pilot == ArcadePilot.Player && HandedOver || CallAge < CallSec);

        /// <summary>"+2400  BULLSEYE x2 · SOFT x1.5": the points and the bonuses that made them.</summary>
        public static string Bonus(ArcadeScore score)
        {
            if (score == null) return "";
            var inv = CultureInfo.InvariantCulture;
            string line = "+" + score.Points.ToString(inv);
            void Add(string call, double x)
            {
                if (string.IsNullOrEmpty(call) || !(x > 1)) return;
                line += "  " + call + " x" + x.ToString("0.##", inv);
            }
            Add(score.CentreCall, score.CentreX);
            Add(score.SoftCall, score.SoftX);
            Add(score.FuelCall, score.FuelX);
            return line;
        }
    }

    /// <summary>The landing HUD's cells (ALT, SINK, OFFSET, TILT, FUEL, THR) and the cue line, as drawn. Static text, no blink.</summary>
    public static class ArcadeHud
    {
        public const string AltKey = "ALT";
        public const string SinkKey = "SINK";
        public const string OffsetKey = "OFFSET";
        public const string TiltKey = "TILT";
        public const string FuelKey = "FUEL";
        public const string ThrottleKey = "T/W";
        public const int Count = 6;
        public static readonly string[] Labels = { AltKey, SinkKey, OffsetKey, TiltKey, FuelKey, ThrottleKey };
        /// <summary>The widest values per cell, for the text-fit sweeps.</summary>
        public static readonly string[] ValueSamples = { "9999 m", "999.9 m/s", "R 999 m", "R 99.9°", "100%", "9.99" };
        /// <summary>The widest cue line.</summary>
        public static readonly string[] CueSamples = { "LIGHT IN 99.9 s", "LIGHT NOW", "ON THE CURVE", "TOO LATE: FULL THR" };
        /// <summary>The widest splash words and bonus line, for the text-fit sweeps.</summary>
        public static readonly string[] SplashSamples = { "OUT OF FUEL", "PERFECT", "SALVAGE", "TOO HARD", "MISSED", "TIPPED" };
        public const string BonusSample = "+6000  BULLSEYE x2  FEATHER x2  FUEL MISER x1.5";
        /// <summary>Faces, points: the callout title, the grade splash, the line under them (wraps to two lines) and the cue.</summary>
        public const float TitleFontPt = 26f, SplashFontPt = 34f, LineFontPt = 13f, CueFontPt = 18f;

        /// <summary>The six cells. <paramref name="eastRight"/>: +east is to the right on screen (the offset and tilt letters follow the picture).</summary>
        public static BoosterHudCell[] Cells(in ArcadeReadout r, bool eastRight, string mode, double vLeg)
        {
            var inv = CultureInfo.InvariantCulture;
            string place = mode == "rtls" ? "pad" : "deck";
            double off = r.OffsetM;
            bool offRight = eastRight ? off > 0 : off < 0;
            double lean = r.LeanDeg;
            bool leanRight = eastRight ? lean > 0 : lean < 0;
            string side(bool right, double v) => Math.Abs(v) < 0.5 ? "" : right ? "R " : "L ";
            var cells = new BoosterHudCell[Count];
            cells[0] = new BoosterHudCell { Key = AltKey, Value = r.AltM.ToString("0", inv) + " m", Spoken = "Height " + r.AltM.ToString("0", inv) + " metres.", Tone = BoosterHudTone.Ink };
            cells[1] = new BoosterHudCell
            {
                Key = SinkKey,
                Value = r.SinkMps.ToString("0.0", inv) + " m/s",
                Spoken = "Sinking " + r.SinkMps.ToString("0.0", inv) + " metres a second.",
                Tone = r.AltM < 60 && r.SinkMps > vLeg ? BoosterHudTone.Amber : r.AltM < 60 ? BoosterHudTone.Green : BoosterHudTone.Ink,
            };
            cells[2] = new BoosterHudCell
            {
                Key = OffsetKey,
                Value = side(offRight, off) + Math.Abs(off).ToString("0", inv) + " m",
                Spoken = Math.Abs(off).ToString("0", inv) + " metres " + (Math.Abs(off) < 0.5 ? "on the " + place + " centre." : (offRight ? "right" : "left") + " of the " + place + " centre."),
                Tone = Math.Abs(off) <= RecoverySim.DeckRadiusM * 0.45 ? BoosterHudTone.Green : Math.Abs(off) <= RecoverySim.DeckRadiusM ? BoosterHudTone.Ink : BoosterHudTone.Amber,
            };
            cells[3] = new BoosterHudCell
            {
                Key = TiltKey,
                Value = side(leanRight, lean) + Math.Abs(lean).ToString("0.0", inv) + "°",
                Spoken = "Leaning " + Math.Abs(lean).ToString("0.0", inv) + " degrees" + (Math.Abs(lean) < 0.5 ? "." : leanRight ? " right." : " left."),
                Tone = r.TiltDeg > RecoverySim.TiltLimitDeg ? BoosterHudTone.Amber : BoosterHudTone.Ink,
            };
            int fuel = (int)Math.Round(r.Prop01 * 100.0, MidpointRounding.AwayFromZero);
            cells[4] = new BoosterHudCell
            {
                Key = FuelKey,
                Value = fuel.ToString(inv) + "%",
                Spoken = "Landing fuel " + fuel.ToString(inv) + " percent, " + r.PropKg.ToString("0", inv) + " kilograms.",
                Tone = r.Prop01 <= BoosterHud.LowFuel01 ? BoosterHudTone.Amber : BoosterHudTone.Green,
            };
            double tw = Math.Max(0, Math.Min(9.99, r.Tw));
            cells[5] = new BoosterHudCell
            {
                Key = ThrottleKey,
                Value = tw.ToString("0.00", inv),
                Spoken = "Thrust " + tw.ToString("0.0", inv) + " times weight" + (r.Throttle > 0.005 ? "." : ", engine off."),
                Tone = tw >= 1 ? BoosterHudTone.Green : tw > 0 ? BoosterHudTone.Amber : BoosterHudTone.Ink,
            };
            return cells;
        }

        /// <summary>The cue line under the gauge.</summary>
        public static string CueText(in ArcadeReadout r)
        {
            var inv = CultureInfo.InvariantCulture;
            switch (r.Cue)
            {
                case LightCue.Wait: return "LIGHT IN " + Math.Min(99.9, r.LightInS).ToString("0.0", inv) + " s";
                case LightCue.LightNow: return "LIGHT NOW";
                case LightCue.Burning: return "ON THE CURVE";
                case LightCue.Late: return "TOO LATE: FULL THR";
                default: return "";
            }
        }

        /// <summary>
        /// The suicide-burn gauge, 0..1 of the hand-over height: where the booster is, the full-throttle stopping height
        /// (the "arc"), and the top of the light-now band. <paramref name="topM"/> is the gauge's top (the hand-over height).
        /// </summary>
        public static void Gauge(in ArcadeReadout r, double topM, ArcadeLandingTune tune, out double alt01, out double stop01, out double light01)
        {
            double top = topM > 1 ? topM : 1;
            alt01 = Clamp01(r.AltM / top);
            double stop = double.IsInfinity(r.StopAltM) || double.IsNaN(r.StopAltM) ? top : r.StopAltM;
            stop01 = Clamp01(stop / top);
            light01 = Clamp01(stop * tune.LightNowRatio / top);
        }

        static double Clamp01(double v) => double.IsNaN(v) ? 0 : v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
