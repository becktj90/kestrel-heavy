// Vertical flight trajectory. A line runs up the screen edge, bottom to top.
// Milestones are ticks: liftoff, max-Q, MECO, fairing, SECO/orbit, and, when the
// booster returns, boostback, entry, and landing on Haven. Passed ticks are done,
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

        struct Hit
        {
            public bool On;
            public double T, Alt, Speed;
            public bool Orbit;
        }

        Hit liftoff, maxq, meco, fairing, seco, boost, entry, landing;
        double qPeak, qPeakT, qPeakAlt, qPeakSpeed;
        bool qHot;

        public void Reset()
        {
            liftoff = maxq = meco = fairing = seco = boost = entry = landing = default;
            qPeak = qPeakT = qPeakAlt = qPeakSpeed = 0;
            qHot = false;
            HasFairing = true;
            Recovery = false;
            Stages = 2;
            FairingAltM = 110000;
        }

        public void Seen(in TrajectoryEvent ev)
        {
            string type = ev.Type ?? "";
            double alt = Finite(ev.AltM) ? ev.AltM : 0;
            double spd = Finite(ev.SpeedMps) ? ev.SpeedMps : 0;
            double t = Finite(ev.T) ? ev.T : 0;
            if (Is(type, "liftoff")) Note(ref liftoff, t, alt, spd, false);
            else if (Is(type, "maxq") || Is(type, "max-q") || Is(type, "max_q")) Note(ref maxq, t, alt, spd, false);
            else if (Is(type, "meco") || (Is(type, "burnout") && ev.Stage <= 1) || (Is(type, "cutoff") && ev.Stage <= 1))
                Note(ref meco, t, alt, spd, false);
            else if (Is(type, "separation") && ev.Stage <= 1 && !meco.On)
                Note(ref meco, t, alt, spd, false);
            else if (Is(type, "fairing")) Note(ref fairing, t, alt, spd, false);
            else if (Is(type, "orbit")) Note(ref seco, t, alt, spd, true);
            else if (Is(type, "seco") || (Is(type, "burnout") && ev.Stage >= 2) || (Is(type, "cutoff") && ev.Stage >= 2))
                Note(ref seco, t, alt, spd, false);
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
            var stops = new List<TrajectoryStop>(8);
            stops.Add(TrajectoryStop.Liftoff);
            stops.Add(TrajectoryStop.MaxQ);
            stops.Add(TrajectoryStop.Meco);
            if (HasFairing || fairing.On) stops.Add(TrajectoryStop.Fairing);
            if (Stages >= 2 || seco.On) stops.Add(TrajectoryStop.Seco);
            if (Recovery || boost.On || entry.On || landing.On)
            {
                stops.Add(TrajectoryStop.Boostback);
                stops.Add(TrajectoryStop.Entry);
                stops.Add(TrajectoryStop.Landing);
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
                double frac = (now - lastT) / gap;
                if (frac < 0) frac = 0;
                else if (frac > 0.82) frac = 0.82;
                float a0 = marks[last].Along;
                float a1 = marks[upcoming].Along;
                progress = a0 + (float)frac * (a1 - a0);
            }
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;
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
                    if (FlightHudLayout.TextWidth(lines[i][k], font) > textW + 0.4f) return false;
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

        Hit HitOf(TrajectoryStop stop)
        {
            switch (stop)
            {
                case TrajectoryStop.Liftoff: return liftoff;
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
                case TrajectoryStop.Meco: return "MECO";
                case TrajectoryStop.Fairing: return "Fairing";
                case TrajectoryStop.Seco: return orbit ? "Orbit" : "SECO";
                case TrajectoryStop.Boostback: return "Boostback";
                case TrajectoryStop.Entry: return "Entry";
                default: return "Haven";
            }
        }

        static double NominalGap(TrajectoryStop from, TrajectoryStop to)
        {
            if (from == TrajectoryStop.Liftoff && to == TrajectoryStop.MaxQ) return 50;
            if (from == TrajectoryStop.MaxQ && to == TrajectoryStop.Meco) return 90;
            if (from == TrajectoryStop.Meco && to == TrajectoryStop.Fairing) return 45;
            if (from == TrajectoryStop.Meco && to == TrajectoryStop.Seco) return 180;
            if (from == TrajectoryStop.Fairing && to == TrajectoryStop.Seco) return 150;
            if (from == TrajectoryStop.Seco && to == TrajectoryStop.Boostback) return 40;
            if (from == TrajectoryStop.Boostback && to == TrajectoryStop.Entry) return 200;
            if (from == TrajectoryStop.Entry && to == TrajectoryStop.Landing) return 80;
            return 60;
        }

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
