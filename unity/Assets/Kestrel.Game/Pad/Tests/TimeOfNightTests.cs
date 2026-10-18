using System.Linq;
using Kestrel.Kit;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class TimeOfNightTests
    {
        static readonly PadFit Heavy = new PadFit(70f, 2.5f);

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void TotalRealtimeBudgetIncludingPlumeLight(int q)
        {
            Assert.That(KitBudgets.PadPlumeLights(q), Is.LessThanOrEqualTo(KitBudgets.PadRealtimeLights(q)));
            foreach (var t in new[] { PadArchetype.Rail, PadArchetype.Tower, PadArchetype.TowerStrongback })
            {
                var plan = PadLighting.Plan(PadGenerator.Generate(4, t, t == PadArchetype.Rail ? new PadFit(15f, 0.6f) : Heavy), q);
                int fixtures = PadLighting.CountRealtime(plan);
                Assert.That(fixtures + KitBudgets.PadPlumeLights(q), Is.LessThanOrEqualTo(KitBudgets.PadRealtimeLights(q)), $"{t} q{q}");
                Assert.That(plan.Count(s => s.Shadows), Is.LessThanOrEqualTo(KitBudgets.PadShadowedLights(q)));
            }
            Assert.That(KitBudgets.PadPlumeLights(KitBudgets.Low) + KitBudgets.PadPlumeLights(KitBudgets.Medium), Is.EqualTo(0), "plume light is High only");
        }

        [Test]
        public void SpotsRingTheDeckAndLightTheRocket()
        {
            var L = PadGenerator.Generate(3, PadArchetype.Tower, Heavy);
            var spots = PadLighting.Plan(L, 2).Where(s => s.Kind == LightKind.Spot).ToList();
            Assert.That(spots.Count, Is.GreaterThanOrEqualTo(4));
            foreach (var s in spots)
            {
                float r = (float)System.Math.Sqrt(s.Position.X * s.Position.X + s.Position.Z * s.Position.Z);
                Assert.That(r, Is.GreaterThan(L.DeckRadius), s.Id); Assert.That(s.Position.Y, Is.LessThan(2f), "ground mounted");
                Assert.That(s.Aim.Y, Is.GreaterThan(0.2f), s.Id + " aims up at the rocket");
                float b = (float)(System.Math.Atan2(s.Position.X, s.Position.Z) * 180 / System.Math.PI);
                Assert.That(System.Math.Abs(((b + 360) % 360) - 90) >= 25f && System.Math.Abs(((b + 360) % 360) - 270) >= 25f, Is.True, s.Id + " in plume sector");
                // the beam hits the vehicle axis
                Assert.That(System.Math.Abs(s.Position.X + s.Aim.X * s.Throw) + System.Math.Abs(s.Position.Z + s.Aim.Z * s.Throw), Is.LessThan(0.1f));
            }
            Assert.That(PadLighting.Plan(L, 2).Count(s => s.Kind == LightKind.Spot && s.Realtime), Is.GreaterThanOrEqualTo(2));
            Assert.That(PadLighting.Plan(L, 0).Count(s => s.Realtime), Is.EqualTo(0));
            Assert.That(PadLighting.Plan(L, 2).Any(s => s.Kind == LightKind.Window), Is.True);
        }

        [Test]
        public void PlumeSplashTracksThrottleAndFadesWithHeight()
        {
            Assert.That(PlumeSplash.Strength(0f, 0f, 20f), Is.EqualTo(0f));
            float prev = 0f;
            for (float t = 0f; t <= 1f; t += 0.1f) { float s = PlumeSplash.Strength(t, 5f, 20f); Assert.That(s, Is.GreaterThanOrEqualTo(prev)); prev = s; }
            prev = 2f;
            for (float h = 0f; h <= 2000f; h += 25f) { float s = PlumeSplash.Strength(1f, h, 20f); Assert.That(s, Is.LessThanOrEqualTo(prev)); prev = s; }
            Assert.That(PlumeSplash.Strength(1f, 0f, 20f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(PlumeSplash.Strength(1f, 30f, 20f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(PlumeSplash.Strength(1f, 500f, 20f), Is.LessThan(0.01f));
            Assert.That(PlumeSplash.NightScale(0f), Is.LessThan(PlumeSplash.NightScale(1f))); Assert.That(PlumeSplash.NightScale(1f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void PlanStillDeterministicWithSpots()
        {
            var a = PadLighting.Plan(PadGenerator.Generate(7, PadArchetype.TowerStrongback, Heavy), 2);
            var b = PadLighting.Plan(PadGenerator.Generate(7, PadArchetype.TowerStrongback, Heavy), 2);
            Assert.That(a.Select(s => s.Id + s.Position.X + s.Realtime + s.Shadows), Is.EqualTo(b.Select(s => s.Id + s.Position.X + s.Realtime + s.Shadows)));
        }
    }
}
