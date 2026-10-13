// Engine-free: Unity EditMode (Kestrel.Vehicle.Tests) and dotnet (Tests/Dotnet~).
using System;
using NUnit.Framework;

namespace Kestrel.Vehicle.Tests
{
    public class BayMeshTests
    {
        static BellSpec Gg(ulong seed = 11) => new BellSpec
        {
            EngineId = "gg", Cycle = EngineCycleKind.GasGenerator, ExpansionRatio = 16f, ExitDiameter = 0.9f, VehicleSeed = seed
        };

        [Test]
        public void LinesAreSixSideTubesAndSolidsAreLathes()
        {
            var line = BayMesh.Build(EngineBayLayout.FeedLine, 0);
            Assert.That(line.TriangleCount, Is.EqualTo(12));
            Assert.That(BayMesh.Build(EngineBayLayout.Actuator, 0).TriangleCount, Is.EqualTo(12));
            Assert.That(BayMesh.Build(EngineBayLayout.ExhaustDuct, 0).TriangleCount, Is.EqualTo(12));
            Assert.That(BayMesh.Build(EngineBayLayout.Turbopump, 0).TriangleCount, Is.GreaterThan(40));
            Assert.That(BayMesh.Build(EngineBayLayout.Valve, 0).TriangleCount, Is.GreaterThan(20));
            AssertNoNaN(line);
            AssertNoNaN(BayMesh.Build(EngineBayLayout.Turbopump, 0));
        }

        [Test]
        public void LodDropsValvesThenEverythingButPumpAndFeed()
        {
            Assert.That(BayMesh.Draws(EngineBayLayout.Valve, 0), Is.True);
            Assert.That(BayMesh.TriangleCount(EngineBayLayout.Valve, 1), Is.EqualTo(0));
            Assert.That(BayMesh.TriangleCount(EngineBayLayout.Preburner, 2), Is.EqualTo(0));
            Assert.That(BayMesh.TriangleCount(EngineBayLayout.Actuator, 2), Is.EqualTo(0));
            Assert.That(BayMesh.TriangleCount(EngineBayLayout.Turbopump, 2), Is.GreaterThan(0));
            Assert.That(BayMesh.TriangleCount(EngineBayLayout.FeedLine, 2), Is.GreaterThan(0));
            Assert.That(BayMesh.TriangleCount(EngineBayLayout.Turbopump, 1), Is.LessThan(BayMesh.TriangleCount(EngineBayLayout.Turbopump, 0)));
        }

        [Test]
        public void NineGasGeneratorBaysStayInsideTheLowBudget()
        {
            var parts = EngineBayLayout.Build(Gg(), true, 1.8f);
            int one = BayMesh.TriangleCount(parts, 0);
            int tris = one * 9;
            int draws = BayMesh.DrawCalls(parts, 0);
            Assert.That(tris, Is.LessThan(8000), "bay tris for 9 gimballed GG engines");
            Assert.That(draws, Is.LessThanOrEqualTo(6));
            Assert.That(draws, Is.GreaterThanOrEqualTo(4));
            Assert.That(BayMesh.TriangleCount(parts, 2) * 9, Is.LessThan(tris));
            Assert.That(BayMesh.DrawCalls(parts, 2), Is.EqualTo(2));
        }

        [Test]
        public void PosesStayInsideTheSkirt()
        {
            foreach (EngineCycleKind cycle in Enum.GetValues(typeof(EngineCycleKind)))
            {
                var spec = new BellSpec
                {
                    EngineId = "e", Cycle = cycle, ExpansionRatio = 18f, ExitDiameter = 1.1f, VehicleSeed = 7
                };
                var poses = EngineBayLayout.Poses(spec, true, 1.7f);
                Assert.That(poses.Count, Is.EqualTo(EngineBayLayout.Build(spec, true, 1.7f).Count));
                for (int i = 0; i < poses.Count; i++)
                {
                    Assert.That(EngineBayLayout.InsideSkin(poses[i], 1.7f), Is.True, cycle + " " + poses[i].Kind);
                    Assert.That(poses[i].Radius, Is.GreaterThan(0.005f));
                    Assert.That(poses[i].Length, Is.GreaterThan(0.01f));
                    float axis = (float)Math.Sqrt(poses[i].AxisX * poses[i].AxisX + poses[i].AxisY * poses[i].AxisY + poses[i].AxisZ * poses[i].AxisZ);
                    Assert.That(axis, Is.EqualTo(1f).Within(1e-4f));
                }
            }
        }

        static void AssertNoNaN(MeshData mesh)
        {
            for (int i = 0; i < mesh.Positions.Count; i++)
                Assert.That(float.IsNaN(mesh.Positions[i]) || float.IsInfinity(mesh.Positions[i]), Is.False);
        }
    }
}
