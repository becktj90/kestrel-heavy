// The Below view's pad hold, solved from the framing (TestFlight 202 / t455, #348). Pure: FlightViewPose calls Solve
// every frame with the same pad inputs, so the hold is a fixed point while the rocket stands on the pad (no per-frame
// snapping), and the EditMode tests call the same evaluation helpers on any camera.
//
// The #332 hold stood 0.15 m UNDER the deck top, 4 m outside a deck rim that PadGround.DeckRadius grows to every
// member touching the deck (light masts, the farm), so on a First Hop pad it sat ~60 m out pitched 3.9 deg: the deck
// plinth wall filled the bottom ~45% of the frame and the rocket was a nose over the deck edge.
//
// Now: the camera stands ON the deck, Eye(h) = 0.6..1.5 m above the deck top, on the side away from the tower and
// turned off the tower line (BelowBearingsDeg), with a wider BelowFovDeg. The distance is solved so the stack's near
// base rim sits at BaseNdc (the deck and the trench lip under it) and its near top rim no higher than TopNdc, the base
// is seen at most MaxBaseDipDeg below eye level (so the pitch stays up and the deck stays small), the skin is at
// least MinSkinDiameters away and the stack's width fits the horizontal FOV. Pitch follows: the base pinned at
// BaseNdc. If the spot is within near-plane reach of pad structure the camera is pushed out along its ray; a bearing
// whose line of sight is blocked by structure gives way to the next. The deck radius is not an input.
using System;
using System.Collections.Generic;

namespace Kestrel.Core.Flight
{
    /// <summary>A pad member as the camera sees it: a capsule (segment A-B, radius) in world space.</summary>
    public struct PoseObstacle
    {
        public PoseVec A, B;
        public double Radius;
        public string Id;
        public PoseObstacle(PoseVec a, PoseVec b, double radius, string id = null) { A = a; B = b; Radius = radius; Id = id; }
    }

    public struct BelowShot
    {
        public PoseVec Camera, Look;
        public double FovDeg, PitchDeg, Distance, Eye, BearingDeg, Visible;
        /// <summary>Metres the spot moved out along its ray to clear structure (0: none).</summary>
        public double Pushed;
    }

    public static class BelowFraming
    {
        /// <summary>Vertical field of view of the pad hold (deg). FlightViewPose blends to it with the hold weight.</summary>
        public const double FovDeg = 68;
        /// <summary>Viewport y (NDC, -1 bottom .. 1 top) of the stack's near base rim, and the highest its near top rim may sit.</summary>
        public const double BaseNdc = -0.74, TopNdc = 0.72;
        /// <summary>The base is never seen more than this far below eye level (deg): the pitch stays up, the deck small.</summary>
        public const double MaxBaseDipDeg = 5;
        /// <summary>Camera height above the deck top: EyeMin + EyePerMetre x stack height, clamped to EyeMax (m).</summary>
        public const double EyeMin = 0.6, EyeMax = 1.5, EyePerMetre = 0.01;
        /// <summary>Skin to camera, at least this many stack diameters (and 1.5 m).</summary>
        public const double MinSkinDiameters = 1.5;
        /// <summary>The stack's width keeps this NDC margin on each side.</summary>
        public const double SideMargin = 0.12;
        /// <summary>Camera near plane (SliceApp: 0.5 m).</summary>
        public const double NearClip = 0.5;
        /// <summary>A bearing whose sight of the stack is at least this clear of structure is taken as is.</summary>
        public const double WantVisible = 0.95;
        public const double DefaultAspect = 9.0 / 19.5;
        public const double PushStep = 0.25, MaxPush = 30;

        public static double Eye(double h) => Clamp(EyeMin + EyePerMetre * Math.Max(0, h), EyeMin, EyeMax);

        /// <summary>Horizontal distance from the stack axis that frames a stack of height h and radius r.</summary>
        public static double Distance(double h, double r, double eye, double fovDeg, double aspect)
        {
            double t = Math.Tan(0.5 * fovDeg * Math.PI / 180);
            double tx = t * (aspect > 0.05 ? aspect : DefaultAspect);
            double span = Math.Atan(TopNdc * t) - Math.Atan(BaseNdc * t);
            double minSkin = Math.Max(1.5, MinSkinDiameters * 2 * r);
            double lo = r + minSkin, hi = Math.Max(lo + 1, 200 * Math.Max(1, h));
            // The span from the near base rim to the near top rim shrinks with distance: smallest distance that fits.
            if (Span(lo, h, r, eye) > span)
            {
                for (int i = 0; i < 80; i++)
                {
                    double mid = 0.5 * (lo + hi);
                    if (Span(mid, h, r, eye) > span) lo = mid; else hi = mid;
                }
                lo = hi;
            }
            double dip = r + eye / Math.Tan(MaxBaseDipDeg * Math.PI / 180);
            double wide = r + r / Math.Max(1e-3, tx * (1 - SideMargin));
            return Math.Max(lo, Math.Max(dip, wide));
        }

        static double Span(double d, double h, double r, double eye)
        {
            double near = Math.Max(1e-3, d - r);
            return Math.Atan2(h - eye, near) + Math.Atan2(eye, near);
        }

        /// <summary>Pitch (deg) that puts the near base rim at BaseNdc from distance d.</summary>
        public static double Pitch(double d, double r, double eye, double fovDeg)
        {
            double t = Math.Tan(0.5 * fovDeg * Math.PI / 180);
            double dip = Math.Atan2(eye, Math.Max(1e-3, d - r));
            return (Math.Atan(-BaseNdc * t) - dip) * 180 / Math.PI;
        }

        /// <summary>
        /// The hold for a stack standing on the deck: base centre (axisX, deckY, axisZ), height h, max radius r.
        /// <paramref name="bearingsWorldDeg"/> in preference order (world yaw, 0 = +Z); <paramref name="trenchSepDeg"/> says how
        /// far each is off the trench line (deg). Obstacles may be null.
        /// </summary>
        public static BelowShot Solve(PoseVec baseCentre, double h, double r, double aspect, IList<double> bearingsWorldDeg,
            IList<double> trenchSepDeg, IList<PoseObstacle> obstacles, double trenchClearDeg = 40)
        {
            h = Math.Max(1, h); r = Math.Max(0.1, r);
            double eye = Eye(h);
            double d0 = Distance(h, r, eye, FovDeg, aspect);
            BelowShot best = default; bool have = false; int bestRank = int.MaxValue;
            for (int i = 0; i < bearingsWorldDeg.Count; i++)
            {
                double sep = trenchSepDeg != null && i < trenchSepDeg.Count ? trenchSepDeg[i] : 90;
                if (sep < 25) continue;   // never down the trench
                var s = At(baseCentre, h, r, eye, d0, bearingsWorldDeg[i], aspect, obstacles);
                // Rank: clear sight and off the trench line first, then clear sight, then the clearest.
                int rank = s.Visible >= WantVisible ? (sep >= trenchClearDeg ? 0 : 1) : 2;
                if (!have || rank < bestRank || (rank == bestRank && rank == 2 && s.Visible > best.Visible + 1e-9))
                {
                    best = s; bestRank = rank; have = true;
                    if (rank == 0) break;
                }
            }
            if (!have) best = At(baseCentre, h, r, eye, d0, bearingsWorldDeg.Count > 0 ? bearingsWorldDeg[0] : 180, aspect, obstacles);
            return best;
        }

        static BelowShot At(PoseVec b, double h, double r, double eye, double d0, double bearingDeg, double aspect, IList<PoseObstacle> obstacles)
        {
            double a = bearingDeg * Math.PI / 180;
            var dir = new PoseVec(Math.Sin(a), 0, Math.Cos(a));
            double reach = NearReach(FovDeg, aspect);
            double d = d0, pushed = 0;
            PoseVec cam = new PoseVec(b.X, b.Y + eye, b.Z) + dir * d;
            while (obstacles != null && pushed < MaxPush && Clearance(cam, obstacles) < reach)
            {
                d += PushStep; pushed += PushStep;
                cam = new PoseVec(b.X, b.Y + eye, b.Z) + dir * d;
            }
            double pitch = Pitch(d, r, eye, FovDeg);
            var look = new PoseVec(b.X, cam.Y + d * Math.Tan(pitch * Math.PI / 180), b.Z);
            return new BelowShot
            {
                Camera = cam, Look = look, FovDeg = FovDeg, PitchDeg = pitch, Distance = d, Eye = eye, BearingDeg = bearingDeg,
                Visible = Visibility(cam, b, h, r, obstacles), Pushed = pushed,
            };
        }

        // ---- evaluation (also used by the EditMode tests on any camera) ------------------------------------------

        /// <summary>Distance from the eye to the farthest corner of the near plane: anything closer can be near-clipped.</summary>
        public static double NearReach(double fovDeg, double aspect)
        {
            double hy = NearClip * Math.Tan(0.5 * fovDeg * Math.PI / 180), hx = hy * Math.Max(aspect, 0.05);
            return Math.Sqrt(NearClip * NearClip + hx * hx + hy * hy);
        }

        /// <summary>Smallest gap (m) from the eye to any obstacle surface. MaxValue when there are none.</summary>
        public static double Clearance(PoseVec eye, IList<PoseObstacle> obstacles)
        {
            double best = double.MaxValue;
            if (obstacles == null) return best;
            foreach (var o in obstacles)
            {
                double g = PointSegment(eye, o.A, o.B) - o.Radius;
                if (g < best) best = g;
            }
            return best;
        }

        /// <summary>Share of silhouette sample points on the stack (11 heights x 3 across, facing the eye) with a clear
        /// line from the eye: no obstacle capsule crosses it.</summary>
        public static double Visibility(PoseVec eye, PoseVec baseCentre, double h, double r, IList<PoseObstacle> obstacles) =>
            Visibility(eye, baseCentre, h, r, obstacles, out _);

        public static double Visibility(PoseVec eye, PoseVec baseCentre, double h, double r, IList<PoseObstacle> obstacles, out string blocker)
        {
            blocker = null;
            if (obstacles == null || obstacles.Count == 0) return 1;
            var toEye = new PoseVec(eye.X - baseCentre.X, 0, eye.Z - baseCentre.Z);
            var fwd = toEye.Length > 1e-6 ? toEye.Unit : new PoseVec(0, 0, -1);
            var side = new PoseVec(fwd.Z, 0, -fwd.X);
            int seen = 0, n = 0;
            for (int k = 0; k <= 10; k++)
            {
                double y = baseCentre.Y + h * Math.Min(0.99, Math.Max(0.01, k / 10.0));
                for (int j = -1; j <= 1; j++)
                {
                    var p = new PoseVec(baseCentre.X, y, baseCentre.Z) + side * (0.85 * r * j) + fwd * (j == 0 ? r : 0.5 * r);
                    n++;
                    bool clear = true;
                    foreach (var o in obstacles)
                        if (SegmentSegment(eye, p, o.A, o.B) < o.Radius) { clear = false; blocker = blocker ?? o.Id; break; }
                    if (clear) seen++;
                }
            }
            return n > 0 ? (double)seen / n : 1;
        }

        /// <summary>Pinhole projection matching Unity's Camera.LookAt(look, up) + fieldOfView/aspect. x, y in NDC.</summary>
        public static bool Project(PoseVec eye, PoseVec look, double fovDeg, double aspect, PoseVec p, out double x, out double y, out double depth)
        {
            var f = (look - eye).Unit;
            var right = Cross(PoseVec.Up, f); right = right.Length > 1e-9 ? right.Unit : new PoseVec(1, 0, 0);
            var up = Cross(f, right);
            var d = p - eye;
            depth = PoseVec.Dot(d, f);
            double t = Math.Tan(0.5 * fovDeg * Math.PI / 180);
            if (depth <= 1e-6) { x = y = double.NaN; return false; }
            x = PoseVec.Dot(d, right) / (depth * t * aspect);
            y = PoseVec.Dot(d, up) / (depth * t);
            return true;
        }

        /// <summary>NDC bounds of a cylinder (base centre, height, radius) sampled on 9 rings of 24 points. False if any
        /// sample is behind the eye.</summary>
        public static bool StackBounds(PoseVec eye, PoseVec look, double fovDeg, double aspect, PoseVec baseCentre, double h, double r,
            out double minX, out double maxX, out double minY, out double maxY)
        {
            minX = minY = double.MaxValue; maxX = maxY = double.MinValue;
            bool ok = true;
            for (int k = 0; k <= 8; k++)
                for (int i = 0; i < 24; i++)
                {
                    double a = i * Math.PI * 2 / 24;
                    var p = new PoseVec(baseCentre.X + r * Math.Cos(a), baseCentre.Y + h * k / 8.0, baseCentre.Z + r * Math.Sin(a));
                    if (!Project(eye, look, fovDeg, aspect, p, out double x, out double y, out _)) { ok = false; continue; }
                    minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                }
            return ok;
        }

        /// <summary>Share of the viewport whose ray lands on the deck top (radius deckR around deckCentre) or the plinth
        /// wall under its rim (down to groundY), on a cols x rows grid of pixel centres.</summary>
        public static double DeckCover(PoseVec eye, PoseVec look, double fovDeg, double aspect, PoseVec deckCentre, double deckR, double groundY, int cols = 48, int rows = 96)
        {
            var f = (look - eye).Unit;
            var right = Cross(PoseVec.Up, f); right = right.Length > 1e-9 ? right.Unit : new PoseVec(1, 0, 0);
            var up = Cross(f, right);
            double t = Math.Tan(0.5 * fovDeg * Math.PI / 180);
            int hit = 0;
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    double x = ((i + 0.5) / cols * 2 - 1) * t * aspect, y = ((j + 0.5) / rows * 2 - 1) * t;
                    var ray = (f + right * x + up * y).Unit;
                    if (HitsDeck(eye, ray, deckCentre, deckR, groundY)) hit++;
                }
            return (double)hit / (cols * rows);
        }

        static bool HitsDeck(PoseVec o, PoseVec d, PoseVec c, double R, double groundY)
        {
            // Deck top (from above).
            if (d.Y < -1e-9 && o.Y > c.Y)
            {
                double s = (c.Y - o.Y) / d.Y;
                double x = o.X + d.X * s - c.X, z = o.Z + d.Z * s - c.Z;
                if (x * x + z * z <= R * R) return true;
            }
            // Plinth wall: the cylinder side between groundY and the deck top, seen from outside.
            double ox = o.X - c.X, oz = o.Z - c.Z;
            double qa = d.X * d.X + d.Z * d.Z, qb = 2 * (ox * d.X + oz * d.Z), qc = ox * ox + oz * oz - R * R;
            if (qa < 1e-12 || qc <= 0) return false;
            double disc = qb * qb - 4 * qa * qc;
            if (disc < 0) return false;
            double s0 = (-qb - Math.Sqrt(disc)) / (2 * qa);
            if (s0 <= 0) return false;
            double yy = o.Y + d.Y * s0;
            return yy <= c.Y && yy >= groundY;
        }

        public static PoseVec Cross(PoseVec a, PoseVec b) => new PoseVec(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

        public static double PointSegment(PoseVec p, PoseVec a, PoseVec b)
        {
            var ab = b - a; double l2 = PoseVec.Dot(ab, ab);
            double s = l2 > 1e-12 ? Clamp(PoseVec.Dot(p - a, ab) / l2, 0, 1) : 0;
            return (a + ab * s - p).Length;
        }

        /// <summary>Closest distance between segments p0-p1 and q0-q1.</summary>
        public static double SegmentSegment(PoseVec p0, PoseVec p1, PoseVec q0, PoseVec q1)
        {
            var u = p1 - p0; var v = q1 - q0; var w = p0 - q0;
            double a = PoseVec.Dot(u, u), b = PoseVec.Dot(u, v), c = PoseVec.Dot(v, v), d = PoseVec.Dot(u, w), e = PoseVec.Dot(v, w);
            double D = a * c - b * b, sN, sD = D, tN, tD = D;
            if (D < 1e-12) { sN = 0; sD = 1; tN = e; tD = c; }
            else
            {
                sN = b * e - c * d; tN = a * e - b * d;
                if (sN < 0) { sN = 0; tN = e; tD = c; }
                else if (sN > sD) { sN = sD; tN = e + b; tD = c; }
            }
            if (tN < 0)
            {
                tN = 0;
                if (-d < 0) sN = 0; else if (-d > a) sN = sD; else { sN = -d; sD = a; }
            }
            else if (tN > tD)
            {
                tN = tD;
                if (-d + b < 0) sN = 0; else if (-d + b > a) sN = sD; else { sN = -d + b; sD = a; }
            }
            double sc = Math.Abs(sN) < 1e-12 ? 0 : sN / sD, tc = Math.Abs(tN) < 1e-12 ? 0 : tN / tD;
            return (w + u * sc - v * tc).Length;
        }

        static double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
