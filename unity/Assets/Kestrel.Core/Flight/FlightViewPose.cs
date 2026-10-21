// Pure flight-camera pose for the five saved views. SliceApp feeds it the live sim state each frame
// (vehicle pivot, axis, altitude, the stack still attached after staging) and smooths the result with
// ChaseCamera.StepOffset, so the EditMode tests can fly a pose through a whole ascent without Unity.
//
// What changed against the #288 offsets (FlightView.Of):
//  - Height is the CURRENT stack: after a stage drops, Above/Below/Chase/Auto frame the upper stage
//    (its base and length) instead of the full launch stack, which left it small and far away.
//  - Below on the pad held the camera under the deck, where the ground clamp pressed it flat into the
//    plume and the trench. Until the stack is about one height up it now rises to at least 0.15 h above
//    the deck and slides to the side of the trench and plume axis, then blends back to the low view.
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
    }

    public static class FlightViewPose
    {
        // Mirrors ChaseCamera (Slice): StandoffX/Z and the horizon hold window. ChaseCamera reads these.
        public const double StandoffX = 2.0, StandoffZ = 4.0;
        public const double HoldFullAlt = 13000, HoldEndAlt = 20000;
        /// <summary>Below hold on the pad: camera at least this many heights above the deck.</summary>
        public const double BelowDeckLift = 0.15;
        /// <summary>Below hold: horizontal standoff from the plume axis, in heights, square to the trench.</summary>
        public const double BelowSide = 0.9;
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
            var pose = new FlightPose { LookAt = look, Fov = baseFov, Height = h };

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
                    cam = Below(p, up, stackBase, h);
                    break;
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
            if (cam.Y < p.GroundY + 3) cam.Y = p.GroundY + 3;
            pose.Position = cam;
            pose.Offset = cam - p.Vehicle;
            return pose;
        }

        static PoseVec Below(FlightPoseInput p, PoseVec up, PoseVec stackBase, double h)
        {
            var o = FlightView.Of(FlightViewMode.Below, h);
            var low = stackBase + up * o.Y + new PoseVec(o.X, 0, o.Z);
            // Square to the trench axis, on the side away from the tower (+Z).
            double b = double.IsNaN(p.TrenchBearingDeg) ? 90 : p.TrenchBearingDeg;
            double rad = b * Math.PI / 180;
            var side = new PoseVec(Math.Cos(rad), 0, -Math.Sin(rad));
            if (side.Z > 0) side = side * -1;
            // A little toward +X so the view is not dead on the tower axis.
            side = (side + new PoseVec(0.35, 0, 0)).Unit;
            double deckY = (p.HasDeck ? p.DeckY : p.GroundY) + BelowDeckLift * h;
            var hold = new PoseVec(stackBase.X, Math.Max(deckY, low.Y), stackBase.Z) + side * (BelowSide * h);
            // 1 on the pad, 0 once the stack is about a height clear, smooth in between.
            double t = Clamp01((p.Altitude - BelowHoldAlt) / Math.Max(30, h));
            double w = 1 - t * t * (3 - 2 * t);
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
