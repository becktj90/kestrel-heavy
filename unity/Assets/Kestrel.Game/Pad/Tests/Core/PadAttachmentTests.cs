// EditMode, engine-free. TestFlight 202, countdown in the Below view: about eight hoses hung in mid-air beside a
// small rocket, and a white beam stood out of its nose. Both came from the Rail pad (stages under 3 m across):
// it has no tower anchors, so every link hung from a point 1.2 m off the skin, and the rail leaned into the
// vehicle. These pin the fix: every umbilical starts on pad structure, and no pad member enters the vehicle.
using System;
using System.Collections.Generic;
using Kestrel.Kit.Contracts;
using Kestrel.Vehicle;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class PadAttachmentTests
    {
        /// <summary>Farthest a hose's pad end may sit from the nearest pad member centre line (m): half a beam
        /// (0.25 m) plus a clamp. A link that hangs from nothing fails by metres.</summary>
        public const float MaxAnchorGapM = 0.6f;

        // Designer-sized stacks: (height, radius, pad). Starter rockets are the Rail rows.
        static readonly (float H, float R, PadArchetype Type)[] Pads =
        {
            (14f, 0.45f, PadArchetype.Rail), (22f, 0.6f, PadArchetype.Rail), (36f, 1.2f, PadArchetype.Rail),
            (48f, 1.6f, PadArchetype.Tower), (70f, 2.5f, PadArchetype.Tower), (90f, 3.6f, PadArchetype.TowerStrongback),
        };

        /// <summary>The plates SliceApp publishes for a two-stage liquid stack: TankShellUmbilicals per tank, rotated into
        /// the pad frame by the pad yaw, as PadTower.Apply does.</summary>
        static List<AttachPoint> Plates(float h, float r)
        {
            var pts = new List<AttachPoint>();
            float lower = h * 0.55f, upper = h * 0.25f;
            var s0 = TankShellSpec.Default(r * 1.005f, lower, 11UL);
            var s1 = TankShellSpec.Default(r * 1.005f, upper, 29UL);
            pts.AddRange(TankShellUmbilicals.ForStage(s0, 0, 0, new Vec3(0, 1f, 0), true, true, true, ReleaseStyle.TailServiceMastDrop));
            pts.AddRange(TankShellUmbilicals.ForStage(s1, 1, 0, new Vec3(0, lower + 3f, 0), true, true, true, ReleaseStyle.SwingArmRetract));
            float yaw = (float)(Math.Atan2(-(r + 6f + 2f), 6f) * 180.0 / Math.PI);
            return UmbilicalRig.IntoPadFrame(pts, ContractMath.Yaw(-yaw));
        }

        static float SegmentDistance(Vec3 p, Vec3 a, Vec3 b)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y, dz = b.Z - a.Z;
            float len2 = dx * dx + dy * dy + dz * dz;
            float t = len2 > 1e-8f ? ((p.X - a.X) * dx + (p.Y - a.Y) * dy + (p.Z - a.Z) * dz) / len2 : 0f;
            t = Math.Max(0f, Math.Min(1f, t));
            float x = a.X + dx * t - p.X, y = a.Y + dy * t - p.Y, z = a.Z + dz * t - p.Z;
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        /// <summary>Nearest structure to a point: tower and rail members, and the swing-arm beams (pivot to tip).</summary>
        static float NearestStructure(PadLayout L, Vec3 p, out string id)
        {
            float best = float.MaxValue; id = null;
            foreach (var m in L.Members)
            {
                float d = SegmentDistance(p, m.A, m.B);
                if (d < best) { best = d; id = m.Id; }
            }
            foreach (var link in L.Links)
                foreach (var m in link.Mast)
                {
                    float d = SegmentDistance(p, m.A, m.B);
                    if (d < best) { best = d; id = m.Id; }
                }
            return best;
        }

        [Test]
        public void EveryUmbilicalHangsFromPadStructure()
        {
            foreach (var pad in Pads)
            {
                var pts = Plates(pad.H, pad.R);
                var L = PadGenerator.Generate(1, pad.Type, new PadFit(pad.H, pad.R), pts);
                Assert.That(L.Links.Count, Is.EqualTo(pts.Count), pad.Type + " " + pad.H);
                foreach (var link in L.Links)
                {
                    string where = pad.Type + " h" + pad.H + " " + link.AttachId;
                    // Swing arms start at their pivot on the tower face; everything else at its anchor or mast head.
                    float gap = UmbilicalRig.StructureGap(L, UmbilicalRig.PadEnd(link), out string on);
                    Assert.That(gap, Is.LessThanOrEqualTo(MaxAnchorGapM), where + " hose starts " + gap.ToString("0.00") + " m from " + on);
                    if (pad.Type == PadArchetype.Rail)
                    {
                        Assert.That(link.OnRail, Is.True, where + " clamps to the rail");
                        Assert.That(on, Does.StartWith("rail"), where);
                    }
                }
            }
        }

        [Test]
        public void ConnectKeepsRailLinksOnTheRail()
        {
            var pts = Plates(22f, 0.6f);
            var L = PadGenerator.Generate(1, PadArchetype.Rail, new PadFit(22f, 0.6f), pts);
            foreach (var link in L.Links)
            {
                // UmbilicalVisual.Connect retargets every link to the live plate. That path used to re-float the anchor.
                var moved = new Vec3(link.Plate.X, link.Plate.Y + 0.4f, link.Plate.Z);
                UmbilicalRig.Retarget(link, moved, link.Outward);
                float gap = UmbilicalRig.StructureGap(L, link.Anchor, out string on);
                Assert.That(gap, Is.LessThanOrEqualTo(MaxAnchorGapM), link.AttachId + " after Connect: " + gap.ToString("0.00") + " m from " + on);
                Assert.That(link.Anchor.Y, Is.EqualTo(moved.Y).Within(0.01f), link.AttachId + " clamp follows the plate height");
            }
        }

        [Test]
        public void NoPadMemberEntersTheVehicle()
        {
            foreach (var pad in Pads)
                for (int seed = 1; seed <= 12; seed++)
                {
                    var L = PadGenerator.Generate(seed, pad.Type, new PadFit(pad.H, pad.R));
                    foreach (var m in L.Members)
                        for (int i = 0; i <= 20; i++)
                        {
                            float t = i / 20f;
                            float y = m.A.Y + (m.B.Y - m.A.Y) * t;
                            if (y < 0f || y > pad.H) continue;
                            float x = m.A.X + (m.B.X - m.A.X) * t, z = m.A.Z + (m.B.Z - m.A.Z) * t;
                            float radial = (float)Math.Sqrt(x * x + z * z);
                            Assert.That(radial, Is.GreaterThan(pad.R + 0.3f), pad.Type + " seed " + seed + " " + m.Id + " at y " + y.ToString("0.0"));
                        }
                }
        }

        [Test]
        public void RailLeansAwayFromTheVehicleAndOvertopsIt()
        {
            for (int seed = 1; seed <= 12; seed++)
            {
                var L = PadGenerator.Generate(seed, PadArchetype.Rail, new PadFit(22f, 0.6f));
                Assert.That(L.HasRail, Is.True);
                Assert.That(L.RailTop.Z, Is.GreaterThan(L.RailBase.Z), "seed " + seed + " leans toward +Z, away from the stack");
                Assert.That(L.RailTop.Y, Is.GreaterThan(22f), "seed " + seed);
                Assert.That(L.RailBase.Z, Is.EqualTo(0.6f + PadGenerator.RailSkinGap).Within(1e-4f));
            }
        }

        [Test]
        public void PoleBesideALowCameraIsSkippedButFarPolesStay()
        {
            var L = PadGenerator.Generate(3, PadArchetype.Rail, new PadFit(22f, 0.6f));
            var plan = PadLighting.Plan(L, 2);
            var poles = plan.FindAll(s => s.Kind == LightKind.Pole);
            Assert.That(poles.Count, Is.GreaterThan(0));
            // Stand the camera 3 m from a pole, as in the 202 Below frame: that pole is skipped.
            var p = poles[0].Position;
            float camX = p.X * 0.9f, camZ = p.Z * 0.9f + 1f;
            Assert.That(PadLighting.PoleTooClose(poles[0], camX, camZ), Is.True);
            // The same camera keeps every pole across the pad, and never skips a non-pole light.
            int kept = 0;
            foreach (var s in plan)
            {
                if (s.Kind != LightKind.Pole) Assert.That(PadLighting.PoleTooClose(s, camX, camZ), Is.False, s.Id);
                else if (!PadLighting.PoleTooClose(s, camX, camZ)) kept++;
            }
            Assert.That(kept, Is.GreaterThanOrEqualTo(poles.Count / 2), "only the foreground poles drop");
            // A camera far out (the Pad overview) only skips poles within PoleNearShare of its range.
            foreach (var s in poles) Assert.That(PadLighting.PoleTooClose(s, 0f, 400f), Is.False, s.Id);
        }
    }
}
