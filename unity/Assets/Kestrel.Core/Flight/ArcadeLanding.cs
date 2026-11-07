// DECISIONS #79 (Trevor, 11-06): the player flies the final landing burn. The autopilot (RecoveryGuidance) still flies
// boostback and entry; once the stage is falling on the suicide-burn curve (ArcadeLanding.HandoverDue) control passes to
// the player, who sets throttle and lean. That input is a RecoveryStick into the same RecoverySim.Step the autopilot
// uses (same fixed steps, same thrust, drag, fin, leg and phase gates): there is no second physics. Grades stay
// RecoveryScore + data/landing.json + BoosterLanding.MissionGrade; ArcadeScore is a points layer on top. Slow motion is a
// PresentationTimeScale (only how much time is fed to the fixed steps). Auto-assist, an expendable stage and a stage
// with no landing keep the autopilot. Tunables: data/arcade-landing.json (ArcadeLandingTune). No Unity types.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Kestrel.Core.Json;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    /// <summary>One multiplier tier: met when the touchdown is within <see cref="Limit"/>.</summary>
    public readonly struct ArcadeTier
    {
        public readonly double Limit, X;
        public readonly string Call;
        public ArcadeTier(double limit, double x, string call) { Limit = limit; X = x; Call = call ?? ""; }
    }

    public sealed class ArcadeLandingTune
    {
        public double HandoverLeadSec, HandoverMaxAltM;
        public double MaxTiltDeg, MaxTw, CutoffInput;
        public double LightNowRatio, LateRatio;
        public double BasePerfect, BaseGood, BaseHard, BaseSalvage;
        public ArcadeTier[] Centre = Array.Empty<ArcadeTier>(), Soft = Array.Empty<ArcadeTier>(), Fuel = Array.Empty<ArcadeTier>();
        public double SlowMoAltM, SlowMoScale, SlowMoHoldSec;
        public double DeckHeaveM, DeckRollDeg, DeckPeriodSec;

        public static ArcadeLandingTune Load(string repoRoot)
            => FromJson(MiniJson.ParseObject(File.ReadAllText(Path.Combine(repoRoot, "data", "arcade-landing.json"))));

        /// <summary>The device fallback when the synced TextAsset is missing; ArcadeLandingTests pins it to data/arcade-landing.json.</summary>
        public static ArcadeLandingTune Default => new ArcadeLandingTune
        {
            HandoverLeadSec = 6, HandoverMaxAltM = 3500,
            MaxTiltDeg = 14, MaxTw = 3.5, CutoffInput = 0.04,
            LightNowRatio = 1.3, LateRatio = 1.0,
            BasePerfect = 1000, BaseGood = 600, BaseHard = 250, BaseSalvage = 100,
            Centre = new[] { new ArcadeTier(3, 2.0, "BULLSEYE"), new ArcadeTier(8, 1.5, "CENTRE"), new ArcadeTier(15, 1.2, "ON THE PAINT") },
            Soft = new[] { new ArcadeTier(0.6, 2.0, "FEATHER"), new ArcadeTier(1.0, 1.5, "SOFT"), new ArcadeTier(1.5, 1.2, "SMOOTH") },
            Fuel = new[] { new ArcadeTier(0.8, 1.5, "FUEL MISER"), new ArcadeTier(0.65, 1.25, "FUEL LEFT") },
            SlowMoAltM = 18, SlowMoScale = 0.35, SlowMoHoldSec = 1.4,
            DeckHeaveM = 0.35, DeckRollDeg = 1.4, DeckPeriodSec = 8.5,
        };

        /// <summary>Missing keys keep <see cref="Default"/>'s value.</summary>
        public static ArcadeLandingTune FromJson(JsonObject o)
        {
            var t = Default;
            if (o == null) return t;
            var h = o.Obj("handover");
            if (h != null) { t.HandoverLeadSec = h.Num("leadSec", t.HandoverLeadSec); t.HandoverMaxAltM = h.Num("maxAltM", t.HandoverMaxAltM); }
            var c = o.Obj("controls");
            if (c != null)
            {
                t.MaxTiltDeg = c.Num("maxTiltDeg", t.MaxTiltDeg);
                t.MaxTw = c.Num("maxTw", t.MaxTw);
                t.CutoffInput = c.Num("cutoffInput", t.CutoffInput);
            }
            var q = o.Obj("cue");
            if (q != null) { t.LightNowRatio = q.Num("lightNowRatio", t.LightNowRatio); t.LateRatio = q.Num("lateRatio", t.LateRatio); }
            var s = o.Obj("score");
            if (s != null)
            {
                var b = s.Obj("base");
                if (b != null)
                {
                    t.BasePerfect = b.Num("Perfect", t.BasePerfect);
                    t.BaseGood = b.Num("Good", t.BaseGood);
                    t.BaseHard = b.Num("Hard", t.BaseHard);
                    t.BaseSalvage = b.Num("Salvage", t.BaseSalvage);
                }
                t.Centre = Tiers(s.Arr("centre"), "withinM", t.Centre);
                t.Soft = Tiers(s.Arr("soft"), "sinkMps", t.Soft);
                t.Fuel = Tiers(s.Arr("fuel"), "left01", t.Fuel);
            }
            var m = o.Obj("slowMo");
            if (m != null)
            {
                t.SlowMoAltM = m.Num("altM", t.SlowMoAltM);
                t.SlowMoScale = m.Num("scale", t.SlowMoScale);
                t.SlowMoHoldSec = m.Num("holdSec", t.SlowMoHoldSec);
            }
            var d = o.Obj("deck");
            if (d != null)
            {
                t.DeckHeaveM = d.Num("heaveM", t.DeckHeaveM);
                t.DeckRollDeg = d.Num("rollDeg", t.DeckRollDeg);
                t.DeckPeriodSec = d.Num("periodSec", t.DeckPeriodSec);
            }
            return t;
        }

        static ArcadeTier[] Tiers(List<object> rows, string key, ArcadeTier[] fallback)
        {
            if (rows == null) return fallback;
            var list = new List<ArcadeTier>();
            foreach (var r in rows)
                if (r is JsonObject j) list.Add(new ArcadeTier(j.Num(key), j.Num("x", 1), j.Str("call", "")));
            return list.ToArray();
        }

        /// <summary>Every value, one line, for comparing two tunes.</summary>
        public string Describe()
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            void N(string k, double v) => sb.Append(k).Append('=').Append(v.ToString("R", inv)).Append(' ');
            N("leadSec", HandoverLeadSec); N("maxAltM", HandoverMaxAltM);
            N("maxTiltDeg", MaxTiltDeg); N("maxTw", MaxTw); N("cutoffInput", CutoffInput);
            N("lightNow", LightNowRatio); N("late", LateRatio);
            N("perfect", BasePerfect); N("good", BaseGood); N("hard", BaseHard); N("salvage", BaseSalvage);
            foreach (var (name, tiers) in new[] { ("centre", Centre), ("soft", Soft), ("fuel", Fuel) })
                foreach (var tier in tiers) sb.Append(name).Append('[').Append(tier.Limit.ToString("R", inv)).Append(',').Append(tier.X.ToString("R", inv)).Append(',').Append(tier.Call).Append("] ");
            N("slowAlt", SlowMoAltM); N("slowScale", SlowMoScale); N("slowHold", SlowMoHoldSec);
            N("heave", DeckHeaveM); N("roll", DeckRollDeg); N("period", DeckPeriodSec);
            return sb.ToString().TrimEnd();
        }
    }

    /// <summary>Who flies the landing burn.</summary>
    public enum ArcadePilot
    {
        /// <summary>The autopilot flies it all (Auto-assist / kid play, an expendable stage, or nothing to land).</summary>
        Autopilot = 0,
        /// <summary>The autopilot flies boostback and entry, then the player flies the landing burn.</summary>
        Player = 1,
    }

    /// <summary>The "light now" cue on the suicide-burn curve.</summary>
    public enum LightCue
    {
        /// <summary>Not the player's burn yet, or already down.</summary>
        None = 0,
        /// <summary>Falling, engine off, still above the light-now band.</summary>
        Wait = 1,
        /// <summary>Inside the band: light the engine.</summary>
        LightNow = 2,
        /// <summary>Engine lit and the curve can still be met.</summary>
        Burning = 3,
        /// <summary>Under the full-throttle stopping height: even full throttle hits hard (drag ignored).</summary>
        Late = 4,
    }

    /// <summary>The player's two axes. Throttle 0..1 is the slider (thrust over weight up to ArcadeLandingTune.MaxTw). Steer -1..1, + leans the thrust toward +east.</summary>
    public struct ArcadeInput
    {
        public double Throttle, Steer;
        public static ArcadeInput Idle => default;
    }

    /// <summary>The numbers the landing HUD shows, read from the RecoverySample the sim already stepped.</summary>
    public struct ArcadeReadout
    {
        public double AltM;
        /// <summary>Falling speed, m/s (0 while climbing).</summary>
        public double SinkMps;
        /// <summary>Signed vertical speed, m/s, + up.</summary>
        public double VerticalMps;
        /// <summary>Booster minus deck (or pad) centre along the ground, metres; + is east of it.</summary>
        public double OffsetM;
        /// <summary>Ground speed, m/s; + east.</summary>
        public double DriftMps;
        /// <summary>Lean from vertical, degrees; + leans the thrust (and nose) toward east.</summary>
        public double LeanDeg;
        public double TiltDeg;
        public double PropKg, Prop01, Throttle;
        /// <summary>Thrust over weight now, and the most the slider can ask for.</summary>
        public double Tw, TwMax;
        /// <summary>Height a full-slider burn needs to stop this sink (the suicide-burn height), metres. Infinity when thrust cannot beat gravity.</summary>
        public double StopAltM;
        /// <summary>Height over the stopping height (1 = light now or never).</summary>
        public double BurnRatio;
        /// <summary>Seconds until the light-now band at the current sink (0 inside it).</summary>
        public double LightInS;
        public LightCue Cue;
        public bool LegsDown;
    }

    /// <summary>The touchdown's points and the multipliers that made them. Grade is the real grade (RecoveryScore), unchanged.</summary>
    public sealed class ArcadeScore
    {
        public string Grade;
        public bool Clean;
        public double Base, CentreX = 1, SoftX = 1, FuelX = 1;
        public string CentreCall = "", SoftCall = "", FuelCall = "";
        public double Fuel01;
        public long Points;
        public double Multiplier => CentreX * SoftX * FuelX;
    }

    public struct ArcadeSplash
    {
        public string Title, Line;
        public bool Good;
    }

    public struct DeckPose
    {
        public double HeaveM, RollDeg;
    }

    public static class ArcadeLanding
    {
        /// <summary>
        /// The player flies the final burn of a planned (graded) landing when Auto-assist is off. Auto-assist (kid play),
        /// an expendable stage and a stage with no landing keep the autopilot. Haven and the pad (RTLS) both qualify.
        /// </summary>
        public static ArcadePilot Pilot(BoosterFate fate, string mode, bool autoAssist)
        {
            if (autoAssist || fate != BoosterFate.Planned) return ArcadePilot.Autopilot;
            return RecoverySim.Offered(mode) ? ArcadePilot.Player : ArcadePilot.Autopilot;
        }

        /// <summary>
        /// The engine's thrust at <paramref name="altM"/> (sea level to vacuum by the air pressure), exactly as
        /// RecoverySim's integrator interpolates it, newtons at full throttle.
        /// </summary>
        public static double Thrust(in RecoveryCraft craft, double altM)
        {
            double sl = craft.ThrustSlN > 0 ? craft.ThrustSlN : craft.ThrustVacN;
            double vac = craft.ThrustVacN > 0 ? craft.ThrustVacN : sl;
            double p = Atmosphere.At(altM > 0 ? altM : 0).p / Atmosphere.P0;
            if (double.IsNaN(p) || p < 0) p = 0;
            if (p > 1) p = 1;
            return sl + (vac - sl) * (1 - p);
        }

        static double Weight(in RecoveryCraft craft, double massKg, double altM)
        {
            double mass = massKg > 1 ? massKg : (craft.DryKg > 1 ? craft.DryKg : 1);
            return mass * Gravity.Accel(Gravity.R_EARTH + Math.Max(0, altM));
        }

        /// <summary>The most thrust over weight the player's slider can ask for: the landing limit, or the engine's whole thrust if that is less.</summary>
        public static double TwMax(in RecoveryCraft craft, double massKg, double altM, ArcadeLandingTune tune)
        {
            double full = Thrust(craft, altM) / Weight(craft, massKg, altM);
            if (double.IsNaN(full) || full < 0) full = 0;
            return Math.Min(full, tune.MaxTw > 0 ? tune.MaxTw : full);
        }

        /// <summary>
        /// Stopping height for <paramref name="sinkMps"/> at the slider's full travel (<see cref="TwMax"/>, at the current
        /// lean, less gravity): sink² / 2·a_net, the suicide-burn height the cue and the gauge read. Drag is left out, so it
        /// errs early. Infinity when the burn cannot out-push gravity.
        /// </summary>
        public static double StopAlt(in RecoveryCraft craft, double massKg, double sinkMps, double tiltDeg, double altM, ArcadeLandingTune tune)
        {
            if (!(sinkMps > 0)) return 0;
            double g = Gravity.Accel(Gravity.R_EARTH + Math.Max(0, altM));
            double vert = Math.Cos(Math.Abs(tiltDeg) * Math.PI / 180.0);
            double aNet = (TwMax(craft, massKg, altM, tune) * vert - 1) * g;
            if (!(aNet > 0.05)) return double.PositiveInfinity;
            return sinkMps * sinkMps / (2 * aNet);
        }

        /// <summary>
        /// True when control passes to the player: past boostback and entry, falling, no higher than maxAltM, and at most
        /// leadSec (at the current sink) above the top of the light-now band (lightNowRatio × the stopping height), so the
        /// player always gets the same lead before the cue; or as soon as the autopilot opens its landing burn.
        /// </summary>
        public static bool HandoverDue(in RecoveryBody body, in RecoveryCraft craft, ArcadeLandingTune tune)
        {
            if (body.Phase == RecoveryPhase.Down) return false;
            if (body.Phase == RecoveryPhase.Landing) return true;
            if (body.Phase == RecoveryPhase.Boostback || body.Phase == RecoveryPhase.Entry) return false;
            RecoveryFrames.Axes(body.X, body.Y, out _, out double alt, out _, out _, out _, out _, out _);
            RecoveryFrames.Split(body.X, body.Y, body.Vx, body.Vy, out _, out double radial);
            if (!(radial < -1) || !(alt > 0)) return false;
            if (alt > tune.HandoverMaxAltM) return false;
            double sink = -radial;
            double stop = StopAlt(craft, body.Mass, sink, 0, alt, tune);
            if (double.IsInfinity(stop)) return true;
            return (alt - tune.LightNowRatio * stop) / sink <= tune.HandoverLeadSec;
        }

        /// <summary>Slider 0..1 to commanded thrust over weight: under the cutoff is engine off, full travel is <see cref="ArcadeLandingTune.MaxTw"/>.</summary>
        public static double TwCommand(double input, ArcadeLandingTune tune)
        {
            if (double.IsNaN(input) || input < tune.CutoffInput) return 0;
            if (input > 1) input = 1;
            return input * tune.MaxTw;
        }

        /// <summary>
        /// The engine throttle (0..1, RecoveryStick.Throttle) that gives thrust over weight <paramref name="tw"/> for this
        /// mass and height. The sim's engine throttles its whole range, as it does for the autopilot.
        /// </summary>
        public static double EngineThrottle(double tw, in RecoveryCraft craft, double massKg, double altM)
        {
            if (double.IsNaN(tw) || !(tw > 0)) return 0;
            double thrust = Thrust(craft, altM);
            if (!(thrust > 0)) return 0;
            double thr = tw * Weight(craft, massKg, altM) / thrust;
            return thr > 1 ? 1 : thr;
        }

        /// <summary>The slider position that gives engine throttle <paramref name="throttle"/> (the inverse of the two above).</summary>
        public static double SliderFor(double throttle, in RecoveryCraft craft, double massKg, double altM, ArcadeLandingTune tune)
        {
            if (!(throttle > 0.005) || !(tune.MaxTw > 0)) return 0;
            double tw = throttle * Thrust(craft, altM) / Weight(craft, massKg, altM);
            double u = tw / tune.MaxTw;
            if (u < tune.CutoffInput) u = tune.CutoffInput;
            return u > 1 ? 1 : u;
        }

        /// <summary>Steer to the commanded attitude: vertical (π/2) minus steer × maxTilt (+ steer leans the thrust east).</summary>
        public static double PitchRad(double steer, ArcadeLandingTune tune)
        {
            if (double.IsNaN(steer)) steer = 0;
            if (steer > 1) steer = 1;
            else if (steer < -1) steer = -1;
            return Math.PI / 2 - steer * tune.MaxTiltDeg * Math.PI / 180.0;
        }

        /// <summary>
        /// The player's input as the stick RecoverySim.Step takes: manual pitch and throttle (thrust over weight for the
        /// body's mass and height); fins, legs and phase stay the law's.
        /// </summary>
        public static RecoveryStick Stick(in ArcadeInput input, in RecoveryCraft craft, double massKg, double altM, ArcadeLandingTune tune)
            => new RecoveryStick
            {
                Auto = false,
                Throttle = EngineThrottle(TwCommand(input.Throttle, tune), craft, massKg, altM),
                PitchRad = PitchRad(input.Steer, tune),
            };

        /// <summary>
        /// The cue for a falling stage: LIGHT NOW inside the band, TOO LATE under the stopping height while still faster
        /// than the legs take (<paramref name="vLeg"/>; a settle at a metre a second is never "late"), else Wait or Burning.
        /// </summary>
        public static LightCue Cue(double altM, double sinkMps, double throttle, double stopAltM, double vLeg, ArcadeLandingTune tune)
        {
            if (!(altM > 0) || !(sinkMps > 0.5)) return throttle > 0.005 ? LightCue.Burning : LightCue.Wait;
            double ratio = stopAltM > 0 ? altM / stopAltM : double.PositiveInfinity;
            if (ratio < tune.LateRatio && sinkMps > vLeg) return LightCue.Late;
            if (throttle > 0.005) return LightCue.Burning;
            return ratio <= tune.LightNowRatio ? LightCue.LightNow : LightCue.Wait;
        }

        public static ArcadeReadout Read(in RecoverySample s, in RecoveryCraft craft, ArcadeLandingTune tune, bool playerBurn)
        {
            double alt = Math.Max(0, s.Alt);
            double sink = s.Sink;
            double lean = RecoveryFrames.Wrap(Math.PI / 2 - s.Pitch) * 180.0 / Math.PI;
            double stop = StopAlt(craft, s.Mass, sink, s.TiltDeg, alt, tune);
            double ratio = stop > 1e-6 ? alt / stop : double.PositiveInfinity;
            double lightIn = 0;
            if (!double.IsInfinity(stop) && sink > 0.5)
            {
                double above = alt - tune.LightNowRatio * stop;
                lightIn = above > 0 ? above / sink : 0;
            }
            double weight = Weight(craft, s.Mass, alt);
            var cue = playerBurn && s.Phase != RecoveryPhase.Down ? Cue(alt, sink, s.Throttle, stop, craft.VLeg, tune) : LightCue.None;
            return new ArcadeReadout
            {
                AltM = alt,
                SinkMps = sink,
                VerticalMps = s.Radial,
                OffsetM = s.MissM,
                DriftMps = s.East,
                LeanDeg = lean,
                TiltDeg = s.TiltDeg,
                PropKg = s.Prop,
                Prop01 = BoosterHud.Fuel01(s.Prop, craft.ReserveKg),
                Throttle = s.Throttle,
                Tw = weight > 0 ? s.Throttle * Thrust(craft, alt) / weight : 0,
                TwMax = TwMax(craft, s.Mass, alt, tune),
                StopAltM = stop,
                BurnRatio = ratio,
                LightInS = lightIn,
                Cue = cue,
                LegsDown = s.Legs01 >= 0.92,
            };
        }

        /// <summary>
        /// Points for a touchdown: base[grade] × centre × soft × fuel. The multipliers apply only to a clean touchdown
        /// (no RecoveryScore cause); a crash or a miss scores its base (0 for no grade). <paramref name="handedPropKg"/> is the
        /// propellant aboard at the hand-over; the fuel tier reads the share of it still aboard.
        /// </summary>
        public static ArcadeScore Score(RecoveryVerdict v, double handedPropKg, ArcadeLandingTune tune)
        {
            var score = new ArcadeScore();
            if (v == null) return score;
            score.Grade = v.GradeName;
            score.Base = Base(v.GradeName, tune);
            score.Clean = v.Cause == null && !string.IsNullOrEmpty(v.GradeName);
            score.Fuel01 = handedPropKg > 1e-6 ? Math.Max(0, Math.Min(1, v.PropLeft / handedPropKg)) : 0;
            if (score.Clean)
            {
                Pick(tune.Centre, Math.Abs(v.MissM), out score.CentreX, out score.CentreCall);
                Pick(tune.Soft, v.Sink, out score.SoftX, out score.SoftCall);
                PickAtLeast(tune.Fuel, score.Fuel01, out score.FuelX, out score.FuelCall);
            }
            score.Points = (long)Math.Round(score.Base * score.Multiplier, MidpointRounding.AwayFromZero);
            return score;
        }

        public static double Base(string grade, ArcadeLandingTune tune)
        {
            if (string.IsNullOrEmpty(grade)) return 0;
            switch (grade.ToLowerInvariant())
            {
                case "perfect": return tune.BasePerfect;
                case "good": return tune.BaseGood;
                case "hard": return tune.BaseHard;
                case "salvage": return tune.BaseSalvage;
                default: return 0;
            }
        }

        static void Pick(ArcadeTier[] tiers, double value, out double x, out string call)
        {
            x = 1; call = "";
            if (tiers == null || double.IsNaN(value)) return;
            for (int i = 0; i < tiers.Length; i++)
                if (value <= tiers[i].Limit) { x = tiers[i].X; call = tiers[i].Call; return; }
        }

        static void PickAtLeast(ArcadeTier[] tiers, double value, out double x, out string call)
        {
            x = 1; call = "";
            if (tiers == null || double.IsNaN(value)) return;
            for (int i = 0; i < tiers.Length; i++)
                if (value >= tiers[i].Limit) { x = tiers[i].X; call = tiers[i].Call; return; }
        }

        /// <summary>The grade splash: the real grade's word, or the named cause when it did not hold.</summary>
        public static ArcadeSplash Splash(RecoveryVerdict v, string mode)
        {
            string place = mode == "rtls" ? "pad" : "Haven";
            if (v == null) return new ArcadeSplash { Title = "", Line = "" };
            if (v.Cause == RecoveryScore.Miss) return new ArcadeSplash { Title = "MISSED", Line = "Came down " + Math.Abs(v.MissM).ToString("0", CultureInfo.InvariantCulture) + " m from " + place + "." };
            if (v.Cause == RecoveryScore.Fuel) return new ArcadeSplash { Title = "OUT OF FUEL", Line = "The tanks ran dry before the " + (mode == "rtls" ? "pad" : "deck") + "." };
            if (v.Cause == RecoveryScore.Fast) return new ArcadeSplash { Title = "TOO HARD", Line = v.Sink.ToString("0.0", CultureInfo.InvariantCulture) + " m/s at touchdown. The legs folded." };
            if (v.Cause == RecoveryScore.Tilt) return new ArcadeSplash { Title = "TIPPED", Line = v.TiltDeg.ToString("0.0", CultureInfo.InvariantCulture) + "° at touchdown. Land upright." };
            string g = string.IsNullOrEmpty(v.GradeName) ? "DOWN" : v.GradeName.ToUpperInvariant();
            bool good = MissionScore.IsGood(v.GradeName);
            return new ArcadeSplash
            {
                Title = g,
                Line = "On " + place + ": " + v.Sink.ToString("0.0", CultureInfo.InvariantCulture) + " m/s, "
                    + Math.Abs(v.MissM).ToString("0.0", CultureInfo.InvariantCulture) + " m off centre.",
                Good = good,
            };
        }

        /// <summary>
        /// How fast presented time runs on the player's burn: <see cref="ArcadeLandingTune.SlowMoScale"/> below the slow-motion
        /// height and for the hold after touchdown (<paramref name="downWallSec"/> is real seconds since touchdown, &lt; 0
        /// before it), else realtime. Only fed time changes; the fixed recovery steps never do.
        /// </summary>
        public static PresentationTimeScale TimeScale(ArcadePilot pilot, bool handedOver, double altM, double downWallSec, ArcadeLandingTune tune)
        {
            if (pilot != ArcadePilot.Player || !handedOver) return PresentationTimeScale.Realtime;
            double s = tune.SlowMoScale > 0 && tune.SlowMoScale <= 1 ? tune.SlowMoScale : 1;
            if (downWallSec >= 0) return downWallSec < tune.SlowMoHoldSec ? new PresentationTimeScale(s) : PresentationTimeScale.Realtime;
            return altM < tune.SlowMoAltM ? new PresentationTimeScale(s) : PresentationTimeScale.Realtime;
        }

        /// <summary>Haven's drawn swell at <paramref name="tSec"/>: heave and roll from two sines. A pad (RTLS) does not move.</summary>
        public static DeckPose Deck(double tSec, string mode, ArcadeLandingTune tune)
        {
            if (mode == "rtls" || !(tune.DeckPeriodSec > 0) || double.IsNaN(tSec) || double.IsInfinity(tSec)) return default;
            double w = 2 * Math.PI / tune.DeckPeriodSec;
            return new DeckPose
            {
                HeaveM = tune.DeckHeaveM * (0.7 * Math.Sin(w * tSec) + 0.3 * Math.Sin(2.3 * w * tSec + 1.1)),
                RollDeg = tune.DeckRollDeg * (0.8 * Math.Sin(0.83 * w * tSec + 0.4) + 0.2 * Math.Sin(1.9 * w * tSec)),
            };
        }
    }
}
