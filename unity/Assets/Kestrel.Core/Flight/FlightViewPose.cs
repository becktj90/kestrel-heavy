// Pure flight-camera pose for the five saved views. SliceApp feeds it the live sim state each frame
// (vehicle pivot, axis, altitude, the stack still attached after staging) and smooths the result with
// ChaseCamera.StepOffset, so the EditMode tests can fly a pose through a whole ascent without Unity.
//
// What changed against the #288 offsets (FlightView.Of):
//  - Height is the CURRENT stack: after a stage drops, Above/Below/Chase/Auto frame the upper stage
//    (its base and length) instead of the full launch stack, which left it small and far away.
//  - Below on the pad (TestFlight 202, #333): the #304 hold sat 0.15 h ABOVE the deck at 0.9 h, i.e. a
//    level side view that did not look up, and in world axes, so on the yawed pad it could land by the
//    tower legs or a light pole. It is now a worm's-eye view: just under the deck top (below the rocket's
//    base), outside the deck rim, on the pad side opposite the tower (pad frame, PadYawDeg) and clear of the
//    trench line, at the range where base to top spans BelowSpan of the view, pitched up to the middle of
//    that span. The ground floor drops to LowClearance for this hold only. Once the stack is about a
//    height up it blends back to the low view.
//  - Pad stays anchored to the ground but tracks with a clamped narrower field of view as range grows,
//    and reports Far (with a short note) once the rocket would be a few pixels.
// Smooth everywhere: no step in position, look-at or FOV as altitude or stage height changes.
using System;

namespace Kestrel.Core.Flight
{
    public struct PoseVec
    {
        public double X, Y, Z;
        public PoseVec(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static PoseVec operator +(PoseVec a, PoseVec b) => new PoseVec(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static PoseVec operator -(PoseVec a, PoseVec b) => new PoseVec(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static PoseVec operator *(PoseVec a, double k) => new PoseVec(a.X * k, a.Y * k, a.Z * k);
        public static PoseVec operator *(double k, PoseVec a) => a * k;
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public PoseVec Unit { get { double l = Length; return l > 1e-12 ? this * (1.0 / l) : new PoseVec(0, 1, 0); } }
        public static double Dot(PoseVec a, PoseVec b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public bool Finite => !(double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(Z) || double.IsInfinity(X) || double.IsInfinity(Y) || double.IsInfinity(Z));
        public static readonly PoseVec Up = new PoseVec(0, 1, 0);
        public override string ToString() => "(" + X.ToString("0.0") + ", " + Y.ToString("0.0") + ", " + Z.ToString("0.0") + ")";
    }

    public struct FlightPoseInput
    {
        public FlightViewMode Mode;
        /// <summary>Vehicle pivot (the base of the launch stack), floating-origin world space.</summary>
        public PoseVec Vehicle;
        /// <summary>Vehicle long axis (unit).</summary>
        public PoseVec Up;
        /// <summary>Launch stack height, metres. Pad framing uses it so the pad camera does not move at staging.</summary>
        public double Height;
        /// <summary>Base of the stack still attached, metres up the axis from the pivot (0 before staging).</summary>
        public double KeptBase;
        /// <summary>Sim altitude, metres.</summary>
        public double Altitude;
        /// <summary>World y of the ground anchor (the site root). The camera never goes under GroundY + 3 m.</summary>
        public double GroundY;
        /// <summary>World y of the deck top (the plume collision plane), when HasDeck. Else GroundY.</summary>
        public double DeckY;
        public bool HasDeck;
        /// <summary>The pad anchor (site root) in world space.</summary>
        public PoseVec Pad;
        /// <summary>Trench axis bearing, degrees (0 = +Z toward the tower, 90 = +X). NaN: the default side exits.</summary>
        public double TrenchBearingDeg;
        /// <summary>The juice FOV this frame (vertical, degrees).</summary>
        public double BaseFovDeg;
        /// <summary>Pad root yaw in world (degrees, Unity Euler y): pad +Z (the tower) points along world
        /// (sin yaw, 0, cos yaw). PadTower.Apply sets it; the trench bearing is in the same pad frame.</summary>
        public double PadYawDeg;
        /// <summary>Deck plinth radius (m, PadGround.DeckRadius). The Below hold stands outside it. 0: no deck.</summary>
        public double DeckRadius;
    }

    public struct FlightPose
    {
        public PoseVec Position, LookAt;
        /// <summary>Position minus the vehicle pivot: the value ChaseCamera.StepOffset smooths.</summary>
        public PoseVec Offset;
        /// <summary>Vertical FOV to use, degrees. Equal to the base FOV except a narrowed Pad view.</summary>
        public double Fov;
        /// <summary>Kept stack height and its centre, for tests and framing.</summary>
        public double Height;
        /// <summary>Pad view at long range: the rocket is a few pixels even zoomed in.</summary>
        public bool Far;
        public string Note;
        /// <summary>Camera floor above GroundY for this pose (m): MinClearance, or LowClearance in the Below
        /// pad hold. SliceApp passes it to PadGround.ClampAboveGround so the runtime agrees.</summary>
        public double Clearance;
    }

    public static class FlightViewPose
    {
        // Mirrors ChaseCamera (Slice): StandoffX/Z and the horizon hold window. ChaseCamera reads these.
        public const double StandoffX = 2.0, StandoffZ = 4.0;
        public const double HoldFullAlt = 13000, HoldEndAlt = 20000;
        /// <summary>Default camera floor above the ground anchor (PadGround.MinCameraClearance).</summary>
        public const double MinClearance = 3;
        /// <summary>Below pad hold floor above the ground anchor (m). The apron sits under GroundY, so the
        /// camera's near plane (0.5 m) never reaches it.</summary>
        public const double LowClearance = 0.6;
        /// <summary>Below hold: camera this far under the deck top / rocket base (m), so it looks up.</summary>
        public const double BelowUnderDeck = 0.15;
        /// <summary>Below hold: base to top of the stack spans this share of the vertical FOV.</summary>
        public const double BelowSpan = 0.86;
        /// <summary>Below hold: at least this far outside the deck rim (m), so the plinth wall is past the near plane.</summary>
        public const double BelowDeckGap = 4;
        /// <summary>Below hold bearings in the pad frame (deg, 0 = tower): opposite the tower, so the tower and
        /// rail stand behind the rocket. The first that is BelowTrenchClearDeg off the trench line wins.</summary>
        public static readonly double[] BelowBearingsDeg = { 205, 155, 180, 230, 130 };
        public const double BelowTrenchClearDeg = 40;
        /// <summary>Below hold is full until here, then blends out over one more stack height.</summary>
        public const double BelowHoldAlt = 30;
        /// <summary>Pad view: aim for the rocket to fill this share of the view height while zooming.</summary>
        public const double PadFill = 0.08;
        public const double MinFovDeg = 2.5;
        /// <summary>Past this range (m) the Pad view reports Far even if it still resolves the rocket.</summary>
        public const double FarRange = 20000;
        /// <summary>Below this share of the view height the rocket is "far".</summary>
        public const double MinFill = 0.02;
        public const string FarNote = "Pad view: rocket far away";
        /// <summary>Exponential smoothing rate SliceApp uses for offsets and FOV (per second).</summary>
        public const float SmoothRate = 4f;

        public static double HoldWeight(double altitude)
        {
            double t = Clamp01((altitude - HoldFullAlt) / (HoldEndAlt - HoldFullAlt));
            return 1 - t * t * (3 - 2 * t);
        }

        /// <summary>Auto's offset from the base of a stack of height h (ChaseCamera.DesiredOffset).</summary>
        public static PoseVec ChaseOffset(PoseVec up, double h, double altitude)
        {
            double k = Lerp(0.6, 0.05, HoldWeight(altitude));
            return up * (-h * k) + new PoseVec(h * StandoffX, 0, -h * StandoffZ);
        }

        public static FlightPose Pose(FlightPoseInput p)
        {
            var up = p.Up.Length > 1e-9 ? p.Up.Unit : PoseVec.Up;
            double full = p.Height > 1 ? p.Height : 1;
            double keptBase = p.KeptBase > 0 && p.KeptBase < full - 1 ? p.KeptBase : 0;
            double h = Math.Max(1, full - keptBase);
            var stackBase = p.Vehicle + up * keptBase;
            var look = stackBase + up * (h * 0.5);
            double baseFov = p.BaseFovDeg > 1 ? p.BaseFovDeg : 50;
            var pose = new FlightPose { LookAt = look, Fov = baseFov, Height = h, Clearance = MinClearance };

            PoseVec cam;
            switch (p.Mode)
            {
                case FlightViewMode.Pad:
                {
                    var o = FlightView.Of(FlightViewMode.Pad, full);
                    cam = p.Pad + new PoseVec(o.X, o.Y, o.Z);
                    double range = (look - cam).Length;
                    double angle = 2 * Math.Atan(h * 0.5 / Math.Max(1, range));
                    double want = angle / PadFill * 180 / Math.PI;
                    pose.Fov = Clamp(want, MinFovDeg, baseFov);
                    double fill = angle * 180 / Math.PI / pose.Fov;
                    pose.Far = range > FarRange || fill < MinFill;
                    pose.Note = pose.Far ? FarNote : null;
                    break;
                }
                case FlightViewMode.Below:
                {
                    double w = BelowHoldWeight(p.Altitude, h);
                    cam = Below(p, up, stackBase, h, baseFov, w, out var holdLook);
                    pose.LookAt = look * (1 - w) + holdLook * w;
                    pose.Clearance = MinClearance + (LowClearance - MinClearance) * w;
                    break;
                }
                case FlightViewMode.Chase:
                case FlightViewMode.Above:
                {
                    var o = FlightView.Of(p.Mode, h);
                    cam = stackBase + up * o.Y + new PoseVec(o.X, 0, o.Z);
                    break;
                }
                default:
                    cam = stackBase + ChaseOffset(up, h, p.Altitude);
                    break;
            }
            // Same floor the runtime applies (PadGround.MinCameraClearance).
            if (cam.Y < p.GroundY + pose.Clearance) cam.Y = p.GroundY + pose.Clearance;
            pose.Position = cam;
            pose.Offset = cam - p.Vehicle;
            return pose;
        }

        /// <summary>1 on the pad, 0 once the stack is about a height clear, smooth in between.</summary>
        public static double BelowHoldWeight(double altitude, double h)
        {
            double t = Clamp01((altitude - BelowHoldAlt) / Math.Max(30, h));
            return 1 - t * t * (3 - 2 * t);
        }

        /// <summary>Pad-frame bearing (deg) of the Below hold: opposite the tower and off the trench line.</summary>
        public static double BelowBearingDeg(double trenchBearingDeg)
        {
            double b = double.IsNaN(trenchBearingDeg) ? 90 : trenchBearingDeg;
            double best = BelowBearingsDeg[0], bestSep = -1;
            foreach (var c in BelowBearingsDeg)
            {
                double sep = LineSeparation(c, b);
                if (sep >= BelowTrenchClearDeg) return c;
                if (sep > bestSep) { bestSep = sep; best = c; }
            }
            return best;
        }

        /// <summary>Angle (0..90 deg) between a bearing and a line through the pad (both exits of the trench).</summary>
        public static double LineSeparation(double bearingDeg, double lineDeg)
        {
            double d = Math.Abs(((bearingDeg - lineDeg) % 180 + 180) % 180);
            return Math.Min(d, 180 - d);
        }

        /// <summary>Range at which a stack from dy0 to dy1 above the camera spans <paramref name="spanRad"/>.</summary>
        static double RangeForSpan(double dy0, double dy1, double spanRad, double h)
        {
            double lo = 0.5, hi = Math.Max(10, 40 * h);
            for (int i = 0; i < 60; i++)
            {
                double mid = 0.5 * (lo + hi);
                double a = Math.Atan2(dy1, mid) - Math.Atan2(dy0, mid);
                if (a > spanRad) lo = mid; else hi = mid;
            }
            return hi;
        }

        /// <summary>
        /// The low view off the pad. #288 put it along the vehicle axis (−0.82 h up the axis), so once the rocket
        /// pitched over it sat behind the engines at the rocket's own height: a side or tail view labelled "Below"
        /// (TestFlight t432). It now also drops under the rocket in world terms as the axis tips, by the same
        /// −0.82 h, so it always looks up at the belly. Unchanged while the stack is vertical.
        /// </summary>
        public static PoseVec BelowLow(PoseVec stackBase, PoseVec up, double h)
        {
            var o = FlightView.Of(FlightViewMode.Below, h);
            double tip = 1 - Clamp01(up.Y);
            return stackBase + up * o.Y + new PoseVec(o.X, 0, o.Z) + PoseVec.Up * (o.Y * tip);
        }

        static PoseVec Below(FlightPoseInput p, PoseVec up, PoseVec stackBase, double h, double fovDeg, double w, out PoseVec holdLook)
        {
            var low = BelowLow(stackBase, up, h);
            // Worm's-eye hold: under the deck top (the rocket's base), outside the rim, opposite the tower.
            // Sized for the stack standing on the pad, so the camera holds still while the rocket rises off it.
            double deckY = p.HasDeck ? p.DeckY : p.GroundY;
            double camY = Math.Max(p.GroundY + LowClearance, deckY - BelowUnderDeck);
            double rise = Math.Max(0, stackBase.Y - deckY);
            double dy0 = deckY - camY, dy1 = deckY + h - camY;
            double span = BelowSpan * fovDeg * Math.PI / 180;
            double range = Math.Max(RangeForSpan(dy0, dy1, span, h), Math.Max(0, p.DeckRadius) + BelowDeckGap);
            double world = (BelowBearingDeg(p.TrenchBearingDeg) + p.PadYawDeg) * Math.PI / 180;
            var dir = new PoseVec(Math.Sin(world), 0, Math.Cos(world));
            var hold = new PoseVec(stackBase.X, camY, stackBase.Z) + dir * range;
            // Aim at the middle of the base-to-top span: pitched up, the base just above the bottom edge.
            double pitch = 0.5 * (Math.Atan2(dy0, range) + Math.Atan2(dy1, range));
            holdLook = new PoseVec(stackBase.X, camY + range * Math.Tan(pitch) + rise, stackBase.Z);
            return low * (1 - w) + hold * w;
        }

        public const double HorizonMin = 0.15, HorizonMax = 0.35, EarthRadius = 6371000;

        /// <summary>
        /// Camera forward for a pose: Auto keeps ChaseCamera.Aim's horizon-held pitch, the rest look straight
        /// at the target. Roll-free, world up. Used by the tests to check the target stays in frame.
        /// </summary>
        public static PoseVec Forward(FlightViewMode mode, PoseVec cam, PoseVec target, double vFovDeg, double altitude, double camAltitude)
        {
            var d = target - cam;
            if (mode != FlightViewMode.Auto) return d.Unit;
            double horiz = Math.Sqrt(d.X * d.X + d.Z * d.Z);
            double pitch = Math.Atan2(d.Y, Math.Max(1e-4, horiz));
            double w = HoldWeight(altitude);
            if (w > 0)
            {
                double t2 = 2 * Math.Tan(0.5 * vFovDeg * Math.PI / 180), dip = HorizonDip(camAltitude);
                double pMin = Math.Atan((0.5 - HorizonMax) * t2) - dip;
                double pMax = Math.Atan((0.5 - HorizonMin) * t2) - dip;
                pitch = Lerp(pitch, Clamp(pitch, pMin, pMax), w);
            }
            double yaw = Math.Atan2(d.X, d.Z);
            return new PoseVec(Math.Sin(yaw) * Math.Cos(pitch), Math.Sin(pitch), Math.Cos(yaw) * Math.Cos(pitch));
        }

        public static double HorizonDip(double camAltitude) =>
            Math.Acos(Clamp(EarthRadius / (EarthRadius + Math.Max(0, camAltitude)), -1, 1));

        /// <summary>Share of the view height the stack fills from this pose (0..).</summary>
        public static double Fill(FlightPose pose)
        {
            double range = (pose.LookAt - pose.Position).Length;
            double angle = 2 * Math.Atan(pose.Height * 0.5 / Math.Max(1e-6, range));
            return angle * 180 / Math.PI / Math.Max(1e-6, pose.Fov);
        }

        /// <summary>ChaseCamera.StepOffset in doubles: frame-rate independent exponential approach.</summary>
        public static PoseVec Step(PoseVec current, PoseVec desired, double rate, double dt)
        {
            double k = 1 - Math.Exp(-rate * dt);
            return current + (desired - current) * k;
        }

        public static double StepFov(double current, double desired, double rate, double dt)
        {
            if (!(current > 0)) return desired;
            double k = 1 - Math.Exp(-rate * dt);
            return current + (desired - current) * k;
        }

        static double Lerp(double a, double b, double t) => a + (b - a) * t;
        static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
        static double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
