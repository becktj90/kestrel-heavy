// Engine-free: Unity EditMode (Kestrel.Vehicle.Tests) and dotnet (Tests/Dotnet~).
using System;
using NUnit.Framework;

namespace Kestrel.Vehicle.Tests
{
    public class EngineBayLayoutTests
    {
        static BellSpec Spec(EngineCycleKind cycle, ulong seed = 3) => new BellSpec
        {
            EngineId = "e-" + cycle, Cycle = cycle, ExpansionRatio = 16f, ExitDiameter = 0.9f, VehicleSeed = seed
        };

        [Test]
        public void CycleSetsPumpDuctAndFeedCounts()
        {
            var pf = EngineBayLayout.Build(Spec(EngineCycleKind.PressureFed), false, 1.8f);
            Assert.That(EngineBayLayout.Count(pf, EngineBayLayout.Turbopump), Is.EqualTo(0));
            Assert.That(EngineBayLayout.Count(pf, EngineBayLayout.FeedLine), Is.EqualTo(2));

            var ff = EngineBayLayout.Build(Spec(EngineCycleKind.FullFlow), false, 1.8f);
            Assert.That(EngineBayLayout.Count(ff, EngineBayLayout.Turbopump), Is.EqualTo(2));
            Assert.That(EngineBayLayout.Count(ff, EngineBayLayout.ExhaustDuct), Is.EqualTo(0));
            Assert.That(EngineBayLayout.Count(ff, EngineBayLayout.Preburner), Is.EqualTo(2));

            var gg = EngineBayLayout.Build(Spec(EngineCycleKind.GasGenerator), true, 1.8f);
            Assert.That(EngineBayLayout.Count(gg, EngineBayLayout.ExhaustDuct), Is.EqualTo(1));
            Assert.That(EngineBayLayout.Count(gg, EngineBayLayout.Turbopump), Is.EqualTo(1));
            Assert.That(EngineBayLayout.Count(gg, EngineBayLayout.Actuator), Is.EqualTo(2));

            var solid = EngineBayLayout.Build(Spec(EngineCycleKind.Solid), false, 1.6f);
            Assert.That(EngineBayLayout.Count(solid, EngineBayLayout.FeedLine), Is.EqualTo(0));
            Assert.That(EngineBayLayout.Count(solid, EngineBayLayout.Turbopump), Is.EqualTo(0));
            Assert.That(EngineBayLayout.Count(solid, EngineBayLayout.Actuator), Is.EqualTo(0));

            var hybrid = EngineBayLayout.Build(Spec(EngineCycleKind.Hybrid), false, 1.4f);
            Assert.That(EngineBayLayout.Count(hybrid, EngineBayLayout.FeedLine), Is.EqualTo(1));
            Assert.That(EngineBayLayout.Count(hybrid, EngineBayLayout.Turbopump), Is.EqualTo(0));
        }

        [Test]
        public void LayoutStaysInsideTheSkirtAndRepeatsForTheSameSeed()
        {
            var spec = Spec(EngineCycleKind.GasGenerator, 11);
            var a = EngineBayLayout.Build(spec, true, 1.5f);
            var b = EngineBayLayout.Build(spec, true, 1.5f);
            Assert.That(a.Count, Is.EqualTo(b.Count));
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(a[i].Kind, Is.EqualTo(b[i].Kind));
                Assert.That(a[i].X, Is.EqualTo(b[i].X).Within(1e-5f));
                Assert.That(a[i].Y, Is.EqualTo(b[i].Y).Within(1e-5f));
                float radial = (float)Math.Sqrt(a[i].X * a[i].X + a[i].Z * a[i].Z);
                Assert.That(radial, Is.LessThan(1.5f));
                Assert.That(a[i].Y, Is.GreaterThan(0f));
            }
            var otherSeed = false;
            for (ulong s = 1; s <= 16 && !otherSeed; s++)
            {
                var other = EngineBayLayout.Build(Spec(EngineCycleKind.GasGenerator, s), true, 1.5f);
                otherSeed = Math.Abs(other[0].X - a[0].X) > 1e-3f || Math.Abs(other[0].Z - a[0].Z) > 1e-3f;
            }
            Assert.That(otherSeed, Is.True, "vehicle seed picks the bay azimuth");
        }
    }
}
