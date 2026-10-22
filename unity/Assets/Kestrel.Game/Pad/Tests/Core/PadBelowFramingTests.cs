// EditMode, engine-free. #348: the countdown Below view (TestFlight 202 / t455, the 390x844 capture) showed the plinth
// wall over ~45% of the frame, two tower columns in the middle and only the nose over the deck edge. The hold now
// stands on the deck near the stack (BelowFraming). These run the hold against real generated pads: Rail, Tower and
// Strongback layouts with the TankShellUmbilicals plates, their members as sight-line capsules (PadSightlines, as
// SliceApp feeds them) plus the light poles PadLights keeps for that camera, at phone and landscape aspects.
using System;
using System.Collections.Generic;
using System.Linq;
using Kestrel.Core.Flight;
using Kestrel.Kit.Contracts;
using Kestrel.Vehicle;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class PadBelowFramingTests
    {
        // (name, height, tank radius, widest radius, pad): First Hop to the big strap-on stack.
        static readonly (string Name, float H, float R, float Rr, PadArchetype Type)[] Stacks =
        {
            ("First Hop", 4.5f, 0.9f, 0.9f, PadArchetype.Rail), ("starter", 14f, 0.45f, 0.45f, PadArchetype.Rail),
            ("Tiny Orbit", 22f, 0.6f, 0.6f, PadArchetype.Rail), ("rail 36", 36f, 1.2f, 1.2f, PadArchetype.Rail),
            ("tower 48", 48f, 1.6f, 1.6f, PadArchetype.Tower), ("tower 70", 70f, 2.5f, 2.5f, PadArchetype.Tower),
            ("strap-ons 90", 90f, 3.6f, 8.5f, PadArchetype.TowerStrongback),
        };
        static readonly (string Name, double Aspect)[] Screens =
        {
            ("375x667", 375.0 / 667), ("390x844", 390.0 / 844), ("430x932", 430.0 / 932), ("471x1024", 471.0 / 1024), ("1280x800", 1.6),
        };

        static List<AttachPoint> Plates(float h, float r, float yaw)
        {
            var pts = new List<AttachPoint>();
            float lower = h * 0.55f, upper = h * 0.25f;
            var s0 = TankShellSpec.Default(r * 1.005f, lower, 11UL);
            var s1 = TankShellSpec.Default(r * 1.005f, upper, 29UL);
            pts.AddRange(TankShellUmbilicals.ForStage(s0, 0, 0, new Vec3(0, 1f, 0), true, true, true, ReleaseStyle.TailServiceMastDrop));
            pts.AddRange(TankShellUmbilicals.ForStage(s1, 1, 0, new Vec3(0, lower + 3f, 0), true, true, true, ReleaseStyle.SwingArmRetract));
            return UmbilicalRig.IntoPadFrame(pts, ContractMath.Yaw(-yaw));
        }

        [Test]
        public void BelowHoldFramesTheStackOnRealPads()
        {
            foreach (var st in Stacks)
            foreach (int seed in new[] { 1, 2, 5 })
            {
                // PadTower.Fit: the tower 6 m off the widest skin, yawed so it stands at (-(r + 8), 6) in the pad frame.
                float yaw = (float)(Math.Atan2(-(st.Rr + 6f + 2f), 6f) * 180 / Math.PI);
                var layout = PadGenerator.Generate(seed, st.Type, new PadFit(st.H, st.Rr, 6f), Plates(st.H, st.R, yaw));
                double cy = Math.Cos(yaw * Math.PI / 180), sy = Math.Sin(yaw * Math.PI / 180);
                Func<Vec3, PoseVec> world = v => new PoseVec(v.X * cy + v.Z * sy, v.Y, -v.X * sy + v.Z * cy);
                var members = PadSightlines.Capsules(layout).Select(c => new PoseObstacle(world(c.A), world(c.B), c.Radius, c.Id)).ToArray();
                // The deck PadGround builds: grown to every member touching it (this is the inflated radius #332 used).
                float deckR = Math.Max(15f, layout.DeckRadius);
                foreach (var m in layout.Members)
                {
                    if (Math.Min(m.A.Y, m.B.Y) > 0.5f) continue;
                    deckR = Math.Max(deckR, (float)Math.Sqrt(m.A.X * m.A.X + m.A.Z * m.A.Z) + 2f);
                    deckR = Math.Max(deckR, (float)Math.Sqrt(m.B.X * m.B.X + m.B.Z * m.B.Z) + 2f);
                }
                var plan = PadLighting.Plan(layout, 2);
                foreach (var sc in Screens)
                {
                    string where = st.Name + " " + st.Type + " seed " + seed + " " + sc.Name;
                    var pose = FlightViewPose.Pose(new FlightPoseInput
                    {
                        Mode = FlightViewMode.Below, Vehicle = new PoseVec(0, 0, 0), Up = PoseVec.Up, Height = st.H,
                        GroundY = -1, DeckY = 0, HasDeck = true, Pad = new PoseVec(0, -1, 0),
                        TrenchBearingDeg = layout.Trench != null ? layout.Trench.BearingDeg : double.NaN,
                        BaseFovDeg = 50, PadYawDeg = yaw, DeckRadius = deckR, Radius = st.Rr, Aspect = sc.Aspect, Obstacles = members,
                    });
                    var cam = pose.Position;
                    var baseP = new PoseVec(0, 0, 0);
                    Assert.That(cam.Y, Is.InRange(BelowFraming.EyeMin - 1e-9, BelowFraming.EyeMax + 1e-9), where + " on the deck");
                    double dist = Math.Sqrt(cam.X * cam.X + cam.Z * cam.Z);
                    double plan0 = BelowFraming.Distance(st.H, st.Rr, BelowFraming.Eye(st.H), BelowFraming.FovDeg, sc.Aspect);
                    Assert.That(dist, Is.LessThanOrEqualTo(plan0 + BelowFraming.MaxPush + 1e-6), where + " within the planned distance");
                    var f = (pose.LookAt - cam).Unit;
                    Assert.That(Math.Asin(f.Y) * 180 / Math.PI, Is.InRange(15.0, 45.0), where + " pitch");
                    Assert.That(BelowFraming.StackBounds(cam, pose.LookAt, pose.Fov, sc.Aspect, baseP, st.H, st.Rr, out double x0, out double x1, out double y0, out double y1), Is.True, where);
                    Assert.That(Math.Min(Math.Min(1 + x0, 1 - x1), Math.Min(1 + y0, 1 - y1)) * 0.5, Is.GreaterThanOrEqualTo(0.03), where + " 3% margin");
                    Assert.That((y1 - y0) * 0.5, Is.GreaterThanOrEqualTo(0.35), where + " fill");
                    Assert.That(BelowFraming.DeckCover(cam, pose.LookAt, pose.Fov, sc.Aspect, baseP, deckR, -1), Is.LessThanOrEqualTo(0.25), where + " deck/plinth cover");
                    // PadLights hides the poles too close to this camera (PoleTooClose); the rest are structure too.
                    double px = cam.X * cy - cam.Z * sy, pz = cam.X * sy + cam.Z * cy;
                    var all = members.Concat(PadSightlines.Poles(plan, (float)px, (float)pz).Select(c => new PoseObstacle(world(c.A), world(c.B), c.Radius, c.Id))).ToArray();
                    double seen = BelowFraming.Visibility(cam, baseP, st.H, st.Rr, all, out string blocker);
                    Assert.That(seen, Is.GreaterThanOrEqualTo(0.8), where + " sight of the stack (" + blocker + ")");
                    Assert.That(BelowFraming.Clearance(cam, all), Is.GreaterThan(BelowFraming.NearReach(pose.Fov, sc.Aspect)), where + " near clip");
                }
            }
        }
    }
}
