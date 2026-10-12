using System.Linq;
using Kestrel.Kit;
using NUnit.Framework;

namespace Kestrel.Pad.Tests
{
    public class PadLightingTests
    {
        static readonly PadFit Heavy = new PadFit(70f, 2.5f);

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void RealtimeLightsStayInsideTierBudget(int q)
        {
            foreach (var t in new[] { PadArchetype.Rail, PadArchetype.Tower, PadArchetype.TowerStrongback })
            {
                var plan = PadLighting.Plan(PadGenerator.Generate(5, t, t == PadArchetype.Rail ? new PadFit(15f, 0.6f) : Heavy), q);
                Assert.That(PadLighting.CountRealtime(plan), Is.LessThanOrEqualTo(KitBudgets.PadRealtimeLights(q)));
                Assert.That(plan.Count(s => s.Shadows), Is.LessThanOrEqualTo(KitBudgets.PadShadowedLights(q)));
            }
            Assert.That(KitBudgets.PadRealtimeLights(KitBudgets.Low), Is.EqualTo(0));
        }

        [Test]
        public void TowerPadHasFloodsAviationAndPoles()
        {
            var plan = PadLighting.Plan(PadGenerator.Generate(3, PadArchetype.Tower, Heavy), 2);
            Assert.That(plan.Count(s => s.Kind == LightKind.Flood), Is.GreaterThanOrEqualTo(4));
            Assert.That(plan.Any(s => s.Id == "av.tower.cap" && s.BlinkHz > 0), Is.True);
            Assert.That(plan.Count(s => s.Kind == LightKind.Pole), Is.GreaterThanOrEqualTo(4));
            Assert.That(plan.Select(s => s.Id).Distinct().Count(), Is.EqualTo(plan.Count));
        }

        [Test]
        public void FloodsAimAtTheVehicleAndPolesAvoidPlumeSectors()
        {
            var L = PadGenerator.Generate(8, PadArchetype.TowerStrongback, Heavy);
            foreach (var s in PadLighting.Plan(L, 2))
            {
                if (s.Kind == LightKind.Flood) { Assert.That(s.Aim.Z, Is.LessThan(0f), s.Id); Assert.That(s.Position.Z, Is.LessThanOrEqualTo(L.Tower.NearFaceZ), s.Id); }
                if (s.Kind == LightKind.Pole)
                {
                    float b = (float)(System.Math.Atan2(s.Position.X, s.Position.Z) * 180 / System.Math.PI); b = (b + 360) % 360;
                    Assert.That(System.Math.Abs(b - 90) >= 25f && System.Math.Abs(b - 270) >= 25f, Is.True, s.Id);
                    Assert.That(System.Math.Sqrt(s.Position.X * s.Position.X + s.Position.Z * s.Position.Z), Is.GreaterThan(L.DeckRadius), s.Id);
                }
            }
        }

        [Test]
        public void HighTierLightsBothSidesOfTheVehicle()
        {
            var rt = PadLighting.Plan(PadGenerator.Generate(4, PadArchetype.Tower, Heavy), 2).Where(s => s.Realtime && s.Kind == LightKind.Flood).ToList();
            Assert.That(rt.Any(s => s.Position.X < 0) && rt.Any(s => s.Position.X > 0), Is.True);
        }

        [Test]
        public void DeterministicPerSeedAndQuality()
        {
            var a = PadLighting.Plan(PadGenerator.Generate(6, PadArchetype.Tower, Heavy), 1);
            var b = PadLighting.Plan(PadGenerator.Generate(6, PadArchetype.Tower, Heavy), 1);
            Assert.That(a.Select(s => s.Id + s.Position.X + s.Realtime), Is.EqualTo(b.Select(s => s.Id + s.Position.X + s.Realtime)));
        }

        [Test]
        public void NightFactorAndBlink()
        {
            Assert.That(PadLighting.NightFactor(30f), Is.EqualTo(0f));
            Assert.That(PadLighting.NightFactor(-10f), Is.EqualTo(1f));
            Assert.That(PadLighting.NightFactor(0f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(PadLighting.Blink(0f, 3.3), Is.EqualTo(1f));
            Assert.That(PadLighting.Blink(0.5f, 0.1), Is.EqualTo(1f)); Assert.That(PadLighting.Blink(0.5f, 1.5), Is.LessThan(0.1f));
        }

        [Test]
        public void FloodsThrowEnoughLightAndLampsHaveHalos()
        {
            var plan = PadLighting.Plan(PadGenerator.Generate(3, PadArchetype.Tower, Heavy), 2);
            foreach (var s in plan.Where(s => s.Kind == LightKind.Flood || s.Kind == LightKind.Pole))
            {
                Assert.That(s.Throw, Is.GreaterThan(5f), s.Id); Assert.That(s.Range, Is.GreaterThan(s.Throw * 1.3f), s.Id);
                // inverse-square: intensity / Throw² == Illum at full night, zero by day
                Assert.That(PadLighting.RealtimeIntensity(s, 1f) / (s.Throw * s.Throw), Is.EqualTo(s.Illum).Within(1e-3f), s.Id);
                Assert.That(PadLighting.RealtimeIntensity(s, 0f), Is.EqualTo(0f));
                Assert.That(s.Halo, Is.GreaterThan(1f), s.Id);
                Assert.That(s.InnerSpotAngle, Is.LessThan(s.SpotAngle), s.Id);
            }
            Assert.That(plan.Where(s => s.Kind == LightKind.Aviation).All(s => s.Halo > 0), Is.True);
        }

        [Test]
        public void VolumetricConesFollowTier()
        {
            Assert.That(PadLighting.HasCone(LightKind.Flood, 0), Is.False);
            Assert.That(PadLighting.HasCone(LightKind.Flood, 1), Is.True); Assert.That(PadLighting.HasCone(LightKind.Pole, 1), Is.False);
            Assert.That(PadLighting.HasCone(LightKind.Pole, 2), Is.True); Assert.That(PadLighting.HasCone(LightKind.Aviation, 2), Is.False);
        }
    }
}
