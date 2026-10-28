// Starter preview framing WITH the pad art (TestFlight 208, KH-1 First Hop: no yellow crown, no base plates, no pad
// circle, "the vehicle cropping the tower"). Build 204's preview framed the vehicle alone, from a heading that puts the
// launch tower almost straight behind it, so the crown fell off the top of the box, the plates hid behind the stack and
// the circle was out of frame. This solves a pose that keeps the vehicle AND the pad art (tower crown, base plates, pad
// circle) inside the preview box, from a heading where the vehicle does not stand in front of them, and Evaluate
// checks a pose against exactly the same points and rules (the EditMode/dotnet frame test calls it; the camera calls
// Solve). Pure math, no UnityEngine, same conventions as StarterPreviewFraming.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;

namespace Kestrel.Core.Designer
{
    /// <summary>Pad features the preview must show. Values match Kestrel.Pad.ShowKind.</summary>
    public enum PadFeature { TowerTop = 0, BasePlate = 1, PadCircle = 2 }

    /// <summary>One point of pad art in world metres (vehicle axis at the origin, deck top at y 0).</summary>
    public struct PadPoint
    {
        public PadFeature Kind; public double X, Y, Z;
        public PadPoint(PadFeature kind, double x, double y, double z) { Kind = kind; X = x; Y = y; Z = z; }
    }

    /// <summary>What <see cref="StarterPadFrame.Evaluate"/> found for one pose.</summary>
    public struct PadFrameReport
    {
        /// <summary>Points of each feature outside the preview box (kept <see cref="StarterPadFrame.ClearMargin"/> clear), or the vehicle itself.</summary>
        public int OutsideTop, OutsideBase, OutsideCircle, OutsideVehicle;
        /// <summary>Share of each feature's points hidden behind the vehicle cylinder (0..1).</summary>
        public double OccludedTop, OccludedBase, OccludedCircle;
        /// <summary>Projected vehicle height as a share of the preview box height.</summary>
        public double VehicleFill;
        /// <summary>Share of samples along the vehicle's axis hidden behind the tower (its crown's footprint, up to the crown): the tower must stand beside the stack, not in front of it.</summary>
        public double VehicleHiddenByTower;
        public int PointsTop, PointsBase, PointsCircle;

        /// <summary>Share of the pad circle's points inside the preview box.</summary>
        public double CircleInBox => PointsCircle > 0 ? 1.0 - (double)OutsideCircle / PointsCircle : 0;
        public bool InFrame => OutsideTop == 0 && OutsideBase == 0 && OutsideVehicle == 0 && CircleInBox >= StarterPadFrame.MinCircleInBox;
        public bool Visible => OccludedTop <= StarterPadFrame.MaxOccludedTop && OccludedBase <= StarterPadFrame.MaxOccludedBase && OccludedCircle <= StarterPadFrame.MaxOccludedCircle
            && VehicleHiddenByTower <= StarterPadFrame.MaxVehicleHiddenByTower;
        public bool Pass => PointsTop > 0 && PointsBase > 0 && InFrame && Visible;   // the circle is optional

        public string Describe() =>
            $"outside top/base/circle/vehicle {OutsideTop}/{OutsideBase}/{OutsideCircle}/{OutsideVehicle} of {PointsTop}/{PointsBase}/{PointsCircle} (circle in box {CircleInBox:0.00}), " +
            $"hidden behind the vehicle top {OccludedTop:0.00} base {OccludedBase:0.00} circle {OccludedCircle:0.00}, vehicle hidden by tower {VehicleHiddenByTower:0.00}, vehicle fill {VehicleFill:0.00}";
    }

    public static class StarterPadFrame
    {
        /// <summary>Share of the preview box kept clear on every edge when fitting (Solve) and when checking (Evaluate).</summary>
        public const double FitMargin = 0.05, ClearMargin = 0.02;
        /// <summary>Most points of a feature the vehicle may hide: the crown is on top of the stack and must show; the far plates and
        /// the far arc of the circle may go behind the stack, the near ones may not.</summary>
        public const double MaxOccludedTop = 0.25, MaxOccludedBase = 0.25, MaxOccludedCircle = 1.0;
        /// <summary>The pad circle is secondary (it is 0.4 x the surface radius, 13-14 m on a KH-1 pad whose vehicle is 4.6 m tall, so it cannot
        /// share a box with a large vehicle): no share of it is required in the box. It is reported, and a heading that shows more of it wins a tie.</summary>
        public const double MinCircleInBox = 0.0;
        /// <summary>None of the 15 samples (axis and both silhouette edges, five heights) may be behind the tower (a heading from the tower's side would "see" the stack through the lattice and fail this).</summary>
        public const double MaxVehicleHiddenByTower = 0.0;
        /// <summary>Looking down a little more than the vehicle-alone view so the circle and plates read as a pad, not a line.</summary>
        public const double PadElevationDeg = 20.0;
        /// <summary>The designer's rule: the rocket is framed large (the vehicle-alone solve targets 0.62), with the crown and plates still whole in the box.</summary>
        public const double MinVehicleFill = 0.5;
        /// <summary>Share of the equal-depth bound (vehicle height over crown top: the tallest the vehicle can be drawn with the crown whole in the box,
        /// before margins and perspective) that the solver must reach where <see cref="MinVehicleFill"/> is out of reach.</summary>
        public const double CeilingShare = 0.9;

        /// <summary>The fill a pose must reach: <see cref="MinVehicleFill"/>, or <see cref="CeilingShare"/> of the geometric ceiling when the tower is
        /// so tall next to the vehicle that 0.5 cannot be reached with the crown in the box (a 4.6 m KH-1 beside an 11 m crown: ceiling 0.42).</summary>
        public static double RequiredFill(StarterPreviewSubject s)
        {
            double top = 0; foreach (var p in s.Pad) if (p.Kind == PadFeature.TowerTop) top = Math.Max(top, p.Y);
            double h = s.YMax - s.YMin;
            if (!(top > 0) || !(h > 0)) return MinVehicleFill;
            return Math.Min(MinVehicleFill, CeilingShare * h / top);
        }

        /// <summary>Camera elevations tried (degrees) with each heading: a high one lets a far tower sit higher in the box, a low one shortens it.</summary>
        public static readonly double[] ElevationsDeg = { 3, 10, 20 };
        /// <summary>Heading search: every <see cref="HeadingStepDeg"/> degrees about the default heading, closest to it first.</summary>
        public const double HeadingStepDeg = 15.0;
        const int Ring = 16;

        /// <summary>A pad-frame point (the pad's +Z toward the tower) in the world: turned about +Y by <paramref name="yawDeg"/>, exactly as
        /// PadTower.Apply turns the Pad child (Unity yaw: local +Z maps to (sin yaw, cos yaw)), then moved by the pad origin.</summary>
        public static PadPoint World(int kind, double x, double y, double z, double yawDeg, double originX = 0, double originY = 0, double originZ = 0)
        {
            double t = yawDeg * Math.PI / 180.0, sn = Math.Sin(t), c = Math.Cos(t);
            return new PadPoint((PadFeature)kind, originX + x * c + z * sn, originY + y, originZ - x * sn + z * c);
        }

        /// <summary>Solved pose: the heading that shows every feature and leaves the vehicle biggest in the box.</summary>
        public static StarterPreviewPose Solve(StarterPreviewSubject s, HudBox box, double panelW, double panelH, double aspect, double vFovDeg)
        {
            var none = new StarterPreviewPose();
            if (!s.Usable || !s.HasPad || !(aspect > 0) || !(vFovDeg > 1) || vFovDeg >= 179) return none;
            if (!StarterPreviewFraming.BoxNdc(box, panelW, panelH, out double x0, out double x1, out double y0, out double y1)) return none;
            var basis = new StarterPreviewPose { ShiftX = 0.5 * (x0 + x1), ShiftY = 0.5 * (y0 + y1), ElevDeg = PadElevationDeg };
            var elevs = ElevationsDeg;
            StarterPreviewPose best = none; double bestScore = double.NegativeInfinity;
            foreach (double el in elevs)
            foreach (double h in Headings())
            {
                basis.HeadingDeg = h; basis.ElevDeg = el;
                var pose = Fit(s, basis, box, panelW, panelH, aspect, vFovDeg);
                if (!pose.Valid) continue;
                var rep = Evaluate(pose, s, box, panelW, panelH, aspect, vFovDeg);
                // A passing heading scores by the vehicle's size; one that fails scores below every pass by how badly it fails.
                double score = rep.Pass ? 1.0 + rep.VehicleFill + 0.01 * rep.CircleInBox : -(rep.OutsideTop + rep.OutsideBase + rep.OutsideVehicle)
                    - rep.VehicleHiddenByTower - Math.Max(0, MinCircleInBox - rep.CircleInBox) - Math.Max(0, rep.OccludedTop - MaxOccludedTop) - Math.Max(0, rep.OccludedBase - MaxOccludedBase) - Math.Max(0, rep.OccludedCircle - MaxOccludedCircle);
                if (score > bestScore + 1e-9) { bestScore = score; best = pose; }
            }
            return best;
        }

        /// <summary>The headings tried (degrees about the default heading): 0, then +/- step outward, so a tie keeps the closest to the default.</summary>
        public static IEnumerable<double> Headings()
        {
            yield return 0.0;
            for (double a = HeadingStepDeg; a < 180.0 - 1e-9; a += HeadingStepDeg) { yield return a; yield return -a; }
            yield return 180.0;
        }

        /// <summary>Smallest distance at which the vehicle and every pad point sit inside the box with <see cref="FitMargin"/> to spare,
        /// the aim moved so the whole set is centred in the box.</summary>
        static StarterPreviewPose Fit(StarterPreviewSubject s, StarterPreviewPose basis, HudBox box, double panelW, double panelH, double aspect, double vFovDeg)
        {
            StarterPreviewFraming.BoxNdc(box, panelW, panelH, out double bx0, out double bx1, out double by0, out double by1);
            double boxW = (bx1 - bx0) * (1 - 2 * FitMargin), boxH = (by1 - by0) * (1 - 2 * FitMargin);
            double tanH = Math.Tan(vFovDeg * Math.PI / 360.0);
            double cx = 0, cy = 0, cz = 0, rad = s.Radius; int nStrict = 0;
            foreach (var p in s.Pad)
            {
                rad = Math.Max(rad, Math.Sqrt(p.X * p.X + p.Z * p.Z));
                if (p.Kind == PadFeature.PadCircle) continue;   // the circle is fitted by share (MinCircleInBox), the rest whole
                cx += p.X; cz += p.Z; nStrict++;
            }
            if (nStrict > 0) { cx = 0.5 * (cx / nStrict); cz = 0.5 * (cz / nStrict); }
            cy = 0.5 * (s.YMin + Math.Max(s.YMax, MaxY(s)));
            double ax0 = cx, ay0 = cy, az0 = cz;

            double Over(double d, out StarterPreviewPose pose)
            {
                double ax = ax0, ay = ay0, az = az0;
                pose = StarterPreviewFraming.Compose(basis, ax, ay, az, d);
                for (int k = 0; k < 10; k++)
                {
                    Extent(pose, s, aspect, vFovDeg, false, out double ex0, out double ex1, out double ey0, out double ey1);
                    if (double.IsInfinity(ex0) || double.IsInfinity(ex1)) return double.PositiveInfinity;
                    double mx = 0.5 * (ex0 + ex1) - basis.ShiftX, my = 0.5 * (ey0 + ey1) - basis.ShiftY;
                    if (Math.Abs(mx) < 1e-7 && Math.Abs(my) < 1e-7) break;
                    if (!StarterPreviewFraming.Basis(pose, out double rx, out double ry, out double rz, out double ux, out double uy, out double uz)) return double.PositiveInfinity;
                    double dx = mx * d * tanH * aspect, dy = my * d * tanH;
                    ax += rx * dx + ux * dy; ay += ry * dx + uy * dy; az += rz * dx + uz * dy;
                    pose = StarterPreviewFraming.Compose(basis, ax, ay, az, d);
                }
                Extent(pose, s, aspect, vFovDeg, false, out double fx0, out double fx1, out double fy0, out double fy1);
                if (double.IsInfinity(fx0) || double.IsInfinity(fx1)) return double.PositiveInfinity;
                double over = Math.Max((fx1 - fx0) / boxW, (fy1 - fy0) / boxH);
                // Too close for the circle: report it as over so the distance grows until MinCircleInBox of its points are inside the (shrunk) box.
                if (CircleShare(pose, s, aspect, vFovDeg, bx0 + (bx1 - bx0) * FitMargin, bx1 - (bx1 - bx0) * FitMargin, by0 + (by1 - by0) * FitMargin, by1 - (by1 - by0) * FitMargin) < MinCircleInBox) over = Math.Max(over, 1.0 + 1e-6);
                return over;
            }

            double lo = Math.Max(1.0, rad), hi = Math.Max(lo * 2, (s.YMax - s.YMin + rad) * 2);
            for (int i = 0; i < 40 && Over(hi, out _) > 1; i++) { lo = hi; hi *= 2; }
            for (int i = 0; i < 48; i++)
            {
                double mid = 0.5 * (lo + hi);
                if (Over(mid, out _) > 1) lo = mid; else hi = mid;
            }
            Over(hi, out var best);
            best.Valid = true;
            return best;
        }

        static double MaxY(StarterPreviewSubject s)
        {
            double m = s.YMax;
            foreach (var p in s.Pad) m = Math.Max(m, p.Y);
            return m;
        }

        /// <summary>Share of the circle's points whose projection lies inside the NDC rectangle.</summary>
        static double CircleShare(StarterPreviewPose pose, StarterPreviewSubject s, double aspect, double vFovDeg, double x0, double x1, double y0, double y1)
        {
            int n = 0, inside = 0;
            foreach (var p in s.Pad)
            {
                if (p.Kind != PadFeature.PadCircle) continue;
                n++;
                if (StarterPreviewFraming.Project(pose, p.X, p.Y, p.Z, aspect, vFovDeg, out double nx, out double ny) && nx >= x0 && nx <= x1 && ny >= y0 && ny <= y1) inside++;
            }
            return n == 0 ? 1.0 : (double)inside / n;
        }

        /// <summary>Projected NDC bounds of the vehicle cylinder (both rings) and the crown and plate points (and the circle when asked); infinite when any is behind the camera.</summary>
        static void Extent(StarterPreviewPose pose, StarterPreviewSubject s, double aspect, double vFovDeg, bool withCircle, out double x0, out double x1, out double y0, out double y1)
        {
            StarterPreviewFraming.Extent(pose, s, aspect, vFovDeg, out x0, out x1, out y0, out y1);
            if (double.IsInfinity(x0) || double.IsInfinity(x1)) return;
            foreach (var p in s.Pad)
            {
                if (!withCircle && p.Kind == PadFeature.PadCircle) continue;
                if (!StarterPreviewFraming.Project(pose, p.X, p.Y, p.Z, aspect, vFovDeg, out double nx, out double ny)) { x0 = double.NegativeInfinity; x1 = double.PositiveInfinity; return; }
                x0 = Math.Min(x0, nx); x1 = Math.Max(x1, nx); y0 = Math.Min(y0, ny); y1 = Math.Max(y1, ny);
            }
        }

        /// <summary>Check a pose: every vehicle and pad point inside the preview box with <see cref="ClearMargin"/> to spare, and how much of
        /// each feature the vehicle cylinder hides from the camera. This is what the frame test asserts.</summary>
        public static PadFrameReport Evaluate(StarterPreviewPose pose, StarterPreviewSubject s, HudBox box, double panelW, double panelH, double aspect, double vFovDeg)
        {
            var r = new PadFrameReport();
            if (!pose.Valid || !s.HasPad || !StarterPreviewFraming.BoxNdc(box, panelW, panelH, out double bx0, out double bx1, out double by0, out double by1))
            {
                r.OutsideVehicle = 1; return r;
            }
            double mx = (bx1 - bx0) * ClearMargin, my = (by1 - by0) * ClearMargin;
            bool Inside(double nx, double ny) => nx >= bx0 + mx && nx <= bx1 - mx && ny >= by0 + my && ny <= by1 - my;
            StarterPreviewFraming.Extent(pose, s, aspect, vFovDeg, out double vx0, out double vx1, out double vy0, out double vy1);
            if (!(Inside(vx0, vy0) && Inside(vx1, vy1))) r.OutsideVehicle = 1;
            r.VehicleFill = (vy1 - vy0) / (by1 - by0);
            int hideTop = 0, hideBase = 0, hideCircle = 0;
            foreach (var p in s.Pad)
            {
                bool inside = StarterPreviewFraming.Project(pose, p.X, p.Y, p.Z, aspect, vFovDeg, out double nx, out double ny) && Inside(nx, ny);
                bool hidden = Hidden(pose, s, p);
                switch (p.Kind)
                {
                    case PadFeature.TowerTop: r.PointsTop++; if (!inside) r.OutsideTop++; if (hidden) hideTop++; break;
                    case PadFeature.BasePlate: r.PointsBase++; if (!inside) r.OutsideBase++; if (hidden) hideBase++; break;
                    default: r.PointsCircle++; if (!inside) r.OutsideCircle++; if (hidden) hideCircle++; break;
                }
            }
            r.VehicleHiddenByTower = VehicleHiddenByTower(pose, s);
            r.OccludedTop = r.PointsTop > 0 ? (double)hideTop / r.PointsTop : 0;
            r.OccludedBase = r.PointsBase > 0 ? (double)hideBase / r.PointsBase : 0;
            r.OccludedCircle = r.PointsCircle > 0 ? (double)hideCircle / r.PointsCircle : 0;
            return r;
        }

        /// <summary>Share of 15 samples on the vehicle (axis and both silhouette edges as seen from the camera, five heights) the tower hides from
        /// the camera. The tower is the prism over its footprint (the base plates, else the crown: circle through the furthest point around their
        /// centre) up to the highest crown point. 0 when the pad has no tower.</summary>
        public static double VehicleHiddenByTower(StarterPreviewPose pose, StarterPreviewSubject s)
        {
            PadFeature kind = PadFeature.BasePlate;
            int n = 0;
            foreach (var p in s.Pad) if (p.Kind == kind) n++;
            if (n == 0) { kind = PadFeature.TowerTop; foreach (var p in s.Pad) if (p.Kind == kind) n++; }
            if (n == 0) return 0;
            double cx = 0, cz = 0, top = 0;
            foreach (var p in s.Pad) { if (p.Kind == kind) { cx += p.X; cz += p.Z; } if (p.Kind == PadFeature.TowerTop) top = Math.Max(top, p.Y); }
            cx /= n; cz /= n;
            double rad = 0;
            foreach (var p in s.Pad) if (p.Kind == kind) rad = Math.Max(rad, Math.Sqrt((p.X - cx) * (p.X - cx) + (p.Z - cz) * (p.Z - cz)));
            if (top <= 0) top = s.YMax;
            // Horizontal direction perpendicular to the view, for the silhouette edges.
            double vx = pose.AimX - pose.PosX, vz = pose.AimZ - pose.PosZ, vl = Math.Sqrt(vx * vx + vz * vz);
            double sx = vl > 1e-9 ? -vz / vl : 1, sz = vl > 1e-9 ? vx / vl : 0;
            int hidden = 0;
            for (int k = 0; k < 5; k++)
            {
                double y = s.YMin + (s.YMax - s.YMin) * (0.1 + 0.2 * k);
                foreach (double e in new[] { -s.Radius, 0.0, s.Radius })
                    if (Blocked(pose, sx * e, y, sz * e, cx, cz, rad, 0, top)) hidden++;
            }
            return hidden / 15.0;
        }

        /// <summary>True when the segment from the camera to the point passes through the vehicle cylinder first.</summary>
        public static bool Hidden(StarterPreviewPose pose, StarterPreviewSubject s, PadPoint q) => Blocked(pose, q.X, q.Y, q.Z, 0, 0, s.Radius, s.YMin, s.YMax);

        /// <summary>True when the segment from the camera to (x, y, z) passes through the vertical cylinder (centre cx, cz, radius R, y0..y1) before it arrives.</summary>
        static bool Blocked(StarterPreviewPose pose, double x, double y, double z, double cx, double cz, double R, double y0, double y1)
        {
            double px = pose.PosX - cx, pz = pose.PosZ - cz;
            double dx = x - pose.PosX, dy = y - pose.PosY, dz = z - pose.PosZ;
            double a = dx * dx + dz * dz;
            double t0 = 0, t1 = 1 - 1e-6;                      // along the segment, camera to point
            if (a < 1e-12) { if (px * px + pz * pz > R * R) return false; }
            else
            {
                double b = 2 * (px * dx + pz * dz), c = px * px + pz * pz - R * R;
                double disc = b * b - 4 * a * c;
                if (disc < 0) return false;
                double sq = Math.Sqrt(disc);
                t0 = Math.Max(t0, (-b - sq) / (2 * a)); t1 = Math.Min(t1, (-b + sq) / (2 * a));
                if (t0 > t1) return false;
            }
            // Height window of the cylinder along the same parameter.
            if (Math.Abs(dy) < 1e-12) { if (pose.PosY < y0 || pose.PosY > y1) return false; }
            else
            {
                double ta = (y0 - pose.PosY) / dy, tb = (y1 - pose.PosY) / dy;
                t0 = Math.Max(t0, Math.Min(ta, tb)); t1 = Math.Min(t1, Math.Max(ta, tb));
            }
            return t0 <= t1;
        }
    }
}
