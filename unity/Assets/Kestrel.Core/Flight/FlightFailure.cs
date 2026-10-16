// Flight failure picture. Each sim cause id used by the post-flight report maps to one
// effect, one sound, and one callout. Age is seconds after the failure event, so the
// same event and the same age always return the same plume, pieces, and line.
// Sounds are procedural, or an in-repo original / NASA public clip named on the tone.
// Nothing here reads a wall clock.
using System;
using System.Collections.Generic;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public struct FlightFailureCue
    {
        public bool Live;
        public string CauseId;
        public string Kind;
        public string EffectId;
        public string Title;
        public string Callout;
        public string ReportText;
        public string ReportTitle;
        public string SoundId;
        public string Clip;
        public int Wave;
        public int EngineIndex;
        public double T;
    }

    public struct FailurePicture
    {
        public bool Visible;
        public string EffectId;
        public float PlumeScale;
        /// <summary>Seconds into the sputter. Negative leaves the plume on <see cref="PlumeScale"/>.</summary>
        public float SputterAge;
        public float Heat;
        public float Fireball;
        public int Debris;
        public float ShearGap;
        public float ShearDeg;
        public float TumbleDeg;
        public float RecontactM;
        public bool CalloutOn;
    }

    public struct FailureBit
    {
        public float X, Y, Z, Spin, Scale;
    }

    public struct FailureTone
    {
        public string SoundId;
        public string Clip;
        public int Wave;
        public float Seconds;
        public float Hz0, Hz1;
    }

    public static class FlightFailure
    {
        public const string SputterCut = "sputter-cut";
        public const string Fireball = "fireball-debris";
        public const string Shear = "shear";
        public const string TumbleSpin = "tumble";
        public const string Recontact = "recontact";
        public const string DryCutoff = "dry-cutoff";
        public const string Burst = "burst";

        public const int WaveSputter = 1;
        public const int WaveBlast = 2;
        public const int WaveCrack = 3;
        public const int WaveSpin = 4;
        public const int WaveClack = 5;
        public const int WaveCutoff = 6;
        public const int WaveBurst = 7;

        public const double SputterSec = 0.45;
        public const double DrySec = 0.40;
        public const double RecontactCloseSec = 0.40;
        public const double RecontactStartM = 3.2;
        public const double CalloutSec = 2.6;
        public const double TailSec = 2.8;
        public const float TumbleDegPerSec = 160f;
        public const float FireballRadiusM = 7.5f;
        public const int FireballPieces = 8;
        public const int ShearPieces = 6;

        public static FlightFailureCue CueOf(FailureEvent ev) => CueOf(ev, null);

        /// <summary>Report text is <see cref="FlightDebrief.NumberLine"/> for this event, so the callout and the card share the numbers.</summary>
        public static FlightFailureCue CueOf(FailureEvent ev, FlightFacts facts)
        {
            if (ev == null || string.IsNullOrEmpty(ev.kind)) return default;
            string effect = EffectId(ev.kind, ev.cause);
            var tone = ToneOf(effect);
            string report = FlightDebrief.NumberLine(ev, facts, false);
            string title = Title(ev.kind);
            return new FlightFailureCue
            {
                Live = true,
                CauseId = ev.cause ?? "",
                Kind = ev.kind,
                EffectId = effect,
                Title = title,
                Callout = string.IsNullOrEmpty(report) ? title : title + "  " + report,
                ReportText = report,
                ReportTitle = FlightDebrief.TitleOf(ev.kind),
                SoundId = tone.SoundId,
                Clip = tone.Clip,
                Wave = tone.Wave,
                EngineIndex = ev.engineIndex,
                T = ev.t,
            };
        }

        public static string EffectId(string kind, string cause)
        {
            if (cause == null) cause = "";
            switch (string.IsNullOrEmpty(kind) ? cause : kind)
            {
                case "engine-out":
                case "bad-mixture":
                    return SputterCut;
                case "engine-overpressure":
                    return Fireball;
                case "tumble":
                    return TumbleSpin;
                case "separation-collision":
                    return Recontact;
                case "prop-depletion":
                    return DryCutoff;
                case "structural-maxq":
                case "overg":
                    return Shear;
                default:
                    return Burst;
            }
        }

        public static FailureTone ToneOf(string effect)
        {
            switch (effect)
            {
                case SputterCut:
                    return Make(SputterCut, "", WaveSputter, 0.55f, 180f, 180f);
                case Fireball:
                    return Make(Fireball, "rud", WaveBlast, 0.45f, 90f, 40f);
                case Shear:
                    return Make(Shear, "", WaveCrack, 0.32f, 70f, 70f);
                case TumbleSpin:
                    return Make(TumbleSpin, "whoosh", WaveSpin, 0.80f, 90f, 42f);
                case Recontact:
                    return Make(Recontact, "hit", WaveClack, 0.28f, 240f, 240f);
                case DryCutoff:
                    return Make(DryCutoff, "", WaveCutoff, 0.50f, 220f, 55f);
                default:
                    return Make(Burst, "", WaveBurst, 0.30f, 110f, 110f);
            }
        }

        /// <summary>8 Hz gate, then silence. High and low are fixed so two clocks cannot drift.</summary>
        public static float SputterGate(double age)
        {
            if (age < 0 || double.IsNaN(age)) return 1f;
            if (age >= SputterSec) return 0f;
            double s = Math.Sin(age * 8.0 * Math.PI * 2.0);
            return s >= 0 ? 0.9f : 0.05f;
        }

        public static float DryScale(double age, bool reduceMotion)
        {
            if (reduceMotion) return 0f;
            if (age <= 0 || double.IsNaN(age)) return 1f;
            if (age >= DrySec) return 0f;
            return (float)(1.0 - age / DrySec);
        }

        public static float RecontactGap(double age, bool reduceMotion)
        {
            if (reduceMotion) return 0f;
            if (age < 0 || double.IsNaN(age)) age = 0;
            if (age <= RecontactCloseSec)
                return (float)(RecontactStartM * (1.0 - age / RecontactCloseSec));
            double u = age - RecontactCloseSec;
            double bounce = u * 3.5;
            if (bounce > 1.4) bounce = 1.4;
            return (float)bounce;
        }

        public static FailurePicture At(in FlightFailureCue cue, double age, bool reduceMotion)
        {
            var picture = new FailurePicture { EffectId = cue.EffectId, SputterAge = -1f };
            if (!cue.Live) return picture;
            if (age < 0 || double.IsNaN(age)) age = 0;
            picture.Visible = true;
            picture.CalloutOn = age < CalloutSec;
            switch (cue.EffectId)
            {
                case SputterCut:
                    if (reduceMotion || age >= SputterSec)
                        picture.PlumeScale = 0f;
                    else
                    {
                        picture.SputterAge = (float)age;
                        picture.PlumeScale = SputterGate(age);
                    }
                    picture.Heat = picture.PlumeScale > 0.2f ? 0.35f : 0f;
                    break;
                case Fireball:
                    picture.PlumeScale = !reduceMotion && age < 0.08 ? 1f : 0f;
                    picture.Fireball = reduceMotion ? FireballRadiusM : (float)Math.Min(age * 28.0, FireballRadiusM);
                    picture.Debris = FireballPieces;
                    picture.Heat = (float)Math.Max(0.0, 1.0 - age / 0.70);
                    break;
                case Shear:
                    picture.ShearGap = reduceMotion ? 8f : (float)Math.Min(age * 12.0, 10.0);
                    picture.ShearDeg = reduceMotion ? 28f : (float)(age * 80.0);
                    picture.Debris = ShearPieces;
                    picture.PlumeScale = picture.ShearGap > 0.8f ? 0f : 1f;
                    picture.Heat = picture.ShearGap > 0.2f ? 0.55f : 0f;
                    break;
                case TumbleSpin:
                    picture.PlumeScale = 0.45f;
                    picture.TumbleDeg = reduceMotion ? 90f : (float)age * TumbleDegPerSec;
                    picture.Heat = 0.15f;
                    break;
                case Recontact:
                    picture.RecontactM = RecontactGap(age, reduceMotion);
                    picture.PlumeScale = 0f;
                    picture.Heat = picture.RecontactM < 0.2f && age > 0.05 ? 1f : 0.1f;
                    break;
                case DryCutoff:
                    picture.PlumeScale = DryScale(age, reduceMotion);
                    picture.Heat = picture.PlumeScale * 0.8f;
                    break;
                default:
                    picture.PlumeScale = 0f;
                    picture.Debris = 6;
                    picture.Fireball = reduceMotion ? 3f : (float)Math.Min(age * 10.0, 3.0);
                    picture.Heat = 0.4f;
                    break;
            }
            return picture;
        }

        public static FailureBit BitAt(string effect, int index, int count, double age, bool reduceMotion, int engineIndex)
        {
            if (count < 1) count = 1;
            if (index < 0) index = 0;
            if (age < 0 || double.IsNaN(age)) age = 0;
            double bias = engineIndex > 0 ? engineIndex : 0;
            double ang = (index + bias) * Math.PI * 2.0 / count;
            float reach;
            float lift;
            if (effect == Shear)
            {
                float gap = reduceMotion ? 8f : (float)Math.Min(age * 12.0, 10.0);
                reach = gap * 0.55f;
                lift = (index % 2 == 0 ? 1f : -0.35f) * gap;
            }
            else
            {
                double speed = effect == Fireball ? 16.0 : 9.0;
                double cap = effect == Fireball ? 12.0 : 7.0;
                reach = reduceMotion ? (float)cap : (float)Math.Min(age * speed, cap);
                lift = (index % 2 == 0 ? 0.55f : -0.2f) * reach;
            }
            return new FailureBit
            {
                X = (float)(Math.Cos(ang) * reach),
                Y = lift,
                Z = (float)(Math.Sin(ang) * reach),
                Spin = (float)(age * 220.0 + index * 37.0),
                Scale = 0.28f + (index % 3) * 0.16f,
            };
        }

        /// <summary>True when this sim event is the failure the report explains.</summary>
        public static bool Matches(FailureEvent failure, SimEvent sim)
        {
            if (failure == null || sim == null || sim.type != "failure") return false;
            return Math.Abs(failure.t - sim.t) <= 0.05;
        }

        public static double EventTime(IList<SimEvent> events, FailureEvent failure)
        {
            if (failure != null) return failure.t < 0 || double.IsNaN(failure.t) ? 0 : failure.t;
            if (events == null) return 0;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                var ev = events[i];
                if (ev != null && ev.type == "failure") return ev.t < 0 ? 0 : ev.t;
            }
            return 0;
        }

        public static void Write(in FailureTone tone, float[] dst, int rate)
        {
            if (dst == null || dst.Length == 0) return;
            if (rate < 1000) rate = 8000;
            int n = dst.Length;
            double dur = n / (double)rate;
            if (dur < 1e-4) dur = 1e-4;
            uint noise = 0x12345678u ^ (uint)(tone.Wave * 997);
            double hz0 = tone.Hz0 > 1f ? tone.Hz0 : 110.0;
            double hz1 = tone.Hz1 > 1f ? tone.Hz1 : hz0;
            for (int i = 0; i < n; i++)
            {
                double u = i / (double)n;
                double t = i / (double)rate;
                float s;
                switch (tone.Wave)
                {
                    case WaveSputter:
                    {
                        double gate = Math.Sin(t * 9.0 * Math.PI * 2.0) > 0 ? 1.0 : 0.0;
                        double sq = Math.Sin(t * hz0 * Math.PI * 2.0) >= 0 ? 1.0 : -1.0;
                        s = (float)(sq * gate * (1.0 - u));
                        break;
                    }
                    case WaveBlast:
                        s = (float)(White(ref noise) * Math.Exp(-t * 7.0));
                        break;
                    case WaveCrack:
                    {
                        double thump = Math.Sin(t * hz0 * Math.PI * 2.0) * Math.Exp(-t * 10.0);
                        double click = t < 0.012 ? White(ref noise) : 0;
                        s = (float)(thump * 0.8 + click * 0.5);
                        break;
                    }
                    case WaveSpin:
                        s = (float)(Math.Sin(Chirp(hz0, hz1, t, dur)) * 0.7);
                        break;
                    case WaveClack:
                    {
                        double a = Math.Exp(-t * 80.0) + 0.85 * Math.Exp(-Math.Abs(t - 0.12) * 80.0);
                        s = (float)(Math.Sin(t * hz0 * Math.PI * 2.0) * a);
                        break;
                    }
                    case WaveCutoff:
                        s = (float)(Math.Sin(Chirp(hz0, hz1, t, dur)) * (1.0 - u));
                        break;
                    default:
                        s = (float)(White(ref noise) * Math.Exp(-t * 4.0) * 0.5);
                        break;
                }
                if (s > 1f) s = 1f;
                if (s < -1f) s = -1f;
                dst[i] = s;
            }
        }

        public static int Fingerprint(string effect)
        {
            var tone = ToneOf(effect);
            var buf = new float[640];
            Write(tone, buf, 8000);
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < buf.Length; i += 5)
                    hash = hash * 31 + (int)Math.Round(buf[i] * 1000.0);
                return hash;
            }
        }

        static FailureTone Make(string id, string clip, int wave, float seconds, float hz0, float hz1)
        {
            return new FailureTone { SoundId = id, Clip = clip, Wave = wave, Seconds = seconds, Hz0 = hz0, Hz1 = hz1 };
        }

        static string Title(string kind)
        {
            switch (kind)
            {
                case "engine-out": return "ENGINE OUT";
                case "bad-mixture": return "MIXTURE";
                case "engine-overpressure": return "RUD";
                case "structural-maxq": return "BREAKUP";
                case "overg": return "OVER-G";
                case "tumble": return "TUMBLE";
                case "separation-collision": return "RECONTACT";
                case "prop-depletion": return "FUEL OUT";
                case "thermal": return "HEAT";
                case "cryo-material": return "CRYO";
                case "leg-collapse": return "LEGS";
                default: return "LOST";
            }
        }

        static double White(ref uint noise)
        {
            noise = noise * 1664525u + 1013904223u;
            return ((noise >> 8) & 0xffff) / 32768.0 - 1.0;
        }

        static double Chirp(double hz0, double hz1, double t, double dur)
        {
            return (hz0 * t + 0.5 * (hz1 - hz0) * t * t / dur) * Math.PI * 2.0;
        }
    }
}
