// Vertical flight trajectory. A line runs up the screen edge, bottom to top.
// Milestones are ticks: liftoff, max-Q, BECO (strap-on boosters), MECO, fairing, SECO/orbit (SECO only with a real
// upper stage), and, when the booster returns, the recovery stops its planned mode flies (RTLS: boostback, entry,
// Haven; barge: entry, Haven), and never a stop the booster skipped once a later one has lit. Passed ticks are done,
// the next one is the highlight, and the fill plus the marker move with the clock.
// Labels are short plain words and the numbers the flight actually flew. Raw ids stay out.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Kestrel.Core.Flight
{
    public enum TrajectoryStop
    {
        Liftoff = 0,
        MaxQ = 1,
        Meco = 2,
        Fairing = 3,
        Seco = 4,
        Boostback = 5,
        Entry = 6,
        Landing = 7,
        /// <summary>Strap-on booster burnout. Numbered last so existing values keep theirs; the rail orders by list.</summary>
        Beco = 8,
    }

    public struct TrajectoryEvent
    {
        public double T;
        public string Type;
        public int Stage;
        public double AltM, SpeedMps;
    }

    public struct TrajectoryMark
    {
        public TrajectoryStop Stop;
        public string Label;
        public string Clock, Altitude, Speed;
        public double TimeS, AltM, SpeedMps;
        public bool Passed, Upcoming, HasNumbers;
        public float Along;
    }

    public sealed class TrajectoryPlan
    {
        public TrajectoryMark[] Marks = Array.Empty<TrajectoryMark>();
        public float Progress;
        public int Upcoming = -1;
    }

    public struct TrajectoryTag
    {
        public int Index;
        public string[] Lines;
        public float FontPt;
        public HudBox Box;
    }

    /// <summary>Accumulates one flight and lays the milestone line out in points.</summary>
    public sealed class TrajectoryWatch
    {
        public const double QArmPa = 8000;
        public const double QDrop = 0.98;

        public bool HasFairing = true;
        public bool Recovery;
        public int Stages = 2;
        public double FairingAltM = 110000;
        /// <summary>The design flies strap-on boosters: sim stage 1 is the boosters (BECO), stage 2 the core (MECO), and
        /// only a stage after the core is an upper stage (SECO). Was: booster burnout read MECO and core burnout SECO.</summary>
        public bool StrapOns;
        /// <summary>The planned recovery mode ("rtls", "barge", anything else = no boostback planned). Null: unknown, all
        /// three recovery stops are planned. A stop the booster never flew is dropped once a later recovery stop lights.</summary>
        public string RecoveryMode;

        int burnStage; double burnLeft = double.NaN;

        /// <summary>The stage burning now (sim stages count from 1) and its propellant left (0..1, NaN when coasting).
        /// Toward Fairing and SECO on the upper stage the marker follows the burn, so a long flat burn keeps moving.</summary>
        public void Burn(int stage, double left01)
        {
            burnStage = stage;
            burnLeft = Finite(left01) ? Math.Max(0, Math.Min(1, left01)) : double.NaN;
        }

        /// <summary>Core stages, without the strap-on phase. SECO needs two.</summary>
        public int CoreStages => StrapOns ? Stages - 1 : Stages;

        struct Hit
        {
            public bool On;
            public double T, Alt, Speed;
            public bool Orbit;
        }

        Hit liftoff, maxq, beco, meco, fairing, seco, boost, entry, landing;
        double qPeak, qPeakT, qPeakAlt, qPeakSpeed;
        bool qHot;

        bool ascentOver; double ascentOverT;

        /// <summary>
        /// The ascent has ended without a failure at flight time <paramref name="t"/> (insertion, or a clean cutoff). From
        /// then on the insertion stop (SECO / Orbit; MECO on a one-stage hop) is the rail's final stop: a recovery stop the
        /// booster has not flown by now, or a fairing that never opened (KH-1/KH-2 hops), is dropped instead of holding the
        /// marker under it. t466: after orbit the marker waited under Boostback for an expendable booster that never boosts
        /// back. Stops already flown keep their place.
        /// </summary>
        public void AscentOver(double t)
        {
            if (ascentOver) return;
            ascentOver = true;
            ascentOverT = Finite(t) ? t : 0;
        }

        public void Reset()
        {
            ascentOver = false; ascentOverT = 0;
            liftoff = maxq = beco = meco = fairing = seco = boost = entry = landing = default;
            qPeak = qPeakT = qPeakAlt = qPeakSpeed = 0;
            qHot = false;
            highWater = 0f; highWaterT = -1;
            HasFairing = true;
            Recovery = false;
            Stages = 2;
            FairingAltM = 110000;
            StrapOns = false;
            RecoveryMode = null;
            burnStage = 0; burnLeft = double.NaN;
        }

        public void Seen(in TrajectoryEvent ev)
        {
            string type = ev.Type ?? "";
            double alt = Finite(ev.AltM) ? ev.AltM : 0;
            double spd = Finite(ev.SpeedMps) ? ev.SpeedMps : 0;
            double t = Finite(ev.T) ? ev.T : 0;
            if (Is(type, "liftoff")) Note(ref liftoff, t, alt, spd, false);
            else if (Is(type, "maxq") || Is(type, "max-q") || Is(type, "max_q")) Note(ref maxq, t, alt, spd, false);
            else if (Is(type, "beco")) Note(ref beco, t, alt, spd, false);
            else if (Is(type, "meco")) Note(ref meco, t, alt, spd, false);
            else if (Is(type, "burnout") || Is(type, "cutoff") || Is(type, "separation"))
            {
                // Sim stages count from 1. Strap-ons: 1 boosters, 2 core, 3+ upper. Otherwise: 1 core, 2+ upper.
                int core = StrapOns ? 2 : 1;
                bool sep = Is(type, "separation");
                if (StrapOns && ev.Stage <= 1) { if (!sep || !beco.On) Note(ref beco, t, alt, spd, false); }
                else if (ev.Stage <= core) { if (!sep || !meco.On) Note(ref meco, t, alt, spd, false); }
                else if (!sep) Note(ref seco, t, alt, spd, false);
            }
            else if (Is(type, "fairing")) Note(ref fairing, t, alt, spd, false);
            else if (Is(type, "orbit")) Note(ref seco, t, alt, spd, true);
            else if (Is(type, "seco")) Note(ref seco, t, alt, spd, false);
            else if (Is(type, "boostback")) Note(ref boost, t, alt, spd, false);
            else if (Is(type, "entry")) Note(ref entry, t, alt, spd, false);
            else if (Is(type, "landing") || Is(type, "touchdown") || Is(type, "haven"))
                Note(ref landing, t, alt, spd, false);
        }

        public void Sample(double t, double alt, double speed, double q)
        {
            if (!Finite(t) || t < 0) t = 0;
            if (!Finite(alt) || alt < 0) alt = 0;
            if (!Finite(speed) || speed < 0) speed = 0;
            if (!Finite(q) || q < 0) q = 0;
            if (!liftoff.On && alt > 1) Note(ref liftoff, t, alt, speed, false);
            if (q > qPeak)
            {
                qPeak = q;
                qPeakT = t;
                qPeakAlt = alt;
                qPeakSpeed = speed;
            }
            if (qPeak >= QArmPa) qHot = true;
            if (qHot && !maxq.On && t > qPeakT + 0.4 && q < qPeak * QDrop)
                Note(ref maxq, qPeakT, qPeakAlt, qPeakSpeed, false);
            if (HasFairing && !fairing.On && maxq.On && alt >= (FairingAltM > 0 ? FairingAltM : 110000))
                Note(ref fairing, t, alt, speed, false);
            FairingByCutoff();
        }

        /// <summary>
        /// A flat ascent (cutoff or orbit below FairingAltM) never reaches the altitude, and the director's Fairing call
        /// is altitude-gated too, so the tick waited forever and held the marker under SECO. The payload is out once the
        /// upper stage cuts off: the fairing lights there, with that moment's numbers, if nothing lit it before.
        /// </summary>
        void FairingByCutoff()
        {
            if (HasFairing && !fairing.On && seco.On) { fairing = seco; fairing.Orbit = false; }
        }

        /// <summary>Recovery phase word the flight already uses: boostback, entry, landing.</summary>
        public void RecoveryPhase(string phase, double t, double alt, double speed)
        {
            if (string.IsNullOrEmpty(phase)) return;
            Recovery = true;
            if (Is(phase, "boostback")) Note(ref boost, t, alt, speed, false);
            else if (Is(phase, "entry")) Note(ref entry, t, alt, speed, false);
            else if (Is(phase, "landing") || Is(phase, "down") || Is(phase, "touchdown"))
                Note(ref landing, t, alt, speed, false);
        }

        public TrajectoryPlan At(double now, double alt, double speed)
        {
            if (!Finite(now) || now < 0) now = 0;
            FairingByCutoff();
            var stops = new List<TrajectoryStop>(9);
            stops.Add(TrajectoryStop.Liftoff);
            stops.Add(TrajectoryStop.MaxQ);
            if (StrapOns || beco.On) stops.Add(TrajectoryStop.Beco);
            stops.Add(TrajectoryStop.Meco);
            if (HasFairing || fairing.On) stops.Add(TrajectoryStop.Fairing);
            if (CoreStages >= 2 || seco.On) stops.Add(TrajectoryStop.Seco);
            // A hop that ended below the jettison altitude never opened its fairing: that stop does not hold the end.
            if (ascentOver && now + 1e-6 >= ascentOverT && !Flown(fairing, now)) stops.Remove(TrajectoryStop.Fairing);
            if (Recovery || boost.On || entry.On || landing.On)
            {
                if (FliesBoostback() || boost.On) stops.Add(TrajectoryStop.Boostback);
                stops.Add(TrajectoryStop.Entry);
                stops.Add(TrajectoryStop.Landing);
                // A stop this booster skipped (no boostback on a barge run, no entry burn on a gentle fall) used to stay
                // upcoming and hold the marker at 0.88 after touchdown. Once a later recovery stop lights, it goes.
                bool later = landing.On && landing.T <= now + 1e-6;
                if (!entry.On && later) stops.Remove(TrajectoryStop.Entry);
                later |= entry.On && entry.T <= now + 1e-6;
                if (!boost.On && later) stops.Remove(TrajectoryStop.Boostback);
                if (ascentOver && now + 1e-6 >= ascentOverT)
                {
                    if (!Flown(landing, now)) stops.Remove(TrajectoryStop.Landing);
                    if (!Flown(entry, now)) stops.Remove(TrajectoryStop.Entry);
                    if (!Flown(boost, now)) stops.Remove(TrajectoryStop.Boostback);
                }
            }

            int n = stops.Count;
            var marks = new TrajectoryMark[n];
            int upcoming = -1;
            int last = -1;
            for (int i = 0; i < n; i++)
            {
                Hit hit = HitOf(stops[i]);
                bool passed = hit.On && hit.T <= now + 1e-6;
                var mark = new TrajectoryMark
                {
                    Stop = stops[i],
                    Label = LabelOf(stops[i], hit.Orbit),
                    TimeS = passed ? hit.T : double.NaN,
                    AltM = passed ? hit.Alt : double.NaN,
                    SpeedMps = passed ? hit.Speed : double.NaN,
                    Passed = passed,
                    Along = n == 1 ? 0f : i / (float)(n - 1),
                };
                if (passed)
                {
                    mark.HasNumbers = true;
                    mark.Clock = Clock(hit.T);
                    mark.Altitude = Altitude(hit.Alt);
                    mark.Speed = Speed(hit.Speed);
                    last = i;
                }
                else if (upcoming < 0) upcoming = i;
                marks[i] = mark;
            }
            if (upcoming >= 0)
            {
                var u = marks[upcoming];
                u.Upcoming = true;
                marks[upcoming] = u;
            }

            float progress = 0f;
            if (last < 0) progress = 0f;
            else if (upcoming < 0) progress = 1f;
            else
            {
                double lastT = marks[last].TimeS;
                double gap = NominalGap(marks[last].Stop, marks[upcoming].Stop);
                if (!(gap > 1)) gap = 60;
                double x = (now - lastT) / gap;
                // On the upper stage, toward Fairing or SECO, the burn itself says how far along we are (how much of its
                // propellant is gone): the table's gap is replaced, not just topped up. A flat ascent's long burn (no
                // altitude jettison, no director call) used to park the marker at the eased cap for minutes.
                var up = marks[upcoming].Stop;
                bool burning = (up == TrajectoryStop.Fairing || up == TrajectoryStop.Seco) && burnStage > (StrapOns ? 2 : 1) && Finite(burnLeft);
                if (burning) x = 1 - burnLeft;
                // Toward the fairing the altitude says how far along we are too (it jettisons at FairingAltM).
                if (up == TrajectoryStop.Fairing && Finite(alt))
                {
                    double a0m = Finite(marks[last].AltM) ? marks[last].AltM : 0;
                    double target = FairingAltM > 0 ? FairingAltM : 110000;
                    if (target > a0m + 1)
                    {
                        double xa = Math.Max(0, Math.Min(1, (alt - a0m) / (target - a0m)));
                        // Either one brings the fairing: 110 km, or (flat) cutoff. Combined so that the burn keeps the
                        // marker moving after the altitude levels off; it is never below either alone.
                        x = burning ? 1 - (1 - x) * (1 - xa) : Math.Max(x, xa);
                    }
                }
                double frac = Ease(x);
                float a0 = marks[last].Along;
                float a1 = marks[upcoming].Along;
                progress = a0 + (float)frac * (a1 - a0);
            }
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;
            // Never backwards within a flight (a late stop, a slow stage): the marker only moves up.
            if (now + 1e-9 >= highWaterT && progress < highWater) progress = highWater;
            if (now + 1e-9 >= highWaterT) { highWater = progress; highWaterT = now; }
            return new TrajectoryPlan { Marks = marks, Progress = progress, Upcoming = upcoming };
        }

        /// <summary>Label boxes inside the rail. Every line fits the box. Nothing is clipped.</summary>
        public static TrajectoryTag[] Tags(HudBox rail, TrajectoryMark[] marks)
        {
            if (!rail.Live || marks == null || marks.Length == 0) return Array.Empty<TrajectoryTag>();
            const float pad = 4f;
            const float gutter = 14f;
            float textW = rail.W - gutter - pad;
            if (textW < 28f) textW = 28f;
            float span = rail.H - pad * 2f;
            if (span < 8f) span = 8f;

            float[] fonts = { 11f, 10f, 9f };
            bool[] compacts = { false, true };
            foreach (bool compact in compacts)
            foreach (float font in fonts)
            {
                if (TryTags(rail, marks, pad, gutter, textW, span, font, compact, out var tags))
                    return tags;
            }
            if (TryTags(rail, marks, pad, gutter, textW, span, 9f, true, out var fallback))
                return fallback;
            return LabelOnly(rail, marks, pad, gutter, textW, span);
        }

        static TrajectoryTag[] LabelOnly(HudBox rail, TrajectoryMark[] marks, float pad, float gutter, float textW, float span)
        {
            int n = marks.Length;
            float font = 9f;
            float lineH = font + 3f;
            var tags = new TrajectoryTag[n];
            for (int i = 0; i < n; i++)
            {
                string label = marks[i].Label ?? "";
                float center = pad + (1f - marks[i].Along) * span;
                float top = center - lineH * 0.5f;
                if (top < pad) top = pad;
                if (top + lineH > pad + span) top = pad + span - lineH;
                tags[i] = new TrajectoryTag
                {
                    Index = i,
                    Lines = new[] { label },
                    FontPt = font,
                    Box = new HudBox { X = rail.X + gutter, Y = rail.Y + top, W = textW, H = lineH },
                };
            }
            return tags;
        }

        public static bool Plain(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            if (PlayerText.Leak(text)) return false;
            if (text.IndexOf("tl.", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return true;
        }

        static bool TryTags(HudBox rail, TrajectoryMark[] marks, float pad, float gutter, float textW, float span, float font, bool compact, out TrajectoryTag[] tags)
        {
            tags = null;
            int n = marks.Length;
            var lines = new string[n][];
            float lineH = font + 3f;
            float maxH = 0f;
            for (int i = 0; i < n; i++)
            {
                lines[i] = Lines(marks[i], compact);
                for (int k = 0; k < lines[i].Length; k++)
                {
                    if (!Plain(lines[i][k])) return false;
                    // BudgetWidth: Unity's text runs about 15% wider than the width table.
                    if (FlightHudLayout.BudgetWidth(lines[i][k], font) > textW + 0.4f) return false;
                }
                float h = lines[i].Length * lineH;
                if (h > maxH) maxH = h;
            }
            if (n * maxH > span + 0.6f) return false;

            var top = new float[n];
            for (int i = 0; i < n; i++)
            {
                float h = lines[i].Length * lineH;
                float center = pad + (1f - marks[i].Along) * span;
                top[i] = center - h * 0.5f;
            }
            // Index 0 sits at the bottom. Push a label up when it would cover the one below it.
            for (int i = 0; i < n - 1; i++)
            {
                float upperH = lines[i + 1].Length * lineH;
                float limit = top[i] - 2f - upperH;
                if (top[i + 1] > limit) top[i + 1] = limit;
            }
            for (int i = 0; i < n; i++)
            {
                float h = lines[i].Length * lineH;
                if (top[i] < pad - 0.6f) return false;
                if (top[i] + h > pad + span + 0.6f) return false;
                if (i + 1 < n && top[i + 1] + lines[i + 1].Length * lineH + 1.5f > top[i]) return false;
            }

            tags = new TrajectoryTag[n];
            for (int i = 0; i < n; i++)
            {
                float h = lines[i].Length * lineH;
                tags[i] = new TrajectoryTag
                {
                    Index = i,
                    Lines = lines[i],
                    FontPt = font,
                    Box = new HudBox { X = rail.X + gutter, Y = rail.Y + top[i], W = textW, H = h },
                };
            }
            return true;
        }

        static string[] Lines(TrajectoryMark mark, bool compact)
        {
            if (!mark.HasNumbers) return new[] { mark.Label ?? "" };
            if (!compact) return new[] { mark.Label ?? "", mark.Clock ?? "", mark.Altitude ?? "", mark.Speed ?? "" };
            string numbers = (mark.Altitude ?? "") + " · " + (mark.Speed ?? "");
            return new[] { mark.Label ?? "", mark.Clock ?? "", numbers.Trim() };
        }

        /// <summary>The planned mode flies a boostback: RTLS, or unknown (null). A barge or any other mode does not.</summary>
        public bool FliesBoostback() => RecoveryMode == null || string.Equals(RecoveryMode, "rtls", StringComparison.OrdinalIgnoreCase);

        Hit HitOf(TrajectoryStop stop)
        {
            switch (stop)
            {
                case TrajectoryStop.Liftoff: return liftoff;
                case TrajectoryStop.Beco: return beco;
                case TrajectoryStop.MaxQ: return maxq;
                case TrajectoryStop.Meco: return meco;
                case TrajectoryStop.Fairing: return fairing;
                case TrajectoryStop.Seco: return seco;
                case TrajectoryStop.Boostback: return boost;
                case TrajectoryStop.Entry: return entry;
                default: return landing;
            }
        }

        static string LabelOf(TrajectoryStop stop, bool orbit)
        {
            switch (stop)
            {
                case TrajectoryStop.Liftoff: return "Liftoff";
                case TrajectoryStop.MaxQ: return "Max-Q";
                case TrajectoryStop.Beco: return "BECO";
                case TrajectoryStop.Meco: return "MECO";
                case TrajectoryStop.Fairing: return "Fairing";
                case TrajectoryStop.Seco: return orbit ? "Orbit" : "SECO";
                case TrajectoryStop.Boostback: return "Boostback";
                case TrajectoryStop.Entry: return "Entry";
                default: return "Haven";
            }
        }

        /// <summary>
        /// Share of the way to the next stop. #332 (t432, T+116 and T+192): it was the nominal-gap fraction clamped at
        /// 0.82, so a stage that burned longer than the table's gap parked the marker for minutes. Now linear to 0.6,
        /// then easing toward 0.95 without ever stopping: the marker always creeps up and never reaches an unlit stop.
        /// </summary>
        public static double Ease(double x)
        {
            if (!(x > 0)) return 0;
            if (x <= EaseKnee) return x;
            return EaseKnee + (EaseCap - EaseKnee) * (1 - Math.Exp(-(x - EaseKnee) / (EaseCap - EaseKnee)));
        }

        public const double EaseKnee = 0.6, EaseCap = 0.95;
        float highWater; double highWaterT = -1;

        static double NominalGap(TrajectoryStop from, TrajectoryStop to)
        {
            if (from == TrajectoryStop.Liftoff && to == TrajectoryStop.MaxQ) return 50;
            if (from == TrajectoryStop.MaxQ && to == TrajectoryStop.Meco) return 90;
            if (from == TrajectoryStop.MaxQ && to == TrajectoryStop.Beco) return 60;
            if (from == TrajectoryStop.Beco && to == TrajectoryStop.Meco) return 90;
            if (from == TrajectoryStop.Meco && to == TrajectoryStop.Fairing) return 45;
            if (from == TrajectoryStop.Meco && to == TrajectoryStop.Seco) return 180;
            if (from == TrajectoryStop.Fairing && to == TrajectoryStop.Seco) return 150;
            if (from == TrajectoryStop.Seco && to == TrajectoryStop.Boostback) return 40;
            if (from == TrajectoryStop.Seco && to == TrajectoryStop.Entry) return 240;
            if (from == TrajectoryStop.Meco && to == TrajectoryStop.Entry) return 240;
            if (from == TrajectoryStop.Seco && to == TrajectoryStop.Landing) return 320;
            if (from == TrajectoryStop.Boostback && to == TrajectoryStop.Entry) return 200;
            if (from == TrajectoryStop.Entry && to == TrajectoryStop.Landing) return 80;
            return 60;
        }

        static bool Flown(in Hit hit, double now) => hit.On && hit.T <= now + 1e-6;

        static void Note(ref Hit hit, double t, double alt, double speed, bool orbit)
        {
            if (hit.On && t > hit.T + 1e-6)
            {
                if (orbit) hit.Orbit = true;
                return;
            }
            hit.On = true;
            hit.T = t;
            hit.Alt = alt;
            hit.Speed = speed;
            if (orbit) hit.Orbit = true;
        }

        static bool Is(string type, string name) =>
            string.Equals(type, name, StringComparison.OrdinalIgnoreCase);

        static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        public static string Clock(double t)
        {
            if (!Finite(t) || t < 0) t = 0;
            int s = (int)Math.Round(t);
            int m = s / 60;
            int r = s % 60;
            return "T+" + m.ToString(CultureInfo.InvariantCulture) + ":" + r.ToString("00", CultureInfo.InvariantCulture);
        }

        public static string Altitude(double meters)
        {
            if (!Finite(meters) || meters < 0) meters = 0;
            if (meters >= 10000) return (meters / 1000.0).ToString("0", CultureInfo.InvariantCulture) + " km";
            if (meters >= 1000) return (meters / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + " km";
            return Math.Round(meters).ToString("0", CultureInfo.InvariantCulture) + " m";
        }

        public static string Speed(double mps)
        {
            if (!Finite(mps) || mps < 0) mps = 0;
            if (mps >= 1000) return (mps / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + " km/s";
            return Math.Round(mps).ToString("0", CultureInfo.InvariantCulture) + " m/s";
        }
    }
}
