// EditMode, engine-free: retraction clocks and attach math for D4 umbilicals.
using System;
using System.Collections.Generic;
using Kestrel.Kit.Contracts;
using Kestrel.Vehicle;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class UmbilicalRigTests
    {
        static AttachPoint Plate(int stage, UmbilicalType t, ReleaseStyle r, float y, float z) => new AttachPoint
        {
            Id = UmbilicalIds.Make(stage, t, 0), Type = t, Stage = stage, Release = r, PlateRadius = 0.3f,
            Pose = new LocalPose(new Vec3(0, y, z), Quat.Identity),
        };

        static List<AttachPoint> Mixed() => new List<AttachPoint>
        {
            Plate(1, UmbilicalType.Fuel, ReleaseStyle.SwingArmRetract, 36f, 2.5f),
            Plate(0, UmbilicalType.Oxidizer, ReleaseStyle.TailServiceMastDrop, 4f, 2.5f),
            Plate(0, UmbilicalType.PowerData, ReleaseStyle.LanyardPull, 18f, 2.5f),
        };

        [Test]
        public void MountUsesArmAnchorAndDoesNotRecomputeTowerSize()
        {
            var anchors = new TowerAnchors(new TowerParams { Width = 11f, Levels = 8, LevelHeight = 5.5f, StandOff = 30f });
            var placed = new Vec3(2.25f, 19f, 6.5f);
            anchors.Add(TowerAnchors.ArmId("s1.fuel.0"), placed, "s", 3);
            var hit = UmbilicalMount.Resolve(anchors, "s1.fuel.0", 3f);
            Assert.That(hit.Ok, Is.True);
            Assert.That(hit.AnchorId, Is.EqualTo("arm.s1.fuel.0"));
            Assert.That(hit.Position.X, Is.EqualTo(placed.X));
            Assert.That(hit.Position.Y, Is.EqualTo(placed.Y));
            Assert.That(hit.Position.Z, Is.EqualTo(placed.Z));

            var face = UmbilicalMount.Resolve(anchors, "s0.lox.0", 14f);
            var near = anchors.OnFaceNear(14f);
            Assert.That(face.AnchorId, Is.EqualTo(near.Id));
            Assert.That(face.Position.Y, Is.EqualTo(near.Position.Y));
            Assert.That(face.Position.Z, Is.EqualTo(near.Position.Z));
            Assert.That(UmbilicalMount.Resolve(null, "s0.lox.0", 1f).Ok, Is.False);
        }

        [Test]
        public void RailPadsHaveNullAnchorsAndStillBuildLanyards()
        {
            var pts = Mixed();
            var rail = PadGenerator.Generate(1, PadArchetype.Rail, new PadFit(12f, 0.5f), pts);
            Assert.That(rail.Anchors, Is.Null);
            Assert.That(rail.Links.Count, Is.EqualTo(pts.Count));
            foreach (var link in rail.Links)
            {
                Assert.That(link.AnchorId, Is.Null);
                Assert.That(link.Rigid, Is.EqualTo(ServiceRigid.None));
                Assert.That(link.Strands, Is.Not.Empty);
            }
        }

        [Test]
        public void TowerLinksPreferArmAnchorsThenFaceAnchors()
        {
            var pts = Mixed();
            var L = PadGenerator.Generate(4, PadArchetype.Tower, new PadFit(70f, 2.5f), pts);
            Assert.That(L.Anchors, Is.Not.Null);
            var fuel = L.Links.Find(u => u.AttachId == "s1.fuel.0");
            var lox = L.Links.Find(u => u.AttachId == "s0.lox.0");
            Assert.That(fuel.AnchorId, Is.EqualTo(TowerAnchors.ArmId(fuel.AttachId)));
            Assert.That(fuel.Rigid, Is.EqualTo(ServiceRigid.Swing));
            Assert.That(L.Anchors.Get(fuel.AnchorId).Position.Y, Is.EqualTo(fuel.Anchor.Y));
            Assert.That(lox.AnchorId, Does.StartWith("level.").And.EndsWith(".s"));
            Assert.That(lox.Rigid, Is.EqualTo(ServiceRigid.Mast));
            Assert.That(lox.Mast.Count, Is.GreaterThan(0));
            Assert.That(L.Graph.Contains("umbilical.s1.fuel.0"), Is.True);
        }

        [Test]
        public void HoseSagsBetweenEnds()
        {
            var dst = new Vec3[25];
            var a = new Vec3(0, 12f, 8f); var b = new Vec3(0.2f, 11.5f, 2f);
            HoseBundle.Sample(a, b, 1.15f, 0f, 24, dst);
            Assert.That(dst[0].Z, Is.EqualTo(a.Z).Within(1e-3f));
            Assert.That(dst[24].Z, Is.EqualTo(b.Z).Within(1e-3f));
            Assert.That(HoseBundle.MinY(dst, 25), Is.LessThan(Math.Min(a.Y, b.Y) - 0.05f));
            HoseBundle.Sample(a, b, 1.18f, 0.2f, 8, dst);
            Assert.That(Math.Abs(dst[0].X - a.X), Is.GreaterThan(0.1f));
        }

        [Test]
        public void SwingArmLeavesBeforeLiftoffAndTailMastDropsAtT0()
        {
            var pts = Mixed();
            var L = PadGenerator.Generate(6, PadArchetype.Tower, new PadFit(70f, 2.5f), pts);
            var ev = UmbilicalTimeline.Build(pts);
            Assert.That(UmbilicalTimeline.Validate(pts, ev), Is.Empty);
            var fuel = L.Links.Find(u => u.AttachId == "s1.fuel.0");
            var tail = L.Links.Find(u => u.AttachId == "s0.lox.0");
            var lanyard = L.Links.Find(u => u.AttachId == "s0.pwr.0");

            ServiceArmMotion.Sample(ev, fuel.AttachId, -160, out var st, out var sinceR, out var sinceD);
            Assert.That(st, Is.EqualTo(UmbilicalState.Flowing));
            var mated = ServiceArmMotion.At(fuel, st, sinceR, sinceD);
            Assert.That(mated.Retract, Is.EqualTo(0f));
            Assert.That(mated.YawDeg, Is.EqualTo(fuel.MatedYaw).Within(1e-3f));

            // Plates start to part at Disconnect; the arm itself waits for ArmRetractStart (+0.2 s).
            ServiceArmMotion.Sample(ev, fuel.AttachId, -149.95, out st, out sinceR, out sinceD);
            var parting = ServiceArmMotion.At(fuel, st, sinceR, sinceD);
            Assert.That(st, Is.EqualTo(UmbilicalState.Released));
            Assert.That(parting.Retract, Is.EqualTo(0f));
            Assert.That(parting.Gap, Is.GreaterThan(QuickDisconnect.Thickness));

            ServiceArmMotion.Sample(ev, fuel.AttachId, -145.8, out st, out sinceR, out sinceD);
            var mid = ServiceArmMotion.At(fuel, st, sinceR, sinceD);
            Assert.That(st, Is.EqualTo(UmbilicalState.Released));
            Assert.That(mid.Retract, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(mid.YawDeg, Is.EqualTo(fuel.MatedYaw + 0.5f * fuel.RetractYawDeg).Within(1e-3f));

            ServiceArmMotion.Sample(ev, fuel.AttachId, -100, out st, out sinceR, out sinceD);
            var away = ServiceArmMotion.At(fuel, st, sinceR, sinceD);
            Assert.That(st, Is.EqualTo(UmbilicalState.Retracted));
            Assert.That(away.YawDeg, Is.EqualTo(fuel.MatedYaw + fuel.RetractYawDeg).Within(1e-3f));
            Assert.That(Math.Abs(away.HoseVehicle.X - fuel.Plate.X) + Math.Abs(away.HoseVehicle.Z - fuel.Plate.Z),
                Is.GreaterThan(1f), "retracted hose has left the skin plate");

            ServiceArmMotion.Sample(ev, tail.AttachId, -1, out st, out sinceR, out sinceD);
            Assert.That(ServiceArmMotion.At(tail, st, sinceR, sinceD).PitchDeg, Is.EqualTo(0f));
            ServiceArmMotion.Sample(ev, tail.AttachId, 0, out st, out sinceR, out sinceD);
            var dropStart = ServiceArmMotion.At(tail, st, sinceR, sinceD);
            Assert.That(st, Is.EqualTo(UmbilicalState.Released));
            Assert.That(dropStart.PitchDeg, Is.EqualTo(0f));
            Assert.That(dropStart.Gap, Is.EqualTo(QuickDisconnect.Thickness).Within(1e-4f));
            ServiceArmMotion.Sample(ev, tail.AttachId, 0.95, out st, out sinceR, out sinceD);
            var dropMid = ServiceArmMotion.At(tail, st, sinceR, sinceD);
            Assert.That(dropMid.PitchDeg, Is.EqualTo(tail.DropDeg * 0.5f).Within(0.05f));
            ServiceArmMotion.Sample(ev, tail.AttachId, 2, out st, out sinceR, out sinceD);
            Assert.That(st, Is.EqualTo(UmbilicalState.Retracted));
            Assert.That(ServiceArmMotion.At(tail, st, sinceR, sinceD).PitchDeg, Is.EqualTo(tail.DropDeg).Within(1e-3f));

            ServiceArmMotion.Sample(ev, lanyard.AttachId, 0, out st, out _, out _);
            Assert.That(st, Is.EqualTo(UmbilicalState.Connected), "lanyard is still mated at T-0");
            ServiceArmMotion.Sample(ev, lanyard.AttachId, 0.05, out st, out _, out _);
            Assert.That(st, Is.EqualTo(UmbilicalState.Released));
            ServiceArmMotion.Sample(ev, lanyard.AttachId, 1.55, out st, out _, out _);
            Assert.That(st, Is.EqualTo(UmbilicalState.Retracted));
        }

        [Test]
        public void AbortRematesUntilTheArmHasRetracted()
        {
            var id = "s1.fuel.0";
            var link = new ServiceLink { AttachId = id, Release = ReleaseStyle.SwingArmRetract, Rigid = ServiceRigid.Swing, MatedYaw = 180f, RetractYawDeg = 90f, Length = 6f, Anchor = new Vec3(0, 10, 8), Plate = new Vec3(0, 10, 2), Outward = new Vec3(0, 0, 1), PlateRadius = 0.3f };
            var ev = new List<UmbilicalEvent>
            {
                new UmbilicalEvent(id, UmbilicalEventKind.Connect, -10),
                new UmbilicalEvent(id, UmbilicalEventKind.FlowStart, -8),
                new UmbilicalEvent(id, UmbilicalEventKind.Disconnect, -5),
                new UmbilicalEvent(id, UmbilicalEventKind.Abort, -4),
            };
            ServiceArmMotion.Sample(ev, id, -4, out var st, out var sinceR, out var sinceD);
            Assert.That(st, Is.EqualTo(UmbilicalState.Connected));
            Assert.That(ServiceArmMotion.At(link, st, sinceR, sinceD).YawDeg, Is.EqualTo(180f).Within(1e-3f));

            ev = UmbilicalTimeline.Build(new[] { new AttachPoint { Id = id, Type = UmbilicalType.Fuel, Release = ReleaseStyle.SwingArmRetract } });
            ev.Add(new UmbilicalEvent(id, UmbilicalEventKind.Abort, -100));
            ev.Sort((a, b) => a.T.CompareTo(b.T));
            ServiceArmMotion.Sample(ev, id, -100, out st, out _, out _);
            Assert.That(st, Is.EqualTo(UmbilicalState.Retracted), "a scrub after retract does not swing the arm back");
        }

        [Test]
        public void AccessArmSwingsClearBeforeLiftoff()
        {
            Assert.That(ServiceArmMotion.AccessYaw(double.NaN), Is.EqualTo(0f));
            Assert.That(ServiceArmMotion.AccessYaw(-140), Is.EqualTo(0f));
            Assert.That(ServiceArmMotion.AccessYaw(-130), Is.EqualTo(0f));
            Assert.That(ServiceArmMotion.AccessYaw(-126), Is.EqualTo(ServiceArmMotion.AccessRetractYaw * 0.5f).Within(1e-3f));
            Assert.That(ServiceArmMotion.AccessYaw(-122), Is.EqualTo(ServiceArmMotion.AccessRetractYaw).Within(1e-3f));
            Assert.That(ServiceArmMotion.AccessYaw(0), Is.EqualTo(ServiceArmMotion.AccessRetractYaw).Within(1e-3f));
            var pivot = new Vec3(0, 40, 10);
            var tip = new Vec3(0, 40, 4);
            var swung = ServiceArmMotion.YawAbout(pivot, tip, 90f);
            Assert.That(swung.X, Is.EqualTo(-6f).Within(1e-3f));
            Assert.That(swung.Z, Is.EqualTo(pivot.Z).Within(1e-3f));
            Assert.That(swung.Y, Is.EqualTo(40f).Within(1e-3f));
        }

        [Test]
        public void ShellPlatesTrackDesignerDiameterAndHeight()
        {
            var small = ShellPoints(1.4f, 6f, 2f);
            var large = ShellPoints(8f, 28f, 5f);
            float Radial(AttachPoint p, Vec3 origin) { float x = p.Pose.Position.X - origin.X, z = p.Pose.Position.Z - origin.Z; return (float)Math.Sqrt(x * x + z * z); }
            Assert.That(Radial(Fuel(large.Points), large.Origin), Is.GreaterThan(Radial(Fuel(small.Points), small.Origin) + 2f));
            Assert.That(Fuel(large.Points).Pose.Position.Y, Is.GreaterThan(Fuel(small.Points).Pose.Position.Y + 10f));

            foreach (var built in new[] { small, large })
            {
                var spec = built.Spec; var origin = built.Origin;
                foreach (var p in built.Points)
                {
                    float yLocal = p.Pose.Position.Y - origin.Y;
                    Assert.That(TankShellUmbilicals.TrySkin(spec, yLocal, out float r, out _, out _), Is.True, p.Id);
                    Assert.That(Radial(p, origin), Is.EqualTo(r).Within(0.02f), p.Id);
                    var outward = ContractMath.Rotate(p.Pose.Rotation, new Vec3(0, 0, 1));
                    var radial = ContractMath.Norm(new Vec3(p.Pose.Position.X - origin.X, 0, p.Pose.Position.Z - origin.Z));
                    Assert.That(ContractMath.Dot(outward, radial), Is.GreaterThan(0.99f), p.Id);
                }
                Assert.That(UmbilicalTimeline.Validate(built.Points, UmbilicalTimeline.Build(built.Points)), Is.Empty);

                float height = origin.Y + spec.ShellTop + 8f;
                float radius = spec.Radius;
                var archetype = radius * 2f >= 3f ? PadArchetype.Tower : PadArchetype.Rail;
                var layout = PadGenerator.Generate(3, archetype, new PadFit(Math.Max(height, 20f), radius), built.Points);
                Assert.That(layout.Links.Count, Is.EqualTo(built.Points.Count));
                if (archetype == PadArchetype.Rail) Assert.That(layout.Anchors, Is.Null);
                foreach (var link in layout.Links)
                {
                    var pose = ServiceArmMotion.At(link, UmbilicalState.Connected, 0, 0);
                    var path = new Vec3[17];
                    HoseBundle.Sample(pose.HoseTower, pose.HoseVehicle, link.Strands[0].Slack, 0f, 16, path);
                    Assert.That(path[0].Y, Is.EqualTo(pose.HoseTower.Y).Within(1e-3f));
                    Assert.That(path[16].Y, Is.EqualTo(pose.HoseVehicle.Y).Within(1e-3f));
                    float span = Math.Abs(pose.HoseTower.Z - pose.HoseVehicle.Z) + Math.Abs(pose.HoseTower.X - pose.HoseVehicle.X);
                    if (span > 1.5f) Assert.That(HoseBundle.MinY(path, 17), Is.LessThan(Math.Min(pose.HoseTower.Y, pose.HoseVehicle.Y) - 0.02f), link.AttachId);
                }
            }
        }

        [Test]
        public void IntoPadFrameRoundTripsAVehicleYaw()
        {
            var pts = Mixed();
            var pad = UmbilicalRig.IntoPadFrame(pts, ContractMath.Yaw(37f));
            var back = ContractMath.Rotate(ContractMath.Yaw(-37f), pad[0].Pose.Position);
            Assert.That(back.X, Is.EqualTo(pts[0].Pose.Position.X).Within(1e-4f));
            Assert.That(back.Y, Is.EqualTo(pts[0].Pose.Position.Y).Within(1e-4f));
            Assert.That(back.Z, Is.EqualTo(pts[0].Pose.Position.Z).Within(1e-4f));
            var z = ContractMath.Rotate(ContractMath.Mul(ContractMath.Yaw(-37f), pad[0].Pose.Rotation), new Vec3(0, 0, 1));
            Assert.That(z.Z, Is.EqualTo(1f).Within(1e-4f));
        }

        struct Built { public TankShellSpec Spec; public Vec3 Origin; public List<AttachPoint> Points; }
        static Built ShellPoints(float diameter, float barrel, float barrelBottom)
        {
            var spec = TankShellSpec.Default(diameter * 0.5f, barrel, 11);
            var origin = TankShellUmbilicals.TankOrigin(barrelBottom, spec.Radius, 0, 0);
            var pts = TankShellUmbilicals.ForStage(spec, 0, 0, origin, true, true, true, ReleaseStyle.TailServiceMastDrop);
            return new Built { Spec = spec, Origin = origin, Points = pts };
        }
        static AttachPoint Fuel(List<AttachPoint> pts) => pts.Find(p => p.Type == UmbilicalType.Fuel);
    }
}
