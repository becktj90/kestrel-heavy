// Starter rocket preview (TestFlight build 204: "When I change the design nothing changes with the rocket").
// The Starter screen drew the hangar-built vehicle, but the design camera fitted the whole pad (deck rim,
// tower, tower top x 1.06) into the band above the sheet, so the rocket was a sliver on a distant pad and a
// stage, fuel or engine change was invisible. This frames the vehicle alone in the sheet-free preview box
// (StarterFrame.Rocket): its projected height is TargetFill of the box, solved from the vehicle bounds, the
// vertical FOV and the screen aspect, with the principal point moved to the box centre (off-axis projection,
// no viewport rect, so nothing is letterboxed). Pure math, no UnityEngine: Kestrel.Core.Tests runs it under
// dotnet, and the Unity camera (Kestrel.Slice StarterPreviewCamera) only applies the pose.
// StarterSettingLoad: a Starter setting is a preset on the mission's gifted campaign winner, so it is loaded
// like a mission handoff (never refused by the credits gate, which silently kept the old rocket).
using System;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Json;

namespace Kestrel.Core.Designer
{
    /// <summary>Camera pose for the Starter preview. Unity convention: +Y up, the camera looks from Position at Aim.
    /// ShiftX/ShiftY are the NDC offsets of the principal point (DesignFraming.ShiftedProjection: m02 -= ShiftX, m12 -= ShiftY).</summary>
    public struct StarterPreviewPose
    {
        public double PosX, PosY, PosZ, AimX, AimY, AimZ, ShiftX, ShiftY, Distance;
        public bool Valid;
    }

    /// <summary>The vehicle the preview frames: a cylinder from YMin to YMax (metres, pad deck at 0) of radius Radius
    /// (the widest point: core, strap-ons, legs).</summary>
    public struct StarterPreviewSubject
    {
        public double YMin, YMax, Radius;
        public StarterPreviewSubject(double yMin, double yMax, double radius) { YMin = yMin; YMax = yMax; Radius = radius; }
        public double Height => YMax - YMin;
        public bool Usable => !(double.IsNaN(YMin) || double.IsNaN(YMax) || double.IsNaN(Radius) || double.IsInfinity(YMax) || double.IsInfinity(Radius)) && YMax - YMin > 0.5 && Radius > 0.05;
    }

    public static class StarterPreviewFraming
    {
        /// <summary>Projected vehicle height as a share of the preview box height (55-70% band, aimed at the middle).</summary>
        public const double TargetFill = 0.62, MinFill = 0.55, MaxFill = 0.70;
        /// <summary>A squat rocket in a narrow box is held to this share of the box width instead (then the height fill drops).</summary>
        public const double MaxWidthFill = 0.80;
        /// <summary>Same three-quarter heading as the Hangar rest pose (offset 0.9, -1.4), a little above the aim point.</summary>
        public const double HeadingX = 0.9, HeadingZ = -1.4, ElevationDeg = 6.0;
        /// <summary>Ease time constant (seconds). About 95% of a reframe lands in 3 x this; Reduce Motion snaps.</summary>
        public const double EaseSeconds = 0.12;
        /// <summary>Samples around the cylinder for the projected extent.</summary>
        const int Ring = 16;

        /// <summary>The preview box (points, y down from the top of a panelW x panelH screen) in NDC (y up).</summary>
        public static bool BoxNdc(HudBox box, double panelW, double panelH, out double x0, out double x1, out double y0, out double y1)
        {
            x0 = x1 = y0 = y1 = 0;
            if (!box.Live || !(panelW > 1) || !(panelH > 1)) return false;
            x0 = 2.0 * box.X / panelW - 1.0; x1 = 2.0 * box.Right / panelW - 1.0;
            y1 = 1.0 - 2.0 * box.Y / panelH; y0 = 1.0 - 2.0 * box.Bottom / panelH;
            return x1 - x0 > 1e-3 && y1 - y0 > 1e-3;
        }

        /// <summary>Unit vector from the aim point to the camera.</summary>
        public static void Heading(out double dx, out double dy, out double dz)
        {
            double h = Math.Sqrt(HeadingX * HeadingX + HeadingZ * HeadingZ);
            double e = ElevationDeg * Math.PI / 180.0;
            dx = HeadingX / h * Math.Cos(e); dy = Math.Sin(e); dz = HeadingZ / h * Math.Cos(e);
        }

        /// <summary>Pose that puts the subject's projected height at TargetFill of the box (or its width at MaxWidthFill,
        /// whichever is tighter), centred in the box. aspect = screen width / height; vFovDeg = Camera.fieldOfView.</summary>
        public static StarterPreviewPose Solve(StarterPreviewSubject s, HudBox box, double panelW, double panelH, double aspect, double vFovDeg)
        {
            var pose = new StarterPreviewPose();
            if (!s.Usable || !(aspect > 0) || !(vFovDeg > 1) || vFovDeg >= 179) return pose;
            if (!BoxNdc(box, panelW, panelH, out double x0, out double x1, out double y0, out double y1)) return pose;
            pose.ShiftX = 0.5 * (x0 + x1); pose.ShiftY = 0.5 * (y0 + y1);
            double boxH = y1 - y0, boxW = x1 - x0;
            double aimY = 0.5 * (s.YMin + s.YMax);
            double tanH = Math.Tan(vFovDeg * Math.PI / 360.0);
            // Overshoot: >1 means too big. Height and width fills both shrink as the camera backs off.
            double Over(double d, ref double ay)
            {
                for (int k = 0; k < 8; k++)
                {
                    Extent(Compose(pose, ay, d), s, aspect, vFovDeg, out _, out _, out double ey0, out double ey1);
                    double mid = 0.5 * (ey0 + ey1) - pose.ShiftY;
                    if (Math.Abs(mid) < 1e-6) break;
                    ay += mid * d * tanH;   // NDC -> metres on the plane through the aim point
                }
                Extent(Compose(pose, ay, d), s, aspect, vFovDeg, out double fx0, out double fx1, out double fy0, out double fy1);
                return Math.Max((fy1 - fy0) / boxH / TargetFill, (fx1 - fx0) / boxW / MaxWidthFill);
            }
            double lo = Math.Max(1.0, s.Radius * 2.5), hi = Math.Max(lo * 2, s.Height * 2);
            double ayLo = aimY;
            if (Over(lo, ref ayLo) < 1)
            {
                // Even this close it is small (a tiny subject): stay at the closest safe distance.
                pose = Compose(pose, ayLo, lo); pose.Valid = true; return pose;
            }
            double ayHi = aimY;
            for (int i = 0; i < 40 && Over(hi, ref ayHi) > 1; i++) { lo = hi; hi *= 2; ayHi = aimY; }
            double ay = aimY;
            for (int i = 0; i < 48; i++)
            {
                double mid = 0.5 * (lo + hi);
                double a = aimY;
                if (Over(mid, ref a) > 1) lo = mid; else { hi = mid; ay = a; }
            }
            Over(hi, ref ay);
            pose = Compose(pose, ay, hi);
            pose.Valid = true;
            return pose;
        }

        static StarterPreviewPose Compose(StarterPreviewPose basis, double aimY, double distance)
        {
            Heading(out double dx, out double dy, out double dz);
            var p = basis;
            p.AimX = 0; p.AimY = aimY; p.AimZ = 0; p.Distance = distance;
            p.PosX = dx * distance; p.PosY = aimY + dy * distance; p.PosZ = dz * distance;
            return p;
        }

        /// <summary>NDC of a world point for the pose (Unity view: right = up x forward). False behind the camera.</summary>
        public static bool Project(StarterPreviewPose p, double x, double y, double z, double aspect, double vFovDeg, out double nx, out double ny)
        {
            double fx = p.AimX - p.PosX, fy = p.AimY - p.PosY, fz = p.AimZ - p.PosZ;
            double fl = Math.Sqrt(fx * fx + fy * fy + fz * fz);
            nx = ny = double.PositiveInfinity;
            if (!(fl > 1e-9)) return false;
            fx /= fl; fy /= fl; fz /= fl;
            // right = cross(up, f) with up = +Y: (fz, 0, -fx)
            double rx = fz, ry = 0, rz = -fx;
            double rl = Math.Sqrt(rx * rx + rz * rz);
            if (!(rl > 1e-9)) return false;
            rx /= rl; rz /= rl;
            // up' = cross(f, right)
            double ux = fy * rz - fz * ry, uy = fz * rx - fx * rz, uz = fx * ry - fy * rx;
            double vx = x - p.PosX, vy = y - p.PosY, vz = z - p.PosZ;
            double cz = vx * fx + vy * fy + vz * fz;
            if (!(cz > 1e-4)) return false;
            double cx = vx * rx + vy * ry + vz * rz, cy = vx * ux + vy * uy + vz * uz;
            double t = Math.Tan(vFovDeg * Math.PI / 360.0);
            nx = cx / (cz * t * aspect) + p.ShiftX;
            ny = cy / (cz * t) + p.ShiftY;
            return true;
        }

        /// <summary>Projected NDC bounds of the subject cylinder (both end rings).</summary>
        public static void Extent(StarterPreviewPose p, StarterPreviewSubject s, double aspect, double vFovDeg, out double x0, out double x1, out double y0, out double y1)
        {
            x0 = y0 = double.PositiveInfinity; x1 = y1 = double.NegativeInfinity;
            for (int end = 0; end < 2; end++)
            {
                double y = end == 0 ? s.YMin : s.YMax;
                for (int i = 0; i < Ring; i++)
                {
                    double a = i * 2 * Math.PI / Ring;
                    if (!Project(p, s.Radius * Math.Cos(a), y, s.Radius * Math.Sin(a), aspect, vFovDeg, out double nx, out double ny))
                    { x0 = y0 = double.NegativeInfinity; x1 = y1 = double.PositiveInfinity; return; }
                    x0 = Math.Min(x0, nx); x1 = Math.Max(x1, nx); y0 = Math.Min(y0, ny); y1 = Math.Max(y1, ny);
                }
            }
        }

        /// <summary>Projected height of the subject as a share of the box height, and whether it sits fully inside the
        /// box with <paramref name="marginFraction"/> of the box kept clear on every edge.</summary>
        public static double Fill(StarterPreviewPose p, StarterPreviewSubject s, HudBox box, double panelW, double panelH, double aspect, double vFovDeg, double marginFraction, out bool inside)
        {
            inside = false;
            if (!BoxNdc(box, panelW, panelH, out double bx0, out double bx1, out double by0, out double by1)) return 0;
            Extent(p, s, aspect, vFovDeg, out double x0, out double x1, out double y0, out double y1);
            double mx = (bx1 - bx0) * marginFraction, my = (by1 - by0) * marginFraction;
            inside = x0 >= bx0 + mx && x1 <= bx1 - mx && y0 >= by0 + my && y1 <= by1 - my;
            return (y1 - y0) / (by1 - by0);
        }

        /// <summary>Exponential ease toward the target (frame-rate independent). Reduce Motion, a first frame or a
        /// non-finite value snaps.</summary>
        public static double Ease(double current, double target, double dt, bool reduceMotion)
        {
            if (reduceMotion || double.IsNaN(current) || double.IsInfinity(current)) return target;
            if (!(dt > 0)) return current;
            double k = 1 - Math.Exp(-Math.Min(dt, 0.1) / EaseSeconds);
            double next = current + (target - current) * k;
            return Math.Abs(target - next) < 1e-3 * Math.Max(1, Math.Abs(target)) ? target : next;   // the last 0.1% lands
        }

        /// <summary>One eased step of the whole pose (aim, distance and principal point; the heading is fixed).</summary>
        public static StarterPreviewPose Step(StarterPreviewPose now, StarterPreviewPose target, double dt, bool reduceMotion)
        {
            if (!target.Valid) return now;
            if (!now.Valid || reduceMotion) return target;
            var p = target;
            p.ShiftX = Ease(now.ShiftX, target.ShiftX, dt, false);
            p.ShiftY = Ease(now.ShiftY, target.ShiftY, dt, false);
            double ay = Ease(now.AimY, target.AimY, dt, false);
            double d = Ease(now.Distance, target.Distance, dt, false);
            var c = Compose(p, ay, d);
            c.Valid = true;
            return c;
        }

        public static bool Same(StarterPreviewPose a, StarterPreviewPose b) =>
            a.Valid == b.Valid && Math.Abs(a.AimY - b.AimY) < 1e-6 && Math.Abs(a.Distance - b.Distance) < 1e-6
            && Math.Abs(a.ShiftX - b.ShiftX) < 1e-9 && Math.Abs(a.ShiftY - b.ShiftY) < 1e-9;
    }

    /// <summary>Load a Starter setting into the design session. The setting is a preset on the mission's campaign winner,
    /// which is handed over (docs/ECONOMY.md: a mission starter is a gift), so the credits gate must not refuse it: a
    /// refused HangarSession.Load returns silently and the pad, Launch and the flight kept the previous rocket while
    /// the Starter readout described the new one. <paramref name="regift"/> re-baselines the wallet on the loaded design
    /// (CreditsWallet.CatchUp: the balance never moves).</summary>
    public static class StarterSettingLoad
    {
        public static bool Apply(HangarSession session, JsonObject design, string name, Action<JsonObject> regift = null)
        {
            if (session == null || design == null) return false;
            var gate = session.Afford;
            session.Afford = null;
            try { session.Load(design, null, name); }
            finally { session.Afford = gate; }
            regift?.Invoke(session.Design);
            return true;
        }
    }
}
