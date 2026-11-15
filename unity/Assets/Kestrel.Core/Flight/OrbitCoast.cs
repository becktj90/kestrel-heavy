// The climax after a clean final cutoff: the real coast to the apex (a hop) or along the new orbit, played as a short
// time-warp before the result card, with one callout ("Orbit!" or "Apogee!"). No Unity types: the track comes from the
// same AscentSim that flew the ascent (AscentSim.CoastTrack), so what the player watches is the propagated state, not a
// canned animation. The player can tap to skip it; Reduce Motion skips it outright (the caller decides).
using System;
using System.Collections.Generic;
using Kestrel.Core.Physics;

namespace Kestrel.Core.Flight
{
    public sealed class OrbitCoast
    {
        public const double MinWallSeconds = 3.0;
        public const double MaxWallSeconds = 5.0;
        public const double DefaultWallSeconds = 4.0;
        /// <summary>A coast shorter than this (sim seconds) is not worth a warp: the vehicle is already at its apex.</summary>
        public const double MinSpanSeconds = 2.0;
        /// <summary>An orbit whose apogee is nearer than this goes a quarter of the way round instead.</summary>
        public const double MinOrbitSpanSeconds = 120.0;
        public const double HopMaxSpanSeconds = 1800.0;
        public const string CalloutOrbit = "Orbit!";
        public const string CalloutApogee = "Apogee!";

        readonly List<CoastState> track;

        public bool IsOrbit { get; }
        public string Callout { get; }
        public string Detail { get; }
        /// <summary>Sim clock at cutoff, seconds.</summary>
        public double StartT { get; }
        /// <summary>Sim seconds the coast covers.</summary>
        public double SpanSeconds { get; }
        /// <summary>Wall-clock seconds the coast plays for (3 to 5).</summary>
        public double WallSeconds { get; }
        /// <summary>Altitude the coast ends at, metres.</summary>
        public double EndAltM { get; }
        /// <summary>Average warp: sim seconds per wall second.</summary>
        public double Warp => WallSeconds > 0 ? SpanSeconds / WallSeconds : 0;
        public int Samples => track.Count;

        OrbitCoast(List<CoastState> track, bool orbit, double wall, string detail)
        {
            this.track = track;
            IsOrbit = orbit;
            Callout = orbit ? CalloutOrbit : CalloutApogee;
            StartT = track[0].t;
            SpanSeconds = track[track.Count - 1].t - StartT;
            WallSeconds = Clamp(wall, MinWallSeconds, MaxWallSeconds);
            var e = track[track.Count - 1];
            EndAltM = Math.Sqrt(e.x * e.x + e.y * e.y) - Gravity.R_EARTH;
            Detail = detail;
        }

        /// <summary>
        /// The coast for a run that just ended, or null when there is nothing to play: no result, a failure, a crash, a
        /// timeout or a run that never cut off, or a vehicle already at (or past) its apex.
        /// </summary>
        public static OrbitCoast Plan(AscentSim sim, SimResult result, double wallSeconds = DefaultWallSeconds)
        {
            if (sim == null || result == null || result.failure != null) return null;
            if (sim.Thrusting) return null;
            bool orbit = result.outcome == "orbit";
            if (!orbit && result.outcome != "suborbital") return null;
            List<CoastState> track;
            var orb = result.orbit;
            if (orbit)
            {
                double period = orb.period > 0 && !double.IsInfinity(orb.period) ? orb.period : 5400;
                track = sim.CoastTrack(period / 2, Step(period / 2), true);
                if (Span(track) < MinOrbitSpanSeconds) track = sim.CoastTrack(period / 4, Step(period / 4), false);
            }
            else
            {
                track = sim.CoastTrack(HopMaxSpanSeconds, 0.25, true);
                if (track.Count > 1) track = Resample(track, Step(Span(track)));
            }
            if (track == null || track.Count < 2 || Span(track) < MinSpanSeconds) return null;
            string detail;
            if (orbit) detail = $"In orbit · {Km(orb.periAlt)} by {Km(orb.apoAlt)} km";
            else
            {
                var e = track[track.Count - 1];
                detail = $"Top of the climb · {Km(Math.Sqrt(e.x * e.x + e.y * e.y) - Gravity.R_EARTH)} km";
            }
            return new OrbitCoast(track, orbit, wallSeconds, detail);
        }

        static double Span(List<CoastState> t) => t == null || t.Count < 2 ? 0 : t[t.Count - 1].t - t[0].t;
        static double Step(double span) => Math.Max(0.05, span / 240.0);
        static string Km(double m) => (m / 1000.0).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        static double Clamp(double v, double lo, double hi) => double.IsNaN(v) ? lo : v < lo ? lo : v > hi ? hi : v;

        static List<CoastState> Resample(List<CoastState> src, double every)
        {
            var o = new List<CoastState> { src[0] };
            double next = src[0].t + every;
            for (int i = 1; i < src.Count - 1; i++)
                if (src[i].t >= next - 1e-9) { o.Add(src[i]); next += every; }
            o.Add(src[src.Count - 1]);
            return o;
        }

        /// <summary>
        /// Wall seconds into the coast -> fraction of the coast's sim span. Eased in and out (sine), so the warp spins up
        /// off the cutoff and settles into the apex instead of jumping.
        /// </summary>
        public double Progress(double wall)
        {
            if (!(WallSeconds > 0)) return 1;
            double u = Clamp(wall / WallSeconds, 0, 1);
            return 0.5 - 0.5 * Math.Cos(Math.PI * u);
        }

        public bool Finished(double wall) => !(wall < WallSeconds);

        /// <summary>Largest step one frame may add to the coast's wall clock (a hitch or a very slow frame).</summary>
        public const double MaxStepSeconds = 0.5;

        /// <summary>
        /// One frame of the coast's wall clock. The coast is a propagated track (no integration), so it follows the real
        /// clock: the larger of the scaled and unscaled frame time, capped at <see cref="MaxStepSeconds"/>. The climax then
        /// ends after its WallSeconds of real time at any frame rate down to 2 fps, and after at most
        /// ceil(WallSeconds / MaxStepSeconds) frames below that. (#431: with the ascent's 0.1 s hitch clamp a slow CI
        /// player took 3.5 s of coast past a 15 s wait.) NaN or negative times count as zero; an endless frame counts as one capped step.
        /// </summary>
        public static double Advance(double wall, double frameSeconds, double realFrameSeconds)
        {
            double a = frameSeconds > 0 ? frameSeconds : 0;
            double b = realFrameSeconds > 0 ? realFrameSeconds : 0;
            return wall + Math.Min(Math.Max(a, b), MaxStepSeconds);
        }

        /// <summary>
        /// How much of a stall the climax watchdog may count between two <c>Time.unscaledTime</c> samples.
        /// Same cap as <see cref="Advance"/>: a hitch longer than <see cref="MaxStepSeconds"/> must not spend the
        /// whole <see cref="MaxWallSeconds"/> + 2 budget in one callback. That ended a live coast
        /// (<c>CoastEnd=watchdog</c>) while <c>CoastWall</c> was still under 1.5 s (PlayMode
        /// GoodRocket_FirstHop_CoastsToApogee_BeforeTheReport, runs 43043315690 and 43047628485).
        /// The first sample only arms the clock. A repeated sample at the same timestamp counts nothing, so
        /// FailureHost and SliceApp can both call in one frame.
        /// </summary>
        public static double WatchdogCredit(double markedAt, double unscaledTime, out double newMark)
        {
            if (double.IsNaN(unscaledTime)) { newMark = markedAt; return 0; }
            newMark = unscaledTime;
            if (double.IsNaN(markedAt)) return 0;
            double gap = unscaledTime - markedAt;
            if (!(gap > 0)) return 0;
            return gap > MaxStepSeconds ? MaxStepSeconds : gap;
        }

        /// <summary>State at <paramref name="wall"/> seconds into the coast: sim time and the planar inertial state, interpolated
        /// (Hermite on position, linear on velocity) between propagated samples.</summary>
        public CoastState At(double wall)
        {
            double t = StartT + Progress(wall) * SpanSeconds;
            if (t <= track[0].t) return track[0];
            int n = track.Count;
            if (t >= track[n - 1].t) return track[n - 1];
            int lo = 0, hi = n - 1;
            while (hi - lo > 1) { int mid = (lo + hi) / 2; if (track[mid].t <= t) lo = mid; else hi = mid; }
            var a = track[lo]; var b = track[hi];
            double h = b.t - a.t, s = h > 0 ? (t - a.t) / h : 0;
            double h00 = 2 * s * s * s - 3 * s * s + 1, h10 = s * s * s - 2 * s * s + s, h01 = -2 * s * s * s + 3 * s * s, h11 = s * s * s - s * s;
            return new CoastState
            {
                t = t,
                x = h00 * a.x + h10 * h * a.vx + h01 * b.x + h11 * h * b.vx,
                y = h00 * a.y + h10 * h * a.vy + h01 * b.y + h11 * h * b.vy,
                vx = a.vx + (b.vx - a.vx) * s,
                vy = a.vy + (b.vy - a.vy) * s,
            };
        }

        public CoastState End => track[track.Count - 1];
    }
}
