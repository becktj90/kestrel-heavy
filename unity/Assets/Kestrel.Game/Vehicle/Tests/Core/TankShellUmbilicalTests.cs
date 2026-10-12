// Plates sit on the tank outer mould line for whatever diameter and barrel the designer asked for.
using System;
using Kestrel.Kit.Contracts;
using NUnit.Framework;

namespace Kestrel.Vehicle.Tests
{
    public class TankShellUmbilicalTests
    {
        [Test]
        public void BarrelSkinIsTheDesignerRadiusAndTheDomeTapers()
        {
            var s = TankShellSpec.Default(2.2f, 16f, 3);
            float mid = s.BarrelBottom + 0.5f * s.BarrelLength;
            Assert.That(TankShellUmbilicals.TrySkin(s, mid, out float r, out float nr, out float ny), Is.True);
            Assert.That(r, Is.EqualTo(s.Radius).Within(1e-3f));
            Assert.That(nr, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(ny, Is.EqualTo(0f).Within(1e-3f));
            float domeY = s.BarrelTop + 0.45f * s.DomeRise;
            Assert.That(TankShellUmbilicals.TrySkin(s, domeY, out float rd, out _, out _), Is.True);
            Assert.That(rd, Is.LessThan(s.Radius - 0.05f));
            Assert.That(TankShellUmbilicals.TrySkin(s, -5f, out _, out _, out _), Is.False);
        }

        [Test]
        public void PlatesFollowDiameterHeightAndHardware()
        {
            var slim = TankShellSpec.Default(0.7f, 5f, 1);
            var wide = TankShellSpec.Default(4.2f, 5f, 1);
            var tall = TankShellSpec.Default(2f, 22f, 1);
            var o = new Vec3(0, 0, 0);
            var a = TankShellUmbilicals.ForStage(slim, 0, 0, o, true, false, false, ReleaseStyle.TailServiceMastDrop);
            var b = TankShellUmbilicals.ForStage(wide, 0, 0, o, true, true, true, ReleaseStyle.SwingArmRetract);
            var c = TankShellUmbilicals.ForStage(tall, 1, 0, new Vec3(0, 30f, 0), true, false, false, ReleaseStyle.SwingArmRetract);
            float Z(System.Collections.Generic.List<AttachPoint> pts) => Fuel(pts).Pose.Position.Z;
            Assert.That(Z(b), Is.GreaterThan(Z(a) + 2f));
            Assert.That(Fuel(c).Pose.Position.Y, Is.GreaterThan(Fuel(a).Pose.Position.Y + 20f));
            Assert.That(Fuel(a).Id, Is.EqualTo("s0.fuel.0"));
            Assert.That(Fuel(c).Id, Is.EqualTo("s1.fuel.0"));
            Assert.That(Fuel(a).Release, Is.EqualTo(ReleaseStyle.TailServiceMastDrop));
            Assert.That(Fuel(c).Release, Is.EqualTo(ReleaseStyle.SwingArmRetract));
            Assert.That(a.Exists(p => p.Type == UmbilicalType.Hydraulic), Is.False);
            Assert.That(b.Exists(p => p.Type == UmbilicalType.Hydraulic), Is.True);
            Assert.That(b.Exists(p => p.Type == UmbilicalType.Purge), Is.True);
            Assert.That(a.Exists(p => p.Type == UmbilicalType.Purge), Is.False);

            var solid = TankShellUmbilicals.ForStage(slim, 0, 0, o, false, true, false, ReleaseStyle.LanyardPull);
            Assert.That(solid.Count, Is.EqualTo(2)); // power + hydraulic
            Assert.That(solid.Exists(p => p.Type == UmbilicalType.Fuel), Is.False);
            Assert.That(UmbilicalTimeline.Validate(b, UmbilicalTimeline.Build(b)), Is.Empty);

            foreach (var p in b)
            {
                Assert.That(TankShellUmbilicals.TrySkin(wide, p.Pose.Position.Y, out float r, out _, out _), Is.True, p.Id);
                float rad = (float)Math.Sqrt(p.Pose.Position.X * p.Pose.Position.X + p.Pose.Position.Z * p.Pose.Position.Z);
                Assert.That(rad, Is.EqualTo(r).Within(0.02f), p.Id);
            }
        }

        static AttachPoint Fuel(System.Collections.Generic.List<AttachPoint> pts) => pts.Find(p => p.Type == UmbilicalType.Fuel);
    }
}
