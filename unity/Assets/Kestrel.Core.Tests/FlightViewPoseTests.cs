// Flight camera poses through a real ascent. The vehicle path, attitude, max-Q and the staging time all
// come from the headless sim (AscentSim, the MediumLifter two-stage archetype), converted to the world
// the way FlightDriver does it: downrange on +X, altitude on +Y, the axis along the flight path above 20 m.
using System;
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using NUnit.Framework;

namespace Kestrel.Core.Tests
{
    public class FlightViewPoseTests
    {
        // A 70 m two-stage stack: first stage to 47 m, upper stage and fairing above.
        const double StackH = 70, CoreTop = 47, Radius = 1.85;
        const double Fov = 50;

        struct Moment
        {
            public string Name;
            public double Altitude, Downrange, PitchDeg, KeptBase;
        }

        static List<Moment> moments;
        static readonly List<Moment> track = new List<Moment>();

        static List<Moment> Flight()
        {
            if (moments != null) return moments;
            var sim = new AscentSim(Headless.MediumLifter(), new SimOptions { payload = 5000, targetAlt = 200e3 });
            double omega = Earth.SiteRotationSpeed(28.5) / Gravity.R_EARTH;
            var list = new List<Moment> { new Moment { Name = "pad T-0", Altitude = 0, Downrange = 0, PitchDeg = 90 } };
            double prevX = 0, prevY = 0, pitch = 90, peakQ = 0, kept = 0;
            bool km1 = false, km10 = false, maxQ = false, staged = false, coast = false;
            Moment atPeak = default;
            int seen = 0;
            for (int i = 0; i < 400000 && !sim.Done; i++)
            {
                sim.Tick();
                double x = sim.Y[0], y = sim.Y[1];
                double r = Math.Sqrt(x * x + y * y);
                double alt = r - Gravity.R_EARTH;
                double down = Gravity.R_EARTH * (Math.Atan2(y, x) - (Math.PI / 2 + omega * sim.T));
                double dx = down - prevX, dy = alt - prevY;
                if (dx * dx + dy * dy > 1e-6 && alt > 20) pitch = Math.Atan2(dy, dx) * 180 / Math.PI;
                prevX = down; prevY = alt;
                for (; seen < sim.Events.Count; seen++)
                    if (sim.Events[seen].type == "separation") { kept = CoreTop; staged = true; }
                var m = new Moment { Altitude = alt, Downrange = down, PitchDeg = pitch, KeptBase = kept };
                if (i % 20 == 0) track.Add(m);
                if (sim.LastQ > peakQ) { peakQ = sim.LastQ; atPeak = m; atPeak.Name = "max-Q"; }
                if (!km1 && alt >= 1000) { km1 = true; m.Name = "1 km"; list.Add(m); }
                if (!km10 && alt >= 10000) { km10 = true; m.Name = "10 km"; list.Add(m); }
                if (!maxQ && peakQ > 0 && sim.LastQ < peakQ * AscentSim.MaxQConfirm) { maxQ = true; list.Add(atPeak); }
                if (staged && list.FindIndex(e => e.Name == "after staging") < 0) { m.Name = "after staging"; list.Add(m); }
                if (!coast && alt >= 120000) { coast = true; m.Name = "coast 120 km"; list.Add(m); break; }
            }
            Assert.That(list.Count, Is.EqualTo(6), "the sim reached every sample point: " + string.Join(", ", list.ConvertAll(e => e.Name)));
            moments = list;
            return list;
        }

        static FlightPoseInput Input(FlightViewMode mode, Moment m, double keptBase)
        {
            double p = m.PitchDeg * Math.PI / 180;
            return new FlightPoseInput
            {
                Mode = mode,
                Vehicle = new PoseVec(m.Downrange, m.Altitude, 0),
                Up = new PoseVec(Math.Cos(p), Math.Sin(p), 0),
                Height = StackH,
                KeptBase = keptBase,
                Altitude = m.Altitude,
                GroundY = 0,
                Pad = new PoseVec(0, 0, 0),
                TrenchBearingDeg = 90,
                BaseFovDeg = Fov,
            };
        }

        static readonly FlightViewMode[] All =
        {
            FlightViewMode.Auto, FlightViewMode.Chase, FlightViewMode.Above, FlightViewMode.Below, FlightViewMode.Pad,
        };

        [Test]
        public void EveryViewFramesTheStackThroughARealAscent()
        {
            foreach (var m in Flight())
            foreach (var mode in All)
            {
                var input = Input(mode, m, m.KeptBase);
                var pose = FlightViewPose.Pose(input);
                string where = mode + " @ " + m.Name + " (alt " + m.Altitude.ToString("0") + " m, pitch " + m.PitchDeg.ToString("0.0") + ")";
                Assert.That(pose.Position.Finite && pose.LookAt.Finite, Is.True, where + " finite");
                Assert.That(pose.Position.Y, Is.GreaterThanOrEqualTo(0.5), where + " above ground");

                var up = input.Up.Unit;
                var baseP = input.Vehicle + up * m.KeptBase;
                double h = StackH - m.KeptBase;
                Assert.That(pose.Height, Is.EqualTo(h).Within(1e-6), where + " frames the stack still attached");
                Assert.That(SegmentDistance(pose.Position, baseP, baseP + up * h), Is.GreaterThan(Radius * 1.5), where + " outside the vehicle capsule");

                var fwd = FlightViewPose.Forward(mode, pose.Position, pose.LookAt, pose.Fov, m.Altitude, pose.Position.Y);
                // The pad camera looks up steeply as the rocket climbs over it; the sweep test checks that one.
                double vertical = mode == FlightViewMode.Pad ? 0.9994 : 0.985;
                Assert.That(Math.Abs(PoseVec.Dot(fwd, PoseVec.Up)), Is.LessThan(vertical), where + " forward is not vertical, LookAt does not flip");
                var toLook = (pose.LookAt - pose.Position).Unit;
                double off = Math.Acos(Math.Max(-1, Math.Min(1, PoseVec.Dot(fwd, toLook)))) * 180 / Math.PI;
                Assert.That(off, Is.LessThan(pose.Fov * 0.5 * 0.9), where + " look-at inside the frustum");

                double dist = (pose.LookAt - pose.Position).Length;
                if (mode == FlightViewMode.Pad)
                {
                    Assert.That(pose.Fov, Is.InRange(FlightViewPose.MinFovDeg, Fov + 1e-9), where + " zoom clamped");
                    if (pose.Far)
                        Assert.That(pose.Note, Is.EqualTo(FlightViewPose.FarNote), where + " far note");
                    else
                        Assert.That(FlightViewPose.Fill(pose), Is.GreaterThanOrEqualTo(0.02), where + " size on screen");
                }
                else
                {
                    Assert.That(pose.Far, Is.False, where);
                    Assert.That(pose.Fov, Is.EqualTo(Fov).Within(1e-9), where + " on-vehicle views keep the juice FOV");
                    Assert.That(dist, Is.InRange(0.5 * h, 6 * h), where + " distance");
                    Assert.That(FlightViewPose.Fill(pose), Is.GreaterThanOrEqualTo(0.02), where + " size on screen");
                }
            }
        }

        [Test]
        public void NoViewEverLooksStraightUpOrDownDuringTheAscent()
        {
            Flight();
            Assert.That(track.Count, Is.GreaterThan(100));
            foreach (var mode in All)
            {
                double worst = 0;
                string at = "";
                foreach (var m in track)
                {
                    var pose = FlightViewPose.Pose(Input(mode, m, m.KeptBase));
                    var fwd = FlightViewPose.Forward(mode, pose.Position, pose.LookAt, pose.Fov, m.Altitude, pose.Position.Y);
                    double d = Math.Abs(PoseVec.Dot(fwd, PoseVec.Up));
                    if (d > worst) { worst = d; at = m.Altitude.ToString("0") + " m"; }
                }
                double minDeg = Math.Acos(worst) * 180 / Math.PI;
                Assert.That(minDeg, Is.GreaterThan(2.0), mode + " comes within " + minDeg.ToString("0.0") + " deg of vertical at " + at);
            }
        }

        [Test]
        public void PadViewAtCoastReportsFar()
        {
            var coast = Flight().Find(e => e.Name == "coast 120 km");
            var pose = FlightViewPose.Pose(Input(FlightViewMode.Pad, coast, coast.KeptBase));
            Assert.That(pose.Far, Is.True);
            Assert.That(pose.Note, Is.EqualTo("Pad view: rocket far away"));
            Assert.That(pose.Fov, Is.EqualTo(FlightViewPose.MinFovDeg).Within(1e-9), "zoomed in as far as the clamp allows");
            var pad = FlightViewPose.Pose(Input(FlightViewMode.Pad, Flight()[0], 0));
            Assert.That(pad.Far, Is.False);
            Assert.That(pad.Fov, Is.EqualTo(Fov).Within(1e-9), "no zoom on the pad");
        }

        /// <summary>The countdown as SliceApp feeds it: ground anchor 1 m under the deck top (the vehicle base),
        /// the pad yawed so its +Z points at the tower (PadTower.Fit), the trench in the pad frame.</summary>
        static FlightPoseInput PadInput(FlightViewMode mode, double h, double r, double deckR, double alt = 0, double trench = 90)
        {
            return new FlightPoseInput
            {
                Mode = mode, Vehicle = new PoseVec(0, alt, 0), Up = PoseVec.Up, Height = h, Altitude = alt,
                GroundY = -1, DeckY = 0, HasDeck = true, DeckRadius = deckR, Pad = new PoseVec(0, -1, 0),
                TrenchBearingDeg = trench, BaseFovDeg = Fov, PadYawDeg = TowerYawDeg(r),
            };
        }

        static double TowerYawDeg(double r) => Math.Atan2(-(r + 8), 6) * 180 / Math.PI;

        static double AngleDeg(PoseVec a, PoseVec b) =>
            Math.Acos(Math.Max(-1, Math.Min(1, PoseVec.Dot(a.Unit, b.Unit)))) * 180 / Math.PI;

        [Test]
        public void BelowOnThePadLooksUpFromThePad()
        {
            // TestFlight 202: a level side view 0.15 h over the deck, beside the tower legs and a light pole.
            foreach (var (h, r, deckR) in new[] { (14.0, 0.45, 15.0), (22.0, 0.6, 15.0), (StackH, Radius, 24.0) })
            foreach (var trench in new[] { 90.0, 0.0, 30.0, 200.0, double.NaN })
            {
                string where = "h " + h + " trench " + trench;
                var input = PadInput(FlightViewMode.Below, h, r, deckR, 0, trench);
                var pose = FlightViewPose.Pose(input);
                var cam = pose.Position;
                // Below the rocket's base (the deck top), above the apron by the low floor.
                Assert.That(cam.Y, Is.LessThan(0), where + " under the rocket's base");
                Assert.That(cam.Y, Is.GreaterThanOrEqualTo(input.GroundY + FlightViewPose.LowClearance - 1e-9), where + " clear of the ground");
                Assert.That(pose.Clearance, Is.EqualTo(FlightViewPose.LowClearance).Within(1e-9), where + " runtime floor matches");
                // Pitched up, with the base and the top both inside the frame.
                var fwd = FlightViewPose.Forward(FlightViewMode.Below, cam, pose.LookAt, pose.Fov, 0, cam.Y + 1);
                double pitch = Math.Asin(fwd.Y) * 180 / Math.PI;
                Assert.That(pitch, Is.GreaterThan(10), where + " pitched up");
                Assert.That(AngleDeg(fwd, new PoseVec(0, 0, 0) - cam), Is.LessThan(Fov * 0.5), where + " base in frame");
                Assert.That(AngleDeg(fwd, new PoseVec(0, h, 0) - cam), Is.LessThan(Fov * 0.5), where + " top in frame");
                Assert.That(FlightViewPose.Fill(pose), Is.GreaterThan(0.6), where + " the rocket fills the frame");
                // Off the deck plinth and on the side away from the tower, so the tower stands behind the rocket.
                double range = Math.Sqrt(cam.X * cam.X + cam.Z * cam.Z);
                Assert.That(range, Is.GreaterThanOrEqualTo(deckR + FlightViewPose.BelowDeckGap - 1e-9), where + " outside the deck rim");
                double yaw = input.PadYawDeg * Math.PI / 180;
                var towerDir = new PoseVec(Math.Sin(yaw), 0, Math.Cos(yaw));
                Assert.That(AngleDeg(new PoseVec(cam.X, 0, cam.Z), towerDir), Is.GreaterThan(125), where + " opposite the tower");
                var tower = new PoseVec(-(r + 8), cam.Y, 6);
                Assert.That((cam - tower).Length, Is.GreaterThan(deckR), where + " clear of the tower legs");
                double camBearing = Math.Atan2(cam.X, cam.Z) * 180 / Math.PI - input.PadYawDeg;
                if (!double.IsNaN(trench) && trench != 0 && trench != 30 && trench != 200)
                    Assert.That(FlightViewPose.LineSeparation(camBearing, trench), Is.GreaterThanOrEqualTo(FlightViewPose.BelowTrenchClearDeg - 1e-6), where + " off the trench line");
                Assert.That(FlightViewPose.LineSeparation(camBearing, double.IsNaN(trench) ? 90 : trench), Is.GreaterThanOrEqualTo(25), where + " never down the trench");
            }

            // Before #288's offset hit the ground clamp: same height as the clamp floor, under the stack.
            var old = FlightView.Of(FlightViewMode.Below, StackH);
            Assert.That(old.Y, Is.LessThan(0), "the old Below sat under the deck at T-0");

            // Rising off the pad the pose moves without a jump, and is the plain low view a stack height up.
            PoseVec prev = FlightViewPose.Pose(PadInput(FlightViewMode.Below, StackH, Radius, 24)).Position;
            PoseVec prevLook = FlightViewPose.Pose(PadInput(FlightViewMode.Below, StackH, Radius, 24)).LookAt;
            for (double alt = 0; alt <= 200; alt += 0.5)
            {
                var pose = FlightViewPose.Pose(PadInput(FlightViewMode.Below, StackH, Radius, 24, alt));
                var p = pose.Position;
                Assert.That((p - prev).Length, Is.LessThan(2.5), "smooth at " + alt + " m");
                Assert.That((pose.LookAt - prevLook).Length, Is.LessThan(2.5), "look smooth at " + alt + " m");
                Assert.That(p.Y - alt, Is.LessThan(StackH * 0.5), "below the stack centre at " + alt + " m");
                var f = FlightViewPose.Forward(FlightViewMode.Below, p, pose.LookAt, pose.Fov, alt, p.Y + 1);
                Assert.That(f.Y, Is.GreaterThan(0), "still looking up at " + alt + " m");
                prev = p; prevLook = pose.LookAt;
            }
            var high = FlightViewPose.Pose(PadInput(FlightViewMode.Below, StackH, Radius, 24, 200)).Position;
            Assert.That(high.Y, Is.EqualTo(200 + old.Y).Within(1e-6), "the low view once clear");
        }

        [Test]
        public void EveryModeIsItsOwnViewMidFlightAndBelowLooksUpAtTheBelly()
        {
            // TestFlight t432 (#332): "Cam: Below is really a side or front view", and switching seemed to do nothing.
            foreach (var m in Flight())
            {
                if (m.Altitude < 500) continue;
                var poses = new FlightPose[All.Length];
                for (int i = 0; i < All.Length; i++) poses[i] = FlightViewPose.Pose(Input(All[i], m, m.KeptBase));
                double h = StackH - m.KeptBase;
                for (int i = 0; i < All.Length; i++)
                for (int j = i + 1; j < All.Length; j++)
                {
                    var a = poses[i]; var b = poses[j];
                    double apart = (a.Position - b.Position).Length;
                    double turn = AngleDeg(a.LookAt - a.Position, b.LookAt - b.Position);
                    Assert.That(apart > 0.25 * h || turn > 10, Is.True,
                        All[i] + " vs " + All[j] + " @ " + m.Name + ": " + apart.ToString("0") + " m apart, " + turn.ToString("0") + " deg");
                }
                var input = Input(FlightViewMode.Below, m, m.KeptBase);
                var below = FlightViewPose.Pose(input);
                var stackBase = input.Vehicle + input.Up.Unit * m.KeptBase;
                Assert.That(below.Position.Y, Is.LessThan(stackBase.Y), "Below sits under the stack's base @ " + m.Name + " (pitch " + m.PitchDeg.ToString("0") + ")");
                var fwd = FlightViewPose.Forward(FlightViewMode.Below, below.Position, below.LookAt, below.Fov, m.Altitude, below.Position.Y);
                Assert.That(fwd.Y, Is.GreaterThan(0.2), "Below looks up @ " + m.Name);
            }
        }

        [Test]
        public void StagingReframesTheUpperStageSmoothly()
        {
            var staged = Flight().Find(e => e.Name == "after staging");
            Assert.That(staged.KeptBase, Is.EqualTo(CoreTop), "the sim's separation event moved the kept base");
            foreach (var mode in new[] { FlightViewMode.Auto, FlightViewMode.Chase, FlightViewMode.Above, FlightViewMode.Below })
            {
                var input = Input(mode, staged, CoreTop);
                var before = FlightViewPose.Pose(Input(mode, staged, 0));
                var after = FlightViewPose.Pose(input);
                // The upper stage on its own: 23 m, centred 58.5 m up the axis. #288 framed the 70 m stack.
                var upper = input.Vehicle + input.Up.Unit * ((CoreTop + StackH) * 0.5);
                double hU = StackH - CoreTop;
                double fillBefore = UpperFill(before, upper, hU);
                double fillAfter = UpperFill(after, upper, hU);
                Assert.That((after.LookAt - upper).Length, Is.LessThan(1e-6), mode + " aims at the upper stage");
                Assert.That(fillAfter, Is.GreaterThan(fillBefore * 2), mode + " the upper stage is at least twice as big on screen");
                Assert.That(fillAfter, Is.GreaterThan(0.1), mode + " and reads as a vehicle, not a dot");

                // Smoothed at 60 fps the swap is a glide, not a cut: the view direction turns under 90 deg/s.
                var off = before.Offset;
                var look = before.LookAt - Input(mode, staged, 0).Vehicle;
                var fwd = Dir(off, look);
                double worst = 0;
                for (int i = 0; i < 90; i++)
                {
                    off = FlightViewPose.Step(off, after.Offset, FlightViewPose.SmoothRate, 1.0 / 60);
                    look = FlightViewPose.Step(look, after.LookAt - Input(mode, staged, 0).Vehicle, FlightViewPose.SmoothRate, 1.0 / 60);
                    var next = Dir(off, look);
                    double turn = Math.Acos(Math.Max(-1, Math.Min(1, PoseVec.Dot(fwd, next)))) * 180 / Math.PI;
                    if (turn > worst) worst = turn;
                    fwd = next;
                }
                Assert.That(worst, Is.LessThan(1.5), mode + " turns at most 1.5 deg a frame");
            }
        }

        [Test]
        public void SwitchingViewsMidAscentConvergesWithinOneAndAHalfSeconds()
        {
            var m = Flight().Find(e => e.Name == "10 km");
            foreach (var from in All)
            foreach (var to in All)
            {
                if (from == to) continue;
                var a = FlightViewPose.Pose(Input(from, m, 0));
                var b = FlightViewPose.Pose(Input(to, m, 0));
                var off = a.Offset;
                double zoom = a.Fov / Fov;
                for (int i = 0; i < 90; i++)
                {
                    off = FlightViewPose.Step(off, b.Offset, FlightViewPose.SmoothRate, 1.0 / 60);
                    zoom = FlightViewPose.StepFov(zoom, b.Fov / Fov, FlightViewPose.SmoothRate, 1.0 / 60);
                }
                double start = (a.Offset - b.Offset).Length;
                double left = (off - b.Offset).Length;
                Assert.That(left, Is.LessThanOrEqualTo(Math.Max(0.05, start * 0.01)), from + " -> " + to + " offset");
                Assert.That(Math.Abs(zoom - b.Fov / Fov), Is.LessThan(0.01), from + " -> " + to + " zoom");
            }
        }

        [Test]
        public void HoldWeightMatchesTheChaseWindow()
        {
            Assert.That(FlightViewPose.HoldWeight(0), Is.EqualTo(1).Within(1e-12));
            Assert.That(FlightViewPose.HoldWeight(13000), Is.EqualTo(1).Within(1e-12));
            Assert.That(FlightViewPose.HoldWeight(16500), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(FlightViewPose.HoldWeight(20000), Is.EqualTo(0).Within(1e-12));
        }

        static double UpperFill(FlightPose pose, PoseVec centre, double h)
        {
            double d = (centre - pose.Position).Length;
            return 2 * Math.Atan(h * 0.5 / d) * 180 / Math.PI / pose.Fov;
        }

        static PoseVec Dir(PoseVec camOffset, PoseVec lookOffset) => (lookOffset - camOffset).Unit;

        static double SegmentDistance(PoseVec p, PoseVec a, PoseVec b)
        {
            var ab = b - a;
            double t = PoseVec.Dot(p - a, ab) / Math.Max(1e-12, PoseVec.Dot(ab, ab));
            t = Math.Max(0, Math.Min(1, t));
            return (p - (a + ab * t)).Length;
        }
    }
}
