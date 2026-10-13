// Engine-free: Unity EditMode (Kestrel.Vehicle.Tests) and dotnet (Tests/Dotnet~).
using System;
using System.Linq;
using Kestrel.Kit.Contracts;
using NUnit.Framework;

namespace Kestrel.Vehicle.Tests
{
    public class EngineBayPortTests
    {
        static BayHardware Liquid(bool pump, bool gimbal) => new BayHardware
        {
            Liquid = true, PumpFed = pump, Gimbal = gimbal, Release = ReleaseStyle.TailServiceMastDrop
        };

        [Test]
        public void StageZeroPlatesUseContractIdsAndFaceOutward()
        {
            float radius = 1.85f, bay = 4.2f;
            var pts = EngineBayPorts.Build(0, radius, bay, Liquid(pump: true, gimbal: true));
            Assert.That(pts.Select(p => p.Id), Is.EqualTo(new[]
            {
                UmbilicalIds.Make(0, UmbilicalType.Fuel, 0),
                UmbilicalIds.Make(0, UmbilicalType.Oxidizer, 0),
                UmbilicalIds.Make(0, UmbilicalType.PowerData, 0),
                UmbilicalIds.Make(0, UmbilicalType.Purge, 0),
                UmbilicalIds.Make(0, UmbilicalType.Hydraulic, 0)
            }));
            Assert.That(pts.Select(p => p.Id).Distinct().Count(), Is.EqualTo(pts.Count));
            foreach (var p in pts)
            {
                Assert.That(p.Stage, Is.EqualTo(0));
                Assert.That(p.Release, Is.EqualTo(ReleaseStyle.TailServiceMastDrop));
                float radial = (float)Math.Sqrt(p.Pose.Position.X * p.Pose.Position.X + p.Pose.Position.Z * p.Pose.Position.Z);
                Assert.That(radial, Is.EqualTo(radius).Within(0.02f));
                PoseMath.RotateZ(p.Pose.Rotation, out float zx, out float zy, out float zz);
                float dot = (zx * p.Pose.Position.X + zz * p.Pose.Position.Z) / Math.Max(1e-6f, radial);
                Assert.That(dot, Is.GreaterThan(0.99f), p.Id);
                Assert.That(Math.Abs(zy), Is.LessThan(0.05f), p.Id);
            }
            var timeline = UmbilicalTimeline.Build(pts);
            Assert.That(UmbilicalTimeline.Validate(pts, timeline), Is.Empty);
        }

        [Test]
        public void HardwareThatIsNotThereDoesNotGetAPlate()
        {
            var noPump = EngineBayPorts.Build(0, 1.5f, 3f, Liquid(pump: false, gimbal: false));
            Assert.That(noPump.Any(p => p.Type == UmbilicalType.Purge), Is.False);
            Assert.That(noPump.Any(p => p.Type == UmbilicalType.Hydraulic), Is.False);
            Assert.That(noPump.Any(p => p.Type == UmbilicalType.Fuel), Is.True);

            var solid = EngineBayPorts.Build(1, 1.2f, 2.5f, new BayHardware { Liquid = false, Gimbal = false, Release = ReleaseStyle.LanyardPull });
            Assert.That(solid.Select(p => p.Type), Is.EqualTo(new[] { UmbilicalType.PowerData }));
            Assert.That(solid[0].Id, Is.EqualTo(UmbilicalIds.Make(1, UmbilicalType.PowerData, 0)));
            Assert.That(solid[0].Release, Is.EqualTo(ReleaseStyle.LanyardPull));
            Assert.That(solid[0].Stage, Is.EqualTo(1));
        }
    }
}
